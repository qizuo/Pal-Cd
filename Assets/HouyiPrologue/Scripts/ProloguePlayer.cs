using UnityEngine;
using Wuxia.Archery;

namespace Houyi.NineSuns
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public sealed class ProloguePlayer : MonoBehaviour
    {
        public ArcherDemo archer;
        public Camera view;
        public PrologueDirector story;
        public Animation locomotion;
        public float yaw, pitch = 13;
        [Tooltip("PC 鼠标转向平滑时间（秒），越大阻尼越明显。")]
        [Range(0, .2f)] public float mouseLookDamping = .045f;
        [Tooltip("在电脑上显示触控界面，可按 F2 切换，用鼠标模拟单指操作。")]
        public bool previewTouchControls;
        public PrologueTouchControls TouchControls { get; private set; }
        public PrologueSunburst Sunburst { get; private set; }
        UnityEngine.CharacterController motor;
        float verticalSpeed;
        Vector3 smoothCamera;
        AudioSource bowAudio;
        AudioClip bowSound;
        public bool IsMoving { get; private set; }
        public const float MaxStamina=100, SprintStaminaPerSecond=5, DodgeStaminaCost=20, StaminaRecoveryPerSecond=10, StaminaRecoveryDelay=1;
        public float Stamina { get; private set; } = MaxStamina;
        float staminaQuietTime;
        bool sprintExhausted;
        const float DodgeDuration = .18f, DodgeDistance = 3f, DodgeCooldown = .5f;
        float dodgeRemaining, nextDodgeTime;
        Vector3 dodgeDirection;
        Vector2 lastKeyboardDirection = Vector2.up;
        readonly PrologueShiftInput shiftInput = new PrologueShiftInput();
        float mouseTargetYaw, mouseTargetPitch;
        bool mouseLookInitialized;
        bool shootingPose;
        public float DodgeCooldownRemaining => Mathf.Max(0, nextDodgeTime - Time.time);

        void Awake()
        {
            motor = GetComponent<UnityEngine.CharacterController>();
            motor.minMoveDistance = 0; // Preserve gentle analogue joystick movement.
            archer.autoDemo = false; archer.acceptDemoInput = false;
            archer.showDemoHUD = false; archer.useWorldCollision = true;
            smoothCamera = CameraPosition();
            TouchControls = GetComponent<PrologueTouchControls>();
            if (!TouchControls) TouchControls = gameObject.AddComponent<PrologueTouchControls>();
            Sunburst = GetComponent<PrologueSunburst>();
            if (!Sunburst) Sunburst = gameObject.AddComponent<PrologueSunburst>();
        }
        void Start()
        {
            bowAudio = gameObject.AddComponent<AudioSource>(); bowAudio.volume = .32f;
            bowSound = AudioClip.Create("Bowstring pluck", 6600, 1, 22050, false);
            var samples = new float[6600];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / 22050f;
                samples[i] = (Mathf.Sin(t * 1900) + .35f * Mathf.Sin(t * 4300)) * Mathf.Exp(-t * 27) * .45f;
            }
            bowSound.SetData(samples, 0); archer.ArrowReleased += PlayBow;
        }
        void PlayBow(float charge) { if (bowAudio) { bowAudio.pitch = 1.2f - charge * .3f; bowAudio.PlayOneShot(bowSound); } }
        void Update()
        {
            bool control = story && story.CanControl && !story.TestButtonPointerActive;
            if (!control) dodgeRemaining = 0;
            bool touchMode = TouchControls && TouchControls.Active;
            bool mouseLook = control && !touchMode && Application.isFocused;
            bool cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = mouseLook ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !mouseLook;
            if (!mouseLook || !cursorWasLocked) mouseLookInitialized = false;
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool shiftTap = shiftInput.Process(shiftHeld, Time.unscaledTime, control && !touchMode && Application.isFocused);
            PrologueTouchInput touch = touchMode ? TouchControls.InputState : null;
            if (!control && archer.CurrentPhase != ArcherDemo.Phase.Idle) archer.CancelDraw();
            if (control && touchMode)
            {
                yaw += touch.LookDelta.x * .12f;
                pitch = Mathf.Clamp(pitch + touch.LookDelta.y * .12f, -22, 48);
            }
            // Ignore the frame that captures the cursor to avoid a recentering jump.
            else if (mouseLook && cursorWasLocked)
            {
                if (!mouseLookInitialized)
                {
                    mouseTargetYaw = yaw; mouseTargetPitch = pitch;
                    mouseLookInitialized = true;
                }
                mouseTargetYaw = Mathf.Repeat(mouseTargetYaw + Input.GetAxis("Mouse X") * 2.4f, 360);
                mouseTargetPitch = Mathf.Clamp(mouseTargetPitch - Input.GetAxis("Mouse Y") * 1.9f, -22, 48);
                float blend = mouseLookDamping <= 0 ? 1 : 1 - Mathf.Exp(-Time.unscaledDeltaTime / mouseLookDamping);
                yaw = Mathf.LerpAngle(yaw, mouseTargetYaw, blend);
                pitch = Mathf.Lerp(pitch, mouseTargetPitch, blend);
            }
            Vector3 movement = Vector3.zero;
            if (control)
            {
                if (archer.IsHitStunned) dodgeRemaining = 0;
                // New presses interrupt other actions; held buttons never restart an action.
                if (touchMode)
                {
                    if (touch.FireCanceled) archer.CancelDraw();
                    else { if (touch.FirePressed) TryBeginAttack(); if (touch.FireReleased) archer.ReleaseDraw(); }
                }
                else
                {
                    if (Input.GetMouseButtonDown(0)) TryBeginAttack();
                    if (Input.GetMouseButtonUp(0)) archer.ReleaseDraw();
                }
                Vector2 axes = touchMode ? touch.Move : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                if (!touchMode && axes.sqrMagnitude > 0) lastKeyboardDirection = axes.normalized;
                movement = Quaternion.Euler(0, yaw, 0) * new Vector3(axes.x, 0, axes.y);
                movement = Vector3.ClampMagnitude(movement, 1);
                bool dodgePressed = touchMode ? touch.DodgePressed : shiftTap || Input.GetKeyDown(KeyCode.Space);
                if (dodgePressed)
                {
                    // Keep the last deliberate stick direction after the finger lifts.
                    Vector2 last = touchMode ? touch.LastMoveDirection : lastKeyboardDirection;
                    TryDodge(last);
                }
            }
            IsMoving = movement.sqrMagnitude > .01f || dodgeRemaining > 0;
            bool aiming = (touchMode ? touch.FireHeld || touch.LookDelta.sqrMagnitude > 0 : Input.GetMouseButton(0)) || archer.CurrentPhase != ArcherDemo.Phase.Idle;
            bool wantsSprint = control && !touchMode && shiftInput.Sprinting && !aiming && movement.sqrMagnitude > .01f && dodgeRemaining <= 0;
            bool sprinting = control && TickStamina(wantsSprint, Time.deltaTime);
            if (locomotion)
            {
                if (archer.IsShooting)
                {
                    if (!shootingPose && locomotion.GetClip("Idle"))
                        locomotion.GetClip("Idle").SampleAnimation(locomotion.gameObject, 0);
                    locomotion.Stop();
                }
                else if (!archer.IsHitStunned)
                {
                string clip = IsMoving ? "Run" : "Idle";
                if (!locomotion.IsPlaying(clip)) locomotion.CrossFade(clip, .18f);
                if (locomotion["Run"] != null) locomotion["Run"].speed = sprinting ? 1.35f : .85f;
                }
                shootingPose = archer.IsShooting;
            }
            // On PC, face the mouse-controlled camera yaw even while idle or strafing.
            Vector3 facing = !touchMode || aiming ? Quaternion.Euler(0, yaw, 0) * Vector3.forward : movement;
            if (control && facing.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), Time.deltaTime * 14);
            float speed = sprinting ? 5.2f : aiming ? 1.8f : 3.3f;
            verticalSpeed = motor.isGrounded ? -2 : verticalSpeed - 20 * Time.deltaTime;
            Vector3 horizontalStep = movement * speed * Time.deltaTime;
            if (dodgeRemaining > 0)
            {
                float step = Mathf.Min(dodgeRemaining, Time.deltaTime);
                horizontalStep = dodgeDirection * (DodgeDistance / DodgeDuration) * step;
                dodgeRemaining -= step;
            }
            if (motor.enabled) motor.Move(horizontalStep + Vector3.up * verticalSpeed * Time.deltaTime);
            if (control && view)
            {
                Ray ray = view.ViewportPointToRay(new Vector3(.5f, .5f));
                Vector3 point = Physics.Raycast(ray, out RaycastHit hit, 160, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point : ray.GetPoint(100);
                archer.aimDirection = (point - (transform.position + Vector3.up * 1.4f)).normalized;
            }
        }
        public bool TryBeginAttack()
        {
            if (!story || !story.CanControl || archer.IsShooting) return false;
            archer.BeginDraw();
            dodgeRemaining = 0;
            return true;
        }
        public bool TryDodge(Vector2 lastDirection)
        {
            if (!story || !story.CanControl || dodgeRemaining > 0 || DodgeCooldownRemaining > 0 || !motor.enabled || Stamina < DodgeStaminaCost) return false;
            Stamina -= DodgeStaminaCost;
            staminaQuietTime = 0;
            archer.CancelForAction();
            if (Sunburst) Sunburst.CancelSelection();
            Vector2 direction = lastDirection.sqrMagnitude < .01f ? Vector2.up
                : Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y)
                    ? new Vector2(Mathf.Sign(lastDirection.x), 0) : new Vector2(0, Mathf.Sign(lastDirection.y));
            dodgeDirection = Quaternion.Euler(0, yaw, 0) * new Vector3(direction.x, 0, direction.y);
            dodgeRemaining = DodgeDuration;
            nextDodgeTime = Time.time + DodgeCooldown;
            return true;
        }
        public bool TickStamina(bool wantsSprint, float dt)
        {
            dt = Mathf.Max(0, dt);
            if (!wantsSprint) sprintExhausted = false;
            if (wantsSprint && !sprintExhausted && Stamina > 0 && dt > 0)
            {
                Stamina = Mathf.Max(0, Stamina - SprintStaminaPerSecond * dt);
                staminaQuietTime = 0;
                if (Stamina <= 0) sprintExhausted = true;
                return true;
            }
            float previous = staminaQuietTime;
            staminaQuietTime += dt;
            float recoveryTime = Mathf.Max(0, staminaQuietTime - StaminaRecoveryDelay) - Mathf.Max(0, previous - StaminaRecoveryDelay);
            Stamina = Mathf.Min(MaxStamina, Stamina + StaminaRecoveryPerSecond * recoveryTime);
            return false;
        }
        public void RecoverStamina(float amount) { Stamina = Mathf.Clamp(Stamina + amount, 0, MaxStamina); }
        public void RestoreStamina()
        {
            Stamina = MaxStamina; staminaQuietTime = 0; sprintExhausted = false;
        }
        Vector3 CameraPosition()
        {
            Vector3 pivot = transform.position + Vector3.up * 1.55f;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            return pivot + rotation * new Vector3(.7f, .2f, -4.1f);
        }
        void LateUpdate()
        {
            if (!view) return;
            if (story && story.Step == PrologueDirector.ChapterStep.Intro)
            {
                view.transform.position = new Vector3(-12, 7, -20);
                view.transform.LookAt(new Vector3(1, 1.8f, 13));
                return;
            }
            Vector3 pivot = transform.position + Vector3.up * 1.55f;
            Vector3 desired = CameraPosition();
            Vector3 offset = desired - pivot;
            if (Physics.SphereCast(pivot, .18f, offset.normalized, out RaycastHit hit, offset.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                desired = pivot + offset.normalized * Mathf.Max(.65f, hit.distance - .15f);
            smoothCamera = Vector3.Lerp(smoothCamera, desired, 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
            view.transform.SetPositionAndRotation(smoothCamera, Quaternion.Euler(pitch, yaw, 0));
        }
        public void Teleport(Vector3 position)
        {
            if (Sunburst) Sunburst.CancelSelection();
            motor.enabled = false; transform.position = position; motor.enabled = true;
            verticalSpeed = 0; smoothCamera = CameraPosition();
            dodgeRemaining = 0;
            mouseLookInitialized = false;
        }
        void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                shiftInput.Cancel(true); dodgeRemaining = 0; mouseLookInitialized = false;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            }
        }
        void OnDisable() { shiftInput.Cancel(true); dodgeRemaining = 0; mouseLookInitialized = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnDestroy() { if (archer) archer.ArrowReleased -= PlayBow; if (bowSound) Destroy(bowSound); }
    }
}

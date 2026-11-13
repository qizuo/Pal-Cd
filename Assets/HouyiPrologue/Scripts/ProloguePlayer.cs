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
        [Tooltip("在电脑上显示触控界面，可按 F2 切换，用鼠标模拟单指操作。")]
        public bool previewTouchControls;
        public PrologueTouchControls TouchControls { get; private set; }
        UnityEngine.CharacterController motor;
        float verticalSpeed;
        Vector3 smoothCamera;
        AudioSource bowAudio;
        AudioClip bowSound;
        public bool IsMoving { get; private set; }
        const float DodgeDuration = .18f, DodgeDistance = 3f, DodgeCooldown = 1f;
        float dodgeRemaining, nextDodgeTime;
        Vector3 dodgeDirection;
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
            bool control = story && story.CanControl;
            if (!control) dodgeRemaining = 0;
            bool touchMode = TouchControls && TouchControls.Active;
            PrologueTouchInput touch = touchMode ? TouchControls.InputState : null;
            if (!control && archer.CurrentPhase != ArcherDemo.Phase.Idle) archer.CancelDraw();
            if (control && touchMode)
            {
                yaw += touch.LookDelta.x * .12f;
                pitch = Mathf.Clamp(pitch + touch.LookDelta.y * .12f, -22, 48);
            }
            else if (control && Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * 2.8f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2.3f, -22, 48);
            }
            Vector3 movement = Vector3.zero;
            if (control)
            {
                Vector2 axes = touchMode ? touch.Move : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                movement = Quaternion.Euler(0, yaw, 0) * new Vector3(axes.x, 0, axes.y);
                movement = Vector3.ClampMagnitude(movement, 1);
                if (touchMode && touch.DodgePressed && DodgeCooldownRemaining <= 0 && motor.enabled)
                {
                    // Keep the last deliberate stick direction after the finger lifts.
                    Vector2 last = touch.LastMoveDirection;
                    Vector2 direction = Mathf.Abs(last.x) > Mathf.Abs(last.y)
                        ? new Vector2(Mathf.Sign(last.x), 0) : new Vector2(0, Mathf.Sign(last.y));
                    dodgeDirection = Quaternion.Euler(0, yaw, 0) * new Vector3(direction.x, 0, direction.y);
                    dodgeRemaining = DodgeDuration;
                    nextDodgeTime = Time.time + DodgeCooldown;
                }
                if (touchMode)
                {
                    if (touch.FireCanceled) archer.CancelDraw();
                    else { if (touch.FirePressed) archer.BeginDraw(); if (touch.FireReleased) archer.ReleaseDraw(); }
                }
                else
                {
                    if (Input.GetMouseButtonDown(0)) archer.BeginDraw();
                    if (Input.GetMouseButtonUp(0)) archer.ReleaseDraw();
                }
            }
            IsMoving = movement.sqrMagnitude > .01f || dodgeRemaining > 0;
            if (locomotion)
            {
                string clip = IsMoving ? "Run" : "Idle";
                if (!locomotion.IsPlaying(clip)) locomotion.CrossFade(clip, .18f);
                if (locomotion["Run"] != null) locomotion["Run"].speed = !touchMode && Input.GetKey(KeyCode.LeftShift) ? 1.35f : .85f;
            }
            bool aiming = (touchMode ? touch.FireHeld || touch.LookDelta.sqrMagnitude > 0 : Input.GetMouseButton(1)) || archer.CurrentPhase != ArcherDemo.Phase.Idle;
            Vector3 facing = aiming ? Quaternion.Euler(0, yaw, 0) * Vector3.forward : movement;
            if (control && facing.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), Time.deltaTime * 14);
            float speed = !touchMode && Input.GetKey(KeyCode.LeftShift) && !aiming ? 5.2f : aiming ? 1.8f : 3.3f;
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
            motor.enabled = false; transform.position = position; motor.enabled = true;
            verticalSpeed = 0; smoothCamera = CameraPosition();
            dodgeRemaining = 0;
        }
        void OnDisable() { dodgeRemaining = 0; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnDestroy() { if (archer) archer.ArrowReleased -= PlayBow; if (bowSound) Destroy(bowSound); }
    }
}

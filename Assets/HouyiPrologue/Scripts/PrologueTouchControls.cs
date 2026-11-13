using System.Collections.Generic;
using UnityEngine;

namespace Houyi.NineSuns
{
    // Each finger keeps its original role until it lifts, even outside its starting region.
    public sealed class PrologueTouchInput
    {
        public enum Phase { Began, Moved, Stationary, Ended, Canceled }
        enum Role { Blocked, Move, Look, Fire, Dodge, AutoMoveCancel, Interact, Pause, Journal }
        sealed class Pointer { public Role role; public Vector2 last; public int frame; }
        readonly Dictionary<int, Pointer> pointers = new Dictionary<int, Pointer>();
        readonly List<int> stale = new List<int>();
        int frame, moveId = int.MinValue, lookId = int.MinValue, fireId = int.MinValue;
        public Vector2 Move { get; private set; }
        public Vector2 LastMoveDirection { get; private set; } = Vector2.up;
        public Vector2 LookDelta { get; private set; }
        public bool FireHeld => fireId != int.MinValue;
        public bool FirePressed { get; private set; }
        public bool FireReleased { get; private set; }
        public bool FireCanceled { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public bool JournalPressed { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool AutoMoving { get; private set; }
        public bool AutoMoveReady { get; private set; }
        float outerHoldStarted = -1, frameTime;
        public Vector2 StickCenter, FireCenter;
        public float StickRadius = 140, FireRadius = 105;
        public Rect LookArea, InteractRect, PauseRect, JournalRect, DodgeRect, AutoMoveRect;
        public bool ShowInteraction;

        public void BeginFrame()
        {
            BeginFrame(Time.time);
        }
        public void BeginFrame(float time)
        {
            frameTime = time;
            frame++; LookDelta = Vector2.zero;
            DodgePressed = false;
            FirePressed = FireReleased = FireCanceled = InteractPressed = PausePressed = JournalPressed = false;
        }
        public void Process(int id, Vector2 position, Phase phase)
        {
            if (phase == Phase.Began)
            {
                if (pointers.ContainsKey(id)) EndPointer(id, pointers[id], true, position);
                Role role = Role.Blocked;
                if (PauseRect.Contains(position)) role = Role.Pause;
                else if (JournalRect.Contains(position)) role = Role.Journal;
                else if (ShowInteraction && InteractRect.Contains(position)) role = Role.Interact;
                else if (AutoMoving && AutoMoveRect.Contains(position))
                { role = Role.AutoMoveCancel; CancelAutoMove(); }
                else if (DodgeRect.Contains(position)) { role = Role.Dodge; DodgePressed = true; }
                else if (Vector2.Distance(position, FireCenter) <= FireRadius)
                { if (fireId == int.MinValue) { role = Role.Fire; fireId = id; FirePressed = true; } }
                else if (Vector2.Distance(position, StickCenter) <= StickRadius && moveId == int.MinValue)
                { CancelAutoMove(); role = Role.Move; moveId = id; }
                else if (LookArea.Contains(position) && lookId == int.MinValue)
                { role = Role.Look; lookId = id; }
                pointers[id] = new Pointer { role = role, last = position, frame = frame };
            }
            if (!pointers.TryGetValue(id, out Pointer pointer)) return;
            pointer.frame = frame;
            if (phase == Phase.Ended || phase == Phase.Canceled)
            { EndPointer(id, pointer, phase == Phase.Canceled, position); return; }
            if (pointer.role == Role.Move)
            {
                Vector2 delta = (position - StickCenter) / StickRadius;
                delta.y = -delta.y;
                float magnitude = Mathf.Clamp01(delta.magnitude);
                Move = magnitude <= .15f ? Vector2.zero : delta.normalized * ((magnitude - .15f) / .85f);
                if (Move.sqrMagnitude > 0) LastMoveDirection = Move.normalized;
                // Top 90-degree sector, at least 95% of the stick radius.
                if (!AutoMoveReady)
                {
                    if (delta.magnitude >= .95f && delta.y > 0 && delta.y >= Mathf.Abs(delta.x))
                    {
                        if (outerHoldStarted < 0) outerHoldStarted = frameTime;
                        if (frameTime - outerHoldStarted >= 1f) AutoMoveReady = true;
                    }
                    else outerHoldStarted = -1;
                }
            }
            // The charging finger also aims, retaining its fire role outside the button.
            else if ((pointer.role == Role.Look || pointer.role == Role.Fire) && phase == Phase.Moved)
                LookDelta += position - pointer.last;
            pointer.last = position;
        }
        void EndPointer(int id, Pointer pointer, bool canceled, Vector2 position)
        {
            if (pointer.role == Role.Move)
            {
                AutoMoving = !canceled && AutoMoveReady && AutoMoveRect.Contains(position);
                Move = AutoMoving ? Vector2.up : Vector2.zero;
                if (AutoMoving) LastMoveDirection = Vector2.up;
                AutoMoveReady = false; outerHoldStarted = -1; moveId = int.MinValue;
            }
            if (pointer.role == Role.Look) lookId = int.MinValue;
            if (pointer.role == Role.Fire)
            { fireId = int.MinValue; if (canceled) FireCanceled = true; else FireReleased = true; }
            if (!canceled)
            {
                if (pointer.role == Role.Interact && ShowInteraction && InteractRect.Contains(position)) InteractPressed = true;
                if (pointer.role == Role.Pause && PauseRect.Contains(position)) PausePressed = true;
                if (pointer.role == Role.Journal && JournalRect.Contains(position)) JournalPressed = true;
            }
            pointers.Remove(id);
        }
        public void EndFrame()
        {
            stale.Clear();
            foreach (var pair in pointers) if (pair.Value.frame != frame) stale.Add(pair.Key);
            foreach (int id in stale) EndPointer(id, pointers[id], true, pointers[id].last);
            // Resolve after all fingers, so a dodge always wins over an auto-move release.
            if (DodgePressed) CancelAutoMove();
            if (AutoMoving) Move = Vector2.up;
        }
        public void CancelAutoMove()
        {
            if (AutoMoving) Move = Vector2.zero;
            AutoMoving = AutoMoveReady = false; outerHoldStarted = -1;
        }
        public void CancelAll()
        {
            CancelAutoMove();
            bool hadFire = FireHeld;
            pointers.Clear(); moveId = lookId = fireId = int.MinValue; Move = LookDelta = Vector2.zero;
            FirePressed = FireReleased = InteractPressed = PausePressed = JournalPressed = false;
            FireCanceled = hadFire;
            DodgePressed = false;
        }
    }

    [DefaultExecutionOrder(-200)]
    public sealed class PrologueTouchControls : MonoBehaviour
    {
        public ProloguePlayer player;
        public PrologueTouchInput InputState { get; } = new PrologueTouchInput();
        public bool Active => Application.isMobilePlatform || (player && player.previewTouchControls);
        Texture2D disc, ring;
        GUIStyle label, hint;
        int width, height;
        Rect safeArea;
        bool wasActive, previewMouseHeld;

        void Awake()
        {
            player = GetComponent<ProloguePlayer>();
            if (Application.isMobilePlatform)
            {
                Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
                Screen.orientation = ScreenOrientation.AutoRotation;
            }
            UpdateLayout();
        }
        void UpdateLayout()
        {
            width = Screen.width; height = Screen.height; safeArea = Screen.safeArea;
            float sx = 1920f / Mathf.Max(1, width), sy = 1080f / Mathf.Max(1, height);
            Rect safe = new Rect(safeArea.x * sx, (height - safeArea.yMax) * sy, safeArea.width * sx, safeArea.height * sy);
            InputState.StickCenter = new Vector2(safe.xMin + 190, safe.yMax - 220);
            InputState.AutoMoveRect = new Rect(safe.xMin + 115, safe.yMax - 455, 150, 70);
            InputState.FireCenter = new Vector2(safe.xMax - 175, safe.yMax - 200);
            InputState.DodgeRect = new Rect(safe.xMax - 420, safe.yMax - 318, 130, 76);
            InputState.InteractRect = new Rect(safe.xMax - 540, safe.yMax - 465, 245, 100);
            InputState.PauseRect = new Rect(safe.xMax - 185, safe.yMin + 22, 155, 68);
            InputState.JournalRect = new Rect(safe.xMax - 365, safe.yMin + 22, 155, 68);
            InputState.LookArea = new Rect(safe.center.x, safe.yMin + 320, safe.width / 2, Mathf.Max(0, safe.height - 320));
        }
        Vector2 ReferencePosition(Vector2 screen) { return new Vector2(screen.x * 1920f / Screen.width, (Screen.height - screen.y) * 1080f / Screen.height); }
        void Update()
        {
            if (player && !Application.isMobilePlatform && UnityEngine.Input.GetKeyDown(KeyCode.F2)) player.previewTouchControls = !player.previewTouchControls;
            if (width != Screen.width || height != Screen.height || safeArea != Screen.safeArea)
            { InputState.CancelAll(); player.archer.CancelDraw(); UpdateLayout(); }
            if (!Active || !player || !player.story)
            {
                if (wasActive) { InputState.CancelAll(); player.archer.CancelDraw(); }
                wasActive = Active; return;
            }
            if (!wasActive) { InputState.CancelAll(); player.archer.CancelDraw(); previewMouseHeld = false; }
            wasActive = true;
            if (!player.story.CanControl) { InputState.CancelAll(); previewMouseHeld = false; return; }
            InputState.BeginFrame(); InputState.ShowInteraction = player.story.CanInteract;
            if (UnityEngine.Input.touchCount > 0)
            {
                previewMouseHeld = false;
                for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                {
                    Touch touch = UnityEngine.Input.GetTouch(i);
                    PrologueTouchInput.Phase phase = touch.phase == TouchPhase.Began ? PrologueTouchInput.Phase.Began : touch.phase == TouchPhase.Ended ? PrologueTouchInput.Phase.Ended
                        : touch.phase == TouchPhase.Canceled ? PrologueTouchInput.Phase.Canceled : touch.phase == TouchPhase.Moved ? PrologueTouchInput.Phase.Moved : PrologueTouchInput.Phase.Stationary;
                    InputState.Process(touch.fingerId, ReferencePosition(touch.position), phase);
                }
            }
            else if (!Application.isMobilePlatform)
            {
                Vector2 position = ReferencePosition(UnityEngine.Input.mousePosition);
                if (UnityEngine.Input.GetMouseButtonDown(0)) { previewMouseHeld = true; InputState.Process(-1, position, PrologueTouchInput.Phase.Began); }
                else if (previewMouseHeld && UnityEngine.Input.GetMouseButtonUp(0)) { InputState.Process(-1, position, PrologueTouchInput.Phase.Ended); previewMouseHeld = false; }
                else if (previewMouseHeld && UnityEngine.Input.GetMouseButton(0)) InputState.Process(-1, position, PrologueTouchInput.Phase.Moved);
            }
            InputState.EndFrame();
            if (InputState.PausePressed) { player.story.TogglePause(); InputState.CancelAll(); player.archer.CancelDraw(); }
            else if (InputState.JournalPressed) { player.story.ToggleJournal(); InputState.CancelAll(); player.archer.CancelDraw(); }
            else if (InputState.InteractPressed) { player.story.Interact(); InputState.CancelAll(); player.archer.CancelDraw(); }
        }
        void OnApplicationFocus(bool focus) { if (!focus) Cancel(); }
        void OnApplicationPause(bool paused) { if (paused) Cancel(); }
        void OnDisable() { Cancel(); }
        void Cancel()
        {
            InputState.CancelAll(); previewMouseHeld = false;
            if (player && player.archer) player.archer.CancelDraw();
        }
        void MakeTextures()
        {
            disc = Circle(false); ring = Circle(true);
        }
        static Texture2D Circle(bool outline)
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float radius = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(64, 64)) / 64;
                float alpha = Mathf.Clamp01((1 - radius) * 64);
                if (outline) alpha *= Mathf.Clamp01((radius - .965f) * 128);
                pixels[y * 128 + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels); texture.Apply(); texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave; return texture;
        }
        void DrawCircle(Vector2 center, float radius, Texture2D texture, Color color)
        {
            GUI.color = color; GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), texture); GUI.color = Color.white;
        }
        void DrawButton(Rect rect, string text)
        {
            GUI.color = new Color(.02f, .055f, .065f, .88f); GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(rect, text, label);
        }
        void OnGUI()
        {
            if (!Active || !player || !player.story || !player.story.CanControl) return;
            if (!disc) MakeTextures();
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { font = player.story.chineseFont, fontSize = 30, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.45f, 1, 1) } };
                hint = new GUIStyle(label) { fontSize = 20, normal = { textColor = new Color(.85f, .95f, .95f, .9f) } };
            }
            Matrix4x4 previous = GUI.matrix; int depth = GUI.depth; GUI.depth = -10;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));
            Vector2 stick = InputState.StickCenter, fire = InputState.FireCenter;
            DrawCircle(stick, 140, disc, new Color(.01f, .035f, .045f, .6f));
            DrawCircle(stick, 140, ring, new Color(.3f, .9f, 1, .9f));
            DrawCircle(stick + new Vector2(InputState.Move.x, -InputState.Move.y) * 85, 54, disc, new Color(.3f, .95f, 1, .8f));
            GUI.Label(new Rect(stick.x - 110, stick.y + 145, 220, 35), "移动", hint);
            if (InputState.AutoMoveReady || InputState.AutoMoving)
                DrawButton(InputState.AutoMoveRect, InputState.AutoMoving ? "取消" : "持续");
            DrawCircle(fire, 105, disc, new Color(.01f, .035f, .045f, .85f));
            DrawCircle(fire, 105, ring, new Color(.25f, 1, 1));
            GUI.Label(new Rect(fire.x - 105, fire.y - 60, 210, 120), InputState.FireHeld ? "松手\n放箭" : "按住\n蓄力", label);
            float cooldown = player.DodgeCooldownRemaining;
            GUI.color = cooldown > 0 ? new Color(.45f, .45f, .45f, .9f) : new Color(.25f, 1, 1);
            GUI.DrawTexture(InputState.DodgeRect, Texture2D.whiteTexture);
            Rect dodgeInner = InputState.DodgeRect;
            dodgeInner.xMin += 2; dodgeInner.xMax -= 2; dodgeInner.yMin += 2; dodgeInner.yMax -= 2;
            DrawButton(dodgeInner, cooldown > 0 ? cooldown.ToString("0.0") + "s" : "闪避");
            GUI.Label(new Rect(1030, 670, 280, 45), "右侧滑动瞄准", hint);
            if (player.story.CanInteract) DrawButton(InputState.InteractRect, player.story.InteractionLabel);
            DrawButton(InputState.JournalRect, "札记"); DrawButton(InputState.PauseRect, "暂停");
            GUI.matrix = previous; GUI.depth = depth;
        }
        void OnDestroy() { if (disc) Destroy(disc); if (ring) Destroy(ring); }
    }
}

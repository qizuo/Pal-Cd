using System;
using UnityEngine;

namespace Houyi.NineSuns
{
    public sealed class PrologueDirector : MonoBehaviour
    {
        public enum ChapterStep { Intro, Village, Trial, Rescue, Altar, Seal, Complete }
        public ChapterStep Step { get; private set; }
        public ProloguePlayer player;
        public Transform elder, survivor, altar;
        public PrologueTarget[] targets, beasts;
        public PrologueTarget seal;
        public GameObject nineSuns;
        public Font chineseFont;
        public Light sunLight;
        public ChapterOneDirector ChapterOne { get; private set; }
        public bool InChapterOne => ChapterOne != null;
        public ChapterTwoDirector ChapterTwo { get; private set; }
        public bool InChapterTwo => ChapterTwo != null;
        public ChapterThreeDirector ChapterThree { get; private set; }
        public bool InChapterThree => ChapterThree != null;
        public bool PrecisionRecoveryUnlocked => InChapterThree && ChapterThree.Finished || HouyiSave.TryRead(3, out var reward) && (reward.precisionRecoveryUnlocked || reward.stage == 6);
        public int Health { get; private set; } = 100;
        public int TrainingHits { get; private set; }
        public int BeastsDefeated { get; private set; }
        public bool CanControl => !PrologueLoadingScreen.IsLoading && Step != ChapterStep.Intro && Step != ChapterStep.Complete && dialogue == null && !paused && !journal && Health > 0 && (!InChapterOne || !ChapterOne.Finished) && (!InChapterTwo || !ChapterTwo.Finished) && (!InChapterThree || !ChapterThree.Finished);
        public bool DialogueOpen => dialogue != null;
        public bool TestButtonPointerActive => (InChapterOne || InChapterTwo || InChapterThree) && CanControl && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
        public bool TouchUIActive => player && player.TouchControls && player.TouchControls.Active;
        public bool CanInteract => CanControl && (InChapterThree ? ChapterThree.CanInteract : InChapterTwo ? ChapterTwo.CanInteract : InChapterOne ? ChapterOne.CanInteract : ((Step == ChapterStep.Village && Near(elder))
            || (Step == ChapterStep.Rescue && BeastsDefeated >= beasts.Length && Near(survivor)) || (Step == ChapterStep.Altar && Near(altar))));
        public string InteractionLabel => InChapterThree ? ChapterThree.InteractionLabel : InChapterTwo ? ChapterTwo.InteractionLabel : InChapterOne ? ChapterOne.InteractionLabel : Step == ChapterStep.Altar ? "触碰祭坛" : "交谈";
        public string Objective => InChapterThree ? ChapterThree.Objective : InChapterTwo ? ChapterTwo.Objective : InChapterOne ? ChapterOne.Objective : objectives[(int)Step];
        static readonly string[] objectives = {
            "序章 · 焦土上的弓声", "循村道寻找村老，与他交谈", "射落三面草靶，重拾弓术", "赶往枯井，击退灾兽并救下村民", "前往山脚祭坛，查明异象", "满蓄力击碎祭坛上方的赤日封印", "序章完成 · 向九日宣战"
        };
        string[] dialogue;
        int line;
        Action afterDialogue;
        bool paused, journal;
        string toast;
        float toastUntil, damageUntil;
        Vector3 checkpoint;
        Material neutralSky;
        bool restoringSave;
        readonly bool[] chapterHasProgress = new bool[4];
        static bool skipNextAutoLoad;
        static int replayChapter = -1;
        Vector3 chapterZeroStart;
        GUIStyle title, heading, body, small, button, marker;
        readonly Color gold = new Color(.90f, .69f, .34f), ivory = new Color(.93f, .88f, .76f);

        void Awake()
        {
            Time.timeScale = 1; Step = ChapterStep.Intro;
            ApplyWhiteLighting();
            if (nineSuns) nineSuns.SetActive(false);
            if (seal) seal.gameObject.SetActive(false);
            checkpoint = player.transform.position;
            chapterZeroStart = checkpoint;
        }
        void Start()
        {
            if (replayChapter >= 0)
            {
                int chapter = replayChapter; replayChapter = -1; skipNextAutoLoad = false;
                if (chapter == 3) StartChapterThree(); else if (chapter == 2) StartChapterTwo(); else if (chapter == 1) StartChapterOne(); else Begin();
                return;
            }
            if (skipNextAutoLoad) { skipNextAutoLoad = false; return; }
            // Startup waits for an explicit chapter choice; existing saves are not auto-entered.
            for (int chapter = 0; chapter < chapterHasProgress.Length; chapter++)
                chapterHasProgress[chapter] = HouyiSave.TryRead(chapter, out _);
        }
        void Update()
        {
            if (PrologueLoadingScreen.IsLoading) return;
            if (Input.GetKeyDown(KeyCode.Escape) && Step != ChapterStep.Intro && Step != ChapterStep.Complete && Health > 0)
                TogglePause();
            if (paused) return;
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleJournal();
            if (Health <= 0) { if (Input.GetKeyDown(KeyCode.R)) Retry(); return; }
            if (Step == ChapterStep.Intro) return;
            if (dialogue != null)
            { if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) AdvanceDialogue(); return; }
            if (Step == ChapterStep.Complete) { if (Input.GetKeyDown(KeyCode.Return)) StartChapterOne(); else if (Input.GetKeyDown(KeyCode.R)) Restart(); return; }
            if (InChapterThree && ChapterThree.Finished) { if (Input.GetKeyDown(KeyCode.R)) RestartCurrentChapter(); return; }
            if (InChapterTwo && ChapterTwo.Finished) { if (Input.GetKeyDown(KeyCode.Return)) StartChapterThree(); else if (Input.GetKeyDown(KeyCode.R)) RestartCurrentChapter(); return; }
            if (InChapterOne && ChapterOne.Finished) { if (Input.GetKeyDown(KeyCode.Return)) StartChapterTwo(); else if (Input.GetKeyDown(KeyCode.R)) RestartCurrentChapter(); return; }
            if (Input.GetKeyDown(KeyCode.E)) Interact();
            if (player.transform.position.y < -5) Retry();
        }
        public void SelectChapter(int chapter)
        {
            if (Step != ChapterStep.Intro || PrologueLoadingScreen.IsLoading) return;
            if (chapter < 0 || chapter > 3) return;
            if (HouyiSave.TryRead(chapter, out HouyiProgress progress))
            {
                if (RestoreProgress(progress)) return;
                Notify("章节进度与当前资源不匹配，已从本章起点开始。", 6);
                if (Step != ChapterStep.Intro) return;
            }
            if (chapter == 0) Begin();
            else if (chapter == 1) StartChapterOne();
            else if (chapter == 2) StartChapterTwo();
            else if (chapter == 3) StartChapterThree();
        }
        public void Begin() { if (Step == ChapterStep.Intro) { SetStep(ChapterStep.Village); Notify("前方的村老似乎知道发生了什么。沿金色路标前进。"); } }
        public void TogglePause()
        {
            if (Step == ChapterStep.Intro || Step == ChapterStep.Complete || Health <= 0) return;
            paused = !paused; Time.timeScale = paused ? 0 : 1;
            player.archer.CancelDraw();
        }
        public void ToggleJournal()
        {
            if (Step == ChapterStep.Intro || Step == ChapterStep.Complete || paused || dialogue != null || Health <= 0) return;
            journal = !journal; player.archer.CancelDraw();
        }
        void SetStep(ChapterStep next)
        {
            Step = next;
            Health = 100; checkpoint = player.transform.position;
            if (next == ChapterStep.Rescue) Notify("枯井传来呼救！灾兽畏惧蓄满的箭。", 7);
            if (next == ChapterStep.Altar) Notify("村民已脱险。祭坛传来不祥的低鸣……", 7);
            if (next == ChapterStep.Seal)
            {
                nineSuns.SetActive(true); seal.gameObject.SetActive(true);
                ApplyWhiteLighting(); sunLight.intensity = 1.8f;
                player.yaw = 0; player.pitch = -12;
                Notify("九日现世！瞄准赤日封印，满蓄力松弦。", 8);
            }
            SaveProgress();
        }
        public bool IsTargetActive(PrologueTarget.Kind kind)
        {
            if (InChapterOne || InChapterTwo || InChapterThree) return false;
            return CanControl && ((kind == PrologueTarget.Kind.Training && Step == ChapterStep.Trial)
                || (kind == PrologueTarget.Kind.Beast && Step == ChapterStep.Rescue)
                || (kind == PrologueTarget.Kind.Seal && Step == ChapterStep.Seal));
        }
        public void TargetDefeated(PrologueTarget target)
        {
            if (target.kind == PrologueTarget.Kind.Training)
            {
                TrainingHits++; Notify("草靶命中  " + TrainingHits + " / 3");
                if (TrainingHits >= targets.Length) SetStep(ChapterStep.Rescue);
            }
            else if (target.kind == PrologueTarget.Kind.Beast)
            {
                BeastsDefeated++; Notify(BeastsDefeated >= beasts.Length ? TouchUIActive ? "灾兽已尽数击退。靠近枯井边的村民，点击「交谈」。" : "灾兽已尽数击退。靠近枯井边的村民，按 E 交谈。" : "灾兽击退  " + BeastsDefeated + " / 3");
            }
            else
            {
                Talk(new[] {
                    "后羿|裂纹……不在石上，而在天上。",
                    "神秘之声|十日失序，九灾将临。执弓之人，你可愿承担逆天的代价？",
                    "后羿|若天不再庇佑苍生，我便亲手，开出一条生路。"
                }, () => SetStep(ChapterStep.Complete));
            }
            SaveProgress();
        }
        public void Interact()
        {
            if (!CanControl) return;
            if (InChapterThree) { ChapterThree.Interact(); return; }
            if (InChapterTwo) { ChapterTwo.Interact(); return; }
            if (InChapterOne) { ChapterOne.Interact(); return; }
            if (Step == ChapterStep.Village && Near(elder))
                Talk(new[] {
                    "村老|羿，你终于回来了。三月无雨，井底也只剩下热沙。今早，山上的祭火竟自己燃了起来。",
                    "后羿|天上的日光，像是要将人间烧穿。我去山上看看。",
                    "村老|先试试你手中的弓吧。射中村口三面草靶，再往枯井去——那里还有没回来的乡亲。"
                }, () => SetStep(ChapterStep.Trial));
            else if (Step == ChapterStep.Rescue && Near(survivor))
            {
                if (BeastsDefeated < beasts.Length) { Notify("先击退周围的灾兽！"); return; }
                Talk(new[] {
                    "受困村民|多谢你……那些东西是从祭坛的红光里爬出来的。",
                    "后羿|回村里去，带大家躲进石屋。不要再靠近那道光。",
                    "受困村民|我看见天上不止一个太阳。羿，千万小心。"
                }, () => SetStep(ChapterStep.Altar));
            }
            else if (Step == ChapterStep.Altar && Near(altar))
                Talk(new[] {
                    "后羿|这不是祈雨的祭坛……有人封住了这里。",
                    "古老铭文|扶桑之火，分而为十。九日不归，山海同焚。",
                    "后羿|封印在吞噬地脉。既然找到了祸根，就先斩断它！"
                }, () => SetStep(ChapterStep.Seal));
        }
        bool Near(Transform point) { return point && Vector3.Distance(player.transform.position, point.position) < 3.4f; }
        public Transform Goal
        {
            get
            {
                if (InChapterThree) return ChapterThree.Goal;
                if (InChapterTwo) return ChapterTwo.Goal;
                if (InChapterOne) return ChapterOne.Goal;
                if (Step == ChapterStep.Village) return elder;
                if (Step == ChapterStep.Trial) { foreach (var target in targets) if (!target.defeated) return target.transform; }
                if (Step == ChapterStep.Rescue)
                { foreach (var beast in beasts) if (!beast.defeated) return beast.transform; return survivor; }
                if (Step == ChapterStep.Altar) return altar;
                if (Step == ChapterStep.Seal) return seal.transform;
                return null;
            }
        }
        public void Talk(string[] lines, Action onFinish) { dialogue = lines; line = 0; afterDialogue = onFinish; player.archer.CancelDraw(); }
        public void AdvanceDialogue()
        {
            if (dialogue == null) return;
            if (++line >= dialogue.Length)
            { dialogue = null; var done = afterDialogue; afterDialogue = null; done?.Invoke(); SaveProgress(); }
        }
        public void Notify(string message, float seconds = 4) { toast = message; toastUntil = Time.unscaledTime + seconds; }
        public void Hurt(int damage)
        {
            if (!CanControl || damage <= 0) return;
            Health = Mathf.Max(0, Health - damage); damageUntil = Time.unscaledTime + .3f;
            player.archer.InterruptByHit();
        }
        public void Retry()
        {
            player.RestoreStamina();
            Health = 100; paused = false; Time.timeScale = 1;
            if (InChapterThree) { ChapterThree.Retry(); return; }
            if (InChapterTwo) { ChapterTwo.Retry(); return; }
            if (InChapterOne) { ChapterOne.Retry(); return; }
            player.Teleport(checkpoint); player.archer.CancelDraw();
            Notify("重整弓弦，再试一次。已完成的目标保留。");
        }
        public void Restart()
        {
            HouyiSave.Delete(); skipNextAutoLoad = true;
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        }
        public void ReturnToChapterSelection()
        {
            SaveProgress();
            replayChapter = -1;
            skipNextAutoLoad = false;
            player.archer.CancelDraw();
            if (player.Sunburst) player.Sunburst.CancelSelection();
            if (player.TouchControls) player.TouchControls.InputState.CancelAll();
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        }
        public void RestartCurrentChapter()
        {
            int chapter = InChapterThree ? 3 : InChapterTwo ? 2 : InChapterOne ? 1 : 0;
            var beginning = new HouyiProgress { chapter = chapter, stage = chapter > 0 ? 0 : (int)ChapterStep.Village,
                checkpoint = chapter == 3 ? ChapterThree.Point(0, 0) : chapter == 2 ? ChapterTwo.Point(0, 0) : chapter == 1 ? ChapterOne.Point(0, 0) : chapterZeroStart,
                yaw = 0, pitch = 8, targets = new bool[targets.Length], beasts = new bool[beasts.Length],
                actors = chapter == 3 ? new bool[ChapterThree.Actors.Count] : chapter == 2 ? new bool[ChapterTwo.Actors.Count] : chapter == 1 ? new bool[ChapterOne.Actors.Count] : null };
            if (!HouyiSave.Write(beginning)) { Notify("无法保存重玩进度，请检查设备存储空间。", 6); return; }
            replayChapter = chapter;
            player.archer.CancelDraw();
            if (player.TouchControls) player.TouchControls.InputState.CancelAll();
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        }
        public void StartChapterOne()
        {
            if (InChapterOne) return;
            paused = journal = false; dialogue = null; afterDialogue = null; Time.timeScale = 1;
            Step = ChapterStep.Village; Health = 100;
            player.archer.CancelDraw();
            if (player.TouchControls) player.TouchControls.InputState.CancelAll();
            ChapterOne = gameObject.AddComponent<ChapterOneDirector>();
            ChapterOne.Initialize(this);
        }
        public void StartChapterTwo()
        {
            if (InChapterTwo) return;
            paused = journal = false; dialogue = null; afterDialogue = null; Time.timeScale = 1;
            Step = ChapterStep.Village; Health = 100;
            player.archer.CancelDraw();
            if (player.Sunburst) player.Sunburst.CancelSelection();
            if (player.TouchControls) player.TouchControls.InputState.CancelAll();
            if (ChapterOne) { ChapterOne.enabled = false; ChapterOne.WorldRoot.gameObject.SetActive(false); Destroy(ChapterOne); ChapterOne = null; }
            ChapterTwo = gameObject.AddComponent<ChapterTwoDirector>();
            ChapterTwo.Initialize(this);
        }
        public void StartChapterThree()
        {
            if (InChapterThree) return;
            paused = journal = false; dialogue = null; afterDialogue = null; Time.timeScale = 1;
            Step = ChapterStep.Village; Health = 100;
            player.archer.CancelDraw();
            if (player.Sunburst) player.Sunburst.CancelSelection();
            if (player.TouchControls) player.TouchControls.InputState.CancelAll();
            if (ChapterOne) { ChapterOne.enabled = false; ChapterOne.WorldRoot.gameObject.SetActive(false); Destroy(ChapterOne); ChapterOne = null; }
            if (ChapterTwo) { ChapterTwo.enabled = false; ChapterTwo.WorldRoot.gameObject.SetActive(false); Destroy(ChapterTwo); ChapterTwo = null; }
            ChapterThree = gameObject.AddComponent<ChapterThreeDirector>();
            ChapterThree.Initialize(this);
        }
        public void ChapterCheckpoint(Vector3 position)
        {
            player.RestoreStamina();
            Health = 100; checkpoint = position;
            if (player.TouchControls) player.TouchControls.InputState.CancelAll();
            player.archer.CancelDraw();
        }
        public void SaveProgress()
        {
            if (restoringSave || dialogue != null || Step == ChapterStep.Intro) return;
            HouyiProgress progress;
            if (InChapterThree) progress = ChapterThree.CaptureProgress();
            else if (InChapterTwo) progress = ChapterTwo.CaptureProgress();
            else if (InChapterOne) progress = ChapterOne.CaptureProgress();
            else
            {
                progress = new HouyiProgress { chapter = 0, stage = (int)Step, checkpoint = checkpoint,
                    targets = Array.ConvertAll(targets, t => t.defeated), beasts = Array.ConvertAll(beasts, b => b.defeated) };
            }
            progress.yaw = player.yaw; progress.pitch = player.pitch;
            progress.sunburstCooldown = player.Sunburst ? player.Sunburst.CooldownRemaining : 0;
            if (!HouyiSave.Write(progress)) Notify("自动存档失败，请检查设备存储空间。", 6);
        }
        public bool RestoreProgress(HouyiProgress progress)
        {
            if (progress == null || progress.version != 1 || InChapterOne || InChapterTwo || InChapterThree) return false;
            if (progress.chapter == 0 && (progress.stage < 1 || progress.stage > 6 || progress.targets == null || progress.beasts == null
                || progress.targets.Length != targets.Length || progress.beasts.Length != beasts.Length)) return false;
            if (progress.chapter == 1 && (progress.stage < 0 || progress.stage > 8 || progress.actors == null)) return false;
            if (progress.chapter >= 2 && (progress.stage < 0 || progress.stage > 6 || progress.actors == null)) return false;
            if (progress.chapter < 0 || progress.chapter > 3) return false;
            restoringSave = true;
            try
            {
                paused = journal = false; dialogue = null; afterDialogue = null; Time.timeScale = 1;
                if (progress.chapter == 3)
                {
                    StartChapterThree();
                    if (!ChapterThree.RestoreProgress(progress)) { Notify("存档与第三章不匹配，已返回第三章起点。", 6); return false; }
                }
                else if (progress.chapter == 2)
                {
                    StartChapterTwo();
                    if (!ChapterTwo.RestoreProgress(progress)) { Notify("存档与第二章不匹配，已返回第二章起点。", 6); return false; }
                }
                else if (progress.chapter == 1)
                {
                    StartChapterOne();
                    if (!ChapterOne.RestoreProgress(progress)) { Notify("存档与当前章节不匹配，已返回第一章起点。", 6); return false; }
                }
                else
                {
                    SetStep((ChapterStep)progress.stage);
                    TrainingHits = BeastsDefeated = 0;
                    for (int i = 0; i < targets.Length; i++) if (progress.targets[i]) { targets[i].RestoreDefeated(); TrainingHits++; }
                    for (int i = 0; i < beasts.Length; i++) if (progress.beasts[i]) { beasts[i].RestoreDefeated(); BeastsDefeated++; }
                    nineSuns.SetActive(Step >= ChapterStep.Seal); seal.gameObject.SetActive(Step == ChapterStep.Seal);
                }
                checkpoint = progress.chapter == 2 && ChapterTwoDirector.BossPreview ? ChapterTwo.Point(0,150) : progress.checkpoint; Health = 100;
                player.yaw = progress.yaw; player.pitch = Mathf.Clamp(progress.pitch, -22, 48);
                player.Teleport(checkpoint); player.archer.CancelDraw();
                if (player.Sunburst) player.Sunburst.RestoreCooldown(progress.sunburstCooldown);
                if (player.TouchControls) player.TouchControls.InputState.CancelAll();
                Notify("已继续上次进度 · " + Objective, 6);
                return true;
            }
            finally { restoringSave = false; }
        }
        public void ApplyWhiteLighting()
        {
            foreach (var light in FindObjectsOfType<Light>(true))
            { light.color = Color.white; light.useColorTemperature = false; }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.47f, .47f, .47f);
            RenderSettings.fogColor = new Color(.65f, .65f, .65f);
            if (!neutralSky && RenderSettings.skybox) neutralSky = new Material(RenderSettings.skybox);
            if (neutralSky)
            {
                if (neutralSky.HasProperty("_Horizon")) neutralSky.SetColor("_Horizon", new Color(.68f, .68f, .68f));
                if (neutralSky.HasProperty("_Zenith")) neutralSky.SetColor("_Zenith", new Color(.26f, .26f, .26f));
                RenderSettings.skybox = neutralSky;
            }
        }
        void OnApplicationPause(bool applicationPaused) { if (applicationPaused) SaveProgress(); }
        void OnApplicationQuit() { SaveProgress(); }
        void OnDestroy() { Time.timeScale = 1; if (neutralSky) Destroy(neutralSky); }

        GUIStyle Style(int size, Color color, bool bold = false)
        {
            return new GUIStyle(GUI.skin.label) { font = chineseFont, fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = color }, wordWrap = true, richText = true };
        }
        void InitStyles()
        {
            if (title != null) return;
            title = Style(66, ivory); heading = Style(28, gold); body = Style(24, ivory); small = Style(18, new Color(.74f, .73f, .65f));
            marker = Style(19, gold); marker.alignment = TextAnchor.MiddleCenter;
            button = new GUIStyle(GUI.skin.button) { font = chineseFont, fontSize = 24, fixedHeight = 56 };
            button.normal.textColor = gold;
        }
        void Panel(Rect r, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        void Text(float x, float y, float w, float h, string text, GUIStyle style) { GUI.Label(new Rect(x, y, w, h), text, style); }
        void Rule(float x, float y, float w) { Panel(new Rect(x, y, w, 2), gold); }
        void OnGUI()
        {
            if (PrologueLoadingScreen.IsLoading) return;
            InitStyles(); GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));
            if (Step == ChapterStep.Intro)
            {
                Panel(new Rect(0, 0, 1920, 1080), new Color(.025f, .03f, .035f, 1));
                Text(180, 110, 1300, 100, "后羿：九日之劫", title);
                Text(185, 240, 1200, 55, "选择章节", heading); Rule(185, 315, 1550);
                string[] names = { "序章  ·  焦土上的弓声", "第 1 章  ·  追火入山", "第 2 章  ·  逐火入山", "第 3 章  ·  扶桑遗址" };
                string[] descriptions = { "返回枯桑村，学习射箭，追查祭坛异火。", "沿山道寻找运水队，救人破核，迎战守山兽。", "深入燃林，射破水囊灭火，挑战赤焰山魈。", "射索开桥，绕后破日晷，迎战扶桑炎卫。" };
                for (int chapter = 0; chapter < names.Length; chapter++)
                {
                    float y = 345 + chapter * 150;
                    if (GUI.Button(new Rect(185, y, 1550, 88), names[chapter] + "    " + (chapterHasProgress[chapter] ? "继续旅程  →" : "开始旅程  →"), button)) SelectChapter(chapter);
                    Text(210, y + 94, 1500, 40, descriptions[chapter], small);
                }
                Text(185, 975, 1500, 40, "点击章节继续已保存的进度；没有进度的章节从头开始", small);
                return;
            }
            Panel(new Rect(0, 0, 1920, 102), new Color(.025f, .035f, .035f, .86f));
            Text(45, 20, 500, 44, "后羿：九日之劫", heading);
            Text(47, 66, 500, 28, InChapterThree ? "第 3 章  /  扶桑遗址" : InChapterTwo ? "第 2 章  /  逐火入山" : InChapterOne ? "第 1 章  /  追火入山" : "序章  /  焦土上的弓声", small);
            if ((InChapterOne || InChapterTwo || InChapterThree) && CanControl)
            {
                if (GUI.Button(new Rect(280, 22, 300, 55), "测试：直达 Boss", button))
                {
                    if (InChapterThree) ChapterThree.EnterBossPreview();
                    else if (InChapterTwo) ChapterTwo.EnterBossPreview();
                    else ChapterOne.EnterBossPreview();
                }
                if (!TouchUIActive) Text(605, 32, 400, 35, "按住 Alt 显示鼠标并点击", small);
            }
            if (!TouchUIActive) Text(1390, 30, 480, 45, "十日失序 · 灾厄初现", heading);
            Panel(new Rect(1450, 140, 425, 170), new Color(.025f, .035f, .035f, .84f));
            Rule(1472, 159, 50); Text(1472, 176, 380, 85, Objective, body);
            string progress = InChapterThree ? ChapterThree.Progress : InChapterTwo ? ChapterTwo.Progress : InChapterOne ? ChapterOne.Progress : Step == ChapterStep.Trial ? "草靶  " + TrainingHits + " / 3" : Step == ChapterStep.Rescue ? "灾兽  " + BeastsDefeated + " / 3" : TouchUIActive ? "点击「札记」查看目标" : "TAB 查看行旅札记";
            Text(1472, 267, 380, 32, progress, small);
            // Reticle and a visible charge bar communicate when a shot is ready.
            if (CanControl)
            {
                DrawReticle();
                if (player.archer.CurrentPhase != Wuxia.Archery.ArcherDemo.Phase.Idle)
                {
                    Panel(new Rect(870, 580, 180, 7), new Color(0, 0, 0, .7f));
                    Panel(new Rect(870, 580, 180 * player.archer.Charge, 7), gold);
                    Text(870, 598, 240, 30, player.archer.Charge >= .98f ? "满弦 · 松开发箭" : "凝息蓄力", small);
                }
                DrawGoal();
            }
            float healthY = TouchUIActive ? 110 : 930;
            Panel(new Rect(40, healthY, 360, 112), new Color(.025f, .035f, .035f, .82f));
            Text(58, healthY + 9, 315, 30, "气血   " + Health + " / 100", small);
            Panel(new Rect(58, healthY + 52, 316, 5), new Color(.16f, .17f, .15f));
            Panel(new Rect(58, healthY + 52, 316 * Health / 100f, 5), new Color(.64f, .29f, .18f));
            Text(58, healthY + 64, 315, 28, "气力   " + Mathf.CeilToInt(player.Stamina) + " / 100", small);
            Panel(new Rect(58, healthY + 99, 316, 5), new Color(.16f, .17f, .15f));
            Panel(new Rect(58, healthY + 99, 316 * player.Stamina / ProloguePlayer.MaxStamina, 5), Color.white);
            Text(490, TouchUIActive ? 1030 : 995, 1250, 35, TouchUIActive ? "左摇杆移动   ·   右侧滑动瞄准   ·   按住射击蓄力，松手放箭" : "WASD 移动  ·  SHIFT 短按闪避 / 长按疾跑  ·  鼠标转视角  ·  左键蓄力 / 松手放箭  ·  E 交互  ·  ESC 暂停", small);
            if (Time.unscaledTime < toastUntil && dialogue == null)
            { Panel(new Rect(490, 850, 940, 70), new Color(.025f, .035f, .035f, .88f)); Text(520, 868, 880, 50, toast, body); }
            if (CanInteract && !TouchUIActive)
                Text(730, 765, 600, 55, "[ E ]   " + InteractionLabel, heading);
            if (InChapterThree) ChapterThree.DrawHUD();
            else if (InChapterTwo) ChapterTwo.DrawHUD();
            else if (InChapterOne) ChapterOne.DrawHUD();
            if (dialogue != null) DrawDialogue();
            if (journal && dialogue == null) DrawJournal();
            if (Time.unscaledTime < damageUntil) Panel(new Rect(0, 0, 1920, 1080), new Color(.7f, .05f, .02f, .17f));
            if (paused || Health <= 0 || Step == ChapterStep.Complete || (InChapterOne && ChapterOne.Finished) || (InChapterTwo && ChapterTwo.Finished) || (InChapterThree && ChapterThree.Finished)) DrawModal();
        }
        void DrawReticle()
        {
            // Keep the center unobstructed while the dark outline survives bright sand and sunlight.
            ReticleMark(new Rect(940, 538, 12, 4));
            ReticleMark(new Rect(968, 538, 12, 4));
            ReticleMark(new Rect(958, 520, 4, 12));
            ReticleMark(new Rect(958, 548, 4, 12));
            ReticleMark(new Rect(958, 538, 4, 4));
        }
        void ReticleMark(Rect rect)
        {
            Panel(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), Color.black);
            Panel(rect, new Color(0.1f, 1f, 1f, 1f));
        }
        void DrawGoal()
        {
            Transform goal = Goal; if (!goal || !player.view) return;
            Vector3 p = player.view.WorldToViewportPoint(goal.position + Vector3.up * 2.3f);
            float distance = Vector3.Distance(player.transform.position, goal.position);
            float x = p.z > 0 ? Mathf.Clamp(p.x * 1920, 140, 1780) : 960;
            float y = p.z > 0 ? Mathf.Clamp((1 - p.y) * 1080, 150, 750) : 180;
            string label = InChapterThree ? ChapterThree.GoalLabel : InChapterTwo ? ChapterTwo.GoalLabel : InChapterOne ? ChapterOne.GoalLabel : Step == ChapterStep.Village ? "村老" : Step == ChapterStep.Trial ? "草靶" : Step == ChapterStep.Altar ? "山脚祭坛" : Step == ChapterStep.Seal ? "赤日封印" : BeastsDefeated >= beasts.Length ? "受困村民" : "灾兽";
            GUI.Label(new Rect(x - 120, y - 30, 240, 65), "◆  " + label + "\n" + Mathf.RoundToInt(distance) + " 米", marker);
        }
        void DrawDialogue()
        {
            Panel(new Rect(0, 775, 1920, 305), new Color(.025f, .035f, .035f, .97f)); Rule(90, 800, 65);
            string[] parts = dialogue[line].Split('|');
            Text(90, 825, 350, 45, parts[0], heading); Text(360, 825, 1430, 145, parts.Length > 1 ? parts[1] : parts[0], body);
            if (TouchUIActive)
            { if (GUI.Button(new Rect(1510, 970, 320, 70), "继续  →   " + (line + 1) + " / " + dialogue.Length, button)) AdvanceDialogue(); }
            else Text(1440, 1005, 420, 35, "E / 空格  继续   " + (line + 1) + " / " + dialogue.Length, small);
        }
        void DrawJournal()
        {
            Panel(new Rect(450, 230, 1020, 500), new Color(.025f, .035f, .035f, .98f));
            Text(505, 275, 900, 55, InChapterThree ? "行旅札记 · 扶桑遗址" : InChapterTwo ? "行旅札记 · 逐火入山" : InChapterOne ? "行旅札记 · 追火入山" : "行旅札记 · 日光不再温柔", heading); Rule(505, 345, 900);
            Text(505, 380, 900, 180, (InChapterThree ? "日晷引火，赤链缚灵。射断悬索、绕到日晷背面破核，解开遗址封印。炎卫聚能时可射胸核打断，或借石柱躲避冲击。" : InChapterTwo ? "循碎片追入燃林，射破水囊救援队员，清除扶桑火核。山顶的赤焰山魈似乎也在逃避天火。" : InChapterOne ? "运水队在焦林失踪，山泉被赤日之火侵蚀。沿水桶与断箭追踪，救下乡亲，查明山中的祸根。" : "旱灾持续三月，村中水源将尽。祭坛异火、井边灾兽与天上的重影，似乎来自同一种力量。") + "\n\n当前目标：" + Objective, body);
            Text(505, 630, 900, 40, TouchUIActive ? "射箭要诀：按住射击按钮蓄力，拖动瞄准，松手放箭。" : "射箭要诀：移动鼠标调整准星，按住左键蓄力，松开命中目标。", small);
            if (TouchUIActive) { if (GUI.Button(new Rect(1130, 650, 280, 65), "收起札记", button)) ToggleJournal(); }
            else Text(505, 675, 900, 35, "TAB 收起", small);
        }
        void DrawModal()
        {
            Panel(new Rect(0, 0, 1920, 1080), new Color(.015f, .025f, .025f, .88f));
            bool thirdComplete = InChapterThree && ChapterThree.Finished;
            bool secondComplete = InChapterTwo && ChapterTwo.Finished;
            bool chapterComplete = thirdComplete || secondComplete || (InChapterOne && ChapterOne.Finished);
            bool complete = Step == ChapterStep.Complete || chapterComplete;
            Text(480, 280, 1100, 105, complete ? thirdComplete ? "第 3 章完 · 扶桑遗址" : secondComplete ? "第 2 章完 · 天火追兽" : chapterComplete ? "第 1 章完 · 山泉复流" : "序章终 · 九日之劫" : Health <= 0 ? "弓弦未断，尚可再战" : "暂歇片刻", title);
            Rule(490, 410, 100);
            Text(490, 460, 950, 190, complete ? thirdComplete ? "赤日锁链断裂，扶桑炎卫恢复神智。\n兽群被天火驱使的真相浮出水面。\n\n解锁：精准回气 · 满蓄力命中弱点恢复 10 点气力\n下一程 · 扶桑天阶（尚未开放）" : secondComplete ? "赤焰山魈恢复神智，道出天火追兽的真相。\n碎片映出云层后的第二轮太阳。\n\n下一程 · 扶桑遗址" : chapterComplete ? "守山兽倒下，清泉再度流向枯桑村。\n水底的赤日碎片证明：灾兽并非祸根，只是被侵蚀的生灵。\n\n远山又升起赤光。第 2 章 · 逐火入山" : "一枚封印碎裂，九轮赤日在天际苏醒。\n为了这片焦土上的生灵，后羿踏上了追日之路。\n\n第 1 章 · 追火入山" : Health <= 0 ? "你被灾兽击倒了。\n从当前任务检查点恢复，已完成的目标保留。" : "风仍在吹，乡亲仍等待着你。", body);
            if (chapterComplete && !secondComplete && !thirdComplete && GUI.Button(new Rect(950, 725, 430, 60), "进入第 2 章    →", button)) StartChapterTwo();
            if (secondComplete && GUI.Button(new Rect(950, 725, 430, 60), "进入第 3 章    →", button)) StartChapterThree();
            if (complete && !chapterComplete && GUI.Button(new Rect(950, 725, 430, 60), "进入第 1 章    →", button)) StartChapterOne();
            if (GUI.Button(new Rect(490, 725, 400, 60), complete ? TouchUIActive ? "再行一次" : "再行一次    [ R ]" : Health <= 0 ? TouchUIActive ? "重整弓弦" : "重整弓弦    [ R ]" : TouchUIActive ? "继续旅程" : "继续旅程    [ ESC ]", button))
            { if (complete) RestartCurrentChapter(); else if (Health <= 0) Retry(); else { paused = false; Time.timeScale = 1; } }
            if (paused && !complete && Health > 0 && GUI.Button(new Rect(950, 725, 400, 60), "重玩本章", button)) RestartCurrentChapter();
            if (GUI.Button(new Rect(490, 810, 400, 60), "返回章节选择", button)) ReturnToChapterSelection();
        }
    }
}

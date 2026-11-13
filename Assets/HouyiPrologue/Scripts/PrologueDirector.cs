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
        public int Health { get; private set; } = 100;
        public int TrainingHits { get; private set; }
        public int BeastsDefeated { get; private set; }
        public bool CanControl => Step != ChapterStep.Intro && Step != ChapterStep.Complete && dialogue == null && !paused && !journal && Health > 0 && (!InChapterOne || !ChapterOne.Finished);
        public bool DialogueOpen => dialogue != null;
        public bool TouchUIActive => player && player.TouchControls && player.TouchControls.Active;
        public bool CanInteract => CanControl && (InChapterOne ? ChapterOne.CanInteract : ((Step == ChapterStep.Village && Near(elder))
            || (Step == ChapterStep.Rescue && BeastsDefeated >= beasts.Length && Near(survivor)) || (Step == ChapterStep.Altar && Near(altar))));
        public string InteractionLabel => InChapterOne ? ChapterOne.InteractionLabel : Step == ChapterStep.Altar ? "触碰祭坛" : "交谈";
        public string Objective => InChapterOne ? ChapterOne.Objective : objectives[(int)Step];
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
        GUIStyle title, heading, body, small, button, marker;
        readonly Color gold = new Color(.90f, .69f, .34f), ivory = new Color(.93f, .88f, .76f);

        void Awake()
        {
            Time.timeScale = 1; Step = ChapterStep.Intro;
            if (nineSuns) nineSuns.SetActive(false);
            if (seal) seal.gameObject.SetActive(false);
            checkpoint = player.transform.position;
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && Step != ChapterStep.Intro && Step != ChapterStep.Complete && Health > 0)
                TogglePause();
            if (paused) return;
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleJournal();
            if (Health <= 0) { if (Input.GetKeyDown(KeyCode.R)) Retry(); return; }
            if (Step == ChapterStep.Intro && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))) { Begin(); return; }
            if (dialogue != null)
            { if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) AdvanceDialogue(); return; }
            if (Step == ChapterStep.Complete) { if (Input.GetKeyDown(KeyCode.Return)) StartChapterOne(); else if (Input.GetKeyDown(KeyCode.R)) Restart(); return; }
            if (InChapterOne && ChapterOne.Finished) { if (Input.GetKeyDown(KeyCode.R)) Restart(); return; }
            if (Input.GetKeyDown(KeyCode.E)) Interact();
            if (player.transform.position.y < -5) Retry();
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
                sunLight.color = new Color(1, .36f, .12f); sunLight.intensity = 1.8f;
                player.yaw = 0; player.pitch = -12;
                Notify("九日现世！瞄准赤日封印，满蓄力松弦。", 8);
            }
        }
        public bool IsTargetActive(PrologueTarget.Kind kind)
        {
            if (InChapterOne) return false;
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
        }
        public void Interact()
        {
            if (!CanControl) return;
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
            { dialogue = null; var done = afterDialogue; afterDialogue = null; done?.Invoke(); }
        }
        public void Notify(string message, float seconds = 4) { toast = message; toastUntil = Time.unscaledTime + seconds; }
        public void Hurt(int damage)
        {
            if (!CanControl) return;
            Health = Mathf.Max(0, Health - damage); damageUntil = Time.unscaledTime + .3f;
            if (Health == 0) player.archer.CancelDraw();
        }
        public void Retry()
        {
            Health = 100; paused = false; Time.timeScale = 1;
            if (InChapterOne) { ChapterOne.Retry(); return; }
            player.Teleport(checkpoint); player.archer.CancelDraw();
            Notify("重整弓弦，再试一次。已完成的目标保留。");
        }
        public void Restart()
        {
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
        public void ChapterCheckpoint(Vector3 position)
        {
            Health = 100; checkpoint = position;
            if (player.TouchControls) player.TouchControls.InputState.CancelAll();
            player.archer.CancelDraw();
        }
        void OnDestroy() { Time.timeScale = 1; }

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
            InitStyles(); GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));
            if (Step == ChapterStep.Intro)
            {
                Panel(new Rect(0, 0, 1920, 1080), new Color(.025f, .025f, .025f, .52f));
                Panel(new Rect(0, 0, 900, 1080), new Color(.025f, .03f, .028f, .78f));
                Text(110, 170, 700, 40, "山 海 旧 卷   /   序 章", heading); Rule(110, 235, 80);
                Text(104, 280, 730, 105, "后羿：九日之劫", title);
                Text(110, 405, 650, 55, "焦土上的弓声", heading);
                Text(110, 490, 590, 180, "十日之下，万物失色。\n当祈祷再也换不来一滴雨，\n一个猎人，拾起了指向苍天的弓。", body);
                if (GUI.Button(new Rect(110, 740, 310, 60), "启程    →", button)) Begin();
                if (GUI.Button(new Rect(110, 820, 460, 60), "直接试玩第 1 章 · 追火入山", button)) StartChapterOne();
                Text(110, 910, 650, 40, TouchUIActive ? "点击启程   ·   横屏触控游玩" : "ENTER 开始序章   ·   F2 触控预览", small);
                return;
            }
            Panel(new Rect(0, 0, 1920, 102), new Color(.025f, .035f, .035f, .86f));
            Text(45, 20, 500, 44, "后羿：九日之劫", heading);
            Text(47, 66, 500, 28, InChapterOne ? "第 1 章  /  追火入山" : "序章  /  焦土上的弓声", small);
            if (!TouchUIActive) Text(1390, 30, 480, 45, "十日失序 · 灾厄初现", heading);
            Panel(new Rect(1450, 140, 425, 170), new Color(.025f, .035f, .035f, .84f));
            Rule(1472, 159, 50); Text(1472, 176, 380, 85, Objective, body);
            string progress = InChapterOne ? ChapterOne.Progress : Step == ChapterStep.Trial ? "草靶  " + TrainingHits + " / 3" : Step == ChapterStep.Rescue ? "灾兽  " + BeastsDefeated + " / 3" : TouchUIActive ? "点击「札记」查看目标" : "TAB 查看行旅札记";
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
            float healthY = TouchUIActive ? 140 : 958;
            Panel(new Rect(40, healthY, 360, 74), new Color(.025f, .035f, .035f, .82f));
            Text(58, healthY + 9, 315, 30, "气血   " + Health + " / 100", small);
            Panel(new Rect(58, healthY + 52, 316, 5), new Color(.16f, .17f, .15f));
            Panel(new Rect(58, healthY + 52, 316 * Health / 100f, 5), new Color(.64f, .29f, .18f));
            Text(490, TouchUIActive ? 1030 : 995, 1250, 35, TouchUIActive ? "左摇杆移动   ·   右侧滑动瞄准   ·   按住射击蓄力，松手放箭" : "WASD 移动  ·  SHIFT 疾行  ·  按住右键转动视角  ·  左键蓄力 / 松开放箭  ·  E 交互  ·  ESC 暂停", small);
            if (Time.unscaledTime < toastUntil && dialogue == null)
            { Panel(new Rect(490, 850, 940, 70), new Color(.025f, .035f, .035f, .88f)); Text(520, 868, 880, 50, toast, body); }
            if (CanInteract && !TouchUIActive)
                Text(730, 765, 600, 55, "[ E ]   " + InteractionLabel, heading);
            if (InChapterOne) ChapterOne.DrawHUD();
            if (dialogue != null) DrawDialogue();
            if (journal && dialogue == null) DrawJournal();
            if (Time.unscaledTime < damageUntil) Panel(new Rect(0, 0, 1920, 1080), new Color(.7f, .05f, .02f, .17f));
            if (paused || Health <= 0 || Step == ChapterStep.Complete || (InChapterOne && ChapterOne.Finished)) DrawModal();
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
            string label = InChapterOne ? ChapterOne.GoalLabel : Step == ChapterStep.Village ? "村老" : Step == ChapterStep.Trial ? "草靶" : Step == ChapterStep.Altar ? "山脚祭坛" : Step == ChapterStep.Seal ? "赤日封印" : BeastsDefeated >= beasts.Length ? "受困村民" : "灾兽";
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
            Text(505, 275, 900, 55, InChapterOne ? "行旅札记 · 追火入山" : "行旅札记 · 日光不再温柔", heading); Rule(505, 345, 900);
            Text(505, 380, 900, 180, (InChapterOne ? "运水队在焦林失踪，山泉被赤日之火侵蚀。沿水桶与断箭追踪，救下乡亲，查明山中的祸根。" : "旱灾持续三月，村中水源将尽。祭坛异火、井边灾兽与天上的重影，似乎来自同一种力量。") + "\n\n当前目标：" + Objective, body);
            Text(505, 630, 900, 40, TouchUIActive ? "射箭要诀：按住射击按钮蓄力，拖动瞄准，松手放箭。" : "射箭要诀：右键调整准星，按住左键蓄力，松开命中目标。", small);
            if (TouchUIActive) { if (GUI.Button(new Rect(1130, 650, 280, 65), "收起札记", button)) ToggleJournal(); }
            else Text(505, 675, 900, 35, "TAB 收起", small);
        }
        void DrawModal()
        {
            Panel(new Rect(0, 0, 1920, 1080), new Color(.015f, .025f, .025f, .88f));
            bool chapterComplete = InChapterOne && ChapterOne.Finished;
            bool complete = Step == ChapterStep.Complete || chapterComplete;
            Text(480, 280, 1100, 105, complete ? chapterComplete ? "第 1 章完 · 山泉复流" : "序章终 · 九日之劫" : Health <= 0 ? "弓弦未断，尚可再战" : "暂歇片刻", title);
            Rule(490, 410, 100);
            Text(490, 460, 950, 190, complete ? chapterComplete ? "守山兽倒下，清泉再度流向枯桑村。\n水底的赤日碎片证明：灾兽并非祸根，只是被侵蚀的生灵。\n\n远山又升起赤光。下一章 · 扶桑余烬（待续）" : "一枚封印碎裂，九轮赤日在天际苏醒。\n为了这片焦土上的生灵，后羿踏上了追日之路。\n\n第 1 章 · 追火入山" : Health <= 0 ? "你被灾兽击倒了。\n从当前任务检查点恢复，已完成的目标保留。" : "风仍在吹，乡亲仍等待着你。", body);
            if (complete && !chapterComplete && GUI.Button(new Rect(950, 725, 430, 60), "进入第 1 章    →", button)) StartChapterOne();
            if (GUI.Button(new Rect(490, 725, 400, 60), complete ? TouchUIActive ? "再行一次" : "再行一次    [ R ]" : Health <= 0 ? TouchUIActive ? "重整弓弦" : "重整弓弦    [ R ]" : TouchUIActive ? "继续旅程" : "继续旅程    [ ESC ]", button))
            { if (complete) Restart(); else if (Health <= 0) Retry(); else { paused = false; Time.timeScale = 1; } }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Houyi.NineSuns
{
    // Chapter one shares the prologue's player, dialogue, health and touch UI.
    public sealed class ChapterOneDirector : MonoBehaviour
    {
        public enum Stage { Village, Trail, Ambush, Rescue, Gate, Approach, Boss, Spring, Complete }
        public Stage Current { get; private set; }
        public bool Finished => Current == Stage.Complete;
        public PrologueDirector Story { get; private set; }
        public readonly List<ChapterOneActor> Actors = new List<ChapterOneActor>();
        public readonly List<Transform> Clues = new List<Transform>();
        public Transform Elder, Captive, Shrine, Spring, Lore, Supply;
        public GameObject Cage, Gate, Water, Corruption;
        public ChapterOneActor Boss, WeakPoint;
        public Transform WorldRoot { get; private set; }
        public int CluesFound { get; private set; }
        public bool RopeCut { get; private set; }
        public string BossWarning { get; set; }
        bool supplyTaken, loreRead;
        Vector3 checkpoint;
        GUIStyle text, bossText;
        readonly List<FireStone> stones = new List<FireStone>();
        sealed class FireStone { public GameObject visual; public Vector3 start, end; public float age; public GameObject warning; }
        static readonly string[] objectives = {
            "在山口与村老交谈，追查运水队下落", "沿山道调查三处运水队留下的痕迹", "击退焦林中的灾狼与赤鸦", "射断笼顶绳结，击退灾狼，与运水队交谈", "满蓄力射碎三枚赤日火核，打开山门", "穿过山门，前往山泉祭坛", "击败焚角山魈，撞壁后射击角上火核", "调查恢复的山泉，取出赤日碎片", "第 1 章完成 · 山泉复流"
        };
        public string Objective => objectives[(int)Current];
        public string Progress
        {
            get
            {
                if (Current == Stage.Trail) return "踪迹  " + CluesFound + " / 3";
                if (Current == Stage.Ambush) return "灾兽  " + Defeated(Stage.Ambush) + " / " + Total(Stage.Ambush);
                if (Current == Stage.Rescue) return "绳结 " + (RopeCut ? "已断" : "未断") + " · 灾狼 " + Defeated(Stage.Rescue) + " / 2";
                if (Current == Stage.Gate) return "火核  " + Defeated(Stage.Gate) + " / 3";
                if (Current == Stage.Boss) return Boss && Boss.Health <= Boss.MaxHealth / 2 ? "首领狂暴 · 预警时间缩短" : "引诱冲撞岩壁，抓住破绽";
                return "支路：补给与古碑可探索";
            }
        }
        int Total(Stage stage) { int n = 0; foreach (var a in Actors) if (a.Encounter == stage && a.IsEnemy) n++; return n; }
        int Defeated(Stage stage)
        {
            int n = 0;
            foreach (var a in Actors) if (a.Encounter == stage && a.Defeated && (a.IsEnemy || a.Kind == ChapterOneActor.Type.Core)) n++;
            return n;
        }
        bool Near(Transform t) => t && Vector3.Distance(Story.player.transform.position, t.position) < 3.5f;
        public bool CanInteract => InteractionTarget != null;
        Transform InteractionTarget
        {
            get
            {
                if (Current == Stage.Village && Near(Elder)) return Elder;
                if (Current == Stage.Trail && CluesFound < Clues.Count && Near(Clues[CluesFound])) return Clues[CluesFound];
                if (Current == Stage.Rescue && RopeCut && Defeated(Stage.Rescue) == 2 && Near(Captive)) return Captive;
                if (Current == Stage.Spring && Near(Spring)) return Spring;
                if (!supplyTaken && Near(Supply)) return Supply;
                if (!loreRead && Near(Lore)) return Lore;
                return null;
            }
        }
        public string InteractionLabel => InteractionTarget == Supply ? "取用补给" : InteractionTarget == Lore ? "阅读古碑"
            : Current == Stage.Trail ? "调查踪迹" : Current == Stage.Spring ? "调查山泉" : "交谈";
        public Transform Goal
        {
            get
            {
                if (Current == Stage.Village) return Elder;
                if (Current == Stage.Trail) return Clues[Mathf.Min(CluesFound, Clues.Count - 1)];
                if (Current == Stage.Ambush || Current == Stage.Rescue || Current == Stage.Gate)
                {
                    foreach (var a in Actors)
                        if (a.Encounter == Current && !a.Defeated && a.Kind != ChapterOneActor.Type.WeakPoint) return a.transform;
                    return Captive;
                }
                if (Current == Stage.Approach) return Shrine;
                if (Current == Stage.Boss) return Boss.Exposed ? WeakPoint.transform : Boss.transform;
                if (Current == Stage.Spring) return Spring;
                return null;
            }
        }
        public string GoalLabel => Current == Stage.Village ? "村老" : Current == Stage.Trail ? "运水队踪迹" : Current == Stage.Gate ? "赤日火核"
            : Current == Stage.Approach ? "山泉祭坛" : Current == Stage.Boss ? Boss.Exposed ? "角上火核 · 破绽" : "焚角山魈"
            : Current == Stage.Spring ? "复流山泉" : Current == Stage.Rescue && RopeCut && Defeated(Stage.Rescue) == 2 ? "运水队长" : "灾兽 / 绳结";

        public void Initialize(PrologueDirector story)
        {
            Story = story;
            WorldRoot = new GameObject("第 1 章 · 追火入山").transform;
            WorldRoot.position = new Vector3(180, 0, 0);
            ChapterOneWorld.Build(this);
            if (Story.nineSuns) Story.nineSuns.SetActive(true);
            if (Story.sunLight) { Story.sunLight.color = new Color(1, .65f, .35f); Story.sunLight.intensity = 1.2f; }
            Story.player.yaw = 0; Story.player.pitch = 8;
            Story.player.Teleport(Point(0, 0));
            SetStage(Stage.Village, Point(0, 0));
            Story.Notify("第 1 章 · 追火入山。山口的村老正等着你。", 7);
        }
        public Vector3 Point(float x, float z, float y = 0) => WorldRoot.position + new Vector3(x, y, z);
        void SetStage(Stage stage, Vector3 safePosition)
        {
            Current = stage; checkpoint = safePosition;
            Story.ChapterCheckpoint(checkpoint);
            Story.Notify(Objective, 7);
        }
        void Update()
        {
            if (!Story || !Story.CanControl) return;
            TickStones(Time.deltaTime);
            if (Current == Stage.Ambush && Defeated(Stage.Ambush) == Total(Stage.Ambush))
                SetStage(Stage.Rescue, Point(0, 94));
            if (Current == Stage.Approach && Vector3.Distance(Story.player.transform.position, Shrine.position) < 15)
            {
                SetStage(Stage.Boss, Point(0, 160));
                Story.Notify("焚角山魈挡住了山泉！引它撞向石壁，蓄力射击角上的火核。", 9);
            }
        }
        public void Interact()
        {
            Transform target = InteractionTarget;
            if (!target) return;
            if (target == Supply)
            {
                supplyTaken = true; Supply.gameObject.SetActive(false);
                Story.ChapterCheckpoint(checkpoint); Story.Notify("取用药草与清水，气血恢复。支路补给只能取用一次。", 6); return;
            }
            if (target == Lore)
            {
                loreRead = true;
                Story.Talk(new[] { "古碑|赤日之火，借生灵而行。焚角既裂，方可伤其本源。", "后羿|守山兽也只是被烈日侵蚀了……必须找到那枚碎片。" }, null); return;
            }
            switch (Current)
            {
                case Stage.Village:
                    Story.Talk(new[] { "村老|羿，封印虽破，山泉还是一滴水也没有。昨日进山的运水队，至今未归。", "后羿|我沿山道寻人。让乡亲们等我的消息。", "村老|沿途留意他们的水桶与断箭。焦林里的野兽已经变了模样，切莫硬接扑击。" }, () => SetStage(Stage.Trail, Point(0, 12))); break;
                case Stage.Trail:
                    string[] clues = { "破损水桶|桶底仍有湿痕，运水队曾在这里停留。前方留下了凌乱的脚印。", "折断的箭|箭杆被利齿咬碎，焦黑的羽毛落在一旁。山林中不止有灾狼。", "焦黑脚印|拖拽痕迹通向山道营地。远处传来狼嚎与呼救。" };
                    string clue = clues[CluesFound++];
                    Story.Talk(new[] { clue }, () => { if (CluesFound == 3) SetStage(Stage.Ambush, Point(0, 64)); }); break;
                case Stage.Rescue:
                    Story.Talk(new[] { "运水队长|多谢！那些灾狼把我们逼进木笼。山门的三团火挡住了回泉眼的路。", "后羿|你们先回村。山门与守山兽，交给我。", "运水队长|那头山魈会直冲岩壁，撞晕时角上发红。猎人，抓住那一瞬间！" }, () => SetStage(Stage.Gate, Point(0, 119))); break;
                case Stage.Spring:
                    Story.Talk(new[] { "后羿|水回来了……泉底却还压着这枚赤红碎片。", "运水队长|灾兽身上的火，竟与它一模一样。难道山魈也是受害者？", "后羿|碎片只是开端。远山的赤光还在蔓延，我会继续追下去。" }, () => { Current = Stage.Complete; ClearStones(); Story.player.archer.CancelDraw(); }); break;
            }
        }
        public bool Active(ChapterOneActor actor) => Story && Story.CanControl && actor.Encounter == Current && !actor.Defeated;
        public void Hit(ChapterOneActor actor, float charge)
        {
            if (!Active(actor)) return;
            if (actor.Kind == ChapterOneActor.Type.Boss) { Story.Notify("坚甲挡住了箭。引它冲撞岩壁，再射击角上火核。"); return; }
            if (actor.Kind == ChapterOneActor.Type.WeakPoint)
            {
                if (!Boss.Exposed) return;
                Boss.TakeDamage(charge >= .8f ? 4 : 1);
                Story.Notify(charge >= .8f ? "满蓄力命中火核！" : "火核受损，满蓄力伤害更高。");
                if (Boss.Defeated) DefeatBoss();
                return;
            }
            if (actor.Kind == ChapterOneActor.Type.Core && charge < .8f) { Story.Notify("火核需要满蓄力才能击碎。"); return; }
            actor.TakeDamage(charge >= .8f ? 2 : 1);
            if (!actor.Defeated) return;
            if (actor.Kind == ChapterOneActor.Type.Rope)
            {
                RopeCut = true; Cage.SetActive(false); Story.Notify("绳结已断！击退周围灾狼，再与运水队交谈。", 6);
            }
            else if (actor.Kind == ChapterOneActor.Type.Core && Defeated(Stage.Gate) == 3)
            {
                Gate.SetActive(false); SetStage(Stage.Approach, Point(0, 145));
            }
            else Story.Notify(Progress);
        }
        void DefeatBoss()
        {
            WeakPoint.gameObject.SetActive(false); ClearStones(); BossWarning = null;
            Corruption.SetActive(false); Water.SetActive(true);
            SetStage(Stage.Spring, Point(0, 170));
            Story.Notify("焚角山魈已倒下，山泉复流！靠近泉眼调查赤日碎片。", 8);
        }
        public void Retry()
        {
            ClearStones(); BossWarning = null;
            foreach (var actor in Actors)
                if (actor.Encounter == Current && !actor.Defeated) actor.ResetEncounter();
            if (Current == Stage.Boss) { Boss.ResetEncounter(); WeakPoint.gameObject.SetActive(false); }
            Story.player.Teleport(checkpoint); Story.ChapterCheckpoint(checkpoint);
            Story.Notify(Current == Stage.Boss ? "从祭坛入口重试，首领气血已恢复。" : "已返回任务检查点，完成的目标保留。");
        }
        public void ThrowStone(Vector3 from, Vector3 target)
        {
            target.y = .05f;
            var visual = ChapterOneWorld.Shape(WorldRoot, PrimitiveType.Sphere, "飞掷火石", from, Vector3.one * .7f, ChapterOneWorld.Fire, false);
            var warning = ChapterOneWorld.Shape(WorldRoot, PrimitiveType.Cylinder, "火石落点预警", target, new Vector3(3.5f, .015f, 3.5f), ChapterOneWorld.Warning, false);
            stones.Add(new FireStone { visual = visual, start = from, end = target, warning = warning });
        }
        void TickStones(float dt)
        {
            for (int i = stones.Count - 1; i >= 0; i--)
            {
                var stone = stones[i]; stone.age += dt;
                float t = Mathf.Clamp01(stone.age / 1.25f);
                stone.visual.transform.position = Vector3.Lerp(stone.start, stone.end, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 4;
                if (t < 1) continue;
                Vector3 delta = Story.player.transform.position - stone.end; delta.y = 0;
                if (delta.magnitude < 1.9f) Story.Hurt(18);
                Destroy(stone.visual); Destroy(stone.warning); stones.RemoveAt(i);
            }
        }
        void ClearStones() { foreach (var s in stones) { Destroy(s.visual); Destroy(s.warning); } stones.Clear(); }
        public void DrawHUD()
        {
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label) { font = Story.chineseFont, fontSize = 22, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1, .85f, .5f) } };
                bossText = new GUIStyle(text) { fontSize = 26 };
            }
            if (Current != Stage.Boss || !Boss || !Story.CanControl) return;
            Color old = GUI.color;
            GUI.color = new Color(.06f, .025f, .02f, .9f); GUI.DrawTexture(new Rect(610, 120, 700, 70), Texture2D.whiteTexture);
            GUI.color = new Color(.85f, .16f, .04f); GUI.DrawTexture(new Rect(630, 173, 660 * Boss.Health / (float)Boss.MaxHealth, 8), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(620, 125, 680, 44), "焚角山魈  " + Boss.Health + " / " + Boss.MaxHealth, bossText);
            GUI.Label(new Rect(530, 210, 860, 55), Boss.Exposed ? "破绽！满蓄力射击角上火核" : BossWarning ?? "侧闪躲冲撞，后闪躲砸地", text);
        }
        void OnDestroy()
        {
            ClearStones(); if (WorldRoot) Destroy(WorldRoot.gameObject);
        }
    }
}

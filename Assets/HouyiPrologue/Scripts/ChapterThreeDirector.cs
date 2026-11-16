using System.Collections.Generic;
using UnityEngine;

namespace Houyi.NineSuns
{
    public sealed class ChapterThreeDirector : MonoBehaviour
    {
        public enum Stage { Entry, Bridge, Sundials, Courtyard, Boss, Revelation, Complete }
        public Stage Current { get; private set; }
        public bool Finished => Current == Stage.Complete;
        public PrologueDirector Story { get; private set; }
        public Transform WorldRoot, Inscription, Supply, Altar;
        public GameObject Bridge, BridgeGate, Seal, ArenaGate;
        public ChapterThreeActor Boss;
        public readonly List<ChapterThreeActor> Actors = new List<ChapterThreeActor>();
        public readonly List<Transform> Pillars = new List<Transform>();
        public bool SupplyTaken { get; private set; }
        public string Warning;
        Vector3 checkpoint;
        readonly List<Hazard> hazards = new List<Hazard>();
        readonly List<ChapterThreeActor> roots = new List<ChapterThreeActor>();
        sealed class Hazard
        {
            public GameObject visual, projectile;
            public Vector3 origin, direction;
            public float delay, width, length, travelled;
            public int damage;
            public bool flying, launched, hit;
        }
        static readonly string[] objectives = {
            "调查入口断碑，击退遗址守卫", "射断断桥两侧悬索，放下木桥", "站在各日晷对应的蓝色区域，满蓄力射碎火核",
            "清除内庭守卫与高台射手，前往炎卫祭场", "击败扶桑炎卫：走位避炎矢、破树根、射胸核", "满蓄力射断祭坛赤日锁链，然后调查祭坛", "第 3 章完成 · 扶桑遗址" };
        public string Objective => Current == Stage.Sundials && BrokenCores == 3 ? "清除日晷守卫，解除中央封印" : objectives[(int)Current];
        public int BrokenCores => CountDefeated(ChapterThreeActor.Kind.Core);
        public string Progress => Current == Stage.Sundials ? "日晷火核 " + BrokenCores + " / 3 · 必须站入对应蓝色区域"
            : Current == Stage.Bridge ? "悬索 " + CountDefeated(ChapterThreeActor.Kind.Rope) + " / 2"
            : Current == Stage.Boss ? Warning ?? "聚能时射胸核 · 冲击波躲石柱后" : "探索遗址 · 补给可恢复气血与气力";
        public string GoalLabel => Current == Stage.Entry ? "入口断碑" : Current == Stage.Bridge ? "断桥悬索"
            : Current == Stage.Sundials ? "日晷背面火核" : Current == Stage.Courtyard ? "内庭守卫" : Current == Stage.Boss ? "扶桑炎卫" : "赤日祭坛";
        public Transform Goal {
            get {
                if (Current == Stage.Entry) return Inscription;
                if (Current == Stage.Boss) return Boss.transform;
                foreach (var a in Actors) if (a.Encounter == Current && !a.Defeated && a != Boss) return a.transform;
                return Finished ? null : Altar;
            }
        }
        int CountDefeated(ChapterThreeActor.Kind kind) { int n = 0; foreach (var a in Actors) if (a.Type == kind && a.Defeated) n++; return n; }
        public bool Cleared(Stage stage) { foreach (var a in Actors) if (a.Encounter == stage && !a.Defeated) return false; return true; }
        bool Near(Transform t) => t && Vector3.Distance(Story.player.transform.position, t.position) < 3.5f;
        public bool CanInteract => !SupplyTaken && Near(Supply) || Current == Stage.Entry && Near(Inscription) || Current == Stage.Revelation && Cleared(Stage.Revelation) && Near(Altar);
        public string InteractionLabel => !SupplyTaken && Near(Supply) ? "取用补给" : Current == Stage.Entry ? "调查断碑" : "调查祭坛";
        public Vector3 Point(float x, float z, float y = 0) => WorldRoot.position + new Vector3(x, y, z);
        public void Initialize(PrologueDirector story)
        {
            Story = story;
            WorldRoot = new GameObject("第 3 章 · 扶桑遗址").transform;
            WorldRoot.position = new Vector3(700, 0, 0);
            ChapterThreeWorld.Build(this); Story.ApplyWhiteLighting();
            if (Story.nineSuns) Story.nineSuns.SetActive(true);
            Story.player.yaw = 0; Story.player.pitch = 8; Story.player.Teleport(Point(0, 0));
            SetStage(Stage.Entry, Point(0, 0));
        }
        void SetStage(Stage stage, Vector3 safe)
        {
            Current = stage; checkpoint = safe; SyncWorld(); Story.ChapterCheckpoint(safe); Story.Notify(Objective, 7); Story.SaveProgress();
        }
        void SyncWorld()
        {
            bool openBridge = CountDefeated(ChapterThreeActor.Kind.Rope) == 2 || Current > Stage.Bridge;
            Bridge.SetActive(openBridge); BridgeGate.SetActive(!openBridge);
            Seal.SetActive(Current < Stage.Courtyard);
            ArenaGate.SetActive(Current == Stage.Boss);
            Supply.gameObject.SetActive(!SupplyTaken);
        }
        void Update() { if (Story && Story.CanControl) Tick(Time.deltaTime); }
        public void Tick(float dt)
        {
            TickHazards(dt);
            if (Current == Stage.Bridge && Cleared(Stage.Bridge)) SetStage(Stage.Sundials, Point(0, 60));
            if (Current == Stage.Sundials && BrokenCores == 3 && Cleared(Stage.Sundials)) SetStage(Stage.Courtyard, Point(0, 114));
            if (Current == Stage.Courtyard && Cleared(Stage.Courtyard) && Story.player.transform.position.z >= Point(0, 153).z)
                SetStage(Stage.Boss, Point(0, 154));
        }
        public void Interact()
        {
            if (!CanInteract) return;
            if (!SupplyTaken && Near(Supply)) { SupplyTaken = true; SyncWorld(); Story.ChapterCheckpoint(checkpoint); Story.SaveProgress(); Story.Notify("清水与药草恢复气血、气力。补给已用完。"); return; }
            if (Current == Stage.Entry)
            {
                if (!Cleared(Stage.Entry)) { Story.Notify("先击退断碑附近的守卫。"); return; }
                Story.Talk(new[] { "扶桑断碑|日晷引火，赤链缚灵。绕其背，断其心，方可入庭。", "后羿|兽群被日力侵蚀的根源，就在这座遗址。先射断悬索，放下前方木桥。" }, () => SetStage(Stage.Bridge, Point(0, 25)));
            }
            else if (Current == Stage.Revelation)
                Story.Talk(new[] { "扶桑炎卫|锁链……终于断了。我们被迫将赤日之力灌入群兽。", "后羿|天火背后果然有人操控。通向上层祭台的路在哪里？", "扶桑炎卫|沿扶桑古根向上……九日的锁阵尚未解除。", "后羿|我会一一射断。下一程，扶桑天阶！" }, () => { Current = Stage.Complete; ClearThreats(); Story.player.archer.CancelDraw(); Story.SaveProgress(); Story.Notify("解锁：精准回气 · 满蓄力命中弱点恢复 10 点气力", 8); });
        }
        public bool Active(ChapterThreeActor actor) => Story && Story.CanControl && actor.Encounter == Current && !actor.Defeated;
        public bool InFiringZone(ChapterThreeActor actor)
        {
            if (!actor.FiringZone) return false;
            Vector3 position = Story.player.transform.position;
            Vector3 local = actor.FiringZone.InverseTransformPoint(position);
            return local.x * local.x + local.z * local.z <= .25001f
                && Mathf.Abs(position.y - actor.FiringZone.position.y) <= 1.25f;
        }
        public void Hit(ChapterThreeActor actor, float charge, bool weakPoint = false)
        {
            if (!Active(actor)) return;
            if (actor.Type == ChapterThreeActor.Kind.Core)
            {
                if (!InFiringZone(actor)) { Story.Notify("请先站入这座日晷对应的蓝色区域，再射击火核。"); return; }
                if (charge < .8f) { Story.Notify("站稳蓝色区域，满蓄力射击火核。"); return; }
            }
            if (actor.Type == ChapterThreeActor.Kind.Chain && charge < .8f) { Story.Notify("赤日锁链需要满蓄力射断。"); return; }
            if (actor == Boss)
            {
                if (!Boss.Exposed || Boss.Charging && !weakPoint) { Story.Notify("炎卫石甲坚硬，聚能时瞄准发光胸核！"); return; }
                if (charge >= .8f && weakPoint)
                {
                    if (Boss.Charging) Boss.Stagger(Boss.Enraged ? 1.7f : 2.1f);
                    if (Story.PrecisionRecoveryUnlocked) Story.player.RecoverStamina(10);
                }
            }
            actor.TakeDamage(actor.Type == ChapterThreeActor.Kind.Core || actor.Type == ChapterThreeActor.Kind.Rope ? actor.MaxHealth : charge >= .8f ? 4 : 1);
            if (actor.Defeated)
            {
                if (actor == Boss) { ClearThreats(); Warning = null; SetStage(Stage.Revelation, Point(0, 154)); }
                if (actor.Type == ChapterThreeActor.Kind.Core) Story.Notify("日晷熄灭 · " + BrokenCores + " / 3");
                Story.SaveProgress();
            }
        }
        public int SunburstHit(Vector3 center, float charge, float radius)
        {
            if (!Story.CanControl) return 0;
            int n = 0;
            foreach (var a in Actors)
            {
                if (!a.IsEnemy || !Active(a) || a == Boss && !Boss.Exposed) continue;
                var col = a.GetComponent<Collider>();
                if (Vector3.Distance(col.ClosestPoint(center), center) > radius) continue;
                a.TakeDamage(charge >= .8f ? 4 : 2); n++;
                if (a.Defeated) { if (a == Boss) { ClearThreats(); SetStage(Stage.Revelation, Point(0, 154)); } Story.SaveProgress(); }
            }
            return n;
        }
        public void LineAttack(Vector3 origin, Vector3 direction, float delay, float width, float length, int damage, bool flying = false)
        {
            direction.y = 0; direction.Normalize(); origin.y = .08f;
            var visual = ChapterThreeWorld.Shape(this, PrimitiveType.Cube, "炎矢地面预警", origin + direction * length * .5f, new Vector3(width, .04f, length), ChapterThreeWorld.Fire, false);
            visual.transform.rotation = Quaternion.LookRotation(direction);
            hazards.Add(new Hazard { visual = visual, origin = origin, direction = direction, delay = delay, width = width, length = length, damage = damage, flying = flying });
        }
        void TickHazards(float dt)
        {
            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                var h = hazards[i]; h.delay -= dt; if (h.delay > 0) continue;
                Vector3 d = Story.player.transform.position - h.origin; d.y = 0; float along = Vector3.Dot(d, h.direction);
                float start = 0, end = h.length;
                if (h.flying)
                {
                    float step = h.launched ? dt : -h.delay;
                    if (!h.launched)
                    {
                        h.launched = true;
                        h.projectile = ChapterThreeWorld.Shape(this, PrimitiveType.Cube, "炎矢立方体", h.origin + Vector3.up * 1.2f, Vector3.one * 1.2f, ChapterThreeWorld.Fire, false);
                    }
                    start = h.travelled;
                    h.travelled = Mathf.Min(h.length, h.travelled + Mathf.Max(0, step) * 18);
                    end = h.travelled;
                    h.projectile.transform.position = h.origin + h.direction * h.travelled + Vector3.up * 1.2f;
                    h.projectile.transform.Rotate(new Vector3(70, 110, 40) * Mathf.Max(0, step), Space.Self);
                }
                if (!h.hit && along >= start - (h.flying ? .6f : 0) && along <= end + (h.flying ? .6f : 0)
                    && Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Vector3.up, h.direction))) < h.width * .5f + .3f)
                { Story.Hurt(h.damage); h.hit = true; }
                if (h.flying && h.travelled < h.length) continue;
                ClearHazard(h); hazards.RemoveAt(i);
            }
        }
        public void CreateCage(Vector3 center)
        {
            ClearRoots(); center.y = 0;
            // Seven roots leave a broad opening; each segment can also be shot apart.
            for (int i = 0; i < 7; i++)
            {
                float angle = (45 + i * 45) * Mathf.Deg2Rad;
                var p = center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * 3;
                var root = ChapterThreeWorld.SpawnActor(this, ChapterThreeActor.Kind.Root, Stage.Boss, p, false); roots.Add(root);
            }
        }
        public bool BehindPillar(Vector3 position)
        {
            Vector3 origin = Boss.transform.position; origin.y = 0; position.y = 0;
            Vector3 d = position - origin; float length = d.magnitude; if (length < .01f) return false; d /= length;
            foreach (var pillar in Pillars)
            {
                Vector3 p = pillar.position - origin; p.y = 0; float along = Vector3.Dot(p, d);
                if (along > 0 && along < length && (p - along * d).magnitude <= 1.55f) return true;
            }
            return false;
        }
        public void Shockwave() { if (!BehindPillar(Story.player.transform.position)) Story.Hurt(Boss.Enraged ? 36 : 32); ClearRoots(); }
        void ClearRoots() { foreach (var r in roots) if (r) { r.gameObject.SetActive(false); Destroy(r.gameObject); } roots.Clear(); }
        void ClearHazard(Hazard h)
        {
            if (h.visual) { h.visual.SetActive(false); Destroy(h.visual); }
            if (h.projectile) { h.projectile.SetActive(false); Destroy(h.projectile); }
        }
        public void ClearThreats() { foreach (var h in hazards) ClearHazard(h); hazards.Clear(); ClearRoots(); }
        public void EnterBossPreview()
        {
            ClearThreats(); Warning = null;
            foreach (var a in Actors) if (a.Encounter < Stage.Boss) a.TakeDamage(a.MaxHealth);
            Boss.ResetEncounter(); Story.player.archer.CancelForAction();
            if (Story.player.Sunburst) { Story.player.Sunburst.CancelSelection(); Story.player.Sunburst.RestoreCooldown(0); }
            Story.player.yaw = 0; Story.player.pitch = 8; Story.player.Teleport(Point(0, 154));
            SetStage(Stage.Boss, Point(0, 154));
        }
        public HouyiProgress CaptureProgress()
        {
            var flags = new bool[Actors.Count]; for (int i = 0; i < flags.Length; i++) flags[i] = Actors[i].Defeated;
            return new HouyiProgress { chapter = 3, stage = (int)Current, checkpoint = checkpoint, actors = flags, supplyTaken = SupplyTaken };
        }
        public bool RestoreProgress(HouyiProgress p)
        {
            if (p.chapter != 3 || p.stage < 0 || p.stage > 6 || p.actors == null || p.actors.Length != Actors.Count) return false;
            ClearThreats(); Current = (Stage)p.stage; checkpoint = p.checkpoint; SupplyTaken = p.supplyTaken;
            for (int i = 0; i < Actors.Count; i++) { Actors[i].ResetEncounter(); if (p.actors[i]) Actors[i].TakeDamage(Actors[i].MaxHealth); }
            SyncWorld(); return true;
        }
        public void Retry()
        {
            ClearThreats(); Warning = null;
            foreach (var a in Actors) if (a.Encounter == Current && !a.Defeated) a.ResetEncounter();
            if (Current == Stage.Boss) Boss.ResetEncounter();
            Story.player.Teleport(checkpoint); Story.ChapterCheckpoint(checkpoint);
        }
        public void DrawHUD()
        {
            if (Current != Stage.Boss || !Story.CanControl) return;
            var style = new GUIStyle(GUI.skin.label) { font = Story.chineseFont, fontSize = 25, alignment = TextAnchor.MiddleCenter };
            Color old = GUI.color; GUI.color = new Color(.08f, .04f, .02f, .95f); GUI.DrawTexture(new Rect(610, 120, 700, 75), Texture2D.whiteTexture);
            GUI.color = new Color(.95f, .35f, .06f); GUI.DrawTexture(new Rect(630, 179, 660 * Boss.Health / Boss.MaxHealth, 7), Texture2D.whiteTexture); GUI.color = old;
            GUI.Label(new Rect(610, 125, 700, 45), "扶桑炎卫" + (Boss.Enraged ? " · 狂暴" : "") + "  " + Boss.Health + " / " + Boss.MaxHealth, style);
            GUI.Label(new Rect(410, 205, 1100, 55), Warning ?? "三线炎矢 · 破树根 · 聚能射胸核", style);
        }
        void OnDestroy() { ClearThreats(); if (WorldRoot) Destroy(WorldRoot.gameObject); }
    }
}

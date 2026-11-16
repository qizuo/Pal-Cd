using UnityEngine;

namespace Houyi.NineSuns
{
    public sealed class ChapterThreeActor : MonoBehaviour
    {
        public enum Kind { Guard, Archer, Rope, Core, Chain, Root, Boss }
        public Kind Type;
        public ChapterThreeDirector Chapter;
        public ChapterThreeDirector.Stage Encounter;
        public Transform FiringZone;
        public int MaxHealth;
        public float Health { get; private set; }
        public bool Defeated => Health <= 0;
        public bool IsEnemy => Type == Kind.Guard || Type == Kind.Archer || Type == Kind.Boss;
        public bool Charging => Type == Kind.Boss && state == 1 && attack == 3;
        public bool Enraged => Type == Kind.Boss && Health <= MaxHealth * .5f;
        public bool Exposed => Type == Kind.Boss && (Charging || state == 2);
        Vector3 home, locked, cageCenter;
        Quaternion homeRotation;
        Vector3 homeScale;
        float timer;
        int state, attack, cycle, weakFrame = -1;
        GameObject core;
        public void Initialize(ChapterThreeDirector chapter, Kind kind, ChapterThreeDirector.Stage stage, int health, bool register)
        {
            Chapter = chapter; Type = kind; Encounter = stage; MaxHealth = health;
            home = transform.position; homeRotation = transform.rotation; homeScale = transform.localScale;
            if (register) chapter.Actors.Add(this);
            if (kind == Kind.Boss)
            {
                core = ChapterThreeWorld.Shape(chapter, PrimitiveType.Sphere, "炎卫胸核", home + Vector3.up * 2.7f + Vector3.forward * 1.4f, Vector3.one * 1.15f, ChapterThreeWorld.Fire);
                core.transform.SetParent(transform, true); core.AddComponent<ChapterThreeWeakPoint>().boss = this;
            }
            ResetEncounter();
        }
        public void ResetEncounter()
        {
            Health = MaxHealth; state = cycle = 0; timer = 1.6f; weakFrame = -1;
            transform.position = home; transform.rotation = homeRotation; transform.localScale = homeScale;
            gameObject.SetActive(true); foreach (var c in GetComponents<Collider>()) c.enabled = true;
            if (core) core.SetActive(false);
        }
        public void OnArrowHit(float charge) { if (weakFrame != Time.frameCount) Chapter.Hit(this, charge); }
        public void HitWeakPoint(float charge) { if (!Exposed) return; weakFrame = Time.frameCount; Chapter.Hit(this, charge, true); }
        public void TakeDamage(int amount)
        {
            if (Defeated) return;
            Health = Mathf.Max(0, Health - amount);
            if (Defeated) { foreach (var c in GetComponents<Collider>()) c.enabled = false; if (core) core.SetActive(false); }
        }
        public void Stagger(float seconds) { state = 2; timer = seconds; Chapter.Warning = "胸核受创！炎卫陷入硬直"; Chapter.ClearThreats(); }
        void Update()
        {
            if (!Chapter || !Chapter.Story) return;
            if (Defeated) { transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 5); return; }
            if (core) core.SetActive(Exposed && Chapter.Current == Encounter);
            if (Chapter.Active(this) && IsEnemy) Tick(Time.deltaTime);
        }
        public void Tick(float dt)
        {
            if (!Chapter.Active(this) || !IsEnemy) return;
            Vector3 delta = Chapter.Story.player.transform.position - transform.position; delta.y = 0;
            float distance = delta.magnitude; timer -= dt;
            if (state == 0)
            {
                if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), dt * 5);
                if (Type == Kind.Guard && distance > 2 && distance < 28) transform.position += delta.normalized * dt * 2.5f;
                if (timer > 0 || Type == Kind.Guard && distance > 4 || Type == Kind.Archer && distance > 36) return;
                if (Type == Kind.Boss)
                {
                    int step = cycle++;
                    attack = Enraged ? RageAttack(step % 6) : step % 4;
                    if (attack == 0 && distance > 9) attack = 1;
                }
                else attack = Type == Kind.Archer ? 1 : 0;
                BeginAttack(attack, delta);
            }
            else if (state == 1 && timer <= 0)
            {
                if (attack == 2)
                {
                    Chapter.CreateCage(cageCenter);
                    Chapter.LineAttack(transform.position, cageCenter - transform.position, 1.2f, 1.6f, 38, 20, true);
                    Chapter.Warning = "树根连射 · 射破树根或穿过缺口，避开地面红线";
                }
                if (attack == 3) Chapter.Shockwave();
                state = 2; timer = Type == Kind.Boss ? Enraged ? 1.25f : 1.7f : 1.2f;
            }
            else if (state == 2 && timer <= 0) { state = 0; timer = Type == Kind.Boss ? Enraged ? .4f : .7f : 1; if (Type == Kind.Boss) Chapter.Warning = null; }
        }
        static int RageAttack(int step) { return step == 0 || step == 2 ? 1 : step == 1 ? 2 : step == 4 ? 0 : 3; }
        void BeginAttack(int kind, Vector3 delta)
        {
            state = 1; attack = kind; locked = delta.sqrMagnitude > .01f ? delta.normalized : Vector3.forward;
            if (kind == 0)
            {
                timer = .85f;
                Chapter.LineAttack(transform.position, locked, .85f, Type == Kind.Boss ? 4.5f : 2.2f, Type == Kind.Boss ? 9 : 4, Type == Kind.Boss ? 24 : 10);
                if (Type == Kind.Boss) Chapter.Warning = "爪击预警 · 侧移离开正面";
            }
            else if (kind == 1)
            {
                timer = Type == Kind.Boss ? 1.6f : 1.1f;
                if (Type == Kind.Boss)
                {
                    int count = Enraged ? 5 : 3, volleys = Enraged ? 2 : 1;
                    for (int volley = 0; volley < volleys; volley++) for (int i = 0; i < count; i++)
                    {
                        float angle = (i - (count - 1) * .5f) * (Enraged ? 10f : 26);
                        Chapter.LineAttack(transform.position, Quaternion.Euler(0, angle, 0) * locked, 1.1f + i * .15f + volley * .85f, 1.6f, 38, 20, true);
                    }
                    timer = Enraged ? 2.65f : 1.5f;
                    Chapter.Warning = Enraged ? "狂暴 · 双轮五线炎矢，移到红线之间的空隙" : "三线炎矢 · 躲开飞来的方块，横向走位";
                }
                else Chapter.LineAttack(transform.position, locked, 1.1f, 1.6f, 36, 12, true);
            }
            else if (kind == 2)
            {
                timer = 1.4f; cageCenter = Chapter.Story.player.transform.position;
                ChapterThreeWorld.CagePreview(Chapter, cageCenter, timer);
                Chapter.Warning = "树根囚笼 · 离开标记，或寻找缺口、射碎树根";
            }
            else { timer = Enraged ? 2.4f : 3; Chapter.Warning = Enraged ? "狂暴聚能 · 2.4 秒内射胸核，或躲石柱后" : "赤日聚能 · 3 秒内满蓄力射胸核打断，或躲到石柱后"; }
            if (core) core.SetActive(Exposed);
        }
        void OnDestroy() { if (core) Destroy(core); }
    }

    public sealed class ChapterThreeWeakPoint : MonoBehaviour
    {
        public ChapterThreeActor boss;
        public void OnArrowHit(float charge) { boss.HitWeakPoint(charge); }
    }
}

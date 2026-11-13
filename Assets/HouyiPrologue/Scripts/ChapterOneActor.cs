using UnityEngine;

namespace Houyi.NineSuns
{
    public sealed class ChapterOneActor : MonoBehaviour
    {
        public enum Type { Wolf, Crow, Boss, Rope, Core, WeakPoint }
        public enum AttackState { Idle, Windup, Dash, Recover, Exposed }
        public Type Kind;
        public ChapterOneDirector Chapter;
        public ChapterOneDirector.Stage Encounter;
        public int MaxHealth = 2;
        public int Health { get; private set; }
        public bool Defeated => Health <= 0;
        public bool IsEnemy => Kind == Type.Wolf || Kind == Type.Crow || Kind == Type.Boss;
        public bool Exposed => State == AttackState.Exposed && !Defeated;
        public AttackState State { get; private set; }
        public Vector3 Home { get; private set; }
        float timer, dashRemaining;
        int attackIndex;
        bool dealtDamage;
        Vector3 lockedDirection, scale;
        GameObject telegraph;
        bool Enraged => Kind == Type.Boss && Health <= MaxHealth / 2;

        public void Initialize(ChapterOneDirector chapter, Type kind, ChapterOneDirector.Stage encounter, int health)
        {
            Chapter = chapter; Kind = kind; Encounter = encounter; MaxHealth = health;
            Home = transform.position; scale = transform.localScale;
            ResetEncounter(); chapter.Actors.Add(this);
        }
        public void ResetEncounter()
        {
            Health = MaxHealth; State = AttackState.Idle; timer = 1.1f; attackIndex = 0; dealtDamage = false;
            transform.position = Home; transform.localScale = scale;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
            if (telegraph) telegraph.SetActive(false);
        }
        public void OnArrowHit(float charge) { if (Chapter) Chapter.Hit(this, charge); }
        public void TakeDamage(int amount)
        {
            if (Defeated) return;
            Health = Mathf.Max(0, Health - amount);
            if (!Defeated) return;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            if (telegraph) telegraph.SetActive(false);
        }
        void Update()
        {
            if (!Chapter) return;
            if (Kind == Type.WeakPoint)
            {
                if (Chapter.Boss) transform.position = Chapter.Boss.transform.position + Vector3.up * 3.35f + Chapter.Boss.transform.forward * .7f;
                return;
            }
            if (Defeated) { transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 3); return; }
            if (!IsEnemy || !Chapter.Active(this)) return;
            float dt = Time.deltaTime;
            Vector3 delta = Chapter.Story.player.transform.position - transform.position; delta.y = 0;
            float distance = delta.magnitude;
            timer -= dt;
            switch (State)
            {
                case AttackState.Idle:
                    if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), dt * 6);
                    if (Kind == Type.Boss)
                    {
                        if (timer <= 0) BeginBossAttack(delta, distance);
                    }
                    else
                    {
                        if (distance > 5 && distance < 32)
                        {
                            Vector3 proposed = transform.position + delta.normalized * dt * (Kind == Type.Crow ? 3 : 2.2f);
                            if (Vector3.Distance(proposed, Home) < 22) transform.position = proposed;
                        }
                        if (distance < 8 && timer <= 0) Windup(delta, .8f, 0);
                    }
                    break;
                case AttackState.Windup:
                    if (timer <= 0)
                    {
                        if (telegraph) telegraph.SetActive(false);
                        if (Kind == Type.Boss && attackIndex % 3 == 1)
                        {
                            if (distance < 4.8f) Chapter.Story.Hurt(24);
                            Recover(1.2f);
                        }
                        else if (Kind == Type.Boss && attackIndex % 3 == 2)
                        {
                            Chapter.ThrowStone(transform.position + Vector3.up * 3, Chapter.Story.player.transform.position);
                            Recover(1);
                        }
                        else
                        {
                            State = AttackState.Dash; dashRemaining = Kind == Type.Boss ? 3.5f : .48f; dealtDamage = false;
                        }
                    }
                    break;
                case AttackState.Dash:
                    float step = Mathf.Min(dt, dashRemaining); dashRemaining -= step;
                    transform.position += lockedDirection * (Kind == Type.Boss ? 15 : 11) * step;
                    Vector3 hitDelta = Chapter.Story.player.transform.position - transform.position; hitDelta.y = 0;
                    if (!dealtDamage && hitDelta.magnitude < (Kind == Type.Boss ? 2 : 1.4f))
                    { Chapter.Story.Hurt(Kind == Type.Boss ? 28 : 12); dealtDamage = true; }
                    if (Kind == Type.Boss)
                    {
                        Vector3 local = transform.position - Chapter.WorldRoot.position;
                        if (Mathf.Abs(local.x) >= 17 || local.z <= 160 || local.z >= 208)
                        {
                            local.x = Mathf.Clamp(local.x, -17, 17); local.z = Mathf.Clamp(local.z, 160, 208);
                            transform.position = Chapter.WorldRoot.position + local;
                            State = AttackState.Exposed; timer = Enraged ? 3.8f : 5;
                            Chapter.WeakPoint.gameObject.SetActive(true);
                            Chapter.Story.Notify("山魈撞壁！角上火核暴露，快蓄力射击！", 4);
                        }
                        else if (dashRemaining <= 0) Recover(.8f);
                    }
                    else if (dashRemaining <= 0) Recover(1.3f);
                    break;
                case AttackState.Exposed:
                    if (timer <= 0) { Chapter.WeakPoint.gameObject.SetActive(false); Recover(.6f); }
                    break;
                case AttackState.Recover:
                    if (timer <= 0) { State = AttackState.Idle; timer = Enraged ? .35f : .8f; }
                    break;
            }
        }
        void Windup(Vector3 delta, float duration, int attack)
        {
            State = AttackState.Windup; timer = duration;
            lockedDirection = delta.sqrMagnitude > .01f ? delta.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(lockedDirection);
            if (!telegraph)
                telegraph = ChapterOneWorld.Shape(Chapter.WorldRoot, PrimitiveType.Cube, "攻击范围预警", transform.position, Vector3.one, ChapterOneWorld.Warning, false);
            telegraph.SetActive(true);
            bool radial = Kind == Type.Boss && attack == 1;
            float length = radial ? 9.6f : Kind == Type.Boss ? 30 : 7;
            telegraph.transform.localScale = radial ? new Vector3(length, .025f, length) : new Vector3(Kind == Type.Boss ? 2.4f : 1.2f, .025f, length);
            telegraph.transform.position = new Vector3(transform.position.x, .04f, transform.position.z) + (radial ? Vector3.zero : lockedDirection * length * .5f);
            telegraph.transform.rotation = Quaternion.LookRotation(lockedDirection);
            if (Kind == Type.Boss && attack == 2) telegraph.SetActive(false);
        }
        void BeginBossAttack(Vector3 delta, float distance)
        {
            // Fixed cycle guarantees a charge/weak-point opportunity even at range.
            int attack = attackIndex % 3;
            if (attack == 1 && distance > 5)
            {
                transform.position += delta.normalized * Mathf.Min(2, distance - 4);
                delta = Chapter.Story.player.transform.position - transform.position; delta.y = 0;
            }
            Chapter.BossWarning = attack == 0 ? "冲撞预警 · 向左右闪避，诱导它撞壁" : attack == 1 ? "砸地预警 · 后闪离开近身范围" : "投石预警 · 移动避开地面红圈";
            Windup(delta, Enraged ? .65f : 1.1f, attack);
        }
        void Recover(float duration)
        {
            State = AttackState.Recover; timer = duration;
            if (Kind == Type.Boss) { attackIndex++; Chapter.BossWarning = null; }
        }
        void OnDestroy() { if (telegraph) Destroy(telegraph); }
    }
}

using UnityEngine;

namespace Houyi.NineSuns
{
    public sealed class PrologueTarget : MonoBehaviour
    {
        public enum Kind { Training, Beast, Seal }
        public Kind kind;
        public PrologueDirector story;
        public int health = 1;
        public bool defeated;
        public Vector3 home;
        float attackTimer, hurtTime;
        Renderer[] surfaces;
        Vector3 initialScale;
        void Awake() { home = transform.position; initialScale = transform.localScale; surfaces = GetComponentsInChildren<Renderer>(); }
        public void RestoreDefeated()
        {
            defeated = true; health = 0; transform.localScale = Vector3.zero;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        }
        public void OnArrowHit(float charge)
        {
            if (defeated || !story || !story.IsTargetActive(kind)) return;
            if (kind == Kind.Seal && charge < .8f) { story.Notify("封印纹丝不动。需要将弓拉至满蓄力。"); return; }
            health -= kind == Kind.Beast && charge >= .8f ? 2 : 1;
            hurtTime = .16f;
            if (health > 0) { story.Notify("命中灾兽！满蓄力可一箭击破。"); return; }
            defeated = true;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            story.TargetDefeated(this);
        }
        void Update()
        {
            if (defeated)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 5);
                return;
            }
            if (hurtTime > 0) { hurtTime -= Time.deltaTime; transform.localScale = initialScale * (1 + hurtTime * .5f); }
            else transform.localScale = initialScale;
            if (kind != Kind.Beast || !story || !story.IsTargetActive(kind) || !story.CanControl) return;
            Vector3 delta = story.player.transform.position - transform.position; delta.y = 0;
            float distance = delta.magnitude;
            if (distance < 13 && distance > 1.5f)
            {
                Vector3 step = delta.normalized * Time.deltaTime * 1.35f;
                Vector3 proposed = transform.position + step;
                if (Vector3.Distance(proposed, home) < 13)
                { transform.position = proposed; transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 4); }
            }
            attackTimer -= Time.deltaTime;
            if (distance <= 1.8f && attackTimer <= 0)
            { attackTimer = 1.6f; story.Hurt(12); }
        }
    }
}

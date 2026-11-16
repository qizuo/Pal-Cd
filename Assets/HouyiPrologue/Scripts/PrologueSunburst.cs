using System;
using UnityEngine;

namespace Houyi.NineSuns
{
    [DefaultExecutionOrder(-150)]
    public sealed class PrologueSunburst : MonoBehaviour
    {
        public const float Radius = 4, Cooldown = 3;
        public bool Unlocked => player && player.story && (player.story.InChapterOne || player.story.InChapterTwo || player.story.InChapterThree);
        public bool Selected { get; private set; }
        public float CooldownRemaining => Mathf.Max(0, readyAt - Time.time);
        ProloguePlayer player;
        float readyAt;
        bool coolingDown;
        SunburstEffects effects;
        GUIStyle label, hint;

        void Awake()
        {
            player = GetComponent<ProloguePlayer>();
            effects = GetComponent<SunburstEffects>() ?? gameObject.AddComponent<SunburstEffects>();
        }
        void OnEnable() { if (player && player.archer) player.archer.ArrowImpactFactory = PrepareImpact; }
        void Update()
        {
            if (coolingDown && CooldownRemaining <= 0)
            {
                coolingDown = false;
                if (player && player.story) player.story.SaveProgress();
            }
            if (!Unlocked || !player.story.CanControl || player.archer.IsHitStunned)
            { CancelSelection(); return; }
            bool touch = player.TouchControls && player.TouchControls.Active;
            if (touch ? player.TouchControls.InputState.SkillPressed : Input.GetKeyDown(KeyCode.Q)) ToggleSelection();
            if (!touch && Selected && Input.GetMouseButtonDown(1)) CancelSelection();
        }
        public void ToggleSelection()
        {
            if (!Unlocked || !player.story.CanControl || player.archer.IsHitStunned) return;
            if (Selected) { CancelSelection(); return; }
            if (CooldownRemaining > 0) { player.story.Notify("落日震冷却中：" + CooldownRemaining.ToString("0.0") + " 秒"); return; }
            Selected = true;
            player.story.Notify("落日震：瞄准后蓄力放箭。再次点击技能可取消。", 4);
        }
        public void CancelSelection()
        {
            if (Selected && player && player.archer) player.archer.CancelDraw();
            Selected = false;
            effects.HideSelection();
        }
        public void RestoreCooldown(float seconds)
        {
            CancelSelection(); readyAt = Time.time + Mathf.Clamp(seconds, 0, Cooldown); coolingDown = seconds > 0;
        }
        Action<Vector3, Collider> PrepareImpact(GameObject arrow, float charge)
        {
            if (!Selected || !Unlocked || !player.story.CanControl || CooldownRemaining > 0)
            {
                effects.AttachNormalTrail(arrow);
                return null;
            }
            Selected = false; readyAt = Time.time + Cooldown; coolingDown = true;
            effects.HideSelection();
            effects.AttachTrail(arrow);
            ChapterOneDirector chapter = player.story.ChapterOne;
            ChapterTwoDirector chapterTwo = player.story.ChapterTwo;
            ChapterThreeDirector chapterThree = player.story.ChapterThree;
            player.story.SaveProgress();
            return (point, collider) =>
            {
                if (!chapter && !chapterTwo && !chapterThree) return;
                if (effects) effects.PlayImpact(point, Radius);
                int count = chapterThree ? chapterThree.SunburstHit(point, charge, Radius) : chapterTwo ? chapterTwo.SunburstHit(point, charge, Radius) : chapter.SunburstHit(point, charge, Radius);
                if (player.story.CanControl) player.story.Notify("落日震！命中 " + count + " 个敌人");
            };
        }
        void LateUpdate()
        {
            if (!Selected || !player.view || !player.story.CanControl) { effects.HideSelection(); return; }
            effects.UpdateCharge(player, player.archer.Charge);
            Ray ray = player.view.ViewportPointToRay(new Vector3(.5f, .5f));
            if (Physics.Raycast(ray, out RaycastHit hit, 160, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                effects.ShowPreview(hit.point, Radius);
            else effects.HidePreview();
        }
        void OnGUI()
        {
            if (!Unlocked || !player.story.CanControl) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { font = player.story.chineseFont, fontSize = 24, normal = { textColor = new Color(.35f, 1, 1) } };
                hint = new GUIStyle(label) { fontSize = 18, normal = { textColor = Color.white } };
            }
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));
            Color oldColor = GUI.color; GUI.color = new Color(.02f, .04f, .05f, .88f);
            GUI.DrawTexture(new Rect(40, 230, 370, 100), Texture2D.whiteTexture); GUI.color = oldColor;
            bool touch = player.TouchControls && player.TouchControls.Active;
            string state = Selected ? "已选中 · 蓄力放箭" : CooldownRemaining > 0 ? "冷却 " + CooldownRemaining.ToString("0.0") + "s" : "就绪";
            GUI.Label(new Rect(55, 242, 350, 40), (touch ? "落日震  " : "[Q] 落日震  ") + state, label);
            GUI.Label(new Rect(55, 285, 350, 30), Selected ? touch ? "再点技能取消 · 半径 4 米" : "Q / 右键取消 · 半径 4 米" : "范围攻击 · 冷却 " + Cooldown + " 秒", hint);
            GUI.matrix = old;
        }
        void OnApplicationFocus(bool focus) { if (!focus) CancelSelection(); }
        void OnDisable()
        {
            CancelSelection();
            if (player && player.archer && player.archer.ArrowImpactFactory == PrepareImpact) player.archer.ArrowImpactFactory = null;
        }
    }
}

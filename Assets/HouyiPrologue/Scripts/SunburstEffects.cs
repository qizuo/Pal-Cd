using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Houyi.NineSuns
{
    // Reuses the project's textures with modern renderers; no legacy emitters or lights.
    public sealed class SunburstEffects : MonoBehaviour
    {
        readonly List<Material> materials = new List<Material>();
        readonly List<GameObject> impacts = new List<GameObject>();
        Material ring, runes, trail, flash, fire, spark, smoke;
        GameObject preview, chargeCircle;
        ParticleSystem chargeSparks;
        MaterialPropertyBlock tint;
        public bool AssetsLoaded { get; private set; }

        void Awake()
        {
            tint = new MaterialPropertyBlock();
            var assets = Resources.Load<SunburstVfxAssets>("Houyi/SunburstVfx");
            if (!assets || !assets.shader) { Debug.LogError("落日震特效资源缺失。", this); return; }
            ring = Material(assets.shader, assets.ring, "冲击环");
            runes = Material(assets.shader, assets.runes, "蓄力符文");
            trail = Material(assets.shader, assets.trail, "技能箭拖尾");
            // AnimatedLineBlue is a 4 x 2 atlas; use the first long streak in its lower row.
            trail.mainTextureScale = new Vector2(.25f, .5f);
            flash = Material(assets.shader, assets.flash, "命中闪光");
            fire = Material(assets.shader, assets.fire, "火焰序列");
            spark = Material(assets.shader, assets.spark, "火星");
            smoke = Material(assets.shader, assets.smoke, "烟尘", true);
            AssetsLoaded = assets.ring && assets.runes && assets.trail && assets.flash && assets.fire && assets.spark && assets.smoke;
        }
        Material Material(Shader shader, Texture texture, string name, bool alpha = false)
        {
            var mat = new Material(shader) { name = "落日震 / " + name, mainTexture = texture };
            mat.SetInt("_DstBlend", (int)(alpha ? BlendMode.OneMinusSrcAlpha : BlendMode.One));
            materials.Add(mat); return mat;
        }
        GameObject Quad(string name, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = name;
            var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return go;
        }
        public static void SetTint(Renderer renderer, Color color, MaterialPropertyBlock block)
        {
            block.Clear(); block.SetColor("_Color", color); renderer.SetPropertyBlock(block);
        }
        public void ShowPreview(Vector3 point, float radius)
        {
            if (!AssetsLoaded) return;
            if (!preview) preview = Quad("落日震 · 贴图范围预览", ring);
            preview.SetActive(true); preview.transform.position = point + Vector3.up * .04f;
            preview.transform.localScale = Vector3.one * (radius * 2 / .855f);
            SetTint(preview.GetComponent<Renderer>(), new Color(.25f, .9f, 1, .55f), tint);
        }
        public void HidePreview() { if (preview) preview.SetActive(false); }
        public void HideSelection()
        {
            HidePreview(); if (chargeCircle) chargeCircle.SetActive(false);
            if (chargeSparks) chargeSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void UpdateCharge(ProloguePlayer player, float charge)
        {
            if (!AssetsLoaded) return;
            if (!chargeCircle) chargeCircle = Quad("落日震 · 蓄力符文", runes);
            chargeCircle.SetActive(true); chargeCircle.transform.position = player.transform.position + Vector3.up * .035f;
            chargeCircle.transform.rotation = Quaternion.Euler(90, 0, 0) * Quaternion.AngleAxis(Time.time * 35, Vector3.forward);
            chargeCircle.transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 2.8f, charge);
            SetTint(chargeCircle.GetComponent<Renderer>(), new Color(1, 1, 1, Mathf.Lerp(.3f, .8f, charge)), tint);
            if (!player.archer.IsShooting) { if (chargeSparks) chargeSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting); return; }
            if (!chargeSparks)
            {
                chargeSparks = Particles(null, "落日震 · 聚能微光", spark, 0, .25f, .05f, .25f, new Color(.4f, .9f, 1, .7f));
                var main = chargeSparks.main; main.loop = true;
                var emission = chargeSparks.emission; emission.rateOverTime = 18;
                var shape = chargeSparks.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .13f;
            }
            chargeSparks.transform.position = player.archer.leftHand ? player.archer.leftHand.position : player.transform.position + Vector3.up * 1.4f;
            if (!chargeSparks.isPlaying) chargeSparks.Play();
        }
        public void AttachNormalTrail(GameObject arrow)
        {
            if (!AssetsLoaded) return;
            var line = arrow.AddComponent<TrailRenderer>(); line.sharedMaterial = trail;
            line.time = .09f; line.startWidth = .075f; line.endWidth = .005f;
            line.minVertexDistance = .025f; line.textureMode = LineTextureMode.Stretch;
            line.startColor = new Color(.8f, .9f, 1, .5f);
            line.endColor = new Color(.8f, .9f, 1, 0);
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
        }
        public void AttachTrail(GameObject arrow)
        {
            if (!AssetsLoaded) return;
            var line = arrow.AddComponent<TrailRenderer>(); line.sharedMaterial = trail;
            line.time = .22f; line.startWidth = .38f; line.endWidth = .035f; line.minVertexDistance = .035f;
            line.textureMode = LineTextureMode.Stretch;
            line.startColor = Color.white; line.endColor = new Color(.6f, .9f, 1, 0);
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
        }
        ParticleSystem Particles(Transform parent, string name, Material material, int count, float lifetime, float size, float speed, Color color)
        {
            var go = new GameObject(name); if (parent) go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 1;
            main.startLifetime = lifetime; main.startSize = size; main.startSpeed = speed; main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 48;
            var emission = ps.emission; emission.rateOverTime = 0;
            if (count > 0) emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
            var shape = ps.shape; shape.enabled = speed > 0; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 65; shape.radius = .12f;
            go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var fade = ps.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.85f, .3f), new GradientAlphaKey(0, 1) }); fade.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return ps;
        }
        public GameObject PlayImpact(Vector3 point, float radius)
        {
            if (!AssetsLoaded) return null;
            impacts.RemoveAll(go => !go);
            var root = new GameObject("落日震 · 素材组合爆发"); root.transform.position = point + Vector3.up * .08f; impacts.Add(root);
            var expanding = Quad("落日震 · 扩散环", ring); expanding.transform.SetParent(root.transform, false);
            expanding.transform.rotation = Quaternion.Euler(90, 0, 0);
            expanding.AddComponent<SunburstExpansion>().Initialize(radius);
            var burst = Particles(root.transform, "命中闪光", flash, 1, .16f, 4.65f, 0, new Color(1, .95f, .85f, .8f)); burst.Play();
            var flame = Particles(root.transform, "16帧火焰爆发", fire, 1, .55f, 4.5f, 0, new Color(1, 1, 1, .85f));
            flame.transform.localPosition = Vector3.up * .35f;
            var sheet = flame.textureSheetAnimation; sheet.enabled = true; sheet.numTilesX = sheet.numTilesY = 4;
            sheet.animation = ParticleSystemAnimationType.WholeSheet; sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0, 1, 1)); sheet.cycleCount = 1;
            flame.Play();
            var sparks = Particles(root.transform, "飞散火星", spark, 22, .48f, .09f, 5, new Color(1, .5f, .12f, .85f));
            var sparkMain = sparks.main; sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(2, 7); sparkMain.gravityModifier = .65f; sparks.Play();
            var dust = Particles(root.transform, "少量烟尘", smoke, 5, .75f, .8f, .8f, new Color(.6f, .63f, .66f, .22f));
            var dustSize = dust.sizeOverLifetime; dustSize.enabled = true; dustSize.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .5f, 1, 1.6f)); dust.Play();
            Destroy(root, 1.2f); return root;
        }
        void OnDestroy()
        {
            if (preview) Destroy(preview); if (chargeCircle) Destroy(chargeCircle); if (chargeSparks) Destroy(chargeSparks.gameObject);
            foreach (var impact in impacts) if (impact) Destroy(impact);
            foreach (var material in materials) if (material) Destroy(material);
        }
    }
    public sealed class SunburstExpansion : MonoBehaviour
    {
        float radius, age;
        MaterialPropertyBlock tint;
        void Awake() { tint = new MaterialPropertyBlock(); }
        public void Initialize(float size) { radius = size; transform.localScale = Vector3.one * .01f; }
        void Update()
        {
            age += Time.deltaTime;
            transform.localScale = Vector3.one * (radius * 2 / .855f * Mathf.Clamp01(age / .3f));
            SunburstEffects.SetTint(GetComponent<Renderer>(), new Color(.7f, .9f, 1, Mathf.Clamp01(1 - age / .65f) * .8f), tint);
        }
    }
}

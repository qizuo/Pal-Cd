using UnityEngine;

namespace Houyi.NineSuns
{
    // Built at runtime so existing saved scenes and Android builds need no asset rebinding.
    public static class ChapterOneWorld
    {
        public static Material Fire, Warning;
        static Material earth, road, bark, stone, leaf, bronze, linen, water, dark;
        static Material Mat(string name, Color color, bool glow = false)
        {
            var mat = new Material(Shader.Find("Standard")) { name = "Chapter One / " + name, color = color };
            mat.SetFloat("_Glossiness", .08f);
            if (glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 1.4f); }
            return mat;
        }
        public static GameObject Shape(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 size, Material mat, bool collision = true)
        {
            GameObject go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collision) { var c = go.GetComponent<Collider>(); c.enabled = false; Object.Destroy(c); }
            return go;
        }
        static GameObject Box(ChapterOneDirector c, string name, float x, float y, float z, Vector3 size, Material mat, bool collision = true)
            => Shape(c.WorldRoot, PrimitiveType.Cube, name, c.Point(x, z, y), size, mat, collision);
        static void Label(ChapterOneDirector c, string name, Vector3 position, float size = .18f)
        {
            var go = new GameObject(name); go.transform.SetParent(c.WorldRoot); go.transform.position = position;
            var text = go.AddComponent<TextMesh>(); text.text = name; text.anchor = TextAnchor.MiddleCenter;
            text.characterSize = size; text.fontSize = 64; text.color = new Color(1, .81f, .42f);
            if (c.Story.chineseFont) { text.font = c.Story.chineseFont; go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material; }
            var face = go.AddComponent<ChapterOneLabel>(); face.view = c.Story.player.view;
        }
        static Transform Person(ChapterOneDirector c, Transform source, string name, float x, float z)
        {
            var root = new GameObject(name); root.transform.SetParent(c.WorldRoot); root.transform.position = c.Point(x, z);
            Transform visual = source ? source.Find("Visual") : null;
            if (visual) Object.Instantiate(visual.gameObject, root.transform, false);
            else Shape(root.transform, PrimitiveType.Capsule, "布衣村民", root.transform.position + Vector3.up * .85f, new Vector3(.6f, .85f, .6f), linen, false);
            root.transform.rotation = Quaternion.Euler(0, 180, 0);
            Label(c, name, root.transform.position + Vector3.up * 2);
            return root.transform;
        }
        static ChapterOneActor Target(ChapterOneDirector c, string name, ChapterOneActor.Type type, ChapterOneDirector.Stage stage, Vector3 p, Vector3 size, int health)
        {
            var go = Shape(c.WorldRoot, PrimitiveType.Sphere, name, p, size, Fire);
            var a = go.AddComponent<ChapterOneActor>(); a.Initialize(c, type, stage, health); return a;
        }
        static void Wolf(ChapterOneDirector c, float x, float z, ChapterOneDirector.Stage stage)
        {
            var go = new GameObject("赤火灾狼"); go.transform.SetParent(c.WorldRoot); go.transform.position = c.Point(x, z);
            Transform visual = c.Story.beasts != null && c.Story.beasts.Length > 0 && c.Story.beasts[0] ? c.Story.beasts[0].transform.Find("Visual") : null;
            if (visual) Object.Instantiate(visual.gameObject, go.transform, false);
            else
            {
                Shape(go.transform, PrimitiveType.Capsule, "狼身", go.transform.position + Vector3.up * .6f, new Vector3(.8f, .5f, 1.4f), bark, false).transform.rotation = Quaternion.Euler(90, 0, 0);
                Shape(go.transform, PrimitiveType.Sphere, "狼首", go.transform.position + new Vector3(0, .8f, .7f), new Vector3(.6f, .6f, .7f), dark, false);
            }
            Shape(go.transform, PrimitiveType.Sphere, "侵蚀火核", go.transform.position + Vector3.up * .8f, Vector3.one * .28f, Fire, false);
            var box = go.AddComponent<BoxCollider>(); box.center = Vector3.up * .6f; box.size = new Vector3(1.1f, 1.3f, 1.8f);
            var actor = go.AddComponent<ChapterOneActor>(); actor.Initialize(c, ChapterOneActor.Type.Wolf, stage, 4);
        }
        static void Crow(ChapterOneDirector c, float x, float z)
        {
            var go = new GameObject("赤鸦"); go.transform.SetParent(c.WorldRoot); go.transform.position = c.Point(x, z, 3.5f);
            Shape(go.transform, PrimitiveType.Sphere, "赤鸦身躯", go.transform.position, new Vector3(.75f, .5f, 1.1f), dark, false);
            foreach (int sign in new[] { -1, 1 })
            {
                var wing = Shape(go.transform, PrimitiveType.Cube, "焦羽", go.transform.position + Vector3.right * sign * .8f, new Vector3(1.3f, .1f, .7f), bark, false);
                wing.transform.rotation = Quaternion.Euler(0, sign * 18, sign * 12);
            }
            Shape(go.transform, PrimitiveType.Sphere, "赤鸦火眼", go.transform.position + Vector3.forward * .48f, Vector3.one * .3f, Fire, false);
            var hitbox = go.AddComponent<BoxCollider>(); hitbox.size = new Vector3(2.2f, .9f, 1.2f);
            go.AddComponent<ChapterOneActor>().Initialize(c, ChapterOneActor.Type.Crow, ChapterOneDirector.Stage.Ambush, 2);
        }
        public static void Build(ChapterOneDirector c)
        {
            earth = Mat("焦土", new Color(.23f, .20f, .14f)); road = Mat("灰烬山道", new Color(.46f, .36f, .23f));
            bark = Mat("焦木", new Color(.16f, .10f, .065f)); stone = Mat("山岩", new Color(.24f, .27f, .25f));
            leaf = Mat("残叶", new Color(.35f, .25f, .10f)); bronze = Mat("古铜", new Color(.53f, .35f, .13f));
            linen = Mat("布衣", new Color(.64f, .60f, .43f)); water = Mat("清泉", new Color(.13f, .55f, .61f));
            dark = Mat("炭灰", new Color(.055f, .055f, .05f)); Fire = Mat("赤日之火", new Color(1, .16f, .025f), true);
            Warning = Mat("危险预警", new Color(.8f, .16f, .045f), true);
            Box(c, "山地", 0, -.3f, 104, new Vector3(70, .6f, 240), earth);
            Box(c, "主山道", 0, .012f, 85, new Vector3(8, .02f, 190), road, false);
            Box(c, "药草支路", -8, .018f, 48, new Vector3(15, .025f, 3.2f), road, false);
            Box(c, "古碑支路", 8, .018f, 118, new Vector3(15, .025f, 3.2f), road, false);
            var random = new System.Random(71103);
            for (int i = 0; i < 88; i++)
            {
                float z = -7 + i * 2.5f, sign = i % 2 == 0 ? -1 : 1;
                float x = sign * (10 + (float)random.NextDouble() * 16);
                if ((z > 43 && z < 54 && sign < 0) || (z > 112 && z < 124 && sign > 0) || z > 156) continue;
                float height = 3 + (float)random.NextDouble() * 4;
                var trunk = Shape(c.WorldRoot, PrimitiveType.Cylinder, "焦林枯树", c.Point(x, z, height * .5f), new Vector3(.5f, height * .5f, .5f), bark);
                foreach (int branchSign in new[] { -1, 1 })
                {
                    var branch = Shape(c.WorldRoot, PrimitiveType.Cylinder, "断枝", c.Point(x + branchSign * .6f, z, height * .75f), new Vector3(.19f, height * .25f, .19f), bark, false);
                    branch.transform.rotation = Quaternion.Euler(0, 0, branchSign * 42);
                }
                if (i % 3 == 0) Shape(trunk.transform, PrimitiveType.Sphere, "焦黄残冠", c.Point(x, z, height), new Vector3(2.5f, .65f, 2), leaf, false);
            }
            for (int i = 0; i < 30; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * (27 + (float)random.NextDouble() * 10);
                Shape(c.WorldRoot, PrimitiveType.Sphere, "山脊岩壁", c.Point(x, -6 + i * 8, 5), new Vector3(14, 15 + (float)random.NextDouble() * 12, 19), stone);
            }
            Box(c, "左边界", -34, 4, 100, new Vector3(2, 10, 244), stone);
            Box(c, "右边界", 34, 4, 100, new Vector3(2, 10, 244), stone);
            Box(c, "山口后壁", 0, 3, -14, new Vector3(70, 6, 2), stone);
            Box(c, "山顶后壁", 0, 4, 223, new Vector3(70, 8, 2), stone);
            c.Elder = Person(c, c.Story.elder, "村老", 2, 5);
            c.Captive = Person(c, c.Story.survivor, "运水队长", 2, 108);
            Label(c, "追 火 入 山", c.Point(0, 14, 4), .35f);
            foreach (float x in new[] { -4.5f, 4.5f }) Box(c, "山口石柱", x, 2, 14, new Vector3(.8f, 4, .8f), stone);
            Box(c, "山口横梁", 0, 4.2f, 14, new Vector3(10, .65f, 1), bark);
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = c.Point(i == 1 ? -2 : 2, 27 + i * 16);
                var clue = Shape(c.WorldRoot, i == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube, "运水队踪迹 " + (i + 1), p + Vector3.up * .2f, i == 0 ? new Vector3(.7f, .2f, .7f) : new Vector3(.2f, .15f, 1.3f), i == 2 ? dark : bronze, false);
                c.Clues.Add(clue.transform); Label(c, i == 0 ? "破损水桶" : i == 1 ? "折断的箭" : "焦黑脚印", p + Vector3.up * 1.1f, .12f);
            }
            c.Supply = Box(c, "药草补给", -13, .45f, 48, new Vector3(1.4f, .9f, 1.2f), linen).transform;
            Label(c, "药草补给", c.Supply.position + Vector3.up * 1.2f, .13f);
            c.Lore = Box(c, "山中古碑", 13, 1.2f, 118, new Vector3(1.8f, 2.4f, .55f), stone).transform;
            Label(c, "赤日古碑", c.Lore.position + Vector3.up * 1.6f, .13f);
            Wolf(c, -3, 77, ChapterOneDirector.Stage.Ambush); Wolf(c, 3, 82, ChapterOneDirector.Stage.Ambush); Wolf(c, 0, 88, ChapterOneDirector.Stage.Ambush);
            Crow(c, -4, 83); Crow(c, 4, 89);
            Wolf(c, -4, 105, ChapterOneDirector.Stage.Rescue); Wolf(c, 4, 114, ChapterOneDirector.Stage.Rescue);
            c.Cage = new GameObject("悬挂木笼"); c.Cage.transform.SetParent(c.WorldRoot);
            for (int i = 0; i < 5; i++) foreach (float side in new[] { -1.3f, 1.3f })
            {
                Shape(c.Cage.transform, PrimitiveType.Cube, "木笼竖栏", c.Point(2 + side, 108 - 1.2f + i * .6f, 1.4f), new Vector3(.12f, 2.8f, .12f), bark);
                Shape(c.Cage.transform, PrimitiveType.Cube, "木笼竖栏", c.Point(.8f + i * .6f, 108 + side, 1.4f), new Vector3(.12f, 2.8f, .12f), bark);
            }
            Shape(c.Cage.transform, PrimitiveType.Cube, "木笼顶", c.Point(2, 108, 2.8f), new Vector3(2.8f, .15f, 2.8f), bark);
            Box(c, "吊笼横梁", 2, 4.5f, 108, new Vector3(6, .3f, .3f), bark);
            foreach (float x in new[] { -1f, 5f }) Box(c, "吊笼支柱", x, 2.25f, 108, new Vector3(.3f, 4.5f, .3f), bark);
            Target(c, "吊笼绳结", ChapterOneActor.Type.Rope, ChapterOneDirector.Stage.Rescue, c.Point(2, 108, 3.6f), Vector3.one * .85f, 1);
            Label(c, "射断发光绳结", c.Point(2, 108, 5), .15f);
            c.Gate = Box(c, "赤日封锁山门", 0, 2.4f, 145, new Vector3(65, 4.8f, .8f), dark);
            foreach (int i in new[] { -1, 0, 1 })
            {
                Box(c, "火核石座", i * 6, 1, 138, new Vector3(1.7f, 2, 1.7f), stone);
                Target(c, "赤日火核", ChapterOneActor.Type.Core, ChapterOneDirector.Stage.Gate, c.Point(i * 6, 138, i == 0 ? 3.8f : 2.8f), Vector3.one * 1.2f, 2);
            }
            Label(c, "三 核 镇 山 门", c.Point(0, 145, 5.5f), .25f);
            c.Shrine = Box(c, "山泉祭坛", 0, .05f, 182, new Vector3(34, .1f, 48), stone, false).transform;
            foreach (int sign in new[] { -1, 1 }) Box(c, "首领岩壁", sign * 19, 2.5f, 184, new Vector3(2, 5, 52), stone);
            Box(c, "首领后岩壁", 0, 2.5f, 210, new Vector3(40, 5, 2), stone);
            foreach (int sign in new[] { -1, 1 }) Box(c, "首领入口石壁", sign * 12, 2.5f, 158, new Vector3(14, 5, 2), stone);
            var boss = new GameObject("焚角山魈"); boss.transform.SetParent(c.WorldRoot); boss.transform.position = c.Point(0, 190);
            Shape(boss.transform, PrimitiveType.Capsule, "岩甲躯干", boss.transform.position + Vector3.up * 1.7f, new Vector3(2.3f, 1.5f, 1.4f), dark, false);
            Shape(boss.transform, PrimitiveType.Sphere, "山魈头颅", boss.transform.position + new Vector3(0, 2.9f, .35f), Vector3.one * 1.4f, stone, false);
            foreach (int sign in new[] { -1, 1 })
            {
                Shape(boss.transform, PrimitiveType.Capsule, "巨臂", boss.transform.position + new Vector3(sign * 1.6f, 1.25f, .1f), new Vector3(.85f, 1.3f, .9f), stone, false);
                Shape(boss.transform, PrimitiveType.Capsule, "粗腿", boss.transform.position + new Vector3(sign * .65f, .7f, 0), new Vector3(.75f, .7f, .85f), dark, false);
                var horn = Shape(boss.transform, PrimitiveType.Cylinder, "焚角", boss.transform.position + new Vector3(sign * .65f, 3.6f, .4f), new Vector3(.25f, .65f, .25f), bronze, false);
                horn.transform.rotation = Quaternion.Euler(25, 0, -sign * 25);
                Shape(boss.transform, PrimitiveType.Sphere, "炽目", boss.transform.position + new Vector3(sign * .3f, 3, .95f), Vector3.one * .2f, Fire, false);
            }
            var bossHitbox = boss.AddComponent<BoxCollider>(); bossHitbox.center = Vector3.up * 1.6f; bossHitbox.size = new Vector3(2.8f, 3.2f, 1.8f);
            c.Boss = boss.AddComponent<ChapterOneActor>(); c.Boss.Initialize(c, ChapterOneActor.Type.Boss, ChapterOneDirector.Stage.Boss, 24);
            c.WeakPoint = Target(c, "角上火核", ChapterOneActor.Type.WeakPoint, ChapterOneDirector.Stage.Boss, boss.transform.position + Vector3.up * 3.35f, Vector3.one * 1.25f, 1);
            c.WeakPoint.gameObject.SetActive(false);
            c.Spring = new GameObject("山泉泉眼").transform; c.Spring.SetParent(c.WorldRoot); c.Spring.position = c.Point(0, 201);
            Shape(c.WorldRoot, PrimitiveType.Cylinder, "泉眼石池", c.Spring.position + Vector3.up * .08f, new Vector3(8, .08f, 8), dark, false);
            c.Water = Shape(c.WorldRoot, PrimitiveType.Cylinder, "复流清泉", c.Spring.position + Vector3.up * .14f, new Vector3(7.5f, .025f, 7.5f), water, false); c.Water.SetActive(false);
            c.Corruption = Shape(c.WorldRoot, PrimitiveType.Sphere, "侵蚀泉眼的赤火", c.Spring.position + Vector3.up * .8f, new Vector3(2, 1.5f, 2), Fire, false);
            Shape(c.WorldRoot, PrimitiveType.Cube, "泉底赤日碎片", c.Spring.position + new Vector3(.5f, .35f, -.8f), Vector3.one * .4f, bronze, false).transform.rotation = Quaternion.Euler(20, 40, 30);
            Label(c, "山 泉 祭 坛", c.Point(0, 208, 6), .3f);
        }
    }
    public sealed class ChapterOneLabel : MonoBehaviour
    {
        public Camera view;
        void LateUpdate() { if (view) transform.rotation = view.transform.rotation; }
    }
}

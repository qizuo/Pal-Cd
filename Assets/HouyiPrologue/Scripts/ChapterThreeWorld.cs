using UnityEngine;

namespace Houyi.NineSuns
{
    public static class ChapterThreeWorld
    {
        public static Material Fire;
        static Material stone, ground, wood, jade, blue, gold;
        static Material Mat(string name, Color color)
        {
            var m = new Material(Shader.Find("Standard")) { name = "Chapter Three / " + name, color = color }; m.SetFloat("_Glossiness", .15f); return m;
        }
        public static GameObject Shape(ChapterThreeDirector c, PrimitiveType type, string name, Vector3 p, Vector3 scale, Material material, bool collision = true)
        {
            var shape = ChapterOneWorld.Shape(c.WorldRoot, type, name, p, scale, material, collision);
            if (collision && type == PrimitiveType.Cylinder)
            {
                // Unity's cylinder primitive uses a capsule. Nonuniform scale makes a
                // wide, flat sundial collide like a tall sphere above the visible disk.
                var capsule = shape.GetComponent<Collider>(); capsule.enabled = false; Object.Destroy(capsule);
                shape.AddComponent<MeshCollider>().sharedMesh = shape.GetComponent<MeshFilter>().sharedMesh;
            }
            return shape;
        }
        static GameObject Box(ChapterThreeDirector c, string name, float x, float z, float y, Vector3 size, Material m, bool collision = true)
            => Shape(c, PrimitiveType.Cube, name, c.Point(x, z, y), size, m, collision);
        static Transform Label(ChapterThreeDirector c, string text, Vector3 p)
        {
            var g = new GameObject(text); g.transform.SetParent(c.WorldRoot); g.transform.position = p;
            var t = g.AddComponent<TextMesh>(); t.text = text; t.anchor = TextAnchor.MiddleCenter; t.fontSize = 64; t.characterSize = .14f; t.color = new Color(1, .83f, .5f);
            if (c.Story.chineseFont) { t.font = c.Story.chineseFont; g.GetComponent<Renderer>().sharedMaterial = t.font.material; }
            g.AddComponent<ChapterOneLabel>().view = c.Story.player.view; return g.transform;
        }
        public static ChapterThreeActor SpawnActor(ChapterThreeDirector c, ChapterThreeActor.Kind kind, ChapterThreeDirector.Stage stage, Vector3 p, bool register = true)
        {
            string name = kind == ChapterThreeActor.Kind.Boss ? "扶桑炎卫" : kind == ChapterThreeActor.Kind.Archer ? "高台射手" : kind == ChapterThreeActor.Kind.Guard ? "遗址守卫" : kind == ChapterThreeActor.Kind.Rope ? "断桥悬索" : kind == ChapterThreeActor.Kind.Core ? "日晷背面火核" : kind == ChapterThreeActor.Kind.Chain ? "赤日锁链" : "可破坏树根";
            var g = new GameObject(name); g.transform.SetParent(c.WorldRoot); g.transform.position = p;
            bool boss = kind == ChapterThreeActor.Kind.Boss, enemy = boss || kind == ChapterThreeActor.Kind.Guard || kind == ChapterThreeActor.Kind.Archer;
            float size = boss ? 2.3f : 1;
            var collider = g.AddComponent<BoxCollider>();
            collider.center = enemy ? Vector3.up * size : kind == ChapterThreeActor.Kind.Root ? Vector3.up * 1.3f : Vector3.zero;
            collider.size = enemy ? new Vector3(size * 1.3f, size * 2.3f, size) : kind == ChapterThreeActor.Kind.Root ? new Vector3(1.5f, 2.6f, 1.5f) : Vector3.one * (kind == ChapterThreeActor.Kind.Core ? 1.5f : 1.1f);
            var visual = Shape(c, enemy ? PrimitiveType.Capsule : kind == ChapterThreeActor.Kind.Root ? PrimitiveType.Cube : PrimitiveType.Sphere, name, p + collider.center, collider.size, enemy ? jade : kind == ChapterThreeActor.Kind.Root ? wood : Fire, false);
            visual.transform.SetParent(g.transform, true);
            if (enemy)
            {
                Shape(c, PrimitiveType.Sphere, "石面", p + Vector3.up * size * 2, Vector3.one * size * .65f, stone, false).transform.SetParent(g.transform, true);
                for (int i = -1; i <= 1; i += 2)
                {
                    var arm = Shape(c, PrimitiveType.Capsule, "扶桑根臂", p + new Vector3(i * size * .65f, size, 0), new Vector3(size * .4f, size, size * .4f), wood, false); arm.transform.SetParent(g.transform, true);
                    if (boss) { var crown = Shape(c, PrimitiveType.Cylinder, "枝冠", p + new Vector3(i * .8f, 5.2f, 0), new Vector3(.3f, 1.1f, .3f), gold, false); crown.transform.rotation = Quaternion.Euler(0, 0, i * 25); crown.transform.SetParent(g.transform, true); }
                }
            }
            var actor = g.AddComponent<ChapterThreeActor>();
            actor.Initialize(c, kind, stage, boss ? 45 : kind == ChapterThreeActor.Kind.Guard ? 5 : kind == ChapterThreeActor.Kind.Archer ? 4 : 1, register);
            return actor;
        }
        public static void CagePreview(ChapterThreeDirector c, Vector3 center, float duration)
        {
            center.y = .07f;
            var preview = Shape(c, PrimitiveType.Cylinder, "树根出现预警", center, new Vector3(6, .015f, 6), Fire, false);
            Object.Destroy(preview, duration);
        }
        public static void Build(ChapterThreeDirector c)
        {
            stone = Mat("古石", new Color(.42f, .41f, .34f)); ground = Mat("砂岩地砖", new Color(.58f, .48f, .32f)); wood = Mat("扶桑古根", new Color(.24f, .14f, .07f));
            jade = Mat("青铜石甲", new Color(.19f, .32f, .29f)); blue = Mat("安全地标", new Color(.12f, .7f, .85f)); gold = Mat("古金", new Color(.85f, .57f, .18f)); Fire = Mat("赤日", new Color(1, .23f, .035f));
            Fire.EnableKeyword("_EMISSION"); Fire.SetColor("_EmissionColor", new Color(.8f, .1f, .01f));
            c.WorldRoot.gameObject.AddComponent<ChapterOneMaterials>().owned = new[] { stone, ground, wood, jade, blue, gold, Fire };
            Box(c, "入口平台", 0, 13, -.4f, new Vector3(56, .8f, 58), ground);
            Box(c, "遗址平台", 0, 130, -.4f, new Vector3(56, .8f, 144), ground);
            Box(c, "断桥下谷底", 0, 50, -9, new Vector3(56, 1, 18), stone);
            c.Bridge = Box(c, "悬索木桥", 0, 50, -.15f, new Vector3(8, .3f, 20), wood); c.Bridge.SetActive(false);
            for (int i = 0; i < 20; i++)
                Box(c, "木桥横板", 0, 40.5f + i, .035f, new Vector3(8, .08f, .85f), stone, false).transform.SetParent(c.Bridge.transform, true);
            foreach (int side in new[] { -1, 1 })
            {
                Box(c, "桥栏扶手", side * 3.7f, 50, 1.2f, new Vector3(.15f, .15f, 20), wood).transform.SetParent(c.Bridge.transform, true);
                for (int i = 0; i < 6; i++) Box(c, "桥栏立柱", side * 3.7f, 41 + i * 3.5f, .65f, new Vector3(.2f, 1.3f, .2f), wood).transform.SetParent(c.Bridge.transform, true);
            }
            c.BridgeGate = Box(c, "断桥前护栏", 0, 40, 1.5f, new Vector3(54, 3, .6f), wood);
            Box(c, "左侧岩壁", -28, 93, 5, new Vector3(2, 18, 220), stone); Box(c, "右侧岩壁", 28, 93, 5, new Vector3(2, 18, 220), stone);
            Box(c, "入口后墙", 0, -15, 3, new Vector3(56, 7, 2), stone); Box(c, "尽头崖壁", 0, 202, 5, new Vector3(56, 12, 2), stone);
            for (int i = 0; i < 28; i++)
            {
                float z = 2 + i * 7;
                if (z > 38 && z < 61) continue;
                Box(c, "古道石砖", 0, z, .025f, new Vector3(6, .04f, 5.7f), stone, false);
                foreach (int side in new[] { -1, 1 }) Box(c, "古道金纹", side * 3.3f, z, .05f, new Vector3(.12f, .02f, 5.7f), gold, false);
            }
            foreach (float z in new[] { 5f, 63f, 116f, 151f })
            {
                foreach (int side in new[] { -1, 1 })
                {
                    Box(c, "遗址门柱", side * 6, z, 3, new Vector3(1.5f, 6, 1.5f), jade);
                    Box(c, "门柱基座", side * 6, z, .3f, new Vector3(2.5f, .6f, 2.5f), stone);
                }
                Box(c, "断裂门楣", 0, z, 6.1f, new Vector3(14, .8f, 1.8f), stone);
                Box(c, "门楣日纹", 0, z - 1, 6.1f, new Vector3(3, .55f, .08f), gold, false);
            }
            for (int i = 0; i < 12; i++)
            {
                float z = 6 + i * 16;
                foreach (int side in new[] { -1, 1 })
                {
                    var column = Shape(c, PrimitiveType.Cylinder, "残柱", c.Point(side * 23, z, 2), new Vector3(1.8f, 2, 1.8f), stone);
                    if (i % 3 == 0) column.transform.rotation = Quaternion.Euler(0, 0, side * 15);
                    var root = Box(c, "盘绕古根", side * 25, z, 1, new Vector3(1.5f, 2, 9), wood); root.transform.rotation = Quaternion.Euler(10, side * 20, side * 12);
                }
            }
            c.Inscription = Label(c, "扶桑断碑 · 调查", c.Point(0, 17, 1.7f)); Box(c, "断碑", 0, 18, .8f, new Vector3(2.5f, 1.6f, .6f), jade);
            SpawnActor(c, ChapterThreeActor.Kind.Guard, ChapterThreeDirector.Stage.Entry, c.Point(-5, 14));
            SpawnActor(c, ChapterThreeActor.Kind.Guard, ChapterThreeDirector.Stage.Entry, c.Point(5, 23));
            foreach (int side in new[] { -1, 1 })
            {
                Box(c, "悬索架", side * 5, 36, 2.2f, new Vector3(.4f, 4.4f, .4f), wood);
                SpawnActor(c, ChapterThreeActor.Kind.Rope, ChapterThreeDirector.Stage.Bridge, c.Point(side * 5, 36, 3));
                Label(c, "射断悬索", c.Point(side * 5, 36, 4));
            }
            for (int i = 0; i < 3; i++)
            {
                float x = i == 1 ? 12 : -12, z = 73 + i * 14;
                Shape(c, PrimitiveType.Cylinder, "日晷台座", c.Point(x, z, .45f), new Vector3(6, .45f, 6), stone);
                Shape(c, PrimitiveType.Cylinder, "古金晷面", c.Point(x, z, .92f), new Vector3(5.5f, .025f, 5.5f), gold, false);
                for (int mark = 0; mark < 12; mark++)
                {
                    float angle = mark * Mathf.PI / 6;
                    var tick = Box(c, "日晷刻度", x + Mathf.Sin(angle) * 2.2f, z + Mathf.Cos(angle) * 2.2f, .96f, new Vector3(.12f, .025f, .5f), jade, false);
                    tick.transform.rotation = Quaternion.Euler(0, mark * 30, 0);
                }
                Box(c, "日晷正面石甲", x, z - .8f, 1.8f, new Vector3(3, 2.7f, .5f), jade);
                var core = SpawnActor(c, ChapterThreeActor.Kind.Core, ChapterThreeDirector.Stage.Sundials, c.Point(x, z + 2.2f, 2.2f));
                core.FiringZone = Shape(c, PrimitiveType.Cylinder, "蓝色射击区域", c.Point(x, z + 7, .035f), new Vector3(4, .015f, 4), blue, false).transform;
                Label(c, "日晷 " + (i + 1) + " · 绕后蓄力", c.Point(x, z, 4));
                SpawnActor(c, ChapterThreeActor.Kind.Guard, ChapterThreeDirector.Stage.Sundials, c.Point(-x * .4f, z));
            }
            c.Seal = Box(c, "日晷封印", 0, 112, 2, new Vector3(54, 4, .7f), Fire);
            foreach (int side in new[] { -1, 1 })
            {
                Box(c, "射手高台", side * 14, 133, 1, new Vector3(4, 2, 4), stone);
                SpawnActor(c, ChapterThreeActor.Kind.Archer, ChapterThreeDirector.Stage.Courtyard, c.Point(side * 14, 133, 2));
                SpawnActor(c, ChapterThreeActor.Kind.Guard, ChapterThreeDirector.Stage.Courtyard, c.Point(side * 6, 128));
            }
            c.Supply = Label(c, "内庭补给", c.Point(19, 143, 1.5f)); Box(c, "补给箱", 19, 143, .4f, new Vector3(1.2f, .8f, 1), wood, false);
            c.ArenaGate = Box(c, "祭场封门", 0, 150, 2, new Vector3(54, 4, .6f), jade); c.ArenaGate.SetActive(false);
            Box(c, "炎卫祭场", 0, 175, .015f, new Vector3(48, .03f, 48), jade, false);
            foreach (int side in new[] { -1, 1 }) foreach (float z in new[] { 161f, 184f })
            {
                c.Pillars.Add(Shape(c, PrimitiveType.Cylinder, "冲击波掩体石柱", c.Point(side * 9, z, 3), new Vector3(3, 3, 3), stone).transform);
                Box(c, "掩体柱冠", side * 9, z, 6, new Vector3(3.5f, .5f, 3.5f), gold, false);
            }
            c.Boss = SpawnActor(c, ChapterThreeActor.Kind.Boss, ChapterThreeDirector.Stage.Boss, c.Point(0, 175));
            c.Altar = Label(c, "赤日祭坛 · 调查", c.Point(0, 195, 1.5f)); Box(c, "赤日祭台", 0, 197, .3f, new Vector3(7, .6f, 4), stone);
            var chain = SpawnActor(c, ChapterThreeActor.Kind.Chain, ChapterThreeDirector.Stage.Revelation, c.Point(0, 194, 3));
            for (int i = 0; i < 5; i++) Shape(c, PrimitiveType.Cube, "赤日链环", c.Point(0, 194, 3.9f + i * .55f), new Vector3(.3f, .4f, .3f), gold, false).transform.SetParent(chain.transform, true);
            Label(c, "扶 桑 遗 址", c.Point(0, 5, 5)); Label(c, "三 晷 封 日", c.Point(0, 65, 5)); Label(c, "扶 桑 炎 卫", c.Point(0, 155, 6));
        }
    }
}

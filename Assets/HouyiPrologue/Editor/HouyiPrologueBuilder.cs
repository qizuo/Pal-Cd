using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Houyi.NineSuns;
using Wuxia.Archery;

public static class HouyiPrologueBuilder
{
    public const string ScenePath = "Assets/HouyiPrologue/Scenes/Houyi_Prologue.unity";
    const string Root = "Assets/HouyiPrologue";
    static Material sand, clay, wood, darkWood, roof, rock, gold, ember, white, black;
    static Transform scenery;

    [MenuItem("Tools/Houyi/Open Prologue")]
    public static void Open()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Tools/Houyi/Build Prologue Scene")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Scenes"); Directory.CreateDirectory(Root + "/Art");
        AssetDatabase.Refresh();
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            sand = Mat("Sunburnt sand", new Color(.51f, .36f, .20f));
            clay = Mat("Ochre plaster", new Color(.58f, .39f, .23f));
            wood = Mat("Dry timber", new Color(.28f, .17f, .09f));
            darkWood = Mat("Charred timber", new Color(.11f, .09f, .075f));
            roof = Mat("Slate roofs", new Color(.18f, .22f, .22f));
            rock = Mat("Weathered stone", new Color(.35f, .34f, .28f));
            gold = Mat("Ritual bronze", new Color(.78f, .48f, .14f));
            ember = Mat("Disaster fire", new Color(1f, .18f, .025f), true);
            white = Mat("Linen", new Color(.87f, .75f, .52f));
            black = Mat("Well darkness", new Color(.045f, .04f, .025f));
            scenery = new GameObject("01 · 枯桑村 / Environment").transform;
            var story = new GameObject("00 · Prologue Director").AddComponent<PrologueDirector>();
            story.chineseFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/MSYH.TTF");

            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ArcheryDemo/BlueRobeArcher.prefab");
            if (!source) throw new InvalidOperationException("Build the BlueRobeArcher prefab first.");
            var hero = (GameObject)PrefabUtility.InstantiatePrefab(source);
            hero.name = "后羿 · 蓝衣替身"; hero.transform.position = new Vector3(0, .05f, -15);
            foreach (var t in hero.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
            var archer = hero.GetComponent<ArcherDemo>();
            archer.autoDemo = false; archer.acceptDemoInput = false; archer.showDemoHUD = false; archer.useWorldCollision = true;
            var motor = hero.AddComponent<UnityEngine.CharacterController>();
            motor.height = 1.8f; motor.radius = .28f; motor.center = new Vector3(0, .9f, 0); motor.stepOffset = .35f;
            var player = hero.AddComponent<ProloguePlayer>(); player.archer = archer; player.story = story; player.pitch = 8;
            var rig = hero.GetComponentsInChildren<Transform>().First(t => t.name == "murongziying");
            var locomotion = rig.gameObject.AddComponent<Animation>();
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath("Assets/Standard Assets/Character Controllers/ZY.FBX").OfType<AnimationClip>())
            {
                if (clip.name.Contains("idle")) { locomotion.AddClip(clip, "Idle"); locomotion.clip = clip; }
                if (clip.name.Contains("run")) locomotion.AddClip(clip, "Run");
            }
            locomotion.wrapMode = WrapMode.Loop; locomotion.playAutomatically = true; player.locomotion = locomotion;
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.fieldOfView = 56; camera.nearClipPlane = .08f; camera.farClipPlane = 260;
            camera.transform.position = new Vector3(.7f, 2.3f, -19);
            camera.transform.rotation = Quaternion.Euler(8, 0, 0);
            camera.gameObject.AddComponent<AudioListener>(); player.view = camera; story.player = player;

            BuildVillage(story);
            story.elder = Character("村老", "Assets/Modles/Npc/LaoTou.FBX", new Vector3(-2.5f, 0, -4), 150, 1.65f);
            story.survivor = Character("受困村民", "Assets/Modles/Npc/ShuSheng.FBX", new Vector3(5, 0, 22), -100, 1.7f);
            Label("村老", story.elder.position + Vector3.up * 2.05f, story.chineseFont);
            Label("受困村民", story.survivor.position + Vector3.up * 2.05f, story.chineseFont);

            var targetList = new List<PrologueTarget>();
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = new Vector3(-3 + i * 3, 0, 6 + (i == 1 ? 1 : 0));
                var target = new GameObject("草靶 " + (i + 1)); target.transform.position = p;
                Box("Target stand", p + new Vector3(0, .65f, .1f), new Vector3(.12f, 1.3f, .12f), wood, target.transform, false);
                for (int j = 0; j < 3; j++)
                {
                    float size = 1.25f - j * .34f;
                    var disc = Primitive(PrimitiveType.Cylinder, "Straw target ring", p + new Vector3(0, 1.55f, -j * .04f), new Vector3(size, .045f, size), j == 1 ? clay : j == 2 ? ember : white, target.transform);
                    disc.transform.rotation = Quaternion.Euler(90, 0, 0);
                }
                var t = target.AddComponent<PrologueTarget>(); t.kind = PrologueTarget.Kind.Training; t.story = story;
                targetList.Add(t);
            }
            story.targets = targetList.ToArray();
            var beasts = new List<PrologueTarget>();
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = new Vector3(i == 0 ? -3 : i == 1 ? 3 : 0, 0, 19 + i * 4);
                var go = Character("炎瘴灾兽 " + (i + 1), "Assets/Modles/Enemy/fox/fox.FBX", p, 180, .95f).gameObject;
                var hitbox = go.AddComponent<BoxCollider>(); hitbox.center = new Vector3(0, .55f, 0); hitbox.size = new Vector3(1.2f, 1.1f, 1.5f);
                var beast = go.AddComponent<PrologueTarget>(); beast.kind = PrologueTarget.Kind.Beast; beast.story = story; beast.health = 2;
                Primitive(PrimitiveType.Sphere, "Ember heart", p + new Vector3(0, .8f, 0), Vector3.one * .3f, ember, go.transform, false);
                var glow = new GameObject("Disaster glow").AddComponent<Light>(); glow.transform.SetParent(go.transform, false); glow.transform.localPosition = Vector3.up;
                glow.type = LightType.Point; glow.color = new Color(1, .15f, .02f); glow.intensity = 1.5f; glow.range = 2;
                beasts.Add(beast);
            }
            story.beasts = beasts.ToArray();
            var altar = new GameObject("祭坛交互点"); altar.transform.position = new Vector3(0, 0, 38); story.altar = altar.transform;
            var sealRoot = new GameObject("赤日封印 · Full charge required"); sealRoot.transform.position = new Vector3(0, 3.0f, 42);
            var sealSurface = Mat("Seal fire surface", new Color(1, .7f, .4f)); sealSurface.shader = Shader.Find("Houyi/SunFire");
            Primitive(PrimitiveType.Sphere, "Sun seal core", sealRoot.transform.position, Vector3.one * 1.35f, sealSurface, sealRoot.transform);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                var shard = Box("Orbiting bronze seal", sealRoot.transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 1.05f, new Vector3(.22f, .5f, .22f), gold, sealRoot.transform, false);
                shard.transform.rotation = Quaternion.Euler(0, 0, i * 45);
            }
            var seal = sealRoot.AddComponent<PrologueTarget>(); seal.kind = PrologueTarget.Kind.Seal; seal.story = story; story.seal = seal;
            sealRoot.SetActive(false);
            BuildSky(story);

            var audio = new GameObject("Village ambience").AddComponent<AudioSource>();
            audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Music/music2.mp3"); audio.loop = true; audio.volume = .16f; audio.playOnAwake = true;
            EditorSceneManager.SaveScene(scene, ScenePath);
            PrefabUtility.SaveAsPrefabAsset(hero, Root + "/Houyi_Player.prefab");
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/HouyiPrologueBuild.result", "SUCCESS " + ScenePath);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
        }
    }

    static void BuildVillage(PrologueDirector story)
    {
        Box("Drought ground", new Vector3(0, -.25f, 20), new Vector3(62, .5f, 98), sand);
        var path = Mat("Dust road", new Color(.64f, .48f, .28f));
        Box("Village road", new Vector3(0, .006f, 12), new Vector3(5, .015f, 65), path, null, false);
        // A continuous readable route: village -> archery yard -> dry well -> altar.
        House(new Vector3(-8, 0, -7), 0, false); House(new Vector3(8, 0, -2), 180, false);
        House(new Vector3(-9, 0, 8), 10, true); House(new Vector3(10, 0, 22), 180, true);
        House(new Vector3(-10, 0, 26), 0, false);
        Gate(new Vector3(0, 0, -12), "枯桑村", story.chineseFont);
        Gate(new Vector3(0, 0, 33), "日 陨 祭 坛", story.chineseFont);
        Label("试弓场", new Vector3(0, 2.8f, 8), story.chineseFont);
        // A dry well with individually placed stones and an empty dark interior.
        Primitive(PrimitiveType.Cylinder, "Dry well bottom", new Vector3(4.8f, .03f, 20), new Vector3(2.4f, .025f, 2.4f), black);
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6;
            var stone = Box("Dry well stone", new Vector3(4.8f + Mathf.Cos(a) * 1.15f, .45f, 20 + Mathf.Sin(a) * 1.15f), new Vector3(.7f, .9f, .5f), rock);
            stone.transform.rotation = Quaternion.Euler(0, -i * 30, 0);
        }
        // Broken irrigation channels and field furrows communicate the drought.
        for (int i = 0; i < 7; i++) Box("Parched field furrow", new Vector3(-17 + i * 1.1f, .015f, 16), new Vector3(.09f, .025f, 10), darkWood, null, false);
        UnityEngine.Random.InitState(90210);
        for (int i = 0; i < 100; i++)
        {
            float x = UnityEngine.Random.Range(-26f, 26f), z = UnityEngine.Random.Range(-23f, 62f);
            if (Mathf.Abs(x) < 4) continue;
            var stone = Primitive(PrimitiveType.Sphere, "Scattered sunbleached rock", new Vector3(x, .08f, z), new Vector3(UnityEngine.Random.Range(.2f, .8f), .16f, UnityEngine.Random.Range(.2f, .7f)), rock, null, false);
            stone.transform.rotation = UnityEngine.Random.rotation;
        }
        for (int i = 0; i < 18; i++) DeadTree(new Vector3((i % 2 == 0 ? -1 : 1) * UnityEngine.Random.Range(14f, 23f), 0, -17 + i * 4.1f));
        // Fractured dark veins in the sandy route.
        for (int i = 0; i < 35; i++)
        {
            Vector3 p = new Vector3(UnityEngine.Random.Range(-2.4f, 2.4f), .022f, UnityEngine.Random.Range(-20, 43));
            var crack = Box("Dried earth crack", p, new Vector3(.025f, .01f, UnityEngine.Random.Range(.3f, 1.3f)), darkWood, null, false);
            crack.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 180), 0);
        }
        // Low ritual steps remain climbable by the character controller.
        for (int i = 0; i < 4; i++) Box("Altar step", new Vector3(0, .1f + i * .18f, 41 + i * .45f), new Vector3(8 - i, .2f, 7 - i * .6f), rock);
        Box("Altar tablet", new Vector3(0, 1.5f, 43), new Vector3(1.8f, 2.3f, .55f), darkWood);
        Label("九 日 不 归\n山 海 同 焚", new Vector3(0, 2.15f, 42.69f), story.chineseFont, .03f);
        foreach (float x in new[] { -3f, 3f })
        {
            Primitive(PrimitiveType.Cylinder, "Altar brazier", new Vector3(x, 1.0f, 41), new Vector3(.65f, .7f, .65f), gold);
            Primitive(PrimitiveType.Sphere, "Undying ritual flame", new Vector3(x, 1.9f, 41), new Vector3(.45f, .8f, .45f), ember, null, false);
            var fire = new GameObject("Brazier light").AddComponent<Light>(); fire.transform.SetParent(scenery); fire.transform.position = new Vector3(x, 2.3f, 41);
            fire.type = LightType.Point; fire.range = 7; fire.intensity = 2; fire.color = new Color(1, .3f, .05f);
        }
        // Distant escarpments form a silhouette, while invisible walls keep the prototype bounded.
        var mountain = Mat("Distant mountains", new Color(.27f, .23f, .18f));
        for (int i = 0; i < 15; i++)
        {
            float x = -85 + i * 12;
            MeshObject("Distant scorched peak", MountainMesh(i), new Vector3(x, -1, 88 + Mathf.Abs(x) * .22f), new Vector3(22, UnityEngine.Random.Range(12f, 24f), 24), mountain);
        }
        Boundary(new Vector3(-29, 3, 20), new Vector3(1, 8, 98)); Boundary(new Vector3(29, 3, 20), new Vector3(1, 8, 98));
        Boundary(new Vector3(0, 3, -28), new Vector3(60, 8, 1)); Boundary(new Vector3(0, 3, 67), new Vector3(60, 8, 1));
    }
    static void BuildSky(PrologueDirector story)
    {
        var main = new GameObject("Sun · oppressive noon").AddComponent<Light>(); main.type = LightType.Directional;
        main.transform.rotation = Quaternion.Euler(43, -28, 0); main.color = new Color(1, .76f, .46f); main.intensity = 1.25f;
        main.shadows = LightShadows.Soft; main.shadowStrength = .8f; story.sunLight = main;
        var fill = new GameObject("Sky fill").AddComponent<Light>(); fill.type = LightType.Directional;
        fill.transform.rotation = Quaternion.Euler(30, 150, 0); fill.color = new Color(.6f, .66f, .69f); fill.intensity = .5f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.47f, .38f, .28f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.58f, .39f, .22f); RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .008f;
        var sky = Mat("Dust sky", Color.white); sky.shader = Shader.Find("Houyi/DustSky");
        sky.SetColor("_Horizon", new Color(.62f, .43f, .25f)); sky.SetColor("_Zenith", new Color(.12f, .17f, .19f));
        RenderSettings.skybox = sky; RenderSettings.sun = main;
        var sunlight = Mat("First sun", new Color(1, 1, .9f)); sunlight.shader = Shader.Find("Houyi/SunFire");
        var forbidden = Mat("Forbidden sun surface", new Color(1, .7f, .4f)); forbidden.shader = Shader.Find("Houyi/SunFire");
        var halo = Mat("Solar corona", new Color(1, .43f, .10f, .7f)); halo.shader = Shader.Find("Houyi/SunHalo");
        Sun("The solitary sun", new Vector3(0, 34, 110), 9, sunlight, halo, scenery);
        var suns = new GameObject("02 · 九日异象 / Nine forbidden suns"); story.nineSuns = suns;
        for (int i = 0; i < 9; i++)
        {
            float a = (i + .5f) / 9 * Mathf.PI;
            Vector3 p = new Vector3(Mathf.Cos(a) * 53, 26 + Mathf.Sin(a) * 33, 105);
            Sun("Forbidden sun " + (i + 1), p, 4.2f + i % 3, forbidden, halo, suns.transform);
        }
        suns.SetActive(false);
        QualitySettings.shadowDistance = 70;
    }

    static Transform Character(string name, string path, Vector3 position, float yaw, float height)
    {
        var root = new GameObject(name); root.transform.position = position;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset)
        {
            var model = UnityEngine.Object.Instantiate(asset, root.transform); model.name = "Visual";
            foreach (var anim in model.GetComponentsInChildren<Animation>())
            { if (anim.clip) anim.clip.SampleAnimation(anim.gameObject, 0); UnityEngine.Object.DestroyImmediate(anim); }
            foreach (var anim in model.GetComponentsInChildren<Animator>()) UnityEngine.Object.DestroyImmediate(anim);
            foreach (var c in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                model.transform.localScale *= height / Mathf.Max(.01f, bounds.size.y);
                bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                model.transform.position -= new Vector3(bounds.center.x - position.x, bounds.min.y - position.y, bounds.center.z - position.z);
                foreach (var r in renderers)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (!mats[i]) continue;
                        var texture = mats[i].mainTexture;
                        // Ancient binary materials still resolve their references through Unity.
                        var converted = Mat("NPC_" + mats[i].name, Color.white);
                        converted.shader = Shader.Find("Legacy Shaders/Transparent/Cutout/Diffuse"); converted.mainTexture = texture;
                        mats[i] = converted;
                    }
                    r.sharedMaterials = mats;
                }
            }
        }
        else Primitive(PrimitiveType.Capsule, "Placeholder figure", position + Vector3.up * height / 2, new Vector3(.65f, height / 2, .65f), clay, root.transform);
        root.transform.rotation = Quaternion.Euler(0, yaw, 0);
        return root.transform;
    }
    static void House(Vector3 p, float yaw, bool ruined)
    {
        var house = new GameObject(ruined ? "Scorched dwelling" : "Village dwelling"); house.transform.SetParent(scenery); house.transform.position = p;
        Box("Plaster walls", p + Vector3.up * 1.45f, new Vector3(5.5f, 2.9f, 4), clay, house.transform);
        Box("Foundation", p + Vector3.up * .15f, new Vector3(6, .3f, 4.5f), rock, house.transform);
        var top = MeshObject("Sweeping tiled roof", RoofMesh(false), p + Vector3.up * 2.9f, new Vector3(6.6f, 1.8f, 5.3f), ruined ? darkWood : roof);
        top.transform.SetParent(house.transform, true);
        Box("Door", p + new Vector3(0, 1, -2.02f), new Vector3(1.1f, 2, .12f), darkWood, house.transform);
        foreach (float x in new[] { -1.9f, 1.9f })
        {
            Box("Window", p + new Vector3(x, 1.65f, -2.06f), new Vector3(.9f, .85f, .08f), darkWood, house.transform);
            for (int i = 0; i < 3; i++) Box("Window lattice", p + new Vector3(x - .3f + i * .3f, 1.65f, -2.12f), new Vector3(.045f, .9f, .05f), wood, house.transform, false);
        }
        foreach (float x in new[] { -2.5f, 2.5f })
            Box("Porch post", p + new Vector3(x, 1.4f, -2.65f), new Vector3(.18f, 2.8f, .18f), wood, house.transform);
        Box("Porch beam", p + new Vector3(0, 2.8f, -2.65f), new Vector3(5.7f, .2f, .2f), wood, house.transform);
        for (int i = 0; i < 3; i++)
            Primitive(PrimitiveType.Cylinder, "Empty clay jar", p + new Vector3(3.1f + i * .4f, .35f, -1.6f), new Vector3(.55f, .35f, .55f), i % 2 == 0 ? clay : rock, house.transform);
        house.transform.rotation = Quaternion.Euler(0, yaw, 0);
    }
    static void Gate(Vector3 p, string name, Font font)
    {
        foreach (float x in new[] { -3.2f, 3.2f }) Box("Gate pillar", p + new Vector3(x, 2.4f, 0), new Vector3(.4f, 4.8f, .4f), darkWood);
        Box("Gate lintel", p + Vector3.up * 4.3f, new Vector3(7.8f, .45f, .55f), wood);
        Box("Gate upper lintel", p + Vector3.up * 4.9f, new Vector3(8.6f, .35f, .8f), roof);
        Box("Village signboard", p + new Vector3(0, 4.05f, -.35f), new Vector3(2.4f, .8f, .15f), darkWood);
        Label(name, p + new Vector3(0, 4.27f, -.46f), font, .09f);
    }
    static void DeadTree(Vector3 p)
    {
        float height = UnityEngine.Random.Range(2.3f, 4.5f);
        Primitive(PrimitiveType.Cylinder, "Dead tree trunk", p + Vector3.up * height / 2, new Vector3(.25f, height / 2, .25f), darkWood);
        for (int i = 0; i < 3; i++)
        {
            var branch = Primitive(PrimitiveType.Cylinder, "Bare branch", p + new Vector3((i - 1) * .45f, height * .7f + i * .2f, .1f), new Vector3(.1f, .8f, .1f), darkWood, null, false);
            branch.transform.rotation = Quaternion.Euler(i * 25, i * 70, (i - 1) * 48);
        }
    }
    static void Boundary(Vector3 p, Vector3 scale)
    { var boundary = new GameObject("World boundary"); boundary.transform.SetParent(scenery); boundary.transform.position = p; boundary.AddComponent<BoxCollider>().size = scale; }
    static void Sun(string name, Vector3 p, float diameter, Material surface, Material halo, Transform parent)
    {
        var core = Primitive(PrimitiveType.Sphere, name, p, Vector3.one * diameter, surface, parent, false);
        Primitive(PrimitiveType.Quad, "Solar corona", p + Vector3.forward * .1f, Vector3.one * diameter * 2.8f, halo, core.transform, false);
    }
    static Mesh MountainMesh(int seed)
    {
        string path = Root + "/Art/Ridge_" + seed + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh) return mesh;
        var random = new System.Random(seed * 37 + 81);
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        var bottom = new Vector3[8]; var middle = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4;
            float radius = .42f + (float)random.NextDouble() * .18f;
            bottom[i] = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            middle[i] = new Vector3(Mathf.Cos(a) * radius * .6f, .28f + (float)random.NextDouble() * .32f, Mathf.Sin(a) * radius * .6f);
        }
        Vector3 summit = new Vector3((float)random.NextDouble() * .25f - .12f, 1, (float)random.NextDouble() * .25f - .12f);
        Action<Vector3, Vector3, Vector3> triangle = (a, b, c) => { int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2); };
        for (int i = 0; i < 8; i++) { int j = (i + 1) % 8; triangle(bottom[i], middle[i], bottom[j]); triangle(bottom[j], middle[i], middle[j]); triangle(middle[i], summit, middle[j]); }
        mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
    }
    static void Label(string text, Vector3 position, Font font, float scale = .065f)
    {
        if (!font) return;
        var go = new GameObject(text); go.transform.SetParent(scenery); go.transform.position = position;
        var label = go.AddComponent<TextMesh>(); label.text = text; label.font = font; label.fontSize = 64;
        label.characterSize = scale; label.anchor = TextAnchor.UpperCenter; label.alignment = TextAlignment.Center; label.color = new Color(.95f, .75f, .38f);
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }
    static GameObject Primitive(PrimitiveType type, string name, Vector3 p, Vector3 scale, Material material, Transform parent = null, bool collision = true)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.position = p; go.transform.localScale = scale;
        go.transform.SetParent(parent ? parent : scenery, true); go.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static GameObject Box(string name, Vector3 p, Vector3 scale, Material material, Transform parent = null, bool collision = true)
    { return Primitive(PrimitiveType.Cube, name, p, scale, material, parent, collision); }
    static Mesh RoofMesh(bool peak)
    {
        string path = Root + "/Art/" + (peak ? "Mountain" : "Roof") + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh) return mesh;
        mesh = new Mesh();
        if (peak)
        {
            mesh.vertices = new[] { new Vector3(-.5f,0,-.5f), new Vector3(.5f,0,-.5f), new Vector3(.5f,0,.5f), new Vector3(-.5f,0,.5f), new Vector3(0,1,0) };
            mesh.triangles = new[] { 0,4,1, 1,4,2, 2,4,3, 3,4,0, 0,1,2, 0,2,3 };
        }
        else
        {
            mesh.vertices = new[] { new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(0,1,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(0,1,.5f) };
            mesh.triangles = new[] { 0,2,1, 3,4,5, 0,3,5, 0,5,2, 2,5,4, 2,4,1, 0,1,4, 0,4,3 };
        }
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
    }
    static GameObject MeshObject(string name, Mesh mesh, Vector3 p, Vector3 scale, Material material)
    {
        var go = new GameObject(name); go.transform.SetParent(scenery); go.transform.position = p; go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material; return go;
    }
    static Material Mat(string name, Color color, bool glow = false)
    {
        string path = Root + "/Art/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color; mat.SetFloat("_Glossiness", .08f);
        if (glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 2); }
        return mat;
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wuxia.Archery;

public static class ArcheryDemoBuilder
{
    const string Folder = "Assets/ArcheryDemo";
    const string ScenePath = Folder + "/ArcheryDemo.unity";
    [MenuItem("Tools/Archery/Create Blue Robe Demo")]
    public static void Build()
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/PlayerCharacterPrefab.prefab");
            if (!source) throw new InvalidOperationException("PlayerCharacterPrefab not found");
            var actor = UnityEngine.Object.Instantiate(source); actor.name = "BlueRobeArcher";
            actor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var c in actor.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach (var t in actor.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach (var c in actor.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach (var c in actor.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach (var anim in actor.GetComponentsInChildren<Animation>(true))
            {
                if (anim.clip) anim.clip.SampleAnimation(anim.gameObject, 0);
                UnityEngine.Object.DestroyImmediate(anim);
            }
            // Normalize the legacy FBX's 8x scale to a 1.8 metre character.
            var renderers = actor.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (!materials[i]) continue;
                    string texturePath = "Assets/Standard Assets/Character Controllers/Materials/" + materials[i].name + ".png";
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if (!texture) continue;
                    var converted = Material("Character_" + materials[i].name, Color.white);
                    converted.shader = Shader.Find("Legacy Shaders/Transparent/Cutout/Diffuse");
                    converted.mainTexture = texture;
                    materials[i] = converted;
                }
                renderer.sharedMaterials = materials;
            }
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            actor.transform.localScale *= 1.8f / bounds.size.y;
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            actor.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            // Keep the controller's frame at the floor origin, independent of model offsets.
            var root = new GameObject("ArcheryCharacter"); actor.transform.SetParent(root.transform, true);
            var demo = root.AddComponent<ArcherDemo>();
            Transform[] bones = actor.GetComponentsInChildren<Transform>(true);
            demo.leftUpper = Bone(bones, "Bip001 L UpperArm"); demo.leftForearm = Bone(bones, "Bip001 L Forearm"); demo.leftHand = Bone(bones, "Bip001 L Hand");
            demo.rightUpper = Bone(bones, "Bip001 R UpperArm"); demo.rightForearm = Bone(bones, "Bip001 R Forearm"); demo.rightHand = Bone(bones, "Bip001 R Hand");
            demo.bowMaterial = Material("BowWood", new Color(.28f, .12f, .035f));
            demo.stringMaterial = Material("StringIvory", new Color(.95f, .85f, .57f));
            demo.arrowMaterial = Material("ArrowWood", new Color(.48f, .27f, .09f));
            demo.autoDemo = true;
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/BlueRobeArcher.prefab");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name = "Practice ground";
            floor.transform.localScale = Vector3.one * 2;
            floor.GetComponent<Renderer>().sharedMaterial = Material("Ground", new Color(.17f, .22f, .23f));
            var targetMat = Material("Target", new Color(.75f, .28f, .16f));
            for (int i = 0; i < 3; i++)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); ring.name = "Target ring " + i;
                ring.transform.position = new Vector3(0, 1.4f, 8 + i * -.035f);
                ring.transform.rotation = Quaternion.Euler(90, 0, 0);
                float diameter = 1.3f - i * .4f;
                ring.transform.localScale = new Vector3(diameter, .03f, diameter);
                ring.GetComponent<Renderer>().sharedMaterial = i == 1 ? demo.stringMaterial : targetMat;
            }
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(3.3f, 2.1f, 3.7f);
            camera.transform.LookAt(new Vector3(0, 1, .3f)); camera.fieldOfView = 40;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f, .09f, .13f);
            var light = new GameObject("Key Light").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(35, -30, 0);
            var fill = new GameObject("Fill Light").AddComponent<Light>(); fill.type = LightType.Directional;
            fill.intensity = .75f; fill.transform.rotation = Quaternion.Euler(25, 155, 0);
            RenderSettings.ambientLight = new Color(.6f, .65f, .72f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/ArcheryDemo.result", "SUCCESS: " + ScenePath);
            Debug.Log("Archery demo created: " + ScenePath);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
        }
    }
    static Transform Bone(Transform[] bones, string name)
    {
        var bone = bones.FirstOrDefault(t => t.name == name);
        if (!bone) throw new InvalidOperationException("Missing bone: " + name);
        return bone;
    }
    static Material Material(string name, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
        material.color = color; return material;
    }
}

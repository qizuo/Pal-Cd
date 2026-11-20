using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Houyi.NineSuns;

public sealed class SunburstProbe : MonoBehaviour
{
    static bool booted;
    PrologueDirector story;
    ChapterOneDirector chapter;
    PrologueSunburst skill;
    string report = "";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (booted) return; booted = true;
        HouyiSave.DirectoryOverride = Path.Combine(Directory.GetCurrentDirectory(), "SunburstTestSave"); HouyiSave.AutoLoadEnabled = false;
        var go = new GameObject("Sunburst integration probe"); DontDestroyOnLoad(go); go.AddComponent<SunburstProbe>();
    }
    IEnumerator Start()
    {
        yield return null;
        var test = Run();
        while (true)
        {
            object current;
            try { if (!test.MoveNext()) break; current = test.Current; }
            catch (Exception e) { Finish(false, e.ToString()); yield break; }
            yield return current;
        }
        Finish(true, "Skill integration passed.");
    }
    void Finish(bool success, string detail)
    {
        File.WriteAllText("Sunburst-Validation.txt", report + (success ? "SUCCESS\n" : "FAIL\n") + detail);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(success ? 0 : 1);
#endif
    }
    void Check(bool value, string label) { if (!value) throw new Exception(label); report += "PASS " + label + "\n"; Debug.Log(label); }
    void Bind()
    {
        story = FindObjectOfType<PrologueDirector>(); chapter = story.ChapterOne; skill = story.player.Sunburst;
        story.enabled = false; story.player.enabled = false; story.player.archer.enabled = false; story.player.TouchControls.enabled = false;
        if (chapter) { chapter.enabled = false; foreach (var a in chapter.Actors) a.enabled = false; }
    }
    void Talk(Transform t)
    {
        story.player.Teleport(new Vector3(t.position.x, .1f, t.position.z - 2)); story.Interact();
        while (story.DialogueOpen) story.AdvanceDialogue();
    }
    void Arrange(ChapterOneActor[] wolves)
    {
        for (int i = 0; i < wolves.Length; i++)
        { wolves[i].ResetEncounter(); wolves[i].transform.position = chapter.Point(i == 0 ? 0 : i == 1 ? 2 : 7, 80); }
        Physics.SyncTransforms();
    }
    void Launch(ChapterOneActor target, bool special)
    {
        var archer = story.player.archer;
        story.player.Teleport(chapter.Point(0, 73)); story.player.transform.rotation = Quaternion.identity;
        archer.CancelDraw(); archer.Tick(.35f);
        if (special) skill.ToggleSelection();
        archer.aimDirection = (target.transform.position + Vector3.up * .6f - (story.player.transform.position + Vector3.up * 1.4f)).normalized;
        archer.BeginDraw(); archer.Tick(.6f); archer.Tick(1.1f); archer.ReleaseDraw(); archer.Tick(.001f);
        Physics.SyncTransforms();
    }
    IEnumerator Run()
    {
        Bind(); HouyiSave.Delete(); Check(!skill.Unlocked, "skill unavailable in prologue");
        story.StartChapterOne(); Bind(); Check(skill.Unlocked && skill.CooldownRemaining == 0, "skill ready at chapter one start");
        var effects = skill.GetComponent<SunburstEffects>();
        Check(effects && effects.AssetsLoaded, "runtime VFX textures and shader loaded");
        var blast = effects.PlayImpact(chapter.Point(0, 73), 4);
        var systems = blast.GetComponentsInChildren<ParticleSystem>();
        Check(systems.Length == 4 && blast.GetComponentsInChildren<Light>().Length == 0, "impact combines four particle layers without lights");
        foreach (var ps in systems) { ps.Simulate(.1f, true, true); ps.Pause(); }
        var expansion = blast.GetComponentInChildren<SunburstExpansion>();
        expansion.transform.localScale = Vector3.one * (8 / .855f);
        var cameraObject = new GameObject("VFX validation camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = blast.transform.position + new Vector3(0, 7, -8);
        camera.transform.LookAt(blast.transform.position);
        var rt = new RenderTexture(960, 540, 24); camera.targetTexture = rt; camera.Render();
        RenderTexture.active = rt;
        var image = new Texture2D(960, 540, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
        File.WriteAllBytes("Sunburst-VFX.png", image.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = null; Destroy(rt); Destroy(image); Destroy(cameraObject); Destroy(blast);
        skill.ToggleSelection(); Check(skill.Selected && skill.CooldownRemaining == 0, "selection does not start cooldown");
        skill.ToggleSelection(); Check(!skill.Selected && skill.CooldownRemaining == 0, "selection can be canceled for free");
        Talk(chapter.Elder); foreach (var clue in chapter.Clues) Talk(clue);
        var wolves = chapter.Actors.FindAll(a => a.Kind == ChapterOneActor.Type.Wolf && a.Encounter == chapter.Current).ToArray();
        Arrange(wolves); Launch(wolves[0], false);
        for (int i = 0; i < 25; i++) { story.player.archer.Tick(.02f); yield return null; }
        Check(wolves[0].Health == 2 && wolves[1].Health == 4, "ordinary arrow still damages only direct target");
        Arrange(wolves); Launch(wolves[0], true);
        Check(!skill.Selected && skill.CooldownRemaining > 7.9f, "skill cooldown starts when real arrow is released");
        for (int i = 0; i < 25; i++) { story.player.archer.Tick(.02f); yield return null; }
        Check(wolves[0].Health == 1 && wolves[1].Health == 1 && wolves[2].Health == 4, "real skill arrow hits each nearby enemy once for 3 damage and respects radius");
        Check(wolves[0].State == ChapterOneActor.AttackState.Recover, "ordinary enemy staggered by blast");
        skill.ToggleSelection(); Check(!skill.Selected, "cooldown blocks reselection");
        story.TogglePause(); int hp = wolves[0].Health;
        chapter.SunburstHit(wolves[0].transform.position, 1, 4);
        Check(wolves[0].Health == hp, "pause blocks area damage"); story.TogglePause();
        skill.RestoreCooldown(0); skill.ToggleSelection(); story.ToggleJournal(); yield return null;
        Check(!skill.Selected && skill.CooldownRemaining == 0, "journal cancels selection without consuming cooldown"); story.ToggleJournal();
        skill.ToggleSelection(); story.player.archer.InterruptByHit(); yield return null;
        Check(!skill.Selected && skill.CooldownRemaining == 0, "being hit cancels selected skill for free"); story.player.archer.Tick(.35f);
        Arrange(wolves);
        int weakHits = chapter.SunburstHit(chapter.Point(0, 80), .2f, 4);
        Check(weakHits == 2 && wolves[0].Health == 2, "lower charge produces lower area damage");
        chapter.SunburstHit(chapter.Point(0, 80), 1, 4);
        Check(HouyiSave.TryRead(out var saved) && saved.actors[0] && saved.actors[1], "area kills update event saves");
        skill.RestoreCooldown(4); story.SaveProgress(); HouyiSave.AutoLoadEnabled = true;
        SceneManager.LoadScene("Assets/HouyiPrologue/Scenes/Houyi_Prologue.unity"); yield return null; yield return null; Bind();
        Check(skill.Unlocked && !skill.Selected && skill.CooldownRemaining > 3 && skill.CooldownRemaining <= 4, "continued chapter retains skill and saved cooldown");
        var state = chapter.CaptureProgress(); state.stage = (int)ChapterOneDirector.Stage.Gate;
        chapter.RestoreProgress(state);
        var core = chapter.Actors.Find(a => a.Kind == ChapterOneActor.Type.Core); int coreHP = core.Health;
        chapter.SunburstHit(core.transform.position, 1, 4); Check(core.Health == coreHP, "area skill cannot bypass gate core puzzle");
        state.stage = (int)ChapterOneDirector.Stage.Boss; chapter.RestoreProgress(state);
        int bossHP = chapter.Boss.Health;
        chapter.SunburstHit(chapter.Boss.transform.position, 1, 4); Check(chapter.Boss.Health == bossHP, "boss armor still blocks area damage");
        typeof(ChapterOneActor).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(chapter.Boss, ChapterOneActor.AttackState.Exposed);
        chapter.SunburstHit(chapter.Boss.transform.position, 1, 4);
        Check(chapter.Boss.Health == bossHP - 3 && chapter.Boss.Exposed, "exposed boss takes area damage without stagger");
        var touch = new PrologueTouchInput { ShowSkill = true, SkillRect = new Rect(1200, 800, 130, 90),
            FireCenter = new Vector2(1700, 880), LookArea = new Rect(960, 300, 960, 780) };
        touch.BeginFrame(0); touch.Process(1, new Vector2(1250, 840), PrologueTouchInput.Phase.Began); touch.EndFrame();
        Check(touch.SkillPressed && !touch.FirePressed && touch.LookDelta == Vector2.zero, "touch skill button owns its finger without firing or looking");
        touch.BeginFrame(1); touch.Process(1, new Vector2(1250, 840), PrologueTouchInput.Phase.Ended);
        touch.Process(2, new Vector2(1700, 880), PrologueTouchInput.Phase.Began); touch.EndFrame();
        touch.BeginFrame(2); touch.Process(2, new Vector2(1720, 860), PrologueTouchInput.Phase.Moved); touch.EndFrame();
        Check(touch.FireHeld && touch.LookDelta != Vector2.zero, "charging finger can still drag to aim after selecting skill");
    }
}

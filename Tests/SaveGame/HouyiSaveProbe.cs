using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Houyi.NineSuns;

public sealed class HouyiSaveProbe : MonoBehaviour
{
    static bool booted;
    PrologueDirector story;
    ChapterOneDirector chapter;
    string report = "";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (booted) return;
        booted = true;
        HouyiSave.DirectoryOverride = Path.Combine(Directory.GetCurrentDirectory(), "SaveReloadTest");
        HouyiSave.AutoLoadEnabled = Array.IndexOf(Environment.GetCommandLineArgs(), "-houyiResumeOnly") >= 0;
        var go = new GameObject("Save and resume integration probe"); DontDestroyOnLoad(go); go.AddComponent<HouyiSaveProbe>();
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
        Finish(true, "Save/load integration passed across scene reloads.");
    }
    void Finish(bool passed, string detail)
    {
        string resultName = Array.IndexOf(Environment.GetCommandLineArgs(), "-houyiResumeOnly") >= 0 ? "SaveGame-Relaunch.txt" : "SaveGame-Validation.txt";
        File.WriteAllText(resultName, report + (passed ? "SUCCESS\n" : "FAIL\n") + detail);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(passed ? 0 : 1);
#endif
    }
    void Check(bool value, string message) { if (!value) throw new Exception(message); report += "PASS " + message + "\n"; Debug.Log(message); }
    void Bind()
    {
        story = FindObjectOfType<PrologueDirector>(); chapter = story.ChapterOne;
        story.enabled = false; story.player.enabled = false; story.player.archer.enabled = false;
        story.player.TouchControls.enabled = false;
        foreach (var b in story.beasts) b.enabled = false;
        if (chapter) { chapter.enabled = false; foreach (var a in chapter.Actors) a.enabled = false; }
    }
    IEnumerator Reload()
    {
        HouyiSave.AutoLoadEnabled = true;
        SceneManager.LoadScene("Assets/HouyiPrologue/Scenes/Houyi_Prologue.unity");
        yield return null; yield return null; Bind();
    }
    void Dialogue() { int guard = 0; while (story.DialogueOpen && guard++ < 20) story.AdvanceDialogue(); }
    void Interact(Transform t) { story.player.Teleport(new Vector3(t.position.x, .1f, t.position.z - 2)); story.Interact(); }
    HouyiProgress Saved() { Check(HouyiSave.TryRead(out var data), "save exists and parses"); return data; }
    void Kill(ChapterOneActor a) { for (int i = 0; i < 40 && !a.Defeated; i++) a.OnArrowHit(1); }
    static void Tick(object target) { target.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }
    IEnumerator Run()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-houyiResumeOnly") >= 0)
        {
            Bind(); Check(chapter && chapter.Current == ChapterOneDirector.Stage.Village, "fresh Unity process resumes replayed chapter from its beginning");
            yield break;
        }
        HouyiSave.Delete(); Bind(); story.Begin();
        Check(Saved().stage == (int)PrologueDirector.ChapterStep.Village, "beginning saves village checkpoint");
        Interact(story.elder); Check(Saved().stage == 1, "unfinished dialogue does not advance save"); Dialogue();
        story.targets[0].OnArrowHit(1);
        Check(Saved().targets[0], "individual training hit is saved");
        yield return Reload();
        Check(story.Step == PrologueDirector.ChapterStep.Trial && story.TrainingHits == 1 && story.targets[0].defeated, "startup restores trial and individual target");
        story.TogglePause(); story.RestartCurrentChapter(); yield return null; yield return null; Bind();
        Check(!story.InChapterOne && story.Step == PrologueDirector.ChapterStep.Village && story.TrainingHits == 0 && story.Health == 100 && Time.timeScale == 1, "replay prologue clears progress and unpauses at chapter start");
        Interact(story.elder); Dialogue();
        foreach (var t in story.targets) if (!t.defeated) t.OnArrowHit(1);
        story.beasts[0].OnArrowHit(1); yield return Reload();
        Check(story.Step == PrologueDirector.ChapterStep.Rescue && story.BeastsDefeated == 1, "startup restores defeated prologue beast");
        foreach (var b in story.beasts) if (!b.defeated) b.OnArrowHit(1);
        Interact(story.survivor); Dialogue(); Interact(story.altar); Dialogue();
        story.seal.OnArrowHit(1); yield return Reload();
        Check(story.Step == PrologueDirector.ChapterStep.Seal && !story.seal.defeated && story.seal.gameObject.activeSelf, "quit during ending dialogue allows seal replay");
        story.seal.OnArrowHit(1); Dialogue(); yield return Reload();
        Check(story.Step == PrologueDirector.ChapterStep.Complete, "prologue completion survives restart");
        story.StartChapterOne(); Bind(); Interact(chapter.Elder); Dialogue();
        Interact(chapter.Clues[0]); Check(Saved().cluesFound == 0, "unfinished clue dialogue is not saved"); Dialogue();
        Interact(chapter.Supply); yield return Reload();
        Check(chapter.Current == ChapterOneDirector.Stage.Trail && chapter.CluesFound == 1 && !chapter.Supply.gameObject.activeSelf, "startup restores clue and consumed supply");
        while (chapter.CluesFound < 3) { Interact(chapter.Clues[chapter.CluesFound]); Dialogue(); }
        foreach (var a in chapter.Actors) if (a.Encounter == ChapterOneDirector.Stage.Ambush) Kill(a);
        Tick(chapter);
        chapter.Actors.Find(a => a.Kind == ChapterOneActor.Type.Rope).OnArrowHit(1);
        Kill(chapter.Actors.Find(a => a.Kind == ChapterOneActor.Type.Wolf && a.Encounter == ChapterOneDirector.Stage.Rescue));
        yield return Reload();
        Check(chapter.Current == ChapterOneDirector.Stage.Rescue && chapter.RopeCut && !chapter.Cage.activeSelf, "startup restores cut rope and open cage");
        Check(chapter.Actors.FindAll(a => a.Encounter == ChapterOneDirector.Stage.Rescue && a.IsEnemy && a.Defeated).Count == 1, "startup restores individual rescue enemy");
        foreach (var a in chapter.Actors) if (a.Encounter == chapter.Current && a.IsEnemy) Kill(a);
        Interact(chapter.Captive); Dialogue();
        Kill(chapter.Actors.Find(a => a.Kind == ChapterOneActor.Type.Core)); yield return Reload();
        Check(chapter.Current == ChapterOneDirector.Stage.Gate && chapter.Gate.activeSelf
            && chapter.Actors.FindAll(a => a.Kind == ChapterOneActor.Type.Core && a.Defeated).Count == 1, "startup restores one broken gate core");
        foreach (var a in chapter.Actors) if (a.Kind == ChapterOneActor.Type.Core) Kill(a);
        Interact(chapter.Shrine); Tick(chapter);
        chapter.Boss.TakeDamage(10); yield return Reload();
        Check(chapter.Current == ChapterOneDirector.Stage.Boss && chapter.Boss.Health == chapter.Boss.MaxHealth && chapter.ArenaGate.activeSelf && !chapter.WeakPoint.gameObject.activeSelf, "startup retries unfinished boss from safe checkpoint");
        chapter.Boss.TakeDamage(chapter.Boss.MaxHealth);
        typeof(ChapterOneDirector).GetMethod("DefeatBoss", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(chapter, null);
        yield return Reload();
        Check(chapter.Current == ChapterOneDirector.Stage.Spring && chapter.Water.activeSelf && chapter.Boss.Defeated, "boss defeat and spring restoration persist");
        Interact(chapter.Spring); Dialogue(); yield return Reload();
        Check(chapter.Finished && !story.CanControl, "chapter ending persists");
        var complete = Saved(); string normalDirectory = HouyiSave.DirectoryOverride;
        HouyiSave.DirectoryOverride = Path.Combine(normalDirectory, "BackupTest");
        HouyiSave.Write(complete); HouyiSave.Write(complete); File.WriteAllText(HouyiSave.SavePath, "{broken");
        Check(HouyiSave.TryRead(out var backup) && backup.stage == complete.stage, "corrupt primary save falls back to previous backup");
        HouyiSave.Delete(); Check(!HouyiSave.TryRead(out backup), "new game clears primary and backup");
        HouyiSave.DirectoryOverride = normalDirectory;
        story.RestartCurrentChapter(); yield return null; yield return null; Bind();
        Check(chapter && chapter.Current == ChapterOneDirector.Stage.Village && chapter.CluesFound == 0 && !chapter.RopeCut
            && chapter.Actors.TrueForAll(a => !a.Defeated) && chapter.Cage.activeSelf && chapter.Gate.activeSelf && chapter.Supply.gameObject.activeSelf
            && story.Health == 100 && Time.timeScale == 1, "replay chapter one resets all events without returning to prologue");
        yield return Reload();
        Check(chapter && chapter.Current == ChapterOneDirector.Stage.Village, "replayed chapter beginning replaces previous save");
    }
}

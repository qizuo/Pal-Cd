// Isolation-only integration probe; do not copy into shipping Assets.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using Houyi.NineSuns;
using Wuxia.Archery;

public sealed class ChapterOneProbe : MonoBehaviour
{
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    PrologueDirector story;
    ChapterOneDirector chapter;
    string report = "";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        HouyiSave.DirectoryOverride = Path.Combine(Directory.GetCurrentDirectory(), "IsolatedSave");
        HouyiSave.AutoLoadEnabled = false;
        new GameObject("Chapter One integration probe").AddComponent<ChapterOneProbe>();
    }
    IEnumerator Start()
    {
        yield return null;
        IEnumerator test = Run();
        while (true)
        {
            object current;
            try { if (!test.MoveNext()) break; current = test.Current; }
            catch (Exception e) { Finish(false, e.ToString()); yield break; }
            yield return current;
        }
        Finish(true, "Complete chapter flow passed.");
    }
    void Assert(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        report += "PASS " + label + "\n"; Debug.Log("PASS " + label);
    }
    static void Tick(object target) { target.GetType().GetMethod("Update", Flags).Invoke(target, null); }
    void Dialogue() { int guard = 0; while (story.DialogueOpen && guard++ < 20) story.AdvanceDialogue(); Assert(!story.DialogueOpen, "dialogue completes"); }
    void Near(Transform point) { story.player.Teleport(new Vector3(point.position.x, .1f, point.position.z - 2)); Physics.SyncTransforms(); }
    void Stage(ChapterOneDirector.Stage expected) { Assert(chapter.Current == expected, "stage " + expected); }
    void Finish(bool success, string detail)
    {
        File.WriteAllText("ChapterOne-Validation.txt", report + (success ? "SUCCESS\n" : "FAIL\n") + detail);
        Debug.Log(detail);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(success ? 0 : 1);
#endif
    }
    IEnumerator Arrow(ChapterOneActor target, float charge)
    {
        var archer = story.player.archer;
        Vector3 aim = target.transform.position + (target.Kind == ChapterOneActor.Type.Wolf ? Vector3.up * .6f : Vector3.zero);
        bool horn = target.Kind == ChapterOneActor.Type.WeakPoint;
        story.player.Teleport(new Vector3(aim.x, 0, aim.z + (horn ? 6 : -6)));
        story.player.transform.rotation = Quaternion.Euler(0, horn ? 180 : 0, 0);
        archer.CancelDraw(); archer.Tick(.31f); archer.aimDirection = (aim - (story.player.transform.position + Vector3.up * 1.4f)).normalized;
        archer.BeginDraw(); archer.Tick(.6f); archer.Tick(1.1f * charge);
        archer.ReleaseDraw(); archer.Tick(.001f);
        Physics.SyncTransforms();
        for (int i = 0; i < 60; i++) { archer.Tick(.02f); yield return null; }
        archer.CancelDraw();
    }
    void Kill(ChapterOneActor target) { for (int i = 0; i < 40 && !target.Defeated; i++) target.OnArrowHit(1); }
    void Capture(string name, Vector3 position, Vector3 lookAt)
    {
        Camera view = story.player.view;
        view.transform.position = position; view.transform.LookAt(lookAt);
        var texture = new RenderTexture(1280, 720, 24);
        view.targetTexture = texture; view.Render(); RenderTexture.active = texture;
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
        File.WriteAllBytes(name, pixels.EncodeToPNG());
        view.targetTexture = null; RenderTexture.active = null; Destroy(texture); Destroy(pixels);
    }
    IEnumerator Run()
    {
        story = FindObjectOfType<PrologueDirector>(); Assert(story, "original prologue scene loads");
        story.enabled = false; story.StartChapterOne(); chapter = story.ChapterOne;
        story.player.enabled = false; story.player.TouchControls.enabled = false; story.player.archer.enabled = false; chapter.enabled = false;
        foreach (var a in chapter.Actors) a.enabled = false;
        yield return null;
        Assert(story.InChapterOne && story.CanControl, "direct chapter entry retains player control");
        Assert(Physics.Raycast(chapter.Point(0, 40, 3), Vector3.down, 4), "mountain path has collision ground");
        Capture("ChapterOne-Trail.png", chapter.Point(7, -4, 5), chapter.Point(0, 43, 2));
        Stage(ChapterOneDirector.Stage.Village); Near(chapter.Elder); story.Interact(); Dialogue(); Stage(ChapterOneDirector.Stage.Trail);
        story.Hurt(30); Near(chapter.Supply); story.Interact();
        Assert(story.Health == 100 && !chapter.Supply.gameObject.activeSelf, "branch supply restores health once");
        Near(chapter.Lore); story.Interact(); Assert(story.DialogueOpen, "branch lore opens dialogue"); Dialogue();
        foreach (var clue in chapter.Clues) { Near(clue); story.Interact(); Dialogue(); }
        Stage(ChapterOneDirector.Stage.Ambush);
        var wolf = chapter.Actors.Find(a => a.Kind == ChapterOneActor.Type.Wolf && a.Encounter == chapter.Current);
        int before = wolf.Health;
        var arrow = Arrow(wolf, 1); while (arrow.MoveNext()) yield return arrow.Current;
        Assert(wolf.Health < before, "real charged arrow collides with disaster wolf");
        story.TogglePause(); Assert(!story.CanControl && Time.timeScale == 0, "pause blocks chapter control");
        before = wolf.Health; wolf.OnArrowHit(1); Assert(wolf.Health == before, "paused enemies reject hits"); story.TogglePause();
        story.ToggleJournal(); Assert(!story.CanControl, "journal blocks combat"); story.ToggleJournal();
        foreach (var a in chapter.Actors) if (a.Encounter == chapter.Current) Kill(a);
        Tick(chapter); Stage(ChapterOneDirector.Stage.Rescue);
        var rope = chapter.Actors.Find(a => a.Kind == ChapterOneActor.Type.Rope); rope.OnArrowHit(.2f);
        Assert(chapter.RopeCut && !chapter.Cage.activeSelf, "shooting rope opens cage");
        foreach (var a in chapter.Actors) if (a.Encounter == chapter.Current && a.IsEnemy) Kill(a);
        Near(chapter.Captive); Assert(story.CanInteract, "rescued captain can talk"); story.Interact(); Dialogue(); Stage(ChapterOneDirector.Stage.Gate);
        var core = chapter.Actors.Find(a => a.Kind == ChapterOneActor.Type.Core); core.OnArrowHit(.4f);
        Assert(!core.Defeated, "weak arrow cannot break gate core");
        arrow = Arrow(core, 1); while (arrow.MoveNext()) yield return arrow.Current;
        Assert(core.Defeated, "real charged arrow breaks gate core");
        foreach (var a in chapter.Actors) if (a.Kind == ChapterOneActor.Type.Core) Kill(a);
        Stage(ChapterOneDirector.Stage.Approach); Assert(!chapter.Gate.activeSelf, "three cores open mountain gate");
        Near(chapter.Shrine); Tick(chapter); Stage(ChapterOneDirector.Stage.Boss);
        Assert(chapter.ArenaGate.activeSelf, "arena entry closes with visible collision wall");
        Capture("ChapterOne-Boss.png", chapter.Point(7, 165, 5), chapter.Point(0, 190, 2));
        int hp = chapter.Boss.Health; chapter.Boss.OnArrowHit(1); chapter.WeakPoint.OnArrowHit(1);
        Assert(chapter.Boss.Health == hp, "armored boss rejects damage outside exposure");
        var boss = chapter.Boss;
        story.player.Teleport(chapter.Point(0, 174));
        typeof(ChapterOneActor).GetField("timer", Flags).SetValue(boss, 0f); Tick(boss);
        Assert(boss.State == ChapterOneActor.AttackState.Windup && chapter.BossWarning.Contains("冲撞"), "boss warns before charging");
        typeof(ChapterOneActor).GetField("timer", Flags).SetValue(boss, 0f); Tick(boss);
        boss.transform.position = chapter.Point(0, 159.8f); Tick(boss);
        Assert(boss.Exposed && chapter.WeakPoint.gameObject.activeSelf, "charge into wall exposes horn core");
        chapter.WeakPoint.OnArrowHit(1); Assert(boss.Health == hp - 4, "full charge punishes boss weak point");
        typeof(ChapterOneActor).GetField("timer", Flags).SetValue(boss, 10f);
        int bossBefore = boss.Health;
        // Shoot from inside the arena, behind the boss that has just hit the entry wall.
        arrow = Arrow(chapter.WeakPoint, 1); while (arrow.MoveNext()) yield return arrow.Current;
        Assert(boss.Health < bossBefore, "real arrow hits exposed boss horn above armor");
        story.Retry(); Assert(boss.Health == boss.MaxHealth && !boss.Exposed && !chapter.WeakPoint.gameObject.activeSelf, "retry resets boss and exposure");
        for (int attack = 1; attack <= 2; attack++)
        {
            typeof(ChapterOneActor).GetField("attackIndex", Flags).SetValue(boss, attack);
            typeof(ChapterOneActor).GetField("timer", Flags).SetValue(boss, 0f); Tick(boss);
            Assert(boss.State == ChapterOneActor.AttackState.Windup, attack == 1 ? "boss slam has windup" : "boss stone throw has windup");
            typeof(ChapterOneActor).GetField("timer", Flags).SetValue(boss, 0f); Tick(boss);
            Assert(boss.State == ChapterOneActor.AttackState.Recover, "boss attack enters recovery");
            typeof(ChapterOneActor).GetField("timer", Flags).SetValue(boss, 0f); Tick(boss);
        }
        boss.TakeDamage(13);
        typeof(ChapterOneActor).GetField("attackIndex", Flags).SetValue(boss, 0);
        typeof(ChapterOneActor).GetField("timer", Flags).SetValue(boss, 0f); Tick(boss);
        Assert((float)typeof(ChapterOneActor).GetField("timer", Flags).GetValue(boss) < 1, "half-health boss shortens attack warning");
        story.Retry(); story.player.Teleport(chapter.Point(0, 174));
        typeof(ChapterOneActor).GetField("timer", Flags).SetValue(boss, 0f); Tick(boss);
        typeof(ChapterOneActor).GetField("timer", Flags).SetValue(boss, 0f); Tick(boss);
        boss.transform.position = chapter.Point(0, 159.8f); Tick(boss);
        for (int i = 0; i < 6; i++) chapter.WeakPoint.OnArrowHit(1);
        Stage(ChapterOneDirector.Stage.Spring);
        Assert(chapter.Water.activeSelf && !chapter.Corruption.activeSelf, "boss defeat restores spring");
        Assert(!chapter.ArenaGate.activeSelf, "boss defeat reopens arena exit");
        Near(chapter.Spring); story.Interact(); Dialogue();
        Assert(chapter.Finished && !story.CanControl, "spring investigation finishes chapter");
    }
}

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Houyi.NineSuns;
using Wuxia.Archery;

public static class ChapterThreeChecks
{
    static void Check(bool value, string message) { if (!value) throw new Exception("Chapter three: " + message); }
    static void Finish(PrologueDirector story) { for (int i = 0; story.DialogueOpen && i < 20; i++) story.AdvanceDialogue(); Check(!story.DialogueOpen, "dialogue finishes"); }
    static PrologueDirector Story()
    {
        var root = new GameObject("Third chapter test"); root.SetActive(false);
        var s = root.AddComponent<PrologueDirector>(); s.enabled = false;
        var hero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ArcheryDemo/BlueRobeArcher.prefab")); hero.SetActive(false);
        var player = hero.AddComponent<ProloguePlayer>(); player.enabled = false; player.story = s; player.archer = hero.GetComponent<ArcherDemo>();
        player.view = new GameObject("Third chapter camera").AddComponent<Camera>(); player.view.farClipPlane = 350;
        s.player = player; s.targets = new PrologueTarget[0]; s.beasts = new PrologueTarget[0]; s.nineSuns = new GameObject("Third chapter suns");
        root.SetActive(true); hero.SetActive(true); player.archer.autoDemo = false; player.archer.enabled = false; return s;
    }
    static void Set(object target, string name, object value) { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    static T Get<T>(object target, string name) { return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(target); }
    static void Defeat(PrologueDirector s, ChapterThreeActor actor)
    {
        for (int i = 0; i < 15 && !actor.Defeated; i++) s.ChapterThree.Hit(actor, 1);
        Check(actor.Defeated, "actor defeated: " + actor.Type);
    }
    static void ShootCore(PrologueDirector story, ChapterThreeActor core, Vector3 offset, bool shouldBreak = true)
    {
        Vector3 position = core.transform.position + offset; position.y = 0;
        story.player.Teleport(position);
        Vector3 delta = core.transform.position - position; delta.y = 0;
        story.player.transform.rotation = Quaternion.LookRotation(delta);
        var archer = story.player.archer;
        archer.CancelForAction();
        archer.aimDirection = (core.transform.position - (position + Vector3.up * 1.4f)).normalized;
        Physics.SyncTransforms();
        Check(Physics.Raycast(position + Vector3.up * 1.4f, archer.aimDirection, out var hit, 20) && hit.collider.GetComponent<ChapterThreeActor>() == core, "rear shooting position has clear line of sight to core; offset=" + offset + "; hit=" + (hit.collider ? hit.collider.name : "none"));
        int shots = archer.ShotsFired;
        string collision = "none"; bool hitCore = false;
        archer.ArrowImpactFactory = (arrow, charge) => (point, collider) => { hitCore = collider.GetComponent<ChapterThreeActor>() == core; collision = collider.name + " charge=" + charge + " at=" + point; collider.SendMessageUpwards("OnArrowHit", charge, SendMessageOptions.DontRequireReceiver); };
        archer.BeginDraw(); archer.Tick(1.2f); archer.ReleaseDraw(); archer.Tick(.02f);
        for (int i = 0; i < 50 && !core.Defeated; i++) archer.Tick(.02f);
        Check(archer.ShotsFired == shots + 1 && hitCore && core.Defeated == shouldBreak, "real charged arrow destroys core from rear offset " + offset + "; shots=" + (archer.ShotsFired - shots) + "; collision=" + collision);
        archer.ArrowImpactFactory = null;
        archer.CancelForAction();
    }
    public static void Run(string outputDirectory)
    {
        HouyiSave.DeleteChapter(3);
        var story = Story(); story.SelectChapter(3); var c = story.ChapterThree;
        Check(c && c.Current == ChapterThreeDirector.Stage.Entry && story.CanControl, "chapter selection enters third chapter");
        Check(c.Actors.Count == 16 && c.Pillars.Count == 4, "stable targets and cover count");
        story.player.Teleport(c.Inscription.position); story.Interact(); Check(!story.DialogueOpen, "entry guards gate investigation");
        foreach (var a in c.Actors.Where(a => a.Encounter == ChapterThreeDirector.Stage.Entry)) Defeat(story, a);
        story.Interact(); Finish(story); Check(c.Current == ChapterThreeDirector.Stage.Bridge, "inscription advances to bridge");
        var ropes = c.Actors.Where(a => a.Type == ChapterThreeActor.Kind.Rope).ToArray();
        Defeat(story, ropes[0]); c.Tick(0); Check(!c.Bridge.activeSelf && c.BridgeGate.activeSelf, "one rope cannot open bridge");
        Defeat(story, ropes[1]); c.Tick(0); Check(c.Bridge.activeSelf && !c.BridgeGate.activeSelf && c.Current == ChapterThreeDirector.Stage.Sundials, "both ropes open bridge");
        var cores = c.Actors.Where(a => a.Type == ChapterThreeActor.Kind.Core).ToArray();
        Vector3 front = cores[0].transform.position - Vector3.forward * 6; front.y = 1.4f;
        Physics.SyncTransforms();
        Check(Physics.Raycast(front, (cores[0].transform.position - front).normalized, out var armor, 10) && armor.collider.name == "日晷正面石甲", "front armor physically blocks core");
        story.player.Teleport(cores[0].FiringZone.position); c.Hit(cores[0], .5f); Check(!cores[0].Defeated, "core requires charge");
        ShootCore(story, cores[0], new Vector3(6, 0, 0), false);
        ShootCore(story, cores[0], new Vector3(2.1f, 0, 4.8f), false);
        story.player.Teleport(cores[1].FiringZone.position); c.Hit(cores[0], 1); Check(!cores[0].Defeated, "another sundial blue zone cannot unlock this core");
        ShootCore(story, cores[0], Vector3.forward * 4.8f);
        Check(cores[0].Defeated && HouyiSave.TryRead(3, out var partial) && partial.stage == 2, "each sundial saves");
        var resume = Story(); resume.SelectChapter(3); Check(resume.InChapterThree && resume.ChapterThree.BrokenCores == 1 && resume.ChapterThree.Bridge.activeSelf && resume.ChapterThree.Seal.activeSelf, "resume preserves partial sundials and bridge");
        resume.ChapterThree.WorldRoot.gameObject.SetActive(false);
        c.Retry(); Check(c.BrokenCores == 1 && story.Health == 100 && story.player.Stamina == 100, "retry preserves broken cores");
        ShootCore(story, cores[1], new Vector3(1.5f, 0, 4.8f));
        ShootCore(story, cores[2], new Vector3(-1.5f, 0, 4.8f));
        c.Tick(0); Check(c.Seal.activeSelf, "surviving sundial guards keep seal closed");
        foreach (var a in c.Actors.Where(a => a.Encounter == ChapterThreeDirector.Stage.Sundials && a.Type == ChapterThreeActor.Kind.Guard)) Defeat(story, a);
        c.Tick(0); Check(c.Current == ChapterThreeDirector.Stage.Courtyard && !c.Seal.activeSelf, "three sundials unlock courtyard");
        story.player.Teleport(c.Point(0, 154)); c.Tick(0); Check(c.Current == ChapterThreeDirector.Stage.Courtyard, "courtyard enemies gate boss");
        foreach (var a in c.Actors.Where(a => a.Encounter == ChapterThreeDirector.Stage.Courtyard)) Defeat(story, a);
        story.player.Teleport(c.Supply.position); story.Hurt(12); story.Interact(); Check(c.SupplyTaken && story.Health == 100, "one-use supply heals");
        story.player.Teleport(c.Point(0, 154)); c.Tick(0); Check(c.Current == ChapterThreeDirector.Stage.Boss && c.ArenaGate.activeSelf, "boss begins and seals arena");
        float hp = c.Boss.Health; c.Hit(c.Boss, 1); Check(c.Boss.Health == hp, "boss armor blocks idle hits");
        c.LineAttack(c.Point(0, 154), Vector3.forward, 1, 2, 15, 18);
        c.Tick(.9f); Check(story.Health == 100, "line warning deals no early damage");
        story.player.Teleport(c.Point(0, 160)); c.Tick(.11f); Check(story.Health == 82, "line releases once after warning"); c.Tick(2); Check(story.Health == 82, "line does not repeat damage");
        story.ChapterCheckpoint(c.Point(0,154)); story.player.Teleport(c.Point(6,160));
        c.LineAttack(c.Point(0,154),Vector3.forward,1,2,15,18);c.Tick(1.1f);Check(story.Health==100,"lateral movement avoids line");
        story.player.Teleport(c.Point(0,160));
        c.LineAttack(c.Point(0,154),Vector3.forward,1,1.6f,15,20,true);
        c.Tick(.9f);Check(!c.WorldRoot.GetComponentsInChildren<Transform>().Any(t=>t.name=="炎矢立方体"),"cube does not launch before warning completes");
        c.Tick(.11f);Check(c.WorldRoot.GetComponentsInChildren<Transform>().Any(t=>t.name=="炎矢立方体") && story.Health==100,"visible cube launches without instant full-line damage");
        var cube=c.WorldRoot.GetComponentsInChildren<Transform>().First(t=>t.name=="炎矢立方体");float previousZ=cube.position.z;
        c.Tick(.1f);Check(cube.position.z>previousZ && story.Health==100,"cube travels toward player before impact");
        c.Tick(.3f);Check(story.Health==80,"cube sweeping player position deals damage once");c.Tick(.1f);Check(story.Health==80,"same cube cannot repeatedly damage player");
        c.ClearThreats();Check(!c.WorldRoot.GetComponentsInChildren<Transform>().Any(t=>t.name=="炎矢立方体"),"retry/interruption cleanup removes visible projectiles");story.ChapterCheckpoint(c.Point(0,154));
        c.CreateCage(c.Point(0,160)); var roots=c.WorldRoot.GetComponentsInChildren<ChapterThreeActor>().Where(a=>a.Type==ChapterThreeActor.Kind.Root).ToArray();
        Check(roots.Length==7,"cage leaves an opening"); c.Hit(roots[0],0);Check(roots[0].Defeated,"ordinary arrow breaks root");c.ClearThreats();
        var pillar=c.Pillars[0];Vector3 cover=pillar.position+(pillar.position-c.Boss.transform.position).normalized*4;cover.y=0;
        story.player.Teleport(cover);Check(c.BehindPillar(cover),"pillar recognizes cover");c.Shockwave();Check(story.Health==100,"pillar blocks shockwave");
        story.player.Teleport(c.Point(0,158));c.Shockwave();Check(story.Health==68,"uncovered shockwave damages player");
        story.Retry();Check(story.Health==100 && c.Boss.Health==45 && c.ArenaGate.activeSelf,"boss retry resets health and cover threats");
        Set(c.Boss,"cycle",1);Set(c.Boss,"timer",0f);c.Boss.Tick(0);
        Check(Get<System.Collections.ICollection>(c,"hazards").Count==3,"normal boss fires three telegraphed lines");
        c.ClearThreats();c.Boss.TakeDamage(23);Check(c.Boss.Enraged,"half health enters rage");
        Set(c.Boss,"state",0);Set(c.Boss,"cycle",0);Set(c.Boss,"timer",0f);c.Boss.Tick(0);
        Check(Get<System.Collections.ICollection>(c,"hazards").Count==10 && Get<float>(c.Boss,"timer")>2.5f,"rage schedules two five-line volleys before recovery");
        c.Tick(1);Check(story.Health==100,"rage volleys retain more than one second of warning");
        var fan=Get<System.Collections.IList>(c,"hazards");
        Vector3 first=Get<Vector3>(fan[0],"direction"),second=Get<Vector3>(fan[1],"direction"),nextVolley=Get<Vector3>(fan[5],"direction");
        Check(Mathf.Abs(Vector3.Angle(first,second)-10f)<.01f && Vector3.Angle(first,nextVolley)<.01f,"five-line fan has 10-degree gaps preserved in both volleys");
        Vector3 gap=Quaternion.Euler(0,5f,0)*Vector3.back;
        story.player.Teleport(c.Boss.transform.position+gap*18);c.Tick(6);Check(story.Health==100,"player standing between rays avoids both full volleys");
        story.player.Teleport(c.Point(0,154));
        c.ClearThreats();Set(c.Boss,"state",0);Set(c.Boss,"cycle",1);Set(c.Boss,"timer",0f);c.Boss.Tick(0);c.Boss.Tick(1.41f);
        Check(Get<System.Collections.ICollection>(c,"hazards").Count==1,"cage follows with aimed line attack");
        c.Tick(1.19f);Check(story.Health==100,"cage follow-up has escape warning");c.Tick(.02f);Check(story.Health==100,"cage cube must travel before dealing damage");c.Tick(1.3f);Check(story.Health==80,"cage follow-up hits if player does not escape");
        story.Retry();c.Boss.TakeDamage(23);Set(c.Boss,"cycle",3);Set(c.Boss,"timer",0f);c.Boss.Tick(0);
        Check(c.Boss.Charging && Mathf.Abs(Get<float>(c.Boss,"timer")-2.4f)<.001f,"rage charge gives 2.4 seconds");
        c.Boss.Tick(2.39f);Check(story.Health==100 && c.Boss.Charging,"rage shockwave does not release early");c.Boss.Tick(.02f);Check(story.Health==64,"rage shockwave deals thirty-six damage");
        story.Retry();Check(!c.Boss.Enraged && Get<System.Collections.ICollection>(c,"hazards").Count==0,"retry clears rage and pending attacks");
        Set(c.Boss,"cycle",3);Set(c.Boss,"timer",0f);c.Boss.Tick(0);Check(c.Boss.Charging,"three-second charge exposes chest");
        c.Hit(c.Boss,1);Check(c.Boss.Health==45,"charging body cannot substitute for weak point");
        c.Boss.HitWeakPoint(1);Check(!c.Boss.Charging && c.Boss.Health==41,"charged weak point interrupts shockwave");
        c.Boss.OnArrowHit(1);Check(c.Boss.Health==41,"one arrow cannot double hit chest and body");
        float recovery=Get<float>(c.Boss,"timer");c.Boss.Tick(.2f);c.Boss.HitWeakPoint(1);
        Check(Mathf.Abs(Get<float>(c.Boss,"timer")-(recovery-.2f))<.001f,"repeated chest hits do not refresh stagger");
        c.Boss.Stagger(100);Check(c.SunburstHit(c.Boss.transform.position+Vector3.up*2,1,4)==1 && c.Boss.Health==33,"Q damages exposed boss once");
        for(int i=0;i<32 && !c.Boss.Defeated;i++)c.Hit(c.Boss,1);
        Check(c.Current==ChapterThreeDirector.Stage.Revelation && !c.ArenaGate.activeSelf,"boss defeat opens altar");
        var chain=c.Actors.Single(a=>a.Type==ChapterThreeActor.Kind.Chain);story.player.Teleport(c.Altar.position);story.Interact();Check(!story.DialogueOpen,"chain gates ending");
        c.Hit(chain,.2f);Check(!chain.Defeated,"chain requires full charge");c.Hit(chain,1);story.Interact();Finish(story);
        Check(c.Finished && !story.CanControl && story.PrecisionRecoveryUnlocked,"ending completes and unlocks reward");
        Check(HouyiSave.TryRead(3,out var completed) && completed.stage==6 && completed.precisionRecoveryUnlocked,"completion and reward saved");
        var end=Story();end.SelectChapter(3);Check(end.ChapterThree.Finished,"completed chapter resumes ending");end.ChapterThree.WorldRoot.gameObject.SetActive(false);
        completed.stage=0;completed.actors=new bool[c.Actors.Count];completed.checkpoint=c.Point(0,0);completed.precisionRecoveryUnlocked=false;HouyiSave.Write(completed);
        Check(HouyiSave.TryRead(3,out var replay) && replay.precisionRecoveryUnlocked,"replaying chapter preserves reward");
        var preview=Story();preview.SelectChapter(3);preview.ChapterThree.EnterBossPreview();
        Check(preview.ChapterThree.Current==ChapterThreeDirector.Stage.Boss && preview.Health==100 && preview.ChapterThree.Actors.Where(a=>a.Encounter<ChapterThreeDirector.Stage.Boss).All(a=>a.Defeated),"boss test entry skips prior tasks");
        preview.player.TickStamina(true,4);preview.ChapterThree.Boss.Stagger(20);preview.ChapterThree.Boss.HitWeakPoint(1);Check(preview.player.Stamina==90,"unlocked precision hit restores ten stamina");
        preview.ChapterThree.WorldRoot.gameObject.SetActive(false);
        var transition=Story();transition.StartChapterTwo();var old=transition.ChapterTwo.WorldRoot;transition.StartChapterThree();Check(transition.InChapterThree && !transition.InChapterTwo && !old.gameObject.activeSelf,"chapter two transitions to third and hides prior world");transition.ChapterThree.WorldRoot.gameObject.SetActive(false);
        Render(c,story.player.view,Path.Combine(outputDirectory,"chapter-three-map.png"));
        File.WriteAllText(Path.Combine(outputDirectory,"chapter-three-result.txt"),"PASS: chapter selection; guarded entry; two-rope bridge; directional charged sundials; partial save/resume; courtyard gate; supply; boss armor; telegraph timing; line avoidance; destructible cage; pillar cover; shockwave damage; chest interruption and single-arrow deduplication; Q; retry; charged chain; ending; persistent stamina reward; boss test entry; chapter-two transition.");
        c.WorldRoot.gameObject.SetActive(false);
    }
    static void Render(ChapterThreeDirector c,Camera camera,string path)
    {
        camera.transform.position=c.Point(45,60,65);camera.transform.LookAt(c.Point(0,96,0));camera.backgroundColor=new Color(.23f,.25f,.25f);camera.clearFlags=CameraClearFlags.SolidColor;
        var light=new GameObject("Third map light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(55,-30,0);light.intensity=1.4f;
        var rt=new RenderTexture(1400,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);
    }
}


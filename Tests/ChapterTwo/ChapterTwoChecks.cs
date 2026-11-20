using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Houyi.NineSuns;
using Wuxia.Archery;

[InitializeOnLoad]
public static class ChapterTwoChecks
{
    static string Output => Path.GetFullPath(Path.Combine(Application.dataPath,"../../chapter-two-result.txt"));
    static ChapterTwoChecks(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ChapterTwoChecks",false)){SessionState.SetBool("ChapterTwoChecks",false);EditorApplication.delayCall+=Execute;}};}
    public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("ChapterTwoChecks",true);EditorApplication.EnterPlaymode();}
    static void Check(bool b,string message){if(!b)throw new Exception(message);}
    static void FinishDialogue(PrologueDirector story){int limit=20;while(story.DialogueOpen && limit-->0)story.AdvanceDialogue();Check(limit>0,"dialogue loop");}
    static PrologueDirector Story(){
        var root=new GameObject("Test story");root.SetActive(false);var s=root.AddComponent<PrologueDirector>();s.enabled=false;
        var hero=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ArcheryDemo/BlueRobeArcher.prefab"));hero.SetActive(false);
        var p=hero.AddComponent<ProloguePlayer>();p.enabled=false;p.archer=hero.GetComponent<ArcherDemo>();p.story=s;
        p.view=new GameObject("Test camera").AddComponent<Camera>();p.view.farClipPlane=300;
        s.player=p;s.targets=new PrologueTarget[0];s.beasts=new PrologueTarget[0];s.nineSuns=new GameObject("suns");
        root.SetActive(true);hero.SetActive(true);p.archer.autoDemo=false;p.archer.enabled=false;return s;
    }
    static void Tick(ChapterTwoDirector c){c.SendMessage("Update");}
    static void Set(object o,string field,object value){o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
    static T Get<T>(object o,string field){return (T)o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
    static void Execute(){try{
        ChapterTwoDirector.BossPreview=false;
        HouyiSave.AutoLoadEnabled=false;HouyiSave.DirectoryOverride=Path.Combine(Path.GetDirectoryName(Output),"test-saves");
        HouyiSave.Delete();
        HouyiSave.Write(new HouyiProgress{chapter=0,stage=1,targets=new bool[0],beasts=new bool[0]});
        HouyiSave.Write(new HouyiProgress{chapter=1,stage=0,actors=new bool[0]});
        HouyiSave.Write(new HouyiProgress{chapter=2,stage=4,actors=new bool[14]});
        Check(HouyiSave.TryRead(0,out var zero) && zero.stage==1 && HouyiSave.TryRead(1,out var one) && one.stage==0 && HouyiSave.TryRead(2,out var two) && two.stage==4,"independent chapter progress survives cross chapter writes");
        HouyiSave.Write(new HouyiProgress{chapter=1,stage=1,actors=new bool[0]});
        File.WriteAllText(HouyiSave.ChapterPath(1),"corrupt");
        Check(HouyiSave.TryRead(1,out one) && one.stage==0,"per chapter backup recovery");
        File.Delete(HouyiSave.ChapterPath(2));Check(HouyiSave.TryRead(2,out two) && File.Exists(HouyiSave.ChapterPath(2)),"legacy progress migrates into chapter slot");
        HouyiSave.DeleteChapter(1);Check(!HouyiSave.TryRead(1,out _) && HouyiSave.TryRead(0,out _) && HouyiSave.TryRead(2,out _),"resetting one chapter preserves other slots");
        HouyiSave.Delete();
        HouyiSave.Write(new HouyiProgress{chapter=2,stage=4,actors=new bool[14]});HouyiSave.AutoLoadEnabled=true;
        var menu=Story();menu.SendMessage("Start");
        Check(menu.Step==PrologueDirector.ChapterStep.Intro && !menu.InChapterTwo && !menu.CanControl,"startup waits for chapter selection despite existing save");
        HouyiSave.DeleteChapter(2);
        HouyiSave.AutoLoadEnabled=false;
        var cover=new GameObject("Loading test");cover.AddComponent<PrologueLoadingScreen>();
        Check(!PrologueLoadingScreen.IsLoading,"play mode has no loading delay");cover.SetActive(false);UnityEngine.Object.Destroy(cover);
        menu.SelectChapter(0);Check(menu.Step==PrologueDirector.ChapterStep.Village && menu.CanControl,"prologue selection enters prologue");
        var firstMenu=Story();firstMenu.SelectChapter(1);Check(firstMenu.InChapterOne && firstMenu.ChapterOne.Current==ChapterOneDirector.Stage.Village,"chapter one selection starts at beginning");firstMenu.ChapterOne.WorldRoot.gameObject.SetActive(false);
        var story=Story();story.SelectChapter(2);var c=story.ChapterTwo;
        Check(story.InChapterTwo && story.CanControl,"second chapter control");
        Check(c.Actors.Count==14,"stable actor ordering/count: "+c.Actors.Count);
        story.player.Teleport(c.Clue.position);c.Interact();FinishDialogue(story);Check(c.Current==ChapterTwoDirector.Stage.Rescue,"trail to rescue");
        var thrower=c.Actors.First(a=>a.Type==ChapterTwoActor.Kind.Thrower);
        story.player.Teleport(thrower.transform.position+Vector3.back*4);Set(thrower,"timer",0f);thrower.SendMessage("Update");
        Set(thrower,"timer",0f);thrower.SendMessage("Update");story.player.archer.BeginDraw();int health=story.Health;
        typeof(ChapterTwoDirector).GetMethod("TickStones",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{2f});
        Check(story.Health<health && !story.player.archer.IsShooting,"telegraphed stone damage interrupts shooting");
        var enemies=c.Actors.Where(a=>a.Encounter==c.Current && a.IsEnemy).ToArray();
        var target=enemies[0];int hit=c.SunburstHit(target.transform.position+Vector3.up,.9f,4);Check(hit>0,"Q hits second chapter enemies");
        foreach(var a in c.Actors.Where(a=>a.Encounter==ChapterTwoDirector.Stage.Rescue).ToArray())for(int i=0;i<6 && !a.Defeated;i++)c.Hit(a,1);
        Check(c.Actors.Where(a=>a.Type==ChapterTwoActor.Kind.Water).All(a=>a.Defeated && !a.LinkedVisual.activeSelf),"water extinguishes fire");
        story.player.Teleport(c.Captain.position);c.Interact();FinishDialogue(story);Check(c.Current==ChapterTwoDirector.Stage.Cores,"rescue to cores");
        var cores=c.Actors.Where(a=>a.Type==ChapterTwoActor.Kind.Core).ToArray();c.Hit(cores[0],.2f);Check(!cores[0].Defeated,"cores require full charge");
        foreach(var a in cores)c.Hit(a,1);Tick(c);Check(c.Current==ChapterTwoDirector.Stage.Approach && !c.Gate.activeSelf,"gate opening");
        story.player.Teleport(c.Point(0,146));Tick(c);Check(c.Current==ChapterTwoDirector.Stage.Boss && c.ArenaGate.activeSelf,"arena entry");
        c.Hit(c.Boss,1);Check(c.Boss.Health==32,"boss armor outside window");
        c.Boss.Stagger(2);c.Hit(c.Boss,1);Check(c.Boss.Health==28,"boss vulnerable damage");
        Set(c.Boss,"state",1);Set(c.Boss,"attack",1);Set(c.Boss,"timer",1f);
        float bossHP=c.Boss.Health;c.Boss.OnArrowHit(1);Check(c.Boss.Health==bossHP,"boss chest must be aimed at during throw");
        c.Boss.HitWeakPoint(1);c.Boss.OnArrowHit(1);Check(c.Boss.Health==bossHP-4,"weak point interrupts throw without duplicate parent damage");
        c.Boss.TakeDamage(8);Set(c.Boss,"state",0);Set(c.Boss,"timer",0f);Set(c.Boss,"cycle",2);c.Boss.SendMessage("Update");
        Check(c.Actors.Count==14 && c.Actors.Any(a=>a.BossReinforcement && a.gameObject.activeSelf),"half health activates fixed reinforcements");
        Check(c.WorldRoot.GetComponentsInChildren<Transform>().Any(t=>t.name=="火环安全缺口"),"ring has visible safe gap");
        Check(Mathf.Abs(Get<float>(c.Boss,"timer")-2)<.001f,"ring telegraph lasts two seconds");
        Vector3 ringCenter=Get<Vector3>(c.Boss,"ringCenter"),gap=Get<Vector3>(c.Boss,"ringGap");
        Vector3 original=story.player.transform.position-ringCenter;original.y=0;
        Check(Vector3.Angle(original,gap)>30,"ring must not give player an automatic safe gap");
        story.player.Teleport(ringCenter+gap*8);c.Boss.SendMessage("Update");
        Check(Get<Vector3>(c.Boss,"ringGap")==gap,"telegraph gap stays fixed after movement");
        health=story.Health;Set(c.Boss,"timer",0f);c.Boss.SendMessage("Update");
        Check(story.Health==health,"safe sector prevents release damage");
        story.player.Teleport(ringCenter-gap*8);Set(c.Boss,"dealt",false);
        typeof(ChapterTwoActor).GetMethod("ReleaseRing",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c.Boss,null);
        Check(story.Health==health-22,"unsafe sector takes release damage anywhere inside radius");
        typeof(ChapterTwoActor).GetMethod("ReleaseRing",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c.Boss,null);
        Check(story.Health==health-22,"one ring causes damage only once");
        story.player.Teleport(ringCenter-gap*19);Set(c.Boss,"dealt",false);
        typeof(ChapterTwoActor).GetMethod("ReleaseRing",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c.Boss,null);
        Check(story.Health==health-22,"outside ring radius is unaffected");
        c.Retry();Check(c.Boss.Health==32 && !c.Actors.Any(a=>a.BossReinforcement && a.gameObject.activeSelf),"retry resets boss and reserves");
        c.Boss.Stagger(100);for(int i=0;i<8;i++)c.Hit(c.Boss,1);Check(c.Current==ChapterTwoDirector.Stage.Revelation,"boss defeat advances");
        story.player.Teleport(c.Altar.position);c.Interact();FinishDialogue(story);Check(c.Finished && !story.CanControl,"ending completion");
        var save=c.CaptureProgress();Check(HouyiSave.Write(save) && HouyiSave.TryRead(out var read) && read.chapter==2,"chapter two save validation");
        var selected=Story();selected.SelectChapter(2);Check(selected.InChapterTwo && selected.ChapterTwo.Finished,"chapter selection continues existing chapter progress");selected.ChapterTwo.WorldRoot.gameObject.SetActive(false);
        var resumed=Story();Check(resumed.RestoreProgress(save) && resumed.InChapterTwo && resumed.ChapterTwo.Finished,"director loads chapter two save");resumed.ChapterTwo.WorldRoot.gameObject.SetActive(false);
        var other=Story();other.StartChapterTwo();Check(other.ChapterTwo.RestoreProgress(save) && other.ChapterTwo.Finished,"restore completed chapter");
        save.stage=2;save.checkpoint=c.Point(0,78);Check(other.ChapterTwo.RestoreProgress(save),"restore intermediate checkpoint");
        Check(!other.ChapterTwo.RestoreProgress(new HouyiProgress{stage=2,actors=new bool[1]}),"reject mismatched save");
        other.ChapterTwo.WorldRoot.gameObject.SetActive(false);
        var transition=Story();transition.StartChapterOne();var old=transition.ChapterOne.WorldRoot;
        Check(transition.ChapterOne.Current==ChapterOneDirector.Stage.Village,"chapter one normal entry preserved");
        transition.ChapterOne.EnterBossPreview();
        Check(transition.ChapterOne.Current==ChapterOneDirector.Stage.Boss && transition.ChapterOne.Boss.Health==transition.ChapterOne.Boss.MaxHealth && transition.Health==100,"chapter one test button starts healthy boss fight");
        Check(!transition.ChapterOne.WeakPoint.gameObject.activeSelf && transition.ChapterOne.ArenaGate.activeSelf && !transition.ChapterOne.Gate.activeSelf,"chapter one arena and weakpoint initialized");
        Check(transition.ChapterOne.Actors.Where(a=>a.Encounter!=ChapterOneDirector.Stage.Boss).All(a=>!a.gameObject.activeSelf),"chapter one previous encounters hidden");
        transition.StartChapterTwo();
        Check(transition.InChapterTwo && !transition.InChapterOne && !old.gameObject.activeSelf,"chapter one to two transition disables old world");transition.ChapterTwo.WorldRoot.gameObject.SetActive(false);
        Check(HouyiSave.Write(new HouyiProgress{chapter=1,stage=8,actors=new bool[0]}) && HouyiSave.TryRead(out var legacy) && legacy.chapter==1,"legacy chapter one save remains readable");
        var manualJump=Story();manualJump.StartChapterTwo();
        Check(manualJump.ChapterTwo.Current==ChapterTwoDirector.Stage.Trail,"normal entry remains at chapter start");
        manualJump.ChapterTwo.EnterBossPreview();
        Check(manualJump.ChapterTwo.Current==ChapterTwoDirector.Stage.Boss && !ChapterTwoDirector.BossPreview && manualJump.Health==100,"test button jumps to boss without enabling automatic preview");
        manualJump.ChapterTwo.WorldRoot.gameObject.SetActive(false);
        ChapterTwoDirector.BossPreview=true;
        var preview=Story();preview.StartChapterTwo();
        Check(preview.ChapterTwo.Current==ChapterTwoDirector.Stage.Boss && preview.CanControl,"boss preview starts combat immediately");
        Check(preview.ChapterTwo.Actors.Where(a=>a.Encounter!=ChapterTwoDirector.Stage.Boss).All(a=>!a.gameObject.activeSelf),"preview disables earlier enemies and targets");
        var player=preview.player;var archer=player.archer;
        Check(player.TryBeginAttack(),"basic attack starts");archer.Tick(.1f);float charge=archer.Charge;
        Check(!player.TryBeginAttack() && archer.Charge==charge,"basic attack cannot restart itself");
        Check(player.TryDodge(Vector2.right) && !archer.IsShooting,"dodge interrupts basic attack");
        float dodgeTime=Get<float>(player,"dodgeRemaining");
        Check(!player.TryDodge(Vector2.up) && Get<float>(player,"dodgeRemaining")==dodgeTime,"dodge cannot restart itself");
        Check(Mathf.Abs(player.DodgeCooldownRemaining-.5f)<.01f,"dodge cooldown is half a second");
        Check(player.TryBeginAttack() && Get<float>(player,"dodgeRemaining")==0,"basic attack interrupts dodge");
        Check(!player.TryDodge(Vector2.up) && archer.IsShooting,"failed cooldown dodge must preserve attack");
        archer.Tick(.6f);archer.ReleaseDraw();archer.Tick(.01f);
        Check(!player.TryBeginAttack() && archer.CurrentPhase==ArcherDemo.Phase.ReleaseArrow,"basic attack does not interrupt its own release");
        archer.InterruptByHit();Check(player.TryBeginAttack() && !archer.IsHitStunned,"basic attack interrupts hit reaction");
        archer.InterruptByHit();Set(player,"nextDodgeTime",Time.time);Check(player.TryDodge(Vector2.up) && !archer.IsHitStunned,"dodge interrupts hit reaction");
        player.RestoreStamina();
        Check(player.TickStamina(true,2) && Mathf.Abs(player.Stamina-90)<.001f,"sprint costs five stamina per second");
        player.TickStamina(false,.5f);Check(player.Stamina==90,"recovery waits after consumption");
        player.TickStamina(false,.5f);Check(player.Stamina==90,"recovery delay is one second");
        player.TickStamina(false,.5f);Check(player.Stamina==95,"recovery is ten per second");
        player.TickStamina(false,10);Check(player.Stamina==100,"stamina capped at one hundred");
        archer.CancelDraw();
        for(int i=0;i<5;i++){Set(player,"dodgeRemaining",0f);Set(player,"nextDodgeTime",Time.time);Check(player.TryDodge(Vector2.up),"affordable dodge accepted");Check(player.Stamina==100-(i+1)*20,"each dodge costs twenty");}
        Set(player,"dodgeRemaining",0f);Set(player,"nextDodgeTime",Time.time);player.TryBeginAttack();
        Check(!player.TryDodge(Vector2.up) && player.Stamina==0 && archer.IsShooting,"insufficient stamina neither spends nor cancels attack");
        player.RestoreStamina();player.TickStamina(true,40);Check(player.Stamina==0,"sprint cannot make stamina negative");
        Check(!player.TickStamina(true,1) && !player.TickStamina(true,1) && player.Stamina==10,"exhausted sprint stops while stamina recovers");
        player.TickStamina(false,0);Check(player.TickStamina(true,1) && player.Stamina==5,"sprint can resume after releasing sprint input");
        player.RestoreStamina();archer.CancelDraw();
        var oldProgress=new HouyiProgress{chapter=2,stage=0,checkpoint=preview.ChapterTwo.Point(0,0),actors=new bool[14]};
        var previewRestore=Story();Check(previewRestore.RestoreProgress(oldProgress),"old chapter two save can enter preview");
        Check(previewRestore.ChapterTwo.Current==ChapterTwoDirector.Stage.Boss && Vector3.Distance(previewRestore.player.transform.position,previewRestore.ChapterTwo.Point(0,150))<.1f,"old save cannot return to earlier tasks");
        preview.ChapterTwo.WorldRoot.gameObject.SetActive(false);previewRestore.ChapterTwo.WorldRoot.gameObject.SetActive(false);
        ChapterThreeChecks.Run(Path.GetDirectoryName(Output));
        Render(c,story.player.view);
        File.WriteAllText(Output,"PASS: chapter/ring regression; attack interrupts dodge/hit; dodge interrupts attack/hit; no self interruption; failed dodge preserves attack; dodge cooldown 0.5 seconds; stamina sprint/dodge costs, delay/recovery/cap/exhaustion and insufficient-stamina action preservation.");EditorApplication.Exit(0);
    }catch(Exception e){File.WriteAllText(Output,"FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);}}
    static void Render(ChapterTwoDirector c,Camera camera){
        camera.transform.position=c.Point(22,20,24);camera.transform.LookAt(c.Point(-2,57,0));camera.backgroundColor=new Color(.16f,.13f,.1f);camera.clearFlags=CameraClearFlags.SolidColor;
        var light=new GameObject("Test light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-30,0);light.intensity=1.4f;
        var rt=new RenderTexture(1200,760,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var img=new Texture2D(1200,760,TextureFormat.RGB24,false);img.ReadPixels(new Rect(0,0,1200,760),0,0);img.Apply();File.WriteAllBytes(Output.Replace("chapter-two-result.txt","chapter-two-map.png"),img.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;
    }
}

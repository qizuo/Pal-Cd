using System.Collections.Generic;
using UnityEngine;

namespace Houyi.NineSuns
{
    public sealed class ChapterTwoDirector : MonoBehaviour
    {
        // Temporary boss playtest entry; switch off to restore the full chapter.
        public static bool BossPreview = false;
        public enum Stage { Trail, Rescue, Cores, Approach, Boss, Revelation, Complete }
        public Stage Current { get; private set; }
        public bool Finished => Current == Stage.Complete;
        public PrologueDirector Story { get; private set; }
        public Transform WorldRoot, Clue, Captain, Altar, Supply, Lore;
        public GameObject Gate, ArenaGate;
        public ChapterTwoActor Boss;
        public readonly List<ChapterTwoActor> Actors = new List<ChapterTwoActor>();
        public int CluesFound { get; private set; }
        public bool SupplyTaken, LoreRead;
        public string Warning;
        Vector3 checkpoint;
        readonly List<Stone> stones = new List<Stone>();
        sealed class Stone { public GameObject visual, warning; public Vector3 from, to; public float age; }
        static readonly string[] objectives = {
            "调查山道上的运水队路标，追踪扶桑碎片", "射破两只悬挂水囊灭火，击退投火山魈与兽群，救出队员",
            "满蓄力击碎三枚扶桑火核，开启古道", "沿古道前往山顶祭场", "击败赤焰山魈：躲扑击、射胸口、穿过火环缺口",
            "调查祭坛，让赤红碎片与扶桑残碑共鸣", "第 2 章完成 · 天火追兽"
        };
        public string Objective => objectives[(int)Current];
        public string Progress => Current == Stage.Rescue ? "水囊 " + Defeated(ChapterTwoActor.Kind.Water) + "/2 · 灾兽 " + RemainingEnemies(Stage.Rescue)
            : Current == Stage.Cores ? "火核 " + Defeated(ChapterTwoActor.Kind.Core) + "/3" : Current == Stage.Boss ? Warning ?? "胸口亮起时，满蓄力可打断投石" : "支路可寻找补给与扶桑残碑";
        public string GoalLabel => Current == Stage.Trail ? "运水队路标" : Current == Stage.Rescue ? RescueReady ? "运水队长" : "水囊 / 灾兽"
            : Current == Stage.Cores ? "扶桑火核" : Current == Stage.Boss ? "赤焰山魈" : "山顶祭坛";
        public Transform Goal {
            get {
                if (Current == Stage.Trail) return Clue;
                if (Current == Stage.Rescue && RescueReady) return Captain;
                if (Current == Stage.Rescue || Current == Stage.Cores)
                    foreach (var a in Actors) if (a.Encounter == Current && !a.Defeated) return a.transform;
                return Current == Stage.Boss ? Boss.transform : Finished ? null : Altar;
            }
        }
        int Defeated(ChapterTwoActor.Kind kind) { int n=0; foreach(var a in Actors) if(a.Type==kind && a.Defeated)n++; return n; }
        int RemainingEnemies(Stage stage) { int n=0;foreach(var a in Actors)if(a.Encounter==stage && a.IsEnemy && !a.Defeated)n++;return n; }
        bool RescueReady => Defeated(ChapterTwoActor.Kind.Water)==2 && RemainingEnemies(Stage.Rescue)==0;
        bool Near(Transform t) => t && Vector3.Distance(Story.player.transform.position,t.position)<3.5f;
        Transform InteractionTarget => Current==Stage.Trail && Near(Clue) ? Clue : Current==Stage.Rescue && RescueReady && Near(Captain) ? Captain
            : Current==Stage.Revelation && Near(Altar) ? Altar : !SupplyTaken && Near(Supply) ? Supply : !LoreRead && Near(Lore) ? Lore : null;
        public bool CanInteract => InteractionTarget;
        public string InteractionLabel => InteractionTarget==Supply ? "取用补给" : InteractionTarget==Lore ? "阅读残碑" : Current==Stage.Trail ? "调查路标" : Current==Stage.Revelation ? "碎片共鸣" : "交谈";
        public Vector3 Point(float x,float z,float y=0) => WorldRoot.position+new Vector3(x,y,z);
        public void Initialize(PrologueDirector story) {
            Story=story; WorldRoot=new GameObject("第 2 章 · 逐火入山").transform;WorldRoot.position=new Vector3(420,0,0);
            ChapterTwoWorld.Build(this);Story.ApplyWhiteLighting();
            if(Story.nineSuns)Story.nineSuns.SetActive(true);
            if(BossPreview){EnterBossPreview();return;}
            Story.player.yaw=0;Story.player.pitch=8;Story.player.Teleport(Point(0,0));
            SetStage(Stage.Trail,Point(0,0));
            Story.Notify("第 2 章 · 逐火入山。循扶桑碎片的赤光，寻找失踪队员。",7);
        }
        public void EnterBossPreview(){
            ClearStones();Warning=null;
            Story.player.archer.CancelForAction();
            if(Story.player.Sunburst)Story.player.Sunburst.CancelSelection();
            foreach(var a in Actors){
                if(a.Encounter!=Stage.Boss)a.gameObject.SetActive(false);
                else {a.ResetEncounter();if(a.BossReinforcement)a.gameObject.SetActive(false);}
                if(a.LinkedVisual)a.LinkedVisual.SetActive(false);
            }
            foreach(var t in new[]{Clue,Captain,Supply,Lore})if(t)t.gameObject.SetActive(false);
            Gate.SetActive(false);ArenaGate.SetActive(true);
            Story.player.yaw=0;Story.player.pitch=8;Story.player.Teleport(Point(0,150));
            SetStage(Stage.Boss,Point(0,150));
            if(Story.player.Sunburst)Story.player.Sunburst.RestoreCooldown(0);
            Story.Notify("Boss 试玩：前置任务已跳过。赤焰山魈半血后释放火环，注意地面 2 秒预警！",7);
        }
        void SetStage(Stage stage,Vector3 safe) { Current=stage;checkpoint=safe;Story.ChapterCheckpoint(safe);Story.Notify(Objective,7);Story.SaveProgress(); }
        void Update() {
            if(!Story || !Story.CanControl)return;
            TickStones(Time.deltaTime);
            if(Current==Stage.Cores && Defeated(ChapterTwoActor.Kind.Core)==3) {Gate.SetActive(false);SetStage(Stage.Approach,Point(0,114));}
            if(Current==Stage.Approach && Story.player.transform.position.z>Point(0,145).z) {ArenaGate.SetActive(true);SetStage(Stage.Boss,Point(0,145));}
        }
        public void Interact() {
            var t=InteractionTarget;if(!t)return;
            if(t==Supply) {SupplyTaken=true;Supply.gameObject.SetActive(false);Story.ChapterCheckpoint(checkpoint);Story.SaveProgress();Story.Notify("饮下清水，气血恢复。补给只能使用一次。");return;}
            if(t==Lore) {LoreRead=true;Story.Talk(new[]{"扶桑残碑|火借日行，兽逐热逃。赤心明灭之时，断其焚石。","后羿|它们不是追着火走，而是被天火驱赶……"},Story.SaveProgress);return;}
            if(Current==Stage.Trail) {
                CluesFound=1;
                Story.Talk(new[]{"断裂路标|泉水未能扑灭山火，剩下的运水队员被困在东侧燃林。","后羿|水囊还挂在树上。射断它们，先救人！"},()=>SetStage(Stage.Rescue,Point(0,23)));
            } else if(Current==Stage.Rescue) {
                Story.Talk(new[]{"运水队长|火灭了！山魈一直在把兽群往山上赶。古道被三枚火核堵住了。","后羿|你带大家下山。那些火核，与泉底的碎片同出一源。"},()=>SetStage(Stage.Cores,Point(0,78)));
            } else if(Current==Stage.Revelation) {
                Story.Talk(new[]{"赤焰山魈|不是我们点燃了山……是天上的火，在追我们。","后羿|守山者也在逃命。九日之劫，竟已深入山海。","扶桑残碑|赤日碎片在碑前展开光影，云层之后，第二轮太阳正缓缓睁开。","后羿|下一步，去扶桑遗址。我要找到操纵这场天火的人。"},()=>{Current=Stage.Complete;ClearStones();Story.player.archer.CancelDraw();Story.SaveProgress();});
            }
        }
        public bool Active(ChapterTwoActor a) => Story && Story.CanControl && a.Encounter==Current && !a.Defeated;
        public void Hit(ChapterTwoActor a,float charge) {
            if(!Active(a))return;
            if(a.Type==ChapterTwoActor.Kind.Core && charge<.8f){Story.Notify("火核需要满蓄力击碎。");return;}
            if(a.Type==ChapterTwoActor.Kind.Boss) {
                if(!a.Exposed){Story.Notify("等待扑击后的破绽，或在投石蓄力时射击胸口。");return;}
                a.TakeDamage(charge>=.8f ? 4 : 1);
                if(charge>=.8f)a.Stagger(1.8f);
            } else a.TakeDamage(a.Type==ChapterTwoActor.Kind.Core || a.Type==ChapterTwoActor.Kind.Water ? a.MaxHealth : charge>=.8f ? 3 : 1);
            if(a.Defeated)OnDefeated(a);else if(a.Type==ChapterTwoActor.Kind.Thrower)a.Stagger(.8f);
        }
        void OnDefeated(ChapterTwoActor a) {
            if(a.Type==ChapterTwoActor.Kind.Water){if(a.LinkedVisual)a.LinkedVisual.SetActive(false);Story.Notify("水囊破裂，火势熄灭！");}
            if(a==Boss){ArenaGate.SetActive(false);ClearStones();Warning=null;SetStage(Stage.Revelation,Point(0,145));}
            Story.SaveProgress();
        }
        public int SunburstHit(Vector3 center,float charge,float radius) {
            if(!Story.CanControl)return 0;int n=0;
            foreach(var a in Actors) {
                if(!a.IsEnemy || !Active(a) || (a==Boss && !a.Exposed))continue;
                var col=a.GetComponent<Collider>();var p=col ? col.ClosestPoint(center) : a.transform.position;
                if((p-center).sqrMagnitude>radius*radius)continue;
                a.TakeDamage(charge>=.8f ? 4 : 2);n++;if(a.Defeated)OnDefeated(a);else a.Stagger(.65f);
            }return n;
        }
        public void ThrowStone(Vector3 from,Vector3 target) {
            target.y=.06f;
            stones.Add(new Stone{from=from,to=target,visual=ChapterTwoWorld.Shape(this,PrimitiveType.Sphere,"飞掷火岩",from,Vector3.one*.65f,ChapterTwoWorld.Fire,false),
                warning=ChapterTwoWorld.Shape(this,PrimitiveType.Cylinder,"火岩落点",target,new Vector3(3.8f,.015f,3.8f),ChapterTwoWorld.Fire,false)});
        }
        void TickStones(float dt) {
            for(int i=stones.Count-1;i>=0;i--){var s=stones[i];s.age+=dt;float t=Mathf.Clamp01(s.age/1.35f);
                s.visual.transform.position=Vector3.Lerp(s.from,s.to,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*4;
                if(t<1)continue;Vector3 d=Story.player.transform.position-s.to;d.y=0;if(d.magnitude<1.9f)Story.Hurt(18);
                Destroy(s.visual);Destroy(s.warning);stones.RemoveAt(i);}
        }
        public HouyiProgress CaptureProgress(){var flags=new bool[Actors.Count];for(int i=0;i<flags.Length;i++)flags[i]=Actors[i].Defeated;
            return new HouyiProgress{chapter=2,stage=(int)Current,checkpoint=checkpoint,actors=flags,cluesFound=CluesFound,supplyTaken=SupplyTaken,loreRead=LoreRead};}
        public bool RestoreProgress(HouyiProgress p){
            if(BossPreview){EnterBossPreview();return true;}
            if(p.actors==null || p.actors.Length!=Actors.Count || p.stage<0 || p.stage>6)return false;
            Current=(Stage)p.stage;checkpoint=p.checkpoint;CluesFound=p.cluesFound;SupplyTaken=p.supplyTaken;LoreRead=p.loreRead;
            for(int i=0;i<Actors.Count;i++)if(p.actors[i]){Actors[i].TakeDamage(Actors[i].MaxHealth);if(Actors[i].LinkedVisual)Actors[i].LinkedVisual.SetActive(false);}
            Gate.SetActive(Current<Stage.Approach);ArenaGate.SetActive(Current==Stage.Boss);Supply.gameObject.SetActive(!SupplyTaken);ClearStones();return true;
        }
        public void Retry(){ClearStones();Warning=null;foreach(var a in Actors)if(a.Encounter==Current && !a.Defeated)a.ResetEncounter();
            if(Current==Stage.Boss){Boss.ResetEncounter();foreach(var a in Actors)if(a.BossReinforcement && !a.Defeated)a.gameObject.SetActive(false);}Story.player.Teleport(checkpoint);Story.ChapterCheckpoint(checkpoint);}
        void ClearStones(){foreach(var s in stones){if(s.visual)Destroy(s.visual);if(s.warning)Destroy(s.warning);}stones.Clear();}
        public void DrawHUD(){if(Current!=Stage.Boss || !Story.CanControl)return;
            var style=new GUIStyle(GUI.skin.label){font=Story.chineseFont,fontSize=25,alignment=TextAnchor.MiddleCenter};Color old=GUI.color;
            GUI.color=new Color(.08f,.03f,.02f,.92f);GUI.DrawTexture(new Rect(610,120,700,75),Texture2D.whiteTexture);
            GUI.color=new Color(.9f,.25f,.06f);GUI.DrawTexture(new Rect(630,179,660*Boss.Health/Boss.MaxHealth,7),Texture2D.whiteTexture);GUI.color=old;
            GUI.Label(new Rect(610,125,700,45),"赤焰山魈  "+Boss.Health+" / "+Boss.MaxHealth,style);GUI.Label(new Rect(460,205,1000,55),Warning??"扑击后反击 · 投石时射胸口 · 火环有缺口",style);}
        void OnDestroy(){ClearStones();if(WorldRoot)Destroy(WorldRoot.gameObject);}
    }
}

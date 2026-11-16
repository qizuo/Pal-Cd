using UnityEngine;

namespace Houyi.NineSuns
{
    public sealed class ChapterTwoActor : MonoBehaviour
    {
        public enum Kind { Beast, Thrower, Water, Core, Boss }
        public Kind Type;
        public ChapterTwoDirector Chapter;
        public ChapterTwoDirector.Stage Encounter;
        public GameObject LinkedVisual;
        public bool BossReinforcement;
        public int MaxHealth=3;
        public float Health {get;private set;}
        public bool Defeated=>Health<=0;
        public bool IsEnemy=>Type==Kind.Beast || Type==Kind.Thrower || Type==Kind.Boss;
        public bool Exposed=>Type==Kind.Boss && (state==2 || (state==1 && attack==1));
        Vector3 home,locked,target,ringGap=Vector3.forward;
        Vector3 ringCenter;
        const float RingRadius=17, RingPreviewRadius=4, RingHalfGap=30, RingWindup=2, RingRelease=1.2f;
        int ringCastCount;
        Mesh safeSectorMesh;
        Quaternion homeRotation;
        float timer=1.4f;
        int state,attack,cycle;
        bool dealt,addsSpawned;
        int weakPointHitFrame=-1;
        GameObject warning,core;
        public void SetHomeHere(){home=transform.position;}
        public void Initialize(ChapterTwoDirector chapter,Kind kind,ChapterTwoDirector.Stage stage,int health){Chapter=chapter;Type=kind;Encounter=stage;MaxHealth=health;
            home=transform.position;homeRotation=transform.rotation;ResetEncounter();chapter.Actors.Add(this);
            if(kind==Kind.Boss)core=ChapterTwoWorld.Shape(chapter,PrimitiveType.Sphere,"胸口火核",transform.position+Vector3.up*2.2f+Vector3.forward*1.5f,Vector3.one*.9f,ChapterTwoWorld.Fire);
            if(core){core.transform.SetParent(transform,true);core.AddComponent<ChapterTwoWeakPoint>().boss=this;core.GetComponent<Collider>().enabled=false;core.GetComponent<Renderer>().enabled=false;}
        }
        public void ResetEncounter(){Health=MaxHealth;state=cycle=0;timer=1.4f;dealt=false;addsSpawned=false;transform.position=home;transform.rotation=homeRotation;gameObject.SetActive(true);if(warning)warning.SetActive(false);foreach(var c in GetComponents<Collider>())c.enabled=true;}
        public void OnArrowHit(float charge){if(weakPointHitFrame==Time.frameCount)return;if(Type==Kind.Boss && state==1){Chapter.Story.Notify("胸口火核正在亮起，瞄准发光部位！");return;}Chapter.Hit(this,charge);}
        public void HitWeakPoint(float charge){if(!Exposed || !Chapter.Active(this))return;weakPointHitFrame=Time.frameCount;if(charge>=.8f && Chapter.Story.PrecisionRecoveryUnlocked)Chapter.Story.player.RecoverStamina(10);Chapter.Hit(this,charge);}
        public void TakeDamage(int amount){if(Defeated)return;Health=Mathf.Max(0,Health-amount);if(Defeated){foreach(var c in GetComponents<Collider>())c.enabled=false;if(warning)warning.SetActive(false);}}
        public void Stagger(float seconds){state=2;timer=seconds;if(warning)warning.SetActive(false);}
        void Update(){
            if(Defeated){transform.localScale=Vector3.Lerp(transform.localScale,Vector3.zero,Time.deltaTime*4);return;}
            if(!IsEnemy || !Chapter.Active(this))return;
            if(core){core.GetComponent<Renderer>().enabled=Exposed;core.GetComponent<Collider>().enabled=Exposed;}
            Vector3 delta=Chapter.Story.player.transform.position-transform.position;delta.y=0;float distance=delta.magnitude;
            timer-=Time.deltaTime;
            if(state==0){
                if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*5);
                if(Type==Kind.Beast && distance>2 && distance<26)transform.position+=delta.normalized*Time.deltaTime*2.7f;
                if(timer>0 || (Type==Kind.Beast && distance>5) || (Type==Kind.Thrower && distance>32))return;
                attack=Type==Kind.Boss ? cycle++%3 : Type==Kind.Thrower ? 1 : 0;
                if(Type==Kind.Boss && attack==2 && Health>MaxHealth/2)attack=0;
                state=1;timer=Type==Kind.Boss ? 1.25f : .95f;target=Chapter.Story.player.transform.position;locked=delta.normalized;dealt=false;
                if(attack==2){
                    timer=RingWindup;
                    ringCenter=transform.position;
                    // Put the player outside the gap, then lock it for the entire cast.
                    Vector3 direction=delta.sqrMagnitude>.01f ? delta.normalized : Vector3.forward;
                    ringGap=Quaternion.Euler(0,ringCastCount++%2==0 ? 50 : -50,0)*direction;
                    Chapter.Warning="火环预警 · 2 秒内移动到蓝色缺口";
                    ShowRing(true);
                }
                else {Chapter.Warning=Type==Kind.Boss ? attack==0 ? "扑击预警 · 侧闪后蓄力反击" : "投石蓄力 · 满蓄力射击胸口可打断" : Chapter.Warning;ShowWarning();}
            }else if(state==1 && timer<=0){
                if(warning)warning.SetActive(false);
                if(attack==1){Chapter.ThrowStone(transform.position+Vector3.up*2.5f,target);Stagger(Type==Kind.Boss ? 2.3f : 1.8f);}
                else if(attack==2){state=4;timer=RingRelease;Chapter.Warning="火环释放！";ReleaseRing();ShowRing(false);}
                else {state=3;timer=Type==Kind.Boss ? .5f : .3f;}
            }else if(state==3){
                transform.position+=locked*Time.deltaTime*(Type==Kind.Boss ? 16 : 10);
                Vector3 p=transform.position-Chapter.WorldRoot.position;p.x=Mathf.Clamp(p.x,-17,17);p.z=Mathf.Clamp(p.z,Type==Kind.Boss ? 143 : 0,187);transform.position=Chapter.WorldRoot.position+p;
                if(!dealt && distance<(Type==Kind.Boss ? 2.3f : 1.4f)){Chapter.Story.Hurt(Type==Kind.Boss ? 24 : 10);dealt=true;}
                if(timer<=0)Stagger(Type==Kind.Boss ? 2.8f : 1.4f);
            }else if(state==4){
                UpdateRing(Mathf.Lerp(RingPreviewRadius,RingRadius,Mathf.Clamp01(1-timer/RingRelease)));
                if(timer<=0){if(warning)warning.SetActive(false);Stagger(2.0f);}
            }else if(state==2 && timer<=0){state=0;timer=.6f;if(Type==Kind.Boss)Chapter.Warning=null;}
            if(Type==Kind.Boss && Health<=MaxHealth/2 && !addsSpawned){addsSpawned=true;ChapterTwoWorld.SpawnBossAdds(Chapter);}
        }
        void ShowWarning(){if(!warning)warning=new GameObject("攻击预警");warning.transform.SetParent(Chapter.WorldRoot);
            foreach(Transform child in warning.transform)Destroy(child.gameObject);
            warning.SetActive(true);
            Vector3 p=attack==1 ? target : transform.position+locked*4;p.y=.05f;
            var shape=ChapterTwoWorld.Shape(Chapter,attack==1 ? PrimitiveType.Cylinder : PrimitiveType.Cube,"红色攻击范围",p,attack==1 ? new Vector3(3.8f,.015f,3.8f) : new Vector3(2,.025f,8),ChapterTwoWorld.Fire,false);
            shape.transform.rotation=Quaternion.LookRotation(locked.sqrMagnitude>.01f ? locked : Vector3.forward);shape.transform.SetParent(warning.transform,true);
        }
        void ReleaseRing(){
            if(dealt)return;
            dealt=true;
            Vector3 d=Chapter.Story.player.transform.position-ringCenter;d.y=0;
            bool safe=d.sqrMagnitude>.001f && Vector3.Angle(ringGap,d)<=RingHalfGap;
            if(d.sqrMagnitude<=RingRadius*RingRadius && !safe)Chapter.Story.Hurt(22);
        }
        void ShowRing(bool windup){
            if(warning)Destroy(warning);
            if(safeSectorMesh)Destroy(safeSectorMesh);
            warning=new GameObject(windup ? "火环地面预警 · 2 秒" : "火环范围释放");warning.transform.SetParent(Chapter.WorldRoot);warning.transform.position=ringCenter;
            if(windup){
                Quaternion rotation=Quaternion.LookRotation(ringGap);
                var outline=new GameObject("带缺口的地面圆环");outline.transform.SetParent(warning.transform,false);
                var line=outline.AddComponent<LineRenderer>();line.sharedMaterial=ChapterTwoWorld.Fire;line.useWorldSpace=false;line.positionCount=65;line.startWidth=line.endWidth=.22f;
                for(int i=0;i<65;i++){float a=(RingHalfGap+i*(360-2*RingHalfGap)/64)*Mathf.Deg2Rad;line.SetPosition(i,rotation*new Vector3(Mathf.Sin(a)*RingPreviewRadius,.055f,Mathf.Cos(a)*RingPreviewRadius));}
                var safe=new GameObject("火环安全缺口");safe.transform.SetParent(warning.transform,false);
                var vertices=new Vector3[18];var triangles=new int[48];vertices[0]=Vector3.up*.045f;
                for(int i=0;i<=16;i++){float a=(-RingHalfGap+i*2*RingHalfGap/16)*Mathf.Deg2Rad;vertices[i+1]=rotation*new Vector3(Mathf.Sin(a)*RingPreviewRadius,.045f,Mathf.Cos(a)*RingPreviewRadius);if(i<16){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}}
                safeSectorMesh=new Mesh{name="火环固定安全扇区"};safeSectorMesh.vertices=vertices;safeSectorMesh.triangles=triangles;safeSectorMesh.RecalculateNormals();
                safe.AddComponent<MeshFilter>().sharedMesh=safeSectorMesh;safe.AddComponent<MeshRenderer>().sharedMaterial=ChapterTwoWorld.Water;
            }else{
                for(int i=0;i<55;i++){var bit=ChapterTwoWorld.Shape(Chapter,PrimitiveType.Sphere,"火环",ringCenter,Vector3.one*.85f,ChapterTwoWorld.Fire,false);bit.transform.SetParent(warning.transform,true);}
                UpdateRing(RingPreviewRadius);
            }
        }
        void UpdateRing(float radius){if(!warning)return;int i=0;Quaternion rotation=Quaternion.LookRotation(ringGap);foreach(Transform child in warning.transform){if(child.name!="火环")continue;float a=(RingHalfGap+i++*(360-2*RingHalfGap)/54)*Mathf.Deg2Rad;child.position=ringCenter+rotation*new Vector3(Mathf.Sin(a)*radius,.4f,Mathf.Cos(a)*radius);}}
        void OnDestroy(){if(warning)Destroy(warning);if(safeSectorMesh)Destroy(safeSectorMesh);}
    }
    public sealed class ChapterTwoWeakPoint : MonoBehaviour
    {
        public ChapterTwoActor boss;
        public void OnArrowHit(float charge){if(boss)boss.HitWeakPoint(charge);}
    }
}

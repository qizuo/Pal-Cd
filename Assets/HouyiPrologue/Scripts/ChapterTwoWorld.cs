using UnityEngine;

namespace Houyi.NineSuns
{
    public static class ChapterTwoWorld
    {
        public static Material Fire,Water;
        static Material earth,road,wood,stone,leaf,cloth;
        public static GameObject Shape(ChapterTwoDirector c,PrimitiveType type,string name,Vector3 p,Vector3 size,Material mat,bool collision=true)
            => ChapterOneWorld.Shape(c.WorldRoot,type,name,p,size,mat,collision);
        static Material Mat(string name,Color color){var m=new Material(Shader.Find("Standard")){name="Chapter Two / "+name,color=color};m.SetFloat("_Glossiness",.12f);return m;}
        static GameObject Box(ChapterTwoDirector c,string name,float x,float z,float y,Vector3 size,Material m,bool collision=true)=>Shape(c,PrimitiveType.Cube,name,c.Point(x,z,y),size,m,collision);
        static Transform Label(ChapterTwoDirector c,string text,Vector3 p){var g=new GameObject(text);g.transform.SetParent(c.WorldRoot);g.transform.position=p;
            var t=g.AddComponent<TextMesh>();t.text=text;t.anchor=TextAnchor.MiddleCenter;t.fontSize=64;t.characterSize=.18f;t.color=new Color(1,.8f,.4f);
            if(c.Story.chineseFont){t.font=c.Story.chineseFont;g.GetComponent<Renderer>().sharedMaterial=t.font.material;}
            g.AddComponent<ChapterOneLabel>().view=c.Story.player.view;return g.transform;}
        static Transform Person(ChapterTwoDirector c,string name,float x,float z){var g=new GameObject(name);g.transform.SetParent(c.WorldRoot);g.transform.position=c.Point(x,z);
            var source=c.Story.survivor ? c.Story.survivor.Find("Visual") : null;
            if(source)Object.Instantiate(source.gameObject,g.transform,false);else Shape(c,PrimitiveType.Capsule,"队员",c.Point(x,z,.85f),new Vector3(.65f,.85f,.65f),cloth,false).transform.SetParent(g.transform,true);
            Label(c,name,c.Point(x,z,2.2f));return g.transform;}
        static ChapterTwoActor Target(ChapterTwoDirector c,string name,ChapterTwoActor.Kind kind,ChapterTwoDirector.Stage stage,float x,float z,float y,int health){
            var g=Shape(c,PrimitiveType.Sphere,name,c.Point(x,z,y),kind==ChapterTwoActor.Kind.Water ? new Vector3(.8f,1.2f,.8f) : Vector3.one*.9f,kind==ChapterTwoActor.Kind.Water ? Water : Fire);
            var a=g.AddComponent<ChapterTwoActor>();a.Initialize(c,kind,stage,health);Label(c,name,c.Point(x,z,y+1));return a;}
        static ChapterTwoActor Enemy(ChapterTwoDirector c,ChapterTwoActor.Kind kind,ChapterTwoDirector.Stage stage,float x,float z,bool reserve=false){
            var g=new GameObject(kind==ChapterTwoActor.Kind.Boss ? "赤焰山魈" : kind==ChapterTwoActor.Kind.Thrower ? "投火山魈" : "逐热兽");g.transform.SetParent(c.WorldRoot);g.transform.position=c.Point(x,z);
            bool boss=kind==ChapterTwoActor.Kind.Boss;float size=boss ? 2.2f : 1;
            var visual=Shape(c,PrimitiveType.Capsule,"焦岩身躯",g.transform.position+Vector3.up*size,new Vector3(size,size,size*.8f),kind==ChapterTwoActor.Kind.Beast ? wood : stone,false);visual.transform.SetParent(g.transform,true);
            if(kind==ChapterTwoActor.Kind.Beast && c.Story.beasts!=null && c.Story.beasts.Length>0 && c.Story.beasts[0]){
                var model=c.Story.beasts[0].transform.Find("Visual");if(model){Object.Instantiate(model.gameObject,g.transform,false);visual.SetActive(false);}
            }
            Shape(c,PrimitiveType.Sphere,"赤焰头冠",g.transform.position+Vector3.up*size*2,Vector3.one*size*.65f,Fire,false).transform.SetParent(g.transform,true);
            if(kind!=ChapterTwoActor.Kind.Beast)foreach(int sign in new[]{-1,1})Shape(c,PrimitiveType.Capsule,"山魈臂",g.transform.position+new Vector3(sign*size*.65f,size,0),new Vector3(size*.4f,size*.8f,size*.4f),wood,false).transform.SetParent(g.transform,true);
            if(boss)foreach(int sign in new[]{-1,1}){
                Shape(c,PrimitiveType.Sphere,"炽目",g.transform.position+new Vector3(sign*.4f,4.35f,.7f),Vector3.one*.22f,Fire,false).transform.SetParent(g.transform,true);
                var horn=Shape(c,PrimitiveType.Cylinder,"赤焰角",g.transform.position+new Vector3(sign*.6f,4.9f,0),new Vector3(.23f,.6f,.23f),wood,false);horn.transform.rotation=Quaternion.Euler(15,0,-sign*22);horn.transform.SetParent(g.transform,true);
            }
            var col=g.AddComponent<BoxCollider>();col.center=Vector3.up*size;col.size=new Vector3(size*1.6f,size*2.4f,size*1.2f);
            var actor=g.AddComponent<ChapterTwoActor>();actor.BossReinforcement=reserve;actor.Initialize(c,kind,stage,boss ? 32 : kind==ChapterTwoActor.Kind.Thrower ? 5 : 3);
            if(reserve)g.SetActive(false);return actor;
        }
        public static void SpawnBossAdds(ChapterTwoDirector c){foreach(var a in c.Actors)if(a.BossReinforcement && !a.Defeated)a.gameObject.SetActive(true);c.Story.Notify("山魈进入狂暴！兽群从祭场两侧出现，Q 技能可震退兽群。",6);}
        public static void Build(ChapterTwoDirector c){
            earth=Mat("焦土",new Color(.26f,.19f,.12f));road=Mat("灰烬古道",new Color(.48f,.34f,.19f));wood=Mat("焦木",new Color(.16f,.08f,.035f));
            stone=Mat("山岩",new Color(.31f,.28f,.23f));leaf=Mat("残叶",new Color(.49f,.26f,.06f));cloth=Mat("布衣",new Color(.76f,.64f,.39f));
            Fire=Mat("天火",new Color(1,.2f,.035f));Water=Mat("清水",new Color(.12f,.7f,.9f));Fire.EnableKeyword("_EMISSION");Fire.SetColor("_EmissionColor",new Color(1,.12f,.01f));
            c.WorldRoot.gameObject.AddComponent<ChapterOneMaterials>().owned=new[]{earth,road,wood,stone,leaf,cloth,Fire,Water};
            Box(c,"山地",0,93,-.3f,new Vector3(56,.6f,218),earth);Box(c,"主山道",0,87,.012f,new Vector3(8,.025f,196),road,false);
            Box(c,"救援支路",-7,49,.02f,new Vector3(22,.03f,5),road,false);Box(c,"补给支路",10,86,.02f,new Vector3(20,.03f,4),road,false);
            Box(c,"左岩壁",-26,93,4,new Vector3(3,9,218),stone);Box(c,"右岩壁",26,93,4,new Vector3(3,9,218),stone);
            Box(c,"后壁",0,-15,3,new Vector3(56,7,3),stone);Box(c,"山顶岩壁",0,200,3,new Vector3(56,7,3),stone);
            var random=new System.Random(22013);
            for(int i=0;i<64;i++){float x=(i%2==0 ? -1 : 1)*(10+(float)random.NextDouble()*12),z=-5+i*3.05f;if(z>140 || (z>42 && z<60 && x<0) || (z>80 && z<93 && x>0))continue;
                float h=3+(float)random.NextDouble()*3;Shape(c,PrimitiveType.Cylinder,"焦林枯树",c.Point(x,z,h/2),new Vector3(.45f,h/2,.45f),wood);
                Shape(c,PrimitiveType.Sphere,"焦黄树冠",c.Point(x,z,h),new Vector3(2.8f,.6f,2),leaf,false);
            }
            for(int i=0;i<30;i++){
                float x=(i%2==0 ? -1 : 1)*(25+(float)random.NextDouble()*3),z=-7+i*7;
                var rock=Shape(c,PrimitiveType.Sphere,"山脊岩层",c.Point(x,z,3),new Vector3(8,7+(float)random.NextDouble()*5,11),stone);rock.transform.rotation=Quaternion.Euler(0,i*31,12);
            }
            c.Clue=Label(c,"断裂路标 · 调查",c.Point(1,13,1.5f));Box(c,"运水木桶",2,13,.4f,new Vector3(.8f,.8f,.8f),wood,false);
            c.Captain=Person(c,"运水队长",-12,51);Person(c,"受困队员",-15,54);
            for(int i=0;i<2;i++){
                float x=-9-i*6;Box(c,"挂囊木架",x,47,2.2f,new Vector3(.2f,4.4f,.2f),wood);
                var water=Target(c,"悬挂水囊",ChapterTwoActor.Kind.Water,ChapterTwoDirector.Stage.Rescue,x,49,3.1f,1);
                water.LinkedVisual=Shape(c,PrimitiveType.Cube,"阻路火墙",c.Point(x,53,.8f),new Vector3(4,1.6f,2),Fire);
            }
            foreach(float x in new[]{-19f,9f}){
                Box(c,"投火高台",x,59,.6f,new Vector3(3,1.2f,3),stone);
                var thrower=Enemy(c,ChapterTwoActor.Kind.Thrower,ChapterTwoDirector.Stage.Rescue,x,59);
                thrower.transform.position+=Vector3.up*1.2f;thrower.SetHomeHere();
            }
            for(int i=0;i<4;i++)Enemy(c,ChapterTwoActor.Kind.Beast,ChapterTwoDirector.Stage.Rescue,-8+i*4,62+i%2*4);
            c.Supply=Label(c,"药草与清水",c.Point(18,86,1.1f));Box(c,"补给箱",18,86,.3f,new Vector3(1.3f,.6f,1),cloth);
            c.Lore=Label(c,"扶桑残碑",c.Point(-17,104,2));Box(c,"古碑",-17,104,1.2f,new Vector3(1.4f,2.4f,.5f),stone);
            for(int i=0;i<3;i++)Target(c,"扶桑火核 "+(i+1),ChapterTwoActor.Kind.Core,ChapterTwoDirector.Stage.Cores,-4+i*4,104,2.2f,1);
            c.Gate=Box(c,"封路火墙",0,110,2,new Vector3(48,4,.8f),Fire);
            c.ArenaGate=Box(c,"祭场封门",0,141,2,new Vector3(48,4,.8f),stone);c.ArenaGate.SetActive(false);
            Box(c,"山顶祭场",0,167,.025f,new Vector3(40,.05f,50),road,false);
            c.Altar=Label(c,"扶桑祭坛 · 碎片共鸣",c.Point(0,191,1.8f));Box(c,"祭坛台座",0,192,.3f,new Vector3(5,.6f,3),stone);
            c.Boss=Enemy(c,ChapterTwoActor.Kind.Boss,ChapterTwoDirector.Stage.Boss,0,170);
            Enemy(c,ChapterTwoActor.Kind.Beast,ChapterTwoDirector.Stage.Boss,-12,178,true);Enemy(c,ChapterTwoActor.Kind.Beast,ChapterTwoDirector.Stage.Boss,12,178,true);
            Label(c,"逐 火 入 山",c.Point(0,5,4));Label(c,"燃 林 救 援",c.Point(-10,45,5));Label(c,"扶 桑 古 道",c.Point(0,119,5));
        }
    }
}

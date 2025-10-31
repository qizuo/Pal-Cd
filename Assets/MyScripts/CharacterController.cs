using UnityEngine;
using System.Collections;

public class CharacterController : MonoBehaviour 
{
	public int gameState;
	public UnityEngine.AI.NavMeshAgent player;
	public Vector3 point; 
	
	public int attack;
	public int defence;
	public int count;
	
	public int task;
	public int distask;
	
	private float dis2;
	
	public GameObject Enemy;
	public GameObject Prefab;
	public GameObject LevelPrefab;
	public GameObject MouseIcon;
	private GameObject Magic;
	public GameObject Emit_position;
	public AudioSource music;
	
	public GameObject ChanChu;
	public GameObject CunZhang;
	public GameObject YaoShi;
	public GameObject TieJiang;
	public GameObject ShuSheng;
	public GameObject CaiFeng;
	public GameObject LaoTou;
	public GameObject CaiShen;
	public GameObject QingWa;
	public GameObject HuLi;
	public GameObject YeLang;
	public GameObject LaoHu;
	public GameObject ChuanSongMen;

	private  float attackTimer=0f;
	
	public GUISkin mySkin;
	public int maxHealth;
	public int curHealth;
	public float PlayerHealthbar;
	
	public int maxMana;
	public int curMana;
	public float PlayerManabar;
	
	public int maxExp=100;
	public int curExp=0;
	public float PlayerExpbar;
	public int Level=1;
	
	public bool playerAttack;
	public bool findNpc = true;
	public bool gat = false;
	
	/***********************************************/
	/*                      地图                   */
	/***********************************************/
	public bool _displayMapWindow = false;
	private const int MAP_WINDOW_ID = 4;
	private Rect _mapWindowRect = new Rect(Screen.width/2-240,Screen.height/2-220,480,440);
	
	/***********************************************/
	/*                 任务框                     */
	/***********************************************/
	public bool _displayTaskWindow = false;
	private const int TASK_WINDOW_ID = 5;
	private Rect _taskWindowRect = new Rect(Screen.width-250,Screen.height/2 - 133,240,266);
	
	/***********************************************/
	/*                  死亡界面                   */
	/***********************************************/
	public bool _displayDeadWindow = false;
	private const int DEAD_WINDOW_ID = 6;
	private Rect _deadWindowRect = new Rect(Screen.width/2-120,Screen.height/2-40,400,80);
	
	/***********************************************/
	/*                  系统界面                   */
	/***********************************************/
	public bool _displaySystemWindow = false;
	private const int SYSTEM_WINDOW_ID = 7;
	private Rect _systemWindowRect = new Rect(Screen.width/2-60,Screen.height/2-120,120,240);
	
	void Start () 
	{
		gat = false;
		count = 0;
		task = 1;
		_displayTaskWindow = true;
		curMana = maxMana = 250;
		PlayerHealthbar = 200;
		gameState = 0;
		player = gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>();  
	}
	
	void Update () 
	{
		if(Input.GetButtonUp("Toggle Map"))
		{
			ToggleMapWindow();
		}
		
		if(Input.GetButtonUp("Toggle Task"))
		{
			ToggleTaskWindow();
		}
		
		if(Input.GetButtonUp("Toggle System"))
		{
			ToggleSystemWindow();
		}
		
		if(Input.GetKey(KeyCode.Escape))
		{
			ToggleSystemWindow();
		}
			
		if(attackTimer>0)
			attackTimer-=Time.deltaTime;
		if(attackTimer<0)
			attackTimer=0;
		switch(gameState)
		{
		case 0:
			player.speed = 0;
			point = transform.position;
			GetComponent<Animation>().CrossFade("ZY_idle");
			break;
		case 1:
			player.speed = 3;
			// Debug.Log("1031."+player.isOnNavMesh);
			player.SetDestination(point);
			GetComponent<Animation>().CrossFade("ZY_run");
			break;
		case 2:
			player.speed = 0;
			point = transform.position;
			gameObject.GetComponent<Animation>()["ZY_attack"].layer = 3;
			GetComponent<Animation>().CrossFade("ZY_attack");
			break;
		}
		if(findNpc)
		{
			if(Input.GetMouseButtonDown(0))
			{
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				Physics.Raycast(ray, out hit, 100);  
				if(hit.collider.name =="Terrain")
				{
					if(hit.transform!=null &&  GameObject.Find("myGUI").GetComponentInChildren<myGUI>()._displayShopWindow==false && GameObject.Find("myGUI").GetComponentInChildren<myGUI>()._displayInventoryWindow == false && GameObject.Find("myGUI").GetComponentInChildren<myGUI>()._displayCharacterWindow == false && _displayDeadWindow == false && _displaySystemWindow == false && task == 0 && GameObject.Find("gate").GetComponentInChildren<gate>().gat == 0)
					{
						point = hit.point;
						transform.LookAt(new Vector3(point.x,transform.position.y,point.z));
						//gameState = 1;	
						playerAttack = false;
						GameObject mousicon = Instantiate(MouseIcon,point,gameObject.transform.rotation)as GameObject;
						Destroy(mousicon,1);
					}
				}
			}
		
			if(Input.GetMouseButtonDown(0))
			{		
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				Physics.Raycast(ray, out hit, 100);  
				if(hit.collider.tag =="Enemy" && GameObject.Find("myGUI").GetComponentInChildren<myGUI>()._displayShopWindow==false && GameObject.Find("myGUI").GetComponentInChildren<myGUI>()._displayInventoryWindow == false && GameObject.Find("myGUI").GetComponentInChildren<myGUI>()._displayCharacterWindow == false && _displayDeadWindow == false && _displaySystemWindow == false && task == 0 && GameObject.Find("gate").GetComponentInChildren<gate>().gat == 0)
				{
					point = hit.transform.position;
					dis2 = Vector3.Distance(point,transform.position);
					transform.LookAt(new Vector3(point.x,transform.position.y,point.z));
					if(dis2 < 10f)
					{
						gameState = 1;
						if(attackTimer==0)
						{
						    Magic = Instantiate(Prefab,Emit_position.transform.position,Emit_position.transform.rotation) as GameObject;
						    Vector3 direction = point - Emit_position.transform.position;
						    Magic.GetComponent<Rigidbody>().AddForceAtPosition( 1.5f*direction, point,ForceMode.Impulse);
					        playerAttack = true;
						    attackTimer=1f;
						}
					}
				}
			}
		}
		
		if(curHealth > 0)
		{
		    Attack();
		}
		Pathfinding();
		
		UpdateLevel();
		AdddjustcurHealth(0);
	
		if(gat)
		{
			player.speed = 1000;
			point = GameObject.Find("gate").GetComponentInChildren<gate>().position_gate.position;
			player.SetDestination(point);
			GetComponent<Animation>().CrossFade("ZY_idle");
			float dis = Vector3.Distance(point,transform.position);
			if(dis < 10f)
			{
				gat = false;
				gameState = 0;
			}
		}
	}
	
	void OnGUI()
	{
		GUI.skin = mySkin; 
		PlayerManabar=(200)*(curMana/(float)maxMana);
		PlayerExpbar=(200)*(curExp/(float)maxExp);
		GameObject pc=GameObject.Find("Player");
		PlayerCharacter pcClass=pc.GetComponent<PlayerCharacter>();
		pcClass.Name = PlayerPrefs.GetString("Player Name","Name Me");
		GUI.skin=mySkin;
		GUI.Label(new Rect(10,10,160,160),"","头像");
		GUI.Label(new Rect(100,130,100,20),"Lv."+Level,"yellow");
		GUI.Box(new Rect(200,40,100,20),""+pcClass.Name,"yellow1");
		GUI.Box(new Rect(125,70,PlayerHealthbar,20),"","血条");
		GUI.Box(new Rect(195,70,100,20),curHealth + "/" + maxHealth,"white");
		GUI.Box(new Rect(125,90,PlayerManabar,20),"","蓝条");
		GUI.Box(new Rect(195,90,100,20),curMana + "/" + maxMana,"white");
		GUI.Box(new Rect(125,110,PlayerExpbar,10),"","经验条");
		GUI.Box(new Rect(195,110,100,20),curExp + "/" + maxExp,"gold");
		
		if(_displayMapWindow)
			_mapWindowRect = GUI.Window(MAP_WINDOW_ID,_mapWindowRect,MapWindow ,"","外框");
		
		if(_displayTaskWindow)
			_taskWindowRect = GUI.Window(TASK_WINDOW_ID,_taskWindowRect,TaskWindow,"","任务框");
		
		if(_displayDeadWindow)
			_deadWindowRect = GUI.Window(DEAD_WINDOW_ID,_deadWindowRect,DeadWindow ,"","说明框");
		
		if(_displaySystemWindow)
			_systemWindowRect = GUI.Window(SYSTEM_WINDOW_ID,_systemWindowRect,SystemWindow ,"","说明框");
		
		if(GUI.Button(new Rect(Screen.width-35,Screen.height/2 - 130,21,21),"","任务按钮"))
		{
			_displayTaskWindow = !_displayTaskWindow;
		}
		
		switch(task)
		{
		case 1:
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"       指引精灵： "+ pcClass.Name +"，欢迎您来到异世大陆，这里是圣魂村，老村长找你有事，","gray");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 140,440,100),"  快去吧！","gray");
			if(GUI.Button(new Rect(Screen.width/2 + 150,Screen.height/2 + 170,60,20),"","按钮1"))
		    {
				task = 0;
				distask = 1;
				GameObject.Find("MainCamera").GetComponentInChildren<ScreenPoint>().task = 1;
				FindNpc(CunZhang);
		    }
			GUI.Button(new Rect(Screen.width/2 + 165,Screen.height/2 + 170,60,20),"确定","gold");
			break;
		case 2:
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"        村长：药师刘老头最近有点忙，找你帮忙，快去吧！","gray");
			if(GUI.Button(new Rect(Screen.width/2 + 150,Screen.height/2 + 170,60,20),"","按钮1"))
		    {
				task = 0;
				distask = 2;
				GameObject.Find("MainCamera").GetComponentInChildren<ScreenPoint>().task = 2;
				FindNpc(YaoShi);
		    }
			GUI.Button(new Rect(Screen.width/2 + 165,Screen.height/2 + 170,60,20),"接受","gold");
			break;
		case 3:
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"        药师：最近店里缺少蟾蜍干，少侠能帮我收集么","gray");
			if(GUI.Button(new Rect(Screen.width/2 + 150,Screen.height/2 + 170,60,20),"","按钮1"))
		    {
				task = 0;
				distask = 3;
				FindNpc(QingWa);
		    }
			GUI.Button(new Rect(Screen.width/2 + 165,Screen.height/2 + 170,60,20),"接受","gold");
			break;
		case 4:
			if(GUI.Button(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框"))
			{
				task = 5;
				GameObject.Find("myGUI").GetComponentInChildren<myGUI>().money += 200;
				curExp +=1800;
			}
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"        药师：少侠，非常感谢您，这使您的报酬，请收下。","gray");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 140,440,100),"        金币：200      经验：1800","gray");
			break;
		case 5:
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"        药师：村里的铁匠在找你，你可以用金币在他那里买武器","gray");
			if(GUI.Button(new Rect(Screen.width/2 + 150,Screen.height/2 + 170,60,20),"","按钮1"))
		    {
				task = 0;
				distask = 5;
				GameObject.Find("MainCamera").GetComponentInChildren<ScreenPoint>().task = 4;
				FindNpc(TieJiang);
		    }
			GUI.Button(new Rect(Screen.width/2 + 165,Screen.height/2 + 170,60,20),"确定","gold");
			break;
		case 6:
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"        铁匠：这些狐狸偷吃了我的鸡，帮我去清理一下。","gray");
			if(GUI.Button(new Rect(Screen.width/2 + 150,Screen.height/2 + 170,60,20),"","按钮1"))
		    {
				task = 0;
				distask = 6;
				FindNpc(HuLi);
		    }
			GUI.Button(new Rect(Screen.width/2 + 165,Screen.height/2 + 170,60,20),"接受","gold");
			break;
		case 7:
			if(GUI.Button(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框"))
			{
				task = 8;
				GameObject.Find("myGUI").GetComponentInChildren<myGUI>().money += 200;
				curExp +=6000;
			}
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"        铁匠：少侠，非常感谢您，这使您的报酬，请收下。","gray");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 140,440,100),"        金币：200      经验：6000","gray");
			break;
		case 8:
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"        铁匠：村里的裁缝在找你，好像有急事。","gray");
			if(GUI.Button(new Rect(Screen.width/2 + 150,Screen.height/2 + 170,60,20),"","按钮1"))
		    {
				task = 0;
				distask = 8;
				GameObject.Find("MainCamera").GetComponentInChildren<ScreenPoint>().task = 6;
				FindNpc(CaiFeng);
		    }
			GUI.Button(new Rect(Screen.width/2 + 165,Screen.height/2 + 170,60,20),"确定","gold");
			break;
		case 9:
			if(GUI.Button(new Rect(Screen.width/2 - 200,Screen.height/2 - 150,400,300),"","说明框"))
			{
				task = 0;
			}
			GUI.Label(new Rect(Screen.width/2 - 180,Screen.height/2 - 110,600,400),"        -鼠标" + "\n" +
				"左键：控制角色移动" + "\n" +
				"右键：旋转角色角度" + "\n" +
				"中键：缩放视角" + "\n" +
			
				"        -按键" + "\n" +
				"N键：显示游戏任务面板" + "\n" +
				"C键：显示角色属性面板" + "\n" +
				"V键：显示游戏技能面板" + "\n" +
				"B键：显示游戏背包面板" + "\n" +
				"N键：显示游戏任务面板" + "\n" +
				"M键：显示游戏地图" + "\n","yellow");
			GUI.Label(new Rect(Screen.width/2 - 10,Screen.height/2 - 140,100,30),"游戏帮助","yellow");
			break;
		case 10:
			GUI.Label(new Rect(Screen.width/2 - 100,Screen.height/2 - 45,200,90),"","说明框");
			if(GUI.Button(new Rect(Screen.width/2 - 70,Screen.height/2 - 15,60,30),"","按钮1"))
			{
				task = 0;
				music.Play();
			}
			if(GUI.Button(new Rect(Screen.width/2 + 10,Screen.height/2 - 15,60,30),"","按钮1"))
			{
				task = 0;
				music.Stop();
			}
			GUI.Label(new Rect(Screen.width/2 - 55,Screen.height/2 - 10,60,30),"播放","gold");
			GUI.Label(new Rect(Screen.width/2 + 25,Screen.height/2 - 10,60,30),"关闭","gold");
			GUI.Label(new Rect(Screen.width/2 - 30,Screen.height/2 - 40,100,30),"音乐控制","yellow");
			break;
		case 11:
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"        裁缝：我要给我孩子做衣裳，缺少几张野狼皮，你能帮我去收集吗？","gray");
			if(GUI.Button(new Rect(Screen.width/2 + 150,Screen.height/2 + 170,60,20),"","按钮1"))
		    {
				task = 0;
				distask = 9;
				FindNpc(YeLang);
		    }
			GUI.Button(new Rect(Screen.width/2 + 165,Screen.height/2 + 170,60,20),"接受","gold");
			break;
		case 12:
			if(GUI.Button(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框"))
			{
				task = 13;
				GameObject.Find("myGUI").GetComponentInChildren<myGUI>().money += 200;
				curExp +=12000;
			}
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"        裁缝：少侠，非常感谢您，这使您的报酬，请收下。","gray");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 140,440,100),"        金币：200      经验：12000","gray");
			break;
		case 13:
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 100,440,100),"","说明框");
			GUI.Label(new Rect(Screen.width/2 - 220,Screen.height/2 + 120,440,100),"        裁缝：想要快点增强你的实力，可以去落日草原历练","gray");
			if(GUI.Button(new Rect(Screen.width/2 + 150,Screen.height/2 + 170,60,20),"","按钮1"))
		    {
				task = 0;
				distask = 11;
				FindNpc(ChuanSongMen);
		    }
			GUI.Button(new Rect(Screen.width/2 + 165,Screen.height/2 + 170,60,20),"确定","gold");
			break;
		}
	}
		
	void OnMouseUp()
	{
		playerAttack = false;
	}
	
	void  OnCollisionEnter(Collision theCollision )
	{
		if(theCollision.gameObject.tag == "Enemy")
		{
			Debug.Log("Enemy");
		}
	}
	
	void UpdateLevel()
	{
		if(curExp >= maxExp)
		{
			GameObject leve = Instantiate(LevelPrefab,gameObject.transform.position,gameObject.transform.rotation)as GameObject;
			Destroy(leve,1);
			curExp = curExp - maxExp;
			Level++;
			curHealth = maxHealth;
			curMana = maxMana;
			maxExp = Level*Level*100;
			for(int cnt=0;cnt<System.Enum.GetValues(typeof(AttributeName)).Length;cnt++)
			{
			    GameObject.Find("myGUI").GetComponentInChildren<myGUI>()._toon.GetPrimaryAttribute(cnt).BaseValue += 5;
			}
			GameObject.Find("myGUI").GetComponentInChildren<myGUI>().pointsLeft += 5;
		}
	}
	
	private void MapWindow(int id)
	{
		if(GUI.Button(new Rect(_mapWindowRect.width - 35, 5, 18, 18),"","closeButton"))
		{
			_displayMapWindow = false;
		}
		GUI.Label(new Rect(15,36,393,388),"","内框2");
		GUI.Label(new Rect(410,36,58,388),"","内框1");
		GUI.Label(new Rect(20,40,380,380),"","地图");
		GUI.Label(new Rect(220,5,380,380),"地图","white");
		
		if(GUI.Button(new Rect(414,50,50,20),"","按钮1"))
		{		
			 FindNpc(CunZhang);
		}	
		if(GUI.Button(new Rect(414,70,50,20),"","按钮1"))
		{		
			 FindNpc(YaoShi);
		}		
		if(GUI.Button(new Rect(414,90,50,20),"","按钮1"))
		{		
			 FindNpc(TieJiang);
		}
		if(GUI.Button(new Rect(414,110,50,20),"","按钮1"))
		{		
			 FindNpc(ShuSheng);
		}
		if(GUI.Button(new Rect(414,130,50,20),"","按钮1"))
		{		
			 FindNpc(CaiFeng);
		}
		if(GUI.Button(new Rect(414,150,50,20),"","按钮1"))
		{		
			 FindNpc(LaoTou);
		}
		if(GUI.Button(new Rect(414,170,50,20),"","按钮1"))
		{		
			 FindNpc(CaiShen);
		}
		if(GUI.Button(new Rect(414,190,50,20),"","按钮1"))
		{		
			 FindNpc(QingWa);
		}
		if(GUI.Button(new Rect(414,210,50,20),"","按钮1"))
		{		
			 FindNpc(HuLi);
		}
		if(GUI.Button(new Rect(414,230,50,20),"","按钮1"))
		{		
			 FindNpc(YeLang );
		}
		if(GUI.Button(new Rect(414,250,50,20),"","按钮1"))
		{		
			 FindNpc(LaoHu);
		}
		if(GUI.Button(new Rect(414,270,50,20),"","按钮1"))
		{		
			 FindNpc(ChuanSongMen);
		}
		
		GUI.Button(new Rect(427,50,50,20),"村长","gold");
		GUI.Button(new Rect(427,70,70,20),"药师","gold");
		GUI.Button(new Rect(427,90,50,20),"铁匠","gold");
		GUI.Button(new Rect(427,110,50,20),"书生","gold");
		GUI.Button(new Rect(427,130,50,20),"裁缝","gold");
		GUI.Button(new Rect(427,150,50,20),"老头","gold");
		GUI.Button(new Rect(427,170,50,20),"财神","gold");
		GUI.Button(new Rect(427,190,50,20),"蟾蜍","gold");
		GUI.Button(new Rect(427,210,50,20),"红狐","gold");
		GUI.Button(new Rect(427,230,50,20),"野狼","gold");
		GUI.Button(new Rect(427,250,50,20),"老虎","gold");
		GUI.Button(new Rect(417,270,50,20),"传送门","gold");
		GUI.DragWindow();
	}
	
	private void TaskWindow(int id)
	{
		if(GUI.Button(new Rect(215,3,21,21),"","任务按钮"))
		{
			_displayTaskWindow = !_displayTaskWindow;
		}
		switch(distask)
		{
		case 1:
			GUI.Label(new Rect(10,30,200,21),"[主]历练开始");
			GUI.Label(new Rect(10,50,200,21),"    去圣魂村找");
			if(GUI.Button(new Rect(90,50,21,21),"村长","gold"))
			{
				FindNpc(CunZhang);
			}
			break;
		case 2:
			GUI.Label(new Rect(10,30,200,21),"[主]帮助药师(1级)");
			GUI.Label(new Rect(10,50,200,21),"    去找");
			if(GUI.Button(new Rect(55,50,21,21),"药师","gold"))
			{
				FindNpc(YaoShi);
			}
			break;
		case 3:
			GUI.Label(new Rect(10,30,200,21),"[主]收集蟾蜍(1级)");
			GUI.Label(new Rect(10,50,200,21),"    帮忙抓        " + count + "/" + 3);
			if(GUI.Button(new Rect(65,50,21,21),"蟾蜍","gold"))
			{
				FindNpc(ChanChu);
			}
			if(count == 3)
			{
				GameObject.Find("MainCamera").GetComponentInChildren<ScreenPoint>().task = 3;
				distask = 4;
				count = 0;
				FindNpc(YaoShi);
			}
			break;
		case 4:
			GUI.Label(new Rect(10,30,200,21),"[主]收集蟾蜍(1级)");
			GUI.Label(new Rect(10,50,200,21),"    任务完成，去找");
			if(GUI.Button(new Rect(120,50,21,21),"药师","gold"))
			{
				FindNpc(YaoShi);
			}
			GUI.Label(new Rect(10,70,200,21),"提交任务");
			break;
		case 5:
			GUI.Label(new Rect(10,30,200,21),"[主]寻找铁匠(4级)");
			GUI.Label(new Rect(10,50,200,21),"    去          那里购买武器");
			if(GUI.Button(new Rect(30,50,21,21),"  铁匠","gold"))
			{
				FindNpc(TieJiang);
			}
			break;
		case 6:
			GUI.Label(new Rect(10,30,200,21),"[主]清理狐狸(4级)");
			GUI.Label(new Rect(10,50,200,21),"    已清理        " + count + "/" + 5);
			if(GUI.Button(new Rect(65,50,21,21),"狐狸","gold"))
			{
				FindNpc(HuLi);
			}
			if(count == 5)
			{
				GameObject.Find("MainCamera").GetComponentInChildren<ScreenPoint>().task = 5;
				distask = 7;
				count = 0;
				FindNpc(TieJiang);
			}
			break;
		case 7:
			GUI.Label(new Rect(10,30,200,21),"[主]清理狐狸(4级)");
			GUI.Label(new Rect(10,50,200,21),"    任务完成，去找");
			if(GUI.Button(new Rect(120,50,21,21),"铁匠","gold"))
			{
				FindNpc(TieJiang);
			}
			GUI.Label(new Rect(10,70,200,21),"提交任务");
			break;
		case 8:
			GUI.Label(new Rect(10,30,200,21),"[主]寻找裁缝(7级)");
			GUI.Label(new Rect(10,50,200,21),"    去找         ");
			if(GUI.Button(new Rect(30,50,21,21),"      裁缝","gold"))
			{
				FindNpc(CaiFeng);
			}
			break;
		case 9:
			GUI.Label(new Rect(10,30,200,21),"[主]清理野狼(7级)");
			GUI.Label(new Rect(10,50,200,21),"    已清理        " + count + "/" + 5);
			if(GUI.Button(new Rect(65,50,21,21),"野狼","gold"))
			{
				FindNpc(YeLang);
			}
			if(count == 5)
			{
				FindNpc(CaiFeng);
				GameObject.Find("MainCamera").GetComponentInChildren<ScreenPoint>().task = 7;
				distask = 10;
				count = 0;
			}
			break;
		case 10:
			GUI.Label(new Rect(10,30,200,21),"[主]清理野狼(7级)");
			GUI.Label(new Rect(10,50,200,21),"    任务完成，去找");
			if(GUI.Button(new Rect(120,50,21,21),"裁缝","gold"))
			{
				FindNpc(CaiFeng);
			}
			GUI.Label(new Rect(10,70,200,21),"提交任务");
			break;
		case 11:
			GUI.Label(new Rect(10,30,200,21),"[主]走出圣魂村(10级)");
			GUI.Label(new Rect(10,50,200,21),"    已杀死        " + count + "/" + 5);
			if(GUI.Button(new Rect(65,50,21,21),"老虎","gold"))
			{
				FindNpc(LaoHu);
			}
			if(count == 5)
			{
				FindNpc(ChuanSongMen);
				GameObject.Find("MainCamera").GetComponentInChildren<ScreenPoint>().task = 8;
				distask = 12;
				count = 0;
			}
			break;
		case 12:
			GUI.Label(new Rect(10,30,200,21),"[主]走出圣魂村(10级)");
			GUI.Label(new Rect(10,50,200,21),"    任务完成，去找");
			if(GUI.Button(new Rect(120,50,21,21),"传送门","gold"))
			{
				FindNpc(ChuanSongMen);
			}
			GUI.Label(new Rect(10,70,200,21),"传送城镇");
			break;
		}
	}
	
	private void DeadWindow(int id)
	{
		if(GUI.Button(new Rect(20,30,170,30),"","按钮1"))
		{
			curHealth = maxHealth;
			_displayDeadWindow = !_displayDeadWindow;
		}
		if(GUI.Button(new Rect(210,30,170,30),"","按钮1"))
		{
			Application.LoadLevel(0);
		}
		GUI.Button(new Rect(80,35,170,30),"原地复活","gold");
		GUI.Button(new Rect(270,35,170,30),"重新开始","gold");
	}
	
	private void SystemWindow(int id)
	{
		if(GUI.Button(new Rect(10,30,100,30),"","按钮1"))
		{
			_displaySystemWindow = false;
		}
		if(GUI.Button(new Rect(10,80,100,30),"","按钮1"))
		{
			task = 10;
			_displaySystemWindow = false;
		}
		if(GUI.Button(new Rect(10,130,100,30),"","按钮1"))
		{
			task = 9;
			_displaySystemWindow = false;
		}
		if(GUI.Button(new Rect(10,180,100,30),"","按钮1"))
		{
			Application.Quit();
		}
		GUI.Label(new Rect(32,35,100,30),"继续游戏","gold");
		GUI.Label(new Rect(32,85,100,30),"音乐播放","gold");
		GUI.Label(new Rect(32,135,100,30),"游戏帮助","gold");
		GUI.Label(new Rect(32,185,100,30),"退出游戏","gold");
	}
	
	private void Pathfinding()
	{
		float dis1 = Vector3.Distance(point,transform.position);
		if(!playerAttack)
		{
		    if(dis1 > 0.5f)
		    {
				gameState = 1;
		    }
			else
			{
			    gameState = 0;
		    }   
		}
		if(playerAttack)
		{
			gameState = 2;
		}
	}
	
	private void FindNpc(GameObject npc)
	{
		point = npc.transform.position;
		gameObject.transform.LookAt(npc.transform);
		gameState = 1;
	}
	
	private void Attack()
	{
		if(!playerAttack)
		{
			if(dis2 > 8f)
			{
				gameState = 1;
			}
			else
			{
				gameState = 0;
			}
		}
		if(playerAttack)
		{
			if(dis2 < 8f)
			{
				if(attackTimer==0)
			    {
				    Magic = Instantiate(Prefab,Emit_position.transform.position,Emit_position.transform.rotation) as GameObject;
				    Vector3 direction = Enemy.transform.position - Emit_position.transform.position;
				    Magic.GetComponent<Rigidbody>().AddForceAtPosition( 1.5f*direction, Enemy.transform.position,ForceMode.Impulse);
				    playerAttack = true;
				    attackTimer=0.8f;
			    }
			    gameState = 2;
			}
		}
	}
	
	public void AdddjustcurHealth(int adj)
	{
		curHealth+=adj;
		if(curHealth<0)
		{
			curHealth=0;
		}
		if(curHealth>maxHealth)
			curHealth=maxHealth;
		if(maxHealth<1)
			maxHealth=1;
		if(curHealth == 0)
		{
			_displayDeadWindow = true;
		}
		PlayerHealthbar=(200)*(curHealth/(float)maxHealth);
	}
	
	public void ToggleMapWindow()
	{
		_displayMapWindow = !_displayMapWindow;
	}
	
	public void ToggleTaskWindow()
	{
		_displayTaskWindow = !_displayTaskWindow;
	}
	
	public void ToggleSystemWindow()
	{
		_displaySystemWindow = !_displaySystemWindow;
	}
}

using UnityEngine;
using System.Collections;

public class ScreenPoint: MonoBehaviour {
	public GUISkin mySkin;
	public AudioClip music2;
	public GameObject Target;
	
	public GameObject CunZhang;
	public GameObject Position_cunzhang;
	public GameObject YaoShi;
	public GameObject Position_yaoshi;
	public GameObject TieJiang;
	public GameObject Position_tiejiang;
	public GameObject ShuSheng;
	public GameObject Position_shusheng;
	public GameObject CaiFeng;
	public GameObject Position_caifeng;
	public GameObject LaoTou;
	public GameObject Position_laotou;
	public GameObject CaiShen;
	public GameObject Position_caishen;
	public GameObject XiaoHai01;
	public GameObject XiaoHai02;
	public GameObject Gate;
	public GameObject Position_gate;
	
	private Transform Player;
	
	public int task;
	
	void Start () {
		task = 0;
		GetComponent<AudioSource>().PlayOneShot(music2);
	}
	// Update is called once per frame
	void Update () {
		
	}
	
	void OnGUI()
	{
		switch(task)
		{
		case 1:
			DisPlayTask(Position_cunzhang,10,2);
			break;
		case 2:
			DisPlayTask(Position_yaoshi,10,3);
			break;
		case 3:
			DisPlayTask(Position_yaoshi,10,4);
			break;
		case 4:
			DisPlayTask(Position_tiejiang,10,6);
			break;
		case 5:
			DisPlayTask(Position_tiejiang,10,7);
			break;
		case 6:
			DisPlayTask(Position_caifeng,10,11);
			break;
		case 7:
			DisPlayTask(Position_caifeng,10,12);
			break;
		}
		DisPlayNpc(CunZhang);
		DisPlayNpc(YaoShi);
		DisPlayNpc(TieJiang);
		DisPlayNpc(ShuSheng);
		DisPlayNpc(CaiFeng);
		DisPlayNpc(LaoTou);
		DisPlayNpc(CaiShen);
		DisPlayNpc(XiaoHai01);
		DisPlayNpc(XiaoHai02);
		DisPlayName(Position_cunzhang,12,"�峤");
		DisPlayName(Position_yaoshi,18,"ҩƷ��");
		DisPlayName(Position_tiejiang,18,"������");
		DisPlayName(Position_shusheng,12,"����");
		DisPlayName(Position_caifeng,18,"������");
		DisPlayName(Position_laotou,12,"��ͷ");
		DisPlayName(Position_caishen,12,"����");
		DisPlayName(Position_gate,18,"������");
		
	}
	
	private void DisPlayNpc(GameObject npc)
	{
		Player = GameObject.FindGameObjectWithTag("Player").transform;
		GUI.skin=mySkin;
		float distance1=Vector3.Distance(npc.transform.position,Player.position);
		if(distance1<8f)
		{
			npc.SetActive(true);
		}
		else
		{
		    npc.SetActive(false);
		}
	}
	
	private void DisPlayName(GameObject position,int x,string name)
	{
		Player = GameObject.FindGameObjectWithTag("Player").transform;
		GUI.skin=mySkin;
		Vector3 screenPos = GetComponent<Camera>().WorldToScreenPoint(position.transform.position);
		float distance2=Vector3.Distance(position.transform.position,Player.position);
		if(distance2<12f)
		{
			GUI.Label(new Rect(screenPos.x-x,Screen.height-screenPos.y,100,30),name);
		}
	}
	
	private void DisPlayTask(GameObject position,int x,int y)
	{
		Player = GameObject.FindGameObjectWithTag("Player").transform;
		GUI.skin=mySkin;
		Vector3 screenPos = GetComponent<Camera>().WorldToScreenPoint(position.transform.position);
		float distance=Vector3.Distance(position.transform.position,Player.position);
		// Debug.Log("1031.disPlayTask."+distance+" "+position.name+" "+Target.GetComponentInChildren<CharacterController>().task+" "+y);
		if(distance<12f)
		{
		    if(GUI.Button(new Rect(screenPos.x - x,Screen.height-screenPos.y - 40,30,40),"","按钮1"))
		    {
			    Target.GetComponentInChildren<CharacterController>().task = y;
				task = 0;
		    }
		}
	}
}

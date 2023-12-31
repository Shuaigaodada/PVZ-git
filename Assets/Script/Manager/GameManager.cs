using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public enum PlantType { customize, Peashooter, SunFlower, WallNut, other }
public enum Direction { Left = -1, Right = 1 }
public enum CreateMode { Random, Table }
public static class DirectionExtensions 
{ public static Vector3 vector3(this Direction direction) => new Vector3((int)direction, 0, 0); }


public class GameManager : MonoBehaviour
{
	// Instance
    public static GameManager Instance;

	[Header("Config")]
	// config
	public int SunNumber = 50;
	public float CD = 1.0f;
	public float consume = 1.0f;
	public float SunCreateSpeed = 10f;
	public float SunCreateSpeedFirst = 10f;

	[Header("prefab")]
	// prefab
	public GameObject bornParent;
	public GameObject zombiePrefab;
	public LevelData levelData;
	public LevelInfo levelInfo;
	public PlantInfo plantInfo;
	public GameObject StartText;

	[Header("current state")]
	// current state
	public bool StartCreateZombie = true;
	public bool LockedPlantCards = false;
	private int zOrderIndex = 0;
	private bool firstStart = true;
	public CreateMode createMode = CreateMode.Table;
	public bool gameStart;
	public int curLevelId = 1;
	public int curProgressId = 0;
	public Dictionary<GameObject, int> curProgressZombie;

	private void Awake() => Instance = this;
	private void Start() => LoadTable();
	public void ChangeSunNumber(int changeNumber)
	{
		SunNumber += changeNumber;
		if(SunNumber < 0) SunNumber = 0;
		UIManager.Instance.UpdateUI();
	}
	public void LoadTable()
	{
		levelData = Resources.Load("TableData/Level") as LevelData;
		levelInfo = Resources.Load("TableData/LevelInfo") as LevelInfo;
		plantInfo = Resources.Load("TableData/plantInfo") as PlantInfo;
		GameStart();
	}
	public void GameStart()
	{
		UIManager.Instance.InitUI();
		SoundManager.Instance.PlayBGM(Globals.BGM3);
		CameraController.Instance.startPreview().OnComplete(
			() =>
			{
				AllCardPanel.Instance.panelUp();
				ChooseCardPanel.Instance.panelDown();
			});
	}
	public void GameReallyStart()
	{
		SoundManager.Instance.OkReadyStartSound( 
			() =>
			{
				SoundManager.Instance.FadeOutBGMAndPlayNewOne(Globals.BGM1);
				GameManager.Instance.gameStart = true;
				CreateZombie();
				InvokeRepeating("CreateSunDown", SunCreateSpeedFirst, SunCreateSpeed);
			}, StartText);
	}
	public void CreateZombie() {
		curProgressZombie = new Dictionary<GameObject, int>();
		TableCreateZombie();
		UIManager.Instance.InitProgressPanel();
	}
	private void TableCreateZombie()
	{
		bool canCreate = false;
		for(int i = 0; i < levelData.levelDataList.Count; i++)
		{
			LevelItem levelItem = levelData.levelDataList[i];
			if(levelItem.levelId == curLevelId && levelItem.progressId == curProgressId) {
				StartCoroutine(ITableCreateZombie(levelItem));
				canCreate = true;
			}
		}
		if(!canCreate)
		{
			StopAllCoroutines();
			gameStart = false;
			curProgressZombie = new Dictionary<GameObject, int>();
		}
	}
	IEnumerator ITableCreateZombie(LevelItem levelItem)
	{
		yield return new WaitForSeconds(levelItem.createTime);
		// Debug.Log(levelItem.createTime + ", progressId: " + levelItem.progressId);
		if(firstStart) {
			SoundManager.Instance.PlaySound(Globals.S_TheZombiesAreComing);
			firstStart = false;
		}
		GameObject zombiePrefab = Resources.Load("Prefab/Zombie" + levelItem.zombieType.ToString()) as GameObject;
		GameObject zombie = Instantiate(zombiePrefab);
		Transform zombieLine = bornParent.transform.Find("born" + levelItem.bornPos.ToString());
		zombie.transform.parent = zombieLine;
		zombie.transform.localPosition = Vector3.zero;
		zombie.GetComponent<Renderer>().sortingOrder = zOrderIndex++;
		curProgressZombie.Add(zombie, levelItem.bornPos);
	}
	public void ZombieDead(GameObject zombie)
	{
		if(curProgressZombie.ContainsKey(zombie))
		{
			curProgressZombie.Remove(zombie);
			UIManager.Instance.UpdateProgressPanel();
		}
		if(curProgressZombie.Count <= 0)
		{
			curProgressId++;
			if(curProgressId < levelInfo.LevelInfoList[curLevelId - 1].progressPercent.Count)
			{
				StartText.transform.Find("ManyZombie").gameObject.SetActive(true);
				SoundManager.Instance.PlaySoundTimeCallback(Globals.S_Hugewave, 2f, () =>
				{
					StartText.transform.Find("ManyZombie").gameObject.SetActive(false);
				}, 0.5f);
			} else
			{
				if(curProgressId > levelInfo.LevelInfoList[curLevelId - 1].progressPercent.Count)
					return;
				StartText.transform.Find("FinalWave").gameObject.SetActive(true);
				SoundManager.Instance.PlaySoundTimeCallback(Globals.S_Finalwave, 2f, () =>
				{
					StartText.transform.Find("FinalWave").gameObject.SetActive(false);
				}, 0.5f);
			}
			
			TableCreateZombie();
		}
	}
	public void CreateSunDown()
	{
		Vector3 leftBottom = Camera.main.ViewportToWorldPoint(Vector2.zero);
		Vector3 rightTop = Camera.main.ViewportToWorldPoint(Vector2.one);
		GameObject sunPrefab = Resources.Load("Prefab/NormalSun") as GameObject;
		float x = Random.Range(leftBottom.x + 30, rightTop.x - 30);
		Vector3 bornPos = new Vector3(x, rightTop.y, -1);
		GameObject sun = Instantiate(sunPrefab, bornPos, Quaternion.identity);
		float y = Random.Range(leftBottom.y + 100, leftBottom.y + 30);
		sun.GetComponent<NormalSun>().SetTargetPos(new Vector3(bornPos.x, y, 0));
	}
	public int getPlantLine(GameObject plant)
	{
		GameObject lineObject = plant.transform.parent.parent.gameObject;
		string lineString = lineObject.name;
		int line = int.Parse(lineString.Split("line")[1]);
		return line;
	}
	public List<GameObject> getLineZombies(int line)
	{
		string lineName = "born" + line.ToString();
		Transform bornObject = bornParent.transform.Find(lineName);
		List<GameObject> zombies = new List<GameObject>();
		for(int i = 0; i < bornObject.childCount; i++) 
			zombies.Add(bornObject.GetChild(i).gameObject);
		return zombies;
	}
}


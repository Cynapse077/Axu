using UnityEngine;
using System.Collections;
using System.IO;
using LitJson;
using System.Threading;

public class MainMenu : MonoBehaviour {

	public GameObject menuScreen;
	public Transform canvas;

	[Space(20)]
	public int localMapWidth = 45;
	public int localMapHeight = 30;
	public int worldMapWidth = 200;
	public int worldMapHeight = 200;
	public int currentSelected = 1;

	bool canLoad = true;
	SoundManager soundManager;
	bool canUseInput = false;
	readonly bool showOptions = false;
	MainMenuPanel mmp;

	void Start() {
		LocalizationManager.LoadLocalizedData();
		StartCoroutine("Init");
	}

	public void DeleteSaveData() {
		File.Delete(Manager.SaveDirectory);
		File.Delete(Manager.SettingsDirectory);
		UnityEngine.SceneManagement.SceneManager.LoadScene(0);
	}

	IEnumerator Init() {
		yield return LocalizationManager.done;
		ObjectManager.SpawnedNPCs = 0;

		GameObject g = (GameObject)Instantiate(menuScreen, canvas);
		mmp = g.GetComponent<MainMenuPanel>();
		soundManager = GetComponent<SoundManager>();

		if (!FilesExist()) {
			currentSelected = 1;
			mmp.DisableContinueButton();
			canLoad = false;
		} else
			currentSelected = 0;

		ReadSettings();
        FillDataLists();
	}

	void FillDataLists() {
		if (Manager.localMapSize == null) 
			Manager.localMapSize = new Coord(localMapWidth, localMapHeight);
		if (Manager.worldMapSize == null) 
			Manager.worldMapSize = new Coord(worldMapWidth, worldMapHeight);

		SpriteManager.Init();
		LuaManager.LoadScripts();
		NameGenerator.FillSylList(Application.streamingAssetsPath);
		SeedManager.InitializeSeeds();
		ItemList.CreateItems();
		SkillList.FillList();
		FactionList.InitializeFactionList();
		EntityList.FillListFromData();
		Tile.InitializeTileDictionary();

        canUseInput = true;
    }

	bool UpPressed() {
		return(GameSettings.Keybindings.GetKey("North") || Input.GetKeyDown(KeyCode.UpArrow));
	}

	bool DownPressed() {
		return(GameSettings.Keybindings.GetKey("South") || Input.GetKeyDown(KeyCode.DownArrow));
	}

	bool SaveOkayToUse() {
		string jsonString = File.ReadAllText(Manager.SaveDirectory);
		JsonData jData = JsonMapper.ToObject(jsonString)["Version"];
		bool canUse = (jData != null && jData.ToString() == GameSettings.version);

		if (!canUse)
			DeleteSaveData();

		return canUse;
	}

	void Update () {
		if (!canUseInput)
            return;

		if (!showOptions) {
			if (UpPressed() && currentSelected > 0) {
				if (currentSelected == 1 && !canLoad)
					return;

				currentSelected --;
				currentSelected = Mathf.Clamp(currentSelected, 0, 2);
				soundManager.MenuTick();
			}

			if (DownPressed() && currentSelected < 2) {
				currentSelected ++;
				soundManager.MenuTick();
				currentSelected = Mathf.Clamp(currentSelected, 0, 2);
			}

			if (GameSettings.Keybindings.GetKey("Enter")) {
				if (currentSelected == 0) 
					Continue();
				else if (currentSelected == 1) 
					New();
				else if (currentSelected == 2) 
					Quit();

				soundManager.MenuTick();
			}
				
			if (!canLoad && currentSelected < 1)
				currentSelected = 1;
		}
	}

	public void SetSelectedNum(int num) {
        currentSelected = num;
    }

	public void New() {
		currentSelected = 1;
		StartCoroutine(NewGame()); 
	}

	public IEnumerator NewGame() {
		Manager.newGame = true;
		Manager.startWeather = Weather.Clear;
		yield return new WaitForSeconds(0.01f);

		AsyncOperation newGame = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(1);
		yield return newGame;
	}

	public void Continue() {
		StartCoroutine("LoadMainGame");
	}

	IEnumerator LoadMainGame() {
		Manager.newGame = false;
		mmp.StartGame();
		yield return new WaitForSeconds(0.01f);
		
		AsyncOperation loadGame = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(2);
		yield return loadGame;
	}

	public void Quit() {
		currentSelected = 2;
		Application.Quit();
	}

	bool FilesExist() {
		return (File.Exists(Manager.SaveDirectory) && SaveOkayToUse());
	}

	void ReadSettings() {
		GameSettings.InitializeFromFile();

		if (File.Exists(Application.streamingAssetsPath + "/Data/Game.json")) {
			string gData = File.ReadAllText(Application.streamingAssetsPath + "/Data/Game.json");
			JsonData dat = JsonMapper.ToObject(gData);

			if (dat.ContainsKey("World")) {
				Manager.worldMapSize = new Coord(200, 200);
				WorldMap.BiomePath = dat["World"]["Tileset"].ToString();
                WorldMap.LandmarkPath = dat["World"]["Location Sprites"].ToString();
                WorldMap_Data.ZonePath = dat["World"]["Locations"].ToString();
			}

			if (dat.ContainsKey("Local")) {
				Manager.localMapSize = new Coord((int)dat["Local"]["Size"][0], (int)dat["Local"]["Size"][1]);
                Manager.localMapSize.x = Mathf.Clamp(Manager.localMapSize.x, 15, 100);
                Manager.localMapSize.y = Mathf.Clamp(Manager.localMapSize.y, 15, 100);
                TileMap.imagePath = dat["Local"]["Tileset"].ToString();
				Manager.localStartPos = new Coord((int)dat["Local"]["Start Position"][0], (int)dat["Local"]["Start Position"][1] - Manager.localMapSize.y);
			}

			if (dat.ContainsKey("Day Length")) {
				TurnManager.dayLength = (int)dat["Day Length"]["Day"];
				TurnManager.nightLength = (int)dat["Day Length"]["Night"];
			}

			if (dat.ContainsKey("Data")) {
				EntityList.dataPath = dat["Data"]["NPCs"].ToString();
				EntityList.bodyDataPath = dat["Data"]["Body Structures"].ToString();
				NPCGroupList.dataPath = dat["Data"]["Spawn Tables"].ToString();
				TileMap_Data.defaultMapPath = dat["Data"]["Default Maps"].ToString();
				QuestList.dataPath = dat["Data"]["Quests"].ToString();
				FactionList.dataPath = dat["Data"]["Factions"].ToString();
				SkillList.dataPath = dat["Data"]["Abilities"].ToString();

				ItemList.artDataPath = dat["Data"]["Artifacts"].ToString();
				ItemList.itemDataPath = dat["Data"]["Items"].ToString();
				ItemList.natItemDataPath = dat["Data"]["Natural Items"].ToString();
				ItemList.modDataPath = dat["Data"]["Item Modifiers"].ToString();
				ItemList.objDataPath = dat["Data"]["Objects"].ToString();
				ItemList.liqDataPath = dat["Data"]["Liquids"].ToString();
			}
		}

        Tile.filePath = Application.streamingAssetsPath + "/Data/Maps/LocalTiles.json";
	}
}

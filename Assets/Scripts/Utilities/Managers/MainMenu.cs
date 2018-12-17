using UnityEngine;
using System.Collections;
using System.IO;
using LitJson;

public class MainMenu : MonoBehaviour
{
    public GameObject menuScreen;
    public Transform canvas;
    public LoadSaveMenu loadMenu;

    [Space(20)]
    public int localMapWidth = 45;
    public int localMapHeight = 30;
    public int worldMapWidth = 200;
    public int worldMapHeight = 200;
    public int currentSelected = 1;

    SoundManager soundManager;
    bool canUseInput = false;
    readonly bool showOptions = false;
    MainMenuPanel mmp;

    void Start()
    {
        LocalizationManager.LoadLocalizedData();
        loadMenu.gameObject.SetActive(false);
        Manager.playerName = "";
        StartCoroutine("Init");
    }

    IEnumerator Init()
    {
        yield return LocalizationManager.done;
        ObjectManager.SpawnedNPCs = 0;

        GameObject g = Instantiate(menuScreen, canvas);
        mmp = g.GetComponent<MainMenuPanel>();
        soundManager = GetComponent<SoundManager>();

        if (!Directory.Exists(Manager.SaveDirectory))
        {
            Directory.CreateDirectory(Manager.SaveDirectory);
        }

        currentSelected = 0;

        ReadSettings();
        FillDataLists();
    }

    void FillDataLists()
    {
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

    bool UpPressed()
    {
        return (GameSettings.Keybindings.GetKey("North") || Input.GetKeyDown(KeyCode.UpArrow));
    }

    bool DownPressed()
    {
        return (GameSettings.Keybindings.GetKey("South") || Input.GetKeyDown(KeyCode.DownArrow));
    }

    void Update()
    {
        if (!canUseInput)
            return;

        if (!showOptions)
        {
            if (UpPressed() && currentSelected > 0)
            {
                currentSelected--;
                currentSelected = Mathf.Clamp(currentSelected, 0, 2);
                soundManager.MenuTick();
            }

            if (DownPressed() && currentSelected < 2)
            {
                currentSelected++;
                soundManager.MenuTick();
                currentSelected = Mathf.Clamp(currentSelected, 0, 2);
            }

            if (GameSettings.Keybindings.GetKey("Enter"))
            {
                if (currentSelected == 0)
                    Continue();
                else if (currentSelected == 1)
                    New();
                else if (currentSelected == 2)
                    Quit();

                soundManager.MenuTick();
            }
        }
    }

    public void SetSelectedNum(int num)
    {
        currentSelected = num;
    }

    public void New()
    {
        currentSelected = 1;
        StartCoroutine(NewGame());
    }

    public IEnumerator NewGame()
    {
        Manager.newGame = true;
        Manager.startWeather = Weather.Clear;
        yield return new WaitForSeconds(0.01f);

        AsyncOperation newGame = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(1);
        yield return newGame;
    }

    public void Continue()
    {
        mmp.StartGame();
        loadMenu.gameObject.SetActive(true);
        loadMenu.SetupButtons();
        canUseInput = false;
    }

    public void Quit()
    {
        currentSelected = 2;
        Application.Quit();
    }

    void ReadSettings()
    {
        GameSettings.InitializeFromFile();
        Tile.filePath = Application.streamingAssetsPath + "/Data/Maps/LocalTiles.json";

        string campaignPath = Application.streamingAssetsPath + "/Data/Game.json";
        string gData = File.ReadAllText(campaignPath);
        JsonData dat = JsonMapper.ToObject(gData);

        if (dat.ContainsKey("World"))
        {
            Manager.worldMapSize = new Coord(200, 200);
            WorldMap.BiomePath = dat["World"]["Tileset"].ToString();
            WorldMap.LandmarkPath = dat["World"]["Location Sprites"].ToString();
            WorldMap_Data.ZonePath = dat["World"]["Locations"].ToString();
        }

        if (dat.ContainsKey("Local"))
        {
            Manager.localMapSize = new Coord((int)dat["Local"]["Size"][0], (int)dat["Local"]["Size"][1]);
            Manager.localMapSize.x = Mathf.Clamp(Manager.localMapSize.x, 15, 100);
            Manager.localMapSize.y = Mathf.Clamp(Manager.localMapSize.y, 15, 100);
            TileMap.imagePath = dat["Local"]["Tileset"].ToString();
            Manager.localStartPos = new Coord((int)dat["Local"]["Start Position"][0], (int)dat["Local"]["Start Position"][1] - Manager.localMapSize.y);
        }

        if (dat.ContainsKey("Day Length"))
        {
            TurnManager.dayLength = (int)dat["Day Length"]["Day"];
            TurnManager.nightLength = (int)dat["Day Length"]["Night"];
        }

        if (dat.ContainsKey("Data"))
        {
            EntityList.dataPath = dat["Data"]["NPCs"].ToString();
            EntityList.bodyDataPath = dat["Data"]["Body Structures"].ToString();
            NPCGroupList.dataPath = dat["Data"]["Spawn Tables"].ToString();
            TileMap_Data.defaultMapPath = dat["Data"]["Maps"].ToString();
            QuestList.dataPath = dat["Data"]["Quests"].ToString();
            FactionList.dataPath = dat["Data"]["Factions"].ToString();
            SkillList.dataPath = dat["Data"]["Abilities"].ToString();

            ItemList.artDataPath = dat["Data"]["Artifacts"].ToString();
            ItemList.itemDataPath = dat["Data"]["Items"].ToString();
            ItemList.natItemDataPath = dat["Data"]["Natural Items"].ToString();
            ItemList.modDataPath = dat["Data"]["Item Modifiers"].ToString();
            ItemList.objDataPath = dat["Data"]["Objects"].ToString();
            ItemList.liqDataPath = dat["Data"]["Liquids"].ToString();
            TraitList.traitPath = dat["Data"]["Traits"].ToString();
            TraitList.woundPath = dat["Data"]["Wounds"].ToString();
            CharacterCreation.felonyPath = dat["Data"]["Classes"].ToString();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using LitJson;

public class MainMenu : MonoBehaviour
{
    public GameObject menuScreen;
    public GameObject partSys;
    public Transform canvas;
    public LoadSaveMenu loadMenu;
    public Image background;
    public Image background2;

    [Space(20)]
    int localMapWidth = 45;
    int localMapHeight = 30;
    int worldMapWidth = 200;
    int worldMapHeight = 200;
    public int currentSelected = 1;

    SoundManager soundManager;
    bool canUseInput = false;
    readonly bool showOptions = false;
    MainMenuPanel mmp;

    const int DefaultScreenWidth = 1280;
    const int DefaultScreenHeight = 720;
    const int DefaultMapSize = 200;

    void Start()
    {
        QualitySettings.vSyncCount = 1;
        background.CrossFadeAlpha(0.1f, 0.01f, false);
        background2.CrossFadeAlpha(0.05f, 0.01f, false);

        loadMenu.gameObject.SetActive(false);
        Manager.playerName = "";
        ObjectManager.playerJournal = null;

        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        ObjectManager.SpawnedNPCs = 0;
        soundManager = GetComponent<SoundManager>();

        if (!Directory.Exists(Manager.SaveDirectory))
        {
            Directory.CreateDirectory(Manager.SaveDirectory);
        }

        currentSelected = 0;

        ReadSettings();
        FillDataLists();
        soundManager.InitializeAndPlay();
        partSys.SetActive(true);
        mmp = Instantiate(menuScreen, canvas).GetComponent<MainMenuPanel>();
        yield break;
    }

    void FillDataLists()
    {
        if (Manager.localMapSize == null)
        {
            Manager.localMapSize = new Coord(localMapWidth, localMapHeight);
        }

        if (Manager.worldMapSize == null)
        {
            Manager.worldMapSize = new Coord(worldMapWidth, worldMapHeight);
        }

        new EventHandler();
        NameGenerator.FillSylList(Application.streamingAssetsPath);
        SeedManager.InitializeSeeds();
        ModManager.InitializeAllMods();
        EntityList.FillListFromData(); //Necessary for body structures

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
        background.CrossFadeAlpha(0.75f, 3.0f, false);
        background2.CrossFadeAlpha(0.4f, 3.0f, false);

        if (!canUseInput)
        {
            return;
        }

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
                {
                    Continue();
                }
                else if (currentSelected == 1)
                {
                    New();
                }
                else if (currentSelected == 2)
                {
                    Quit();
                }

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

    public void EndContinue()
    {
        canUseInput = true;
        mmp.BackToMain();
        loadMenu.gameObject.SetActive(false);
        loadMenu.gameObject.SetActive(false);
    }

    public void Quit()
    {
        currentSelected = 2;
        Application.Quit();
    }

    void ReadSettings()
    {
        GameSettings.InitializeFromFile();
        string gameFile = Application.streamingAssetsPath + "/Game.json";
        JsonData dat = JsonMapper.ToObject(File.ReadAllText(gameFile));

        TurnManager.dayLength = 4000;
        TurnManager.nightLength = 2000;
        if (dat.ContainsKey("Day Length"))
        {
            dat["Day Length"].TryGetInt("Day", out TurnManager.dayLength, TurnManager.dayLength);
            dat["Day Length"].TryGetInt("Night", out TurnManager.nightLength, TurnManager.nightLength);
        }

        dat.TryGetCoord("Default Screen Size", out GameSettings.DefaultScreenSize, new Coord(DefaultScreenWidth, DefaultScreenHeight));
        dat.TryGetInt("Respawn Time", out GameSettings.RespawnTime, 6000);

        if (dat.ContainsKey("World Map"))
        {
            dat["World Map"].TryGetCoord("Size", out Manager.worldMapSize, new Coord(DefaultMapSize));
            WorldMap.BiomePath = dat["World Map"]["Texture"].ToString();
            WorldMap.LandmarkPath = dat["World Map"]["Location Texture"].ToString();
        }

        if (dat.ContainsKey("Local Map"))
        {
            dat["Local Map"].TryGetCoord("Size", out Manager.localMapSize, new Coord(localMapWidth, localMapHeight));
            TileMap.imagePath = dat["Local Map"]["Texture"].ToString();
        }
    }
}

using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LitJson;

public class LoadSaveMenu : MonoBehaviour
{
    public GameObject loadButtonPrefab;
    public Transform loadButtonAnchor;
    public Text infoText;
    public YesNoPanel ynPanel;

    List<SaveGameObject> savedGames;
    int currentSelected = 0;

    public void SetupButtons()
    {
        loadButtonAnchor.DestroyChildren();
        savedGames = new List<SaveGameObject>();

        string modPath = (Manager.SaveDirectory);
        string[] ss = Directory.GetFiles(modPath, "*.axu", SearchOption.AllDirectories);

        GetDataFromDirectory(ss);

        if (savedGames.Count > 0)
        {
            savedGames = savedGames.OrderByDescending(o => o.time).ToList();

            foreach (SaveGameObject sg in savedGames)
            {
                GameObject button = Instantiate(loadButtonPrefab, loadButtonAnchor);
                button.GetComponentInChildren<Text>().text = sg.charName + " - <color=grey>" + sg.time + "</color>";
                MainMenu_LoadButton mmlb = button.GetComponent<MainMenu_LoadButton>();
                mmlb.Init(this);
                mmlb.deleteButton.onClick.AddListener(() => {
                    ynPanel.gameObject.SetActive(true);
                    ynPanel.Display("Really delete this file?",
                        () => { DeleteSaveFile(button.transform.GetSiblingIndex()); ynPanel.gameObject.SetActive(false); },
                        () => { ynPanel.gameObject.SetActive(false); }, "");
                    });
                button.GetComponent<Button>().onClick.AddListener(() => { LoadGame(button.transform.GetSiblingIndex()); });
            }

            HoverFile(0);
        }
    }

    public void DeleteSaveFile(int index)
    {
        int m = index;
        string modPath = (Manager.SaveDirectory + "/" + savedGames[m].charName + ".axu");

        if (File.Exists(modPath))
        {
            File.Delete(modPath);
            SetupButtons();
        }
    }

    void GetDataFromDirectory(string[] ss)
    {
        foreach (string s in ss)
        {
            string jsonString = File.ReadAllText(s);
            JsonData d = JsonMapper.ToObject(jsonString);

            if (!d.ContainsKey("Player") || !d["Player"].ContainsKey("Name") 
                || !d.ContainsKey("Version") || !d.ContainsKey("World") || !d["World"].ContainsKey("Time"))
            {
                return;
            }

            SaveGameObject sgo = new SaveGameObject()
            {
                charName = d["Player"]["Name"].ToString(),
                charProf = d["Player"]["ProfName"].ToString(),
                level = (int)d["Player"]["xpLevel"]["CurrentLevel"],
                days = ((int)d["World"]["Turn_Num"] / (TurnManager.dayLength + TurnManager.nightLength)) + 1,
                version = d["Version"].ToString(),
                time = d["World"]["Time"].ToString(),
                diffName = ((Difficulty.DiffLevel)((int)d["World"]["Diff"]["Level"])).ToString()
            };

            savedGames.Add(sgo);
        }
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
        if (savedGames.Count > 0)
        {
            if (UpPressed())
            {
                currentSelected--;

                if (currentSelected < 0)
                {
                    currentSelected = savedGames.Count - 1;
                }

                HoverFile(currentSelected);
            }

            if (DownPressed())
            {
                currentSelected++;

                if (currentSelected >= savedGames.Count)
                {
                    currentSelected = 0;
                }

                HoverFile(currentSelected);
            }

            if (GameSettings.Keybindings.GetKey("Enter"))
            {
                LoadGame(currentSelected);
            }
        }
        
        if (GameSettings.Keybindings.GetKey("Pause"))
        {
            Back();
        }
    }

    public void Back()
    {
        GameObject.FindObjectOfType<MainMenu>().EndContinue();
        Manager.playerName = "";
    }

    public void LoadGame(int index)
    {
        Manager.newGame = false;
        Manager.playerName = savedGames[index].charName;
        StartCoroutine(AsyncLoad());
        gameObject.SetActive(false);
    }

    IEnumerator AsyncLoad()
    {
        AsyncOperation load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(2);

        yield return load;
    }

    public void HoverFile(int i)
    {
        currentSelected = i;
        EventSystem.current.SetSelectedGameObject(loadButtonAnchor.GetChild(currentSelected).gameObject);

        infoText.text = string.Format("{0} (Level {1})\n\nDifficulty: {2}\nFelony: {3}\nDay {4}\n\n\n<color=grey>VERSION {5}</color>", 
            savedGames[i].charName, savedGames[i].level, savedGames[i].diffName, savedGames[i].charProf, savedGames[i].days, savedGames[i].version);

        if (savedGames[i].version != GameSettings.version)
        {
            infoText.text += " <color=red>[OUT OF DATE] - May not work correctly</color>";
        }
    }

    public struct SaveGameObject
    {
        public string version;
        public string charName;
        public string charProf;
        public int days;
        public int level;
        public string time;
        public string diffName;
    }
}

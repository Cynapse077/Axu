using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class CharacterCreation : MonoBehaviour
{
    public string characterName { get; set; }
    [HideInInspector]
    public List<Trait> appliedTraits = new List<Trait>();

    [Header("UI")]
    public Canvas canvas;
    public YesNoPanel YNPanel;
    public Text[] AttTexts;
    public Transform classAnchor;
    public Transform diffAnchor;
    public Transform profAnchor;
    public Transform traitAnchor;
    public Transform abilAnchor;
    public Text pNameText;
    public Text pDescText;
    public Text diffDescText;
    public GameObject textPrefab;
    public GameObject classPrefab;
    public InputField field;
    public GameObject loadingGO;
    public GameObject DiffPanel;
    public GameObject CharPanel;

    bool canSelectProf = true;
    List<WeaponProficiency> profs;
    int selectedNum = 0, selectedMax = 3, profNum = 0;
    bool confirmStart = false, confirmReturn = false, loading = false, done = false, waitBufferFinished = true;
    List<Felony> professions { get { return GameData.GetAll<Felony>(); } }
    Difficulty[] difficulties;
    Felony currentProf;
    Difficulty currentDiff = null;

    public bool YNOpen
    {
        get { return YNPanel.gameObject.activeSelf; }
    }

    void Awake()
    {
        if (!ModManager.PreInitialized)
        {
            SceneManager.LoadScene(0);
            return;
        }

        DiffPanel.SetActive(false);
        CharPanel.SetActive(true);

        loadingGO.SetActive(true);
    }

    void Start()
    {
        difficulties = new Difficulty[] {
            new Difficulty(Difficulty.DiffLevel.Adventurer, "Diff_0"),
            new Difficulty(Difficulty.DiffLevel.Scavenger, "Diff_1"),
            new Difficulty(Difficulty.DiffLevel.Rogue, "Diff_2"),
            new Difficulty(Difficulty.DiffLevel.Hunted, "Diff_3")
        };

        SetProficiencies();
        Initialize();
    }

    void Initialize()
    {
        Manager.playerBuilder = new PlayerBuilder();

        characterName = NameGenerator.CharacterName(SeedManager.textRandom);
        field.text = characterName;

        SetupUI();
        InitializePanels();
        SelectProfession();

        loadingGO.SetActive(false);
        canSelectProf = true;
    }

    public void RandomName()
    {
        characterName = NameGenerator.CharacterName(SeedManager.textRandom);
        field.text = characterName;
        EndEdit();
    }

    public void InputName()
    {
        characterName = field.text;
        canSelectProf = false;
    }

    public void EndEdit()
    {
        characterName = field.text;
        EventSystem.current.SetSelectedGameObject(classAnchor.GetChild(selectedNum).gameObject);
        StartCoroutine("InputBuffer");
    }

    IEnumerator InputBuffer()
    {
        yield return new WaitForSeconds(0.1f);
        canSelectProf = true;
    }

    void SetupUI()
    {
        classAnchor.DestroyChildren();

        for (int i = 0; i < professions.Count; i++)
        {
            GameObject g = Instantiate(classPrefab, classAnchor);
            g.GetComponentInChildren<Text>().text = professions[i].name;
            g.GetComponent<Button>().onClick.AddListener(() => {
                SetSelectedNumber(g.transform.GetSiblingIndex());
                DiffPanel.SetActive(true);
                CharPanel.SetActive(false);
                FillDifficulties();
            });
        }

        EventSystem.current.SetSelectedGameObject(classAnchor.GetChild(0).gameObject);
    }

    void InitializePanels()
    {
        profAnchor.DestroyChildren();
        traitAnchor.DestroyChildren();
        abilAnchor.DestroyChildren();
    }

    void SetProficiencies()
    {
        profs = new List<WeaponProficiency>() {
            new WeaponProficiency("Blade", Proficiencies.Blade),
            new WeaponProficiency("Blunt", Proficiencies.Blunt),
            new WeaponProficiency("Polearm", Proficiencies.Polearm),
            new WeaponProficiency("Axe", Proficiencies.Axe),
            new WeaponProficiency("Firearm", Proficiencies.Firearm),
            new WeaponProficiency("Unarmed", Proficiencies.Unarmed),
            new WeaponProficiency("Misc", Proficiencies.Misc_Object),
            new WeaponProficiency("Throwing", Proficiencies.Throw),
            new WeaponProficiency("Armor", Proficiencies.Armor),
            new WeaponProficiency("Shield", Proficiencies.Shield),
            new WeaponProficiency("Butchery", Proficiencies.Butchery),
            new WeaponProficiency("Martial Arts", Proficiencies.MartialArts)
        };
    }

    void Update()
    {
        if (loading)
        {
            canvas.gameObject.SetActive(false);
            return;
        }

        selectedMax = (DiffPanel.activeSelf) ? difficulties.Length - 1 : professions.Count - 1;
        HandleKeys();
    }

    bool UpPressed()
    {
        return (GameSettings.Keybindings.GetKey("North") || Input.GetKeyDown(KeyCode.UpArrow));
    }

    bool DownPressed()
    {
        return (GameSettings.Keybindings.GetKey("South") || Input.GetKeyDown(KeyCode.DownArrow));
    }

    void HandleKeys()
    {
        if (!waitBufferFinished || !canSelectProf)
        {
            return;
        }

        if (GetKey("Enter"))
        {
            World.soundManager.MenuTick();
            if (EventSystem.current.currentSelectedGameObject == field.transform.GetChild(0).gameObject)
            {
                EventSystem.current.SetSelectedGameObject(classAnchor.GetChild(selectedNum).gameObject);
                return;
            }
            if (!confirmStart && !confirmReturn)
            {
                if (DiffPanel.activeSelf)
                {
                    ConfirmStart();
                }
                else
                {
                    DiffPanel.SetActive(true);
                    CharPanel.SetActive(false);
                    FillDifficulties();
                }
                return;
            }
        }

        if (GetKey("Pause"))
        {
            if (EventSystem.current.currentSelectedGameObject == field.transform.GetChild(0).gameObject)
            {
                EventSystem.current.SetSelectedGameObject(classAnchor.GetChild(selectedNum).gameObject);
            }
            else
            {
                Back();
            }
        }

        if (UpPressed())
        {
            int newNum = selectedNum - 1;
            World.soundManager.MenuTick();
            SetSelectedNumber(newNum);

        }
        else if (DownPressed())
        {
            int newNum = selectedNum + 1;
            World.soundManager.MenuTick();
            SetSelectedNumber(newNum);
        }
    }

    void FillDifficulties()
    {
        for (int i = 0; i < difficulties.Length; i++)
        {
            diffAnchor.GetChild(i).GetComponentInChildren<Text>().text = LocalizationManager.GetLocalizedContent(difficulties[i].descTag).display;
            diffAnchor.GetChild(i).GetComponent<Button>().onClick.RemoveAllListeners();
            diffAnchor.GetChild(i).GetComponent<Button>().onClick.AddListener(ConfirmStart);
        }

        int cur = 2;

        selectedNum = cur;
        currentDiff = difficulties[cur];
        diffDescText.text = LocalizationManager.GetLocalizedContent(difficulties[cur].descTag).display2;
        diffAnchor.GetChild(selectedNum).Highlight();
    }

    public void SetSelectedNumber(int num)
    {
        selectedNum = num;

        if (selectedNum < 0)
            selectedNum = selectedMax;
        else if (selectedNum > selectedMax)
            selectedNum = 0;

        if (CharPanel.activeSelf)
        {
            EventSystem.current.SetSelectedGameObject(classAnchor.GetChild(selectedNum).gameObject);
            SelectProfession();
        }
        else if (DiffPanel.activeSelf)
        {
            currentDiff = difficulties[selectedNum];
            EventSystem.current.SetSelectedGameObject(diffAnchor.GetChild(selectedNum).gameObject);
            diffDescText.text = LocalizationManager.GetLocalizedContent(difficulties[selectedNum].descTag).display2;
        }
    }


    void SelectProfession()
    {
        if (confirmStart || confirmReturn || loading || DiffPanel.activeSelf)
            return;

        profNum = selectedNum;
        Felony p = professions[profNum];
        InitializePanels();

        AttTexts[0].text = AttTexts[0].GetComponent<LocalizedText>().BaseText + ": <color=orange>" + ((p.ID == "experiment") ? "??" : p.STR.ToString()) + "</color>";
        AttTexts[1].text = AttTexts[1].GetComponent<LocalizedText>().BaseText + ": <color=orange>" + ((p.ID == "experiment") ? "??" : p.DEX.ToString()) + "</color>";
        AttTexts[2].text = AttTexts[2].GetComponent<LocalizedText>().BaseText + ": <color=orange>" + ((p.ID == "experiment") ? "??" : p.INT.ToString()) + "</color>";
        AttTexts[3].text = AttTexts[3].GetComponent<LocalizedText>().BaseText + ": <color=orange>" + ((p.ID == "experiment") ? "??" : p.END.ToString()) + "</color>";
        pNameText.text = "The " + p.name;
        pDescText.text = p.description;

        appliedTraits.Clear();

        if (p.traits.Length <= 0)
        {
            GameObject g = Instantiate(textPrefab, traitAnchor);
            g.GetComponent<Text>().text = "";
        }
        for (int x = 0; x < p.traits.Length; x++)
        {
            Trait t = TraitList.GetTraitByID(p.traits[x]);
            appliedTraits.Add(t);
            GameObject g = Instantiate(textPrefab, traitAnchor);
            g.GetComponent<Text>().text = string.Format("<color=yellow>{0}</color> - <i>{1}</i>", t.name, t.description);
        }

        profAnchor.DespawnChildren();
        abilAnchor.DespawnChildren();

        if (p.proficiencies.Length <= 0)
        {
            GameObject g = SimplePool.Spawn(textPrefab, profAnchor);
            g.GetComponent<Text>().text = "";
        }

        for (int y = 0; y < p.proficiencies.Length; y++)
        {
            profs[y].level = p.proficiencies[y];

            if (profs[y].level > 0)
            {
                GameObject g = SimplePool.Spawn(textPrefab, profAnchor);
                g.GetComponent<Text>().text = string.Format("<b>" + profs[y].name + "</b> : " + profs[y].CCLevelName());
            }
        }

        if (p.skills.Count <= 0)
        {
            GameObject g = SimplePool.Spawn(textPrefab, abilAnchor);
            g.GetComponent<Text>().text = "";
        }

        for (int z = 0; z < p.skills.Count; z++)
        {
            Ability s = new Ability(GameData.Get<Ability>(p.skills[z].Name));
            GameObject g = SimplePool.Spawn(textPrefab, abilAnchor);
            g.GetComponent<Text>().text = string.Format("<color=yellow>{0}</color> - <i>{1}</i>", s.Name, s.Description);
        }

        currentProf = p;
    }

    public void Back()
    {
        if (!confirmReturn)
        {
            if (YNOpen)
            {
                YNPanel.noButton.onClick.Invoke();
                return;
            }
            if (DiffPanel.activeSelf)
            {
                DiffPanel.SetActive(false);
                CharPanel.SetActive(true);
                selectedNum = profNum;
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(classAnchor.GetChild(selectedNum).gameObject);
            }
            else
            {
                YNPanel.gameObject.SetActive(true);
                YNPanel.Display("YN_MainMenu".Translate(), () => { LoadMainMenu(); }, () => { EndConfirmReturn(); }, "");
                confirmReturn = true;
            }
        }
    }

    public void LoadMainMenu()
    {
        StartCoroutine(AsyncLoad());
    }

    IEnumerator AsyncLoad()
    {
        DiffPanel.SetActive(false);
        CharPanel.SetActive(false);
        loadingGO.SetActive(true);
        Manager.newGame = false;
        Manager.playerName = "";

        AsyncOperation async = SceneManager.LoadSceneAsync(0);

        while (!async.isDone)
        {
            yield return null;
        }
    }

    public void ConfirmStart()
    {
        confirmStart = true;
        YNPanel.gameObject.SetActive(true);
        YNPanel.Display("YN_StartProf".Translate(), () => { SendDataToManager(); }, () => { EndConfirmStart(); }, currentProf.name);
    }

    void EndConfirmStart()
    {
        YNPanel.gameObject.SetActive(false);
        waitBufferFinished = false;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(diffAnchor.GetChild(selectedNum).gameObject);
        StartCoroutine(waitTime());
        confirmStart = false;
    }

    void EndConfirmReturn()
    {
        YNPanel.gameObject.SetActive(false);
        SetSelectedNumber(selectedNum);
        waitBufferFinished = false;
        StartCoroutine(waitTime());
        confirmReturn = false;
    }

    IEnumerator waitTime()
    {
        yield return new WaitForSeconds(0.2f);
        waitBufferFinished = true;
    }

    bool GetKey(string name, KeyPress press = KeyPress.Down)
    {
        return GameSettings.Keybindings.GetKey(name, press);
    }

    string GetWepFromProfs()
    {
        int highest = 0, profNum = 0;

        for (int i = 0; i < profs.Count; i++)
        {
            if (profs[i].GetProficiency() == Proficiencies.Armor || profs[i].GetProficiency() == Proficiencies.Butchery)
            {
                continue;
            }
            if (profs[i].level > highest)
            {
                highest = profs[i].level;
                profNum = i;
            }
        }

        if (profNum == 0)
            return "dagger"; //blade
        if (profNum == 1)
            return "club"; //blunt
        if (profNum == 2)
            return "staff"; //polearms
        if (profNum == 3)
            return "hatchet"; //axe
        if (profNum == 4)
            return "dagger"; //firearm
        if (profNum == 5)
            return "fistfillers"; //unarmed
        if (profNum == 6)
            return "dagger"; //misc
        if (profNum == 7)
            return "javelin"; //throwing

        return "dagger"; //default
    }

    int MaxHealth()
    {
        int hp = (Manager.playerBuilder.attributes["Endurance"] * 3) + 9;

        for (int i = 0; i < appliedTraits.Count; i++)
        {
            if (appliedTraits[i] != null && appliedTraits[i].stats != null)
            {
                for (int j = 0; j < appliedTraits[i].stats.Count; j++)
                {
                    if (appliedTraits[i].stats[j].Stat == "Health")
                    {
                        hp += appliedTraits[i].stats[j].Amount;
                    }
                }
            }
        }

        return hp + currentProf.HP;
    }

    int MaxStam()
    {
        int st = Manager.playerBuilder.attributes["Endurance"] + (Manager.playerBuilder.attributes["Strength"] / 2) + 3;

        for (int i = 0; i < appliedTraits.Count; i++)
        {
            if (appliedTraits[i] != null && appliedTraits[i].stats != null)
            {
                for (int j = 0; j < appliedTraits[i].stats.Count; j++)
                {
                    if (appliedTraits[i].stats[j].Stat == "Stamina")
                    {
                        st += appliedTraits[i].stats[j].Amount;
                    }
                }
            }
        }

        return st + currentProf.ST;
    }

    void SendDataToManager()
    {
        confirmStart = false;
        loading = true;

        World.difficulty = currentDiff;

        if (World.difficulty.Level == Difficulty.DiffLevel.Hunted)
        {
            World.BaseDangerLevel += 2;
        }

        Manager.profName = currentProf.name;
        Manager.ClearFiles();

        Manager.playerBuilder.money = currentProf.startingMoney;
        Manager.playerName = characterName;
        Manager.worldSeed = System.DateTime.Now.GetHashCode();
        Manager.playerBuilder.attributes["Strength"] = currentProf.STR;
        Manager.playerBuilder.attributes["Dexterity"] = currentProf.DEX;
        Manager.playerBuilder.attributes["Intelligence"] = currentProf.INT;
        Manager.playerBuilder.attributes["Endurance"] = currentProf.END;
        Manager.playerBuilder.attributes["Accuracy"] = 1;
        Manager.playerBuilder.attributes["Defense"] = 0;
        Manager.playerBuilder.attributes["Stealth"] = 6;
        Manager.playerBuilder.attributes["Heat Resist"] = 0;
        Manager.playerBuilder.attributes["Cold Resist"] = 0;
        Manager.playerBuilder.attributes["Energy Resist"] = 0;
        Manager.playerBuilder.attributes["Charisma"] = 0;

        if (currentProf.ID == "experiment")
        {
            Manager.playerBuilder.attributes["Strength"] = Random.Range(4, 8);
            Manager.playerBuilder.attributes["Dexterity"] = Random.Range(4, 8);
            Manager.playerBuilder.attributes["Intelligence"] = Random.Range(4, 8);
            Manager.playerBuilder.attributes["Endurance"] = Random.Range(4, 8);
        }

        Manager.playerBuilder.traits = appliedTraits;

        Manager.playerBuilder.statusEffects = new Dictionary<string, int>();

        Manager.playerBuilder.proficiencies = new PlayerProficiencies();
        Manager.playerBuilder.proficiencies.Blade.level = profs[0].level + 1;
        Manager.playerBuilder.proficiencies.Blunt.level = profs[1].level + 1;
        Manager.playerBuilder.proficiencies.Polearm.level = profs[2].level + 1;
        Manager.playerBuilder.proficiencies.Axe.level = profs[3].level + 1;
        Manager.playerBuilder.proficiencies.Firearm.level = profs[4].level + 1;
        Manager.playerBuilder.proficiencies.Unarmed.level = profs[5].level + 1;
        Manager.playerBuilder.proficiencies.Misc.level = profs[6].level + 1;
        Manager.playerBuilder.proficiencies.Throwing.level = profs[7].level + 1;
        Manager.playerBuilder.proficiencies.Armor.level = profs[8].level + 1;
        Manager.playerBuilder.proficiencies.Shield.level = profs[9].level + 1;
        Manager.playerBuilder.proficiencies.Butchery.level = profs[10].level + 1;
        Manager.playerBuilder.proficiencies.MartialArts.level = profs[11].level + 1;

        Manager.playerBuilder.maxHP = MaxHealth();
        Manager.playerBuilder.maxST = MaxStam();

        for (int i = 0; i < appliedTraits.Count; i++)
        {
            Manager.playerBuilder.attributes["Defense"] += appliedTraits[i].GetStatIncrease("Defense");
            Manager.playerBuilder.attributes["Accuracy"] += appliedTraits[i].GetStatIncrease("Accuracy");
            Manager.playerBuilder.attributes["Intelligence"] += appliedTraits[i].GetStatIncrease("Intelligence");
            Manager.playerBuilder.attributes["Stealth"] += appliedTraits[i].GetStatIncrease("Stealth");
            Manager.playerBuilder.attributes["Speed"] += appliedTraits[i].GetStatIncrease("Speed");
            Manager.playerBuilder.attributes["Endurance"] += appliedTraits[i].GetStatIncrease("Endurance");

            Manager.playerBuilder.maxHP += appliedTraits[i].GetStatIncrease("Endurance") * 3;
            Manager.playerBuilder.maxST += appliedTraits[i].GetStatIncrease("Endurance");
            Manager.playerBuilder.attributes["Charisma"] += appliedTraits[i].GetStatIncrease("Charisma");
        }

        Manager.playerBuilder.hp = Manager.playerBuilder.maxHP;
        Manager.playerBuilder.st = Manager.playerBuilder.maxST;

        SetupEquipment();

        Manager.playerBuilder.abilities = new List<Ability>();

        for (int i = 0; i < currentProf.skills.Count; i++)
        {
            Ability s = new Ability(GameData.Get<Ability>(currentProf.skills[i].Name));
            FlagsHelper.Set(ref s.origin, Ability.AbilityOrigin.Book);

            Manager.playerBuilder.abilities.Add(s);
        }

        for (int i = 0; i < currentProf.items.Count; i++)
        {
            Item it = ItemList.GetItemByID(currentProf.items[i].String);
            it.amount = currentProf.items[i].Int;
            Manager.playerBuilder.items.Add(it);
        }

        Item wep = currentProf.weapon.NullOrEmpty() ? ItemList.GetItemByID(GetWepFromProfs()) : ItemList.GetItemByID(currentProf.weapon);

        if (!currentProf.firearm.NullOrEmpty())
        {
            Manager.playerBuilder.firearm = ItemList.GetItemByID(currentProf.firearm);
        }

        Manager.playerBuilder.handItems = new List<Item>() { wep };

        StartCoroutine("StartGame");
    }

    IEnumerator StartGame()
    {
        yield return done;
        SceneManager.LoadScene(2);
    }

    void SetupEquipment()
    {
        Manager.playerBuilder.firearm = ItemList.GetNone();
        Manager.playerBuilder.bodyParts = new List<BodyPart>();
        Manager.playerBuilder.items = new List<Item>();
        Manager.playerBuilder.bodyParts = EntityList.GetBodyStructure(currentProf.bodyStructure);
        done = true;
    }

    public static List<BodyPart> SortBodyParts(List<BodyPart> bps)
    {
        List<BodyPart> newBPs = new List<BodyPart>();
        List<BodyPart> heads = bps.FindAll(x => x.slot == ItemProperty.Slot_Head);

        for (int i = 0; i < heads.Count; i++)
        {
            heads[i].displayName = GetDisplayName(heads[i].name, heads.Count > 1, i);

            newBPs.Add(heads[i]);
            bps.Remove(heads[i]);
        }

        List<BodyPart> torsos = bps.FindAll(x => x.slot == ItemProperty.Slot_Chest);

        for (int i = 0; i < torsos.Count; i++)
        {
            torsos[i].displayName = GetDisplayName(torsos[i].name, torsos.Count > 1, i);

            newBPs.Add(torsos[i]);
            bps.Remove(torsos[i]);
        }

        List<BodyPart> backs = bps.FindAll(x => x.slot == ItemProperty.Slot_Back);

        for (int i = 0; i < backs.Count; i++)
        {
            backs[i].displayName = GetDisplayName(backs[i].name, backs.Count > 1, i);

            newBPs.Add(backs[i]);
            bps.Remove(backs[i]);
        }

        List<BodyPart> wings = bps.FindAll(x => x.slot == ItemProperty.Slot_Wing);

        for (int i = 0; i < wings.Count; i++)
        {
            wings[i].displayName = GetDisplayName(wings[i].name, wings.Count > 1, i);

            newBPs.Add(wings[i]);
            bps.Remove(wings[i]);
        }

        List<BodyPart> arms = bps.FindAll(x => x.slot == ItemProperty.Slot_Arm);

        for (int i = 0; i < arms.Count; i++)
        {
            arms[i].displayName = GetDisplayName(arms[i].name, arms.Count > 1, i);

            newBPs.Add(arms[i]);
            bps.Remove(arms[i]);
        }

        List<BodyPart> legs = bps.FindAll(x => x.slot == ItemProperty.Slot_Leg);

        for (int i = 0; i < legs.Count; i++)
        {
            legs[i].displayName = GetDisplayName(legs[i].name, legs.Count > 1, i);

            newBPs.Add(legs[i]);
            bps.Remove(legs[i]);
        }

        List<BodyPart> tails = bps.FindAll(x => x.slot == ItemProperty.Slot_Tail);

        for (int i = 0; i < tails.Count; i++)
        {
            tails[i].displayName = GetDisplayName(tails[i].name, tails.Count > 1, i);

            newBPs.Add(tails[i]);
            bps.Remove(tails[i]);
        }

        return newBPs;
    }

    static string GetDisplayName(string pName, bool moreThanOne, int index)
    {
        if (moreThanOne)
        {
            string s = (index % 2 == 0) ? "Limb_Right" : "Limb_Left";

            return pName + " " + LocalizationManager.GetContent(s);
        }

        return pName;
    }
}

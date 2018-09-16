using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class UserInterface : MonoBehaviour
{
    public static int selectedItemNum = 0;
    public static bool paused = false;
    public static bool loading = false;
    public static bool showConsole = false;
    [HideInInspector] public bool pickedTrait = true;
    [HideInInspector] public int column = 1;

    [Header("UI Panels")]
    public ReplaceLimb_Panel RLPanel;
    public BodyPartTargetPanel BPPanel;
    public InventoryPanel InvPanel;
    public EquipmentPanel EqPanel;
    public ThrowItemPanel ThrowPanel;
    public AbilityPanel AbPanel;
    public GrapplePanel GPanel;
    public TooltipPanel TTPanel;
    public LootPanel LPanel;
    public ShopPanel ShopPanel;
    public AlertPanel AlPanel;
    public YesNoPanel YNPanel;
    public CharacterPanel CharPanel;
    public ItemActionsPanel IAPanel;
    public SlotSelectPanel SSPanel;
    public DialoguePanel DPanel;
    public JournalPanel JPanel;
    public PauseMenuPanel pausePanel;
    public MapFeaturePanel mapFeaturePanel;
    public LevelUpPanel LvlPanel;
    public OptionsPanel optionsPanel;
    public UseItemOnOtherPanel UsePanel;
    public MessageLogUI MLog;
    public Text currentMapText;
    public StatusEffectPanel SEPanel;
    public LookTooltipPanel LTPanel;
    public LiquidActionsPanel LAPanel;
    public ContextualActionsPanel CAPanel;

    public GameObject loadingGO;
    public Image fadePanel;

    int indexToUse, limbToReplaceIndex = -1;
    List<Trait> levelUpTraits = new List<Trait>();
    TileMap tileMap;
    CursorControl cursorControl;
    UIWindow uiState;
    bool playerLooking = false, dead = false, showInfo = false;
    Inventory shopInv, playerInventory, boxInv;
    Body calledShotTarget;
    Entity playerEntity;
    Stats playerStats;
    EntitySkills playerAbilities;
    PlayerInput playerInput;
    bool miniMapOn = true;

    [Space(5)]
    public GameObject miniMapObject;
    public GameObject fullMapObject;

    public bool selectedItemActions
    {
        get { return IAPanel.gameObject.activeSelf; }
    }
    public bool selectBodyPart
    {
        get { return SSPanel.gameObject.activeSelf; }
    }

    public bool canMove
    {
        get { return (uiState == UIWindow.None); }
    }

    void OnEnable()
    {
        World.userInterface = this;
        fadePanel.gameObject.SetActive(true);
    }

    void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != 2)
            return;

        loading = true;
        paused = false;

        miniMapObject.SetActive(miniMapOn);
        fullMapObject.SetActive(false);

        uiState = UIWindow.None;
    }

    public void ShowInitialMessage(string name)
    {
        loadingGO.SetActive(false);
        fadePanel.CrossFadeAlpha(0, 1.5f, false);

        if (Manager.newGame)
            Alert.NewAlert("Start_Message", name, null);
    }

    public void ToggleFullMap(bool fullMap)
    {
        miniMapObject.SetActive(!fullMap && miniMapOn);
        fullMapObject.SetActive(fullMap);
    }

    public void UpdateStatusEffects(Stats stats)
    {
        SEPanel.UpdateEnabledStatuses(stats);
    }

    public void NewLogMessage(string text)
    {
        MLog.NewMessage(text);
    }

    void Update()
    {
        if (dead && (playerInput.keybindings.GetKey("Enter") || playerInput.keybindings.GetKey("Pause")))
        {
            if (World.difficulty.Level == Difficulty.DiffLevel.Rogue || World.difficulty.Level == Difficulty.DiffLevel.Hunted)
            {
                Manager.ClearFiles();
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
            else
            {
                CloseWindows();
                ObjectManager.playerEntity.stats.health = ObjectManager.playerEntity.stats.maxHealth;
                ObjectManager.playerEntity.stats.stamina = ObjectManager.playerEntity.stats.maxStamina;
                ObjectManager.playerEntity.stats.statusEffects.Clear();
                World.tileMap.GoToArea("Home_Base");
                World.tileMap.HardRebuild();
                ObjectManager.playerEntity.UnDie();
                dead = false;
            }

            return;
        }

        if (selectedItemNum > SelectedMax())
            selectedItemNum = 0;
        if (ObjectManager.player != null)
            AssignPlayerValues();

        HandleInput();
        ItemScrolling();

        if (Input.GetKeyDown(KeyCode.Slash) && Input.GetKey(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.F1))
            uiState = UIWindow.Options;
    }

    public void OpenGrapple()
    {
        uiState = UIWindow.Grapple;
        GPanel.gameObject.SetActive(true);
        GPanel.Initialize(playerEntity.GetComponent<Body>());
    }

    public void NewAlert(string title, string content)
    {
        uiState = UIWindow.Alert;
        AlPanel.gameObject.SetActive(true);
        AlPanel.NewAlert(title, content);
    }

    public void CloseAlert()
    {
        CloseWindows();
    }

    public void OpenJournal()
    {
        if (uiState == UIWindow.Journal)
        {
            CloseWindows();
        }
        else if (uiState == UIWindow.None)
        {
            CloseWindows();
            uiState = UIWindow.Journal;
            JPanel.gameObject.SetActive(true);
            JPanel.Init();
        }
    }

    public void OpenInventory(Inventory inv = null)
    {
        if (inv == null)
            inv = playerInventory;

        if (uiState == UIWindow.Inventory)
        {
            CloseWindows();
        }
        else if (uiState == UIWindow.None)
        {
            CloseWindows();
            uiState = UIWindow.Inventory;
            InvPanel.gameObject.SetActive(true);
            EqPanel.gameObject.SetActive(true);
            InvPanel.Init(inv);
            EqPanel.Init(inv);
        }
    }

    public void OpenLoot(Inventory inv)
    {
        CloseWindows();

        if (inv.items == null || inv.items.Count <= 0)
        {
            CombatLog.NewMessage("The container is empty.");
        }
        else
        {
            uiState = UIWindow.Loot;
            boxInv = inv;
            LPanel.gameObject.SetActive(true);
            LPanel.Init(boxInv);
        }
    }

    public void OpenAbilities()
    {
        if (uiState == UIWindow.Abilities)
        {
            CloseWindows();
            return;
        }
        else if (uiState == UIWindow.None)
        {
            CloseWindows();
            AbPanel.gameObject.SetActive(true);
            AbPanel.Init();
            uiState = UIWindow.Abilities;
        }
    }

    public void OpenPauseMenu()
    {
        paused = true;
        uiState = UIWindow.PauseMenu;
        pausePanel.gameObject.SetActive(true);
    }

    public void OpenReplacementMenu(bool fromDoctor)
    {
        uiState = UIWindow.ReplacePartWithItem;
        RLPanel.gameObject.SetActive(true);
        RLPanel.ReplaceLimb(playerInventory, fromDoctor);
    }

    public void Amputate()
    {
        uiState = UIWindow.AmputateLimb;
        BPPanel.gameObject.SetActive(true);
        BPPanel.TargetPart(playerStats.entity.body, BodyPartTargetPanel.SelectionType.Amputate);
    }

    public void OpenCharacterPanel()
    {
        CharPanel.gameObject.SetActive(true);
        CharPanel.Initialize(playerStats, playerInventory);
    }

    public void ItemOnItem_Fill(Item i, Inventory inv)
    {
        CloseWindows();
        uiState = UIWindow.UseItemOnItem;
        UsePanel.gameObject.SetActive(true);
        UsePanel.Init(i, inv, (x => x.GetItemComponent<CLiquidContainer>() != null && x != i), "Fill");
    }

    public void ItemOnItem_Coat(Item i, Inventory inv)
    {
        CloseWindows();
        uiState = UIWindow.UseItemOnItem;
        UsePanel.gameObject.SetActive(true);
        UsePanel.Init(i, inv, (x => x != i), "Pour_Item");
    }

    public void ItemOnItem_Mod(Item i, Inventory inv, CModKit cmod)
    {
        CloseWindows();
        uiState = UIWindow.UseItemOnItem;
        UsePanel.gameObject.SetActive(true);
        UsePanel.Init(i, inv, cmod.ItemsICanAddTo(inv.items), "Mod_Item");
    }

    public void PourActions(Item cl)
    {
        CloseWindows();
        uiState = UIWindow.LiquidActions;
        LAPanel.gameObject.SetActive(true);
        LAPanel.Init(cl);
    }

    public void OpenRelevantWindow(UIWindow window)
    {
        uiState = window;

        switch (uiState)
        {
            case UIWindow.Abilities:
                OpenAbilities();
                break;
            case UIWindow.Inventory:
                OpenInventory();
                break;
            case UIWindow.Character:
                OpenCharacterPanel();
                break;
            case UIWindow.Journal:
                OpenJournal();
                break;
            case UIWindow.PauseMenu:
                OpenPauseMenu();
                break;
            case UIWindow.Options:
                optionsPanel.gameObject.SetActive(true);
                break;
            case UIWindow.UseItemOnItem:
                CloseWindows();
                UsePanel.gameObject.SetActive(true);
                break;
            case UIWindow.SelectItemToThrow:
                ThrowPanel.gameObject.SetActive(true);
                ThrowPanel.Init();
                break;
            default:
                CloseWindows();
                break;
        }
    }

    public void OpenContextActions(List<ContextualMenu.ContextualAction> actions)
    {
        CloseWindows();
        CAPanel.gameObject.SetActive(true);
        uiState = UIWindow.ContextActions;

        CAPanel.Refresh(actions);
    }

    void HandleInput()
    {
        if (loading || playerInput == null || PlayerInput.lockInput)
            return;

        if (playerInput.keybindings.GetKey("Enter"))
            SelectPressed();
        if (playerInput.keybindings.GetKey("Pause"))
            BackPressed();

        if (Input.GetKeyDown(KeyCode.Home) && !PlayerInput.fullMap)
        {
            miniMapOn = !miniMapOn;
            miniMapObject.SetActive(miniMapOn);
        }

        if (boxInv != null && playerInput.keybindings.GetKey("Pickup"))
            SelectPressed();

        if (!paused && uiState != UIWindow.Alert)
        {
            if (playerInput.keybindings.GetKey("Inventory"))
                OpenInventory();
            else if (playerInput.keybindings.GetKey("Character"))
            {
                if (uiState == UIWindow.Character)
                {
                    CloseWindows();
                    return;
                }
                else if (uiState == UIWindow.None || uiState == UIWindow.PauseMenu)
                {
                    uiState = UIWindow.Character;
                    OpenRelevantWindow(UIWindow.Character);
                }
            }
            else if (playerInput.keybindings.GetKey("Abilities"))
                OpenAbilities();
            else if (playerInput.keybindings.GetKey("Journal"))
                OpenJournal();
            if (uiState == UIWindow.Shop)
                boxInv = null;
        }

        if (uiState == UIWindow.Inventory)
        {
            if (playerInput.keybindings.GetKey("East") && !selectedItemActions && !selectBodyPart)
            {
                if (column >= 1 || InvPanel.curInv.items.Count <= 0)
                    return;

                World.soundManager.MenuTick();
                selectedItemNum = 0;
                column++;
                InvPanel.UpdateTooltip();
            }
            if (playerInput.keybindings.GetKey("West") && !selectBodyPart && !selectedItemActions)
            {
                if (column < 1 || selectedItemActions)
                    return;

                selectedItemNum = 0;

                if (!canMove)
                    World.soundManager.MenuTick();

                column--;
                InvPanel.UpdateTooltip();
            }
        }

        if (uiState == UIWindow.ReplacePartWithItem)
        {
            if (playerInput.keybindings.GetKey("East"))
            {
                if (column < 1 && limbToReplaceIndex > -1)
                    column++;
                World.soundManager.MenuTick();
            }
            if (playerInput.keybindings.GetKey("West"))
            {
                if (column > 0)
                    column--;
                World.soundManager.MenuTick();
            }
        }

        if (uiState == UIWindow.Loot)
        {
            if (boxInv != null && boxInv.items.Count > 0)
            {
                if (selectedItemNum > boxInv.items.Count - 1)
                    selectedItemNum = 0;
            }
            else
                selectedItemNum = 0;
        }

        if (uiState == UIWindow.Shop)
        {
            if (shopInv == null)
                return;
            if (playerInput.keybindings.GetKey("East") && !selectedItemActions)
            {
                if (column >= 1)
                    return;

                World.soundManager.MenuTick();
                selectedItemNum = 0;
                column++;
                ShopPanel.UpdateTooltip();
            }
            if (playerInput.keybindings.GetKey("West") && !selectedItemActions)
            {
                if (column < 1)
                    return;
                World.soundManager
                    .MenuTick();
                selectedItemNum = 0;
                column--;
                ShopPanel.UpdateTooltip();
            }
            if (column == 0)
            {
                if (shopInv.items.Count > 0)
                {
                    if (selectedItemNum > shopInv.items.Count - 1)
                        selectedItemNum = 0;
                }
            }
            else if (selectedItemNum > playerInventory.items.Count - 1)
                selectedItemNum = 0;
        }

        if (uiState == UIWindow.ReplacePartWithItem)
        {
            if (playerInput.keybindings.GetKey("East"))
            {
                RLPanel.SwitchMode(1);
                World.soundManager.MenuTick();
            }
            if (playerInput.keybindings.GetKey("West"))
            {
                RLPanel.SwitchMode(0);
                World.soundManager.MenuTick();
            }
        }
    }

    public void SwitchSelectedNum(int amount)
    {
        if (!canMove)
            World.soundManager.MenuTick();

        if (uiState == UIWindow.ReplacePartWithItem)
            RLPanel.SwitchSelectedNum(amount);

        if (selectBodyPart)
            SSPanel.SwitchSelectedNum(amount);
        else if (selectedItemActions)
        {
            IAPanel.SwitchSelectedNum(amount);
        }
        else
        {
            selectedItemNum += amount;

            if (selectedItemNum > SelectedMax())
                selectedItemNum = 0;
            else if (selectedItemNum < 0)
                selectedItemNum = SelectedMax();

            if (uiState == UIWindow.Inventory)
                InvPanel.UpdateTooltip();
            else if (uiState == UIWindow.Shop)
                ShopPanel.UpdateTooltip();
            else if (uiState == UIWindow.Loot)
                LPanel.UpdateTooltip();
            else if (uiState == UIWindow.SelectItemToThrow)
                ThrowPanel.UpdateTooltip();
            else if (uiState == UIWindow.Journal)
                JPanel.UpdateTooltip();
            else if (uiState == UIWindow.PauseMenu)
                pausePanel.UpdateSelected(selectedItemNum);
            else if (uiState == UIWindow.Abilities)
                AbPanel.UpdateTooltip();

        }
    }

    public void OpenSaveAndQuitDialogue()
    {
        CloseWindows();
        YesNoAction("YN_SaveQuit", () => { fadePanel.CrossFadeAlpha(1.0f, 0.3f, true); SaveAndQuit(); }, () => { CloseWindows(); }, "");
    }

    public void LookToolipOn(Transform tr, BaseAI npc)
    {
        LTPanel.gameObject.SetActive(true);

        Vector3 screenPos = Camera.main.WorldToScreenPoint(tr.position);
        LTPanel.gameObject.GetComponent<RectTransform>().pivot = new Vector2((screenPos.x > Screen.width * 0.5f ? 1 : 0), (screenPos.y > Screen.height * 0.5f) ? 1 : 0);

        LTPanel.GetComponent<RectTransform>().position = screenPos;
        LTPanel.HoverOver(npc);
    }

    public void LookToolipOn(Transform tr, MapObjectSprite mos)
    {
        LTPanel.gameObject.SetActive(true);

        Vector3 screenPos = Camera.main.WorldToScreenPoint(tr.position);
        LTPanel.gameObject.GetComponent<RectTransform>().pivot = new Vector2((screenPos.x > Screen.width * 0.5f ? 1 : 0), (screenPos.y > Screen.height * 0.5f) ? 1 : 0);

        LTPanel.GetComponent<RectTransform>().position = screenPos;
        LTPanel.HoverOver(mos);
    }

    public void LookTooltipOff()
    {
        LTPanel.gameObject.SetActive(false);
    }

    void SaveAndQuit()
    {
        loadingGO.SetActive(true);
        loadingGO.GetComponentInChildren<Text>().text = "Saving...";
        World.objectManager.SaveAndQuit();
    }

    public bool ShowItemTooltip()
    {
        if (column == 1)
            return (uiState == UIWindow.Inventory && playerInventory.items.Count > 0 && !selectedItemActions && !selectBodyPart);
        else
            return (uiState == UIWindow.Inventory && !selectedItemActions && !selectBodyPart);
    }

    public void ShowDialogue(DialogueController diaController)
    {
        uiState = UIWindow.Dialogue;
        DPanel.gameObject.SetActive(true);
        DPanel.Display(diaController);
        shopInv = diaController.GetComponent<Inventory>();
    }

    public void Dialogue_Chat(Faction faction, string npcID)
    {
        DPanel.SetText(Dialogue.Chat(faction, npcID));
    }

    public void Dialogue_CustomChat(string text)
    {
        DPanel.SetText(text);
    }

    public void Dialogue_Shop()
    {
        CloseWindows();
        column = 1;
        uiState = UIWindow.Shop;
        ShopPanel.gameObject.SetActive(true);
        ShopPanel.Init(shopInv);
    }

    public void Dialogue_Heal()
    {
        if (playerStats.CostToCureWounds() <= 0)
            return;

        if (playerInventory.gold < playerStats.CostToCureWounds())
        {
            DPanel.SetText(LocalizationManager.GetLocalizedContent("Cannot_Afford")[0]);
            return;
        }

        playerInventory.gold -= playerStats.CostToCureWounds();
        DPanel.SetText(LocalizationManager.GetLocalizedContent("Healed_Wounds")[0]);
        playerStats.CureAllWounds();
    }

    public void Dialogue_ReplaceLimb(bool fromDoctor = true)
    {
        if (fromDoctor)
        {
            if (playerInventory.gold < playerStats.CostToReplaceLimbs())
            {
                DPanel.SetText(LocalizationManager.GetLocalizedContent("Cannot_Afford")[0]);
                return;
            }
        }

        CloseWindows();
        OpenReplacementMenu(fromDoctor);
    }

    public void Dialogue_AmputateLimb()
    {
        CloseWindows();
        Amputate();
    }

    public void YesNoAction(string question, Action yAction, Action nAction, string input)
    {
        if (nAction == null)
            nAction = (() => { CloseWindows(); });

        uiState = UIWindow.YesNoPrompt;
        YNPanel.gameObject.SetActive(true);
        YNPanel.Display(question, yAction, nAction, input);
    }

    public void BanditYes(int goldAmount, Item item)
    {
        World.userInterface.CloseWindows();

        if (item != null && playerInventory.gold > 0)
        {
            Alert.NewAlert("Bandit_Yes");

            for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++)
            {
                BaseAI bai = World.objectManager.onScreenNPCObjects[i].GetComponent<BaseAI>();
                bai.OverrideHostility(false);
            }
        }
        else
        {
            Alert.NewAlert("Bandit_Yes_NoMoney");

            for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++)
            {
                BaseAI bai = World.objectManager.onScreenNPCObjects[i].GetComponent<BaseAI>();
                bai.NoticePlayer();
            }
        }
    }

    public void BanditNo()
    {
        World.userInterface.CloseWindows();
        Alert.NewAlert("Bandit_No");

        for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++)
        {
            BaseAI bai = World.objectManager.onScreenNPCObjects[i].GetComponent<BaseAI>();
            bai.NoticePlayer();
        }
    }

    public UIWindow CurrentState()
    {
        return uiState;
    }

    public void SelectPressed(int selectedNumOverride = -1)
    {
        if (selectedNumOverride > -1)
            selectedItemNum = selectedNumOverride;

        if (uiState == UIWindow.Inventory)
            return;

        //Level Up
        if (uiState == UIWindow.LevelUp)
        {
            if (selectedItemNum > 2)
            {
                pickedTrait = true;
                CloseWindows();
                return;
            }
            playerStats.InitializeNewTrait(levelUpTraits[selectedItemNum]);
            pickedTrait = true;
            CloseWindows();
        }
        else if (uiState == UIWindow.AmputateLimb)
        {
            List<BodyPart> parts = playerInventory.entity.body.SeverableBodyParts();
            indexToUse = selectedItemNum;

            if (parts[indexToUse].isAttached)
            {
                if (parts[indexToUse].slot == ItemProperty.Slot_Head && playerInventory.entity.body.GetBodyPartsBySlot(ItemProperty.Slot_Head).Count < 2)
                {
                    Alert.NewAlert("Cannot_Amputate", UIWindow.AmputateLimb);
                }
                else
                {
                    CloseWindows();
                    YesNoAction("YN_Amputate", () =>
                    {
                        CloseWindows();
                        playerInventory.entity.body.RemoveLimb(parts[indexToUse]);
                    }, null, parts[indexToUse].displayName);
                }
            }
        }
        else if (uiState == UIWindow.SelectItemToThrow && playerInventory.items.Count > 0)
        {
            playerEntity.fighter.SelectItemToThrow(playerInventory.Items_ThrowingFirst()[selectedItemNum]);
            playerInput.ToggleThrow();
            CloseWindows();
        }
        else if (uiState == UIWindow.Alert)
        {
            Alert.CloseAlert();
        }
        else if (paused && uiState == UIWindow.PauseMenu)
        {
            if (selectedItemNum == 0)
            {
                CloseWindows();
                OpenRelevantWindow(UIWindow.Options);
            }
            else if (selectedItemNum == 1)
            {
                CloseWindows();
                YesNoAction("YN_SaveQuit", () => { SaveAndQuit(); }, () => { CloseWindows(); }, "");
            }
            else
            {
                //OTHER STUFF HERE.
            }
        }
        else if (uiState == UIWindow.Journal)
        {
            Journal journal = ObjectManager.playerJournal;
            if (ObjectManager.playerJournal.quests.Count > 0)
            {
                journal.trackedQuest = journal.quests[selectedItemNum];
                JPanel.Init();
            }
        }
        else if (uiState == UIWindow.TargetBodyPart)
        {
            BPPanel.SelectPressed();
            CloseWindows();
        }
        else if (uiState == UIWindow.Shop)
        {
            Select_Shop();
        }
        else if (uiState == UIWindow.Abilities)
        {
            playerAbilities.abilities[selectedItemNum].Cast(playerEntity);
        }
        else if (uiState == UIWindow.Dialogue)
        {
            DPanel.ChooseDialogue();
        }
        else if (uiState == UIWindow.ReplacePartWithItem)
        {
            RLPanel.SelectPressed();
        }
    }

    void Select_Shop()
    {
        int charisma = playerStats.Attributes["Charisma"];

        if (column == 0)
        { //Shop inventory
            bool friendly = shopInv.entity.AI.npcBase.HasFlag(NPC_Flags.Follower);

            if (selectedItemNum >= shopInv.items.Count || shopInv.items[selectedItemNum] == null)
                return;

            int cost = shopInv.items[selectedItemNum].buyCost(charisma);

            if ((playerInventory.canAfford(cost) || friendly) && !playerInventory.atMaxCapacity())
            {
                Item newItem = new Item(shopInv.items[selectedItemNum]);

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                    cost *= newItem.amount;
                else
                    newItem.amount = 1;

                if (friendly)
                    cost = 0;

                if (playerInventory.CanPickupItem(newItem) && playerInventory.canAfford(cost))
                {
                    if (World.soundManager != null)
                        World.soundManager.UseItem();

                    playerInventory.PickupItem(newItem);

                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                        shopInv.RemoveInstance_All(shopInv.items[selectedItemNum]);
                    else
                        shopInv.RemoveInstance(shopInv.items[selectedItemNum]);

                    playerInventory.gold -= cost;
                }
            }
        }
        else
        { //Player Inventory
            if (selectedItemNum >= playerInventory.items.Count || playerInventory.items[selectedItemNum] == null)
                return;

            bool friendly = shopInv.entity.AI.npcBase.HasFlag(NPC_Flags.Follower);
            int cost = playerInventory.items[selectedItemNum].sellCost(charisma);
            Item newItem = new Item(playerInventory.items[selectedItemNum]);

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                cost *= newItem.amount;
            else
                newItem.amount = 1;

            if (World.soundManager != null)
                World.soundManager.UseItem();

            if (friendly && !shopInv.CanPickupItem(newItem))
            {
                Alert.NewAlert("Inv_Full_Follower");
                return;
            }

            shopInv.PickupItem(newItem);

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                playerInventory.RemoveInstance_All(playerInventory.items[selectedItemNum]);
            else
                playerInventory.RemoveInstance(playerInventory.items[selectedItemNum]);

            if (!friendly)
                playerInventory.gold += cost;
        }

        ShopPanel.Init(shopInv);
    }

    public void OpenMap()
    {
        CloseWindows();
        paused = false;
        playerInput.TriggerLocalOrWorldMap();
    }

    public void OpenOptionsFromButton()
    {
        CloseWindows();
        OpenRelevantWindow(UIWindow.Options);
    }

    void BackPressed()
    {
        if (dead)
        {
            return;
        }

        if (!selectedItemActions && !selectBodyPart)
            selectedItemNum = 0;

        if (Options_KeyPanel.WaitingForRebindingInput)
            return;

        if (uiState == UIWindow.Alert)
        {
            Alert.CloseAlert();
            return;
        }
        if (paused)
        {
            if (uiState == UIWindow.Options)
            {
                CloseWindows();
                OpenPauseMenu();
                return;
            }
            else
            {
                CloseWindows();
                paused = false;
                uiState = UIWindow.None;
            }
        }
        else
        {
            if (PlayerInput.fullMap)
                return;
            if (selectedItemActions)
            {
                IAPanel.gameObject.SetActive(false);
                return;
            }
            if (selectBodyPart)
            {
                SSPanel.gameObject.SetActive(false);
                return;
            }
            if (playerLooking)
            {
                playerInput.CancelLook();
                return;
            }
            if (showInfo)
            {
                showInfo = false;
                return;
            }
            if (playerInput.cursorMode == PlayerInput.CursorMode.Direction)
            {
                playerInput.CancelLook();
                return;
            }
            if (uiState == UIWindow.None)
            {
                CloseWindows();
                selectedItemNum = 0;
                OpenPauseMenu();
                return;
            }

            CloseWindows();
        }
    }

    public void InitializeAllWindows(Inventory plInv = null)
    {
        if (plInv == null)
            plInv = playerInventory;

        if (uiState == UIWindow.Inventory)
        {
            InvPanel.Init(plInv);
            EqPanel.Init(plInv);
        }

        else if (uiState == UIWindow.Loot)
            LPanel.Init(boxInv);
        else if (uiState == UIWindow.SelectItemToThrow)
            ThrowPanel.Init();
    }

    public void SetSelectedNumber(int num)
    {
        if (uiState != UIWindow.None)
        {
            if (selectedItemActions)
            {
                InvPanel.UpdateTooltip();
                return;
            }
            if (uiState == UIWindow.Inventory && num > SelectedMax())
            {
                selectedItemNum = SelectedMax();
                InvPanel.UpdateTooltip();
                return;
            }

            selectedItemNum = num;

            if (uiState == UIWindow.Inventory)
                InvPanel.UpdateTooltip();
            else if (uiState == UIWindow.Shop)
                ShopPanel.UpdateTooltip();
            else if (uiState == UIWindow.SelectItemToThrow)
                ThrowPanel.UpdateTooltip();
            else if (uiState == UIWindow.Loot)
                LPanel.UpdateTooltip();
            else if (uiState == UIWindow.Journal)
                JPanel.UpdateTooltip();
            else if (uiState == UIWindow.PauseMenu)
                pausePanel.UpdateSelected(selectedItemNum);
        }
        else if (num < playerAbilities.abilities.Count)
        {
                Skill currAbility = playerAbilities.abilities[num];
                currAbility.Cast(playerEntity);
        }
    }

    int SelectedMax()
    {
        if (selectedItemActions || selectBodyPart)
            return selectedItemNum;

        switch (uiState)
        {
            case UIWindow.PauseMenu:
                return 2;
            case UIWindow.Inventory:
                return (column != 0) ? InvPanel.SelectedMax : EqPanel.SelectedMax;
            case UIWindow.Shop:
                return (column == 1) ? playerInventory.items.Count - 1 : shopInv.items.Count - 1;
            case UIWindow.TargetBodyPart:
                return calledShotTarget.bodyParts.Count - 1;
            case UIWindow.AmputateLimb:
                return playerInventory.entity.body.bodyParts.Count - 1;
            case UIWindow.Abilities:
                return playerAbilities.abilities.Count - 1;
            case UIWindow.Journal:
                return ObjectManager.playerJournal.quests.Count - 1;
            case UIWindow.Dialogue:
                return DPanel.cMax;
            case UIWindow.Loot:
                return LPanel.max;
            case UIWindow.UseItemOnItem:
                return UsePanel.numItems - 1;
            case UIWindow.SelectItemToThrow:
                return playerInventory.Items_ThrowingFirst().Count - 1;
            case UIWindow.LevelUp:
                return Mathf.Max(1, levelUpTraits.Count);
            case UIWindow.LiquidActions:
                return 3;
            default:
                return 0;
        }
    }

    void ClosePanels()
    {
        InvPanel.gameObject.SetActive(false);
        EqPanel.gameObject.SetActive(false);
        AbPanel.gameObject.SetActive(false);
        TTPanel.gameObject.SetActive(false);
        LPanel.gameObject.SetActive(false);
        ShopPanel.gameObject.SetActive(false);
        ThrowPanel.gameObject.SetActive(false);
        AlPanel.gameObject.SetActive(false);
        YNPanel.gameObject.SetActive(false);
        CharPanel.gameObject.SetActive(false);
        IAPanel.gameObject.SetActive(false);
        SSPanel.gameObject.SetActive(false);
        DPanel.gameObject.SetActive(false);
        JPanel.gameObject.SetActive(false);
        pausePanel.gameObject.SetActive(false);
        LvlPanel.gameObject.SetActive(false);
        optionsPanel.gameObject.SetActive(false);
        RLPanel.gameObject.SetActive(false);
        BPPanel.gameObject.SetActive(false);
        GPanel.gameObject.SetActive(false);
        UsePanel.gameObject.SetActive(false);
        LAPanel.gameObject.SetActive(false);
        CAPanel.gameObject.SetActive(false);
    }

    public void CloseWindows()
    {
        if (uiState == UIWindow.Options)
            optionsPanel.ApplyChanges();

        ClosePanels();

        column = 1;
        selectedItemNum = 0;
        boxInv = null;
        paused = false;
        showConsole = false;
        calledShotTarget = null;
        uiState = UIWindow.None;

        if (!pickedTrait)
            LevelUp();
    }

    void AssignPlayerValues()
    {
        if (playerEntity == null)
            playerEntity = ObjectManager.playerEntity;
        if (playerInventory == null)
            playerInventory = ObjectManager.player.GetComponent<Inventory>();
        if (playerStats == null)
            playerStats = ObjectManager.player.GetComponent<Stats>();
        if (playerInput == null)
            playerInput = ObjectManager.player.GetComponent<PlayerInput>();
        if (playerAbilities == null)
            playerAbilities = ObjectManager.player.GetComponent<EntitySkills>();
        if (cursorControl == null)
            cursorControl = playerInput.GetComponentInChildren<CursorControl>();

        playerLooking = (playerInput.cursorMode == PlayerInput.CursorMode.Tile);

        if (tileMap == null)
            tileMap = World.tileMap;
    }

    public void ChangeMapNameInSideBar()
    {
        currentMapText.text = World.tileMap.TileName();
    }

    void FillLevelTraits()
    {
        levelUpTraits = new List<Trait>();
        List<Trait> availableTraits = TraitList.traits.FindAll(x => x.ContainsEffect(TraitEffects.Random_Trait) && !playerStats.traits.Contains(x));
        int tr = availableTraits.Count;

        if (tr == 0)
            return;

        tr = (availableTraits.Count < 3) ? availableTraits.Count : 3;

        for (int i = 0; i < tr; i++)
        {
            Trait t = availableTraits.GetRandom(SeedManager.combatRandom);
            availableTraits.Remove(t);
            levelUpTraits.Add(t);
        }
    }

    public void LevelUp()
    {
        pickedTrait = false;

        if (uiState == UIWindow.None)
        {
            FillLevelTraits();
            uiState = UIWindow.LevelUp;
            LvlPanel.gameObject.SetActive(true);
            LvlPanel.Init(levelUpTraits);
        }
    }

    //Select a body part with the "Called Shot" skill
    public void CalledShot(Body target)
    {
        uiState = UIWindow.TargetBodyPart;

        calledShotTarget = target;
        BPPanel.gameObject.SetActive(true);
        BPPanel.TargetPart(target, BodyPartTargetPanel.SelectionType.CalledShot);
    }

    public void Grab(Body target)
    {
        uiState = UIWindow.TargetBodyPart;

        calledShotTarget = target;
        BPPanel.gameObject.SetActive(true);
        BPPanel.TargetPart(target, BodyPartTargetPanel.SelectionType.Grab);
    }

    void ItemScrolling()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetSelectedNumber(0);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetSelectedNumber(1);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetSelectedNumber(2);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            SetSelectedNumber(3);
        if (Input.GetKeyDown(KeyCode.Alpha5))
            SetSelectedNumber(4);
        if (Input.GetKeyDown(KeyCode.Alpha6))
            SetSelectedNumber(5);
        if (Input.GetKeyDown(KeyCode.Alpha7))
            SetSelectedNumber(6);
        if (Input.GetKeyDown(KeyCode.Alpha8))
            SetSelectedNumber(7);
        if (Input.GetKeyDown(KeyCode.Alpha9))
            SetSelectedNumber(8);
        if (Input.GetKeyDown(KeyCode.Alpha0))
            SetSelectedNumber(9);
    }

    public void PlayerDied(string killer)
    {
        if (killer == Manager.playerName)
            killer = "Yourself";

        dead = true;
        Alert.NewAlert("Dead", killer, null);
    }

    public static string ColorByPercent(string text, int percent)
    {
        string returnText = "<color=white>" + text + "</color>";
        if (percent < 100)
            returnText = "<color=green>" + text + "</color>";
        if (percent < 75)
            returnText = "<color=yellow>" + text + "</color>";
        if (percent < 50)
            returnText = "<color=orange>" + text + "</color>";
        if (percent < 25)
            returnText = "<color=red>" + text + "</color>";

        return returnText;
    }
}

public enum UIWindow
{
    None,
    PauseMenu,
    Inventory,
    Character,
    Shop,
    TargetBodyPart,
    AmputateLimb,
    Abilities,
    Journal,
    Dialogue,
    Options,
    Alert,
    Loot,
    Keybindings,
    YesNoPrompt,
    ReplacePartWithItem,
    UseItemOnItem,
    SelectItemToThrow,
    LevelUp,
    Grapple,
    LiquidActions,
    ContextActions
}
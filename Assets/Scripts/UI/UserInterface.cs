using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class UserInterface : MonoBehaviour
{
    public static int selectedItemNum = 0;
    public static bool paused = false;
    public static bool loading = false;
    public static bool showConsole = false;
    [HideInInspector] public int pendingTraitsToPick;
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
    public GiveItemPanel GIPanel;
    public CyberneticsPanel CybPanel;

    public GameObject loadingGO;
    public Image fadePanel;
    int indexToUse;
    readonly int limbToReplaceIndex = -1;
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

    [Space(5)]
    public GameObject miniMapObject;
    public GameObject fullMapObject;

    public bool SelectItemActions
    {
        get { return IAPanel.gameObject.activeSelf; }
    }
    public bool SelectBodyPart
    {
        get { return SSPanel.gameObject.activeSelf; }
    }
    public UIWindow CurrentState
    {
        get { return uiState; }
    }
    public bool NoWindowsOpen
    {
        get { return uiState == UIWindow.None; }
    }

    void OnEnable()
    {
        World.userInterface = this;
        fadePanel.gameObject.SetActive(true);
    }

    void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 2)
        {
            loading = true;
            paused = false;

            pausePanel.Init();
            ToggleFullMap(false);
            ToggleMessageLog();

            uiState = UIWindow.None;
        }
    }

    public void ShowInitialMessage(string name)
    {
        CloseWindows();
        loadingGO.SetActive(false);
        fadePanel.CrossFadeAlpha(0, 1.5f, false);

        if (Manager.newGame)
        {
            Alert.NewAlert("Start_Message", name, null);
        }
    }

    public void ToggleFullMap(bool fullMap)
    {
        miniMapObject.SetActive(!fullMap);
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
    
    public void ToggleMessageLog()
    {
        MLog.gameObject.SetActive(GameSettings.ShowLog);
    }

    void Update()
    {
        if (dead && (playerInput.keybindings.GetKey("Enter") || playerInput.keybindings.GetKey("Pause")))
        {
            if (World.difficulty.Permadeath)
            {
                Manager.ClearFiles();
                World.Reset();
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
            else
            {
                CloseWindows();
                ObjectManager.playerEntity.stats.health = ObjectManager.playerEntity.stats.MaxHealth;
                ObjectManager.playerEntity.stats.stamina = ObjectManager.playerEntity.stats.MaxStamina;
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

        if (Input.GetKeyDown(KeyCode.F1))
            uiState = UIWindow.Options;
    }

    public void OpenGrapple(Body targetBody)
    {
        uiState = UIWindow.Grapple;
        GPanel.gameObject.SetActive(true);
        GPanel.Initialize(playerEntity.body, targetBody);
    }

    public void NewAlert(string title, string content)
    {
        CloseWindows();
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
        CloseWindows();

        if (uiState != UIWindow.Journal)
        {
            uiState = UIWindow.Journal;
            JPanel.gameObject.SetActive(true);
            JPanel.Initialize();
        }
    }

    public void OpenInventory(Inventory inv = null)
    {
        if (inv == null)
        {
            inv = playerInventory;
        }

        CloseWindows();
        uiState = UIWindow.Inventory;
        InvPanel.gameObject.SetActive(true);
        EqPanel.gameObject.SetActive(true);
        InvPanel.Init(inv);
        EqPanel.Init(inv);
    }

    public void OpenPlayerInventory()
    {
        OpenInventory(playerInventory);
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
        }
        else
        {
            CloseWindows();
            uiState = UIWindow.Abilities;
            AbPanel.gameObject.SetActive(true);
            AbPanel.Initialize();
        }
    }

    public void OpenPauseMenu()
    {
        paused = !paused;
        uiState = UIWindow.PauseMenu;
        pausePanel.TogglePause(paused);
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

    public void OpenCharacterPanel(Entity e)
    {
        CloseWindows();
        uiState = UIWindow.Character;
        CharPanel.gameObject.SetActive(true);
        CharPanel.Initialize(e.stats, e.inventory);
    }

    public void ItemOnItem_Fill(Item i, Inventory inv)
    {
        CloseWindows();
        uiState = UIWindow.UseItemOnItem;
        UsePanel.gameObject.SetActive(true);
        UsePanel.Init(i, inv, (x => x.GetCComponent<CLiquidContainer>() != null && x != i), "Fill");
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

    public void GiveItem(ItemProperty prop)
    {
        CloseWindows();
        uiState = UIWindow.UseItemOnItem;
        UsePanel.gameObject.SetActive(true);
        UsePanel.Init(null,  ObjectManager.playerEntity.inventory, (x => x.HasProp(prop)), "Give Item");
    }
    
    public void GiveItem(string id)
    {
        CloseWindows();
        uiState = UIWindow.UseItemOnItem;
        UsePanel.gameObject.SetActive(true);
        UsePanel.Init(null, ObjectManager.playerEntity.inventory, (x => x.ID == id), "Give Item");
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
                OpenCharacterPanel(ObjectManager.playerEntity);
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
                ThrowPanel.Initialize();
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

    public void OpenCyberneticsPanel(Body body)
    {
        CloseWindows();
        CybPanel.gameObject.SetActive(true);
        uiState = UIWindow.Cybernetics;

        CybPanel.SetupLists(body);
    }

    void HandleInput()
    {
        if (loading || playerInput == null || PlayerInput.lockInput)
            return;

        if (playerInput.keybindings.GetKey("Enter"))
            SelectPressed();
        if (playerInput.keybindings.GetKey("Pause"))
            BackPressed();

        if (boxInv != null && playerInput.keybindings.GetKey("Pickup"))
            SelectPressed();

        if (paused)
        {
            if (playerInput.keybindings.GetKey("East"))
            {
                selectedItemNum++;

                if (selectedItemNum >= pausePanel.buttons.Length)
                    selectedItemNum = 0;

                pausePanel.UpdateSelected(selectedItemNum);
                World.soundManager.MenuTick();
            }
            else if (playerInput.keybindings.GetKey("West"))
            {
                selectedItemNum--;

                if (selectedItemNum < 0)
                    selectedItemNum = pausePanel.buttons.Length - 1;

                pausePanel.UpdateSelected(selectedItemNum);
                World.soundManager.MenuTick();
            }
        }

        if (!paused && uiState != UIWindow.Alert && uiState != UIWindow.LevelUp)
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
                else if (uiState == UIWindow.None)
                {
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

        else if (uiState == UIWindow.ReplacePartWithItem)
        {
            if (playerInput.keybindings.GetKey("East"))
            {
                if (column < 1 && limbToReplaceIndex > -1)
                {
                    column++;
                }

                World.soundManager.MenuTick();
            }
            if (playerInput.keybindings.GetKey("West"))
            {
                if (column > 0)
                {
                    column--;
                }

                World.soundManager.MenuTick();
            }
        }

        else if (uiState == UIWindow.Loot && boxInv != null)
        {
            if (boxInv.items.Count > 0)
            {
                if (selectedItemNum > boxInv.items.Count - 1)
                    selectedItemNum = 0;
            }
            else
                selectedItemNum = 0;
        }

        if (uiState == UIWindow.Shop && shopInv != null)
        {
            if (playerInput.keybindings.GetKey("East"))
            {
                if (column >= 1 || playerInventory.items.Count <= 0)
                    return;

                World.soundManager.MenuTick();
                selectedItemNum = 0;
                column++;
                ShopPanel.UpdateTooltip();
            }

            if (playerInput.keybindings.GetKey("West"))
            {
                if (column <= 0 || shopInv.items.Count <= 0)
                    return;

                World.soundManager.MenuTick();
                selectedItemNum = 0;
                column--;
                ShopPanel.UpdateTooltip();
            }

            if (column == 0 && selectedItemNum > shopInv.items.Count - 1 || column == 1 && selectedItemNum > playerInventory.items.Count - 1)
                selectedItemNum = 0;
        }

        else if (uiState == UIWindow.ReplacePartWithItem)
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
        if (!NoWindowsOpen)
            World.soundManager.MenuTick();

        if (uiState == UIWindow.ReplacePartWithItem)
            RLPanel.SwitchSelectedNum(amount);

        if (SelectBodyPart)
            SSPanel.SwitchSelectedNum(amount);
        else if (SelectItemActions)
            IAPanel.SwitchSelectedNum(amount);
        else
        {
            selectedItemNum += amount;

            if (selectedItemNum > SelectedMax())
                selectedItemNum = 0;
            else if (selectedItemNum < 0)
                selectedItemNum = SelectedMax();

            if (uiState == UIWindow.Shop)
                ShopPanel.UpdateTooltip();
            else if (uiState == UIWindow.Loot)
                LPanel.UpdateTooltip(true);
            else if (uiState == UIWindow.SelectItemToThrow)
                ThrowPanel.UpdateTooltip();
            else if (uiState == UIWindow.PauseMenu)
                pausePanel.UpdateSelected(selectedItemNum);
        }
    }

    public void OpenSaveAndQuitDialogue()
    {
        CloseWindows();
        YesNoAction("YN_SaveQuit".Localize(), () => { fadePanel.CrossFadeAlpha(1.0f, 0.3f, true); SaveAndQuit(); }, () => { CloseWindows(); }, "");
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
            return (uiState == UIWindow.Inventory && playerInventory.items.Count > 0 && !SelectItemActions && !SelectBodyPart);
        else
            return (uiState == UIWindow.Inventory && !SelectItemActions && !SelectBodyPart);
    }

    public void ShowNPCDialogue(DialogueController diaController)
    {
        selectedItemNum = 0;
        uiState = UIWindow.Dialogue;
        DPanel.gameObject.SetActive(true);
        DPanel.Display(diaController);
        shopInv = diaController.GetComponent<Inventory>();

        EventHandler.instance.OnTalkTo(diaController.GetComponent<BaseAI>().npcBase);
    }

    public void Dialogue_Chat(Faction faction)
    {
        DPanel.SetText(Dialogue.Chat(faction));
    }

    public void Dialogue_Inquire(string nodeID)
    {
        selectedItemNum = 0;

        if (nodeID == "End")
        {
            CloseWindows();
            return;
        }

        DialogueNode node = GameData.Get<DialogueNode>(nodeID);
        DPanel.Display(node);

        if (node.onSelect != null)
        {
            LuaManager.CallScriptFunction(node.onSelect);
        }
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
        {
            return;
        }

        if (playerInventory.gold < playerStats.CostToCureWounds())
        {
            DPanel.SetText(LocalizationManager.GetContent("Cannot_Afford"));
            return;
        }

        playerInventory.gold -= playerStats.CostToCureWounds();
        DPanel.SetText(LocalizationManager.GetContent("Healed_Wounds"));
        playerStats.CureAllWounds();
    }

    public void Dialogue_ReplaceLimb(bool fromDoctor = true)
    {
        if (fromDoctor)
        {
            if (playerInventory.gold < playerStats.CostToReplaceLimbs())
            {
                DPanel.SetText(LocalizationManager.GetContent("Cannot_Afford"));
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

    public void YesNoAction(string question, Action yAction, Action nAction, string input = "")
    {
        if (nAction == null)
        {
            nAction = () => CloseWindows();
        }

        uiState = UIWindow.YesNoPrompt;
        YNPanel.gameObject.SetActive(true);
        YNPanel.Display(question, yAction, nAction, input);
    }

    public void YNAction(string question, string modID, string fileName, string functionName)
    {
        LuaCall luaScript = new LuaCall(string.Format("{0}.{1}.{2}", modID, fileName, functionName));
        YesNoAction(question, () => LuaManager.CallScriptFunction(luaScript), () => CloseWindows());
    }

    public void BanditYes(int goldAmount, Item item)
    {
        World.userInterface.CloseWindows();

        if (item != null && playerInventory.gold > 0)
        {
            Alert.NewAlert("Bandit_Yes");

            for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++)
            {
                if (World.objectManager.onScreenNPCObjects[i].AI.npcBase.faction.ID == "bandits")
                {
                    BaseAI bai = World.objectManager.onScreenNPCObjects[i].GetComponent<BaseAI>();
                    bai.OverrideHostility(false);
                }
            }
        }
        else
        {
            Alert.NewAlert("Bandit_Yes_NoMoney");

            for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++)
            {
                if (World.objectManager.onScreenNPCObjects[i].AI.npcBase.faction.ID == "bandits")
                {
                    BaseAI bai = World.objectManager.onScreenNPCObjects[i].GetComponent<BaseAI>();
                    bai.NoticePlayer();
                }
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

    public void SelectPressed(int selectedNumOverride = -1)
    {
        if (selectedNumOverride > -1)
        {
            selectedItemNum = selectedNumOverride;
        }

        //Level Up
        if (uiState == UIWindow.LevelUp)
        {
            pendingTraitsToPick--;
            if (selectedItemNum < 3)
            {
                playerStats.InitializeNewTrait(levelUpTraits[selectedItemNum]);
            }

            uiState = UIWindow.None;
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
                    YesNoAction("YN_Amputate".Localize(), () => {
                        CloseWindows();
                        playerInventory.entity.body.RemoveLimb(parts[indexToUse]);
                    }, null, parts[indexToUse].displayName);
                }
            }
        }
        else if (uiState == UIWindow.Alert)
        {
            Alert.CloseAlert();
        }
        else if (paused && uiState == UIWindow.PauseMenu)
        {
            pausePanel.buttons[selectedItemNum].onClick.Invoke();
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
        else if (uiState == UIWindow.Dialogue)
        {
            DPanel.ChooseDialogue();
        }
        else if (uiState == UIWindow.ReplacePartWithItem)
        {
            RLPanel.SelectPressed();
        }
        else if (NoWindowsOpen)
        {
            if (World.tileMap.GetTileID(ObjectManager.playerEntity.posX, ObjectManager.playerEntity.posY) == TileManager.tiles["Stairs_Up"].ID)
                playerInput.GoUp();
            else if (World.tileMap.GetTileID(ObjectManager.playerEntity.posX, ObjectManager.playerEntity.posY) == TileManager.tiles["Stairs_Down"].ID)
                playerInput.GoDown();
        }
    }

    void Select_Shop()
    {
        int charisma = playerStats.Attributes["Charisma"];

        if (column == 0)
        { //Shop inventory
            bool friendly = shopInv.entity.AI.isFollower();

            if (selectedItemNum >= shopInv.items.Count || shopInv.items[selectedItemNum] == null)
                return;

            int cost = shopInv.items[selectedItemNum].buyCost(charisma);

            if ((playerInventory.CanAfford(cost) || friendly))
            {
                Item newItem = new Item(shopInv.items[selectedItemNum]);

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                    cost *= newItem.amount;
                else
                    newItem.amount = 1;

                if (friendly)
                    cost = 0;

                if (playerInventory.CanPickupItem(newItem) && playerInventory.CanAfford(cost))
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

            bool isFollower = shopInv.entity.AI.isFollower();
            int cost = playerInventory.items[selectedItemNum].sellCost(charisma);
            Item newItem = new Item(playerInventory.items[selectedItemNum]);

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                cost *= newItem.amount;
            else
                newItem.amount = 1;

            if (World.soundManager != null)
            {
                World.soundManager.UseItem();
            }

            if (isFollower && !shopInv.CanPickupItem(newItem))
            {
                Alert.NewAlert("Inv_Full_Follower");
                return;
            }

            shopInv.PickupItem(newItem);

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                playerInventory.RemoveInstance_All(playerInventory.items[selectedItemNum]);
            else
                playerInventory.RemoveInstance(playerInventory.items[selectedItemNum]);

            if (!isFollower)
            {
                playerInventory.gold += cost;
            }
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

        if (!SelectItemActions && !SelectBodyPart)
        {
            selectedItemNum = 0;
        }

        if (Options_KeyPanel.WaitingForRebindingInput)
        {
            return;
        }

        if (uiState == UIWindow.Alert)
        {
            Alert.CloseAlert();
            return;
        }
        if (paused)
        {
            paused = false;
        }
        else
        {
            if (PlayerInput.fullMap)
            {
                return;
            }
            if (SelectItemActions)
            {
                IAPanel.gameObject.SetActive(false);
                return;
            }
            if (SelectBodyPart)
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
        }

        CloseWindows();
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
    }

    public void SetSelectedNumber(int num, bool scroll)
    {
        if (uiState != UIWindow.None)
        {
            if (SelectItemActions)
            {
                InvPanel.UpdateTooltip(num);
                return;
            }

            selectedItemNum = num;

            if (uiState == UIWindow.Shop)
                ShopPanel.UpdateTooltip();
            else if (uiState == UIWindow.SelectItemToThrow)
                ThrowPanel.UpdateTooltip();
            else if (uiState == UIWindow.Loot)
                LPanel.UpdateTooltip(false);
            else if (uiState == UIWindow.PauseMenu)
                pausePanel.UpdateSelected(selectedItemNum);
            else if (uiState == UIWindow.Abilities)
                AbPanel.ChangeSelectedNum(num, scroll);
            else if (uiState == UIWindow.Journal)
                JPanel.ChangeSelectedNum(num, scroll);
        }
    }

    int SelectedMax()
    {
        if (SelectItemActions || SelectBodyPart)
            return selectedItemNum;

        switch (uiState)
        {
            case UIWindow.PauseMenu:
                return pausePanel.SelectedMax;
            case UIWindow.Shop:
                return (column == 1) ? playerInventory.items.Count - 1 : shopInv.items.Count - 1;
            case UIWindow.TargetBodyPart:
                return calledShotTarget.bodyParts.Count - 1;
            case UIWindow.AmputateLimb:
                return playerInventory.entity.body.bodyParts.Count - 1;
            case UIWindow.Dialogue:
                return DPanel.cMax;
            case UIWindow.Loot:
                return LPanel.max;
            case UIWindow.UseItemOnItem:
                return UsePanel.numItems - 1;
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
        LvlPanel.gameObject.SetActive(false);
        optionsPanel.gameObject.SetActive(false);
        RLPanel.gameObject.SetActive(false);
        BPPanel.gameObject.SetActive(false);
        GPanel.gameObject.SetActive(false);
        UsePanel.gameObject.SetActive(false);
        LAPanel.gameObject.SetActive(false);
        CAPanel.gameObject.SetActive(false);
        GIPanel.gameObject.SetActive(false);
        CybPanel.gameObject.SetActive(false);
        pausePanel.TogglePause(false);
    }

    public void CloseWindows()
    {
        if (uiState == UIWindow.LevelUp && pendingTraitsToPick > 0)
            return;

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

        if (pendingTraitsToPick > 0)
            PickLevelUpTrait();
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

        if (miniMapObject.activeSelf)
        {
            bool hideMinimap = World.tileMap.currentElevation != 0 || !playerInput.showMinimap;
            miniMapObject.GetComponent<Animator>().SetBool("Hide", hideMinimap);
        }
    }

    void FillLevelTraits()
    {
        levelUpTraits = new List<Trait>();
        List<Trait> availableTraits = GameData.GetAll<Trait>().FindAll(x => x.ContainsEffect(TraitEffects.Random_Trait) && !playerStats.hasTrait(x.ID));

        for (int i = 0; i < Mathf.Min(availableTraits.Count, 3); i++)
        {
            int index = SeedManager.combatRandom.Next(0, availableTraits.Count);
            Trait t = new Trait(availableTraits[index]);
            availableTraits.Remove(availableTraits[index]);
            levelUpTraits.Add(t);
        }
    }

    public void PickLevelUpTrait()
    {
        if (uiState == UIWindow.None)
        {
            FillLevelTraits();
            uiState = UIWindow.LevelUp;
            LvlPanel.gameObject.SetActive(true);
            LvlPanel.Init(levelUpTraits);
        }
    }

    //Select a body part with the "Called Shot" skill (using ctrl + direction)
    public void CalledShot(Body target)
    {
        uiState = UIWindow.TargetBodyPart;

        calledShotTarget = target;
        BPPanel.gameObject.SetActive(true);
        BPPanel.TargetPart(target, BodyPartTargetPanel.SelectionType.CalledShot);
    }

    //Selected a body part with the "Grapple" skill
    public void Grab(Body target)
    {
        uiState = UIWindow.TargetBodyPart;

        calledShotTarget = target;
        BPPanel.gameObject.SetActive(true);
        BPPanel.TargetPart(target, BodyPartTargetPanel.SelectionType.Grab);
    }

    public void PlayerDied(string killer)
    {
        if (killer == Manager.playerName)
        {
            killer = "Yourself";
        }

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
    ContextActions,
    Cybernetics
}
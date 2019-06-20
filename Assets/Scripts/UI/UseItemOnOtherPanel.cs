using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

public class UseItemOnOtherPanel : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject inventoryButton;
    [Header("Children")]
    public Transform inventoryBase;
    public TooltipPanel ToolTipPanel;
    public Scrollbar scrollBar;
    public Text title;

    Inventory inventory;
    Item itemToUse;
    List<Item> relevantItems;
    string actionName;

    public int numItems
    {
        get
        {
            return (relevantItems == null) ? 0 : relevantItems.Count;
        }
    }

    void OnDisable()
    {
        if (relevantItems != null)
        {
            relevantItems.Clear();
        }

        itemToUse = null;
        inventory = null;
        actionName = "";
    }

    public void Init(Item i, Inventory inv, Predicate<Item> p, string action)
    {
        itemToUse = i;
        inventory = inv;
        actionName = action;

        UpdateInventory(p);
    }

    public void Init(Item i, Inventory inv, List<Item> relItems, string action)
    {
        itemToUse = i;
        inventory = inv;
        relevantItems = relItems;
        actionName = action;

        UpdateInventory(null);
    }

    public void UpdateInventory(Predicate<Item> p)
    {
        title.text = (actionName == "Give Item") ? LocalizationManager.GetContent("Title_Give") : LocalizationManager.GetContent("Title_Use");

        inventoryBase.DespawnChildren();

        if (!gameObject.activeSelf)
        {
            return;
        }

        relevantItems = (p != null) ? inventory.items.FindAll(p) : relevantItems;

        for (int i = 0; i < relevantItems.Count; i++)
        {
            GameObject g = SimplePool.Spawn(inventoryButton, inventoryBase);
            g.GetComponent<ItemButton>().icon.sprite = InventoryPanel.SwitchSprite(relevantItems[i]);
            g.GetComponentInChildren<Text>().text = relevantItems[i].InvDisplay("");
            g.GetComponent<Button>().onClick.AddListener(() => { SelectPressed(g.transform.GetSiblingIndex()); });
        }

        UpdateTooltip();
    }

    void SelectPressed(int index)
    {
        switch (actionName)
        {
            case "Fill":
                Fill(index);
                World.userInterface.CloseWindows();
                break;
            case "Pour_Item":
                Coat(index);
                World.userInterface.CloseWindows();
                break;
            case "Mod_Item":
                ModItem(index);
                World.userInterface.CloseWindows();
                break;
            case "Give Item":
                GiveItem(index);
                return;
        }
    }

    void GiveItem(int index)
    {
        List<Quest> quests = ObjectManager.playerJournal.quests;
        Item item = relevantItems[index];

        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i].ActiveGoal.goalType == "FetchPropertyGoal")
            {
                FetchPropertyGoal fpg = (FetchPropertyGoal)quests[i].ActiveGoal;

                if (item.HasProp(fpg.itemProperty))
                {
                    inventory.RemoveInstance(relevantItems[index]);
                    relevantItems.Remove(item);
                    UpdateInventory(null);
                    fpg.AddAmount(1);

                    if (fpg.isComplete)
                    {
                        gameObject.SetActive(false);
                        return;
                    }

                    break;
                }
            }
            else if (quests[i].ActiveGoal.goalType == "FetchGoal")
            {
                FetchGoal fg = (FetchGoal)quests[i].ActiveGoal;

                if (item.ID == fg.itemID)
                {
                    inventory.RemoveInstance(relevantItems[index]);
                    UpdateInventory(null);
                    fg.AddAmount(1);

                    if (fg.isComplete)
                    {
                        gameObject.SetActive(false);
                        return;
                    }

                    break;
                }
            }
            else if (quests[i].ActiveGoal.goalType == "Fetch_Homonculus")
            {
                Fetch_Homonculus fg = (Fetch_Homonculus)quests[i].ActiveGoal;

                if (item.HasProp(fg.itemProperty))
                {
                    inventory.RemoveInstance(item);
                    relevantItems.Remove(item);
                    fg.AddItem(item);
                    UpdateInventory(null);

                    if (fg.isComplete)
                    {
                        gameObject.SetActive(false);
                        return;
                    }

                    break;
                }
            }
        }

        if (relevantItems.Count <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    void ModItem(int index)
    {
        CModKit cmod = itemToUse.GetCComponent<CModKit>();

        relevantItems[index].AddModifier(ItemList.GetModByID(cmod.modID));
        inventory.RemoveInstance(itemToUse);
    }

    void Coat(int index)
    {
        CLiquidContainer container = itemToUse.GetCComponent<CLiquidContainer>();

        if (container == null)
        {
            return;
        }

        Item target = relevantItems[index];
        Liquid lq = ItemList.GetLiquidByID(container.sLiquid.ID, container.sLiquid.units);

        if (target.HasCComponent<CCoat>())
        {
            CCoat cc = target.GetCComponent<CCoat>();
            cc.liquid = new Liquid(lq, 1);
            cc.strikes = 10;
        }
        else
        {
            CCoat cc = new CCoat(5, new Liquid(lq, 1));
            target.AddComponent(cc);
        }

        lq.Coat(target);
        container.SetLiquidVolume(container.sLiquid.units - 1);
        container.CheckLiquid();
    }

    void Fill(int index)
    {
        CLiquidContainer frm = itemToUse.GetCComponent<CLiquidContainer>();
        CLiquidContainer to = relevantItems[index].GetCComponent<CLiquidContainer>();
        Liquid liquid = ItemList.GetLiquidByID(frm.sLiquid.ID, frm.sLiquid.units);
        int amount = to.Fill(liquid);

        if (amount > 0)
        {
            CombatLog.NewMessage("You pour " + amount.ToString() + " units of " + liquid.Name + " into the " + relevantItems[index].DisplayName() + ".");
            frm.CheckLiquid();
        }
        else
        {
            Alert.NewAlert("Pour_No_Room");
        }

        World.objectManager.CheckMapObjectInventories();
    }

    public void UpdateTooltip()
    {
        ToolTipPanel.gameObject.SetActive(numItems > 0);

        if (numItems > 0)
        {
            bool display = (inventory.items.Count > 0 && UserInterface.selectedItemNum < numItems);
            ToolTipPanel.UpdateTooltip(inventory.items[UserInterface.selectedItemNum], display);
        }
    }

    void Update()
    {
        if (World.userInterface.column == 1 && inventoryBase.childCount > 0 && inventoryBase.childCount > UserInterface.selectedItemNum)
            EventSystem.current.SetSelectedGameObject(inventoryBase.GetChild(UserInterface.selectedItemNum).gameObject);

        if (numItems > 0)
        {
            scrollBar.value = 1f - (UserInterface.selectedItemNum / (float)numItems);

            for (int i = 0; i < inventoryBase.childCount; i++)
            {
                inventoryBase.GetChild(i).GetComponent<ItemButton>().selected = (i == UserInterface.selectedItemNum);
            }

            if (GameSettings.Keybindings.GetKey("Enter"))
            {
                SelectPressed(UserInterface.selectedItemNum);
            }
        }
    }
}

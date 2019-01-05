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
            relevantItems.Clear();

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
                break;
            case "Pour_Item":
                Coat(index);
                break;
            case "Mod_Item":
                ModItem(index);
                break;
        }

        World.userInterface.CloseWindows();
    }

    void ModItem(int index)
    {
        CModKit cmod = itemToUse.GetCComponent<CModKit>();

        relevantItems[index].AddModifier(ItemList.GetModByID(cmod.modID));
        inventory.RemoveInstance(itemToUse);
    }

    void Coat(int index)
    {
        CLiquidContainer liq = itemToUse.GetCComponent<CLiquidContainer>();

        if (liq == null)
        {
            return;
        }

        Item target = relevantItems[index];

        if (target.HasCComponent<CCoat>())
        {
            CCoat cc = target.GetCComponent<CCoat>();
            cc.liquid = new Liquid(liq.liquid, 1);
            cc.strikes = 10;
        }
        else
        {
            CCoat cc = new CCoat(10, new Liquid(liq.liquid, 1));
            target.AddComponent(cc);
        }

        liq.liquid.Coat(target);
        liq.liquid.units--;
        liq.CheckLiquid();
    }

    void Fill(int index)
    {
        CLiquidContainer frm = itemToUse.GetCComponent<CLiquidContainer>();
        CLiquidContainer to = relevantItems[index].GetCComponent<CLiquidContainer>();
        int amount = to.Fill(frm.liquid);

        if (amount > 0)
        {
            CombatLog.NewMessage("You pour " + amount.ToString() + " units of " + frm.liquid.Name + " into the " + relevantItems[index].DisplayName() + ".");

            if (frm.liquid.units <= 0)
                itemToUse.GetCComponent<CLiquidContainer>().liquid = null;
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
        bool display = (inventory.items.Count > 0 && UserInterface.selectedItemNum < numItems);
        ToolTipPanel.UpdateTooltip(inventory.items[UserInterface.selectedItemNum], display);
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

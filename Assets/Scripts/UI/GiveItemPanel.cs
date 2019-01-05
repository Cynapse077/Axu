using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

public class GiveItemPanel : UIPanel
{
    [Header("Prefabs")]
    public GameObject inventoryButton;
    [Header("Children")]
    public Transform inventoryBase;
    public TooltipPanel ToolTipPanel;
    public Scrollbar scrollBar;

    Inventory playerInventory;
    Inventory targetInventory;
    List<Item> giveItems;

    //Called via GetSortedInventory, else giveItems will not be initialized.
    public override void Initialize()
    {
        playerInventory = ObjectManager.playerEntity.inventory;
        base.Initialize();
    }

    public void GetSortedInventory(Predicate<Item> i, Inventory targInv)
    {
        targetInventory = targInv;
        Initialize();
        giveItems = playerInventory.items.FindAll(i);
        UpdateInventory();
    }

    public override void ChangeSelectedNum(int newIndex)
    {
        if (World.userInterface.column == 1 && SelectedMax > 0 && SelectedMax > SelectedNum)
        {
            EventSystem.current.SetSelectedGameObject(inventoryBase.GetChild(SelectedNum).gameObject);
            scrollBar.value = 1f - (SelectedNum / (float)SelectedMax);
        }

        base.ChangeSelectedNum(newIndex);
    }

    protected override void OnSelect(int index)
    {
        base.OnSelect(index);
        targetInventory.PickupItem(giveItems[index]);
        playerInventory.RemoveInstance(giveItems[index]);
        giveItems.RemoveAt(index);
        Initialize();
    }

    void UpdateInventory()
    {
        inventoryBase.DespawnChildren();

        SelectedMax = 0;
        SelectedNum = 0;

        for (int i = 0; i < giveItems.Count; i++)
        {
            GameObject g = SimplePool.Spawn(inventoryButton, inventoryBase);
            g.GetComponent<ItemButton>().icon.sprite = SwitchSprite(giveItems[i]);
            g.GetComponentInChildren<Text>().text = giveItems[i].InvDisplay("none");
            g.GetComponent<Button>().onClick.AddListener(() => OnSelect(g.transform.GetSiblingIndex()));
            SelectedMax++;
        }

        if (SelectedMax > 0)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(inventoryBase.GetChild(0).gameObject);
            scrollBar.value = 1f - (SelectedNum / (float)SelectedMax);
        }

        UpdateTooltip();
    }

    Sprite SwitchSprite(Item item)
    {
        string id = (string.IsNullOrEmpty(item.renderer.onGround)) ? "item-empty.png" : item.renderer.onGround;

        return SpriteManager.GetObjectSprite(id);
    }

    public void UpdateTooltip()
    {
        if (giveItems.Count == 0)
        {
            ToolTipPanel.gameObject.SetActive(false);
            return;
        }

        ToolTipPanel.gameObject.SetActive(giveItems.Count > 0);
        bool display = (giveItems.Count > 0 && SelectedNum < SelectedMax);
        ToolTipPanel.UpdateTooltip(giveItems[SelectedNum], display);
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryPanel : UIPanel
{
    [Header("Prefabs")]
    public GameObject inventoryButton;
    [Header("Children")]
    public Transform inventoryBase;
    public Text Currency;
    public Text Capacity;
    public TooltipPanel ToolTipPanel;
    public Scrollbar scrollBar;

    public Inventory curInv;

    public void Init(Inventory inv)
    {
        curInv = inv;
        initialized = true;
        UpdateInventory();
    }

    void UpdateInventory()
    {
        inventoryBase.DespawnChildren();

        Currency.text = "<color=yellow>$</color> " + curInv.gold;
        Capacity.text = string.Format("<color=olive>({0} / {1})</color>", curInv.items.Count.ToString(), curInv.maxItems.ToString());
        SelectedNum = 0;
        SelectedMax = 0;

        foreach (Item it in curInv.items)
        {
            GameObject g = SimplePool.Spawn(inventoryButton, inventoryBase);
            g.GetComponent<ItemButton>().icon.sprite = SwitchSprite(it);
            g.GetComponentInChildren<Text>().text = it.InvDisplay("none");
            g.GetComponent<Button>().onClick.AddListener(() => { OnSelect(g.transform.GetSiblingIndex()); });
            g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(1, UIWindow.Inventory, false, false);
            SelectedMax++;
        }

        if (SelectedMax > 0)
        {
            EventSystem.current.Highlight(inventoryBase.GetChild(SelectedNum).gameObject);
        }

        UpdateTooltip(SelectedNum);
    }

    public static Sprite SwitchSprite(Item item)
    {
        string id = (string.IsNullOrEmpty(item.renderer.onGround)) ? "item-empty.png" : item.renderer.onGround;

        return SpriteManager.GetObjectSprite(id);
    }

    public void UpdateTooltip(int index)
    {
        ToolTipPanel.gameObject.SetActive(true);
        Item i = null;

        if (World.userInterface.column == 1)
        {
            if (inventoryBase.childCount > 0 && curInv.items.Count > 0 && SelectedNum < curInv.items.Count)
            {
                i = curInv.items[SelectedNum];
            }
        }
        else
        {
            i = GetEquipmentSlot(index);
        }

        if (i != null)
        {
            ToolTipPanel.UpdateTooltip(i, World.userInterface.ShowItemTooltip());
        }
        else
        {
            ToolTipPanel.gameObject.SetActive(false);
        }
    }

    Item GetEquipmentSlot(int index)
    {
        if (index < 0)
        {
            return null;
        }

        int handCount = curInv.entity.body.Hands.Count;

        if (index < handCount)
        {
            return curInv.entity.body.Hands[index].EquippedItem;
        }
        else if (index == handCount)
        {
            return curInv.firearm;
        }
        else
        {
            return curInv.entity.body.bodyParts[index - handCount - 1].equippedItem;
        }
    }

    public override void Update()
    {
        if (GameSettings.Keybindings.GetKey("Pause"))
        {
            if (World.userInterface.SelectItemActions)
            {
                World.userInterface.IAPanel.gameObject.SetActive(false);
                return;
            }
            else if (World.userInterface.SelectBodyPart)
            {
                World.userInterface.SSPanel.gameObject.SetActive(false);
                return;
            }
        }

        if (!initialized || World.userInterface.CurrentState() != UIWindow.Inventory || World.userInterface.SelectItemActions || World.userInterface.SelectBodyPart)
        {
            return;
        }

        base.Update();

        if (World.userInterface.column == 1)
        {
            if (GameSettings.Keybindings.GetKey("GoUpStairs") && SelectedNum < SelectedMax - 1)
            {
                int newIndex = SelectedNum + 1;
                curInv.items.Move(SelectedNum, SelectedNum + 1);
                UpdateInventory();
                SelectedNum = newIndex;
                EventSystem.current.Highlight(inventoryBase.GetChild(SelectedNum).gameObject);
            }
            else if (GameSettings.Keybindings.GetKey("GoDownStairs") && SelectedNum > 0)
            {
                int newIndex = SelectedNum - 1;
                curInv.items.Move(SelectedNum, SelectedNum - 1);
                UpdateInventory();
                SelectedNum = newIndex;
                EventSystem.current.Highlight(inventoryBase.GetChild(SelectedNum).gameObject);
            }
            else if (GameSettings.Keybindings.GetKey("West"))
            {
                SelectedNum = 0;
                World.userInterface.column--;
                EventSystem.current.SetSelectedGameObject(null);
                UpdateTooltip(World.userInterface.EqPanel.SelectedNum);
                World.soundManager.MenuTick();
            }
        }
        else if (GameSettings.Keybindings.GetKey("East"))
        {
            if (World.userInterface.column < 1 && SelectedMax > 0)
            {
                SelectedNum = 0;
                World.userInterface.column++;
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(inventoryBase.GetChild(0).gameObject);

                UpdateTooltip(SelectedNum);
                World.soundManager.MenuTick();
            }
        }

        for (int i = 0; i < inventoryBase.childCount; i++)
        {
            inventoryBase.GetChild(i).GetComponent<ItemButton>().selected = (!World.userInterface.SelectItemActions &&
                !World.userInterface.SelectBodyPart && World.userInterface.column == 1 && i == SelectedNum);
        }

        Capacity.text = string.Format("<color=olive>({0} / {1})</color>", curInv.items.Count.ToString(), curInv.maxItems.ToString());
    }

    public override void ChangeSelectedNum(int newIndex)
    {
        if (!World.userInterface.SelectItemActions && !World.userInterface.SelectBodyPart && World.userInterface.column == 1)
        {
            base.ChangeSelectedNum(newIndex);

            if (SelectedMax > 0 && SelectedMax > SelectedNum)
            {
                scrollBar.value = 1f - (SelectedNum / (float)curInv.items.Count);
                EventSystem.current.SetSelectedGameObject(inventoryBase.GetChild(SelectedNum).gameObject);
            }

            UpdateTooltip(SelectedNum);
        }
    }

    protected override void OnSelect(int index)
    {
        if (World.userInterface.column != 1 || curInv.items.Count <= 0 || World.userInterface.SelectBodyPart || World.userInterface.SelectItemActions)
        {
            return;
        }

        World.userInterface.IAPanel.gameObject.SetActive(true);
        World.userInterface.IAPanel.Display(curInv.items[index], curInv);
        base.OnSelect(index);
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LootPanel : MonoBehaviour
{
    public Transform inventoryBase;
    public GameObject inventoryButton;
    public TooltipPanel toolTipPanel;
    public Scrollbar scrollbar;
    public Text capacityText;

    Inventory inv, playerInventory;
    List<Item> relevantItems;
    bool inFocus = true;

    public int max
    {
        get { return (relevantItems == null) ? 0 : relevantItems.Count - 1; }
    }

    public void Init(Inventory lootInv)
    {
        inv = lootInv;
        UpdateInventory();
        UpdateTooltip(true);
    }

    public void UpdateInventory()
    {
        inFocus = true;
        inventoryBase.DespawnChildren();

        if (!gameObject.activeSelf)
        {
            return;
        }

        if (inv == null)
        {
            gameObject.SetActive(false);
            return;
        }
        else
        {
            relevantItems = inv.items.FindAll(x => !x.HasProp(ItemProperty.Pool) || x.HasProp(ItemProperty.Pool) && x.GetCComponent<CLiquidContainer>() != null
                && !x.GetCComponent<CLiquidContainer>().IsFull() && x.GetCComponent<CLiquidContainer>().FilledUnits() > 0);

            if (relevantItems.Count <= 0)
            {
                World.userInterface.CloseWindows();
                return;
            }

            for (int i = 0; i < relevantItems.Count; i++)
            {
                GameObject g = SimplePool.Spawn(inventoryButton, inventoryBase);

                g.GetComponent<ItemButton>().icon.sprite = SwitchSprite(relevantItems[i]);
                g.GetComponentInChildren<Text>().text = relevantItems[i].InvDisplay("");

                Button b = g.GetComponent<Button>();
                b.onClick.AddListener(() => { SelectPressed(g.transform.GetSiblingIndex()); });
            }
        }

        if (ObjectManager.player != null)
        {
            playerInventory = ObjectManager.playerEntity.inventory;
            capacityText.text = string.Format("<color=olive>Your Capacity: ({0}/{1})</color>", playerInventory.items.Count, playerInventory.maxItems);
        }

        UpdateTooltip(true);
    }

    Sprite SwitchSprite(Item item)
    {
        string id = (string.IsNullOrEmpty(item.renderer.onGround)) ? "item-empty.png" : item.renderer.onGround;

        return SpriteManager.GetObjectSprite(id);
    }

    public void UpdateTooltip(bool scroll)
    {
        if (inventoryBase.childCount > 0 && inventoryBase.childCount > UserInterface.selectedItemNum)
            inventoryBase.GetChild(UserInterface.selectedItemNum).Highlight();

        if (inventoryBase.childCount <= 0 || relevantItems == null || inv == null || inv.items == null 
            || relevantItems.Count <= UserInterface.selectedItemNum || relevantItems[UserInterface.selectedItemNum] == null)
        {
            toolTipPanel.gameObject.SetActive(false);
            return;
        }

        if (scroll && inventoryBase.childCount > 0)
            scrollbar.value = 1f - (UserInterface.selectedItemNum / (float)inventoryBase.childCount);

        toolTipPanel.gameObject.SetActive(true);
        bool display = (inv.items.Count > 0 && UserInterface.selectedItemNum < inv.items.Count);
        toolTipPanel.UpdateTooltip(inv.items[UserInterface.selectedItemNum], display);
    }

    void SelectPressed(int index)
    {
        Item newItem = new Item(relevantItems[index]);

        if (!newItem.HasProp(ItemProperty.Ammunition) && !Input.GetKey(KeyCode.LeftControl))
        {
            newItem.amount = 1;
        }

        if (playerInventory.CanPickupItem(newItem))
        {
            playerInventory.PickupItem(newItem);

            if (World.soundManager != null)
            {
                World.soundManager.UseItem();
            }

            if (newItem.HasProp(ItemProperty.Ammunition) || Input.GetKey(KeyCode.LeftControl))
                inv.RemoveInstance_All(relevantItems[index]);
            else
                inv.RemoveInstance(relevantItems[index]);

            if (inv.gameObject)
            {
                inv.gameObject.BroadcastMessage("CheckInventory", SendMessageOptions.DontRequireReceiver);
                Init(inv);
                UpdateTooltip(true);
            }
        }
        else if (newItem.HasProp(ItemProperty.Pool) && newItem.HasCComponent<CLiquidContainer>())
        {
            CLiquidContainer old = relevantItems[index].GetCComponent<CLiquidContainer>();

            if (old.IsEmpty())
            {
                return;
            }

            inFocus = false;
            World.userInterface.YesNoAction("Fill a container from the pool?",
                () => {
                    World.userInterface.ItemOnItem_Fill(newItem, playerInventory);
                    inv.RemoveInstance(relevantItems[index]);
                },
                () => { World.userInterface.CloseWindows(); }, "");
        }
    }

    void Update()
    {
        if (!inFocus || relevantItems == null || relevantItems.Count <= 0)
        {
            return;
        }

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

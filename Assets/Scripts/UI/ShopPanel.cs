using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopPanel : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject itemButton;
    public TooltipPanel ToolTip;

    [Header("Children")]
    public Transform merchantBase;
    public Text merchCap;
    public Transform inventoryBase;
    public Text invMoney;
    public Text invCap;
    public Scrollbar merchScroll;
    public Scrollbar invScroll;

    Inventory playerInventory;
    Inventory merchantInventory;

    public void Init(Inventory mInv)
    {
        if (playerInventory == null)
            playerInventory = ObjectManager.playerEntity.inventory;

        merchantInventory = mInv;
        UpdateInventories();
    }

    public void UpdateInventories()
    {
        DisableSpawned();

        int charisma = ObjectManager.player.GetComponent<Stats>().Attributes["Charisma"];
        merchCap.text = string.Format("<color=olive>({0} / {1})</color>", merchantInventory.items.Count.ToString(), merchantInventory.maxItems.ToString());

        invMoney.text = string.Format("<color=yellow>$</color>{0}", playerInventory.gold.ToString());
        invCap.text = string.Format("<color=olive>({0} / {1})</color>", playerInventory.items.Count.ToString(), playerInventory.maxItems.ToString());

        if (!gameObject.activeSelf)
            return;

        foreach (Item it in merchantInventory.items)
        {
            GameObject g = SimplePool.Spawn(itemButton, merchantBase);
            g.GetComponent<ItemButton>().icon.sprite = InventoryPanel.SwitchSprite(it);
            g.GetComponentInChildren<Text>().text = it.InvDisplay("") + " - <color=yellow>$</color>" + it.buyCost(charisma);
            g.GetComponent<Button>().onClick.AddListener(() => { World.userInterface.SelectPressed(g.transform.GetSiblingIndex()); });
            g.GetComponent<OnHover_SetSelectedIndex>().column = 0;
        }

        foreach (Item it in playerInventory.items)
        {
            GameObject g = SimplePool.Spawn(itemButton, inventoryBase);
            g.GetComponent<ItemButton>().icon.sprite = InventoryPanel.SwitchSprite(it);
            g.GetComponentInChildren<Text>().text = it.InvDisplay("") + " - <color=yellow>$</color>" + it.sellCost(charisma);
            g.GetComponent<Button>().onClick.AddListener(() => { World.userInterface.SelectPressed(g.transform.GetSiblingIndex()); });
            g.GetComponent<OnHover_SetSelectedIndex>().column = 1;
        }

        UpdateTooltip();
    }

    void DisableSpawned()
    {
        merchantBase.DespawnChildren();
        inventoryBase.DespawnChildren();
    }

    public void UpdateTooltip()
    {
        ToolTip.gameObject.SetActive(true);

        Item item = null;

        if (World.userInterface.column == 0 && merchantBase.childCount > 0 && merchantBase.childCount > UserInterface.selectedItemNum)
        {
            item = merchantInventory.items[UserInterface.selectedItemNum];
        }
        else if (inventoryBase.childCount > 0 && inventoryBase.childCount > UserInterface.selectedItemNum)
        {
            item = playerInventory.items[UserInterface.selectedItemNum];
        }

        ToolTip.UpdateTooltip(item, true, true);
    }

    void Update()
    {
        bool merchInv = (World.userInterface.column == 0 && merchantBase.childCount > 0 && merchantBase.childCount > UserInterface.selectedItemNum);
        bool playerInv = (World.userInterface.column == 1 && inventoryBase.childCount > 0 && inventoryBase.childCount > UserInterface.selectedItemNum);

        if (playerInv)
        {
            EventSystem.current.SetSelectedGameObject(inventoryBase.GetChild(UserInterface.selectedItemNum).gameObject);
            invScroll.value = 1f - (UserInterface.selectedItemNum / (float)playerInventory.items.Count);
        }
        else if (merchInv)
        {
            EventSystem.current.SetSelectedGameObject(merchantBase.GetChild(UserInterface.selectedItemNum).gameObject);
            merchScroll.value = 1f - (UserInterface.selectedItemNum / (float)merchantInventory.items.Count);
        }

        AnimateIcons(merchInv, playerInv);

        merchCap.text = string.Format("<color=olive>({0} / {1})</color>", merchantInventory.items.Count.ToString(), merchantInventory.maxItems.ToString());
        invMoney.text = string.Format("<color=yellow>$</color>{0}", playerInventory.gold.ToString());
        invCap.text = string.Format("<color=olive>({0} / {1})</color>", playerInventory.items.Count.ToString(), playerInventory.maxItems.ToString());
    }

    void AnimateIcons(bool merchInv, bool playerInv)
    {
        for (int i = 0; i < merchantBase.childCount; i++)
        {
            merchantBase.GetChild(i).GetComponent<ItemButton>().selected = (merchInv && i == UserInterface.selectedItemNum);
        }

        for (int i = 0; i < inventoryBase.childCount; i++)
        {
            inventoryBase.GetChild(i).GetComponent<ItemButton>().selected = (playerInv && i == UserInterface.selectedItemNum);
        }
    }
}

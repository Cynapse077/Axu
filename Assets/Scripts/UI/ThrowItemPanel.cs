using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ThrowItemPanel : UIPanel {
	[Header("Prefabs")]
	public GameObject inventoryButton;
	[Header("Children")]
	public Transform inventoryBase;
	public TooltipPanel ToolTipPanel;
	public Scrollbar scrollBar;

	Inventory playerInventory;
    List<Item> throwingItems;

    public override void Initialize()
    {
        playerInventory = ObjectManager.playerEntity.inventory;
        UpdateInventory();
        base.Initialize();
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

        playerInventory.entity.fighter.SelectItemToThrow(playerInventory.Items_ThrowingFirst()[SelectedNum]);
        playerInventory.GetComponent<PlayerInput>().ToggleThrow();
        World.userInterface.CloseWindows();
    }

	void UpdateInventory() {
		inventoryBase.DespawnChildren();

        SelectedMax = 0;
        SelectedNum = 0;
		throwingItems = playerInventory.Items_ThrowingFirst();
		
		for (int i = 0; i < throwingItems.Count; i++) {
			GameObject g = SimplePool.Spawn(inventoryButton, inventoryBase);
            g.GetComponent<ItemButton>().icon.sprite = SwitchSprite(throwingItems[i]);
			g.GetComponentInChildren<Text>().text = throwingItems[i].InvDisplay("none");
            g.GetComponent<Button>().onClick.AddListener(() => OnSelect(g.transform.GetSiblingIndex() ));
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

    public void UpdateTooltip() {
		if (playerInventory.items.Count == 0) {
			ToolTipPanel.gameObject.SetActive(false);
			return;
		}

		ToolTipPanel.gameObject.SetActive(playerInventory.items.Count > 0);
		bool display = (playerInventory.items.Count > 0 && SelectedNum < SelectedMax);
		ToolTipPanel.UpdateTooltip(playerInventory.Items_ThrowingFirst()[SelectedNum], display);
	}
}

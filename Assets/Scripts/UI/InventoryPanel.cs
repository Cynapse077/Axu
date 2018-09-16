using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryPanel : MonoBehaviour {
	[Header("Prefabs")]
	public GameObject inventoryButton;
	[Header("Children")]
	public Transform inventoryBase;
	public Text Currency;
	public Text Capacity;
	public TooltipPanel ToolTipPanel;
	public Scrollbar scrollBar;

	public Inventory curInv;
	bool initialized = false;

	public int SelectedMax {
		get {
            return (curInv == null || curInv.items == null) ? 0 : curInv.items.Count - 1;
		}
	}

	public void Init(Inventory inv) {
		curInv = inv;
		initialized = true;
		UpdateInventory();
	}

	public void UpdateInventory() {
		inventoryBase.DestroyChildren();

		Currency.text = "<color=yellow>$</color> " + curInv.gold;
		Capacity.text = string.Format("<color=olive>({0} / {1})</color>", curInv.items.Count.ToString(), curInv.maxItems.ToString());

		if (!gameObject.activeSelf)
			return; 
		for (int i = 0; i < curInv.items.Count; i++) {
			GameObject g = (GameObject)Instantiate(inventoryButton, inventoryBase);
            Image img = g.transform.Find("Icon").GetComponent<Image>();
            img.sprite = SwitchSprite(curInv.items[i]);

			g.GetComponentInChildren<Text>().text = curInv.items[i].InvDisplay(curInv.baseWeapon);
			g.GetComponent<Button>().onClick.AddListener(() => { SelectPressed(g.transform.GetSiblingIndex()); } );
			g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(1, UIWindow.Inventory, false, false);
		}

		UpdateTooltip();
	}

    Sprite SwitchSprite(Item item)
    {
        string id = (string.IsNullOrEmpty(item.renderer.onGround)) ? "item-empty.png" : item.renderer.onGround;

        return SpriteManager.GetObjectSprite(id);
    }

    public void UpdateTooltip() {
		ToolTipPanel.gameObject.SetActive(true);
		Item i = null;

		if (World.userInterface.column == 1) {
			if (inventoryBase.childCount > 0 && curInv.items.Count > 0 && UserInterface.selectedItemNum < curInv.items.Count)
				i = curInv.items[UserInterface.selectedItemNum];
		} else
			i = GetEquipmentSlot();
		
		ToolTipPanel.UpdateTooltip(i, World.userInterface.ShowItemTooltip());
	}

	void SelectPressed(int selectedNum) {
		if (World.userInterface.column != 1 || curInv.items.Count < 1 || World.userInterface.selectBodyPart || World.userInterface.selectedItemActions)
			return;
		
		World.userInterface.IAPanel.gameObject.SetActive(true);
		World.userInterface.IAPanel.Display(curInv.items[selectedNum], curInv);
		World.userInterface.InitializeAllWindows(curInv);
	}

	Item GetEquipmentSlot() {
		if (UserInterface.selectedItemNum < curInv.entity.body.Hands.Count) {
			return curInv.entity.body.Hands[UserInterface.selectedItemNum].equippedItem;
		} else if (UserInterface.selectedItemNum == curInv.entity.body.Hands.Count) {
			return curInv.firearm;
		} else
			return curInv.entity.body.bodyParts[UserInterface.selectedItemNum - curInv.entity.body.Hands.Count - 1].equippedItem;
	}

	void Update() {
		if (!initialized || World.userInterface.CurrentState() != UIWindow.Inventory|| World.userInterface.selectedItemActions || World.userInterface.selectBodyPart)
			return;
		
		if (World.userInterface.column == 1) {
			if (inventoryBase.childCount > 0 && inventoryBase.childCount > UserInterface.selectedItemNum)
				EventSystem.current.SetSelectedGameObject(inventoryBase.GetChild(UserInterface.selectedItemNum).gameObject);
			
			if (World.playerInput.keybindings.GetKey("Enter"))
				SelectPressed(UserInterface.selectedItemNum);

			if (GameSettings.Keybindings.GetKey("GoUpStairs") && UserInterface.selectedItemNum < curInv.items.Count - 1) {
				curInv.items.Move(UserInterface.selectedItemNum, UserInterface.selectedItemNum + 1);
				UserInterface.selectedItemNum ++;
				UpdateInventory();
			} else if (GameSettings.Keybindings.GetKey("GoDownStairs") && UserInterface.selectedItemNum > 0) {
				curInv.items.Move(UserInterface.selectedItemNum, UserInterface.selectedItemNum - 1);
				UserInterface.selectedItemNum --;
				UpdateInventory();
			}
				
			scrollBar.value = 1f - (UserInterface.selectedItemNum / (float)curInv.items.Count);
		}

        for (int i = 0; i < inventoryBase.childCount; i++)
        {
            inventoryBase.GetChild(i).GetComponent<ItemButton>().selected = (World.userInterface.column == 1 && i == UserInterface.selectedItemNum);
        }

		Capacity.text = string.Format("<color=olive>({0} / {1})</color>", curInv.items.Count.ToString(), curInv.maxItems.ToString());
	}
}

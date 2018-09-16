using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class LootPanel : MonoBehaviour {

	public Transform inventoryBase;
	public GameObject inventoryButton;
	public TooltipPanel toolTipPanel;
	public Scrollbar scrollbar;
	public Text capacityText;

	Inventory inv, playerInventory;
	List<Item> relevantItems;
	bool inFocus = true;

	public int max {
		get { return (relevantItems == null) ? 0 : relevantItems.Count - 1; }
	}

	public void Init(Inventory lootInv) {
		inv = lootInv;
		UpdateInventory();
	}

	public void UpdateInventory() {
		inFocus = true;
		inventoryBase.DestroyChildren();

		if (!gameObject.activeSelf)
			return;

		if (inv == null) {
			gameObject.SetActive(false);
			return;
		} else {
			relevantItems = inv.items.FindAll(x => !x.HasProp(ItemProperty.Pool) || x.HasProp(ItemProperty.Pool) && x.GetItemComponent<CLiquidContainer>() != null 
				&& !x.GetItemComponent<CLiquidContainer>().isFull && x.GetItemComponent<CLiquidContainer>().currentAmount > 0);

			if (relevantItems.Count <= 0) {
				World.userInterface.CloseWindows();
				return;
			}

			for (int i = 0; i < relevantItems.Count; i++) {
				GameObject g = (GameObject)Instantiate(inventoryButton, inventoryBase);
                Image img = g.transform.Find("Icon").GetComponent<Image>();
                img.sprite = SwitchSprite(relevantItems[i]);

                g.GetComponentInChildren<Text>().text = relevantItems[i].InvDisplay("");
				Button b = g.GetComponent<Button>();
				b.onClick.AddListener(() => { SelectPressed(g.transform.GetSiblingIndex()); } );
				b.onClick.AddListener(() => { World.userInterface.InitializeAllWindows(); } );
			}
		}

		if (ObjectManager.player != null) {
			playerInventory = ObjectManager.playerEntity.inventory;
			capacityText.text = string.Format("<color=olive>Your Capacity: ({0}/{1})</color>", playerInventory.items.Count, playerInventory.maxItems);
		}

		UpdateTooltip();
	}

    Sprite SwitchSprite(Item item)
    {
        string id = (string.IsNullOrEmpty(item.renderer.onGround)) ? "item-empty.png" : item.renderer.onGround;

        return SpriteManager.GetObjectSprite(id);
    }

    public void UpdateTooltip() {
		if (inventoryBase.childCount <= 0 || relevantItems == null || inv == null || inv.items == null || relevantItems.Count <= UserInterface.selectedItemNum || relevantItems[UserInterface.selectedItemNum] == null) {
			toolTipPanel.gameObject.SetActive(false);
			return;
		}

		toolTipPanel.gameObject.SetActive(true);
		bool display = (inv.items.Count > 0 && UserInterface.selectedItemNum < inv.items.Count);
		toolTipPanel.UpdateTooltip(inv.items[UserInterface.selectedItemNum], display);
	}

	void SelectPressed(int index) {
		Item newItem = new Item(relevantItems[index]);		

		if (!newItem.HasProp(ItemProperty.Ammunition) && !Input.GetKey(KeyCode.LeftControl))
			newItem.amount = 1;
		if (playerInventory.CanPickupItem(newItem)) {
			playerInventory.PickupItem(newItem);

			if (World.soundManager != null)
				World.soundManager.UseItem();

			if (newItem.HasProp(ItemProperty.Ammunition) || Input.GetKey(KeyCode.LeftControl))
				inv.RemoveInstance_All(relevantItems[index]);
			else
				inv.RemoveInstance(relevantItems[index]);

			inv.gameObject.BroadcastMessage("CheckInventory", SendMessageOptions.DontRequireReceiver);

			Init(inv);
		} else if (newItem.HasProp(ItemProperty.Pool) && newItem.HasComponent<CLiquidContainer>()) {
			CLiquidContainer old = relevantItems[index].GetItemComponent<CLiquidContainer>();

			if (old.liquid == null)
				return;

			/*CLiquidContainer cl = null;

			for (int i = 0; i < playerInventory.items.Count; i++) {
				if (playerInventory.items[i].GetItemComponent<CLiquidContainer>() != null) {
					CLiquidContainer cl2 = playerInventory.items[i].GetItemComponent<CLiquidContainer>();

					if (!cl2.isFull) {
						cl = cl2;
						break;
					}
				}
			}

			if (cl == null)
				return;*/

			inFocus = false;
			World.userInterface.YesNoAction("Fill a container from the pool?", 
				() => {
					World.userInterface.ItemOnItem_Fill(newItem, playerInventory);
					/*cl.Fill(old.liquid);
					old.CheckLiquid();

					if (old.liquid == null) {
						inv.RemoveInstance(relevantItems[index]);
						inv.gameObject.BroadcastMessage("CheckInventory", SendMessageOptions.DontRequireReceiver);
					}

					World.userInterface.CloseWindows();*/
				},
				() => { World.userInterface.CloseWindows(); }, "");
		}
	}

	void Update() {
		if (!inFocus || relevantItems == null || relevantItems.Count <= 0)
			return;
		
		if (inventoryBase.childCount > 0 && inventoryBase.childCount > UserInterface.selectedItemNum)
			EventSystem.current.SetSelectedGameObject(inventoryBase.GetChild(UserInterface.selectedItemNum).gameObject);

        for (int i = 0; i < inventoryBase.childCount; i++)
        {
            inventoryBase.GetChild(i).GetComponent<ItemButton>().selected = (i == UserInterface.selectedItemNum);
        }

		scrollbar.value = 1f - (UserInterface.selectedItemNum / (float)relevantItems.Count);

		if (GameSettings.Keybindings.GetKey("Enter"))
			SelectPressed(UserInterface.selectedItemNum);
	}
}

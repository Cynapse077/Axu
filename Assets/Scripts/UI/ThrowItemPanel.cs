using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ThrowItemPanel : MonoBehaviour {
	[Header("Prefabs")]
	public GameObject inventoryButton;
	[Header("Children")]
	public Transform inventoryBase;
	public TooltipPanel ToolTipPanel;
	public Scrollbar scrollBar;

	Inventory playerInventory;

	public void Init() {
		playerInventory = ObjectManager.player.GetComponent<Inventory>();
		UpdateInventory();
	}

	public void UpdateInventory() {
		inventoryBase.DestroyChildren();

		if (!gameObject.activeSelf)
			return;

		List<Item> throwingItems = playerInventory.Items_ThrowingFirst();
		
		for (int i = 0; i < throwingItems.Count; i++) {
			GameObject g = (GameObject)Instantiate(inventoryButton, inventoryBase);
			g.GetComponentInChildren<Text>().text = throwingItems[i].InvDisplay(playerInventory.baseWeapon);
			g.GetComponent<Button>().onClick.AddListener(() => { World.userInterface.SelectPressed(g.transform.GetSiblingIndex()); } );
			g.GetComponent<Button>().onClick.AddListener(() => { World.userInterface.InitializeAllWindows(); } );
		}

		UpdateTooltip();
	}

	public void UpdateTooltip() {
		if (playerInventory.items.Count == 0) {
			ToolTipPanel.gameObject.SetActive(false);
			return;
		}

		ToolTipPanel.gameObject.SetActive(playerInventory.items.Count > 0);
		bool display = (playerInventory.items.Count > 0 && UserInterface.selectedItemNum < playerInventory.items.Count);
		ToolTipPanel.UpdateTooltip(playerInventory.Items_ThrowingFirst()[UserInterface.selectedItemNum], display);
	}

	void Update() {
		if (World.userInterface.column == 1 && inventoryBase.childCount > 0 && inventoryBase.childCount > UserInterface.selectedItemNum)
			EventSystem.current.SetSelectedGameObject(inventoryBase.GetChild(UserInterface.selectedItemNum).gameObject);

		scrollBar.value = 1f - ((float)UserInterface.selectedItemNum / (float)playerInventory.items.Count);
	}
}

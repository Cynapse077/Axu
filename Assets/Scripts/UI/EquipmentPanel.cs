using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentPanel : MonoBehaviour {

	[Header("Prefabs")]
	public GameObject equipmentButton;
	[Header("Children")]
	public Transform equipmentBase;
	public Scrollbar scrollBar;

	public Inventory curInv;

	public int SelectedMax {
		get {
			if (curInv == null)
				return 0;
			return curInv.entity.body.bodyParts.Count + curInv.entity.body.Hands.Count;
		}
	}

	public void Init(Inventory inv) {
		curInv = inv;
		UpdateEquipment();
	}

	void UpdateEquipment() {
		equipmentBase.DestroyChildren();

		if (!gameObject.activeSelf)
			return;

		List<BodyPart.Hand> hands = curInv.entity.body.Hands;

		for (int i = 0; i < hands.Count; i++) {
			GameObject wep = (GameObject)Instantiate(equipmentButton, equipmentBase);
			wep.GetComponentInChildren<Text>().text = hands[i].equippedItem.InvDisplay(curInv.baseWeapon, false, true, false);

			string n = LocalizationManager.GetContent("Slot_Hand") + " " + ((i % 2 == 0) ? LocalizationManager.GetContent("Limb_Right") : LocalizationManager.GetContent("Limb_Left"));

			n = ((hands[i] == curInv.entity.body.MainHand) ? "<color=yellow>" : "<color=orange>") + n + "</color>";

			wep.transform.GetChild(1).GetComponent<Text>().text = n;
			wep.GetComponent<Button>().onClick.AddListener(() => { SelectPressed(wep.transform.GetSiblingIndex()); });
			wep.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(0, UIWindow.Inventory);

		}

		GameObject fire = (GameObject)Instantiate(equipmentButton, equipmentBase);
		fire.GetComponentInChildren<Text>().text = (curInv.firearm == null) ? ItemList.GetNone().Name : curInv.firearm.InvDisplay(curInv.baseWeapon, false, true, true);
		fire.transform.GetChild(1).GetComponent<Text>().text = "<color=orange>" + LocalizationManager.GetLocalizedContent("TT_Ranged")[0] + "</color>";
		fire.GetComponent<Button>().onClick.AddListener(() => { SelectPressed(fire.transform.GetSiblingIndex()); } );
		fire.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(0, UIWindow.Inventory);

		for (int i = 0; i < curInv.entity.body.bodyParts.Count; i++) {
			GameObject g = (GameObject)Instantiate(equipmentButton, equipmentBase);
			g.GetComponentInChildren<Text>().text = curInv.entity.body.bodyParts[i].equippedItem.InvDisplay(curInv.baseWeapon, true, false);
			g.transform.GetChild(1).GetComponent<Text>().text = curInv.entity.body.bodyParts[i].displayName;
			g.GetComponent<Button>().onClick.AddListener(() => { SelectPressed(g.transform.GetSiblingIndex()); } );
			g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(0, UIWindow.Inventory);
		}
	}

	void SelectPressed(int selectedNum) {
		if (World.userInterface.column != 0 || World.userInterface.selectBodyPart || World.userInterface.selectedItemActions)
			return;
		List<BodyPart.Hand> hands = curInv.entity.body.Hands;

		if (selectedNum < hands.Count) {
			curInv.UnEquipWeapon(hands[selectedNum].equippedItem, selectedNum);
		} else if (selectedNum == hands.Count) {
			curInv.UnEquipFirearm(true);
		} else {
			curInv.UnEquipArmor(curInv.entity.body.bodyParts[selectedNum - curInv.entity.body.Hands.Count - 1], true);
		}

		World.userInterface.InvPanel.Init(curInv);
		World.userInterface.InitializeAllWindows(curInv);
		Init(curInv);
	}

	void Update() {
		if (World.userInterface.CurrentState() != UIWindow.Inventory || World.userInterface.selectBodyPart || World.userInterface.selectedItemActions)
			return;

		if (World.userInterface.column == 0) {
			if (equipmentBase.childCount > 0 && equipmentBase.childCount > UserInterface.selectedItemNum)
				EventSystem.current.SetSelectedGameObject(equipmentBase.GetChild(UserInterface.selectedItemNum).gameObject);

			if (World.playerInput.keybindings.GetKey("Enter"))
				SelectPressed(UserInterface.selectedItemNum);

			scrollBar.value = 1f - ((float)UserInterface.selectedItemNum / (float)(curInv.entity.body.bodyParts.Count + curInv.entity.body.Hands.Count + 1));	
		}
	}
}

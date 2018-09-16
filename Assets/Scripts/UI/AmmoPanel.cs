using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class AmmoPanel : MonoBehaviour {

	Text ammoText;

	Inventory playerInventory;
	bool isActive = false;

	public void Display(bool show) {
		if (playerInventory == null) {
			playerInventory = ObjectManager.player.GetComponent<Inventory>();
			ammoText = GetComponentInChildren<Text>();
		}

		for (int i = 0; i < transform.childCount; i++) {
			transform.GetChild(i).gameObject.SetActive(show);
		}

		GetComponent<Image>().enabled = show;
		isActive = show;
	}

	void Update() {
		if (!isActive || playerInventory == null)
			return;

		ChangeDisplayText();
	}

	void ChangeDisplayText() {
		if (playerInventory.firearm.HasProp(ItemProperty.Ranged)) {
			CFirearm cf = playerInventory.firearm.GetItemComponent<CFirearm>();

			if (cf != null) {
				ammoText.text = "(" + cf.curr + "/" + cf.max + ")";
			} else {
				Display(false);
			}
		} else {
			if (playerInventory.firearm.stackable) {
				ammoText.text = "(" + playerInventory.firearm.amount + ")";
			} else {
				Display(false);
			}
		}
	}
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LookTooltipPanel : MonoBehaviour {

	public Text DisplayName;
	public Text Content1;
	public Text Content2;
	public Text Content3;

	public void HoverOver(BaseAI npc) {
		Reset();

		DisplayName.text = npc.npcBase.name;

		string hostility = (npc.isHostile) ? "TT_Hostile" : "TT_Passive", awareness = (npc.HasSeenPlayer()) ? "TT_Aware" : "TT_Unaware";
		if (npc.npcBase.HasFlag(NPC_Flags.Follower))
			hostility = "TT_Follower";

		string localizedHos = LocalizationManager.GetLocalizedContent(hostility)[0];
		string localizedAwa = LocalizationManager.GetLocalizedContent(awareness)[0];
			
		Content1.text = "<color=silver>[" + npc.npcBase.faction.Name + "]</color>";
		Content2.text = "(" + localizedHos + " / " + localizedAwa + ")";

		Inventory npcInv = npc.GetComponent<Inventory>();
		string wepName = "";
		List<BodyPart.Hand> hands = npcInv.entity.body.Hands;

		if (hands.Count > 0) {
			for (int i = 0; i < hands.Count; i++) {
				if (hands[i].equippedItem == null)
					hands[i].SetEquippedItem(ItemList.GetItemByID(npc.entity.inventory.baseWeapon), npc.entity);

				wepName += hands[i].equippedItem.DisplayName();

				if (i < hands.Count - 1)
					wepName += ", ";
			}
		} else {
			wepName = npcInv.entity.body.defaultHand.equippedItem.DisplayName();
		}

		if (npc.entity.inventory.firearm != null && npc.entity.inventory.firearm.ID != "none")
			wepName += ", " + npc.entity.inventory.firearm.DisplayName();

		Content3.text = LocalizationManager.GetLocalizedContent("TT_Weapon")[0] + ": " + wepName + "\n"
			+ LocalizationManager.GetLocalizedContent("TT_Armor")[0] + ": " + npc.npcBase.Attributes["Defense"].ToString();
	}

	public void HoverOver(MapObjectSprite mos) {
		Reset();

		DisplayName.text = mos.name;
		Inventory inv = mos.GetComponent<Inventory>();

		if (inv != null && inv.items.Count > 0) {
			for (int i = 0; i < inv.items.Count; i++) {
				if (i <= 3)
					Content1.text += inv.items[i].DisplayName() + "\n";
			}

			if (inv.items.Count > 4)
				Content2.text = LocalizationManager.GetLocalizedContent("TT_More")[0];
		} else {
			Content1.text = mos.Description;
		}
	}

	public void Reset() {
		DisplayName.text = "";
		Content1.text = "";
		Content2.text = "";
		Content3.text = "";
	}
}
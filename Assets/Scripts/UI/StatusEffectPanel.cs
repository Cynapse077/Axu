using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusEffectPanel : MonoBehaviour {

	public GameObject hunger;
	public GameObject flying;
	public GameObject poison;
	public GameObject bleed;
	public GameObject confuse;
	public GameObject topple;
	public GameObject stun;
	public GameObject slow;
	public GameObject regen;
	public GameObject haste;
	public GameObject stuck;
	public GameObject held;
	public GameObject unconscious;
	public GameObject shield;
	public GameObject drunk;
	public GameObject burning;
	public GameObject sick;
	public GameObject blind;

	public Sprite[] hungerSprites;

	Image hungerImage;

	public void UpdateEnabledStatuses(Stats stats) {
		if (stats == null)
			return;

		if (hungerImage == null)
			hungerImage = hunger.GetComponent<Image>();

		hungerImage.sprite = hungerSprites[HungerSprites(stats)];

		flying.SetActive(stats.IsFlying());
		poison.SetActive(stats.HasEffect("Poison"));
		bleed.SetActive(stats.HasEffect("Bleed"));
		confuse.SetActive(stats.HasEffect("Confuse"));
		topple.SetActive(stats.HasEffect("Topple"));
		stun.SetActive(stats.HasEffect("Stun"));
		slow.SetActive(stats.HasEffect("Slow"));
		regen.SetActive(stats.HasEffect("Regen"));
		haste.SetActive(stats.HasEffect("Haste"));
		stuck.SetActive(stats.HasEffect("Stuck"));
		held.SetActive(stats.entity.body.AllGripsAgainst().Count > 0);
		shield.SetActive(stats.HasEffect("Shield"));
		unconscious.SetActive(stats.HasEffect("Unconscious"));
		drunk.SetActive(stats.HasEffect("Drunk"));
		burning.SetActive(stats.HasEffect("Aflame"));
		sick.SetActive(stats.HasEffect("Sick"));
		blind.SetActive(stats.HasEffect("Blind"));
	}

	int HungerSprites(Stats stats) {
		bool isVamp = stats.hasTraitEffect(TraitEffects.Vampirism);
		string localTextKey = "";
		int id = 0;

		if (stats.Hunger >= Globals.Satiated) {
			localTextKey = (isVamp) ? "Thirst_1" : "Food_1";
			id = (isVamp) ? 5 : 0;
		} else if (stats.Hunger < Globals.Satiated && stats.Hunger >= Globals.Hungry) {
			localTextKey = (isVamp) ? "Thirst_2" : "Food_2";
			id = (isVamp) ? 6 : 1;
		} else if (stats.Hunger < Globals.Hungry && stats.Hunger >= Globals.VHungry) {
			localTextKey = (isVamp) ? "Thirst_3" : "Food_3";
			id = (isVamp) ? 7 : 2;
		} else if (stats.Hunger < Globals.VHungry && stats.Hunger > Globals.Starving) {
			localTextKey = (isVamp) ? "Thirst_4" : "Food_4";
			id = (isVamp) ? 8 : 3;
		} else {
			localTextKey = (isVamp) ? "Thirst_5" : "Food_5";
			id = (isVamp) ? 9 : 4;
		}

		hunger.GetComponentInChildren<LocalizedText>().GetLocalizedText(localTextKey);
		return id;
	}
}

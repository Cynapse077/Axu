using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilityTooltip : MonoBehaviour {

	public Text title;
	public Text lvXP;
	public Text stCooldown;
	public Text description;

	public void UpdateTooltip(Skill ability) {
		title.text = ability.Name;
		lvXP.text = (ability.CanLevelUp) ? "Lv " + ability.level + "\n<color=silver>(" + (ability.XP/10.0).ToString() + "% xp)</color>" : LocalizationManager.GetLocalizedContent("Cannot_Level_Up")[0];
		stCooldown.text = "<color=green>ST</color> Cost: " + ability.staminaCost.ToString() + "\n<color=orange>Cooldown</color>: " + ability.maxCooldown;
		description.text = ability.Description;

		if (ability.Description.Contains("[ROLL]")) {
			description.text = ability.Description.Replace("[ROLL]", ability.totalDice.ToString());
		}
	}
}

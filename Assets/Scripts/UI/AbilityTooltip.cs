using UnityEngine;
using UnityEngine.UI;

public class AbilityTooltip : MonoBehaviour
{
    public Text title;
    public Text lvXP;
    public Text stCooldown;
    public Text description;
    public Image icon;

    public void UpdateTooltip(Ability ability)
    {
        title.text = ability.Name;
        lvXP.text = (ability.CanLevelUp) ? "Lv " + ability.level + "  <color=silver>(" + (ability.XP / 10.0).ToString() + "% xp)</color>" : LocalizationManager.GetContent("Cannot_Level_Up");
        stCooldown.text = "<color=green>ST</color> Cost: " + ability.staminaCost.ToString() + "\n<color=orange>Cooldown</color>: " + ability.maxCooldown;
        description.text = ability.Description;
        icon.sprite = ability.IconSprite;

        if (ability.Description.Contains("[ROLL]"))
        {
            description.text = ability.Description.Replace("[ROLL]", ability.totalDice.ToString());
        }
    }
}

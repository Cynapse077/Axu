using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPanel : MonoBehaviour
{

    public Text playerName;
    public Text level;

    [Header("Attributes")]
    public Text strength;
    public Text dexterity;
    public Text intelligence;
    public Text endurance;

    public Text speed;
    public Text attackDelay;
    public Text accuracy;
    public Text stealth;
    public Text charisma;
    public Text heatResist;
    public Text coldResist;
    public Text energyResist;
    public Text radiation;

    [Header("Anchors")]
    public Transform profAnchor;
    public Transform traitAnchor;
    public Transform bpAnchor;

    [Header("Prefabs")]
    public GameObject profGO;
    public GameObject traitGO;
    public GameObject bpGO;

    public void Initialize(Stats stats, Inventory inv)
    {
        DestroyChildren();

        playerName.text = Manager.playerName + " the " + Manager.profName;
        level.text = string.Format("Lvl: {0} <color=silver>({1} / {2})xp</color>", stats.MyLevel.CurrentLevel, stats.MyLevel.XP, stats.MyLevel.XPToNext);

        strength.text = strength.GetComponent<LocalizedText>().BaseText + ": <color=orange>" + stats.Strength.ToString() + "</color>";
        dexterity.text = dexterity.GetComponent<LocalizedText>().BaseText + ": <color=orange>" + stats.Dexterity.ToString() + "</color>";
        intelligence.text = intelligence.GetComponent<LocalizedText>().BaseText + ": <color=orange>" + stats.Intelligence.ToString() + "</color>";
        endurance.text = endurance.GetComponent<LocalizedText>().BaseText + ": <color=orange>" + stats.Endurance.ToString() + "</color>";

        charisma.text = charisma.GetComponent<LocalizedText>().BaseText + ": <color=orange>" + stats.Attributes["Charisma"].ToString() + "</color>";

        speed.text = speed.GetComponent<LocalizedText>().BaseText + ": <color=orange>" + stats.Speed.ToString() + "</color>";
        attackDelay.text = attackDelay.GetComponent<LocalizedText>().BaseText + ": <color=orange>" + stats.AttackDelay.ToString() + "</color>";
        accuracy.text = accuracy.GetComponent<LocalizedText>().BaseText + ": <color=orange>" + stats.Accuracy.ToString() + "</color>";
        stealth.text = stealth.GetComponent<LocalizedText>().BaseText + ": <color=orange>" + stats.Attributes["Stealth"].ToString() + "</color>";

        heatResist.text = "<color=orange>" + heatResist.GetComponent<LocalizedText>().BaseText + "</color>: " + stats.HeatResist.ToString() + "%";
        coldResist.text = "<color=cyan>" + coldResist.GetComponent<LocalizedText>().BaseText + "</color>: " + stats.ColdResist.ToString() + "%";
        energyResist.text = "<color=yellow>" + energyResist.GetComponent<LocalizedText>().BaseText + "</color>: " + stats.EnergyResist.ToString() + "%";

        radiation.GetComponent<LocalizedText>().SetText(stats.RadiationDesc());

        List<WeaponProficiency> profs = stats.proficiencies.GetProfs().OrderByDescending(p => p.level).ToList();

        foreach (WeaponProficiency p in profs)
        {
            if (p.level > 1 || p.xp > 0)
            {
                GameObject pr = SimplePool.Spawn(profGO, profAnchor);
                string pXP = (p.xp / 10.0).ToString();
                string levelText = (p.level - 1).ToString();
                pr.GetComponent<Text>().text = string.Format("{0} - {1} <color=orange>({2})</color>\n  <color=silver>({3}%x p)</color>", p.name, p.LevelName(), levelText, pXP);
            }
        }

        foreach (Trait t in stats.traits)
        {
            GameObject tr = SimplePool.Spawn(traitGO, traitAnchor);
            string tName = "    " + t.name;

            if (t.ContainsEffect(TraitEffects.Mutation))
                tName = "<color=magenta>" + tName + "</color>";
            else if (t.ContainsEffect(TraitEffects.Disease))
                tName = "<color=red>" + tName + "</color>";
            else
                tName = "<color=yellow>" + tName + "</color>";

            tr.GetComponent<Text>().text = string.Format("{0} - \n<i>{1}</i>", tName, t.description);
        }

        foreach (BodyPart b in stats.entity.body.bodyParts)
        {
            GameObject bp = SimplePool.Spawn(bpGO, bpAnchor);
            Text text = bp.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.text = (b.isAttached ? "<color=green>" : "<color=red>") + b.displayName + "</color> <color=grey>[" + (b.armor + b.equippedItem.armor + stats.Defense).ToString() + "]</color>";

            if (b.isAttached && b.wounds.Count > 0)
            {
                for (int i = 0; i < b.wounds.Count; i++)
                {
                    text.text += "\n    <color=red>[" + b.wounds[i].Name + ":";

                    for (int j = 0; j < b.wounds[i].statMods.Count; j++)
                    {
                        text.text += " " + LocalizationManager.GetContent(b.wounds[i].statMods[j].Stat) + " " + b.wounds[i].statMods[j].Amount.ToString();
                    }

                    text.text += "]</color>";
                }
            }

            for (int i = 0; i < b.Attributes.Count; i++)
            {
                if (b.Attributes[i].Stat != "Hunger" && b.Attributes[i].Stat != "Defense")
                    text.text += "\n    " + LocalizationManager.GetLocalizedContent(b.Attributes[i].Stat)[0] + " <color=orange>(" + b.Attributes[i].Amount + ")</color>";
            }
        }
    }

    void DestroyChildren()
    {
        profAnchor.DespawnChildren();
        traitAnchor.DespawnChildren();
        bpAnchor.DespawnChildren();
    }
}

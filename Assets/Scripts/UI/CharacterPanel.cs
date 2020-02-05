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

    [Header("Prefabs")]
    public GameObject profGO;

    public void Initialize(Stats stats, Inventory inv)
    {
        DestroyChildren();

        playerName.text = (stats.entity.isPlayer) ? Manager.playerName + " the " + Manager.profName : stats.entity.MyName;
        level.text = (stats.entity.isPlayer) ? string.Format("Lvl: {0} <color=silver>({1} / {2})xp</color>", stats.level.CurrentLevel, stats.level.XP, stats.level.XPToNext) : "";

        strength.text = stats.Strength.ToString();
        dexterity.text = stats.Dexterity.ToString();
        intelligence.text = stats.Intelligence.ToString();
        endurance.text = stats.Endurance.ToString();

        speed.text = stats.Speed.ToString();

        string atkDly = stats.AttackDelay.ToString();
        attackDelay.text = atkDly;

        if (stats.AttackDelay > 0)
        {
            attackDelay.text = "<color=red>" + atkDly + "</color>";
        }
        else if (stats.AttackDelay < 0)
        {
            attackDelay.text = "<color=green>" + atkDly + "</color>";
        }

        accuracy.text = stats.Accuracy.ToString();
        stealth.text = (stats.entity.isPlayer) ? stats.Attributes["Stealth"].ToString() : "0";
        charisma.text = (stats.entity.isPlayer) ? stats.Attributes["Charisma"].ToString() : "0";

        heatResist.text = stats.HeatResist.ToString() + "%";
        coldResist.text = stats.ColdResist.ToString() + "%";
        energyResist.text = stats.EnergyResist.ToString() + "%";

        radiation.text = LocalizationManager.GetContent(stats.RadiationDesc());

        if (stats.entity.isPlayer)
        {
            List<WeaponProficiency> profs = stats.proficiencies.GetProfs().OrderByDescending(p => p.level).ToList();

            foreach (WeaponProficiency p in profs)
            {
                if (p.level > 1 || p.xp > 0)
                {
                    GameObject pr = Instantiate(profGO, profAnchor);
                    string pXP = (p.xp / 10.0).ToString();
                    string levelText = (p.level - 1).ToString();
                    pr.GetComponent<Text>().text = string.Format("{0} - {1} <color=orange>({2})</color>\n  <color=silver>({3}% xp)</color>", p.name, p.LevelName(), levelText, pXP);
                }
            }
        }
        
        GetComponentInChildren<BodyDisplayPanel>().Init(stats.entity.body);
    }

    void DestroyChildren()
    {
        profAnchor.DestroyChildren();
    }
}

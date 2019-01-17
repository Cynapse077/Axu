using System.Collections.Generic;

public class Wound
{
    public string Name, ID, Desc;
    public ItemProperty slot;
    public List<DamageTypes> damTypes;
    public List<Stat_Modifier> statMods;

    public Wound() { }

    public Wound(string _name, string _ID, ItemProperty _slot, List<DamageTypes> dt)
    {
        Name = _name;
        ID = _ID;
        slot = _slot;
        damTypes = dt;
        statMods = new List<Stat_Modifier>();
    }

    public Wound(Wound other)
    {
        Name = other.Name;
        ID = other.ID;
        Desc = other.Desc;
        slot = other.slot;
        damTypes = other.damTypes;
        statMods = new List<Stat_Modifier>();

        for (int i = 0; i < other.statMods.Count; i++)
        {
            statMods.Add(new Stat_Modifier(other.statMods[i]));
        }
    }

    public void Inflict(BodyPart bp)
    {
        ChangeStats(bp, false);

        string message = LocalizationManager.GetContent("Message_Wound");
        message = message.Replace("[NAME]", bp.myBody.entity.MyName);
        message = message.Replace("[PART]", bp.displayName);
        message = message.Replace("[WOUND]", Name);
        CombatLog.NewMessage(message);

        bp.wounds.Add(this);
    }

    public void Cure(BodyPart bp)
    {
        ChangeStats(bp, true);

        if (bp.myBody.entity.isPlayer)
            CombatLog.NameItemMessage("Message_Wound_Cure", bp.displayName, Name);

        bp.wounds.Remove(this);
    }

    void ChangeStats(BodyPart bp, bool removeMe)
    {
        int am = (removeMe) ? -1 : 1;

        foreach (Stat_Modifier sm in statMods)
        {
            if (sm.Stat == "Health")
            {
                bp.myBody.entity.stats.maxHealth += (sm.Amount * am);
                bp.myBody.entity.stats.health += (sm.Amount * am);
            }
            else if (sm.Stat == "Stamina")
            {
                bp.myBody.entity.stats.maxStamina += (sm.Amount * am);
                bp.myBody.entity.stats.stamina += (sm.Amount * am);
            }
            else if (sm.Stat == "Armor")
            {
                bp.armor += sm.Amount * am;
            }
            else
            {
                bp.myBody.entity.stats.ChangeAttribute(sm.Stat, sm.Amount * am);
            }
        }
    }
}

using System.Collections.Generic;
using LitJson;

public class Wound : IAsset
{
    public string Name;
    public string ID { get; set; }
    public string ModID { get; set; }
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
        slot = other.slot;
        damTypes = other.damTypes;
        statMods = new List<Stat_Modifier>();

        for (int i = 0; i < other.statMods.Count; i++)
        {
            statMods.Add(new Stat_Modifier(other.statMods[i]));
        }
    }

    public Wound(JsonData dat)
    {
        FromJson(dat);
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
            switch (sm.Stat)
            {
                case "Armor":
                    bp.armor += sm.Amount * am;
                    break;

                default:
                    bp.myBody.entity.stats.ChangeAttribute(sm.Stat, sm.Amount * am);
                    break;
            }
        }
    }

    public void FromJson(JsonData dat)
    {
        Name = dat["Name"].ToString();
        ID = dat["ID"].ToString();
        slot = dat["Slot"].ToString().ToEnum<ItemProperty>();
        damTypes = new List<DamageTypes>();

        if (dat.ContainsKey("Damage Types"))
        {
            for (int i = 0; i < dat["Damage Types"].Count; i++)
            {
                DamageTypes dt = (dat["Damage Types"][i].ToString()).ToEnum<DamageTypes>();
                damTypes.Add(dt);
            }
        }

        List<Stat_Modifier> sm = new List<Stat_Modifier>();

        if (dat.ContainsKey("Stats"))
        {
            for (int i = 0; i < dat["Stats"].Count; i++)
            {
                Stat_Modifier s = new Stat_Modifier(dat["Stats"][i]["Stat"].ToString(), (int)dat["Stats"][i]["Amount"]);
                sm.Add(s);
            }

            statMods = sm;
        }
    }

    public IEnumerable<string> LoadErrors()
    {
        if (Name.NullOrEmpty())
        {
            yield return "Name not set.";
        }
    }
}

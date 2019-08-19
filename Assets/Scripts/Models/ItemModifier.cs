using System.Collections.Generic;
using LitJson;

public class ItemModifier : IAsset
{

    public string ID { get; set; }
    public string ModID { get; set; }
    public string name;
    public Damage damage;
    public int armor, accuracy, cost;
    public bool unique = false;
    public string description;
    public List<ItemProperty> properties = new List<ItemProperty>();
    public DamageTypes damageType;
    public ModType modType;
    public List<Stat_Modifier> statMods = new List<Stat_Modifier>();
    public List<CComponent> components = new List<CComponent>();

    public static ItemModifier Empty()
    {
        return new ItemModifier();
    }

    public ItemModifier()
    {
        name = "";
        ID = "";
        damage = new Damage(0, 0, 0, DamageTypes.Blunt);
        damageType = DamageTypes.Blunt;
        description = "";
    }

    public ItemModifier(ItemModifier other)
    {
        name = other.name;
        ID = other.ID;
        damage = other.damage;
        damageType = other.damageType;
        description = other.description;
        statMods = other.statMods;
        accuracy = other.accuracy;
        cost = other.cost;
        properties = other.properties;
        modType = other.modType;
        unique = other.unique;
        components = other.components;
    }

    public ItemModifier(JsonData dat)
    {
        FromJson(dat);
    }

    public bool CanAddToItem(Item i)
    {
        if (i.HasProp(ItemProperty.NoMods) || i.stackable || !i.lootable)
            return false;

        if (modType == ModType.Weapon && i.HasProp(ItemProperty.Weapon))
            return true;
        if (modType == ModType.Armor && i.HasProp(ItemProperty.Armor))
            return true;
        if (modType == ModType.All && (i.HasProp(ItemProperty.Weapon) || i.HasProp(ItemProperty.Armor) || i.HasProp(ItemProperty.Ranged)))
            return true;
        if (modType == ModType.Ranged && i.HasProp(ItemProperty.Ranged))
            return true;
        if (modType == ModType.Shield && i.itemType == Proficiencies.Shield)
            return true;

        return false;
    }

    void FromJson(JsonData dat)
    {
        name = dat["Name"].ToString();
        ID = dat["ID"].ToString();
        description = dat["Description"].ToString();

        dat.TryGetValue("Armor", out armor);
        dat.TryGetValue("Cost", out cost);
        dat.TryGetValue("Mod Type", out modType, true);
        dat.TryGetValue("Damage Type", out damageType, true);
        dat.TryGetValue("Unique", out unique);

        if (dat.ContainsKey("Damage"))
        {
            string mDamage = dat["Damage"].ToString();
            damage = Damage.GetByString(mDamage);
        }
        if (dat.ContainsKey("Properties"))
        {
            for (int p = 0; p < dat["Properties"].Count; p++)
            {
                string tag = dat["Properties"][p].ToString();
                properties.Add(tag.ToEnum<ItemProperty>());
            }
        }

        if (dat.ContainsKey("Stat Mods"))
        {
            for (int p = 0; p < dat["Stat Mods"].Count; p++)
            {
                string tag = dat["Stat Mods"][p]["Stat"].ToString();
                int amount = (int)dat["Stat Mods"][p]["Amount"];
                statMods.Add(new Stat_Modifier(tag, amount));
            }
        }

        if (dat.ContainsKey("Components"))
        {
            components = ItemUtility.GetComponentsFromData(dat["Components"]);
        }
    }

    public enum ModType
    {
        All, Weapon, Armor, Ranged, Shield
    }
}

using System.Collections.Generic;
using LitJson;

public class NPC_Blueprint
{
    public readonly string name = "", id = "";
    public readonly Faction faction;
    public readonly int health, stamina;
    public readonly int heatResist, coldResist;
    public readonly string quest = "", dialogue = "";
    public readonly int maxItems, maxItemRarity;
    public readonly string firearm;
    public readonly string Corpse_Item;
    public readonly Coord localPosition;
    public readonly int elevation;
    public readonly string zone;
    public readonly int weaponSkill;

    public readonly string[] spriteIDs;
    public readonly KeyValuePair<string, Coord>[] inventory;
    public readonly KeyValuePair<string, int>[] skills;

    public readonly List<BodyPart> bodyParts;
    public readonly List<NPC_Flags> flags;
    public readonly List<string> weaponPossibilities;
    public Dictionary<string, int> attributes;

    public NPC_Blueprint(JsonData dat)
    {
        attributes = DefaultAttributes();
        flags = new List<NPC_Flags>();
        weaponPossibilities = new List<string>();
        bodyParts = EntityList.GetBodyStructure(dat["Body Structure"].ToString());

        name = dat["Name"].ToString();
        id = dat["ID"].ToString();
        faction = FactionList.GetFactionByID(dat["Faction"].ToString());
        health = (int)dat["Stats"]["Health"];
        stamina = (int)dat["Stats"]["Stamina"];
        maxItems = (dat.ContainsKey("MaxItems")) ? (int)dat["MaxItems"] : 0;
        maxItemRarity = (dat.ContainsKey("MaxItemRarity")) ? (int)dat["MaxItemRarity"] : 0;

        if (dat.ContainsKey("Sprite"))
        {
            string s = dat["Sprite"].ToString();

            if (s.Contains("|"))
            {
                string[] ids = s.Split("|"[0]);
                spriteIDs = new string[ids.Length];

                for (int i = 0; i < ids.Length; i++)
                {
                    spriteIDs[i] = ids[i];
                }
            }
            else
            {
                spriteIDs = new string[1];
                spriteIDs[0] = s;
            }
        }
        else
        {
            spriteIDs = new string[0];
        }

        for (int f = 0; f < dat["Flags"].Count; f++)
        {
            string flag = dat["Flags"][f].ToString();
            flags.Add(flag.ToEnum<NPC_Flags>());
        }

        List<string> keys = new List<string>(attributes.Keys);

        foreach (string a in keys)
        {
            if (dat["Stats"].ContainsKey(a))
                attributes[a] = (int)dat["Stats"][a];
        }

        if (dat.ContainsKey("Skills"))
        {
            skills = new KeyValuePair<string, int>[dat["Skills"].Count];

            for (int i = 0; i < dat["Skills"].Count; i++)
            {
                skills[i] = new KeyValuePair<string, int>(dat["Skills"][i]["ID"].ToString(), (int)dat["Skills"][i]["Level"]);
            }
        }
        else
        {
            skills = new KeyValuePair<string, int>[0];
        }

        if (dat.ContainsKey("Inventory"))
        {
            inventory = new KeyValuePair<string, Coord>[dat["Inventory"].Count];

            for (int i = 0; i < dat["Inventory"].Count; i++)
            {
                Coord amt = new Coord(1, (int)dat["Inventory"][i]["Max"]);

                if (dat["Inventory"][i].ContainsKey("Min"))
                    amt.x = (int)dat["Inventory"][i]["Min"];

                inventory[i] = new KeyValuePair<string, Coord>(dat["Inventory"][i]["Item"].ToString(), amt);
            }
        }
        else
        {
            inventory = new KeyValuePair<string, Coord>[0];
        }

        for (int w = 0; w < dat["Weapon_Choices"].Count; w++)
        {
            weaponPossibilities.Add(dat["Weapon_Choices"][w].ToString());
        }

        if (dat.ContainsKey("Weapon Skill"))
        {
            weaponSkill = (int)dat["Weapon Skill"];
        }

        if (dat.ContainsKey("Firearm"))
        {
            firearm = dat["Firearm"].ToString();
        }
            

        if (dat.ContainsKey("Corpse_Item"))
        {
            Corpse_Item = dat["Corpse_Item"].ToString();
        }

        if (dat.ContainsKey("Quest"))
            quest = dat["Quest"].ToString();

        if (dat.ContainsKey("Dialogue"))
            dialogue = dat["Dialogue"].ToString();

        if (dat.ContainsKey("Position"))
        {
            localPosition = new Coord((int)dat["Position"]["x"], (int)dat["Position"]["y"]);
            elevation = (int)dat["Position"]["z"];
            zone = dat["Position"]["Zone"].ToString();
        }
        else
        {
            zone = "";
        }
    }

    public static Dictionary<string, int> DefaultAttributes()
    {
        return new Dictionary<string, int>()
        {
            { "Strength", 1 }, { "Dexterity", 1 }, { "Intelligence", 1 }, { "Endurance", 1 },
            { "Accuracy", 1 }, { "Speed", 10 }, { "Perception", 10 }, { "Defense", 0 },
            { "Heat Resist", 0 }, { "Cold Resist", 0 }, { "Energy Resist", 0 }, { "Attack Delay", 0 },
            { "HP Regen", 0 }, { "ST Regen", 0 }
        };
    }
}

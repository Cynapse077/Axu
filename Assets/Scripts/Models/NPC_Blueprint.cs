using System.Collections.Generic;
using LitJson;

public class NPC_Blueprint : IAsset
{
    public string name = "";
    public string ID { get; set; }
    public Faction faction;
    public int health, stamina;
    public int heatResist, coldResist;
    public string quest = "", dialogue = "";
    public int maxItems, maxItemRarity;
    public string firearm;
    public string Corpse_Item;
    public Coord localPosition;
    public int elevation;
    public string zone;
    public int weaponSkill;

    public string[] spriteIDs;
    public KeyValuePair<string, Coord>[] inventory;
    public KeyValuePair<string, int>[] skills;

    public List<BodyPart> bodyParts;
    public List<NPC_Flags> flags;
    public List<string> weaponPossibilities;
    public Dictionary<string, int> attributes;

    public NPC_Blueprint(JsonData dat)
    {
        FromJson(dat);    
    }

    void FromJson(JsonData dat)
    {
        attributes = DefaultAttributes();
        flags = new List<NPC_Flags>();
        weaponPossibilities = new List<string>();
        bodyParts = EntityList.GetBodyStructure(dat["Body Structure"].ToString());

        name = dat["Name"].ToString();
        ID = dat["ID"].ToString();
        
        faction = GameData.instance.Get<Faction>(dat["Faction"].ToString()) as Faction;
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
                spriteIDs = new string[1] { s };
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
            {
                attributes[a] = (int)dat["Stats"][a];
            }
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

        dat.TryGetValue("Weapon Skill", out weaponSkill);
        dat.TryGetValue("Firearm", out firearm);
        dat.TryGetValue("Corpse_Item", out Corpse_Item);
        dat.TryGetValue("Quest", out quest);
        dat.TryGetValue("Dialogue", out dialogue);

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

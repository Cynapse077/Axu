using System;
using System.Text;
using System.Collections.Generic;
using LitJson;

public class NPC_Blueprint : IAsset
{
    public string name = "";
    public string ID { get; set; }
    public string ModID { get; set; }
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

    public NPC_Blueprint(NPC_Blueprint other)
    {
        CopyFrom(other);
    }

    public void CopyFrom(NPC_Blueprint other)
    {
        name = other.name;
        ID = other.ID;
        ModID = other.ModID;
        faction = other.faction;
        health = other.health;
        stamina = other.stamina;
        heatResist = other.heatResist;
        coldResist = other.coldResist;
        quest = other.quest;
        dialogue = other.dialogue;
        maxItemRarity = other.maxItemRarity;
        maxItems = other.maxItems;
        firearm = other.firearm;
        Corpse_Item = other.Corpse_Item;
        localPosition = other.localPosition;
        elevation = other.elevation;
        zone = other.zone;
        weaponSkill = other.weaponSkill;
        spriteIDs = other.spriteIDs;

        inventory = new KeyValuePair<string, Coord>[other.inventory.Length];
        for (int i = 0; i < other.inventory.Length; i++)
        {
            inventory[i] = new KeyValuePair<string, Coord>(other.inventory[i].Key, other.inventory[i].Value);
        }

        skills = new KeyValuePair<string, int>[other.skills.Length];
        for (int i = 0; i < other.skills.Length; i++)
        {
            skills[i] = new KeyValuePair<string, int>(other.skills[i].Key, other.skills[i].Value);
        }

        bodyParts = new List<BodyPart>(other.bodyParts);
        flags = new List<NPC_Flags>(other.flags);
        weaponPossibilities = new List<string>(other.weaponPossibilities);
        attributes = new Dictionary<string, int>(other.attributes);
    }

    public void FromJson(JsonData dat)
    {
        attributes = DefaultAttributes();
        flags = new List<NPC_Flags>();
        weaponPossibilities = new List<string>();
        bodyParts = EntityList.GetBodyStructure(dat["Body Structure"].ToString());

        name = dat["Name"].ToString();
        ID = dat["ID"].ToString();
        
        faction = GameData.Get<Faction>(dat["Faction"].ToString());
        health = (int)dat["Stats"]["Health"];
        stamina = (int)dat["Stats"]["Stamina"];
        maxItems = (dat.ContainsKey("MaxItems")) ? (int)dat["MaxItems"] : 0;
        maxItemRarity = (dat.ContainsKey("MaxItemRarity")) ? (int)dat["MaxItemRarity"] : 0;

        if (dat.ContainsKey("Sprite"))
        {
            string s = dat["Sprite"].ToString();

            if (s.Contains("|"))
            {
                string[] ids = s.Split('|');
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

        dat.TryGetInt("Weapon Skill", out weaponSkill);
        dat.TryGetString("Firearm", out firearm);
        dat.TryGetString("Corpse_Item", out Corpse_Item);
        dat.TryGetString("Quest", out quest);
        dat.TryGetString("Dialogue", out dialogue);

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

    public IEnumerable<string> LoadErrors()
    {
        yield break;
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

    double Difficulty
    {
        get
        {
            double difficulty = health / 2.0;
            difficulty += attributes["Strength"] * 4;
            difficulty += attributes["Dexterity"];
            difficulty += attributes["Defense"] * 2;
            difficulty += attributes["Intelligence"];

            difficulty += skills.Length;
            difficulty += weaponSkill * 2;

            return Math.Round(difficulty, 3);
        }
    }

    class DifficultyLevel
    {
        public double difficulty;
        readonly string npcName;

        public DifficultyLevel(double diff, string npc)
        {
            difficulty = diff;
            npcName = npc;
        }

        public override string ToString()
        {
            return npcName + " : " + difficulty.ToString();
        }
    }

    public static void PrintNPCDifficultyLevels()
    {
        Felony fel = GameData.Get<Felony>(0);
        double playerStart = 2.0;

        playerStart += fel.HP / 2.0;
        playerStart += fel.STR * 4;
        playerStart += fel.DEX;
        playerStart += fel.INT;

        List<DifficultyLevel> dvals = new List<DifficultyLevel>();
        double lowest = 1000;

        foreach (NPC_Blueprint bp in GameData.GetAll<NPC_Blueprint>())
        {
            double diff = bp.Difficulty / playerStart * 10f;

            if (diff < lowest)
            {
                lowest = diff;
            }

            dvals.Add(new DifficultyLevel(diff, bp.name));
        }

        dvals.Sort((x, y) => { return x.difficulty.CompareTo(y.difficulty); });

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Difficulty Rankings of NPCs:\n");

        foreach (DifficultyLevel d in dvals)
        {
            d.difficulty = Math.Round(d.difficulty - lowest, 1);
            sb.AppendLine(d.ToString());
        }

        System.IO.File.WriteAllText(UnityEngine.Application.streamingAssetsPath + "/Difficulty.txt", sb.ToString());
    }
}

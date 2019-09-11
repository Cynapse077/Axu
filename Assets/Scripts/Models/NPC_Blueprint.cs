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

    public void FromJson(JsonData dat)
    {
        attributes = DefaultAttributes();
        flags = new List<NPC_Flags>();
        weaponPossibilities = new List<string>();
        bodyParts = EntityList.GetBodyStructure(dat["Body Structure"].ToString());

        name = dat["Name"].ToString();
        ID = dat["ID"].ToString();
        
        faction = GameData.Get<Faction>(dat["Faction"].ToString()) as Faction;
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

    float Difficulty
    {
        get
        {
            float difficulty = health / 5;
            difficulty += attributes["Strength"] * 4;
            difficulty += attributes["Dexterity"];
            difficulty += attributes["Defense"] * 2;
            difficulty += attributes["Intelligence"];

            difficulty += skills.Length;
            difficulty += weaponSkill * 2;

            return (float)System.Math.Round(difficulty, 3);
        }
    }

    class DifficultyLevel
    {
        public float difficulty;
        public readonly string npcName;

        public DifficultyLevel(float diff, string npc)
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
        Felony fel = GameData.Get<Felony>(0) as Felony;
        float playerStart = 2;
        playerStart += fel.HP / 10;
        playerStart += fel.STR * 4;
        playerStart += fel.DEX;
        playerStart += fel.INT;

        List<DifficultyLevel> dvals = new List<DifficultyLevel>();
        float lowest = 1000;

        foreach (NPC_Blueprint bp in GameData.GetAll<NPC_Blueprint>())
        {
            float diff = bp.Difficulty / playerStart * 10f;

            if (diff < lowest)
            {
                lowest = diff;
            }

            dvals.Add(new DifficultyLevel(diff, bp.name));
        }

        dvals.Sort((x, y) => { return x.difficulty.CompareTo(y.difficulty); });

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Difficulty Rankings of NPCs:\n");

        foreach (DifficultyLevel d in dvals)
        {
            d.difficulty = (float)System.Math.Round(d.difficulty - lowest, 1);
            sb.AppendLine(d.ToString());
        }

        System.IO.File.WriteAllText(UnityEngine.Application.streamingAssetsPath + "/Difficulty.txt", sb.ToString());
    }
}

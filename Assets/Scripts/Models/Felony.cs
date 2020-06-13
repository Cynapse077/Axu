using System.Collections.Generic;
using LitJson;

[System.Serializable]
public class Felony : IAsset
{
	public string name;
	public string ID { get; set; }
    public string ModID { get; set; }
	public string description;

	public int HP, ST;
	public int STR, DEX, INT, END;

    public string weapon;
    public string firearm;
	public string[] traits;
	public int[] proficiencies;
	public List<SSkill> skills;
	public List<StringInt> items;
    public List<ProgressionLevel> progression;
	public int startingMoney;
    public string bodyStructure;
    public int healthPerLevel;
    public int staminaPerLevel;
    public string baseBodyTexture;

    public Felony(JsonData dat)
    {
        FromJson(dat);
    }

    public void FromJson(JsonData dat)
    {
        name = dat["Name"].ToString();
        ID = dat["ID"].ToString();
        dat.TryGetString("Description", out description);
        dat.TryGetString("Body Structure", out bodyStructure, "Humanoid");
        dat.TryGetString("BodyTexturePath", out baseBodyTexture, "Mods/Core/Art/Player/Bases/char-baseBody.png");

        dat["Stats"].TryGetInt("HP Bonus", out HP);
        dat["Stats"].TryGetInt("ST Bonus", out ST);
        dat["Stats"].TryGetInt("Strength", out STR);
        dat["Stats"].TryGetInt("Dexterity", out DEX);
        dat["Stats"].TryGetInt("Intelligence", out INT);
        dat["Stats"].TryGetInt("Endurance", out END);

        traits = new string[dat["Traits"].Count];
        if (dat["Traits"].Count > 0)
        {
            for (int t = 0; t < dat["Traits"].Count; t++)
            {
                string trait = dat["Traits"][t].ToString();

                if (!trait.NullOrEmpty())
                {
                    traits[t] = trait;
                }
            }
        }

        dat.TryGetInt("Money", out startingMoney);
        dat.TryGetString("Weapon", out weapon);
        dat.TryGetString("Firearm", out firearm);

        items = new List<StringInt>();
        if (dat.ContainsKey("Items"))
        {
            for (int j = 0; j < dat["Items"].Count; j++)
            {
                items.Add(new StringInt(dat["Items"][j]["Item"].ToString(), (int)dat["Items"][j]["Amount"]));
            }
        }

        proficiencies = new int[12];

        SetProf(dat, "Blade", 0);
        SetProf(dat, "Blunt", 1);
        SetProf(dat, "Polearm", 2);
        SetProf(dat, "Axe", 3);
        SetProf(dat, "Firearm", 4);
        SetProf(dat, "Unarmed", 5);
        SetProf(dat, "Misc", 6);
        SetProf(dat, "Throwing", 7);
        SetProf(dat, "Armor", 8);
        SetProf(dat, "Shield", 9);
        SetProf(dat, "Butchery", 10);
        SetProf(dat, "Martial Arts", 11);

        skills = new List<SSkill>();
        if (dat.ContainsKey("Skills"))
        {
            for (int s = 0; s < dat["Skills"].Count; s++)
            {
                string skillName = dat["Skills"][s].ToString();
                skills.Add(new SSkill(skillName, 1, 0, 0, new List<Ability.AbilityOrigin>() { Ability.AbilityOrigin.Natrual }));
            }
        }

        progression = new List<ProgressionLevel>();
        if (dat.ContainsKey("Progression"))
        {
            for (int i = 0; i < dat["Progression"].Count; i++)
            {
                progression.Add(new ProgressionLevel(dat["Progression"][i]));
            }
        }

        dat.TryGetInt("HealthPerLevel", out healthPerLevel);
        dat.TryGetInt("StaminaPerLevel", out staminaPerLevel);
    }

    void SetProf(JsonData dat, string name, int id)
    {
        if (dat.ContainsKey("Proficiencies") && dat["Proficiencies"].ContainsKey(name))
        {
            proficiencies[id] = (int)dat["Proficiencies"][name];
        }
    }

    public ProgressionLevel GetLevelChanges(int level)
    {
        for (int i = 0; i < progression.Count; i++)
        {
            if (progression[i].level == level)
            {
                return progression[i];
            }
        }

        return null;
    }

    public static Felony PlayerFelony()
    {
        return GameData.Get<Felony>(Manager.profName);
    }

    public IEnumerable<string> LoadErrors()
    {
        if (name.NullOrEmpty())
        {
            yield return "Name is null or empty.";
        }

        if (description.NullOrEmpty())
        {
            yield return "Description is not set.";
        }

        if (bodyStructure.NullOrEmpty())
        {
            yield return "No body structure set.";
        }
    }
}

public class ProgressionLevel
{
    public readonly int level;
    public readonly List<string> abilities = new List<string>();
    public readonly List<string> traits = new List<string>();
    public readonly List<Stat_Modifier> stats = new List<Stat_Modifier>();

    public ProgressionLevel(JsonData dat)
    {
        level = (int)dat["Level"];

        if (dat.ContainsKey("Abilities"))
        {
            for (int i = 0; i < dat["Abilities"].Count; i++)
            {
                abilities.Add(dat["Abilities"][i].ToString());
            }
        }

        if (dat.ContainsKey("Traits"))
        {
            for (int i = 0; i < dat["Traits"].Count; i++)
            {
                traits.Add(dat["Traits"][i].ToString());
            }
        }

        if (dat.ContainsKey("Stats"))
        {
            for (int i = 0; i < dat["Stats"].Count; i++)
            {
                stats.Add(new Stat_Modifier(dat["Stats"][i]["Stat"].ToString(), (int)dat["Stats"][i]["Amount"]));
            }
        }
    }
}

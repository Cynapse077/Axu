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
	public int startingMoney;
    public string bodyStructure;

    public Felony(JsonData dat)
    {
        FromJson(dat);
    }

    public void FromJson(JsonData dat)
    {
        name = dat["Name"].ToString();
        ID = dat["ID"].ToString();
        description = dat["Description"].ToString();
        bodyStructure = dat["Body Structure"].ToString();

        HP = (int)dat["Stats"]["HP Bonus"];
        ST = (int)dat["Stats"]["ST Bonus"];

        STR = (int)dat["Stats"]["Strength"];
        DEX = (int)dat["Stats"]["Dexterity"];
        INT = (int)dat["Stats"]["Intelligence"];
        END = (int)dat["Stats"]["Endurance"];
        startingMoney = (int)dat["Money"];

        traits = new string[dat["Traits"].Count];
        items = new List<StringInt>();
        proficiencies = new int[10];
        skills = new List<SSkill>();

        if (dat["Traits"].Count > 0)
        {
            for (int t = 0; t < dat["Traits"].Count; t++)
            {
                string trait = dat["Traits"][t].ToString();

                if (!string.IsNullOrEmpty(trait))
                {
                    traits[t] = trait;
                }
            }
        }

        if (dat.ContainsKey("Weapon"))
        {
            weapon = dat["Weapon"].ToString();
        }

        if (dat.ContainsKey("Firearm"))
        {
            firearm = dat["Firearm"].ToString();
        }

        if (dat.ContainsKey("Items"))
        {
            for (int j = 0; j < dat["Items"].Count; j++)
            {
                items.Add(new StringInt(dat["Items"][j]["Item"].ToString(), (int)dat["Items"][j]["Amount"]));
            }
        }

        for (int j = 0; j < proficiencies.Length; j++)
        {
            proficiencies[j] = 0;
        }

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

        for (int s = 0; s < dat["Skills"].Count; s++)
        {
            string skillName = dat["Skills"][s].ToString();
            skills.Add(new SSkill(skillName, 1, 0, 0, 0));
        }
    }

    void SetProf(JsonData dat, string name, int id)
    {
        if (dat["Proficiencies"].ContainsKey(name))
        {
            proficiencies[id] = (int)dat["Proficiencies"][name];
        }
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

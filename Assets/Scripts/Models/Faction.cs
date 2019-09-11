using LitJson;
using System.Collections.Generic;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Faction : IAsset
{
    public string Name { get; protected set; }
    public string ID { get; set; }
    public string ModID { get; set; }
    public List<string> hostileTo = new List<string>();

    public Faction(JsonData dat)
    {
        FromJson(dat);
    }

    public void FromJson(JsonData dat)
    {
        if (dat.ContainsKey("Name"))
            Name = dat["Name"].ToString();
        if (dat.ContainsKey("ID"))
            ID = dat["ID"].ToString();

        if (dat.ContainsKey("Hostile To"))
        {
            hostileTo = new List<string>();
            for (int j = 0; j < dat["Hostile To"].Count; j++)
            {
                hostileTo.Add(dat["Hostile To"][j].ToString());
            }
        }
    }

    public bool HostileToPlayer()
    {
        return (isHostileTo("player"));
    }

    public bool isHostileTo(Faction otherFaction)
    {
        return isHostileTo(otherFaction.ID);
    }

    public bool isHostileTo(string otherFaction)
    {
        if (hostileTo.Contains("all") && otherFaction != ID)
        {
            return true;
        }
        else if (hostileTo.Contains("none"))
        {
            return false;
        }

        return (hostileTo.Contains(otherFaction) || DynamicHostility(otherFaction));
    }

    bool DynamicHostility(string otherFaction)
    {
        if (ObjectManager.player == null || ObjectManager.playerJournal == null)
        {
            return false;
        }

        if (otherFaction == "player" || otherFaction == "followers")
        {
            switch (ID)
            {
                case "ensis":
                    return (ObjectManager.playerJournal.HasFlag(ProgressFlags.Hostile_To_Ensis));
                case "kin":
                    return (ObjectManager.playerJournal.HasFlag(ProgressFlags.Hostile_To_Kin));
                case "magna":
                    return (ObjectManager.playerJournal.HasFlag(ProgressFlags.Hostile_To_Oromir));
            }
        }

        return false;
    }
}
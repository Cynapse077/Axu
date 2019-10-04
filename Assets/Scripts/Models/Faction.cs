using LitJson;
using System.Collections.Generic;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Faction : IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public string name;
    public bool hidden;
    public List<string> hostileTo;

    public Faction(JsonData dat)
    {
        FromJson(dat);
    }

    public void FromJson(JsonData dat)
    {
        if (dat.ContainsKey("ID"))
            ID = dat["ID"].ToString();

        dat.TryGetString("Name", out name);
        dat.TryGetBool("Hidden", out hidden, false);

        hostileTo = new List<string>();

        if (dat.ContainsKey("Hostile To"))
        {
            for (int j = 0; j < dat["Hostile To"].Count; j++)
            {
                hostileTo.Add(dat["Hostile To"][j].ToString());
            }
        }
    }

    public bool HostileToPlayer()
    {
        return isHostileTo("player");
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
        if (hostileTo.Contains("none"))
        {
            return false;
        }

        return hostileTo.Contains(otherFaction) || DynamicHostility(otherFaction);
    }

    bool DynamicHostility(string otherFaction)
    {
        if (ObjectManager.player == null || ObjectManager.playerJournal == null)
        {
            return false;
        }

        if (otherFaction == "player" || otherFaction == "followers")
        {
            return ObjectManager.playerJournal.HasFlag("HostileTo_" + ID);
        }

        return false;
    }

    public IEnumerable<string> LoadErrors()
    {
        if (name.NullOrEmpty())
        {
            yield return "Name not set.";
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Faction
{
    public string Name { get; protected set; }
    public string ID { get; protected set; }
    public List<string> hostileTo;

    public Faction(string _name, string _id)
    {
        Name = _name;
        ID = _id;
        hostileTo = new List<string>();
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
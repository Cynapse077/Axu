using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Faction {
	public string Name { get; protected set; }
	public string ID { get; protected set; }
	public List<string> hostileTo;

	public Faction(string _name, string _id) { 
		Name = _name;
		ID = _id;
		hostileTo = new List<string>();
	}
		
	public bool HostileToPlayer() {
		return (isHostileTo("player"));
	}

	public bool isHostileTo(Faction otherFaction) {
		return isHostileTo(otherFaction.ID);
	}

	public bool isHostileTo(string otherFaction) {
		if (hostileTo.Contains("all") && otherFaction != this.ID)
			return true;
		if (hostileTo.Contains("none"))
			return false;
		
		return (hostileTo.Contains(otherFaction) || DynamicHostility(otherFaction));
	}

	bool DynamicHostility(string otherFaction) {
		if (ObjectManager.player == null || ObjectManager.playerJournal == null)
			return false;
		
		if (otherFaction == "player" || otherFaction == "followers") {
			if (ID == "ensis")
				return (ObjectManager.playerJournal.HasFlag(ProgressFlags.Hostile_To_Ensis));

			if (ID == "kin")
				return (ObjectManager.playerJournal.HasFlag(ProgressFlags.Hostile_To_Kin));

			if (ID == "magna")
				return (ObjectManager.playerJournal.HasFlag(ProgressFlags.Hostile_To_Oromir));
		}

		return false;
	}
}
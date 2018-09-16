using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPC_Blueprint {

	public string name = "", id = "";
	public List<string> spriteIDs;
	public Faction faction;
	public int health, stamina;
	public Dictionary<string, int> attributes;
	public int heatResist, coldResist;
	public string quest = "";
	public int maxItems, maxItemRarity;
	public List<string> weaponPossibilities;
	public string firearm;
    public List<KeyValuePair<string, Coord>> inventory;
	public List<KeyValuePair<string, int>> skills;
	public List<BodyPart> bodyParts;
	public List<NPC_Flags> flags;
	public string Corpse_Item;
	public Coord localPosition;
	public int elevation;
	public string zone;

	public NPC_Blueprint() {
		weaponPossibilities = new List<string>();
		bodyParts = new List<BodyPart>();
		flags = new List<NPC_Flags>();
        inventory = new List<KeyValuePair<string, Coord>>();
		skills = new List<KeyValuePair<string, int>>();
		spriteIDs = new List<string>();
		attributes = new Dictionary<string, int>() {
			{ "Strength", 0 }, { "Dexterity", 0 }, { "Intelligence", 0 }, { "Endurance", 0 },
			{ "Speed", 0 }, { "Perception", 0 }, { "Accuracy", 0 }, { "Defense", 0 },
			{ "Heat Resist", 0 }, { "Cold Resist", 0 }, { "Energy Resist", 0 }, { "Attack Delay", 0 },
			{ "HP Regen", 0 }, { "ST Regen", 0 }
		};
	}
}

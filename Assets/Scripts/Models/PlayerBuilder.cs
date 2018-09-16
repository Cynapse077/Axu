using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.IO;

public class PlayerBuilder {

	public int hp, maxHP;
	public int st, maxST;
	public int radiation, hunger;
	public PlayerProficiencies proficiencies;
	public List<Trait> traits;
	public List<Skill> skills;
	public Dictionary<string, int> statusEffects;
	public Dictionary<string, int> attributes;
	public List<Addiction> addictions;
	public XPLevel level;

	public List<BodyPart> bodyParts;
	public List<Item> items;
	public List<Item> handItems;
	public Item firearm;
	public int money;
	public string baseWeapon = "fists";

	public List<Quest> quests;
	public List<ProgressFlags> progressFlags;

	public PlayerBuilder() {
		Manager.playerBuilder = this;

		proficiencies = new PlayerProficiencies();
		traits = new List<Trait>();
		skills = new List<Skill>();
		statusEffects = new Dictionary<string, int>();
		addictions = new List<Addiction>();

		attributes = new Dictionary<string, int>() {
			{ "Strength", 5 }, { "Dexterity", 5 }, { "Intelligence", 5 }, { "Endurance", 5 },
			{ "Speed", 10 }, { "Accuracy", 1 }, { "Defense", 1 }, { "Heat Resist", 0 },
			{ "Cold Resist", 0 }, { "Energy Resist", 0 }, { "Attack Delay", 0 },
			{ "HP Regen", 0 }, { "ST Regen", 0 }
		};

		level = new XPLevel(null, 1, 0, 100);
		bodyParts = new List<BodyPart>();
		items = new List<Item>();
		handItems = new List<Item>();
		quests = new List<Quest>();
		progressFlags = new List<ProgressFlags>();
	}
}

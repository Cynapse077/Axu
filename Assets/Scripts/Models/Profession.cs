using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct Profession {

	public string name;
	public string ID;
	public string description;

	public int HP;
	public int ST;
	public int strength;
	public int dexterity;
	public int intelligence;
	public int endurance;

	public string[] traits;
	public int[] proficiencies;
	public List<SSkill> skills;
	public List<StringInt> items;
	public int startingMoney;
    public string bodyStructure;

}

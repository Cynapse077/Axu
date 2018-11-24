using System.Collections.Generic;

[System.Serializable]
public struct Profession {

	public string name;
	public string ID;
	public string description;

	public int HP, ST;
	public int STR, DEX, INT, END;

	public string[] traits;
	public int[] proficiencies;
	public List<SSkill> skills;
	public List<StringInt> items;
	public int startingMoney;
    public string bodyStructure;
}

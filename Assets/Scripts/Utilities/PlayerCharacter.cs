using System.Collections.Generic;

[System.Serializable]
public class PlayerCharacter : Character {

	public string ProfName;
	public int WorldSeed;
    public XPLevel xpLevel;
    public int Charisma, Hunger, Gold;

	public SStats Stats;
	public List<STrait> Traits;
	public List<SSkill> Skills;
    public List<WeaponProficiency> WepProf;
	public List<SBodyPart> BodyParts;
	public List<SItem> Inv;
	public SItem F;
	public string BW;
	public List<SQuest> Quests;
	public List<ProgressFlags> Flags;
    public Weather CWeather;
	public int HumEat;

	public PlayerCharacter(int worldSeed, string playerName, string profName, XPLevel _xpLevel, SStats _stats, Coord worldPos, Coord localPos, int elevation, List<STrait> traits,
		List<WeaponProficiency> profs, List<SBodyPart> bodyParts, int gold, List<SItem> items, List<SItem> handItems, SItem firearm, List<SSkill> skills, int charisma, int hunger, List<SQuest> qs,
		Weather weather, List<ProgressFlags> proflags, string baseWeapon, int humanEaten) {

		WorldSeed = worldSeed;
        Name = playerName;
		ProfName = profName;
        xpLevel = _xpLevel;
        Stats = _stats;
        Hunger = hunger;

		WP = new int[3] { worldPos.x, worldPos.y, elevation };
		LP = new int[2] { localPos.x, localPos.y };

		Traits = traits;
		Skills = skills;
        WepProf = profs;
        BodyParts = bodyParts;
        Gold = gold;
        Inv = items;
		HIt = handItems;
		F = firearm;
        Charisma = charisma;
        CWeather = weather;
		Quests = qs;
		Flags = proflags;
		BW = baseWeapon;
		HumEat = humanEaten;
    }
}

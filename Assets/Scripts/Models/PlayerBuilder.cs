using System.Collections.Generic;

public class PlayerBuilder
{
    public int hp, maxHP;
    public int st, maxST;
    public int radiation;
    public PlayerProficiencies proficiencies;
    public List<Trait> traits;
    public List<Ability> abilities;
    public Dictionary<string, int> statusEffects;
    public Dictionary<string, int> attributes;
    public List<Addiction> addictions;
    public XPLevel level;

    public List<BodyPart> bodyParts;
    public List<Item> items;
    public List<Item> handItems;
    public Item firearm;
    public int money;

    public List<Quest> quests;
    public List<string> completedQuests;
    public List<string> killedStaticNPCs;
    public List<ProgressFlags> progressFlags;
     
    public PlayerBuilder()
    {
        proficiencies = new PlayerProficiencies();
        traits = new List<Trait>();
        abilities = new List<Ability>();
        statusEffects = new Dictionary<string, int>();
        killedStaticNPCs = new List<string>();
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
        completedQuests = new List<string>();
        progressFlags = new List<ProgressFlags>();
    }
}

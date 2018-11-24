using System.Collections.Generic;

[System.Serializable]
public class NPCCharacter : Character
{
    public string ID, UID, Fac, Spr, QN;
    public List<NPC_Flags> Flags = new List<NPC_Flags>();
    public List<SItem> Inv;
    public SItem F;
    public bool Host;

    public NPCCharacter(string _name, string id, string uid, Coord worldPos, Coord localPos, int elevation,
        List<SItem> items, List<SItem> handItems, SItem firearm, bool hostile, string sprite, string faction, List<NPC_Flags> flags,
        string questName)
    {

        Name = _name;
        ID = id;
        UID = uid;
        Fac = faction;

        Coord wp = worldPos ?? new Coord(0, 0);

        WP = new int[3] { wp.x, wp.y, elevation };
        LP = new int[2] { localPos.x, localPos.y };

        HIt = handItems;
        F = firearm;
        Inv = items;
        Flags = flags;

        Host = hostile;
        Spr = sprite;
        QN = questName;
    }
}

using System.Collections.Generic;

[System.Serializable]
public class Character
{

    public string Name;
    public int[] WP;
    public int[] LP;
    public List<SItem> HIt;

    public Character() { }

    public Character(string myName, Coord worldPos, Coord localPos, int elevation, List<SItem> handItems)
    {
        Name = myName;
        HIt = handItems;
        WP = new int[3] { worldPos.x, worldPos.y, elevation };
        LP = new int[2] { localPos.x, localPos.y };
    }
}

using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class Tile_Data
{
    public int ID;
    public WorldMap.Biome biome;
    List<string> tags;
    public int costToEnter;

    public Tile_Data(int _id, WorldMap.Biome _biome, List<string> _tags, int _cost)
    {
        ID = _id;
        tags = _tags;
        biome = _biome;
        costToEnter = _cost;
    }

    public bool HasTag(string tag)
    {
        return tags.Contains(tag);
    }

    public bool CanDig(bool explosion)
    {
        return (!explosion) ? HasTag("Breakable") : HasTag("Breakable_Explosion");
    }
}

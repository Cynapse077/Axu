using System.Collections.Generic;
using System.IO;
using LitJson;
using System.Linq;

[MoonSharp.Interpreter.MoonSharpUserData]
public class Tile_Data
{
    public int ID;
    public WorldMap.Biome biome;
    List<string> tags;

    public Tile_Data(int _id, WorldMap.Biome _biome, List<string> _tags)
    {
        ID = _id;
        tags = _tags;
        biome = _biome;
    }

    public bool HasTag(string tag)
    {
        if (tags == null)
            UnityEngine.Debug.LogError("No tags on Tile_Data!");

        return tags.Contains(tag);
    }
}

[MoonSharp.Interpreter.MoonSharpUserData]
public static class Tile
{
    public static Dictionary<string, Tile_Data> tiles;

    public static string GetKey(int value)
    {
        foreach (KeyValuePair<string, Tile_Data> entry in tiles)
        {
            if (entry.Value.ID == value)
                return entry.Key;
        }

        return "";
    }

    public static void InitializeTileDictionary()
    {
        if (tiles != null)
            return;

        tiles = new Dictionary<string, Tile_Data>();

        string jsonString = File.ReadAllText(UnityEngine.Application.streamingAssetsPath + "/Data/Maps/LocalTiles.json");
        JsonData dat = JsonMapper.ToObject(jsonString);

        for (int i = 0; i < dat.Count; i++)
        {
            string ID = dat[i]["ID"].ToString();
            int tIndex = (int)dat[i]["TileIndex"];
            List<string> tags = new List<string>();

            if (dat[i].ContainsKey("Tags"))
            {
                for (int j = 0; j < dat[i]["Tags"].Count; j++)
                {
                    tags.Add(dat[i]["Tags"][j].ToString());
                }
            }

            string b = dat[i]["Biome"].ToString();
            WorldMap.Biome biome = b == "None" ? WorldMap.Biome.Default : b.ToEnum<WorldMap.Biome>();

            tiles.Add(ID, new Tile_Data(tIndex, biome, tags));
        }
    }

    public static Tile_Data GetByID(int id)
    {
        return tiles.SingleOrDefault(x => x.Value.ID == id).Value;
    }

    public static Tile_Data GetByName(string id)
    {
        return tiles[id];
    }

    public static bool IsInTag(int id, string tag)
    {
        return (tiles.SingleOrDefault(x => x.Value.ID == id).Value.HasTag(tag));
    }

    public static Tile_Data DesertTile()
    {
        int ranNum = GetRandomNumber(100);

        if (ranNum == 99)
            return tiles["Desert_Cactus"];

        if (ranNum < 3)
            return tiles["Desert_Vines"];
        else if (ranNum < 5)
            return CoinFlip() ? tiles["Desert_Sand_3"] : tiles["Desert_Sand_4"];
        else if (ranNum < 6)
            return tiles["Desert_Sand_5"];

        return CoinFlip() ? tiles["Desert_Sand_1"] : tiles["Desert_Sand_2"];
    }

    public static bool isMountain(int tileID)
    {
        return (tileID == tiles["Mountain"].ID || tileID == tiles["Volcano_Wall"].ID);
    }

    public static Tile_Data SwampTile()
    {
        int ranNum = GetRandomNumber(55);

        if (ranNum < 1)
        {
            return tiles["Swamp_Frond"];
        }
        else if (ranNum < 10)
        {
            return tiles["Swamp_Grass_2"];
        }
        else if (ranNum < 20)
        {
            return tiles["Swamp_Grass_3"];
        }
        else if (ranNum < 21)
        {
            return tiles["Swamp_Frond_2"];
        }
        else if (ranNum < 24)
        {
            return tiles["Swamp_Flower"];
        }
        else
        {
            return tiles["Swamp_Grass_1"];
        }
    }

    public static Tile_Data TundraTile()
    {
        int ranNum = GetRandomNumber(100);

        if (ranNum > 1)
        {
            if (ranNum == 2)
                return tiles["Snow_2"];

            return (GetRandomNumber(100) < 96) ? tiles["Snow_1"] : tiles["Snow_3"];
        }
        else
            return CoinFlip() ? tiles["Snow_Tree"] : tiles["Snow_Grass"];

    }

    public static Tile_Data ForestTile()
    {
        int ranNum = GetRandomNumber(100);

        if (ranNum < 32)
            return tiles["Forest_Grass_1"];
        else if (ranNum < 48)
            return tiles["Forest_Grass_2"];
        else if (ranNum < 53)
            return tiles["Forest_Grass_3"];
        else if (ranNum < 70)
            return tiles["Forest_Grass_4"];
        else if (ranNum < 83)
            return tiles["Forest_Grass_5"];
        else
            return tiles["Forest_Grass_6"];
    }

    //Chooses whether or not to place a tree-like object. (0 = Plains, 1 = Forest, 2+ = Desert)
    public static Tile_Data TreeOrNoTree(int biomeNum)
    {
        if (biomeNum == 0)
            return (SeedManager.localRandom.Next(100) < 98) ? tiles["Plains_Grass_1"] : tiles["Plains_Tree_1"];
        else if (biomeNum == 1)
            return (SeedManager.localRandom.Next(100) < 93) ? ForestTile() : tiles["Forest_Tree"];
        else
            return (SeedManager.localRandom.Next(100) < 98) ? DesertTile() : tiles["Desert_Cactus"];
    }

    public static bool CanDig(Tile_Data tile, bool explosion)
    {
        return (!explosion) ? tile.HasTag("Breakable") : tile.HasTag("Breakable_Explosion");
    }

    public static bool isWaterTile(int tileNum, bool includeAllLiquids)
    {
        if (tileNum == tiles["Water"].ID)
            return true;

        return (includeAllLiquids && tileNum == tiles["Water_Swamp"].ID);
    }

    static int GetRandomNumber(int max)
    {
        return (SeedManager.localRandom == null) ? UnityEngine.Random.Range(0, max) : SeedManager.localRandom.Next(max);
    }
    static bool CoinFlip()
    {
        return (SeedManager.localRandom == null) ? UnityEngine.Random.Range(0, 100) < 50 : SeedManager.localRandom.Next(100) < 50;
    }
}

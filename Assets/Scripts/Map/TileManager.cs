using System.Collections.Generic;
using System.IO;
using LitJson;
using System.Linq;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class TileManager
{
    public static Dictionary<string, Tile_Data> tiles;

    public static string GetKey(int value)
    {
        foreach (KeyValuePair<string, Tile_Data> entry in tiles)
        {
            if (entry.Value.ID == value)
            {
                return entry.Key;
            }
        }

        return string.Empty;
    }

    public static void LoadTiles(string path)
    {
        if (tiles == null)
        {
            tiles = new Dictionary<string, Tile_Data>();
        }

        string jsonString = File.ReadAllText(path);
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

            dat[i].TryGetInt("Path Cost", out int cost, 0);
            dat[i].TryGetString("Biome", out string b, "None");

            Biome biome = b == "None" ? Biome.Default : b.ToEnum<Biome>();

            TileAtlas atlas = null;
            if (dat[i].ContainsKey("Atlas"))
            {
                string atlasPath = dat[i]["Atlas"]["Path"].ToString();
                int columns = (int)dat[i]["Atlas"]["Columns"];

                atlas = new TileAtlas(atlasPath, columns);
            }

            Tile_Data newTile = new Tile_Data(tIndex, biome, tags, cost, atlas);

            if (tiles.ContainsKey(ID))
            {
                tiles[ID] = newTile;
            }
            else
            {
                tiles.Add(ID, newTile);
            }
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

    public static bool isMountain(int tileID)
    {
        return (tileID == tiles["Mountain"].ID || tileID == tiles["Volcano_Wall"].ID || tileID == tiles["Ice_Wall"].ID);
    }

    public static bool isWaterTile(int tileNum, bool includeAllLiquids)
    {
        if (tileNum == tiles["Water"].ID)
        {
            return true;
        }

        return (includeAllLiquids && tileNum == tiles["Water_Swamp"].ID);
    }
}

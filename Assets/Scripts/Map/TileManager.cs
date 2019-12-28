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
        return tiles.FirstOrDefault(x => x.Value.ID == id).Value;
    }

    public static Tile_Data GetByName(string id)
    {
        if (tiles.ContainsKey(id))
        {
            return tiles[id];
        }

        return null;
    }

    public static bool isMountain(int tileID)
    {
        return GetByID(tileID).HasTag("Mountain_Wall");
    }

    public static bool isWaterTile(int tileNum, bool includeAllLiquids = true)
    {
        if (IsTile(tileNum, "Water"))
        {
            return true;
        }

        return includeAllLiquids && GetByID(tileNum).HasTag("Liquid");
    }

    public static bool IsTile(int tileNum, string key)
    {
        return tiles.ContainsKey(key) && tileNum == tiles[key].ID;
    }
}

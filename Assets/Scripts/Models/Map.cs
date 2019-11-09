using System.Collections.Generic;
using LitJson;

public class Map : IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public string name;
    public string locationID;
    public int[] tiles;
    public List<KeyValuePair<string, Coord>> objects;
    public List<KeyValuePair<string, Coord>> npcs;
    public Coord size;
    public int elevation;

    public Map(JsonData dat)
    {
        FromJson(dat);
    }

    public Map(Map other)
    {
        ID = other.ID;
        ModID = other.ModID;
        name = other.name;
        locationID = other.locationID;
        tiles = other.tiles;
        objects = other.objects;
        npcs = other.npcs;
        size = other.size;
        elevation = other.elevation;
    }

    public void FromJson(JsonData dat)
    {
        dat.TryGetString("Name", out name);
        ID = name;
        dat.TryGetString("locationID", out locationID);
        dat.TryGetInt("elev", out elevation);
        size = new Coord((int)dat["width"], (int)dat["height"]);

        tiles = new int[dat["IDs"].Count];
        for (int i = 0; i < dat["IDs"].Count; i++)
        {
            tiles[i] = (int)dat["IDs"][i];
        }

        objects = new List<KeyValuePair<string, Coord>>();
        for (int i = 0; i < dat["objects"].Count; i++)
        {
            JsonData ob = dat["objects"][i];
            objects.Add(new KeyValuePair<string, Coord>(ob["Name"].ToString(), new Coord((int)ob["Pos"]["x"], (int)ob["Pos"]["y"])));
        }

        npcs = new List<KeyValuePair<string, Coord>>();
        if (dat.ContainsKey("npcs"))
        {
            for (int i = 0; i < dat["npcs"].Count; i++)
            {
                JsonData npc = dat["npcs"][i];
                npcs.Add(new KeyValuePair<string, Coord>(npc["Name"].ToString(), new Coord((int)npc["Pos"]["x"], (int)npc["Pos"]["y"])));
            }
        }        
    }

    public IEnumerable<string> LoadErrors()
    {
        yield break;
    }
}

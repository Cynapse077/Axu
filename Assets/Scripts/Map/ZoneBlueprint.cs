using System.Collections.Generic;
using LitJson;

public class Zone_Blueprint : IAsset
{
    public const string DefaultPlacementBiome = "Any Land";

    public string ID { get; set; }
    public string ModID { get; set; }
    public string name, underground;
    public int tileID, amount, radiation;
    public bool walkable, expand, isStart, friendly;
    public Placement placement;
    public Zone_Blueprint parent;
    public Zone_Blueprint[] neighbors;

    public Zone_Blueprint(JsonData dat)
    {
        placement = new Placement();
        FromJson(dat);
    }

    public void FromJson(JsonData dat)
    {
        if (dat.ContainsKey("ID"))
            ID = dat["ID"].ToString();

        dat.TryGetString("Display", out name, name);
        dat.TryGetInt("Tile Index", out tileID, tileID);
        dat.TryGetBool("Walkable", out walkable, true);
        dat.TryGetInt("Amount", out amount, 1);
        dat.TryGetBool("Expand", out expand, false);
        dat.TryGetBool("Start Location", out isStart, isStart);
        dat.TryGetBool("Friendly", out friendly, friendly);
        dat.TryGetString("Underground", out underground, underground);
        dat.TryGetInt("Radiation", out radiation, radiation);

        if (dat.ContainsKey("Place At"))
        {
            dat["Place At"].TryGetString("Biome", out placement.zoneID, DefaultPlacementBiome);

            if (dat["Place At"].ContainsKey("Location"))
                placement.landmark = dat["Place At"]["Location"].ToString();
            if (dat["Place At"].ContainsKey("Relative"))
                placement.relativePosition = new Coord((int)dat["Place At"]["Relative"][0], (int)dat["Place At"]["Relative"][1]);
            if (dat["Place At"].ContainsKey("Location"))
                placement.landmark = dat["Place At"]["Location"].ToString();

            if (dat["Place At"].ContainsKey("Mainland"))
            {
                placement.onMain = (bool)dat["Place At"]["Mainland"];
            }
        }

        if (dat.ContainsKey("Neighbors"))
        {
            neighbors = new Zone_Blueprint[dat["Neighbors"].Count];

            for (int i = 0; i < dat["Neighbors"].Count; i++)
            {
                neighbors[i] = new Zone_Blueprint(dat["Neighbors"][i])
                {
                    parent = this
                };
            }
        }
    }

    public IEnumerable<string> LoadErrors()
    {
        yield break;
    }

    public class Placement
    {
        public string zoneID;
        public Coord relativePosition;
        public string landmark;
        public bool onMain;
    }
}

public class Vault_Blueprint :IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public string name;
    public int depth, light;
    public Rules rules;
    public TileInfo tileInfo;
    public int[] excludeSpawnsOn;

    public Vault_Blueprint(JsonData dat)
    {
        rules = Rules.Empty();
        FromJson(dat);
    }

    public void FromJson(JsonData dat)
    {
        if (dat.ContainsKey("ID"))
            ID = dat["ID"].ToString();
        if (dat.ContainsKey("Display"))
            name = dat["Display"].ToString();
        if (dat.ContainsKey("Depth"))
            depth = (int)dat["Depth"];
        if (dat.ContainsKey("Light"))
            light = (int)dat["Light"];

        if (dat.ContainsKey("Rules"))
        {
            rules = Rules.Empty();

            if (dat["Rules"].ContainsKey("Load"))
                rules.loadFromData = (bool)dat["Rules"]["Load"];

            if (dat["Rules"].ContainsKey("Algorithms"))
            {
                rules.algorithms = new string[dat["Rules"]["Algorithms"].Count];

                for (int i = 0; i < dat["Rules"]["Algorithms"].Count; i++)
                {
                    rules.algorithms[i] = dat["Rules"]["Algorithms"][i].ToString();
                }
            }

            if (dat["Rules"].ContainsKey("Feature"))
            {
                string layerID = (dat["Rules"]["Feature"].ContainsKey("ID")) ? dat["Rules"]["Feature"]["ID"].ToString() : "";
                int chance = (dat["Rules"]["Feature"].ContainsKey("Chance")) ? (int)dat["Rules"]["Feature"]["Chance"] : 0;
                rules.layer2 = new StringInt(layerID, chance);
            }
        }

        string wallTile = "";

        if (dat.ContainsKey("Wall Tile"))
            wallTile = dat["Wall Tile"].ToString();

        WeightedTile[] wt = new WeightedTile[0];

        if (dat.ContainsKey("Floor Tiles"))
        {
            wt = new WeightedTile[dat["Floor Tiles"].Count];

            for (int i = 0; i < dat["Floor Tiles"].Count; i++)
            {
                wt[i].tileID = dat["Floor Tiles"][i]["ID"].ToString();
                wt[i].Weight = (int)dat["Floor Tiles"][i]["Weight"];
            }
        }

        if (dat.ContainsKey("Exclude Spawns On"))
        {
            excludeSpawnsOn = new int[dat["Exclude Spawns On"].Count];

            for (int i = 0; i < dat["Exclude Spawns On"].Count; i++)
            {
                excludeSpawnsOn[i] = (int)dat["Exclude Spawns On"][i];
            }
        }

        tileInfo = new TileInfo(wallTile, wt);
    }

    public IEnumerable<string> LoadErrors()
    {
        yield break;
    }

    public struct Rules
    {
        public bool loadFromData;
        public string[] algorithms;
        public StringInt layer2;

        public Rules(bool load, string[] algo, StringInt l2)
        {
            loadFromData = load;
            algorithms = algo;
            layer2 = l2;
        }

        public static Rules Empty()
        {
            return new Rules(false, new string[] { }, new StringInt("", 0));
        }
    }

    public struct TileInfo
    {
        public string wallTile;
        public WeightedTile[] floorTiles;

        public TileInfo(string wall, WeightedTile[] floor)
        {
            wallTile = wall;
            floorTiles = floor;
        }

        public Tile_Data GetRandomTile()
        {
            return TileManager.GetByName(Utility.WeightedChoice(floorTiles).tileID);
        }
    }

    public struct WeightedTile : IWeighted
    {
        public int Weight { get; set; }
        public string tileID;

        public WeightedTile(int weight, string id)
        {
            Weight = weight;
            tileID = id;
        }
    }
}
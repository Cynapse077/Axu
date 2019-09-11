using LitJson;

public class ZoneBlueprint : IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public string name, underground;
    public int tileID, amount, radiation;
    public bool walkable, expand, isStart, friendly;
    public Placement placement;
    public ZoneBlueprint[] neighbors;
    public ZoneBlueprint parent;

    public ZoneBlueprint(JsonData dat)
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
            if (dat["Place At"].ContainsKey("Biome"))
                placement.zoneID = dat["Place At"]["Biome"].ToString();
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
            neighbors = new ZoneBlueprint[dat["Neighbors"].Count];

            for (int i = 0; i < dat["Neighbors"].Count; i++)
            {
                neighbors[i] = new ZoneBlueprint(dat["Neighbors"][i])
                {
                    parent = this
                };
            }
        }
    }

    public class Placement
    {
        public string zoneID;
        public Coord relativePosition;
        public string landmark;
        public bool onMain;
    }
}

public class ZoneBlueprint_Underground :IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public string name;
    public int depth, light;
    public Rules rules;
    public TileInfo tileInfo;

    public ZoneBlueprint_Underground(JsonData dat)
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

        tileInfo = new TileInfo(wallTile, wt);
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
            return Tile.GetByName(Utility.WeightedChoice(floorTiles).tileID);
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
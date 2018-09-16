using LitJson;
using System.Collections.Generic;
using System;

public class ZoneBlueprint {
    public string id, name, underground;
    public int tileID, amount;
    public bool walkable, expand, isStart, friendly;
    public Placement placement;
    public Border border;
    public ZoneBlueprint[] neighbors;
    public ZoneBlueprint parent;

    public ZoneBlueprint(string _id, string _name, int _tileID, bool _walkable, bool _friendly, int _amount, bool _expand, bool _isStart) {
        id = _id;
        name = _name;
        tileID = _tileID;
        walkable = _walkable;
        amount = _amount;
        expand = _expand;
        isStart = _isStart;
        friendly = _friendly;

        border = null;
        placement = null;
        underground = null;
    }

    public static ZoneBlueprint LoadFromJson(JsonData dat) {
        if (dat == null)
            return null;

        int tileID = (int)dat["Tile Index"];
        bool walkable = (bool)dat["Walkable"];
        int amount = (dat.ContainsKey("Amount")) ? (int)dat["Amount"] : 1;
        bool expand = (dat.ContainsKey("Expand")) ? (bool)dat["Expand"] : false;
        bool isStart = (dat.ContainsKey("Start Location")) ? (bool)dat["Start Location"] : false;
        bool isFriendly = (dat.ContainsKey("Friendly")) ? (bool)dat["Friendly"] : false;

        ZoneBlueprint zb = new ZoneBlueprint(dat["ID"].ToString(), dat["Display"].ToString(), tileID, walkable, isFriendly, amount, expand, isStart);

        if (dat.ContainsKey("Place At")) {
            zb.placement = new Placement();

            if (dat["Place At"].ContainsKey("Biome"))
                zb.placement.zoneID = dat["Place At"]["Biome"].ToString();
            if (dat["Place At"].ContainsKey("Location"))
                zb.placement.landmark = dat["Place At"]["Location"].ToString();
            if (dat["Place At"].ContainsKey("Relative"))
                zb.placement.relativePosition = new Coord((int)dat["Place At"]["Relative"][0], (int)dat["Place At"]["Relative"][1]);

            if (dat["Place At"].ContainsKey("Location"))
                zb.placement.landmark = dat["Place At"]["Location"].ToString();

            if (dat["Place At"].ContainsKey("Distance From Start")) {
                zb.placement.distFromStart = (int)dat["Place At"]["Distance From Start"];
            }
        }

        if (dat.ContainsKey("Border")) {
            zb.border = new Border();

            zb.border.width = (int)dat["Border"]["Width"];
            zb.border.tileID = dat["Border"]["Tile ID"].ToString();

            if (dat["Border"].ContainsKey("Exclude")) {
                zb.border.exclude = new Coord[dat["Border"]["Exclude"].Count];

                for (int i = 0; i < dat["Border"]["Exclude"].Count; i++) {
                    zb.border.exclude[i] = new Coord((int)dat["Border"]["Exclude"][i][0], (int)dat["Border"]["Exclude"][i][1]);
                }
            }
        }

        if (dat.ContainsKey("Neighbors")) {
            zb.neighbors = new ZoneBlueprint[dat["Neighbors"].Count];

            for (int i = 0; i < dat["Neighbors"].Count; i++) {
                zb.neighbors[i] = LoadFromJson(dat["Neighbors"][i]);
                zb.neighbors[i].parent = zb;
            }
        }

        if (dat.ContainsKey("Underground"))
            zb.underground = dat["Underground"].ToString();

        if (dat.ContainsKey("Expand"))
            zb.expand = (bool)dat["Expand"];

        return zb;
    }

    public class Placement {
        public string zoneID;
        public Coord relativePosition;
        public string landmark;
        public int distFromStart;
    }

    public class Border {
        public int width;
        public Coord[] exclude;
        public string tileID;
    }
}

public class ZoneBlueprint_Underground {
    public string id, name;
    public int depth, light;
    public Rules rules;
    public TileInfo tileInfo;

    public ZoneBlueprint_Underground(string _id, string _name, int _depth, int _light, Rules _rules, TileInfo tInfo) {
        id = _id;
        name = _name;
        depth = _depth;
        light = _light;
        rules = _rules;
        tileInfo = tInfo;
    }

    public static ZoneBlueprint_Underground LoadFromJson(JsonData dat) {
        if (dat == null)
            return null;

        string _id = dat["ID"].ToString();
        string _name = dat["Display"].ToString();
        int _depth = (int)dat["Depth"];
        int _light = (int)dat["Light"];
        Rules _rules = Rules.Empty();

        if (dat.ContainsKey("Rules")) {
            if (dat["Rules"].ContainsKey("Load"))
                _rules.loadFromData = (bool)dat["Rules"]["Load"];

            if (dat["Rules"].ContainsKey("Algorithms")) {
                _rules.algorithms = new string[dat["Rules"]["Algorithms"].Count];

                for (int i = 0; i < dat["Rules"]["Algorithms"].Count; i++) {
                    _rules.algorithms[i] = dat["Rules"]["Algorithms"][i].ToString();
                }
            }

            if (dat["Rules"].ContainsKey("Feature")) {
                string layerID = (dat["Rules"]["Feature"].ContainsKey("ID")) ? dat["Rules"]["Feature"]["ID"].ToString() : "";
                int chance = (dat["Rules"]["Feature"].ContainsKey("Chance")) ? (int)dat["Rules"]["Feature"]["Chance"] : 0;
                _rules.layer2 = new StringInt(layerID, chance);
            }
        }

        string wallTile = "";

        if (dat.ContainsKey("Wall Tile"))
            wallTile = dat["Wall Tile"].ToString();

        WeightedTile[] wt = new WeightedTile[0];
        
        if (dat.ContainsKey("Floor Tiles")) {
            wt = new WeightedTile[dat["Floor Tiles"].Count];

            for (int i = 0; i < dat["Floor Tiles"].Count; i++) {
                wt[i].tileID = dat["Floor Tiles"][i]["ID"].ToString();
                wt[i].Weight = (int)dat["Floor Tiles"][i]["Weight"];
            }
        }

        TileInfo ti = new TileInfo(wallTile, wt);

        return new ZoneBlueprint_Underground(_id, _name, _depth, _light, _rules, ti);
    }

    public struct Rules {
        public bool loadFromData;
        public string[] algorithms;
        public StringInt layer2;

        public Rules(bool load, string[] algo, StringInt l2) {
            loadFromData = load;
            algorithms = algo;
            layer2 = l2;
        }

        public static Rules Empty() {
            return new Rules(false, new string[] { }, new StringInt("", 0));
        }
    }

    public struct TileInfo {
        public string wallTile;
        public WeightedTile[] floorTiles;

        public TileInfo(string wall, WeightedTile[] floor) {
            wallTile = wall;
            floorTiles = floor;
        }

        public Tile_Data GetRandomTile() {
            return Tile.GetByName(Utility.WeightedChoice<WeightedTile>(floorTiles).tileID);
        }
    }

    public struct WeightedTile : IWeighted {
        public int Weight { get; set; }
        public string tileID;

        public WeightedTile(int weight, string id) {
            Weight = weight;
            tileID = id;
        }
    }
}
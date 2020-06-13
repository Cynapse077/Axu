using UnityEngine;
using LitJson;
using System.IO;
using Pathfinding;
using System.Collections.Generic;

public class TileMap_Data
{
    public Tile_Data[,] map_data;
    public bool[,] has_seen;
    public MapInfo mapInfo;
    public int elevation, lastTurnSeen = 0;
    public bool visited, doneLoading, loadedFromData, dream;
    public List<TileChange> changes;
    public List<House> houses;
    public Vault vault;
    public string mapName = null;

    Path_TileData[,] pathTileData;

    System.Random RNG
    {
        get { return SeedManager.localRandom; }
    }

    int Width
    {
        get { return Manager.localMapSize.x; }
    }

    int Height
    {
        get { return Manager.localMapSize.y; }
    }

    WorldMap_Data WorldData
    {
        get { return World.tileMap.worldMap; }
    }

    /// <summary> 
    /// New outdoors map.
    /// </summary>
	public TileMap_Data(int x, int y, int elev, bool vis)
    {
        mapInfo = World.tileMap.worldMap.GetTileAt(x, y);
        SeedManager.CoordinateSeed(mapInfo.position.x, mapInfo.position.y, elev);
        Init();
        elevation = elev;
        visited = vis;

        InitialTileFill();
    }


    /// <summary>
    /// New Vault map.
    /// </summary>
    public TileMap_Data(int x, int y, int elev, Vault _vault, bool _visited)
    {
        mapInfo = World.tileMap.worldMap.GetTileAt(x, y);
        vault = _vault;
        SeedManager.CoordinateSeed(mapInfo.position.x, mapInfo.position.y, elev);
        Init();

        elevation = elev;
        visited = _visited;
        CreateVaultLevel(vault);
        FinalPass();
    }

    /// <summary>
    /// Load a specific map. Previous map is for layering purposes.
    /// </summary>
    public TileMap_Data(string mapName, bool friendly, TileMap_Data previousMap = null)
    {
        this.mapName = mapName;
        mapInfo = new MapInfo(World.tileMap.WorldPosition, World.tileMap.worldMap.GetTileAt(World.tileMap.worldCoordX, World.tileMap.worldCoordY).biome);
        SeedManager.NPCSeed(mapName);
        Init();
        mapInfo.friendly = friendly;
        visited = true;

        if (previousMap != null)
        {
            for (int x = 0; x < previousMap.Width; x++)
            {
                for (int y = 0; y < previousMap.Height; y++)
                {
                    map_data[x, y] = previousMap.map_data[x, y];
                }
            }
        }
        else
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    map_data[x, y] = TileMap_Generator.TileFromBiome(mapInfo.biome);
                }
            }
        }

        LoadSpecificMap(mapName);
        FinalPass();
    }

    void InitialTileFill()
    {
        map_data = TileMap_Generator.Generate(mapInfo.biome, mapInfo.HasLandmark());

        PlaceAdjacentBiomes();
        CheckPrefabs();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (mapInfo.landmark == "Ruins")
                {
                    if (RNG.Next(100) < 35 && WalkableTile(x, y))
                    {
                        map_data[x, y] = TileManager.tiles["Floor_Brick"];
                    }
                }
            }
        }

        FinalPass();
    }

    void CheckPrefabs()
    {
        //Load custom maps
        if (RNG.Next(1000) <= 2 && LoadCustomMap())
        {
            loadedFromData = true;
        }
        else if (mapInfo.HasLandmark())
        {
            //Load custom maps for relevant landmarks here.
            switch (mapInfo.landmark)
            {
                case "Volcano":
                    Volcano();
                    break;
                case "Ruins":
                    Ruins();
                    Coord stairs = GetRandomFloorTile();
                    map_data[stairs.x, stairs.y] = TileManager.tiles["Stairs_Down"];
                    break;
                case "River":
                    CreateRiver(mapInfo.position.x, mapInfo.position.y);
                    break;
                case "Village":
                    CreateRoad(mapInfo.position.x, mapInfo.position.y);

                    int numTries = 0, numHouses = 0;
                    int maxHouses = RNG.Next(5, 9);

                    while (numTries < 2000 && numHouses < maxHouses)
                    {
                        if (CreateHouse())
                            numHouses++;
                        else
                            numTries++;
                    }

                    if (RNG.Next(100) <= 5 && houses.Count > 0)
                    {
                        House r = houses.GetRandom(RNG);
                        Coord c = r.GetRandomPosition();
                        map_data[c.x, c.y] = TileManager.tiles["Stairs_Down"];
                    }
                    break;
                default:
                    LoadCustomMap();                    
                    break;
            }
        }
        else if (mapInfo.biome != Biome.Ocean && mapInfo.biome != Biome.Mountain)
        {
            int ranNum = RNG.Next(1000);

            if (ranNum <= 5)
            {
                Ruins();
            }
            else if (ranNum <= 70)
            {
                RandomRocks();
            }
        }
    }

    void RandomRocks()
    {
        //Random rock formations.
        List<Coord> points = new List<Coord>();

        for (int i = 0; i < RNG.Next(5, 10); i++)
        {
            points.Add(GetRandomFloorTile());
        }

        for (int x = 4; x < Manager.localMapSize.x - 4; x++)
        {
            for (int y = 4; y < Manager.localMapSize.y - 4; y++)
            {
                int ranNum = RNG.Next(100);

                for (int i = 0; i < points.Count; i++)
                {
                    float dist = Vector2.Distance(points[i].toVector2(), new Vector2(x, y));

                    if (!TileManager.isWaterTile(map_data[x, y].ID) && dist < 5 && (1.0f / dist) * 250 > ranNum)
                    {
                        map_data[x, y] = TileManager.tiles["Mountain"];
                        break;
                    }
                }
            }
        }
    }

    public void ChangeTile(int x, int y, Tile_Data tile)
    {
        if (changes == null)
        {
            changes = new List<TileChange>();
        }

        changes.Add(new TileChange(x, y, tile.ID));
        map_data[x, y] = tile;
        Autotile();
    }

    //Final pass, defining details
    void FinalPass()
    {
        //Doors
        if (!visited)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (map_data[x, y] == TileManager.tiles["Door"] && World.tileMap.GetCellAt(x, y).mapObjects.Empty())
                    {
                        PlaceDoor(x, y);
                    }
                }
            }
        }

        SetUpTileData();
        doneLoading = true;
    }

    void PlaceAdjacentBiomes()
    {
        if (mapInfo.biome == Biome.Ocean || mapInfo.biome == Biome.Mountain)
        {
            return;
        }

        //N
        if (mapInfo.position.y > 0)
        {
            Biome nBiome = WorldData.GetTileAt(mapInfo.position.x, mapInfo.position.y + 1).biome;

            if (MapInfo.BiomeHasEdge(nBiome) && mapInfo.biome != nBiome)
            {
                for (int x = 0; x < Width; x++)
                {
                    int offset = RNG.Next(2, 4);

                    for (int y = Height - 1; y > Height - offset; y--)
                    {
                        map_data[x, y] = TileMap_Generator.TileFromBiome(nBiome);
                    }
                }
            }
        }

        //E
        if (mapInfo.position.x > 0)
        {
            Biome eBiome = WorldData.GetTileAt(mapInfo.position.x + 1, mapInfo.position.y).biome;

            if (MapInfo.BiomeHasEdge(eBiome) && mapInfo.biome != eBiome)
            {
                for (int y = 0; y < Height; y++)
                {
                    int offset = RNG.Next(2, 4);

                    for (int x = Width - 1; x > Width - offset; x--)
                    {
                        map_data[x, y] = TileMap_Generator.TileFromBiome(eBiome);
                    }
                }
            }
        }

        //S
        if (mapInfo.position.y < Manager.worldMapSize.y - 1)
        {
            Biome sBiome = WorldData.GetTileAt(mapInfo.position.x, mapInfo.position.y - 1).biome;

            if (MapInfo.BiomeHasEdge(sBiome) && mapInfo.biome != sBiome)
            {
                for (int x = 0; x < Width; x++)
                {
                    int offset = RNG.Next(2, 4);

                    for (int y = 0; y < offset; y++)
                    {
                        map_data[x, y] = TileMap_Generator.TileFromBiome(sBiome);
                    }
                }
            }
        }

        //W
        if (mapInfo.position.x < Manager.worldMapSize.x - 1)
        {
            Biome wBiome = WorldData.GetTileAt(mapInfo.position.x - 1, mapInfo.position.y).biome;

            if (MapInfo.BiomeHasEdge(wBiome) && mapInfo.biome != wBiome)
            {
                for (int y = 0; y < Height; y++)
                {
                    int offset = RNG.Next(2, 4);

                    for (int x = 0; x < offset; x++)
                    {
                        map_data[x, y] = TileMap_Generator.TileFromBiome(wBiome);
                    }
                }
            }
        }
    }

    public bool IsMagna()
    {
        return mapInfo.HasLandmark() && mapInfo.landmark.Contains("Magna_");
    }

    void PlaceDoor(int x, int y)
    {
        if (!visited && !loadedFromData)
        {
            World.objectManager.NewObjectAtSpecificScreen("Door_Closed", new Coord(x, y), mapInfo.position, -elevation);
        }
    }

    public void Autotile()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Tile_Data tile = map_data[x, y];

                //TODO: Move pews to map objects
                if (tile == TileManager.tiles["Door"] || tile == TileManager.tiles["Stairs_Up"] || tile == TileManager.tiles["Stairs_Down"])
                {
                    continue;
                }

                if (TileManager.isWaterTile(tile.ID))
                {
                    int tIndex = (elevation == 0 && mapInfo.biome != Biome.Mountain) ? 0 : 8;
                    BitwiseAutotile(x, y, tIndex, (z => TileManager.isWaterTile(z)), true);
                }
                else if (tile.HasTag("Wall"))
                {
                    int tIndex = 11;
                    bool eightWay = false;

                    if (tile.HasTag("Construct_Ensis"))
                    {
                        tIndex = 12;
                        eightWay = true;
                    }
                    else if (tile.HasTag("Construct Prison"))
                    {
                        tIndex = 18;
                    }
                    else if (tile.HasTag("Construct_Magna"))
                    {
                        tIndex = 19;
                        eightWay = true;
                    }
                    else if (tile.HasTag("Construct_Facility"))
                    {
                        tIndex = 20;
                    }
                    else if (tile.HasTag("Construct_Kin"))
                    {
                        tIndex = 22;
                        eightWay = true;
                    }
                    else if (tile.HasTag("Construct_Store"))
                    {
                        tIndex = 5;
                        eightWay = true;
                    }
                    else if (tile.HasTag("Construct_Hospital"))
                    {
                        tIndex = 2;
                        eightWay = true;
                    }
                    else if (tile.HasTag("Construct_Steel"))
                    {
                        tIndex = 24;
                        eightWay = true;
                    }

                    BitwiseAutotile(x, y, tIndex, (z => TileManager.GetByID(z).HasTag("Wall") && !TileManager.isMountain(z)), eightWay);
                }
                else if (TileManager.isMountain(tile.ID))
                {
                    int tIndex = 9;

                    if (elevation != 0 && World.tileMap.GetVaultAt(mapInfo.position) != null)
                    {
                        //TODO: Hard-coded. Switch this out to check the vault's wall tile.
                        string t = World.tileMap.GetVaultAt(mapInfo.position).blueprint.ID;

                        if (t == "Caves_Volcano")
                            tIndex = 13;
                        else if (t == "Caves_Ice")
                            tIndex = 15;
                        else if (t == "SubMagna")
                            tIndex = 19;

                    }
                    else if (tile == TileManager.tiles["Volcano_Wall"])
                        tIndex = 13;

                    BitwiseAutotile(x, y, tIndex, (z => TileManager.isMountain(z)), true);

                }
                else if (tile == TileManager.tiles["Lava"])
                {
                    int tIndex = 10;
                    BitwiseAutotile(x, y, tIndex, (z => z == tile.ID), true);
                }
                else if (tile == TileManager.tiles["Ice"])
                {
                    int tIndex = 16;
                    BitwiseAutotile(x, y, tIndex, (z => z == tile.ID), true);
                }
                else if (tile == TileManager.tiles["Dream_Floor"])
                {
                    int tIndex = 23;
                    BitwiseAutotile(x, y, tIndex, (z => z == tile.ID), true);
                }
                else if (elevation == 0 && tile.biome != mapInfo.biome)
                {
                    BitwiseAutotile(x, y, 21,
                        (
                            z => TileManager.GetByID(z).biome == Biome.Default && !TileManager.isWaterTile(z) 
                            || TileManager.GetByID(z).biome == tile.biome
                        ), true, TileMap_Generator.TileFromBiome(mapInfo.biome, false).ID);
                }
                else if (elevation != 0 && tile.HasTag("Walkable") && !tile.HasTag("Underground"))
                {
                    BitwiseAutotile(x, y, 21,
                        (
                            z => !TileManager.GetByID(z).HasTag("Walkable") || !TileManager.GetByID(z).HasTag("Underground")
                        ), true, TileManager.tiles["UG_Dirt_1"].ID);
                }
            }
        }
    }

    void BitwiseAutotile(int x, int y, int tIndex, System.Predicate<int> p, bool eightWay, int replaceID = -1)
    {
        int sum = 0;

        if (y < Height - 1 && p(map_data[x, y + 1].ID) || y >= Height - 1)
            sum++;
        if (x > 0 && p(map_data[x - 1, y].ID) || x <= 0)
            sum += 2;
        if (x < Width - 1 && p(map_data[x + 1, y].ID) || x >= Width - 1)
            sum += 4;
        if (y > 0 && p(map_data[x, y - 1].ID) || y <= 0)
            sum += 8;

        if (eightWay && sum == 15)
        {
            bool NE = (y < Height - 1 && x < Width - 1 && !p(map_data[x + 1, y + 1].ID));
            bool SE = (y > 0 && x < Width - 1 && !p(map_data[x + 1, y - 1].ID));
            bool SW = (y > 0 && x > 0 && !p(map_data[x - 1, y - 1].ID));
            bool NW = (y < Height - 1 && x > 0 && !p(map_data[x - 1, y + 1].ID));

            if      (NE) sum = 16;
            else if (SE) sum = 17;
            else if (SW) sum = 18;
            else if (NW) sum = 19;

            if      (NE && SW && !NW && !SE) sum = 20;
            else if (NW && SE && !NE && !SW) sum = 21;
            else if (NE && NW && !SE && !SW) sum = 22;
            else if (SE && SW && !NE && !NW) sum = 23;
            else if (NE && SE && !NW && !SW) sum = 24;
            else if (NW && SW && !NE && !SE) sum = 25;
        }

        if (replaceID == -1)
        {
            World.tileMap.SetTileGraphic(this, x, y, tIndex, sum);
        }
        else
        {
            World.tileMap.SetTileGraphic_AlphaMask(this, x, y, sum, replaceID);
        }
    }

    //Sets up pathfinding tiledata
    public void SetUpTileData()
    {
        pathTileData = new Path_TileData[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                pathTileData[x, y] = new Path_TileData(WalkableTile(x, y), new Coord(x, y), map_data[x, y].costToEnter);
            }
        }
    }

    public Path_TileData GetTileData(int x, int y)
    {
        if (World.OutOfLocalBounds(x, y))
        {
            return null;
        }

        if (pathTileData[x, y] == null)
        {
            pathTileData[x, y] = new Path_TileData(map_data[x, y].HasTag("Walkable"), new Coord(x, y), map_data[x, y].costToEnter);
        }

        return pathTileData[x, y];
    }

    public void SetWalkable(int x, int y, bool walk)
    {
        if (World.OutOfLocalBounds(x, y))
        {
            return;
        }

        if (pathTileData[x, y] == null)
        {
            pathTileData[x, y] = new Path_TileData(map_data[x, y].HasTag("Walkable"), new Coord(x, y), map_data[x, y].costToEnter);
        }

        pathTileData[x, y].walkable = walk;
    }


    public void ModifyTilePathCost(int x, int y, int cost)
    {
        if (World.OutOfLocalBounds(x, y))
        {
            return;
        }

        if (pathTileData[x, y] == null)
        {
            pathTileData[x, y] = new Path_TileData(map_data[x, y].HasTag("Walkable"), new Coord(x, y), map_data[x, y].costToEnter);
        }

        pathTileData[x, y].costToEnter += cost;
    }

    public Coord GetRandomFloorTile()
    {
        List<Coord> floorTiles = new List<Coord>();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (map_data[x, y].HasTag("Walkable") && map_data[x, y] != TileManager.tiles["Lava"])
                {
                    floorTiles.Add(new Coord(x, y));
                }
            }
        }

        return floorTiles.GetRandom(RNG);
    }
    //return tile id at a certain point
    public int GetTileNumAt(int x, int y)
    {
        if (World.OutOfLocalBounds(x, y))
        {
            return TileManager.tiles["Default"].ID;
        }

        return map_data[x, y].ID;
    }

    public bool WalkableTile(int x, int y)
    {
        if (World.OutOfLocalBounds(x, y))
        {
            return false;
        }

        return map_data[x, y].HasTag("Walkable");
    }

    bool CreateHouse()
    {
        int width = RNG.Next(4, 7), height = RNG.Next(4, 7);
        int left = RNG.Next(4, (Width - width) - 4), bottom = RNG.Next(4, (Height - height) - 4);

        Room room1 = new Room(width, height, left, bottom);
        Room room2 = new Room(height + RNG.Next(-2, 1), width + RNG.Next(-2, 1), RNG.Next(left, room1.right), RNG.Next(bottom, room1.top));
        House.HouseType ht = House.HouseType.Villager;
        string wallType = "Wall_Brick";

        if (RNG.Next(100) < 5)
        {
            ht = House.HouseType.Merchant;
            wallType = "Wall_Store";
        }
        else if (RNG.Next(100) < 5)
        {
            ht = House.HouseType.Doctor;
            wallType = "Wall_Hospital";
        }

        House h = new House(room1, room2, ht);

        for (int i = 0; i < houses.Count; i++)
        {
            if (h.houseType != House.HouseType.Villager && h.houseType == houses[i].houseType)
            {
                return false;
            }

            for (int j = 0; j < h.rooms.Length; j++)
            {
                for (int k = 0; k < houses[i].rooms.Length; k++)
                {
                    if (h.rooms[j].CollidesWith(houses[i].rooms[k]))
                    {
                        return false;
                    }
                }
            }
        }

        const string roadTile = "Floor_Brick_2";
        const string floorTile = "Floor_Brick";

        //If it overlaps roads, discard
        for (int i = 0; i < h.rooms.Length; i++)
        {
            for (int x = h.rooms[i].left - 1; x < h.rooms[i].right + 1; x++)
            {
                for (int y = h.rooms[i].bottom - 1; y < h.rooms[i].top + 1; y++)
                {
                    if (map_data[x, y] == TileManager.tiles[roadTile])
                    {
                        return false;
                    }
                }
            }
        }

        List<Coord> possibleDoorPos = new List<Coord>();

        //set tiles
        for (int i = 0; i < h.rooms.Length; i++)
        {
            for (int x = h.rooms[i].left; x < h.rooms[i].right; x++)
            {
                for (int y = h.rooms[i].bottom; y < h.rooms[i].top; y++)
                {
                    map_data[x, y] = TileManager.tiles[floorTile];
                }
            }
        }

        for (int x = h.Left() - 1; x <= h.Right(); x++)
        {
            for (int y = h.Bottom() - 1; y <= h.Top(); y++)
            {
                if (World.OutOfLocalBounds(x, y) || map_data[x, y] == TileManager.tiles[floorTile] || map_data[x, y] == TileManager.tiles[wallType])
                {
                    continue;
                }

                bool visited = false;

                for (int ex = -1; ex <= 1; ex++)
                {
                    for (int ey = -1; ey <= 1; ey++)
                    {
                        if (ex == 0 && ey == 0 || visited)
                            continue;
                        if (x + ex <= 0 || x + ex >= Manager.localMapSize.x - 1 || y + ey <= 0 || y + ey >= Manager.localMapSize.y - 1)
                            continue;

                        if (map_data[x + ex, y + ey] == TileManager.tiles[floorTile])
                        {
                            map_data[x, y] = TileManager.tiles[wallType];
                            Coord newWall = new Coord(x, y);

                            if (x == h.Left() - 1 || x == h.Right() || y == h.Bottom() - 1 || y == h.Top())
                            {
                                possibleDoorPos.Add(newWall);
                            }

                            visited = true;
                        }
                    }
                }
            }
        }

        PlaceDoorInHouse(possibleDoorPos, wallType);

        houses.Add(h);
        return true;
    }

    void PlaceDoorInHouse(List<Coord> possibleDoorPos, string wallType)
    {
        List<Coord> finalDoors = new List<Coord>();

        for (int i = 0; i < possibleDoorPos.Count; i++)
        {
            int dx = possibleDoorPos[i].x, dy = possibleDoorPos[i].y;

            if (map_data[dx + 1, dy] == TileManager.tiles[wallType] && map_data[dx - 1, dy] == TileManager.tiles[wallType] ||
                map_data[dx, dy + 1] == TileManager.tiles[wallType] && map_data[dx, dy - 1] == TileManager.tiles[wallType])
            {
                finalDoors.Add(possibleDoorPos[i]);
            }
        }

        Coord door = null;

        //Place door
        if (finalDoors.Count > 0)
        {
            door = finalDoors.GetRandom(RNG);
        }
        //Fallback
        else if (possibleDoorPos.Count > 0)
        {
            door = possibleDoorPos.GetRandom(RNG);
        }

        if (door != null)
        {
            map_data[door.x, door.y] = TileManager.tiles["Door"];
            RemoveTreesAroundDoor(door.x, door.y, wallType);
        }
    }

    void RemoveTreesAroundDoor(int x, int y, string wallType)
    {
        for (int ex = -1; ex <= 1; ex++)
        {
            for (int ey = -1; ey <= 1; ey++)
            {
                if (World.OutOfLocalBounds(x + ex, y + ey) || ex == 0 && ey == 0)
                {
                    continue;
                }

                Tile_Data t = map_data[x + ex, y + ey];

                if (t != TileManager.tiles[wallType] && !t.HasTag("Walkable"))
                {
                    map_data[x + ex, y + ey] = TileMap_Generator.TileFromBiome(mapInfo.biome);
                }
            }
        }            
    }

    bool LoadCustomMap()
    {
        bool p(IAsset asset) { return asset is Map m && CanLoadMap(m.elevation, m.locationID); }
        List<Map> maps = GameData.Get<Map>(p);

        if (maps.NullOrEmpty())
        {
            return false;
        }

        return LoadMap(maps.GetRandom(SeedManager.localRandom));
    }

    bool CanLoadMap(int ele, string locID)
    {
        if (Mathf.Abs(elevation) == Mathf.Abs(ele))
        {
            if (elevation == 0 && mapInfo.HasLandmark() && locID == mapInfo.landmark ||
                elevation != 0 && locID == World.tileMap.GetVaultAt(mapInfo.position).blueprint.ID ||
                locID == "Random_Encounter" && !mapInfo.HasLandmark())
            {
                return true;
            }
        }

        return false;
    }

    bool LoadSpecificMap(string mapName)
    {
        Map map = GameData.Get<Map>(mapName);

        if (map == null)
        {
            return false;
        }

        return LoadMap(map);
    }

    bool LoadMap(Map map)
    {
        int diffX = (Width - map.size.x) / 2;
        int diffY = (Height - map.size.y) / 2;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                map_data[x, y] = TileMap_Generator.TileFromBiome(mapInfo.biome, false);
            }
        }

        for (int x = 0; x < map.size.x; x++)
        {
            for (int y = 0; y < map.size.y; y++)
            {
                if (x >= Width || y >= Height)
                {
                    continue;
                }

                int id = map.tiles[x * map.size.y + y];
                if (id != TileManager.tiles["Default"].ID)
                {
                    map_data[x + diffX, y + diffY] = TileManager.GetByID(id);
                }

                if (id == TileManager.tiles["Default_NoBlock"].ID)
                {
                    map_data[x + diffX, y + diffY] = TileMap_Generator.TileFromBiome(mapInfo.biome, false);
                }
            }
        }

        Coord AdjustedPosition(Coord c)
        {
            return new Coord(c.x + diffX, c.y + diffY);
        }

        if (!visited)
        {
            for (int i = 0; i < map.objects.Count; i++)
            {
                World.objectManager.NewObjectAtSpecificScreen(map.objects[i].Key, AdjustedPosition(map.objects[i].Value), mapInfo.position, -elevation);
            }

            for (int i = 0; i < map.npcs.Count; i++)
            {
                string npcID = map.npcs[i].Key;
                Coord localPos = AdjustedPosition(map.npcs[i].Value);

                if (!GameData.TryGet<NPC_Blueprint>(npcID, out var bp))
                {
                    continue;
                }

                //If this NPC is static and has died, do not spawn it.
                if (bp.flags.Contains(NPC_Flags.Static) && ObjectManager.playerJournal != null 
                    && ObjectManager.playerJournal.staticNPCKills.Contains(bp.ID))
                {
                    continue;
                }

                if (bp.flags.Contains(NPC_Flags.Static) && World.objectManager.NPCExists(npcID))
                {
                    NPC npc = World.objectManager.npcClasses.Find(o => o.ID == npcID);
                    if (npc.worldPosition == mapInfo.position)
                    {
                        npc.localPosition = localPos;
                    }
                }
                else
                {
                    NPC n = new NPC(bp, new Coord(mapInfo.position), localPos, -elevation);
                    World.objectManager.CreateNPC(n);
                }
            }
        }

        loadedFromData = true;
        SetUpTileData();
        return true;
    }

    void Volcano()
    {
        int centerX = Width / 2, centerY = Height / 2;
        Vector2 center = new Vector2(centerX, centerY);

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                float distToCenter = Vector2.Distance(center, new Vector2(x, y));

                if (distToCenter < RNG.Next(4, 6))
                {
                    map_data[x, y] = TileManager.GetByName("Lava");
                }
                else if (distToCenter < RNG.Next(11, 13))
                {
                    map_data[x, y] = TileManager.GetByName("Volcano_Wall");
                }
                else
                {
                    if (RNG.OneIn(100))
                    {
                        map_data[x, y] = TileManager.GetByName("Volcano_Wall");
                    }
                    else
                    {
                        map_data[x, y] = RNG.OneIn(4) ? TileManager.GetByName("Mountain_Floor") : TileManager.GetByName("UG_Dirt_1");
                    }
                }
            }
        }

        Coord c = GetRandomFloorTile();

        if (map_data[c.x, c.y] == TileManager.GetByName("Lava"))
        {
            int numTries = 0;

            while (map_data[c.x, c.y] == TileManager.GetByName("Lava") && numTries < 1000)
            {
                c = GetRandomFloorTile();
                numTries++;
            }
        }

        if (c != null)
        {
            map_data[c.x, c.y] = TileManager.GetByName("Stairs_Down");
        }
    }

    void CreateVaultLevel(Vault v)
    {
        if (LoadCustomMap())
        {
            return;
        }

        Dungeon d = new Dungeon(v.blueprint);

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                map_data[x, y] = TileManager.GetByID(d.GetTileAt(x, y));

                if (WalkableTile(x, y) && map_data[x, y] != TileManager.GetByName("Stairs_Up") && map_data[x, y] != TileManager.tiles["Stairs_Down"])
                {
                    if (v.blueprint.ID == "Cave_Start" && RNG.Next(1000) < 12 || v.blueprint.ID == "Spider" && RNG.Next(1000) < 40)
                        World.objectManager.NewObjectAtSpecificScreen("Web", new Coord(x, y), mapInfo.position, -elevation);
                    else if (RNG.Next(10000) < Mathf.Abs(elevation) + 1)
                        World.objectManager.NewObjectAtSpecificScreen("Chest", new Coord(x, y), mapInfo.position, -elevation);
                    else if (RNG.Next(50000) < Mathf.Abs(elevation + 1))
                        World.objectManager.NewObjectAtSpecificScreen("Chest_Large", new Coord(x, y), mapInfo.position, -elevation);
                }
            }
        }

        bool spawnDownStairs = Mathf.Abs(elevation) < v.screens.Length - 1;
        Coord stairsUp = GetRandomFloorTile();
        Coord stairsDown = GetRandomFloorTile();

        map_data[stairsUp.x, stairsUp.y] = TileManager.tiles["Stairs_Up"];

        if (spawnDownStairs)
        {
            int numFails = 0;

            while (stairsUp.DistanceTo(stairsDown) < 15f && numFails < 500)
            {
                stairsDown = GetRandomFloorTile();
                numFails++;
            }

            map_data[stairsDown.x, stairsDown.y] = TileManager.tiles["Stairs_Down"];
        }

        if (v.blueprint.ID == "Cellar" && !visited)
        {
            PlaceCellarLoot();
        }
    }

    void PlaceCellarLoot()
    {
        if (World.objectManager.mapObjects.Find(x => x.worldPosition == mapInfo.position && x.elevation == elevation) != null)
        {
            return;
        }

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (WalkableTile(x, y) && RNG.Next(100) < 10 && map_data[x, y] != TileManager.tiles["Stairs_Up"] && map_data[x, y] != TileManager.tiles["Stairs_Down"])
                {
                    string t = (RNG.Next(100) < 85) ? "Barrel" : "Chest";

                    if (RNG.Next(100) == 0)
                    {
                        t = "Chest_Large";
                    }

                    World.objectManager.NewObjectAtSpecificScreen(t, new Coord(x, y), mapInfo.position, World.tileMap.currentElevation);
                }
            }
        }
    }

    void Ruins()
    {
        List<Room> ruins = new List<Room>();

        for (int e = 0; e < 50; e++)
        {
            int width = RNG.Next(4, 8), height = RNG.Next(5, 9);
            int left = RNG.Next(2, Width - width - 2), bottom = RNG.Next(2, Height - height - 2);
            Room house = new Room(width, height, left, bottom);
            bool canPlace = true;

            for (int h = 0; h < ruins.Count; h++)
            {
                if (house.CollidesWith(ruins[h]))
                {
                    canPlace = false;
                    break;
                }
            }

            if (canPlace)
            {
                for (int x = left; x < house.right + 1; x++)
                {
                    for (int y = bottom + 1; y < house.top + 1; y++)
                    {
                        if ((x == left || x == house.right || y == bottom + 1 || y == house.top))
                        {
                            if (RNG.Next(100) < 80)
                            {
                                map_data[x, y] = TileManager.tiles["Wall_Brick"];
                            }
                        }
                        else if (RNG.Next(0, 101) < 75)
                        {
                            map_data[x, y] = TileManager.tiles["Floor_House"];
                        }
                    }
                }

                //Spawn bed rolls
                if (!visited && RNG.Next(100) < 30)
                {
                    for (int i = 0; i < RNG.Next(0, 3); i++)
                    {
                        Coord lp = new Coord(RNG.Next(left + 1, left + width - 1), RNG.Next(bottom + 2, bottom + height - 1));
                        World.objectManager.NewObjectAtSpecificScreen("Bed_Roll", lp, new Coord(mapInfo.position.x, mapInfo.position.y), elevation);
                    }
                }

                //Place openings 
                int dPos = RNG.Next(0, 4);

                if (dPos == 0 && map_data[house.centerX, house.top] == TileManager.tiles["Wall_Brick"]) //N
                    map_data[house.centerX, house.top] = TileManager.tiles["Floor_House"];
                else if (dPos == 1 && map_data[house.centerX, bottom] == TileManager.tiles["Wall_Brick"]) //S
                    map_data[house.centerX, bottom] = TileManager.tiles["Floor_House"];
                else if (dPos == 2 && map_data[house.right, house.centerY] == TileManager.tiles["Wall_Brick"]) //E
                    map_data[house.right, house.centerY] = TileManager.tiles["Floor_House"];
                else if (dPos == 3 && map_data[house.left, house.centerY] == TileManager.tiles["Wall_Brick"]) //W
                    map_data[house.left, house.centerY] = TileManager.tiles["Floor_House"];

                ruins.Add(house);
            }
        }

        //Place barrels.
        if (!visited)
        {
            for (int i = 0; i < RNG.Next(3, 7); i++)
            {
                Coord lp = GetRandomFloorTile();
                World.objectManager.NewObjectAtSpecificScreen("Barrel", lp, mapInfo.position, elevation);
            }
        }
    }

    bool RiverAt(int x, int y)
    {
        MapInfo mi = WorldData.GetTileAt(x, y);
        return (mi.landmark == "River" || mi.biome == Biome.Ocean);
    }

    bool VillageAt(int x, int y)
    {
        MapInfo mi = WorldData.GetTileAt(x, y);
        return (mi.landmark == "Village");
    }

    void CreateRiver(int x, int y)
    {
        int centerX = Width / 2, centerY = Height / 2;
        int w = 3, h = 3;
        float sinXAmount = RNG.Next(50, 100) / 100f, sinYAmount = RNG.Next(50, 100) / 100f;
        float amplitude = RNG.Next(20, 50) * 0.1f;
        float offset;

        bool W = (x > 0 && RiverAt(x - 1, y));
        bool E = (x < Manager.worldMapSize.x - 1 && RiverAt(x + 1, y));
        bool S = (y > 0 && RiverAt(x, y - 1));
        bool N = (y < Manager.worldMapSize.y - 1 && RiverAt(x, y + 1));

        for (int rx = 0; rx < Width; rx++)
        {
            offset = 1.0f;

            for (int ry = 0; ry < Height; ry++)
            {
                int ranNumX = (int)(Mathf.Sin(ry / 3) * sinXAmount * offset);
                int ranNumY = (int)(Mathf.Sin(rx / 3) * sinYAmount * offset);

                if (N && rx > centerX - w + ranNumX && rx < centerX + w + ranNumX && ry > centerY - h + ranNumY)
                    map_data[rx, ry] = TileManager.tiles["Water"];
                if (S && rx > centerX - w + ranNumX && rx < centerX + w + ranNumX && ry < centerY + h + ranNumY)
                    map_data[rx, ry] = TileManager.tiles["Water"];
                if (E && ry > centerY - h + ranNumY && ry < centerY + h + ranNumY && rx > centerX - w + ranNumX)
                    map_data[rx, ry] = TileManager.tiles["Water"];
                if (W && ry > centerY - h + ranNumY && ry < centerY + h + ranNumY && rx < centerX + w + ranNumX)
                    map_data[rx, ry] = TileManager.tiles["Water"];

                offset += RNG.ZeroToOne() * 0.15f;
                amplitude += RNG.ZeroToOne() * 0.01f * RNG.Next(-1, 2);
            }
        }

        //Place Ice instead of water
        if (mapInfo.biome == Biome.Tundra)
        {
            for (int ex = 0; ex < Width; ex++)
            {
                for (int ey = 0; ey < Height; ey++)
                {
                    if (TileManager.isWaterTile(map_data[ex, ey].ID, false))
                    {
                        map_data[ex, ey] = TileManager.tiles["Ice"];
                    }
                }
            }
        }
    }

    void CreateRoad(int x, int y)
    {
        int centerX = Width / 2, centerY = Height / 2;
        int w = 2, h = 2;
        float sinXAmount = RNG.ZeroToOne(), sinYAmount = RNG.ZeroToOne();
        float amplitude = RNG.Next(30, 50) * 0.1f;
        float offset;

        bool W = (x > 0 && VillageAt(x - 1, y));
        bool E = (x < Manager.worldMapSize.x - 1 && VillageAt(x + 1, y));
        bool S = (y > 0 && VillageAt(x, y - 1));
        bool N = (y < Manager.worldMapSize.y - 1 && VillageAt(x, y + 1));

        for (int rx = 0; rx < Width; rx++)
        {
            offset = 1.0f;

            for (int ry = 0; ry < Height; ry++)
            {
                int ranNumX = (int)(Mathf.Sin(ry / amplitude) * sinXAmount * offset);
                int ranNumY = (int)(Mathf.Sin(rx / amplitude) * sinYAmount * offset);

                if (N && rx > centerX - w + ranNumX && rx < centerX + w + ranNumX && ry > centerY - h + ranNumY)
                    map_data[rx, ry] = TileManager.tiles["Floor_Brick_2"];
                if (S && rx > centerX - w + ranNumX && rx < centerX + w + ranNumX && ry < centerY + h + ranNumY)
                    map_data[rx, ry] = TileManager.tiles["Floor_Brick_2"];
                if (E && ry > centerY - h + ranNumY && ry < centerY + h + ranNumY && rx > centerX - w + ranNumX)
                    map_data[rx, ry] = TileManager.tiles["Floor_Brick_2"];
                if (W && ry > centerY - h + ranNumY && ry < centerY + h + ranNumY && rx < centerX + w + ranNumX)
                    map_data[rx, ry] = TileManager.tiles["Floor_Brick_2"];

                offset += RNG.ZeroToOne() * 0.15f;
                amplitude += RNG.ZeroToOne() * 0.01f * RNG.Next(-1, 2);
            }
        }
    }

    public Coord StairsDown()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (map_data[x, y] == TileManager.tiles["Stairs_Down"])
                {
                    return new Coord(x, y);
                }
            }
        }

        return null;
    }

    public Coord StairsUp()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (map_data[x, y] == TileManager.tiles["Stairs_Up"])
                {
                    return new Coord(x, y);
                }
            }
        }

        return null;
    }

    public bool IsWaterTile(int x, int y)
    {
        if (x < 0 || x > Width || y < 0 || y > Height)
        {
            return false;
        }

        return TileManager.isWaterTile(map_data[x, y].ID);
    }

    void Init()
    {
        map_data = new Tile_Data[Width, Height];
        has_seen = new bool[Width, Height];
        houses = new List<House>();

        if (World.turnManager != null)
        {
            lastTurnSeen = World.turnManager.turn;
        }
    }

    public struct TileChange
    {
        public int x;
        public int y;
        public int tType;

        public TileChange(int x, int y, int tType)
        {
            this.x = x;
            this.y = y;
            this.tType = tType;
        }
    }
}
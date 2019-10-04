using UnityEngine;
using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Pathfinding;
using LitJson;

[Serializable]
public class WorldMap_Data
{
    public static List<SMapFeature> featuresToAdd;

    public Coord startPosition;
    public List<Coord> ruinsPos = new List<Coord>(), vaultAreas = new List<Coord>();
    public List<Landmark> landmarks = new List<Landmark>();
    public List<Village_Data> villages = new List<Village_Data>();
    public bool doneLoading = false;
    public Path_TileData[,] tileData { get; set; }
    public List<Landmark> postGenLandmarks { get; protected set; }

    readonly MapInfo[,] tiles;
    readonly JsonData worldTileData;
    MapGenInfo mapGenInfo;
    List<Coord> mountains, ocean;
    Action callbackAction;

    const int riversMin = 8, riversMax = 16;

    System.Random rng
    {
        get { return SeedManager.worldRandom; }
    }

    int width
    {
        get { return Manager.worldMapSize.x; }
    }

    int height
    {
        get { return Manager.worldMapSize.y; }
    }

    //Generating a new world
    public WorldMap_Data(bool newGame, Action callback)
    {
        SeedManager.SetSeedFromName(Manager.worldSeed);
        tiles = new MapInfo[width, height];
        callbackAction = callback;
        mountains = new List<Coord>();
        ocean = new List<Coord>();
        postGenLandmarks = new List<Landmark>();

        if (!newGame)
        {
            OldWorld ow = new OldWorld();

            for (int i = 0; i < ow.postGenLandmarks.Count; i++)
            {
                postGenLandmarks.Add(ow.postGenLandmarks[i]);
            }
        }

        Thread t = new Thread(new ThreadStart(GenerateTerrain));
        t.Start();
    }

    public void NewPostGenLandmark(Coord c, string zoneID)
    {
        postGenLandmarks.Add(new Landmark(c, zoneID));
        World.worldMap.PlaceLandmark(c.x, c.y, tiles[c.x, c.y]);
    }

    void GenerateTerrain()
    {
        mapGenInfo = Generate_WorldMap.Generate(rng, width, height, 4, 8f, 20f);
        int[,] values = mapGenInfo.mapData;

        for (int x = 0; x < values.GetLength(0); x++)
        {
            for (int y = 0; y < values.GetLength(1); y++)
            {
                float height = 0.4f;

                switch (values[x, y])
                {
                    case 0: height = 0.1f; break;
                    case 5: height = 0.22f; break;
                    case 1: height = 0.4f; break;
                    case 2: height = 0.6f; break;
                    case 3: height = 0.8f; break;
                    case 4: height = 0.99f; break;
                }

                SetBiome(height, x, y);
            }
        }

        GenerateHeat();
    }

    float DistanceToPoint_Biased(int x1, int y1, float x, float y)
    {
        float distanceX = (x1 - x) * (x1 - x), distanceY = (y1 - y) * (y1 - y);
        float biasX = 0.5f;
        float biasY = 1.1f;

        return Mathf.Sqrt(distanceX * biasX + distanceY * biasY) / (height / 2 - 1);
    }

    void SetBiome(float biome, int x, int y)
    {
        Coord c = new Coord(x, y);

        if (biome <= 0.16f)
        { //Ocean
            tiles[x, y] = new MapInfo(c, Biome.Ocean);
            ocean.Add(c);

        }
        else if (biome <= 0.19f)
        { //Shore
            tiles[x, y] = new MapInfo(c, Biome.Shore);
        }
        else if (biome <= 0.42f)
        { //Plains
            tiles[x, y] = new MapInfo(c, (rng.Next(0, 101) < 95) ? Biome.Plains : Biome.Forest);

        }
        else if (biome <= 0.63f)
        { //Forest
            tiles[x, y] = new MapInfo(c, (rng.Next(0, 101) < 95) ? Biome.Forest : Biome.Plains);
        }
        else
        { //Mountains
            tiles[x, y] = new MapInfo(c, Biome.Mountain);
            mountains.Add(c);
        }
    }

    void GenerateHeat()
    {
        float scale = 6;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y].biome == Biome.Mountain || tiles[x, y].biome == Biome.Ocean)
                    continue;

                float yOrg = rng.Next(-1000, 1000), yCoord = yOrg + y / height * (scale / 1.5f);
                float perlin = Mathf.PerlinNoise(x * 3, yCoord);
                int centerY = height / 2 - 1;
                float distToCenter = DistanceToPoint_Biased(width / 2 - 1, centerY, x, y);
                float val = (perlin + distToCenter);

                if (Mathf.Abs(val) > 0.9f)
                {
                    if (y > centerY + 15)
                        tiles[x, y].biome = Biome.Tundra;
                    else if (y < centerY - 15)
                        tiles[x, y].biome = Biome.Desert;
                }
            }
        }

        for (int i = 0; i < 4; i++)
        {
            for (int hx = 1; hx < width - 1; hx++)
            {
                for (int hy = 1; hy < height - 1; hy++)
                {
                    if (tiles[hx, hy].biome == Biome.Tundra)
                        SmoothBiome(hx, hy, Biome.Tundra);
                    else if (tiles[hx, hy].biome == Biome.Desert)
                        SmoothBiome(hx, hy, Biome.Desert);
                }
            }
        }

        for (int i = 0; i < 2; i++)
        {
            RemoveIsolatedTiles();
        }

        SurroundWaterWithShore();

        if (mountains.Count > 0)
        {
            //Place rivers
            int numRivers = 0, numTries = 0, maxRivers = rng.Next(riversMin, riversMax + 1), maxTries = 10000;

            while (numRivers < maxRivers && numTries < maxTries)
            {
                if (PlaceRiver(mountains.GetRandom(rng)))
                    numRivers++;
                else
                    numTries++;
            }
        }


        SetupPathfindingGrid();
        FinalPass();
    }

    void SmoothBiome(int x, int y, Biome b)
    {
        int neighbors = 0;

        for (int ex = -1; ex <= 1; ex++)
        {
            for (int ey = -1; ey <= 1; ey++)
            {
                if (Mathf.Abs(ex) + Mathf.Abs(ey) > 1 || ex == 0 && ey == 0)
                    continue;

                if (tiles[x, y].biome == b || tiles[x, y].biome == Biome.Ocean)
                    neighbors++;
            }
        }

        if (neighbors <= 2 && rng.Next(100) < 65 || neighbors == 0)
        {
            tiles[x, y].biome = Biome.Plains;
        }
    }

    bool GrassTile(int x, int y)
    {
        return tiles[x, y].biome == Biome.Plains || tiles[x, y].biome == Biome.Forest ||
            (tiles[x, y].biome == Biome.Tundra && rng.Next(100) < 40) || (tiles[x, y].biome == Biome.Desert && rng.Next(100) < 40);
    }

    void SurroundWaterWithShore()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //Place down shore tiles along edges.
                if (GrassTile(x, y) && tiles[x, y].biome != Biome.Tundra && tiles[x, y].biome != Biome.Desert)
                {
                    for (int ex = -1; ex <= 1; ex++)
                    {
                        for (int ey = -1; ey <= 1; ey++)
                        {
                            if (x == 0 && y == 0 || x + ex >= Manager.worldMapSize.x || x + ex < 0 || y + ey >= Manager.worldMapSize.x || y + ey < 0)
                                continue;

                            if (tiles[x + ex, y + ey].biome == Biome.Ocean)
                                tiles[x, y].biome = Biome.Shore;
                        }
                    }
                }
            }
        }
    }

    bool PlaceRiver(Coord startTile)
    {
        Coord endTile = ocean.GetRandom(rng);
        int numTries = 0, maxTries = 10000;

        while (Vector2.Distance(startTile.toVector2(), endTile.toVector2()) > 10 && numTries < maxTries)
        {
            endTile = ocean.GetRandom(rng);
            numTries++;
        }

        if (numTries >= maxTries)
            return false;

        int dx = (startTile.x > endTile.x) ? -1 : 1, dy = (startTile.y > endTile.y) ? -1 : 1;

        if (startTile.x == endTile.x)
            dx = 0;
        if (startTile.y == endTile.y)
            dy = 0;

        List<Coord> riverTiles = new List<Coord>();

        int nx = startTile.x;
        int ny = startTile.y;
        bool breakOut = false;

        while (tiles[nx, ny].biome != Biome.Ocean)
        {
            if (nx < 0 || ny < 0 || nx >= Manager.worldMapSize.x || ny >= Manager.worldMapSize.y)
                return false;
            if (breakOut)
                break;

            //Check adjacent oceans to avoid strangeness in autotiling.
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) >= 2 || x == 0 && y == 0)
                        continue;
                    if (nx + x < 0 || ny + y < 0 || nx + x >= Manager.worldMapSize.x || ny + y >= Manager.worldMapSize.y)
                        continue;

                    if (tiles[nx + x, ny + y].biome == Biome.Ocean)
                    {
                        breakOut = true;
                    }
                }
            }

            Coord newRiver = new Coord(nx, ny);
            riverTiles.Add(newRiver);

            if (rng.Next(100) < 50)
                nx += dx;
            else
                ny += dy;
        }

        if (!CanPlaceRiver(riverTiles))
            return false;

        for (int i = 0; i < riverTiles.Count; i++)
        {
            tiles[riverTiles[i].x, riverTiles[i].y].landmark = "River";
        }

        return true;
    }

    bool CanPlaceRiver(List<Coord> riverTiles)
    {
        for (int i = 1; i < riverTiles.Count; i++)
        {
            if (tiles[riverTiles[i].x, riverTiles[i].y].HasLandmark() || tiles[riverTiles[i].x, riverTiles[i].y].biome == Biome.Mountain)
            {
                return false;
            }
            else
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        int rx = riverTiles[i].x + x;
                        int ry = riverTiles[i].y + y;

                        if (rx <= 0 || rx >= Manager.worldMapSize.x - 1 || ry <= 0 || ry >= Manager.worldMapSize.y)
                        {
                            continue;
                        }

                        if (tiles[rx, ry].landmark == "River" && !riverTiles.Contains(new Coord(rx, ry)))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    void FinalPass()
    {
        for (int s = 0; s < rng.Next(12, 19); s++)
        {
            Swamp();
        }

        PlaceZones();
        doneLoading = true;
        callbackAction();
    }

    void PlaceZones()
    {
        foreach (ZoneBlueprint z in GameData.GetAll<ZoneBlueprint>())
        {
            for (int i = 0; i < z.amount; i++)
            {
                PlaceZone(z);
            }
        }

        for (int i = 0; i < postGenLandmarks.Count; i++)
        {
            ZoneBlueprint zb = GetZone(postGenLandmarks[i].desc);

            if (zb != null)
            {
                PlaceZone(zb, null, postGenLandmarks[i].pos);
            }
        }
    }

    public Coord PlaceZone(ZoneBlueprint zb, Coord parentPos = null, Coord forcePos = null)
    {
        Coord pos = null;
        bool hasParent = (parentPos != null && zb.placement.relativePosition != null);

        if (forcePos != null)
        {
            pos = forcePos;
        }
        else if (!hasParent)
        {
            if (!zb.placement.landmark.NullOrEmpty())
            {
                if (zb.placement.zoneID == ZoneBlueprint.DefaultPlacementBiome ||
                    !TryGetLandmark(zb.placement.landmark, zb.placement.zoneID.ToEnum<Biome>(), out pos))
                {
                    pos = GetRandomLandmark(zb.placement.landmark);
                }
            }
            else
            {
                if (zb.placement.zoneID == ZoneBlueprint.DefaultPlacementBiome)
                {
                    pos = zb.placement.onMain ? GetOpenFromMainIsland(zb.neighbors) : GetOpenPosition(zb.neighbors);
                }
                else
                {
                    pos = GetOpenPosition(zb.placement.zoneID.ToEnum<Biome>());
                }
            }
        }
        else
        {
            pos = parentPos + zb.placement.relativePosition;
        }

        if (pos == null || pos.x < 0 || pos.y < 0 || pos.x >= width || pos.y >= height)
        {
            Debug.LogError("Zone \"" + zb.name + "\" could not be placed.");
            return null;
        }

        if (tiles[pos.x, pos.y].biome == Biome.Mountain)
        {
            tiles[pos.x, pos.y].biome = Biome.Shore;
        }

        if (zb.radiation > 0)
        {
            tiles[pos.x, pos.y].radiation = zb.radiation;
        }

        tiles[pos.x, pos.y].landmark = zb.ID;
        tiles[pos.x, pos.y].friendly = zb.friendly;
        tileData[pos.x, pos.y].walkable = zb.walkable;

        if (!string.IsNullOrEmpty(zb.underground))
        {
            vaultAreas.Add(new Coord(pos.x, pos.y));
        }

        if (zb.isStart)
        {
            startPosition = pos;
        }

        if (zb.neighbors != null)
        {
            for (int i = 0; i < zb.neighbors.Length; i++)
            {
                PlaceZone(zb.neighbors[i], pos);
            }
        }

        if (zb.ID == "Village")
        {
            Village_Data vd = new Village_Data(pos, NameGenerator.CityName(rng), pos);
            villages.Add(vd);

            if (zb.expand)
            {
                ExpandVillage(vd);
            }
        }

        landmarks.Add(new Landmark(pos, (zb.ID == "Village" ? "Village of " + zb.name : zb.name)));
        return pos;
    }

    void RemoveIsolatedTiles()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (tiles[x, y].biome != Biome.Ocean && !tiles[x, y].HasLandmark())
                {
                    int waterNeighbours = 0;

                    for (int ex = -1; ex <= 1; ex++)
                    {
                        for (int ey = -1; ey <= 1; ey++)
                        {
                            if (ex == 0 && ey == 0)
                            {
                                continue;
                            }

                            if (tiles[x + ex, y + ey].biome == Biome.Ocean)
                            {
                                waterNeighbours++;
                            }
                        }
                    }

                    if (waterNeighbours > 5)
                    {
                        tiles[x, y].biome = Biome.Ocean;
                    }
                }
            }
        }
    }

    void ExpandVillage(Village_Data vData)
    {
        int xMax = 1, yMax = 1;
        List<Coord> possibleLocations = new List<Coord>();

        for (int x = -xMax; x <= xMax; x++)
        {
            for (int y = -yMax; y <= yMax; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                int vx = vData.center.x + x, vy = vData.center.y + y;

                if (tiles[vx, vy].biome != Biome.Ocean && tiles[vx, vy].biome != Biome.Mountain && !tiles[vx, vy].HasLandmark())
                {
                    possibleLocations.Add(new Coord(vx, vy));
                }
            }
        }

        for (int i = 0; i < rng.Next(4, 7); i++)
        {
            if (possibleLocations.Count <= 0)
            {
                break;
            }

            Coord v = possibleLocations.GetRandom(rng);
            possibleLocations.Remove(v);
            Village_Data vd = new Village_Data(v, vData.name, vData.mapPosition);
            tiles[v.x, v.y].landmark = "Village";
            tiles[v.x, v.y].friendly = true;
            villages.Add(vd);
        }
    }

    public Coord GetOpenPosition(ZoneBlueprint[] neighbors)
    {
        List<Coord> openPositions = new List<Coord>();

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (GrassTile(x, y) && !tiles[x, y].HasLandmark())
                {
                    bool canAdd = true;

                    if (neighbors != null && neighbors.Length > 0)
                    {
                        for (int i = 0; i < neighbors.Length; i++)
                        {
                            int offX = x + neighbors[i].placement.relativePosition.x, offY = y + neighbors[i].placement.relativePosition.y;

                            if (!GrassTile(offX, offY) || tiles[offX, offY].HasLandmark())
                            {
                                canAdd = false;
                            }
                        }
                    }
                    
                    if (canAdd)
                    {
                        openPositions.Add(new Coord(x, y));
                    }
                }
            }
        }

        if (openPositions.Count <= 0)
        {
            Debug.LogError("WorldMap_Data::GetOpenPosition() - No available open positions!");
        }

        return openPositions.GetRandom(rng);
    }

    public Coord GetOpenFromMainIsland(ZoneBlueprint[] neighbors)
    {
        List<Coord> openPositions = new List<Coord>();

        for (int i = 0; i < mapGenInfo.biggestIsland.Count; i++)
        {
            int x = mapGenInfo.biggestIsland[i].x, y = mapGenInfo.biggestIsland[i].y;

            if (GrassTile(x, y) && !tiles[x, y].HasLandmark())
            {
                bool canAdd = true;

                if (neighbors != null && neighbors.Length > 0)
                {
                    for (int j = 0; j < neighbors.Length; j++)
                    {
                        int newX = x + neighbors[j].placement.relativePosition.x, newY = y + neighbors[j].placement.relativePosition.y;

                        if (newX < 0 || newX >= Manager.worldMapSize.x || newY < 0 || newY >= Manager.worldMapSize.y)
                        {
                            canAdd = false;
                            break;
                        }

                        if (!GrassTile(newX, newY) || tiles[newX, newY].HasLandmark())
                        {
                            canAdd = false;
                            break;
                        }
                    }
                }

                if (canAdd)
                {
                    openPositions.Add(mapGenInfo.biggestIsland[i]);
                }
            }
        }

        if (openPositions.Count <= 0)
        {
            Debug.LogError("WorldMap_Data::GetOpenMainPosition() - No available open positions!");
        }

        return openPositions.GetRandom(rng);
    }

    public Coord GetOpenPos_Conditional(Predicate<MapInfo> p)
    {
        List<Coord> openPositions = new List<Coord>();

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (GrassTile(x, y) && !tiles[x, y].HasLandmark() && p(tiles[x, y]))
                {
                    openPositions.Add(new Coord(x, y));
                }
            }
        }

        return openPositions.GetRandom(rng);
    }

    public Coord GetLandmark(string search)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y].landmark == search)
                {
                    return new Coord(x, y);
                }
            }
        }

        return null;
    }

    public Coord GetRandomLandmark(string search)
    {
        List<Coord> cs = new List<Coord>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y].landmark == search)
                {
                    cs.Add(new Coord(x, y));
                }
            }
        }

        return cs[rng.Next(0, cs.Count)];
    }

    public bool TryGetLandmark(string search, Biome biome, out Coord c)
    {
        List<Coord> cs = new List<Coord>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y].landmark == search && tiles[x, y].biome == biome)
                {
                    cs.Add(new Coord(x, y));
                }
            }
        }

        if (!cs.Empty())
        {
            c = cs[rng.Next(0, cs.Count)];
            return true;
        }

        c = new Coord();
        return false;
    }

    public Coord GetOpenPosition(Biome b)
    {
        List<Coord> c = new List<Coord>();

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (b ==tiles[x, y].biome && !tiles[x, y].HasLandmark())
                {
                    c.Add(new Coord(x, y));
                }
            }
        }

        return c.GetRandom(rng);
    }

    public Coord GetClosestBiome(Biome b)
    {
        Coord closest = new Coord(1000, 1000);
        float dist = Mathf.Infinity;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Coord c = new Coord(x, y);

                if (tiles[x, y].biome == b)
                {
                    float d = c.DistanceTo(World.tileMap.WorldPosition);

                    if (d < dist)
                    {
                        closest = c;
                        dist = d;
                    }
                }
            }
        }

        return closest;
    }

    public Coord GetClosestLandmark(string land)
    {
        Coord closest = new Coord(1000, 1000);
        float dist = Mathf.Infinity;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Coord c = new Coord(x, y);

                if (tiles[x, y].landmark == land)
                {
                    float d = c.DistanceTo(World.tileMap.WorldPosition);

                    if (d < dist)
                    {
                        closest = c;
                        dist = d;
                    }
                }
            }
        }

        return closest;
    }

    public Coord GetRandomFromBiome(Biome b)
    {
        List<Coord> coords = new List<Coord>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y].biome == b)
                {
                    coords.Add(new Coord(x, y));
                }
            }
        }

        return coords.GetRandom(rng);
    }

    void Swamp()
    {
        Coord start = GetOpenPosition(new ZoneBlueprint[0]);
        tiles[start.x, start.y].biome = Biome.Swamp;
        tiles[start.x, start.y].radiation = 3;
        int width = rng.Next(3, 8), height = rng.Next(3, 8);
        int left = start.x, bottom = start.y;

        Room r = new Room(width, height, left, bottom);

        for (int x = left; x < r.right; x++)
        {
            for (int y = bottom; y < r.top; y++)
            {
                if (x <= 0 || x >= Manager.worldMapSize.x - 1 || y <= 0 || y >= Manager.worldMapSize.y - 1 ||
                    (x == left || x == r.right - 1 || y == bottom || y == r.top - 1) && rng.Next(0, 101) < 60)
                {
                    continue;
                }

                Biome b = tiles[x, y].biome;

                if (b != Biome.Ocean && b != Biome.Shore && b != Biome.Mountain)
                {
                    tiles[x, y].biome = Biome.Swamp;
                    tiles[x, y].radiation = 3;
                }
            }
        }
    }

    public MapInfo GetTileAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return new MapInfo(new Coord(x, y), Biome.Ocean);
        }

        return tiles[x, y];
    }

    public Village_Data GetVillageAt(int x, int y)
    {
        return (villages.Find(v => v.mapPosition.x == x && v.mapPosition.y == y));
    }

    void SetupPathfindingGrid()
    {
        tileData = new Path_TileData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tileData[x, y] = new Path_TileData(tiles[x, y].Walkable(), new Coord(x, y), 0);
            }
        }
    }

    public Path_TileData GetPathDataAt(int x, int y)
    {
        if (x >= width || y >= height || x < 0 || y < 0)
        {
            return null;
        }

        if (tileData[x, y] == null)
        {
            tileData[x, y] = new Path_TileData(tiles[x, y].Walkable(), new Coord(x, y), 0);
        }

        return tileData[x, y];
    }

    public ZoneBlueprint GetZone(string search)
    {
        foreach (ZoneBlueprint bp in GameData.GetAll<ZoneBlueprint>())
        {
            if (bp.ID == search)
                return bp;

            if (bp.neighbors != null)
            {
                foreach (ZoneBlueprint n in bp.neighbors)
                {
                    if (n.ID == search)
                        return n;
                }
            }
        }

        //Debug.LogError("No ZoneBlueprint with the ID \"" + search + "\".");
        return null;
    }

    public ZoneBlueprint_Underground GetUndergroundFromLandmark(string search)
    {
        ZoneBlueprint zb = GetZone(search);

        if (zb != null && !string.IsNullOrEmpty(zb.underground))
        {
            return GameData.Get<ZoneBlueprint_Underground>(zb.underground);
        }

        Debug.LogError("Underground area \"" + search + "\" does not exist.");

        return null;
    }

    public ZoneBlueprint_Underground GetUnderground(string search)
    {
        return GameData.Get<ZoneBlueprint_Underground>(search);
    }

    public string GetZoneNameAt(int x, int y, int ele)
    {
        if (ele != 0)
        {
            return World.tileMap.GetVaultAt(new Coord(x, y)).blueprint.name + " -" + Mathf.Abs(ele).ToString();
        }

        MapInfo mi = tiles[x, y];

        if (mi.HasLandmark())
        {
            if (mi.landmark == "River")
            {
                return mi.biome.ToString();
            }
            else if (mi.landmark == "Village")
            {
                string s = LocalizationManager.GetContent("loc_village");

                if (s.Contains("[NAME]"))
                {
                    s = s.Replace("[NAME]", GetVillageAt(x, y).name);
                }

                return s;
            }
            else
            {
                return GetZone(mi.landmark).name;
            }
        }

        string id = LocalizationManager.GetContent("Biome_" + mi.biome.ToString());
        return id;
    }

    public void RemoveLandmark(Coord c)
    {
        tiles[c.x, c.y].landmark = null;

        for (int i = 0; i < postGenLandmarks.Count; i++)
        {
            if (postGenLandmarks[i].pos == c)
            {
                postGenLandmarks.RemoveAt(i);
                return;
            }
        }
    }
}

public struct MapInfo
{
    public Biome biome;
    public string landmark;
    public Coord position;
    public bool friendly;
    public int radiation;

    public MapInfo(Coord pos, Biome b)
    {
        position = pos;
        biome = b;
        landmark = "";
        radiation = 0;
        friendly = false;
    }

    public bool HasLandmark()
    {
        return (!string.IsNullOrEmpty(landmark));
    }

    public bool Walkable()
    {
        return biome != Biome.Mountain;
    }

    public static bool BiomeHasEdge(Biome b)
    {
        return b == Biome.Mountain || b == Biome.Tundra || b == Biome.Ocean || b == Biome.Desert;
    }
}

[Serializable]
public struct Landmark
{
    public Coord pos;
    public string desc;

    public Landmark(Coord pos, string desc = "")
    {
        this.pos = pos;
        this.desc = desc;
    }
}
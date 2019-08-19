using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;

[MoonSharp.Interpreter.MoonSharpUserData]
public class TileMap : MonoBehaviour
{
    public static string imagePath;
    public static bool doneSetup = false;

    public int worldCoordX, worldCoordY;
    public int currentElevation;
    public Path_TileGraph tileGraph;
    public TileRenderer[,] tileRenderers;
    public List<Vault> Vaults { get; protected set; }
    public Dictionary<Coord, MapFeatures> mapFeatures;
    public event Func<TileMap_Data, TileMap_Data, bool> OnScreenChange;

    [HideInInspector] public Cell[,] cells;
    [HideInInspector] public WorldMap_Data worldMap;
    [HideInInspector] public OldScreens worldData;

    public GameObject tilePrefab;

    Sprite[] sprites;
    Coord startCoord;
    int size_x, size_y;
    TileMap_Data currentMap;
    TileMap_Data[,] screens;
    TilesetManager tilesetManager;

    public TileMap_Data CurrentMap
    {
        get { return currentMap; }
        set
        {
            if (currentMap != value)
            {
                if (World.turnManager != null && currentMap != null)
                {
                    currentMap.lastTurnSeen = World.turnManager.turn;
                }

                TileMap_Data old = currentMap;

                currentMap = value;

                if (OnScreenChange != null)
                {
                    OnScreenChange(old, currentMap);
                }
            }

            tileGraph = null;
        }
    }

    public Coord WorldPosition
    {
        get { return new Coord(worldCoordX, worldCoordY); }
    }

    public void SetMapSize(int x, int y)
    {
        size_x = x;
        size_y = y;
    }

    public void Init()
    {
        sprites = LoadImageFromStreamingAssets();
        CacheVars();
        SetupTileRenderers();

        currentElevation = Manager.startElevation;
        startCoord = Manager.newGame ? worldMap.startPosition : worldData.GetStartPos();
        worldCoordX = startCoord.x;
        worldCoordY = startCoord.y;
        InitializeVaults();

        if (Manager.newGame)
        {
            SpawnController.SpawnStaticNPCs();
        }

        if (WorldMap_Data.featuresToAdd != null)
        {
            for (int i = 0; i < WorldMap_Data.featuresToAdd.Count; i++)
            {
                SMapFeature s = WorldMap_Data.featuresToAdd[i];

                for (int j = 0; j < s.feats.Length; j++)
                {
                    AddMapFeature(new Coord(s.x, s.y), s.feats[j]);
                }
            }
        }

        WorldMap_Data.featuresToAdd = null;

        HardRebuild();
        CheckNPCTiles();
        doneSetup = true;
    }

    public void LoadMap(string mapName)
    {
        screens[worldCoordX, worldCoordY] = CurrentMap = new TileMap_Data(mapName, true);
        HardRebuild();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            StartCoroutine(CaptureScreenshot());
        }
    }

    public static Sprite[] LoadImageFromStreamingAssets()
    {
        byte[] imageBytes = File.ReadAllBytes(Application.streamingAssetsPath + imagePath);
        Texture2D tex = new Texture2D(169, 186, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        tex.LoadImage(imageBytes);

        int width = tex.width / Manager.tileResolution;
        int height = tex.height / Manager.tileResolution;
        Sprite[] ss = new Sprite[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Rect r = new Rect(x * Manager.tileResolution, y * Manager.tileResolution, Manager.tileResolution, Manager.tileResolution);

                r.x += (x != 0) ? x : 0;
                r.y += (y != 0) ? y : 0;

                Sprite s = Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), Manager.tileResolution);
                ss[width * y + x] = s;
            }
        }

        return ss;
    }

    public static IEnumerator CaptureScreenshot()
    {
        yield return new WaitForEndOfFrame();
        string scPath = Application.persistentDataPath + "/Screenshots";

        if (!Directory.Exists(scPath))
        {
            Directory.CreateDirectory(scPath);
        }

        DateTime dt = DateTime.Now;
        string path = Path.Combine(scPath, string.Format("Axu-{0}-{1}-{2}-{3}{4}{5}.png", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second));
        Texture2D screenImage = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        screenImage.Apply();

        byte[] imageBytes = screenImage.EncodeToPNG();

        File.WriteAllBytes(path, imageBytes);

        CombatLog.NewMessage("Screenshot saved to: " + path);
    }

    void SetupTileRenderers()
    {
        for (int x = 0; x < size_x; x++)
        {
            for (int y = 0; y < size_y; y++)
            {
                cells[x, y] = new Cell(new Coord(x, y));
                GameObject t = Instantiate(tilePrefab, new Vector3(x + 0.5f, y - (Manager.localMapSize.y - 0.5f), 0), Quaternion.identity, transform);
                tileRenderers[x, y] = t.GetComponent<TileRenderer>();
                tileRenderers[x, y].GiveCoords(x, y);
            }
        }
    }

    void InitializeVaults()
    {
        //Create vaults.
        for (int i = 0; i < worldMap.vaultAreas.Count; i++)
        {
            Coord vPos = worldMap.vaultAreas[i];

            string landmark = worldMap.GetTileAt(worldMap.vaultAreas[i].x, worldMap.vaultAreas[i].y).landmark;
            if (string.IsNullOrEmpty(landmark))
                continue;

            ZoneBlueprint zb = worldMap.GetZone(worldMap.GetTileAt(worldMap.vaultAreas[i].x, worldMap.vaultAreas[i].y).landmark);
            if (string.IsNullOrEmpty(zb.underground))
                continue;

            ZoneBlueprint_Underground vault = worldMap.GetUndergroundFromLandmark(zb.ID);
            Vault v = new Vault(worldMap.vaultAreas[i], vault);

            Vaults.Add(v);

        }
    }

    public void UpdateMapFeatures()
    {
        if (!mapFeatures.ContainsKey(WorldPosition))
        {
            mapFeatures.Add(WorldPosition, new MapFeatures(worldCoordX, worldCoordY));
        }

        TileMap_Data dat = null;

        if (screens[worldCoordX, worldCoordY] != null)
        {
            dat = screens[worldCoordX, worldCoordY];
        }

        mapFeatures[WorldPosition].SetupFeatureList(dat, World.objectManager.onScreenMapObjects, World.objectManager.NPCsInScreen(WorldPosition, 0));
    }

    public void AddMapFeature(string s)
    {
        mapFeatures[WorldPosition].AddFeature(s);
        UpdateMapFeatures();
    }

    //Called when loading world data.
    public void AddMapFeature(Coord c, string s)
    {
        if (!mapFeatures.ContainsKey(c))
        {
            mapFeatures.Add(c, new MapFeatures(c.x, c.y));
        }

        mapFeatures[c].AddFeature(s);
    }

    public void RemoveMapFeature(string s)
    {
        mapFeatures[WorldPosition].RemoveFeature(s);
        UpdateMapFeatures();
    }

    public void SetPosition(Coord pos, int elev = 0)
    {
        worldCoordX = pos.x;
        worldCoordY = pos.y;
        currentElevation = elev;
    }

    public void LightCheck()
    {
        for (int y = 0; y < size_y; y++)
        {
            for (int x = 0; x < size_x; x++)
            {
                cells[x, y].UpdateInSight(false, CurrentMap.has_seen[x, y]);
            }
        }

        List<Coord> cos = ShadowCasting.GetVisibleCells();

        foreach (Coord c in cos)
        {
            CurrentMap.has_seen[c.x, c.y] = true;
            cells[c.x, c.y].UpdateInSight(true, CurrentMap.has_seen[c.x, c.y]);
        }

        for (int y = 0; y < size_y; y++)
        {
            for (int x = 0; x < size_x; x++)
            {
                tileRenderers[x, y].SetParams(cells[x, y].InSight, CurrentMap.has_seen[x, y]);
            }
        }
    }

    #region Rebuild Map Functions
    public void SoftRebuild()
    {
        RebuildMap(worldCoordX, worldCoordY, currentElevation);
    }
    public void HardRebuild()
    {
        RebuildTexture(worldCoordX, worldCoordY, currentElevation, true);
    }
    public void HardRebuild_NoLight()
    {
        RebuildTexture(worldCoordX, worldCoordY, currentElevation, false);
    }

    //Used if you don't want to clear objects and enemies
    void RebuildMap(int mx, int my, int elevation)
    {
        Rebuild(mx, my, elevation);
        DrawTiles(false);
        LightCheck();
    }

    //normal rebuild. Rebuilds map, destroys all objects on screen, and respawns them.
    void RebuildTexture(int mx, int my, int elevation, bool lightCheck)
    {
        World.objectManager.RemoveNPCInstances();
        World.objectManager.RemoveObjectInstances();

        Rebuild(mx, my, elevation, lightCheck);
        DrawTiles(true);
        CheckNPCTiles();

        if (lightCheck)
        {
            LightCheck();
        }
    }

    //The base rebuild function
    void Rebuild(int mx, int my, int elevation, bool lightCheck = true)
    {
        Coord worldPos = new Coord(mx, my);
        bool exists = false;
        currentElevation = elevation;

        if (currentElevation != 0)
        {
            exists = (worldData != null && worldData.dataExists(mx, my, Mathf.Abs(currentElevation)));
            CurrentMap = GetVaultAt(worldPos).GetLevel(Mathf.Abs(currentElevation), exists);
        }
        else
        {
            if (screens[mx, my] == null)
            {
                exists = (worldData != null && worldData.dataExists(mx, my));
                screens[mx, my] = CurrentMap = new TileMap_Data(mx, my, currentElevation, exists);

                ApplyMapChanges(exists, mx, my, worldPos);
            }
            else
            {
                CurrentMap = screens[mx, my];
                exists = true;
            }
        }

        if (!CurrentMap.visited)
        {
            SpawnController.BiomeSpawn(mx, my, CurrentMap);
        }

        CurrentMap.visited = true;
        World.objectManager.CheckFollowers();

        if (lightCheck)
        {
            LightCheck();
        }

        World.objectManager.CheckFollowers();
    }

    void ApplyMapChanges(bool exists, int mx, int my, Coord worldPos)
    {
        //For mountains, current position set to walkable.
        if (ObjectManager.playerEntity != null)
        {
            if (screens[mx, my].GetTileNumAt(ObjectManager.playerEntity.posX, ObjectManager.playerEntity.posY) == Tile.tiles["Mountain"].ID)
            {
                screens[mx, my].ChangeTile(ObjectManager.playerEntity.posX, ObjectManager.playerEntity.posY, Tile.tiles["Mountain_Floor"]);
            }
        }

        if (exists)
        {
            List<TileMap_Data.TileChange> changes = worldData.GetChanges(worldPos);

            for (int i = 0; i < changes.Count; i++)
            {
                CurrentMap.ChangeTile(changes[i].x, changes[i].y, Tile.GetByID(changes[i].tType));
            }
        }
    }
    #endregion

    //Right now only goes home
    public void GoToArea(string name)
    {
        Coord c = WorldPosition;

        currentElevation = (name == "Workshop") ? -5 : 0;
        ObjectManager.playerEntity.posX = Manager.localMapSize.x / 2;
        ObjectManager.playerEntity.posY = 2;
        ObjectManager.playerEntity.ForcePosition();

        if (name == "Home")
            c = worldMap.GetLandmark("Home");
        else if (name == "Home_Base")
        {
            if (ObjectManager.playerJournal.HasFlag(ProgressFlags.Found_Base))
                c = worldMap.GetLandmark("Home");
            else
                c = worldMap.GetLandmark("Abandoned Building");
        }

        worldCoordX = c.x;
        worldCoordY = c.y;

        ObjectManager.playerEntity.BeamDown();
        ObjectManager.player.GetComponent<PlayerInput>().CheckMinimap();
    }

    public Cell GetCellAt(Coord c)
    {
        if (c.x < 0 || c.x > size_x || c.y < 0 || c.y > size_y)
            return null;

        return cells[c.x, c.y];
    }

    public Cell GetCellAt(int x, int y)
    {
        if (x < 0 || x >= size_x || y < 0 || y >= size_y)
            return null;

        return cells[x, y];
    }

    //empty tiles in current map
    public Coord GetRandomPosition()
    {
        return CurrentMap.GetRandomFloorTile();
    }

    public Coord EmptyAdjacent(Coord c)
    {
        List<Coord> empty = new List<Coord>();

        for (int x = c.x - 1; x <= c.x + 1; x++)
        {
            for (int y = c.y - 1; y <= c.y + 1; y++)
            {
                if (cells[x, y].Walkable && cells[x, y].entity == null)
                {
                    empty.Add(new Coord(x, y));
                }
            }
        }

        if (empty.Count <= 0)
        {
            return null;
        }

        return empty.GetRandom();
    }

    public bool IsWaterTile(int x, int y)
    {
        if (y < size_y && x < size_x && x >= 0 && y >= 0)
            return CurrentMap.IsWaterTile(x, y);

        return false;
    }

    public Coord FindStairsDown()
    {
        for (int x = 0; x < size_x; x++)
        {
            for (int y = 0; y < size_y; y++)
            {
                if (CurrentMap.map_data[x, y] == Tile.tiles["Stairs_Down"])
                    return new Coord(x, y);
            }
        }

        return null;
    }

    public Coord FindStairsUp()
    {
        for (int x = 0; x < size_x; x++)
        {
            for (int y = 0; y < size_y; y++)
            {
                if (CurrentMap.map_data[x, y] == Tile.tiles["Stairs_Up"])
                    return new Coord(x, y);
            }
        }

        return null;
    }

    public Vault GetVaultAt(Coord myPos)
    {
        if (Vaults.Find(x => x.position == myPos) == null)
        {
            if (worldMap.GetTileAt(myPos.x, myPos.y).landmark == "Village")
            {
                Vaults.Add(new Vault(myPos, worldMap.GetUnderground("Cellar")));
            }
            else
            {
                Vaults.Add(new Vault(myPos, worldMap.GetUnderground("Default")));
            }
        }

        return Vaults.Find(x => x.position == myPos);
    }

    /// <summary>
    /// Checks to see if a tile on the local map can be traversed by the player.
    /// </summary>
    public bool WalkableTile(int x, int y)
    {
        return CurrentMap.WalkableTile(x, y);
    }
    public bool WalkableTile(Coord c)
    {
        return CurrentMap.WalkableTile(c.x, c.y);
    }

    public bool IsTileLit(int x, int y)
    {
        if (x < 0 || x >= Manager.localMapSize.x || y < 0 || y >= Manager.localMapSize.y)
            return false;

        return tileRenderers[x, y].lit;
    }

    /// <summary>
    /// Dictates whether or not things can pass over this tile (if you are flying, you can. projectiles can, etc.)
    /// </summary>
    public bool PassThroughableTile(int x, int y)
    {
        if (x < 0 || y < 0 || x > size_x - 1 || y > size_y - 1)
            return false;

        bool walkable = CurrentMap.WalkableTile(x, y);

        return walkable;
    }

    /// <summary>
    /// Checks to see if a tile on the world map can be traversed by the player.
    /// </summary>
    public bool WalkableWorldTile(int x, int y)
    {
        if (x < 0 || x >= Manager.worldMapSize.x || y < 0 || y >= Manager.worldMapSize.y)
            return false;


        return (worldMap.tileData[x, y].walkable);
    }

    public bool IsOceanWorldTile(int x, int y)
    {
        return worldMap.GetTileAt(x, y).biome == WorldMap.Biome.Ocean;
    }

    public List<Coord> InSightCoords()
    {
        List<Coord> coordList = new List<Coord>();

        for (int y = 0; y < size_y; y++)
        {
            for (int x = 0; x < size_x; x++)
            {
                if (ObjectManager.playerEntity.inSight(new Coord(x, y)) && WalkableTile(x, y))
                    coordList.Add(new Coord(x, y));
            }
        }

        return coordList;
    }

    /// <summary>
    /// Asks if the tile can have light pass through it.
    /// </summary>
    public bool LightPassableTile(int x, int y)
    {
        if (World.OutOfLocalBounds(x, y))
            return false;

        Tile_Data tData = CurrentMap.map_data[x, y];

        if (CurrentMap.WalkableTile(x, y))
        {
            if (tData.ID == Tile.tiles["Door"].ID)
            {
                Cell c = GetCellAt(new Coord(x, y));

                if (c.mapObjects.Count <= 0)
                    return false;

                return (c.mapObjects.FindAll(m => m.isDoor_Closed).Count == 0);
            }
            else
                return true;
        }

        return tData.HasTag("See Through");
    }

    /// <summary>
    /// Checks to see if the player has hit the edge of the map. If so, switch to the local screen in the proper direction.
    /// </summary>
    public bool CheckEdgeLocalMap(int x, int y)
    {
        if (World.OutOfLocalBounds(x, y))
        {
            Entity playerEntity = ObjectManager.playerEntity;

            if (playerEntity.walkDirection != null || currentElevation != 0)
            {
                playerEntity.CancelWalk();
                return false;
            }

            PlayerInput pi = playerEntity.GetComponent<PlayerInput>();
            pi.BringNPCs1();

            if (x < 0 && worldCoordX > 0)
            {
                worldCoordX--;
                playerEntity.posX = size_x;
            }
            else if (x >= size_x && worldCoordX < Manager.worldMapSize.x - 1)
            {
                worldCoordX++;
                playerEntity.posX = -1;
            }

            if (y < 0 && worldCoordY > 0)
            {
                worldCoordY--;
                playerEntity.posY = size_y;
            }
            else if (y >= size_y && worldCoordY < Manager.worldMapSize.y - 1)
            {
                worldCoordY++;
                playerEntity.posY = -1;
            }

            playerEntity.ForcePosition();

            HardRebuild();
            pi.BringNPCs2();

            return true;
        }
        return false;
    }

    //Check the locations of all NPCs against their world position, same for map objects
    public void CheckNPCTiles()
    {
        World.objectManager.CheckOnScreenNPCs();
    }

    public bool DigTile(int x, int y, bool canDestroyBuiltWalls)
    {
        if (World.OutOfLocalBounds(x, y) || CurrentMap.WalkableTile(x, y) || !CurrentMap.map_data[x, y].CanDig(canDestroyBuiltWalls))
            return false;

        SetTile(Tile.tiles["Mountain_Floor"], x, y);
        HardRebuild();
        return true;
    }

    public void FreezeTile(int x, int y)
    {
        if (World.OutOfLocalBounds(x, y) || !Tile.isWaterTile(CurrentMap.GetTileNumAt(x, y), true) && CurrentMap.GetTileNumAt(x, y) != Tile.tiles["Lava"].ID)
            return;

        int tileNum = CurrentMap.GetTileNumAt(x, y);
        Tile_Data newTile = (tileNum == Tile.tiles["Lava"].ID) ? Tile.tiles["Mountain_Floor"] : Tile.tiles["Ice"];

        SetTile(newTile, x, y);
        SoftRebuild();
    }

    //Change a tile to another type.
    public bool SetTile(Tile_Data tile, int x, int y)
    {
        if (World.OutOfLocalBounds(x, y))
            return false;

        CurrentMap.ChangeTile(x, y, tile);
        tileRenderers[x, y].SetSprite(sprites[tile.ID]);
        LightCheck();
        CurrentMap.SetUpTileData();
        return true;
    }

    public void SetTileGraphic_AlphaMask(TileMap_Data tm, int x, int y, int autoNum, int replaceID)
    {
        Texture2D tex = new Texture2D(16, 16, TextureFormat.ARGB32, true) { filterMode = FilterMode.Point };
        
        Color[] c1 = sprites[tm.map_data[x, y].ID].GetPixels(), c2 = sprites[replaceID].GetPixels();
        Color[] alphaMask = tilesetManager.GetTileSet(21).Autotile[autoNum].GetPixels();
        Color[] newC = new Color[c2.Length];

        for (int i = 0; i < c2.Length; i++)
        {
            newC[i] = Color.Lerp(c1[i], c2[i], alphaMask[i].a);
        }

        tex.SetPixels(newC);
        tex.Apply();

        Sprite s = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        tileRenderers[x, y].SetSprite(s);
    }

    public void SetTileGraphic(TileMap_Data tm, int x, int y, int tsIndex, int autoNum)
    {
        if (tm != currentMap)
            return;

        int tileIndex = autoNum;

        if (tsIndex == 0)
        {
            if (currentMap.mapInfo.biome == WorldMap.Biome.Forest)
                tsIndex = 1;
            else if (currentMap.mapInfo.biome == WorldMap.Biome.Shore || currentMap.IsMagna())
                tsIndex = 3;
            else if (currentMap.mapInfo.biome == WorldMap.Biome.Swamp)
                tsIndex = 4;
            else if (currentMap.mapInfo.biome == WorldMap.Biome.Desert)
                tsIndex = 6;
            else if (currentMap.mapInfo.biome == WorldMap.Biome.Tundra)
                tsIndex = 17;
            else if (currentMap.mapInfo.biome == WorldMap.Biome.Mountain)
                tsIndex = 8;
        }
        else if (tsIndex == 21)
        {
            SetTileGraphic_AlphaMask(tm, x, y, autoNum, 32);
            return;
        }

        Tileset ts = tilesetManager.GetTileSet(tsIndex);

        if (tileIndex == 15 && SeedManager.localRandom.CoinFlip())
        {
            if (SeedManager.localRandom.Next(100) < 85 && ts.Norm.Length > 0)
                tileRenderers[x, y].SetSprite(sprites[ts.Norm.GetRandom(SeedManager.localRandom)]);
            else if (ts.Rare.Length > 0)
                tileRenderers[x, y].SetSprite(sprites[ts.Rare.GetRandom(SeedManager.localRandom)]);
            else
                tileRenderers[x, y].SetSprite(ts.Autotile[autoNum]);
        }
        else
        {
            tileRenderers[x, y].SetSprite(ts.Autotile[autoNum]);
        }
    }

    public int GetTileID(int x, int y)
    {
        return CurrentMap.GetTileNumAt(x, y);
    }

    public string TileName(int x = -1, int y = -1)
    {
        if (x == -1 && y == -1)
        {
            x = worldCoordX;
            y = worldCoordY;
        }

        return worldMap.GetZoneNameAt(x, y, currentElevation);
    }

    public List<Landmark> NearbyLandmarks(int distMax)
    {
        List<Landmark> lms = new List<Landmark>();

        for (int i = 0; i < worldMap.landmarks.Count; i++)
        {
            if (Vector2.Distance(WorldPosition.toVector2(), worldMap.landmarks[i].pos.toVector2()) < distMax)
            {
                lms.Add(worldMap.landmarks[i]);
            }
        }

        return lms;
    }

    public Coord ClosestTown()
    {
        return worldMap.GetClosestLandmark("Village");
    }

    public Landmark GetRandomLandmark()
    {
        return worldMap.landmarks.GetRandom(SeedManager.textRandom);
    }

    public void RevealMap()
    {
        for (int x = 0; x < size_x; x++)
        {
            for (int y = 0; y < size_y; y++)
            {
                CurrentMap.has_seen[x, y] = true;
            }
        }

        SoftRebuild();
    }

    void DrawTiles(bool reAutotile)
    {
        if (reAutotile)
        {
            for (int y = 0; y < size_y; y++)
            {
                for (int x = 0; x < size_x; x++)
                {
                    int tileNum = CurrentMap.GetTileNumAt(x, y);
                    tileRenderers[x, y].SetSprite(sprites[tileNum]);
                }
            }

            SeedManager.CoordinateSeed(worldCoordX, worldCoordY, currentElevation);
            CurrentMap.Autotile();
        }
    }

    public TileMap_Data GetScreen(int x, int y)
    {
        if (screens[x, y] == null && worldData != null && worldData.dataExists(x, y))
        {
            screens[x, y] = new TileMap_Data(x, y, 0, worldData.dataExists(x, y));
        }

        return screens[x, y];
    }

    public void DeleteScreen(int x, int y)
    {
        if (x == worldCoordX && y == worldCoordY)
        {
            Debug.LogError("Attempting to delete current loaded map. Aborting.");
            return;
        }

        screens[x, y] = null;
    }

    public List<SMapFeature> GetCustomFeatures()
    {
        List<SMapFeature> fs = new List<SMapFeature>();

        foreach (KeyValuePair<Coord, MapFeatures> m in mapFeatures)
        {
            if (m.Value.HasCustomFeatures())
            {
                fs.Add(m.Value.CustomFeatureList());
            }
        }

        return fs;
    }

    void CacheVars()
    {
        World.tileMap = this;
        cells = new Cell[Manager.localMapSize.x, Manager.localMapSize.y];
        screens = new TileMap_Data[Manager.worldMapSize.x, Manager.worldMapSize.y];
        tileRenderers = new TileRenderer[size_x, size_y];
        Vaults = new List<Vault>();
        mapFeatures = new Dictionary<Coord, MapFeatures>();

        worldMap = GameObject.FindWithTag("WorldMap").GetComponent<WorldMap>().worldMapData;
        tilesetManager = GetComponent<TilesetManager>();
    }
}
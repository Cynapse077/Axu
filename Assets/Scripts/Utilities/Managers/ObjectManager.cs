using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class ObjectManager : MonoBehaviour
{
    public static GameObject player;
    public static Entity playerEntity;
    public static Journal playerJournal;
    public static bool doneLoading = false;
    public static int SpawnedNPCs = 0;

    public GameObject playerPrefab;
    public GameObject npcPrefab;
    public GameObject objectPrefab;
    public GameObject mapIcon;
    public Sprite[] mapIconSprites;
    public GameObject sideArrows;

    WorldMap worldMap;
    TurnManager turnManager;
    TileMap tileMap;

    [HideInInspector] public List<Entity> onScreenNPCObjects;
    [HideInInspector] public List<NPC> npcClasses;
    [HideInInspector] public List<GameObject> onScreenMapObjects = new List<GameObject>();
    [HideInInspector] public List<MapObject> mapObjects = new List<MapObject>();
    [HideInInspector] public Dictionary<Coord, GameObject> mapIconGameObjects = new Dictionary<Coord, GameObject>();

    #region "Initialization"
    void Start()
    {
        if (!ModManager.PreInitialized)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            return;
        }

        World.objectManager = this;
        onScreenNPCObjects = new List<Entity>();
        StartCoroutine("Initialize", Manager.playerName);
    }

    void FindStatics()
    {
        turnManager = GetComponent<TurnManager>();
        worldMap = GameObject.FindObjectOfType<WorldMap>();
        tileMap = GameObject.FindObjectOfType<TileMap>();

    }

    IEnumerator Initialize(string playerName)
    {
        GetComponent<UserInterface>().loadingGO.SetActive(true);

        if (!Manager.newGame)
        {
            GetComponent<SaveData>().LoadPlayer(playerName);
        }

        CreateWorldMap();

        while (!worldMap.worldMapData.doneLoading)
        {
            yield return null;
        }

        ModManager.PostLoadData();

        worldMap.BuildTexture();

        CreateLocalMap();
        SpawnPlayer();

        turnManager = GetComponent<TurnManager>();
        turnManager.Init();

        UserInterface.loading = false;
        World.tileMap.OnScreenChange += CheckArrowsEnabled;
        CheckArrowsEnabled(null, World.tileMap.CurrentMap);
        GameObject.FindObjectOfType<SidePanelUI>().Init();
        MyConsole.NewMessageColor("Type \"?\" or \"help\" to show a list of commands", Color.cyan);

        World.userInterface.ShowInitialMessage(Manager.playerName);
        doneLoading = true;
        GetComponent<MusicManager>().Init(World.tileMap.CurrentMap);
    }

    void CreateWorldMap()
    {
        worldMap = GameObject.FindObjectOfType<WorldMap>();
        worldMap.Init();
    }

    public void CreateLocalMap()
    {
        tileMap = GameObject.FindObjectOfType<TileMap>();
        tileMap.SetMapSize(Manager.localMapSize.x, Manager.localMapSize.y);

        if (!Manager.newGame)
        {
            tileMap.worldData = new OldScreens();
        }

        tileMap.Init();
    }

    public void SpawnPlayer()
    {
        if (!Manager.newGame)
        {
            GetComponent<SaveData>().SetUpJournal();
        }

        player = Instantiate(playerPrefab);
        player.name = "Player";
        playerEntity = player.GetComponent<Entity>();
        playerEntity.myPos = new Coord(Manager.localStartPos.x, Manager.localStartPos.y);

        Camera.main.GetComponent<CameraControl>().Init();
    }
    #endregion

    public bool CanDiscard(int x, int y, int z)
    {
        if (x == World.tileMap.worldCoordX && y == World.tileMap.worldCoordY && z == World.tileMap.currentElevation)
        {
            return false;
        }

        Coord homePos = World.tileMap.worldMap.GetLandmark("Home");

        if (homePos.x == x && homePos.y == y && z == 0)
        {
            return false;
        }

        return true;
    }

    bool CheckArrowsEnabled(TileMap_Data oldMap, TileMap_Data newMap)
    {
        sideArrows.SetActive(newMap.elevation == 0 && Manager.localMapSize.x == 45 && Manager.localMapSize.y == 30);
        return true;
    }

    public Entity GetEntityFromNPC(NPC n)
    {
        return onScreenNPCObjects.Find(x => x.AI.npcBase == n);
    }

    public List<NPC> NPCsAt(Coord pos, int z = 0)
    {
        return npcClasses.FindAll(x => x.worldPosition == pos && x.elevation == z);
    }

    public List<MapObject> ObjectsAt(Coord pos, int z = 0)
    {
        return mapObjects.FindAll(x => x.worldPosition == pos && x.elevation == z);
    }

    public void DeleteNPCsAt(Coord pos, int z = 0)
    {
        List<NPC> npcs = new List<NPC>(npcClasses);

        foreach (NPC n in npcClasses)
        {
            if (n.worldPosition == pos && n.elevation == z)
            {
                npcs.Remove(n);
            }
        }

        npcClasses = npcs;

        GameObject newObject = new GameObject("temp");

        foreach (Entity e in onScreenNPCObjects)
        {
            if (!npcClasses.Contains(e.AI.npcBase))
            {
                e.transform.SetParent(newObject.transform);
            }
        }

        newObject.transform.DestroyChildren();
        Destroy(newObject);
    }

    public void DeleteObjectsAt(Coord pos, int z = 0)
    {
        List<MapObject> mos = new List<MapObject>(mapObjects);

        foreach (MapObject m in mapObjects)
        {
            if (m.worldPosition == pos && m.elevation == z)
            {
                mos.Remove(m);
            }
        }

        mapObjects = mos;

        GameObject newObject = new GameObject("temp");

        foreach (GameObject g in onScreenMapObjects)
        {
            if (!mapObjects.Contains(g.GetComponent<MapObjectSprite>().objectBase))
            {
                g.transform.SetParent(newObject.transform);
            }
        }

        newObject.transform.DestroyChildren();
        Destroy(newObject);
    }

    public bool NPCExists(string id)
    {
        return npcClasses.Find(x => x.ID == id) != null;
    }

    public NPC GetNPCByID(string id)
    {
        return npcClasses.Find(x => x.ID == id);
    }

    public NPC GetNPCByUID(int uid)
    {
        return npcClasses.Find(x => x.UID == uid);
    }

    //to spawn one on the current screen
    public BaseAI SpawnNPC(NPC npcB)
    {
        if (npcB == null || npcB.onScreen || npcB.localPosition == null)
        {
            return null;
        }

        if (!npcClasses.Contains(npcB))
        {
            if (npcB.HasFlag(NPC_Flags.Boss))
            {
                npcB.maxHealth += Mathf.Min(World.DangerLevel() / 10, 20);
            }

            npcClasses.Add(npcB);
        }

        if (npcB.worldPosition == tileMap.WorldPosition && npcB.elevation == tileMap.currentElevation)
        {
            //Apparently this check is important.
            for (int i = 0; i < onScreenNPCObjects.Count; i++)
            {
                if (onScreenNPCObjects[i] != null && npcB.localPosition == onScreenNPCObjects[i].myPos)
                {
                    return null;
                }
            }

            npcB.onScreen = true;
            GameObject newNPCObject = Instantiate(npcPrefab);

            newNPCObject.GetComponent<BaseAI>().npcBase = npcB;
            Entity npcEntity = newNPCObject.GetComponent<Entity>();
            npcEntity.ForcePosition(npcB.localPosition);
            onScreenNPCObjects.Add(npcEntity);

            return npcEntity.AI;
        }

        return null;
    }

    //When a creature spawns another creature.
    public void SummonNPC(NPC npcB, NPC summonerNPC)
    {
        if (npcB.onScreen)
        {
            return;
        }

        for (int i = 0; i < onScreenNPCObjects.Count; i++)
        {
            if (onScreenNPCObjects[i] == null || npcB.localPosition.x == summonerNPC.localPosition.x && npcB.localPosition.y == summonerNPC.localPosition.y)
            {
                return;
            }
        }

        npcClasses.Add(npcB);

        //Remove the summon flag, so you don't end up with a screen full of baddies.
        if (npcB.HasFlag(NPC_Flags.Summon_Adds))
        {
            npcB.flags.Remove(NPC_Flags.Summon_Adds);
        }

        npcB.faction = summonerNPC.faction;

        if (npcB.worldPosition == tileMap.WorldPosition && npcB.elevation == tileMap.currentElevation)
        {
            SpawnNPC(npcB);
        }
    }

    //Call this to check NPCs positions upon entering a screen, to make sure they don't overlap the player.
    public void NoStickNPCs()
    {
        for (int i = 0; i < onScreenNPCObjects.Count; i++)
        {
            Entity entity = onScreenNPCObjects[i];

            if (entity.posX == playerEntity.posX && entity.posY == playerEntity.posY)
            {
                List<Coord> adjacent = playerEntity.GetEmptyCoords();
                Coord c = (adjacent.Count == 0) ? World.tileMap.GetRandomPosition() : adjacent.GetRandom();
                entity.posX = c.x;
                entity.posY = c.y;
            }
        }
    }

    /// <summary>
    /// Only used in world initialization from file for now.
    /// </summary>
    public void CreateNPC(NPC npcB)
    {
        if (!npcClasses.Contains(npcB))
        {
            npcClasses.Add(npcB);
        }
    }

    //Clears the screen of NPCs
    public void RemoveNPCInstances()
    {
        for (int i = 0; i < onScreenNPCObjects.Count; i++)
        {
            if (onScreenNPCObjects[i] != null)
            {
                Destroy(onScreenNPCObjects[i].gameObject);
            }
        }

        onScreenNPCObjects.Clear();
    }

    public List<NPC> NPCsInScreen(Coord pos, int elev)
    {
        return npcClasses.FindAll(x => x.worldPosition == pos && x.elevation == elev);
    }

    public void SpawnExplosion(Entity spawner, Coord position)
    {
        Vector2 newPos = new Vector2(position.x, position.y - Manager.localMapSize.y);

        Instantiate(World.poolManager.abilityEffects[6], newPos, Quaternion.identity);
        TileDamage td = new TileDamage(spawner, position, new HashSet<DamageTypes>() { DamageTypes.Heat })
        {
            myName = LocalizationManager.GetContent("Explosion"),
            damage = Random.Range(4, 10)
        };
        td.ApplyDamage();
    }

    public void SpawnPoisonGas(Entity spawner, Coord position)
    {
        Vector2 newPos = new Vector2(position.x, position.y - Manager.localMapSize.y);

        Instantiate(World.poolManager.abilityEffects[2], newPos, Quaternion.identity);
        TileDamage td = new TileDamage(spawner, position, new HashSet<DamageTypes>() { DamageTypes.Venom })
        {
            myName = LocalizationManager.GetContent("Poisonous Gas"),
            damage = Random.Range(2, 5)
        };
        td.ApplyDamage();
    }

    public void SpawnEffect(int index, string effectName, Entity spawner, int x, int y, int dmg, Ability skill)
    {
        SpawnEffect(index, effectName, spawner, new Coord(x, y), dmg, skill, 0f);
    }

    public void SpawnEffect(int index, string effectName, Entity spawner, int x, int y, int dmg, Ability skill, float rotation)
    {
        SpawnEffect(index, effectName, spawner, new Coord(x, y), dmg, skill, rotation);
    }

    void SpawnEffect(int index, string effectName, Entity spawner, Coord position, int dmg, Ability skill, float rotation)
    {
        if (playerEntity.inSight(position))
        {
            Vector2 pos = new Vector2(position.x, position.y - Manager.localMapSize.y);
            GameObject effect = Instantiate(World.poolManager.abilityEffects[index], pos, Quaternion.identity);
            effect.name = effectName;

            if (index == 1)
                effect.transform.position += new Vector3(0.5f, 0.5f, 0);

            effect.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        }


        if (dmg > 0 || skill.damageType == DamageTypes.NonLethal)
        {
            HashSet<DamageTypes> dt = (skill == null) ? new HashSet<DamageTypes>() { DamageTypes.Energy } : new HashSet<DamageTypes>() { skill.damageType };
            TileDamage td = new TileDamage(spawner, position, dt)
            {
                damage = dmg,
                myName = effectName
            };
            td.ApplyDamage();
        }
    }

    public void SpawnShock(Entity spawner, Coord position, int dmg, float rotation)
    {
        if (playerEntity.inSight(position))
        {
            GameObject effect = Instantiate(World.poolManager.abilityEffects[1], position.toVector2(), Quaternion.identity);
            effect.name = LocalizationManager.GetContent("Shock");
            effect.transform.position += new Vector3(0.5f, 0.5f, 0);

            effect.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        }


        if (dmg > 0)
        {
            TileDamage td = new TileDamage(spawner, position, new HashSet<DamageTypes>() { DamageTypes.Energy })
            {
                damage = dmg,
                myName = LocalizationManager.GetContent("Shock")
            };
            td.ApplyDamage();
        }
    }

    public void CreatePoolOfLiquid(Coord localPos, Coord worldPos, int elev, string liquidID, int amount)
    {
        Cell c = World.tileMap.GetCellAt(localPos);
        Liquid newLiq = ItemList.GetLiquidByID(liquidID, amount);

        if (newLiq == null || c == null)
        {
            return;
        }

        if (c.GetPool() == null && !World.tileMap.IsWaterTile(localPos.x, localPos.y + Manager.localMapSize.y))
        {
            MapObject m = NewObjectAtSpecificScreen("Loot", localPos, World.tileMap.WorldPosition, World.tileMap.currentElevation);
            Item i = ItemList.GetItemByID("pool");
            i.GetCComponent<CLiquidContainer>().SetLiquid(newLiq);
            m.inv.Insert(0, i);
        }
        else
        {
            c.GetPool().GetCComponent<CLiquidContainer>().Fill(newLiq);
        }
    }

    //Spawn an object on the current screen.
    public MapObject NewObjectOnCurrentScreen(string type, Coord locPos)
    {
        for (int i = 0; i < onScreenMapObjects.Count; i++)
        {
            MapObjectSprite mos = onScreenMapObjects[i].GetComponent<MapObjectSprite>();

            if (mos.localPos == locPos)
            {
                return null;
            }
        }

        MapObject_Blueprint bp = GameData.Get<MapObject_Blueprint>(type);

        if (bp != null)
        {
            MapObject mapOb = new MapObject(bp, locPos, World.tileMap.WorldPosition, World.tileMap.currentElevation);
            mapObjects.Add(mapOb);

            SpawnObject(mapOb);

            return mapOb;
        }

        return null;
    }

    //Spawn an object at a specific screen
    public MapObject NewObjectAtSpecificScreen(string type, Coord locPos, Coord worPos, int elevation = 0)
    {
        MapObject_Blueprint bp = GameData.Get<MapObject_Blueprint>(type);

        if (bp != null)
        {
            MapObject mapOb = new MapObject(bp, new Coord(locPos.x, locPos.y), worPos, elevation);
            mapObjects.Add(mapOb);

            SpawnObject(mapOb);

            return mapOb;
        }

        return null;
    }

    public Inventory NewInventory(string type, Coord locPos, Coord worldPos, int elevation, Item item)
    {
        return NewInventory(type, locPos, worldPos, elevation, new List<Item>() { item });
    }

    //Create a new object with an inventory. Return its inventory component.
    public Inventory NewInventory(string type, Coord locPos, Coord worPos, int elevation, List<Item> items = null)
    {
        Cell c = World.tileMap.GetCellAt(locPos);

        if (c != null && items != null && c.HasInventory())
        {
            Inventory inv = c.MyInventory();

            for (int i = 0; i < items.Count; i++)
            {
                inv.PickupItem(new Item(items[i]));
            }

            return null;
        }

        MapObject_Blueprint bp = GameData.Get<MapObject_Blueprint>(type);

        if (bp != null)
        {
            MapObject mapOb = new MapObject(bp, new Coord(locPos.x, locPos.y), worPos, World.tileMap.currentElevation);

            if (items != null)
            {
                mapOb.inv = items;
            }

            mapObjects.Add(mapOb);

            mapOb.onScreen = true;
            GameObject newObject = Instantiate(objectPrefab, new Vector3(locPos.x, locPos.y, 0), Quaternion.identity);
            MapObjectSprite mos = newObject.GetComponent<MapObjectSprite>();
            mos.objectBase = mapOb;
            onScreenMapObjects.Add(newObject);

            Inventory myInv = newObject.GetComponent<Inventory>();
            return myInv;
        }

        return null;
    }

    public void AddMapObject(MapObject obj)
    {
        for (int i = 0; i < mapObjects.Count; i++)
        {
            if (obj != mapObjects[i] && mapObjects[i].worldPosition == obj.worldPosition && mapObjects[i].localPosition == obj.localPosition)
            {
                if (obj.inv != null && mapObjects[i].inv != null && obj.inv.Count > 0)
                {
                    for (int j = 0; j < obj.inv.Count; j++)
                    {
                        mapObjects[i].inv.Add(obj.inv[j]);
                    }

                    CheckMapObjectInventories();
                }
                if (obj.blueprint.objectType == mapObjects[i].blueprint.objectType && !mapObjects[i].blueprint.objectType.Contains("Bloodstain"))
                {
                    mapObjects.Remove(obj);
                    return;
                }
            }
        }

        if (!mapObjects.Contains(obj))
        {
            mapObjects.Add(obj);
        }
    }

    public void SpawnObject(MapObject obj)
    {
        if (!mapObjects.Contains(obj))
        {
            mapObjects.Add(obj);
        }

        if (obj.onScreen)
        {
            return;
        }

        if (obj.worldPosition != World.tileMap.WorldPosition || obj.elevation != World.tileMap.currentElevation)
        {
            obj.onScreen = false;
            return;
        }

        obj.onScreen = true;
        GameObject newObject = Instantiate(objectPrefab, new Vector3(obj.localPosition.x, obj.localPosition.y, 0), Quaternion.identity);
        newObject.GetComponent<MapObjectSprite>().objectBase = obj;

        onScreenMapObjects.Add(newObject);
    }

    /// <summary>
    /// Removes the object from ObjectManager's lists. Does not destroy the GameObject!
    /// </summary>
    public void RemoveObject(MapObject obj, GameObject go)
    {
        mapObjects.Remove(obj);
        onScreenMapObjects.Remove(go);
    }

    /// <summary>
    /// Remove all map objects on screen.
    /// </summary>
    public void RemoveObjectInstances()
    {
        for (int i = 0; i < onScreenMapObjects.Count; i++)
        {
            if (onScreenMapObjects[i] != null)
            {
                MapObjectSprite objectInstance = onScreenMapObjects[i].GetComponent<MapObjectSprite>();

                if (objectInstance.objectBase != null)
                {
                    objectInstance.objectBase.onScreen = false;
                    Destroy(onScreenMapObjects[i]);
                }
            }
        }

        onScreenMapObjects.Clear();
    }

    public Entity CheckEnitiyInTile(Coord pos)
    {
        if (World.tileMap.WalkableTile(pos.x, pos.y) && World.tileMap.GetCellAt(pos) != null)
        {
            return World.tileMap.GetCellAt(pos).entity;
        }

        return null;
    }

    //Spawn everything you need for a normal house, given coords of left, right, top, bottom
    public void BuildingObjects(House h, Coord worldPos, string npcID)
    {
        Coord localPos = h.GetRandomPosition();
        if (SeedManager.combatRandom.Next(100) < 20 && npcID == "villager")
        {
            localPos = World.tileMap.GetRandomPosition();
        }

        NPC newNPC = EntityList.GetNPCByID(npcID, worldPos, localPos);

        SpawnNPC(newNPC);

        if (newNPC.HasFlag(NPC_Flags.Merchant))
        {
            ShopObjects(h, worldPos);
        }
        else if (newNPC.HasFlag(NPC_Flags.Doctor))
        {
            HospitalObjects(h, worldPos);
        }
        else
        {
            HouseObjects(h, worldPos);
        }
    }

    void HouseObjects(House h, Coord worldPos)
    {
        Coord c = h.GetRandomPosition();
        NewObjectAtSpecificScreen("Bed", c, worldPos);
        Coord c2 = h.GetRandomPosition();
        int numTries = 0;

        while (c == c2 && numTries < 10)
        {
            c2 = h.GetRandomPosition();
            numTries++;
        }

        NewObjectAtSpecificScreen("Table", c2, worldPos);

        Coord c3 = h.GetRandomPosition();
        numTries = 0;

        while ((c3 == c2 || c3 == c) && numTries < 10)
        {
            c3 = h.GetRandomPosition();
            numTries++;
        }

        NewObjectAtSpecificScreen(SeedManager.localRandom.CoinFlip() ? "Chair_Left" : "Chair_Right", c3, worldPos);
    }

    void ShopObjects(House h, Coord worldPos)
    {
        List<Coord> takenPositions = new List<Coord>();

        for (int i = 0; i < SeedManager.localRandom.Next(2, 5); i++)
        {
            Coord c = h.GetRandomPosition();
            int tries = 0;

            while (takenPositions.Contains(c) && tries < 10)
            {
                c = h.GetRandomPosition();
                tries++;
            }

            takenPositions.Add(c);

            NewObjectAtSpecificScreen("Barrel", c, worldPos);
        }
    }

    void HospitalObjects(House h, Coord worldPos)
    {
        List<Coord> poses = h.Interior();

        for (int i = 0; i < poses.Count; i++)
        {
            if (poses[i].x % 2 == 0 && poses[i].y % 2 == 0)
            {
                NewObjectAtSpecificScreen("Bed", poses[i], worldPos);
            }
        }
    }

    //Used to place an icon on the world map.
    public void NewMapIcon(int iconType, Coord mapCoord)
    {
        if (mapCoord == null || mapIconGameObjects.ContainsKey(mapCoord))
        {
            return;
        }

        Vector3 pointerPos = new Vector3(mapCoord.x + 50, -200 + mapCoord.y + 0.1f, -0.001f);
        GameObject i = Instantiate(mapIcon, pointerPos, Quaternion.identity);
        i.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        i.name = "Map Icon";
        SpriteRenderer sp = i.GetComponentInChildren<SpriteRenderer>();
        sp.sprite = mapIconSprites[iconType];
        sp.sortingLayerID = 0;
        i.transform.parent = transform;
        mapIconGameObjects.Add(mapCoord, i);
    }

    public void RemoveMapIconAt(Coord mapCoord)
    {
        if (mapIconGameObjects.ContainsKey(mapCoord))
        {
            DestroyImmediate(mapIconGameObjects[mapCoord]);
            mapIconGameObjects.Remove(mapCoord);
        }
    }

    //check for inventories in a tile
    public void CheckMapObjectInventories()
    {
        for (int i = 0; i < onScreenMapObjects.Count; i++)
        {
            onScreenMapObjects[i].GetComponent<MapObjectSprite>().CheckInventory();
        }
    }

    public bool SafeToRest()
    {
        for (int i = 0; i < onScreenNPCObjects.Count; i++)
        {
            if (onScreenNPCObjects[i].AI == null)
            {
                continue;
            }

            BaseAI bAI = onScreenNPCObjects[i].AI;

            if (bAI.isHostile && playerEntity.inSight(onScreenNPCObjects[i].myPos))
            {
                if (bAI.isStationary)
                {
                    float dist = onScreenNPCObjects[i].myPos.DistanceTo(playerEntity.myPos);

                    if (dist > 2.5f)
                    {
                        Item firearm = bAI.GetComponent<Inventory>().firearm;
                        if (firearm.IsNullOrDefault())
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                return false;
            }
        }

        return true;
    }

    public void DemolishNPC(Entity e, NPC n)
    {
        if (n != null)
        {
            npcClasses.Remove(n);
        }

        if (e != null)
        {
            onScreenNPCObjects.Remove(e);
        }
    }

    public void UpdateDialogueOptions()
    {
        foreach (Entity e in onScreenNPCObjects)
        {
            e.gameObject.GetComponent<DialogueController>().SetupDialogueOptions();
        }
    }

    public int NumFollowers()
    {
        int num = 0;

        for (int i = 0; i < npcClasses.Count; i++)
        {
            if (npcClasses[i].IsFollower() && !npcClasses[i].HasFlag(NPC_Flags.At_Home))
            {
                num++;
            }
        }

        return num;
    }

    public void CheckFollowers()
    {
        for (int i = 0; i < npcClasses.Count; i++)
        {
            if (npcClasses[i].IsFollower() && !npcClasses[i].HasFlag(NPC_Flags.At_Home))
            {
                if (npcClasses[i].HasFlag(NPC_Flags.Deteriortate_HP))
                {
                    npcClasses[i].maxHealth = 0;
                    continue;
                }

                npcClasses[i].worldPosition = tileMap.WorldPosition;
                npcClasses[i].elevation = tileMap.currentElevation;

                List<Coord> empties = (playerEntity != null) ? playerEntity.GetEmptyCoords() : new List<Coord>();

                npcClasses[i].localPosition = (empties.Count > 0) ? empties.GetRandom() : World.tileMap.CurrentMap.GetRandomFloorTile();
            }
        }
    }

    /// <summary>
    /// Check each NPC, and see if they are on screen. Also calls CheckOnScreenMapObjects() too, to check map objects.
    /// </summary>
	public void CheckOnScreenNPCs(bool checkobjects = true)
    {
        for (int i = 0; i < npcClasses.Count; i++)
        {
            if (npcClasses[i].CanSpawnThisNPC(World.tileMap.WorldPosition, World.tileMap.currentElevation))
                SpawnNPC(npcClasses[i]);
            else
                npcClasses[i].onScreen = false;
        }

        if (checkobjects)
        {
            CheckOnScreenObjects();
        }
    }

    public void CheckOnScreenObjects()
    {
        for (int j = 0; j < mapObjects.Count; j++)
        {
            if (mapObjects[j].CanSpawnThisObject(World.tileMap.WorldPosition, World.tileMap.currentElevation))
                SpawnObject(mapObjects[j]);
            else
                mapObjects[j].onScreen = false;
        }
    }

    //Starts the process for saving the game upon quitting.
    public void SaveAndQuit()
    {
        StartCoroutine(SaveAndQuitCo());
    }

    IEnumerator SaveAndQuitCo()
    {
        World.userInterface.loadingGO.SetActive(true);
        World.userInterface.loadingGO.GetComponentInChildren<UnityEngine.UI.Text>().text = "Saving...";
        World.tileMap.OnScreenChange -= CheckArrowsEnabled;

        if (playerEntity != null)
        {
            tileMap.CurrentMap.lastTurnSeen = World.turnManager.turn;

            if (PlayerInput.fullMap)
            {
                World.playerInput.TriggerLocalOrWorldMap();
            }

            for (int i = 0; i < onScreenNPCObjects.Count; i++)
            {
                DestroyImmediate(onScreenNPCObjects[i].gameObject);
            }

            Manager.playerBuilder.quests.Clear();
            Manager.playerBuilder.progressFlags.Clear();
            GameSettings.Save();

            GetComponent<SaveData>().SaveNPCs(npcClasses);

            while (!NewWorld.doneSaving)
            {
                yield return null;
            }

            World.Reset();
        }

        MyConsole.ClearLog();

        yield return ReturnToMainMenu();
    }

    public void SaveGame()
    {
        StartCoroutine(QuickSave());
    }

    IEnumerator QuickSave()
    {
        World.userInterface.loadingGO.SetActive(true);
        World.userInterface.loadingGO.GetComponentInChildren<UnityEngine.UI.Text>().text = "Saving...";

        if (playerEntity != null)
        {
            tileMap.CurrentMap.lastTurnSeen = World.turnManager.turn;
            GetComponent<SaveData>().SaveNPCs(npcClasses);

            while (!NewWorld.doneSaving)
            {
                yield return null;
            }
        }

        World.userInterface.loadingGO.SetActive(false);
    }

    IEnumerator ReturnToMainMenu()
    {
        AsyncOperation loadScene = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0);
        yield return loadScene;
    }
}
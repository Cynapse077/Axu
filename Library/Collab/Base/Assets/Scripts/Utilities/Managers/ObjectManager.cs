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
        if (ItemList.items == null || ItemList.items.Count <= 0)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            return;
        }

        World.objectManager = this;
        onScreenNPCObjects = new List<Entity>();
        StartCoroutine("Initialize");
    }

    void GetVars()
    {
        turnManager = GetComponent<TurnManager>();
        worldMap = GameObject.FindObjectOfType<WorldMap>();
        tileMap = GameObject.FindObjectOfType<TileMap>();

    }

    IEnumerator Initialize()
    {
        Alert.LoadAlerts();
        GetComponent<UserInterface>().loadingGO.SetActive(true);

        if (!Manager.newGame)
            GetComponent<SaveData>().LoadPlayer();

        CreateWorldMap();
        yield return (worldMap.worldMapData.doneLoading);

        CreateLocalMap();
        SpawnPlayer();

        Camera.main.GetComponent<CameraControl>().Init();

        turnManager = GetComponent<TurnManager>();
        turnManager.Init();

        UserInterface.loading = false;
        World.tileMap.OnScreenChange += CheckArrowsEnabled;
        CheckArrowsEnabled(null, World.tileMap.CurrentMap);
        GameObject.FindObjectOfType<SidePanelUI>().Init();
        MyConsole.NewMessageColor("Type \"?\" or \"help\" to show a list of commands", Color.cyan);

        World.userInterface.CloseWindows();
        World.userInterface.ShowInitialMessage(Manager.playerName);
        doneLoading = true;
    }

    bool CheckArrowsEnabled(TileMap_Data oldMap, TileMap_Data newMap)
    {
        sideArrows.SetActive(newMap.elevation == 0 && Manager.localMapSize.x == 45 && Manager.localMapSize.y == 30);
        return true;
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
            tileMap.worldData = new OldScreen();

        tileMap.Init();
    }

    public bool CanDiscard(int x, int y)
    {
        if (x == World.tileMap.worldCoordX && y == World.tileMap.worldCoordY)
            return false;


        //TODO: Check if recently open, or has important quest details.
        /*List<NPC> ns = NPCsAt(new Coord(x, y));

        for (int i = 0; i < ns.Count; i++) {
            if (ns[i].HasFlag(NPC_Flags.Static) || ns[i].HasFlag(NPC_Flags.Follower) || ns[i].questID != "")
                return false;


            if (ns[i].HasFlag(NPC_Flags.Doctor) || ns[i].HasFlag(NPC_Flags.Merchant) || ns[i].HasFlag(NPC_Flags.SpawnedFromQuest))
                return false;

            for (int j = 0; j < playerJournal.quests.Count; j++) {
                if (ns[i] == playerJournal.quests[j].questGiver)
                    return false;
            }
        }*/

        return false;
    }

    public void SpawnPlayer()
    {
        if (!Manager.newGame)
            GetComponent<SaveData>().SetUpJournal();

        player = Instantiate(playerPrefab);
        player.name = "Player";
        playerEntity = player.GetComponent<Entity>();
        playerJournal = player.GetComponent<Journal>();
        playerEntity.myPos = new Coord(Manager.localStartPos.x, Manager.localStartPos.y);
    }
    #endregion

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
                npcs.Remove(n);
        }

        npcClasses = npcs;

        GameObject newObject = new GameObject("temp");

        foreach (Entity e in onScreenNPCObjects)
        {
            if (!npcClasses.Contains(e.AI.npcBase))
                e.transform.SetParent(newObject.transform);
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
                mos.Remove(m);
        }

        mapObjects = mos;

        GameObject newObject = new GameObject("temp");

        foreach (GameObject g in onScreenMapObjects)
        {
            if (!mapObjects.Contains(g.GetComponent<MapObjectSprite>().objectBase))
                g.transform.SetParent(newObject.transform);
        }

        newObject.transform.DestroyChildren();
        Destroy(newObject);
    }

    public bool NPCExists(string id)
    {
        return npcClasses.Find(x => x.ID == "arenamaster") != null;
    }

    //to spawn one on the current screen
    public BaseAI SpawnNPC(NPC npcB)
    {
        if (npcB == null || npcB.onScreen || npcB.localPosition == null)
            return null;

        if (!npcClasses.Contains(npcB))
        {
            if (npcB.HasFlag(NPC_Flags.Boss))
            {
                npcB.maxHealth += (World.DangerLevel() / 3);
                npcB.health = npcB.maxHealth;
            }

            npcClasses.Add(npcB);
        }

        if (npcB.worldPosition == tileMap.WorldPosition && npcB.elevation == tileMap.currentElevation)
        {
            for (int i = 0; i < onScreenNPCObjects.Count; i++)
            {
                if (onScreenNPCObjects[i] == null)
                    continue;

                if (npcB.localPosition == onScreenNPCObjects[i].GetComponent<BaseAI>().npcBase.localPosition)
                    return null;
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
            return;

        for (int i = 0; i < onScreenNPCObjects.Count; i++)
        {
            if (onScreenNPCObjects[i] == null || npcB.localPosition.x == summonerNPC.localPosition.x && npcB.localPosition.y == summonerNPC.localPosition.y)
                return;
        }

        npcClasses.Add(npcB);

        //Remove the summon flag, so you don't end up with a screen full of baddies.
        if (npcB.HasFlag(NPC_Flags.Summon_Adds))
            npcB.flags.Remove(NPC_Flags.Summon_Adds);

        npcB.faction = summonerNPC.faction;

        if (npcB.worldPosition == tileMap.WorldPosition && npcB.elevation == tileMap.currentElevation)
            SpawnNPC(npcB);
    }

    //Call this to check NPCs positions upon entering a screen, to make sure they don't overlap the player.
    public void NoStickNPCs(int posX, int posY)
    {
        for (int i = 0; i < onScreenNPCObjects.Count; i++)
        {
            Entity entity = onScreenNPCObjects[i].GetComponent<Entity>();

            if (entity.posX == posX && entity.posY == posY)
            {
                Coord c = World.tileMap.GetRandomPosition();
                entity.posX = c.x;
                entity.posY = c.y;
            }
        }
    }

    //use this for making an NPC at a certain location other than on screen
    //Only used in world initialization from file for now.
    public void CreateNPC(NPC npcB)
    {
        if (!npcClasses.Contains(npcB))
            npcClasses.Add(npcB);
    }

    //Clears the screen of NPCs
    public void RemoveNPCInstances()
    {
        for (int i = 0; i < onScreenNPCObjects.Count; i++)
        {
            if (onScreenNPCObjects[i] != null)
                Destroy(onScreenNPCObjects[i].gameObject);
        }

        onScreenNPCObjects.Clear();
    }

    public List<NPC> NPCsInScreen(Coord pos, int elev)
    {
        return npcClasses.FindAll(x => x.worldPosition == pos && x.elevation == elev);
    }

    public void SpawnExplosion(Entity spawner, Coord position)
    {
        GameObject exp = Instantiate(World.poolManager.abilityEffects[6], position.toVector2(), Quaternion.identity);
        TileDamage td = new TileDamage(spawner, position, new HashSet<DamageTypes>() { DamageTypes.Heat })
        {
            myName = exp.name = LocalizationManager.GetContent("Explosion"),
            damage = Random.Range(14, 20)
        };
        td.ApplyDamage();
    }

    public void SpawnPoisonGas(Entity spawner, Coord position)
    {
        GameObject exp = Instantiate(World.poolManager.abilityEffects[2], position.toVector2(), Quaternion.identity);
        TileDamage td = new TileDamage(spawner, position, new HashSet<DamageTypes>() { DamageTypes.Venom })
        {
            myName = exp.name = LocalizationManager.GetContent("Poisonous Gas"),
            damage = Random.Range(1, 3)
        };
        td.ApplyDamage();
    }

    public void SpawnEffect(int index, string effectName, Entity spawner, int x, int y, int dmg, Skill skill, float rotation)
    {
        SpawnEffect(index, effectName, spawner, new Coord(x, y), dmg, skill, rotation);
    }

    void SpawnEffect(int index, string effectName, Entity spawner, Coord position, int dmg, Skill skill, float rotation)
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

    //Spawn an object at a specific screen
    public MapObject NewObjectAtOtherScreen(string type, Coord locPos, Coord worPos, int elevation = 0)
    {
        MapObject mapOb = new MapObject(type, new Coord(locPos.x, locPos.y), worPos, elevation, "");
        mapObjects.Add(mapOb);

        if (mapOb.onScreen)
            return null;

        SpawnObject(mapOb);
        return mapOb;
    }

    public void CreatePoolOfLiquid(Coord localPos, Coord worldPos, int elev, string liquidID, int amount)
    {
        Cell c = World.tileMap.GetCellAt(localPos);
        Liquid newLiq = ItemList.GetLiquidByID(liquidID, amount);

        if (newLiq == null || c == null)
            return;

        if (c.GetPool() == null && !World.tileMap.IsWaterTile(localPos.x, localPos.y + Manager.localMapSize.y))
        {
            MapObject m = NewObjectAtOtherScreen("Loot", localPos, World.tileMap.WorldPosition, World.tileMap.currentElevation);
            Item i = ItemList.GetItemByID("pool");
            i.GetItemComponent<CLiquidContainer>().liquid = newLiq;
            m.inv.Insert(0, i);
        }
        else
        {
            c.GetPool().GetItemComponent<CLiquidContainer>().Fill(newLiq);
        }
    }

    //Spawn an object on the current screen.
    public MapObject NewObject(string type, Coord locPos)
    {
        for (int i = 0; i < onScreenMapObjects.Count; i++)
        {
            MapObjectSprite mos = onScreenMapObjects[i].GetComponent<MapObjectSprite>();

            if (mos.localPos == locPos)
                return null;
        }

        if (type == "Tall_Grass" && World.tileMap.IsWaterTile(locPos.x, locPos.y))
            return null;

        MapObject mapOb = new MapObject(type, locPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, "");
        mapObjects.Add(mapOb);

        if (mapOb.onScreen)
            return mapOb;

        SpawnObject(mapOb);
        return mapOb;
    }

    //Create a new object with an inventory. Return its inventory component.
    public Inventory NewInventory(string type, Coord locPos, Coord worPos, List<Item> items = null)
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

        MapObject mapOb = new MapObject(type, new Coord(locPos.x, locPos.y), worPos, World.tileMap.currentElevation, "");

        if (items != null)
            mapOb.inv = items;

        mapObjects.Add(mapOb);

        mapOb.onScreen = true;
        GameObject newObject = Instantiate(objectPrefab, new Vector3(locPos.x, locPos.y, 0), Quaternion.identity);
        MapObjectSprite mos = newObject.GetComponent<MapObjectSprite>();
        mos.objectBase = mapOb;
        onScreenMapObjects.Add(newObject);

        Inventory myInv = newObject.GetComponent<Inventory>();
        return myInv;
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
                if (obj.objectType == mapObjects[i].objectType && !mapObjects[i].objectType.Contains("Bloodstain"))
                {
                    mapObjects.Remove(obj);
                    return;
                }
            }
        }

        if (!mapObjects.Contains(obj))
            mapObjects.Add(obj);
    }

    public void SpawnObject(MapObject obj)
    {
        if (!mapObjects.Contains(obj))
            mapObjects.Add(obj);

        if (obj.onScreen)
            return;

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

    public void RemoveObject(MapObject obj, GameObject go)
    {
        mapObjects.Remove(obj);
        onScreenMapObjects.Remove(go);
    }

    //remove all map objects on screen
    public void RemoveObjectInstances()
    {
        for (int i = 0; i < onScreenMapObjects.Count; i++)
        {
            MapObjectSprite objectInstance = onScreenMapObjects[i].GetComponent<MapObjectSprite>();

            if (objectInstance.objectBase != null)
            {
                objectInstance.objectBase.onScreen = false;
                DestroyImmediate(onScreenMapObjects[i]);
            }
        }

        onScreenMapObjects.Clear();
    }

    public Entity CheckEnitiyInTile(Coord pos)
    {
        if (World.tileMap.WalkableTile(pos.x, pos.y) && World.tileMap.GetCellAt(pos) != null)
            return World.tileMap.GetCellAt(pos).entity;

        return null;
    }

    //Spawn everything you need for a normal house, given coords of left, right, top, bottom
    public void BuildingObjects(House h, Coord worldPos, string npcID)
    {
        Coord localPos = h.GetRandomPosition();
        NPC newNPC = EntityList.GetNPCByID(npcID, worldPos, localPos);

        SpawnNPC(newNPC);

        if (newNPC.HasFlag(NPC_Flags.Merchant))
            ShopObjects(h, worldPos);
        else if (newNPC.HasFlag(NPC_Flags.Doctor))
            HospitalObjects(h, worldPos);
        else
            HouseObjects(h, worldPos);
    }

    void HouseObjects(House h, Coord worldPos)
    {
        Coord c = h.GetRandomPosition();
        NewObjectAtOtherScreen("Bed", c, worldPos);
        Coord c2 = h.GetRandomPosition();

        if (c == c2)
            c2 = h.GetRandomPosition();

        NewObjectAtOtherScreen("Table", c2, worldPos);

        Coord c3 = h.GetRandomPosition();

        if (c3 == c || c3 == c2)
            c3 = h.GetRandomPosition();

        NewObjectAtOtherScreen((SeedManager.localRandom.CoinFlip() ? "Chair_Left" : "Chair_Right"), c3, worldPos);
    }

    void ShopObjects(House h, Coord worldPos)
    {
        List<Coord> takenPositions = new List<Coord>();

        for (int i = 0; i < SeedManager.localRandom.Next(2, 6); i++)
        {
            Coord c = h.GetRandomPosition();

            if (takenPositions.Contains(c))
                c = h.GetRandomPosition();

            takenPositions.Add(c);

            NewObjectAtOtherScreen("Barrel", c, worldPos);
        }
    }

    void HospitalObjects(House h, Coord worldPos)
    {
        List<Coord> takenPositions = new List<Coord>();

        for (int i = 0; i < SeedManager.localRandom.Next(4, 6); i++)
        {
            Coord c = h.GetRandomPosition();

            if (takenPositions.Contains(c))
                c = h.GetRandomPosition();

            takenPositions.Add(c);

            NewObjectAtOtherScreen("Barrel", c, worldPos);
        }
    }

    //Used to place an icon on the world map.
    public void NewMapIcon(int iconType, Coord mapCoord)
    {
        if (mapCoord == null || mapIconGameObjects.ContainsKey(mapCoord))
            return;

        Vector3 pointerPos = new Vector3(mapCoord.x + 50, -200 + mapCoord.y + 0.1f, -0.001f);
        GameObject i = (GameObject)Instantiate(mapIcon, pointerPos, Quaternion.identity);
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

    public List<Entity> InSightEntities()
    {
        List<Entity> ise = new List<Entity>();

        for (int i = 0; i < onScreenNPCObjects.Count; i++)
        {
            if (playerEntity.inSight(onScreenNPCObjects[i].GetComponent<Entity>().myPos))
                ise.Add(onScreenNPCObjects[i].GetComponent<Entity>());
        }

        return ise;
    }

    public bool SafeToRest()
    {
        for (int i = 0; i < onScreenNPCObjects.Count; i++)
        {
            if (onScreenNPCObjects[i].GetComponent<BaseAI>() == null)
                continue;

            BaseAI bAI = onScreenNPCObjects[i].GetComponent<BaseAI>();

            if (bAI.isHostile && playerEntity.inSight(bAI.GetComponent<Entity>().myPos))
            {
                if (bAI.isStationary)
                {
                    float dist = Vector2.Distance(bAI.GetComponent<Entity>().myPos.toVector2(), playerEntity.myPos.toVector2());

                    if (dist > 2.5f)
                    {
                        Item firearm = bAI.GetComponent<Inventory>().firearm;
                        if (firearm == null || firearm.Name == ItemList.GetNone().Name)
                            continue;
                    }
                    else
                        return false;
                }

                return false;
            }
        }

        return true;
    }

    public void DemolishNPC(Entity e, NPC n)
    {
        if (n != null)
            npcClasses.Remove(n);

        if (e != null)
        {
            onScreenNPCObjects.Remove(e);

            if (n.HasFlag(NPC_Flags.Static))
            {
                World.BaseDangerLevel++;
                CombatLog.SimpleMessage("Message_Darkness");
            }
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
            if (npcClasses[i].HasFlag(NPC_Flags.Follower) && !npcClasses[i].HasFlag(NPC_Flags.At_Home))
                num++;
        }

        return num;
    }

    public void CheckFollowers()
    {
        for (int i = 0; i < npcClasses.Count; i++)
        {
            if (npcClasses[i].HasFlag(NPC_Flags.Follower) && !npcClasses[i].onScreen && !npcClasses[i].HasFlag(NPC_Flags.At_Home))
            {
                if (npcClasses[i].HasFlag(NPC_Flags.Deteriortate_HP))
                {
                    npcClasses[i].maxHealth = 0;
                    continue;
                }

                npcClasses[i].worldPosition = tileMap.WorldPosition;
                npcClasses[i].elevation = tileMap.currentElevation;
                npcClasses[i].localPosition = (playerEntity.GetEmptyCoords().Count > 0) ? playerEntity.GetEmptyCoords().GetRandom() : playerEntity.myPos;
                SpawnNPC(npcClasses[i]);
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
            CheckOnScreenObjects();
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

        yield return new WaitForSeconds(0.01f);
        if (playerEntity != null)
        {
            tileMap.CurrentMap.lastTurnSeen = World.turnManager.turn;

            if (PlayerInput.fullMap)
                World.playerInput.TriggerLocalOrWorldMap();

            for (int i = 0; i < onScreenNPCObjects.Count; i++)
            {
                DestroyImmediate(onScreenNPCObjects[i].gameObject);
            }

            Manager.playerBuilder.quests.Clear();
            Manager.playerBuilder.progressFlags.Clear();
            GameSettings.Save();
            GetComponent<SaveData>().SaveNPCs(npcClasses, true);
            World.Reset();
        }

        MyConsole.ClearLog();

        yield return ReturnToMainMenu();
    }

    IEnumerator ReturnToMainMenu()
    {
        AsyncOperation loadScene = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0);
        yield return loadScene;
    }
}
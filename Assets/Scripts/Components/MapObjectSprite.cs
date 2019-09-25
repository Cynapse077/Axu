﻿using UnityEngine;
using System.IO;
using LitJson;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class MapObjectSprite : MonoBehaviour
{
    const int MAX_PULSES = 300;
    const int AutotileSpriteSize = 16;
    static readonly Rect SpriteRect = new Rect(0, 0, AutotileSpriteSize, AutotileSpriteSize);
    static readonly Vector2 Pivot = new Vector2(0.5f, 0.5f);

    public MapObject objectBase;
    public Transform childObject;
    public Coord localPos;
    public GameObject fire;
    public SpriteRenderer moreIconRenderer;
    public Cell cell;
    [HideInInspector] public Inventory inv;

    bool inSight;
    bool renderInBack = false;
    SpriteRenderer spriteRenderer;
    LightSource lightSource;
    SpriteRenderer fi; 
    Color myColor = Color.white;
    bool on;

    public string objectType
    {
        get { return objectBase.blueprint.objectType; }
    }
    public int PathCost
    {
        get { return objectBase.blueprint.pathCost; }
    }

    void Start()
    {
        Setup();
    }

    public void Setup()
    {
        objectBase.onScreen = true;
        localPos = objectBase.localPosition;

        CacheVars();
        World.tileMap.GetCellAt(localPos).AddObject(this);
        Initialize();
    }

    void OnDisable()
    {
        if (cell != null)
        {
            cell.RemoveObject(this);
        }

        if (objectBase.HasEvent("OnTurn"))
        {
            World.turnManager.incrementTurnCounter -= OnTurn;
        }

        if (objectType == "Loot" || objectType == "Brazier" || objectType == "Campfire")
        {
            World.turnManager.incrementTurnCounter -= UpdateVisuals;
        }

        if (inv != null)
        {
            objectBase.inv = inv.items;
        }

        if (lightSource != null)
        {
            SetLit(false);
        }
    }

    void GrabFromBlueprint(MapObjectBlueprint bp) 
    {
        if (gameObject)
        {
            gameObject.name = bp.Name;
            spriteRenderer.sprite = SpriteManager.GetObjectSprite(bp.spriteID);
        }
        else
        {
            return;
        }
        
        switch (bp.renderLayer)
        {
            case ObjectRenderLayer.Back:
                spriteRenderer.sortingLayerName = "Items";
                spriteRenderer.sortingOrder = 0;
                renderInBack = true;
                break;
            case ObjectRenderLayer.Mid:
                spriteRenderer.sortingLayerName = "Items";
                spriteRenderer.sortingOrder = 2;
                break;
            case ObjectRenderLayer.Front:
                spriteRenderer.sortingLayerName = "Characters";
                break;
        }

        if (objectBase.Solid)
        {
            cell.SetUnwalkable();
        }

        if (bp.tint != Vector4.one)
        {
            myColor = bp.tint;
        }

        if (bp.light != 0)
        {
            SetLit(false);
            lightSource = new LightSource(bp.light);
            SetLit(true);
        }

        if (bp.autotile) 
        {
            Texture2D t = SpriteManager.GetObjectSprite(ItemList.GetMOB(objectType).spriteID).texture;
            spriteRenderer.sprite = Sprite.Create(t, new Rect(SpriteRect), Pivot, AutotileSpriteSize);
            Autotile(true);
        }

        if (bp.container != null)
        {
            SetInventory(100);
        }

        if (objectBase.HasEvent("OnTurn"))
        {
            World.turnManager.incrementTurnCounter += OnTurn;
        }
    }

    void Initialize()
    {
        GrabFromBlueprint(ItemList.GetMOB(objectType));

        switch (objectType)
        {
            case "Campfire":
            case "Brazier":
                World.turnManager.incrementTurnCounter += UpdateVisuals;
                SpawnFire();
                break;
            case "Loot":
                World.turnManager.incrementTurnCounter += UpdateVisuals;
                break;
        }

        SetPositionToLocalPos();
        UpdateVisuals();
    }

    public void StartPulse(bool pulseOn)
    {
        on = pulseOn;
        SendPulses(null, 0, on);
    }

    void SendPulses(Coord previous, int moveCount, bool pulseOn)
    {
        if (moveCount > MAX_PULSES)
        {
            return;
        }

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0 || Mathf.Abs(x) + Mathf.Abs(y) > 1)
                {
                    continue;
                }

                Cell c = World.tileMap.GetCellAt(localPos.x + x, localPos.y + y);

                if (c != null && c.position != previous)
                {
                    c.RecievePulse(localPos, moveCount, pulseOn);
                }
            }
        }
    }

    public void ReceivePulse(Coord previous, int moveCount, bool pulseOn)
    {
        if (objectBase.blueprint.pulseInfo.receive)
        {
            if (isDoor_Closed && pulseOn)
            {
                ForceOpen();
            }

            if (objectBase.HasEvent("OnPulseReceived"))
            {
                LuaManager.CallScriptFunction(objectBase.GetEvent("OnPulseReceived"), pulseOn, this);
            }

            on = pulseOn;

            if (objectBase.blueprint.pulseInfo.send)
            {
                if (objectBase.blueprint.pulseInfo.reverse)
                {
                    on = !on;
                }

                SendPulses(previous, ++moveCount, on);
            }
        }
    }

    public void OnEntityEnter(Entity e)
    {
        if (objectBase.HasEvent("OnEntityEnter") && e != null)
        {
            LuaManager.CallScriptFunction(objectBase.GetEvent("OnEntityEnter"), e, this);
        }
    }

    public void OnEntityExit(Entity e)
    {
        if (objectBase.HasEvent("OnEntityExit") && e != null)
        {
            LuaManager.CallScriptFunction(objectBase.GetEvent("OnEntityExit"), e, this);
        }
    }

    void OnTurn()
    {
        LuaManager.CallScriptFunction(objectBase.GetEvent("OnTurn"), this);
    }

    public void Autotile(bool initial)
    {
        int xOffset = BitwiseNeighbors() * 16;

        if (initial)
        {
            AutotileAdjacent();
        }

        Texture2D t = SpriteManager.GetObjectSprite(ItemList.GetMOB(objectBase.blueprint.objectType).spriteID).texture;
        spriteRenderer.sprite = Sprite.Create(t, new Rect(xOffset, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }

    void AutotileAdjacent()
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0 || Mathf.Abs(x) + Mathf.Abs(y) > 1)
                {
                    continue;
                }
                else if (NeighborAt(localPos.x + x, localPos.y + y))
                {
                    World.tileMap.GetCellAt(localPos.x + x, localPos.y + y).mapObjects.Find(z => z.CanAutotile(objectBase)).Autotile(false);
                }
            }
        }
    }

    bool CanAutotile(MapObject other)
    {
        return (other.blueprint.objectType == objectType);
    }

    int BitwiseNeighbors()
    {
        int tIndex = 0;

        if (NeighborAt(localPos.x, localPos.y + 1)) tIndex++;
        if (NeighborAt(localPos.x - 1, localPos.y)) tIndex += 2;
        if (NeighborAt(localPos.x + 1, localPos.y)) tIndex += 4;
        if (NeighborAt(localPos.x, localPos.y - 1)) tIndex += 8;

        return tIndex;
    }

    bool NeighborAt(int x, int y)
    {
        if (World.OutOfLocalBounds(x, y))
        {
            return false;
        }

        return World.tileMap.GetCellAt(x, y).mapObjects.Find(z => z.CanAutotile(objectBase));
    }

    public void SetTypeAndSwapSprite(string t)
    {
        MapObjectBlueprint bp = ItemList.GetMOB(t);

        if (bp != null)
        {
            if (objectBase.HasEvent("OnTurn"))
            {
                World.turnManager.incrementTurnCounter -= OnTurn;
            }

            if (objectBase.blueprint.pathCost != 0)
            {
                World.tileMap.GetCellAt(localPos.x, localPos.y).EditPathCost(-objectBase.blueprint.pathCost);
            }

            objectBase.SetBlueprint(bp);
            GrabFromBlueprint(bp);

            if (objectBase.blueprint.pathCost != 0)
            {
                World.tileMap.GetCellAt(localPos.x, localPos.y).EditPathCost(objectBase.blueprint.pathCost);
            }
        }
    }

    void SpawnFire()
    {
        GameObject f = Instantiate(fire, transform.position + Vector3.up * 0.15f, Quaternion.identity);
        f.transform.parent = transform;
        f.name = "Fire";
        f.GetComponent<DestroyAfterTurns>().enabled = false;
        fi = f.GetComponentInChildren<SpriteRenderer>();
        fi.sortingLayerName = "Items";
        fi.sortingOrder = 1;
    }

    void SetLit(bool lit)
    {
        if (lightSource == null)
        {
            return;
        }

        int rad = lightSource.radius;
        Coord lPos = objectBase.localPosition;

        for (int x = lPos.x - rad; x <= lPos.x + rad; x++)
        {
            for (int y = lPos.y - rad; y <= lPos.y + rad; y++)
            {
                if (x < 0 || y < 0 || x >= Manager.localMapSize.x || y >= Manager.localMapSize.y)
                {
                    continue;
                }

                float dist = lPos.DistanceTo(new Coord(x, y));

                if (dist <= rad && Line.inSight(lPos, x, y))
                {
                    World.tileMap.tileRenderers[x, y].lit = lit;
                }
            }
        }
    }

    public bool InSight()
    {
        if (cell == null)
        {
            return false;
        }

        return (cell.InSight);
    }

    void SetPositionToLocalPos()
    {
        localPos.x = Mathf.Clamp(localPos.x, 0, Manager.localMapSize.x - 1);
        localPos.y = Mathf.Clamp(localPos.y, 0, Manager.localMapSize.y - 1);
        transform.position = new Vector2(localPos.x, localPos.y - Manager.localMapSize.y);

        int renderLayer = -localPos.y;

        if (renderInBack)
        {
            renderLayer--;
        }

        childObject.GetComponent<SpriteRenderer>().sortingOrder = renderLayer;
        childObject.GetComponentInChildren<SpriteRenderer>().sortingOrder = renderLayer;
    }

    public void UpdateVisuals()
    {
        if ((objectType == "Campfire" || objectType == "Brazier") && fi != null)
        {
            fi.enabled = inSight;
        }

        if (objectType == "Loot" && spriteRenderer != null)
        {
            moreIconRenderer.enabled = (spriteRenderer.color != Color.clear) ? ShowMoreItemIcon() : false;

            if (inv != null && inv.items.Count > 0)
            {
                spriteRenderer.sprite = SwitchSprite(inv.items[0]);

                Item first = inv.items[0];

                if (first.HasCComponent<CLiquidContainer>() && !first.GetCComponent<CLiquidContainer>().isEmpty())
                {
                    Liquid liquid = ItemList.GetLiquidByID(first.GetCComponent<CLiquidContainer>().sLiquid.ID);

                    myColor = (liquid != null) ? (Color)liquid.color : Color.white;
                }
                else
                {
                    myColor = Color.white;
                }
            }

            ShowMoreItemIcon();
        }
    }

    bool ShowMoreItemIcon()
    {
        if (objectType != "Loot")
        {
            return false;
        }

        if (inv == null)
        {
            inv = GetComponent<Inventory>();
        }

        return (inv.items.Count > 1);
    }

    Sprite SwitchSprite(Item item)
    {
        string id = (string.IsNullOrEmpty(item.renderer.onGround)) ? ItemList.GetMOB(objectType).spriteID : item.renderer.onGround;
        return SpriteManager.GetObjectSprite(id);
    }

    void SetInventory(int capacity)
    {
        Inventory inv2 = gameObject.AddComponent<Inventory>();
        inv2.maxItems = 300;

        if (objectBase.inv != null)
        {
            inv2.items = objectBase.inv;
        }
    }

    public void CheckInventory()
    {
        UpdateVisuals();

        if (objectType == "Loot" || objectType == "Body")
        {
            if (inv == null)
            {
                inv = GetComponent<Inventory>();
            }

            if (inv != null)
            {
                if (inv.items.Count <= 0)
                {
                    World.objectManager.RemoveObject(objectBase, gameObject);
                    Destroy(gameObject);
                }
                else if (objectBase.blueprint.objectType == "Pool" && inv.items.Count == 1 && inv.items[0].HasCComponent<CLiquidContainer>())
                {
                    CLiquidContainer cl = inv.items[0].GetCComponent<CLiquidContainer>();

                    if (cl.isEmpty())
                    {
                        World.objectManager.RemoveObject(objectBase, gameObject);
                        Destroy(gameObject);
                    }
                }
            }
        }
    }

    public void ForceOpen()
    {
        if (isDoor_Closed)
        {
            if (objectType == "Door_Closed")
                SetTypeAndSwapSprite("Door_Open");
            else if (objectType == "Ensis_Door_Closed")
                SetTypeAndSwapSprite("Ensis_Door_Open");
            else if (objectType == "Prison_Door_Closed")
                SetTypeAndSwapSprite("Prison_Door_Open");
            else if (objectType == "Magna_Door_Closed")
                SetTypeAndSwapSprite("Magna_Door_Open");
            else if (objectType == "Kin_Door_Closed")
                SetTypeAndSwapSprite("Kin_Door_Open");
            else if (objectType == "Elec_Door_Closed")
                SetTypeAndSwapSprite("Elec_Door_Open");

            World.soundManager.OpenDoor();
        }
    }

    public bool isDoor_Open
    {
        get { return objectType.ToString().Contains("Door_Open"); }
    }

    public bool isDoor_Closed
    {
        get { return objectType.ToString().Contains("Door_Closed"); }
    }

    public void Interact()
    {
        if (isDoor_Closed)
        {
            bool opened = objectBase.blueprint.PermissionsMatch();

            if (opened)
            {
                string newObjectType = objectType.Replace("Closed", "Open");
                SetTypeAndSwapSprite(newObjectType);
                World.soundManager.OpenDoor();
                World.tileMap.SoftRebuild();
            }
            else
            {
                Alert.NewAlert("Locked");
                ObjectManager.playerEntity.CancelWalk();
            }
        }
        else
        {
            for (int i = 0; i < objectBase.blueprint.permissions.Length; i++)
            {
                if (!ObjectManager.playerJournal.HasFlag(objectBase.blueprint.permissions[i]))
                {
                    CombatLog.NewMessage("You don't know what to do with it.");
                    return;
                }
            }
        }

        EventHandler.instance.OnInteract(objectBase);

        if (objectBase.HasEvent("OnInteract"))
        {
            LuaManager.CallScriptFunction(objectBase.GetEvent("OnInteract"), this);
            return;
        }

        switch (objectType)
        {
            case "Barrel":
                SetTypeAndSwapSprite("Barrel_Open");
                break;

            case "Chest":
                SetTypeAndSwapSprite("Chest_Open");
                break;

            case "Crystal":
                Landmark landmark = World.tileMap.GetRandomLandmark();
                World.tileMap.SetPosition(landmark.pos, 0);

                ObjectManager.playerEntity.ForcePosition(World.tileMap.CurrentMap.GetRandomFloorTile());
                ObjectManager.playerEntity.BeamDown();

                World.tileMap.HardRebuild();
                CombatLog.SimpleMessage("Crystal_Tele");

                if (SeedManager.combatRandom.Next(100) < 10 && !ObjectManager.playerEntity.stats.hasTraitEffect(TraitEffects.Crystallization))
                {
                    ObjectManager.playerEntity.stats.InitializeNewTrait(TraitList.GetTraitByID("crystal"));
                    CombatLog.SimpleMessage("Crystal_Rad");
                }
                break;

            case "Bookshelf":
                World.userInterface.YesNoAction("YN_Read_Bookshelf".Translate(),
                    () => {
                        World.userInterface.CloseWindows();
                        SetTypeAndSwapSprite("Bookshelf_Empty");

                        if (GameData.GetRandom<Book>() is Book book)
                        {
                            book.Read();
                        }
                    },
                    () => { World.userInterface.CloseWindows(); }, "");
                break;

            case "Ore":
                if (ObjectManager.playerEntity.inventory.DiggingEquipped())
                {
                    List<Item> newInv = new List<Item> { ItemList.GetItemByID("artifact4") };

                    World.objectManager.NewInventory("Loot", new Coord(localPos.x, localPos.y), World.tileMap.WorldPosition, World.tileMap.currentElevation, newInv);
                    World.soundManager.BreakArea();
                    DestroyMe();
                    return;
                }
                else
                {
                    Alert.NewAlert("Need_Dig");
                }

                break;

            case "Robot_Frame":
                if (ObjectManager.playerEntity.inventory.HasItem("ai_core"))
                {
                    if (World.objectManager.NumFollowers() >= 3)
                    {
                        CombatLog.NewMessage("You have too many followers to accept another.");
                        break;
                    }

                    NPC_Blueprint bp = GameData.Get<NPC_Blueprint>("hauler");

                    if (bp == null)
                    {
                        break;
                    }

                    ObjectManager.playerEntity.inventory.RemoveInstance(ObjectManager.playerEntity.inventory.items.Find(x => x.ID == "ai_core"));
                    NPC n = new NPC(bp, World.tileMap.WorldPosition, localPos, World.tileMap.currentElevation);

                    n.MakeFollower();
                    CombatLog.NewMessage("<color=green>You insert the AI Core into the Robotic Frame. After a short bootup, the Hauler begins to follow you!</color>");

                    World.objectManager.SpawnNPC(n);
                    World.objectManager.RemoveObject(objectBase, gameObject);
                    Destroy(gameObject);

                    return;
                }
                else
                {
                    CombatLog.NewMessage("This Hauler is currently offline. It seems to be missing something.");
                }

                break;

            case "Headstone":
                Alert.CustomAlert_WithTitle("Obituary", ObituaryCreator.GetNewObituary(World.tileMap.WorldPosition, localPos));

                break;
        }

        if (GetComponent<Inventory>() != null)
        {
            inv = GetComponent<Inventory>();

            if (objectType == "Chest" || objectType == "Chest_Open" || inv.items.Count > 0)
            {
                World.userInterface.OpenLoot(inv);
            }
        }
    }

    public void DestroyMe()
    {
        if (World.objectManager.mapObjects.Contains(objectBase))
        {
            World.objectManager.mapObjects.Remove(objectBase);
        }

        if (World.objectManager.onScreenMapObjects.Contains(gameObject))
        {
            World.objectManager.onScreenMapObjects.Remove(gameObject);
        }

        Destroy(gameObject);
    }

    public string Description
    {
        get { return objectBase.blueprint.description; }
    }

    public void SetParams(bool insight, bool hasseen)
    {
        objectBase.seen = hasseen;
        ShowHide(insight);
    }

    void ShowHide(bool sighted)
    {
        if (inSight != sighted)
        {
            inSight = sighted;

            if (inSight)
            {
                objectBase.seen = true;
            }
        }

        SetSpriteColor();
    }

    void SetSpriteColor()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (!objectBase.seen)
        {
            spriteRenderer.color = Color.clear;
        }
        else
        {

            if (World.tileMap.IsTileLit(localPos.x, localPos.y))
                spriteRenderer.color = (inSight) ? myColor * new Color(1.0f, 1.0f, 0.9f) : myColor * Color.grey;
            else
                spriteRenderer.color = (inSight) ? myColor : myColor * Color.grey;
        }
    }

    void CacheVars()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        childObject.rotation = Quaternion.Euler(0, 0, objectBase.rotation);
    }

    public class LightSource
    {
        public int radius { get; protected set; }

        public LightSource(int _radius)
        {
            radius = _radius;
        }
    }
}

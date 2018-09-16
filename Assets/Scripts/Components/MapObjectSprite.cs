using UnityEngine;
using System.IO;
using LitJson;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class MapObjectSprite : MonoBehaviour
{
    public MapObject objectBase;
    public Transform childObject;
    public Coord localPos;
    public GameObject fire;
    public SpriteRenderer moreIconRenderer;
    public Cell cell;
    [HideInInspector] public Inventory inv;
    public LuaCall onInteract;

    bool inSight;
    bool renderInBack = false;
    SpriteRenderer spriteRenderer;
    LightSource lightSource;
    SpriteRenderer fi;
    string _description;
    Color myColor = Color.white;

    public string objectType
    {
        get { return objectBase.objectType; }
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
            cell.RemoveObject(this);

        if (objectType == "Loot" || objectType == "Brazier" || objectType == "Campfire")
            World.turnManager.incrementTurnCounter -= UpdateVisuals;

        if (inv != null)
            objectBase.inv = inv.items;

        if (lightSource != null)
            SetLit(false);
    }

    void Initialize()
    {
        MapObjectBlueprint bp = ItemList.GetMOB(objectType);

        gameObject.name = bp.Name;
        spriteRenderer.sprite = SpriteManager.GetObjectSprite(ItemList.GetMOB(objectBase.objectType).spriteID);

        if (bp.renderInFront)
        {
            spriteRenderer.sortingLayerName = "Characters";
        }
        else if (bp.renderInBack)
        {
            spriteRenderer.sortingLayerName = "Items";
            spriteRenderer.sortingOrder = 0;
            renderInBack = true;
        }
        else
        {
            spriteRenderer.sortingLayerName = "Items";
            spriteRenderer.sortingOrder = 2;
        }

        //Container
        if (bp.container != null)
            SetInventory(bp.container.capacity);

        //Tint
        if (bp.tint != Vector4.one)
            myColor = bp.tint;

        if (bp.solid == MapObjectBlueprint.MapOb_Interactability.Solid)
            objectBase.solid = true;

        _description = bp.description;

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
            case "Fence":
            case "Bars":
                Texture2D t = SpriteManager.GetObjectSprite(ItemList.GetMOB(objectBase.objectType).spriteID).texture;
                spriteRenderer.sprite = Sprite.Create(t, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
                Autotile(true);
                break;
        }

        if (bp.light != 0)
        {
            lightSource = new LightSource(bp.light);
            SetLit(true);
        }

        if (bp.onInteract != null)
        {
            onInteract = bp.onInteract;
        }

        SetPositionToLocalPos();
        UpdateVisuals();
    }

    public void Autotile(bool initial)
    {
        if (objectType == "Fence" || objectType == "Bars")
        {
            int xOffset = BitwiseNeighbors() * 16;

            if (initial)
                AutotileAdjacent();

            Texture2D t = SpriteManager.GetObjectSprite(ItemList.GetMOB(objectBase.objectType).spriteID).texture;
            spriteRenderer.sprite = Sprite.Create(t, new Rect(xOffset, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }
    }

    void AutotileAdjacent()
    {
        if (NeighborAt(localPos.x, localPos.y + 1))
            World.tileMap.GetCellAt(localPos.x, localPos.y + 1).mapObjects.Find(z => z.objectType == objectType).Autotile(false);

        if (NeighborAt(localPos.x - 1, localPos.y))
            World.tileMap.GetCellAt(localPos.x - 1, localPos.y).mapObjects.Find(z => z.objectType == objectType).Autotile(false);

        if (NeighborAt(localPos.x + 1, localPos.y))
            World.tileMap.GetCellAt(localPos.x + 1, localPos.y).mapObjects.Find(z => z.objectType == objectType).Autotile(false);

        if (NeighborAt(localPos.x, localPos.y - 1))
            World.tileMap.GetCellAt(localPos.x, localPos.y - 1).mapObjects.Find(z => z.objectType == objectType).Autotile(false);
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
        if (x < 0 || y < 0 || x >= Manager.localMapSize.x || y >= Manager.localMapSize.y)
            return false;

        return World.tileMap.GetCellAt(x, y).mapObjects.Find(z => z.objectType == objectType);
    }

    public void SetTypeAndSwapSprite(string t)
    {
        objectBase.objectType = t;

        if (spriteRenderer != null)
            spriteRenderer.sprite = SpriteManager.GetObjectSprite(ItemList.GetMOB(objectBase.objectType).spriteID);
    }

    void SpawnFire()
    {
        GameObject f = Instantiate(fire, transform.position + Vector3.up * 0.15f, Quaternion.identity);
        f.transform.parent = transform;
        f.name = "Fire";
        f.GetComponent<DestroyAfterTurns>().enabled = false;
        fi = f.GetComponentInChildren<SpriteRenderer>();
    }

    void SetLit(bool lit)
    {
        if (lightSource == null)
            return;

        int rad = lightSource.radius;

        for (int x = objectBase.localPosition.x - rad; x <= objectBase.localPosition.x + rad; x++)
        {
            for (int y = objectBase.localPosition.y - rad; y <= objectBase.localPosition.y + rad; y++)
            {
                if (x < 0 || y < 0 || x >= Manager.localMapSize.x || y >= Manager.localMapSize.y)
                    continue;

                float dist = objectBase.localPosition.DistanceTo(new Coord(x, y));

                if (dist > rad)
                    continue;

                if (Line.inSight(objectBase.localPosition, x, y))
                    World.tileMap.tileRenderers[x, y].lit = lit;
            }
        }
    }

    void SetPositionToLocalPos()
    {
        localPos.x = Mathf.Clamp(localPos.x, 0, Manager.localMapSize.x - 1);
        localPos.y = Mathf.Clamp(localPos.y, 0, Manager.localMapSize.y - 1);
        transform.position = new Vector2(localPos.x, localPos.y - Manager.localMapSize.y);

        int renderLayer = -localPos.y;

        if (renderInBack)
            renderLayer--;

        childObject.GetComponent<SpriteRenderer>().sortingOrder = renderLayer;
        childObject.GetComponentInChildren<SpriteRenderer>().sortingOrder = renderLayer;
    }

    public void UpdateVisuals()
    {
        if ((objectBase.objectType == "Campfire" || objectType == "Brazier") && fi != null)
            fi.enabled = inSight;

        if (objectType == "Loot" && spriteRenderer != null)
        {
            moreIconRenderer.enabled = (spriteRenderer.color != Color.clear) ? ShowMoreItemIcon() : false;

            if (inv != null && inv.items.Count > 0)
            {
                spriteRenderer.sprite = SwitchSprite(inv.items[0]);

                if (inv.items[0].HasComponent<CLiquidContainer>() && inv.items[0].GetItemComponent<CLiquidContainer>().liquid != null)
                    myColor = inv.items[0].GetItemComponent<CLiquidContainer>().liquid.color;
                else
                    myColor = Color.white;
            }

            ShowMoreItemIcon();
        }
    }

    bool ShowMoreItemIcon()
    {
        if (objectType != "Loot")
            return false;

        if (inv == null)
            inv = GetComponent<Inventory>();

        return (inv.items.Count > 1);
    }

    Sprite SwitchSprite(Item item)
    {
        string id = (string.IsNullOrEmpty(item.renderer.onGround)) ? ItemList.GetMOB(objectBase.objectType).spriteID : item.renderer.onGround;

        return SpriteManager.GetObjectSprite(id);
    }

    void SetInventory(int capacity)
    {
        Inventory inv2 = gameObject.AddComponent<Inventory>();
        inv2.SetStorage(capacity);

        if (objectBase.inv != null)
            inv2.items = objectBase.inv;
    }

    public void CheckInventory()
    {
        UpdateVisuals();

        if (objectType == "Loot" || objectType == "Body")
        {
            if (inv == null)
                inv = GetComponent<Inventory>();

            if (inv != null)
            {
                if (inv.items.Count <= 0)
                {
                    World.objectManager.RemoveObject(objectBase, gameObject);
                    Destroy(gameObject);
                }
                else if (inv.items.Count == 1 && inv.items[0].GetItemComponent<CLiquidContainer>() != null)
                {
                    CLiquidContainer cl = inv.items[0].GetItemComponent<CLiquidContainer>();

                    if (cl.liquid == null || cl.currentAmount <= 0)
                    {
                        World.objectManager.RemoveObject(objectBase, gameObject);
                        Destroy(gameObject);
                    }
                }
            }
        }
    }

    public void OpenDoor(bool isPlayer)
    {
        //Regular
        if (objectType == "Door_Closed")
            SetTypeAndSwapSprite("Door_Open");

        if (!isPlayer)
            return;

        //ensis
        if (objectType == "Ensis_Door_Closed")
        {
            if (ObjectManager.playerJournal.HasFlag(ProgressFlags.Can_Enter_Ensis))
            {
                SetTypeAndSwapSprite("Ensis_Door_Open");
            }
            else
            {
                Alert.NewAlert("Locked");
                return;
            }
        }

        //Prison
        if (objectType == "Prison_Door_Closed")
        {
            if (ObjectManager.playerJournal.HasFlag(ProgressFlags.Can_Open_Prison_Cells))
            {
                SetTypeAndSwapSprite("Prison_Door_Open");
            }
            else
            {
                Alert.NewAlert("Locked");
                return;
            }
        }

        //Magna
        if (objectType == "Magna_Door_Closed")
        {
            if (ObjectManager.playerJournal.HasFlag(ProgressFlags.Can_Enter_Magna))
            {
                SetTypeAndSwapSprite("Magna_Door_Open");
            }
            else
            {
                Alert.NewAlert("Locked");
                return;
            }
        }

        World.soundManager.OpenDoor();
        World.tileMap.SoftRebuild();
    }

    public bool isDoor_Open
    {
        get { return (objectType.ToString().Contains("Door_Open")); }
    }

    public bool isDoor_Closed
    {
        get { return (objectType.ToString().Contains("Door_Closed")); }
    }

    public void Interact()
    {
        if (isDoor_Open && cell.entity == null)
        {
            if (objectBase.objectType == "Door_Open")
                SetTypeAndSwapSprite("Door_Closed");
            else if (objectBase.objectType == "Ensis_Door_Open")
                SetTypeAndSwapSprite("Ensis_Door_Closed");
            else if (objectBase.objectType == "Prison_Door_Open")
                SetTypeAndSwapSprite("Prison_Door_Closed");
            else if (objectBase.objectType == "Magna_Door_Open")
                SetTypeAndSwapSprite("Magna_Door_Closed");
        }

        if (onInteract != null)
        {
            LuaManager.CallScriptFunction(onInteract.scriptName, onInteract.functionName, this);
            return;
        }

        if (isDoor_Closed)
        {
            World.soundManager.CloseDoor();
            World.tileMap.SoftRebuild();
        }
        else
        {
            switch (objectType)
            {
                case "Barrel":
                    SetTypeAndSwapSprite("Barrel_Open");
                    break;

                case "Chest":
                    SetTypeAndSwapSprite("Chest_Open");
                    break;

                case "Statue":
                    if (ObjectManager.playerJournal.OnDestroyShrine_CheckQuestProgress())
                    {
                        SetTypeAndSwapSprite("Statue_Broken");
                        CombatLog.SimpleMessage("Break_Statue");
                        World.soundManager.BreakArea();
                    }

                    break;

                case "Crystal":
                    Landmark landmark = World.tileMap.GetRandomLandmark();
                    World.tileMap.SetPosition(landmark.position, 0);

                    ObjectManager.playerEntity.ForcePosition(World.tileMap.CurrentMap.GetRandomFloorTile());
                    ObjectManager.playerEntity.BeamDown();

                    World.tileMap.HardRebuild();
                    CombatLog.SimpleMessage("Crystal_Tele");

                    if (SeedManager.combatRandom.Next(100) < 5 && !ObjectManager.playerEntity.stats.hasTraitEffect(TraitEffects.Crystallization))
                    {
                        ObjectManager.playerEntity.stats.InitializeNewTrait(TraitList.GetTraitByID("crystal"));
                        CombatLog.SimpleMessage("Crystal_Rad");
                    }
                    break;

                case "Bookshelf":
                    World.userInterface.YesNoAction("YN_Read_Bookshelf",
                        () =>
                        {
                            World.userInterface.CloseWindows();
                            string filePath = Application.streamingAssetsPath + "/Data/Dialogue/Books.json";
                            string jsonText = File.ReadAllText(filePath);
                            JsonData dat = JsonMapper.ToObject(jsonText);

                            int num = SeedManager.combatRandom.Next(dat.Count);
                            string title = dat[num]["Title"].ToString();
                            string text = dat[num]["Text"].ToString();

                            Alert.CustomAlert_WithTitle(title, text);
                        },
                        () => { World.userInterface.CloseWindows(); }, "");
                    break;

                case "Ore":
                    if (ObjectManager.playerEntity.inventory.DiggingEquipped())
                    {
                        List<Item> newInv = new List<Item>
                        {
                            ItemList.GetItemByID("artifact4")
                        };
                        World.objectManager.NewInventory("Loot", new Coord(localPos.x, localPos.y), World.tileMap.WorldPosition, newInv);
                        World.soundManager.BreakArea();
                        DestroyMe();
                    }
                    else
                        Alert.NewAlert("Need_Dig");
                    break;

                default:
                    break;
            }

            if (GetComponent<Inventory>() != null)
            {
                inv = GetComponent<Inventory>();

                if (objectType == "Chest" || objectType == "Chest_Open" || inv.items.Count > 0)
                    World.userInterface.OpenLoot(inv);
            }
        }
    }

    public void DestroyMe()
    {
        if (World.objectManager.mapObjects.Contains(objectBase))
            World.objectManager.mapObjects.Remove(objectBase);

        if (World.objectManager.onScreenMapObjects.Contains(gameObject))
            World.objectManager.onScreenMapObjects.Remove(gameObject);

        Destroy(gameObject);
    }

    public string Description
    {
        get { return _description + objectBase.description; }
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
                objectBase.seen = true;
        }

        SetSpriteColor();
    }

    void SetSpriteColor()
    {
        if (spriteRenderer == null)
            return;

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

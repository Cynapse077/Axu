using UnityEngine;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class MapObject
{
    static int CurrentUID = 0;

    public int UID = -1;
    public int elevation;
    public bool onScreen, seen;
    public float rotation;
    public List<Item> inv;
    public Coord localPosition, worldPosition;
    public MapObject_Blueprint blueprint { get; private set; }

    public string Name
    {
        get { return blueprint.Name; }
    }
    public bool Solid
    {
        get { return blueprint.solid == MapObject_Blueprint.MapOb_Interactability.Solid; }
    }

    public MapObject(MapObject_Blueprint bp, Coord lp, Coord wp, int ele, int uid = -1)
    {
        localPosition = lp;
        worldPosition = wp;
        elevation = ele;
        UID = uid;

        Init(bp);
    }

    void Init(MapObject_Blueprint bp)
    {
        if (UID > -1)
        {
            if (UID > CurrentUID)
            {
                CurrentUID = UID + 1;
            }
        }
        else
        {
            UID = CurrentUID;
            CurrentUID++;
        }

        SetBlueprint(bp);

        if (bp.randomRotation)
        {
            SetRotation();
        }

        SetupInventory();
    }

    public void SetBlueprint(MapObject_Blueprint bp)
    {
        blueprint = bp;
    }

    void SetRotation()
    {
        if (RNG.OneIn(10) || blueprint.objectType == "Bloodstain_Permanent")
        {
            rotation = Random.Range(0, 360);
        }
        //Bloodstain wall smears
        else if (blueprint.objectType.Contains("Bloodstain"))
        {
            List<Coord> possPos = new List<Coord>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int cx = localPosition.x + x, cy = localPosition.y + y;

                    if (cx <= 0 || cx >= Manager.localMapSize.x - 1 || cy <= 0 || cy >= Manager.localMapSize.y - 1 
                        || Mathf.Abs(x) + Mathf.Abs(y) > 1 || x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (!World.tileMap.WalkableTile(cx, cy))
                    {
                        possPos.Add(new Coord(x, y));
                    }
                }
            }

            if (possPos.Count > 0)
            {
                MapObject_Blueprint bp = GameData.Get<MapObject_Blueprint>("Bloodstain_Wall");
                if (bp != null)
                {
                    SetBlueprint(bp);
                    Coord offset = possPos.GetRandom(SeedManager.combatRandom);
                    localPosition += offset;
                    RotationFromOrientation(offset);
                }
            }
            else
            {
                rotation = Random.Range(0, 360);
                return;
            }
        }
    }

    void SetupInventory()
    {
        //Setup inventory
        if (blueprint.inventory != null)
        {
            inv = new List<Item>();

            for (int i = 0; i < blueprint.inventory.Count; i++)
            {
                Item item = ItemList.GetItemByID(blueprint.inventory[i]);

                if (!item.IsNullOrDefault())
                {
                    inv.Add(item);
                }
            }
        }
        else if (blueprint.container != null)
        {
            inv = new List<Item>();

            if (blueprint.container.chanceToContainItems > 0 && RNG.Next(100) < blueprint.container.chanceToContainItems)
            {
                int numItems = RNG.Next(1, blueprint.container.maxItems + 1);

                if (blueprint.container.possibleItems.NullOrEmpty())
                {
                    inv = Inventory.GetDrops(numItems);
                }
                else
                {
                    for (int i = 0; i < numItems; i++)
                    {
                        if (GameData.TryGet(blueprint.container.possibleItems.GetRandom(), out Item item))
                        {
                            inv.Add(new Item(item));
                        }
                    }
                }
            }
        }
    }

    void RotationFromOrientation(Coord offset)
    {
        if (offset.y > 0) rotation = 0;
        if (offset.y < 0) rotation = 180;
        if (offset.x < 0) rotation = 90;
        if (offset.x > 0) rotation = -90;
    }

    public bool CanSpawnThisObject(Coord pos, int elev)
    {
        return pos == worldPosition && elev == elevation;
    }

    public bool HasEvent(string eventName)
    {
        return blueprint.luaEvents != null && blueprint.luaEvents.ContainsKey(eventName);
    }

    public LuaCall GetEvent(string eventName)
    {
        if (blueprint.luaEvents == null)
        {
            return null;
        }

        return blueprint.luaEvents[eventName];
    }

    public bool CanDiscard()
    {
        return true;
    }
}
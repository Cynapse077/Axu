using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using System;

[MoonSharpUserData]
public class Cell
{
    public static readonly int EntityInCellCost = 4;

    public Coord position;
    public Entity entity;
    public List<MapObjectSprite> mapObjects;

    event Action<Entity> onEntityEnter;
    bool inSight, hasSeen;


    public Cell()
    {
        position = new Coord(0, 0);
        entity = null;
        mapObjects = new List<MapObjectSprite>();
        World.tileMap.OnScreenChange += Reset;
    }

    public Cell(Coord pos)
    {
        position = pos;
        entity = null;
        mapObjects = new List<MapObjectSprite>();
        World.tileMap.OnScreenChange += Reset;
    }

    public bool InSight
    {
        get { return inSight; }
        set { inSight = value; }
    }

    public void AddOnEnterCallback(Action<Entity> act)
    {
        onEntityEnter += act;
    }

    public void RemoveOnEnterCallback(Action<Entity> act)
    {
        onEntityEnter -= act;
    }

    public bool BlocksSpearAttacks()
    {
        for (int i = 0; i < mapObjects.Count; i++)
        {
            if (mapObjects[i].isDoor_Closed || mapObjects[i].objectBase.Solid)
            {
                return true;
            }
        }

        return false;
    }

    public bool Walkable
    {
        get { return entity == null && !mapObjects.Any(x => x.objectBase.Solid); }
    }

    public bool Walkable_IgnoreEntity
    {
        get { return !mapObjects.Any(x => x.objectBase.Solid || x.isDoor_Closed); }
    }

    public void RecievePulse(Coord previous, int moveCount, bool on)
    {
        mapObjects.IterateAction_Reverse((x) => x.ReceivePulse(previous, moveCount, on));
    }

    public void SetUnwalkable()
    {
        if (World.tileMap.CurrentMap != null)
        {
            World.tileMap.CurrentMap.SetWalkable(position.x, position.y, false);
        }
    }

    public void SetEntity(Entity e)
    {
        e.cell = this;

        if (entity != e)
        {
            entity = e;
            
            if (World.tileMap.CurrentMap != null)
            {
                World.tileMap.CurrentMap.ModifyTilePathCost(position.x, position.y, EntityInCellCost);
            }

            if (onEntityEnter != null)
            {
                onEntityEnter(entity);
            }

            mapObjects.IterateAction_Reverse((x) => x.OnEntityEnter(e));
        }
    }

    public void UnSetEntity(Entity e)
    {
        e.cell = null;

        if (entity == e)
        {
            mapObjects.IterateAction_Reverse((x) => x.OnEntityExit(e));

            entity = null;

            if (World.tileMap.CurrentMap != null)
            {
                World.tileMap.CurrentMap.ModifyTilePathCost(position.x, position.y, -EntityInCellCost);
            }
        }
    }

    public void AddObject(MapObjectSprite mos)
    {
        mapObjects.Add(mos);

        mos.cell = this;
        mos.SetParams(InSight, hasSeen);

        if (World.tileMap.CurrentMap != null)
        {
            World.tileMap.CurrentMap.ModifyTilePathCost(position.x, position.y, mos.PathCost);
        }
    }

    public void RemoveObject(MapObjectSprite mos)
    {
        if (World.tileMap.CurrentMap != null)
        {
            World.tileMap.CurrentMap.ModifyTilePathCost(position.x, position.y, -mos.PathCost);
        }

        mapObjects.Remove(mos);
        mos.cell = null;
    }

    public void EditPathCost(int editAmount)
    {
        if (World.tileMap.CurrentMap != null)
        {
            World.tileMap.CurrentMap.ModifyTilePathCost(position.x, position.y, editAmount);
        }
    }

    public bool Reset(TileMap_Data oldMap, TileMap_Data newMap)
    {
        mapObjects.IterateAction_Reverse((x) => RemoveObject(x));

        entity = null;
        InSight = false;
        hasSeen = false;

        return true;
    }

    public bool HasInventory()
    {
        return (mapObjects.Any(x => x.inv != null));
    }

    public Inventory MyInventory()
    {
        return (mapObjects.Find(x => x.inv != null).inv);
    }

    public Item GetPool()
    {
        foreach (MapObjectSprite m in mapObjects)
        {
            if (m.inv != null)
            {
                foreach (Item i in m.inv.items)
                {
                    if (i.HasProp(ItemProperty.Pool) && i.HasCComponent<CLiquidContainer>())
                    {
                        return i;
                    }
                }
            }
        }

        return null;
    }

    public void UnregisterCallbacks()
    {
        World.tileMap.OnScreenChange -= Reset;
    }

    public void UpdateInSight(bool _inSight, bool _hasSeen)
    {
        InSight = _inSight;
        hasSeen = _hasSeen;

        foreach (MapObjectSprite mos in mapObjects)
        {
            mos.SetParams(InSight, hasSeen);
        }
    }
}

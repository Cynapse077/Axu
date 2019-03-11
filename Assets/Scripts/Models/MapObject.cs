using UnityEngine;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class MapObject
{
    public int elevation;
    public int pathfindingCost;
    public bool onScreen, seen, solid, opaque;
    public float rotation;
    public string description;
    public List<Item> inv;
    public string objectType;
    public Coord localPosition, worldPosition;
    public ObjectPulseInfo pulseInfo;
    public ProgressFlags[] permissions;

    Dictionary<string, LuaCall> luaEvents;

    public MapObject(string objType, Coord localPos, Coord worldPos, int elev)
    {
        localPosition = localPos;
        worldPosition = worldPos;
        elevation = elev;

        Init(ItemList.GetMOB(objType));
    }

    public MapObject(MapObjectBlueprint bp, Coord lp, Coord wp, int ele)
    {
        localPosition = lp;
        worldPosition = wp;
        elevation = ele;

        Init(bp);
    }

    void Init(MapObjectBlueprint bp)
    {
        ReInitialize(bp);

        if (objectType.Contains("Bloodstain_"))
        {
            SetRotation();
        }
        else if (objectType == "Chest")
        {
            inv = Inventory.GetDrops(SeedManager.combatRandom.Next(1, 4));
        }
        else if (objectType == "Barrel" && SeedManager.combatRandom.Next(100) < 5)
        {
            inv = Inventory.GetDrops(1);
        }
        else if (objectType == "Loot" || objectType == "Body")
        {
            inv = new List<Item>();
        }
    }

    public void ReInitialize(MapObjectBlueprint bp)
    {
        objectType = bp.objectType;
        description = bp.description;
        solid = (bp.solid == MapObjectBlueprint.MapOb_Interactability.Solid);
        opaque = bp.opaque;
        pathfindingCost = bp.pathCost;
        luaEvents = bp.luaEvents;
        pulseInfo = bp.pulseInfo;
        permissions = bp.permissions;
    }

    void SetRotation()
    {
        if (SeedManager.combatRandom.Next(100) < 10 || objectType == "Bloodstain_Permanent")
        {
            rotation = Random.Range(0, 360);
            return;
        }

        List<Coord> possPos = new List<Coord>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int cx = localPosition.x + x, cy = localPosition.y + y;

                if (cx <= 0 || cx >= Manager.localMapSize.x - 1 || cy <= 0 || cy >= Manager.localMapSize.y - 1 || Mathf.Abs(x) + Mathf.Abs(y) > 1 || x == 0 && y == 0)
                    continue;

                if (!World.tileMap.WalkableTile(cx, cy))
                {
                    possPos.Add(new Coord(x, y));
                }
            }
        }

        if (possPos.Count > 0)
        {
            Coord offset = possPos.GetRandom(SeedManager.combatRandom);
            objectType = "Bloodstain_Wall";
            localPosition.x += offset.x;
            localPosition.y += offset.y;
            RotationFromOrientation(offset);
        }
        else
        {
            rotation = Random.Range(0, 360);
            return;
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
        return (pos == worldPosition && elev == elevation);
    }

    public bool HasEvent(string eventName)
    {
        if (luaEvents == null)
        {
            return false;
        }

        return luaEvents.ContainsKey(eventName);
    }

    public LuaCall GetEvent(string eventName)
    {
        return luaEvents[eventName];
    }

    public bool CanDiscard()
    {
        return true;
    }
}
using UnityEngine;
using System.Collections.Generic;

public class MapObjectBlueprint
{
    public string Name, spriteID;
    public string objectType;
    public int light, pathCost;
    public Vector4 tint;
    public bool randomRotation, renderInBack, renderInFront, opaque, autotile;
    public string description;
    public MapOb_Interactability solid;
    public Container container;
    public Dictionary<string, LuaCall> luaEvents;
    public bool sendPulse, recievePulse;

    public MapObjectBlueprint()
    {
        Name = "";
        objectType = "None";
        tint = new Vector4(1, 1, 1, 1);
        randomRotation = false;
        renderInBack = false;
        renderInFront = false;
        autotile = false;
        opaque = false;
        description = "";
        spriteID = "";
        light = 0;
        solid = MapOb_Interactability.No;
        luaEvents = new Dictionary<string, LuaCall>();
    }

    public void SetTint(Vector4 color)
    {
        tint = color;
    }

    public enum MapOb_Interactability
    {
        No,
        Solid,
        Damage
    }

    public class Container
    {
        public int capacity;

        public Container(int _capacity)
        {
            capacity = _capacity;
        }
    }
}

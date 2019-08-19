using UnityEngine;
using LitJson;
using System.Collections.Generic;

public class MapObjectBlueprint : IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public string Name, spriteID;
    public string objectType;
    public int light, pathCost;
    public Vector4 tint;
    public bool randomRotation, renderInBack, renderInFront, opaque, autotile;
    public string description;
    public MapOb_Interactability solid;
    public Container container;
    public Dictionary<string, LuaCall> luaEvents;
    public ObjectPulseInfo pulseInfo;
    public ProgressFlags[] permissions;

    public MapObjectBlueprint()
    {
        Defaults();
    }

    public MapObjectBlueprint(JsonData dat)
    {
        Defaults();
        FromJson(dat);
    }

    public void SetTint(Vector4 color)
    {
        tint = color;
    }

    void Defaults()
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
        permissions = new ProgressFlags[0];
    }

    void FromJson(JsonData dat)
    {
        Name = dat["Name"].ToString();
        ID = objectType = dat["ObjectType"].ToString();
        spriteID = dat["Sprite"].ToString();
        description = dat["Description"].ToString();

        dat.TryGetValue("Path Cost", out pathCost);
        dat.TryGetValue("Physics", out solid, true);
        dat.TryGetValue("Opaque", out opaque);
        dat.TryGetValue("Autotile", out autotile);
        dat.TryGetValue("Render In Back", out renderInBack);
        dat.TryGetValue("Render In Front", out renderInFront);
        dat.TryGetValue("Random Rotation", out randomRotation);
        dat.TryGetValue("Light", out light);

        if (dat.ContainsKey("Container"))
        {
            container = new Container((int)dat["Container"]["Capacity"]);
        }

        if (dat.ContainsKey("Tint"))
        {
            double x = (double)dat["Tint"][0], y = (double)dat["Tint"][1];
            double z = (double)dat["Tint"][2], w = (double)dat["Tint"][3];
            tint = new Vector4((float)x, (float)y, (float)z, (float)w);
        }

        if (dat.ContainsKey("Pulse"))
        {
            if (dat["Pulse"].ContainsKey("Send"))
            {
                pulseInfo.send = (bool)dat["Pulse"]["Send"];
            }

            if (dat["Pulse"].ContainsKey("Receive"))
            {
                pulseInfo.receive = (bool)dat["Pulse"]["Receive"];
            }

            if (dat["Pulse"].ContainsKey("Reverse"))
            {
                pulseInfo.reverse = (bool)dat["Pulse"]["Reverse"];
            }
        }

        if (dat.ContainsKey("Permissions"))
        {
            permissions = new ProgressFlags[dat["Permissions"].Count];

            for (int j = 0; j < dat["Permissions"].Count; j++)
            {
                string perm = dat["Permissions"][j].ToString();
                permissions[j] = perm.ToEnum<ProgressFlags>();
            }
        }

        if (dat.ContainsKey("LuaEvents"))
        {
            for (int j = 0; j < dat["LuaEvents"].Count; j++)
            {
                JsonData luaEvent = dat["LuaEvents"][j];
                string key = luaEvent["Event"].ToString();
                LuaCall lc = new LuaCall(luaEvent["File"].ToString(), luaEvent["Function"].ToString());

                luaEvents.Add(key, lc);
            }
        }
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

public struct ObjectPulseInfo
{
    public bool send;
    public bool receive;
    public bool reverse;
}

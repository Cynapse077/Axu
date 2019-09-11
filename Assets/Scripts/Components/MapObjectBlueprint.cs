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
    public bool randomRotation, opaque, autotile;
    public string description;
    public MapOb_Interactability solid;
    public Container container;
    public Dictionary<string, LuaCall> luaEvents;
    public ObjectPulseInfo pulseInfo;
    public ProgressFlags[] permissions;
    public bool saved = true;
    public ObjectRenderLayer renderLayer;

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
        renderLayer = ObjectRenderLayer.Mid;
        autotile = false;
        opaque = false;
        description = "";
        spriteID = "";
        light = 0;
        solid = MapOb_Interactability.No;
        luaEvents = new Dictionary<string, LuaCall>();
        permissions = new ProgressFlags[0];
        saved = true;
    }

    public void FromJson(JsonData dat)
    {
        if (dat.ContainsKey("Name"))
            Name = dat["Name"].ToString();
        if (dat.ContainsKey("ObjectType"))
            ID = objectType = dat["ObjectType"].ToString();
        if (dat.ContainsKey("Sprite"))
            spriteID = dat["Sprite"].ToString();
        if (dat.ContainsKey("Description"))
            description = dat["Description"].ToString();

        dat.TryGetInt("Path Cost", out pathCost, pathCost);
        dat.TryGetEnum("Physics", out solid, solid);
        dat.TryGetBool("Opaque", out opaque, opaque);
        dat.TryGetBool("Autotile", out autotile, autotile);
        dat.TryGetEnum("Render Layer", out renderLayer, ObjectRenderLayer.Mid);
        dat.TryGetBool("Random Rotation", out randomRotation, randomRotation);
        dat.TryGetInt("Light", out light, light);
        dat.TryGetBool("Saved", out saved, true);
        dat.TryGetBool("Random Rotation", out randomRotation, false);

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
            luaEvents = new Dictionary<string, LuaCall>();

            for (int j = 0; j < dat["LuaEvents"].Count; j++)
            {
                JsonData luaEvent = dat["LuaEvents"][j];
                string key = luaEvent["Event"].ToString();
                LuaCall lc = new LuaCall(luaEvent["Script"].ToString());

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

public enum ObjectRenderLayer
{
    Front,
    Mid, 
    Back
}

public struct ObjectPulseInfo
{
    public bool send;
    public bool receive;
    public bool reverse;
}

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
    public string[] permissions;
    public bool saved = true;
    public bool displayInEditor = true;
    public ObjectRenderLayer renderLayer;

    //For storedObject in Quests
    public Coord localPosition;
    public string zone;
    public int elevation;
    public List<string> inventory;

    public MapObjectBlueprint()
    {
        Defaults();
    }

    public MapObjectBlueprint(MapObjectBlueprint other)
    {
        Defaults();
        CopyFrom(other);
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

    public bool PermissionsMatch()
    {
        if (ObjectManager.playerJournal == null)
        {
            return false;
        }

        for (int i = 0; i < permissions.Length; i++)
        {
            if (!ObjectManager.playerJournal.HasFlag(permissions[i]))
            {
                return false;
            }
        }

        return true;
    }

    void Defaults()
    {
        Name = "";
        ID = "";
        ModID = "";
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
        permissions = new string[0];
        saved = true;
        displayInEditor = true;
    }

    void CopyFrom(MapObjectBlueprint other)
    {
        ID = other.ID;
        ModID = other.ModID;
        Name = other.Name;
        objectType = other.objectType;
        tint = other.tint;
        randomRotation = other.randomRotation;
        renderLayer = other.renderLayer;
        autotile = other.autotile;
        opaque = other.opaque;
        description = other.description;
        spriteID = other.spriteID;
        light = other.light;
        solid = other.solid;
        luaEvents = new Dictionary<string, LuaCall>(other.luaEvents);
        permissions = other.permissions;
        saved = other.saved;
        pathCost = other.pathCost;
        pulseInfo = other.pulseInfo;
        container = other.container;
        displayInEditor = other.displayInEditor;
    }

    public void FromJson(JsonData dat)
    {
        if (dat.ContainsKey("Base") && GameData.TryGet(dat["Base"].ToString(), out MapObjectBlueprint baseBP))
        {
            CopyFrom(baseBP);
        }

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
        dat.TryGetEnum("Render Layer", out renderLayer, renderLayer);
        dat.TryGetBool("Random Rotation", out randomRotation, randomRotation);
        dat.TryGetInt("Light", out light, light);
        dat.TryGetBool("Saved", out saved, saved);
        dat.TryGetBool("Display In Editor", out displayInEditor, displayInEditor);
        dat.TryGetBool("Random Rotation", out randomRotation, randomRotation);

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
            List<string> flags = new List<string>();

            if (permissions != null)
            {
                for (int i = 0; i < permissions.Length; i++)
                {
                    flags.Add(permissions[i]);
                }
            }

            for (int j = 0; j < dat["Permissions"].Count; j++)
            {
                flags.Add(dat["Permissions"][j].ToString());
            }

            permissions = flags.ToArray();
        }

        if (dat.ContainsKey("LuaEvents"))
        {
            if (luaEvents == null)
            {
                luaEvents = new Dictionary<string, LuaCall>();
            }

            for (int j = 0; j < dat["LuaEvents"].Count; j++)
            {
                JsonData luaEvent = dat["LuaEvents"][j];
                string key = luaEvent["Event"].ToString();
                LuaCall lc = new LuaCall(luaEvent["Script"].ToString());

                luaEvents.Add(key, lc);
            }
        }
    }

    public IEnumerable<string> LoadErrors()
    {
        yield break;
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

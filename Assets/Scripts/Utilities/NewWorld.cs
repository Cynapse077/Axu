using System.Collections.Generic;
using System.IO;
using LitJson;

public class NewWorld
{
    public static bool doneSaving = false;
    const int Cutoffthreshold = 6000;

    readonly int Turn_Num;
    readonly string Version;
    TileMap tileMap;
    List<NPCCharacter> chars;
    WorldToJson w2json;

    public NewWorld(List<NPCCharacter> c, int turn)
    {
        chars = new List<NPCCharacter>(c);
        tileMap = World.tileMap;
        Turn_Num = turn;
        Version = GameSettings.version;
        doneSaving = false;

        SetWorldData();
    }

    void SetWorldData()
    {
        doneSaving = false;
        PlayerCharacter player = ObjectManager.playerEntity.ToCharacter();

        //world
        List<MapObject> mapObjects = World.objectManager.mapObjects;
        w2json = new WorldToJson(chars, mapObjects, Turn_Num, World.DangerLevel(), ObjectManager.SpawnedNPCs, World.difficulty);

        //local
        ScreenHolder holder = new ScreenHolder(tileMap.worldCoordX, tileMap.worldCoordY);
        for (int x = 0; x < Manager.worldMapSize.x; x++)
        {
            for (int y = 0; y < Manager.worldMapSize.y; y++)
            {
                TileMap_Data tmData = tileMap.GetScreen(x, y);

                if (tmData != null)
                {
                    AddDataToArray(ref holder, new ScreenToJson(tmData, 0));
                }
                else if (tileMap.worldData != null && tileMap.worldData.dataExists(x, y))
                {
                    tmData = new TileMap_Data(x, y, 0, true) { lastTurnSeen = tileMap.worldData.LastTurnSeen(x, y) };
                    AddDataToArray(ref holder, new ScreenToJson(tmData, 0));
                }
            }
        }

        //vaults
        for (int i = 0; i < World.tileMap.Vaults.Count; i++)
        {
            for (int j = 0; j < World.tileMap.Vaults[i].screens.Length; j++)
            {
                TileMap_Data tmd = World.tileMap.Vaults[i].screens[j];

                if (tmd != null)
                {
                    AddDataToArray(ref holder, new ScreenToJson(tmd, tmd.elevation));
                }
            }
        }

        GameSave save = new GameSave(Version, player, w2json, holder);
        SaveFile(save, player.Name);
    }

    void SaveFile(GameSave save, string playerName)
    {
        JsonData saveJson = JsonMapper.ToJson(save);
        string saveFilePath = Manager.SaveDirectory + "/" + playerName + ".axu";

        File.WriteAllText(saveFilePath, saveJson.ToString());
        doneSaving = true;
    }

    void RemoveStuffAt(int x, int y, int z)
    {
        List<NPCCharacter> npcs = new List<NPCCharacter>(chars);

        foreach (NPCCharacter n in npcs)
        {
            if (n.WP[0] == x && n.WP[1] == y && n.WP[2] == z && n.CanDiscard())
            {
                chars.Remove(n);
            }
        }

        List<WorldObject> wObjects = new List<WorldObject>(w2json.Objects);

        foreach (WorldObject w in wObjects)
        {
            if (w.WP[0] == x && w.WP[1] == y && w.WP[2] == z)
            {
                w2json.Objects.Remove(w);
            }
        }
    }

    void AddDataToArray(ref ScreenHolder holder, ScreenToJson s2json)
    {
        if (s2json.LTS > 0 && World.objectManager.CanDiscard(s2json.P[0], s2json.P[1], s2json.P[2]) && 
            Turn_Num - s2json.LTS >= Cutoffthreshold)
        {
			RemoveStuffAt(s2json.P[0], s2json.P[1], s2json.P[2]);
		} else
        {
            holder.Sc.Add(s2json);
        }
    }
}

//Loading world data from the json string
public class OldWorld
{
    JsonData wData;
    string loadJson;

    public OldWorld()
    {
        LoadWorldData();
    }

    public void LoadWorldData()
    {
        loadJson = File.ReadAllText(Manager.SaveDirectory + "/" + Manager.playerName + ".axu");
        wData = JsonMapper.ToObject(loadJson)["World"];
        ObjectManager objM = World.objectManager;
        objM.GetComponent<TurnManager>().turn = (int)wData["Turn_Num"];

        World.BaseDangerLevel = (int)wData["Danger_Level"];
        World.difficulty = new Difficulty((Difficulty.DiffLevel)((int)wData["Diff"]["Level"]), (double)wData["Diff"]["XPScale"], wData["Diff"]["descTag"].ToString());
        ObjectManager.SpawnedNPCs = (int)wData["Spawned_NPCs"];

        //Load NPCs
        for (int i = 0; i < wData["NPCs"].Count; i++)
        {
            JsonData npcData = wData["NPCs"][i];
            Coord worldPos = (npcData["WP"] == null) ? new Coord(0, 0) : new Coord((int)npcData["WP"][0], (int)npcData["WP"][1]);
            Coord localPos = new Coord((int)npcData["LP"][0], (int)npcData["LP"][1]);
            NPC n = new NPC(npcData["ID"].ToString(), worldPos, localPos, (int)wData["NPCs"][i]["WP"][2]);

            if (n.faction.ID == "followers" && !n.flags.Contains(NPC_Flags.Follower))
            {
                n.flags.Add(NPC_Flags.Follower);
            }

            n.name = npcData["Name"].ToString();
            n.UID = (int)npcData["UID"];
            n.faction = FactionList.GetFactionByID(npcData["Fac"].ToString());
            n.isHostile = (bool)npcData["Host"];
            n.inventory = SetUpInventory(i);
            n.spriteID = npcData["Spr"].ToString();

            n.handItems = new List<Item>();
            for (int j = 0; j < npcData["HIt"].Count; j++)
            {
                n.handItems.Add(SaveData.GetItemFromJsonData(npcData["HIt"][j]));
            }

            n.firearm = SaveData.GetItemFromJsonData(wData["NPCs"][i]["F"]);

            objM.CreateNPC(n);
            ObjectManager.SpawnedNPCs--;

            if (npcData["QN"] != null)
            {
                n.questID = npcData["QN"].ToString();
            }
        }

        //Get map objects
        for (int i = 0; i < wData["Objects"].Count; i++)
        {
            JsonData obj = wData["Objects"][i];
            Coord lp = new Coord((int)obj["LP"][0], (int)wData["Objects"][i]["LP"][1]);
            Coord wp = new Coord((int)obj["WP"][0], (int)wData["Objects"][i]["WP"][1]);
            int elevation = (int)obj["WP"][2];
            string type = obj["Type"].ToString();

            MapObject m = new MapObject(type, new Coord(lp.x, lp.y), wp, elevation);

            //Inventory
            if (obj["Items"] != null)
            {
                m.inv = new List<Item>();

                for (int j = 0; j < obj["Items"].Count; j++)
                {

                    if (j < 50)
                    {
                        m.inv.Add(SaveData.GetItemFromJsonData(obj["Items"][j]));
                    }
                }
            }

            objM.AddMapObject(m);
        }
    }

    List<Item> SetUpInventory(int num)
    {
        List<Item> items = new List<Item>();

        if (wData["NPCs"][num]["Inv"].Count <= 0)
            return items;

        for (int i = 0; i < wData["NPCs"][num]["Inv"].Count; i++)
        {
            items.Add(SaveData.GetItemFromJsonData(wData["NPCs"][num]["Inv"][i]));
        }

        return items;
    }
}

public class OldScreens
{
    readonly JsonData sData;
    readonly string loadJson;

    public OldScreens()
    {
        loadJson = File.ReadAllText(Manager.SaveDirectory + "/" + Manager.playerName + ".axu");
        sData = JsonMapper.ToObject(loadJson)["Local"];
    }

    public int LastTurnSeen(int x, int y, int z = 0)
    {
        JsonData sc = sData["Sc"];

        for (int i = 0; i < sc.Count; i++)
        {
            if ((int)sc[i]["P"][0] == x && (int)sc[i]["P"][1] == y)
            {
                if ((int)sc[i]["P"][2] == z)
                {
                    return (int)sc[i]["LTS"];
                }
            }
        }

        return World.turnManager.turn;
    }

    public bool dataExists(int x, int y, int z = 0)
    {
        JsonData sc = sData["Sc"];

        for (int i = 0; i < sc.Count; i++)
        {
            if ((int)sc[i]["P"][0] == x && (int)sc[i]["P"][1] == y)
            {
                if ((int)sc[i]["P"][2] == z)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public Coord GetStartPos()
    {
        int startX = (int)(sData["P"][0]);
        int startY = (int)(sData["P"][1]);

        return new Coord(startX, startY);
    }

    public List<TileMap_Data.TileChange> GetChanges(Coord pos)
    {
        List<TileMap_Data.TileChange> changes = new List<TileMap_Data.TileChange>();

        for (int i = 0; i < sData["Sc"].Count; i++)
        {
            if ((int)sData["Sc"][i]["P"][0] == pos.x && (int)sData["Sc"][i]["P"][1] == pos.y)
            {
                if (sData["Sc"][i]["Ch"] != null)
                {
                    for (int y = 0; y < sData["Sc"][i]["Ch"].Count; y++)
                    {
                        TileMap_Data.TileChange c = new TileMap_Data.TileChange((int)sData["Sc"][i]["Ch"][y]["x"], (int)sData["Sc"][i]["Ch"][y]["y"], (int)sData["Sc"][i]["Ch"][y]["tType"]);
                        changes.Add(c);
                    }
                }

                return changes;
            }
        }

        return changes;
    }
}
//World map simplified for json
[System.Serializable]
public class WorldToJson
{
    public List<NPCCharacter> NPCs;
    public List<WorldObject> Objects;
    public int Turn_Num, Danger_Level, Spawned_NPCs;
    public Difficulty Diff;
    public string Time;

    public WorldToJson(List<NPCCharacter> npcs, List<MapObject> objects, int tn, int dl, int spwned, Difficulty diff)
    {
        NPCs = npcs;
        Turn_Num = tn;
        Danger_Level = dl;
        Spawned_NPCs = spwned;
        Diff = diff;
        Time = System.DateTime.Now.ToString();

        Objects = new List<WorldObject>();

        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i].objectType != "Bloodstain" && objects[i].objectType != "Bloodstain_Wall")
            {
                Objects.Add(new WorldObject(objects[i].worldPosition, objects[i].localPosition, objects[i].elevation, objects[i].objectType, objects[i].inv));
            }
        }
    }
}

//List of screens
[System.Serializable]
public struct ScreenHolder
{
    public List<ScreenToJson> Sc; //Screent
    public int[] P; //Start Position

    public ScreenHolder(int startX, int startY)
    {
        Sc = new List<ScreenToJson>();
        P = new int[2] { startX, startY };
    }
}

//local map simplified for json
[System.Serializable]
public struct ScreenToJson
{
    public int[] P; //Position
    public int LTS; //Last Turn Seen
    public List<TileMap_Data.TileChange> Ch; //Changes

    public ScreenToJson(TileMap_Data dt, int elevation)
    {
        P = new int[3] { dt.mapInfo.position.x, dt.mapInfo.position.y, dt.elevation };
        LTS = dt.lastTurnSeen;
        Ch = new List<TileMap_Data.TileChange>();

        if (dt.changes != null)
        {
            Ch = dt.changes;
        }
    }
}

[System.Serializable]
public struct GameSave
{
    public string Version;
    public PlayerCharacter Player;
    public WorldToJson World;
    public ScreenHolder Local;

    public GameSave(string vers, PlayerCharacter player, WorldToJson world, ScreenHolder local)
    {
        Version = vers;
        Player = player;
        World = world;
        Local = local;
    }
}

[System.Serializable]
public struct WorldObject
{
    public int[] WP;
    public int[] LP;
    public List<SItem> Items;
    public string Type;

    public WorldObject(Coord worldPos, Coord localPos, int elev, string type, List<Item> items)
    {
        WP = new int[3] { worldPos.x, worldPos.y, elev };
        LP = new int[2] { localPos.x, localPos.y };
        Type = type;

        Items = new List<SItem>();
        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    Items.Add(items[i].ToSimpleItem());
                }
            }
        }
    }
}

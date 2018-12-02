using System.Collections.Generic;
using System.IO;
using LitJson;
using UnityEngine;

public static class QuestList
{
    static List<Quest> quests;
    public static string dataPath;

    public static void InitializeFromJson()
    {
        new EventHandler();

        quests = new List<Quest>();

        string jsonString = File.ReadAllText(Application.streamingAssetsPath + dataPath);

        JsonData data = JsonMapper.ToObject(jsonString);

        for (int i = 0; i < data["Quests"].Count; i++)
        {
            quests.Add(new Quest(data["Quests"][i]));
        }
    }

    public static Quest GetByID(string id)
    {
        Quest q = quests.Find(x => x.ID == id);

        if (q != null)
        {
            return new Quest(quests.Find(x => x.ID == id));
        }
        else
        {
            MyConsole.Error("No quest with ID of \"" + id);
            return null;
        }
    }

    static Coord GetZone(string zone)
    {
        return World.worldMap.worldMapData.GetLandmark(zone);
    }

    public static QuestEvent GetEvent(string key, JsonData e)
    {
        //TODO: Move this to a ToJson function in each class.
        switch (key)
        {
            case "Spawn NPC":
                string npcID = e["NPC"].ToString();
                Coord npcWorldPos = GetZone(e["Coordinate"].ToString());
                int npcElevation = (int)e["Elevation"];
                Coord npcLocalPos = new Coord(SeedManager.combatRandom.Next(2, Manager.localMapSize.x - 2), SeedManager.combatRandom.Next(2, Manager.localMapSize.y - 2));

                if (e.ContainsKey("Local Position"))
                {
                    npcLocalPos.x = (int)e["Local Position"]["x"];
                    npcLocalPos.y = (int)e["Local Position"]["y"];
                }

                string giveItem = e.ContainsKey("Give Item") ? e["Give Item"].ToString() : "";

                return new SpawnNPCEvent(npcID, npcWorldPos, npcLocalPos, npcElevation, giveItem);

            case "Spawn Group":
                string groupID = e["Group"].ToString();
                Coord groupWorldPos = GetZone(e["Coordinate"].ToString());
                int groupElevation = (int)e["Elevation"];
                int groupAmount = (int)e["Amount"];

                return new SpawnNPCGroupEvent(groupID, groupWorldPos, groupElevation, groupAmount);

            case "Spawn Object":
                string objectID = e["Object"].ToString();
                Coord objectWorldPos = GetZone(e["Coordinate"].ToString());
                Coord objectLocalPos = new Coord((int)e["Local Position"][0], (int)e["Local Position"][1]);
                int objectElevation = (int)e["Elevation"];
                string objectGiveItem = e.ContainsKey("Give Item") ? e["Give Item"].ToString() : "";

                return new SpawnObjectEvent(objectID, objectWorldPos, objectLocalPos, objectElevation, objectGiveItem);

            case "Remove Spawns":
                return new RemoveAllSpawnedNPCsEvent();

            case "Move NPC":
                Coord npcMoveWorldPos = GetZone(e["Coordinate"].ToString());
                Coord npcMoveLocalPos = new Coord((int)e["Local Position"][0], (int)e["Local Position"][1]);
                int moveNPCEle = (int)e["Elevation"];
                string npcMoveID = e["NPC"].ToString();

                return new MoveNPCEvent(npcMoveID, npcMoveWorldPos, npcMoveLocalPos, moveNPCEle);

            case "Give Quest":
                string giveQuestNPC = e["NPC"].ToString();
                string giveQuestID = e["Quest"].ToString();

                return new GiveNPCQuestEvent(giveQuestNPC, giveQuestID);

            case "Spawn Blocker":
                Coord blockerWorldPos = GetZone(e["Coordinate"].ToString());
                Coord blockerLocalPos = new Coord((int)e["Local Position"][0], (int)e["Local Position"][1]);
                int blockerEle = (int)e["Elevation"];

                return new PlaceBlockerEvent(blockerWorldPos, blockerLocalPos, blockerEle);

            case "Remove Blockers":
                Coord remBlockerPos = GetZone(e["Coordinate"].ToString());
                int remBlockEle = (int)e["Elevation"];

                return new RemoveBlockersEvent(remBlockerPos, remBlockEle);

            case "Console Command":
                return new ConsoleCommandEvent(e.ToString());

            case "Remove Item":
                string removeNPC = e["NPC"].ToString();
                string removeItem = e["Item"].ToString();
                string replacement = e.ContainsKey("Replacement") ? e["Replacement"].ToString() : "";

                return new ReplaceItemOnNPCEvent(removeNPC, removeItem, replacement);

            case "Set Local Position":
                Coord lp = new Coord((int)e["x"], (int)e["y"]);

                return new LocalPosChangeEvent(lp);

            case "Set World Position":
                Coord wp = GetZone(e["Coordinate"].ToString());
                int ele = (int)e["Elevation"];

                return new WorldPosChangeEvent(wp, ele);

            case "Set Elevation":
                int newEle = (int)e;

                return new ElevationChangeEvent(newEle);

            default:
                Debug.LogError("Get Quest Event - No event with ID " + key + ".");
                return null;
        }
    }
}
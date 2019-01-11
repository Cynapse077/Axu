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

        if (q == null)
        {
            MyConsole.Error("No quest with ID of \"" + id);
        }

        return q;
    }

    static Coord GetZone(string zone)
    {
        return World.worldMap.worldMapData.GetLandmark(zone);
    }

    public static QuestEvent GetEvent(string key, JsonData data)
    {
        //TODO: Move this to a FromJson function in each class.
        switch (key)
        {
            case "Spawn NPC":
                string npcID = data["NPC"].ToString();
                Coord npcWorldPos = GetZone(data["Coordinate"].ToString());
                int npcElevation = (int)data["Elevation"];
                Coord npcLocalPos = new Coord(SeedManager.combatRandom.Next(2, Manager.localMapSize.x - 2), SeedManager.combatRandom.Next(2, Manager.localMapSize.y - 2));

                if (data.ContainsKey("Local Position"))
                {
                    npcLocalPos.x = (int)data["Local Position"]["x"];
                    npcLocalPos.y = (int)data["Local Position"]["y"];
                }

                string giveItem = data.ContainsKey("Give Item") ? data["Give Item"].ToString() : "";

                return new SpawnNPCEvent(npcID, npcWorldPos, npcLocalPos, npcElevation, giveItem);

            case "Spawn Group":
                string groupID = data["Group"].ToString();
                Coord groupWorldPos = GetZone(data["Coordinate"].ToString());
                int groupElevation = (int)data["Elevation"];
                int groupAmount = (int)data["Amount"];

                return new SpawnNPCGroupEvent(groupID, groupWorldPos, groupElevation, groupAmount);

            case "Spawn Object":
                string objectID = data["Object"].ToString();
                Coord objectWorldPos = GetZone(data["Coordinate"].ToString());
                Coord objectLocalPos = new Coord((int)data["Local Position"][0], (int)data["Local Position"][1]);
                int objectElevation = (int)data["Elevation"];
                string objectGiveItem = data.ContainsKey("Give Item") ? data["Give Item"].ToString() : "";

                return new SpawnObjectEvent(objectID, objectWorldPos, objectLocalPos, objectElevation, objectGiveItem);

            case "Remove Spawns":
                return new RemoveAllSpawnedNPCsEvent();

            case "Move NPC":
                Coord npcMoveWorldPos = GetZone(data["Coordinate"].ToString());
                Coord npcMoveLocalPos = new Coord((int)data["Local Position"][0], (int)data["Local Position"][1]);
                int moveNPCEle = (int)data["Elevation"];
                string npcMoveID = data["NPC"].ToString();

                return new MoveNPCEvent(npcMoveID, npcMoveWorldPos, npcMoveLocalPos, moveNPCEle);

            case "Give Quest":
                string giveQuestNPC = data["NPC"].ToString();
                string giveQuestID = data["Quest"].ToString();

                return new GiveNPCQuestEvent(giveQuestNPC, giveQuestID);

            case "Spawn Blocker":
                Coord blockerWorldPos = GetZone(data["Coordinate"].ToString());
                Coord blockerLocalPos = new Coord((int)data["Local Position"][0], (int)data["Local Position"][1]);
                int blockerEle = (int)data["Elevation"];

                return new PlaceBlockerEvent(blockerWorldPos, blockerLocalPos, blockerEle);

            case "Remove Blockers":
                Coord remBlockerPos = GetZone(data["Coordinate"].ToString());
                int remBlockEle = (int)data["Elevation"];

                return new RemoveBlockersEvent(remBlockerPos, remBlockEle);

            case "Console Command":
                return new ConsoleCommandEvent(data.ToString());

            case "Remove Item":
                string removeNPC = data["NPC"].ToString();
                string removeItem = data["Item"].ToString();
                string replacement = data.ContainsKey("Replacement") ? data["Replacement"].ToString() : "";

                return new ReplaceItemOnNPCEvent(removeNPC, removeItem, replacement);

            case "Set Local Position":
                Coord lp = new Coord((int)data["x"], (int)data["y"]);

                return new LocalPosChangeEvent(lp);

            case "Set World Position":
                Coord wp = GetZone(data["Coordinate"].ToString());
                int ele = (int)data["Elevation"];

                return new WorldPosChangeEvent(wp, ele);

            case "Set Elevation":
                int newEle = (int)data;

                return new ElevationChangeEvent(newEle);

            case "Remove NPC":
                string remNPC = data["NPC"].ToString();

                return new RemoveNPCEvent(remNPC);

            case "Remove NPCs At":
                Coord remcoord = GetZone(data["Coordinate"].ToString());
                int remele = (int)data["Elevation"];

                return new RemoveNPCsAtEvent(remcoord, remele);

            case "Become Follower":
                string folNPC = data["NPC"].ToString();

                return new BecomeFollowerEvent(folNPC);

            case "Set NPC Dialogue":
                string dialogueNPC = data["NPC"].ToString();
                string diaID = data["Dialogue"].ToString();

                return new SetNPCDialogueTree(dialogueNPC, diaID);

            default:
                Debug.LogError("QuestList::GetEvent() - No event with ID \"" + key + "\".");
                return null;
        }
    }
}
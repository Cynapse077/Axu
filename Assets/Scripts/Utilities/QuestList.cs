using System.Collections.Generic;
using LitJson;
using UnityEngine;

public static class QuestList
{
    static List<Quest> quests
    {
        get { return GameData.GetAll<Quest>(); }
    }

    public static Quest GetByID(string id)
    {
        Quest q = GameData.Get<Quest>(id);

        if (q != null)
        {
            return new Quest(q);
        }

        return null;
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
                int npcElevation = (int)data["Elevation"];
                Coord npcLocalPos = new Coord(SeedManager.combatRandom.Next(2, Manager.localMapSize.x - 2), SeedManager.combatRandom.Next(2, Manager.localMapSize.y - 2));

                if (data.ContainsKey("Local Position"))
                {
                    npcLocalPos.x = (int)data["Local Position"]["x"];
                    npcLocalPos.y = (int)data["Local Position"]["y"];
                }

                string giveItem = data.ContainsKey("Give Item") ? data["Give Item"].ToString() : string.Empty;

                List<string> giveItems = new List<string>();

                if (data.ContainsKey("Give Items"))
                {
                    for (int i = 0; i < data["Give Items"].Count; i++)
                    {
                        giveItems.Add(data["Give Items"][i].ToString());
                    }
                }

                return new SpawnNPCEvent(npcID, data["Coordinate"].ToString(), npcLocalPos, npcElevation, giveItem, giveItems);

            case "Spawn Group":
                string groupID = data["Group"].ToString();
                int groupElevation = (int)data["Elevation"];
                int groupAmount = (int)data["Amount"];

                return new SpawnNPCGroupEvent(groupID, data["Coordinate"].ToString(), groupElevation, groupAmount);

            case "Spawn Object":
                string objectID = data["Object"].ToString();
                Coord objectLocalPos = new Coord((int)data["Local Position"][0], (int)data["Local Position"][1]);
                int objectElevation = (int)data["Elevation"];
                string objectGiveItem = data.ContainsKey("Give Item") ? data["Give Item"].ToString() : "";

                return new SpawnObjectEvent(objectID, data["Coordinate"].ToString(), objectLocalPos, objectElevation, objectGiveItem);

            case "Remove Spawns":
                return new RemoveAllSpawnedNPCsEvent();

            case "Move NPC":
                Coord npcMoveLocalPos = new Coord((int)data["Local Position"][0], (int)data["Local Position"][1]);
                int moveNPCEle = (int)data["Elevation"];
                string npcMoveID = data["NPC"].ToString();

                return new MoveNPCEvent(npcMoveID, data["Coordinate"].ToString(), npcMoveLocalPos, moveNPCEle);

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

            case "Open Dialogue":
                string speaker = data["Speaker"].ToString();
                string dialogue = data["Dialogue"].ToString();

                return new OpenDialogue(speaker, dialogue);

            case "Create Location":
                string zoneID = data["Zone ID"].ToString();

                return new CreateLocation(zoneID);

            case "Remove Location":
                string remID = data["Zone ID"].ToString();

                return new RemoveLocation(remID);

            default:
                Debug.LogError("QuestList::GetEvent() - No event with ID \"" + key + "\".");
                return null;
        }
    }
}
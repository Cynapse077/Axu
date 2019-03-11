using System.Collections.Generic;

public class QuestEvent
{
    public Quest myQuest;
    public QuestEvent() { }

    public virtual void RunEvent() { }

    public enum EventType
    {
        OnStart, OnComplete, OnFail
    }
}

public class LuaQuestEvent : QuestEvent
{
    readonly LuaCall luaCall;

    public LuaQuestEvent(LuaCall lc)
    {
        luaCall = lc;
    }

    public override void RunEvent()
    {
        LuaManager.CallScriptFunction(luaCall);
    }
}

public class WorldPosChangeEvent : QuestEvent
{
    readonly Coord worldPos;
    readonly int elevation;

    public WorldPosChangeEvent(Coord wPos, int ele)
    {
        worldPos = wPos;
        elevation = ele;
    }

    public override void RunEvent()
    {
        World.tileMap.worldCoordX = worldPos.x;
        World.tileMap.worldCoordY = worldPos.y;
        World.tileMap.currentElevation = elevation;
        World.tileMap.HardRebuild();
    }
}

public class LocalPosChangeEvent : QuestEvent
{
    readonly Coord localPos;

    public LocalPosChangeEvent(Coord lPos)
    {
        localPos = lPos;
    }

    public override void RunEvent()
    {
        ObjectManager.playerEntity.ForcePosition(new Coord(localPos.x, localPos.y));
        ObjectManager.playerEntity.BeamDown();
        World.tileMap.SoftRebuild();
    }
}

public class ElevationChangeEvent : QuestEvent
{
    readonly int elevation;

    public ElevationChangeEvent(int ele)
    {
        elevation = ele;
    }

    public override void RunEvent()
    {
        World.tileMap.currentElevation = elevation;
        World.tileMap.HardRebuild();
    }
}

public class SpawnNPCEvent : QuestEvent
{
    readonly string npcID;
    readonly string giveItem;
    readonly string zone;
    readonly int elevation;
    readonly Coord localPos;

    public SpawnNPCEvent(string nID, string z, Coord lPos, int ele, string gItem = "")
    {
        npcID = nID;
        zone = z;
        localPos = lPos;
        elevation = ele;
        giveItem = gItem;
    }

    public override void RunEvent()
    {
        Coord wPos = World.worldMap.worldMapData.GetLandmark(zone);
        NPC n = new NPC(EntityList.GetBlueprintByID(npcID), wPos, localPos, elevation);

        if (giveItem != "")
        {
            n.inventory.Add(ItemList.GetItemByID(giveItem));
        }

        myQuest.SpawnNPC(n);
    }
}

public class SpawnNPCGroupEvent : QuestEvent
{
    readonly string groupID;
    readonly string zone;
    readonly int elevation;
    readonly int amount;

    public SpawnNPCGroupEvent(string group, string wPos, int ele, int amt)
    {
        groupID = group;
        zone = wPos;
        elevation = ele;
        amount = amt;
    }

    public override void RunEvent()
    {
        Coord wPos = World.worldMap.worldMapData.GetLandmark(zone);
        List<NPC> ns = SpawnController.SpawnFromGroupNameAt(groupID, amount, wPos, elevation);

        foreach (NPC n in ns)
        {
            myQuest.spawnedNPCs.Add(n.UID);
        }
    }
}

public class RemoveAllSpawnedNPCsEvent : QuestEvent
{
    public RemoveAllSpawnedNPCsEvent() { }

    public override void RunEvent()
    {
        foreach (int uid in myQuest.spawnedNPCs)
        {
            NPC n = World.objectManager.npcClasses.Find(x => x.UID == uid);

            if (n != null)
            {
                World.objectManager.npcClasses.Remove(n);
            }
        }

        myQuest.spawnedNPCs.Clear();
    }
}

public class SpawnObjectEvent : QuestEvent
{
    readonly string objectID;
    readonly string giveItem;
    readonly string zone;
    readonly int elevation;
    readonly Coord localPos;

    public SpawnObjectEvent(string oID, string wPos, Coord lPos, int ele, string gItem = "")
    {
        objectID = oID;
        zone = wPos;
        localPos = lPos;
        elevation = ele;
        giveItem = gItem;
    }

    public override void RunEvent()
    {
        Coord c = myQuest.GetZone(zone);

        if (c != null)
        {
            MapObject m = World.objectManager.NewObjectAtOtherScreen(objectID, localPos, c, elevation);

            if (giveItem != "")
            {
                m.inv.Add(ItemList.GetItemByID(giveItem));
            }
        }        
    }
}

public class ConsoleCommandEvent : QuestEvent
{
    readonly string command;

    public ConsoleCommandEvent(string com)
    {
        command = com;
    }

    public override void RunEvent()
    {
        string[] cons = command.Split("|"[0]);
        Console console = World.objectManager.GetComponent<Console>();

        for (int i = 0; i < cons.Length; i++)
        {
            console.ParseTextField(cons[i]);
        }
    }
}

public class GiveNPCQuestEvent : QuestEvent
{
    readonly string npcID;
    readonly string questID;

    public GiveNPCQuestEvent(string nID, string qID)
    {
        npcID = nID;
        questID = qID;
    }

    public override void RunEvent()
    {
        NPC n = World.objectManager.npcClasses.Find(x => x.ID == npcID);

        if (n != null)
        {
            n.questID = questID;
        }
    }
}

public class MoveNPCEvent : QuestEvent
{
    readonly string npcID;
    readonly string zone;
    readonly Coord localPos;
    readonly int elevation;

    public MoveNPCEvent(string nID, string wPos, Coord lPos, int ele)
    {
        npcID = nID;
        zone = wPos;
        localPos = lPos;
        elevation = ele;
    }

    public override void RunEvent()
    {
        Coord c = myQuest.GetZone(zone);
        NPC n = World.objectManager.npcClasses.Find(x => x.ID == npcID);

        if (n != null)
        {
            if (zone != null)
            {
                n.worldPosition = c;
            }

            if (localPos != null)
            {
                n.localPosition = localPos;
            }

            n.elevation = elevation;

            World.tileMap.HardRebuild();
        }
        else
        {
            //Create NPC
            NPC_Blueprint bp = EntityList.GetBlueprintByID(npcID);

            if (bp == null)
            {
                UnityEngine.Debug.LogError("NPC with ID " + npcID + " does not exist.");
                return;
            }

            NPC npc = new NPC(bp, new Coord(0, 0), new Coord(0, 0), 0)
            {
                localPosition = localPos,
                elevation = elevation,
                worldPosition = c
            };

            World.objectManager.CreateNPC(npc);
        }
    }
}

public class ReplaceItemOnNPCEvent : QuestEvent
{
    readonly string npcID;
    readonly string itemID;
    readonly string replacementID;

    public ReplaceItemOnNPCEvent(string nID, string iID, string replacement)
    {
        npcID = nID;
        itemID = iID;
        replacementID = replacement;
    }

    public override void RunEvent()
    {
        NPC n = World.objectManager.npcClasses.Find(x => x.ID == npcID);

        if (n != null)
        {
            for (int e = 0; e < n.handItems.Count; e++)
            {
                if (n.handItems[e].ID == itemID)
                {
                    n.handItems[e] = ItemList.GetItemByID(replacementID);
                    return;
                }
            }

            if (n.firearm != null && n.firearm.ID == itemID)
            {
                n.firearm = ItemList.GetItemByID(replacementID);
                return;
            }

            for (int b = 0; b < n.bodyParts.Count; b++)
            {
                if (n.bodyParts[b].equippedItem != null && n.bodyParts[b].equippedItem.ID == itemID)
                {
                    n.bodyParts[b].equippedItem = ItemList.GetItemByID(replacementID);
                    return;
                }
            }
        }
    }
}

public class PlaceBlockerEvent : QuestEvent
{
    readonly Coord worldPos;
    readonly Coord localPos;
    readonly int elevation;

    public PlaceBlockerEvent(Coord wPos, Coord lPos, int ele)
    {
        worldPos = wPos;
        localPos = lPos;
        elevation = ele;
    }

    public override void RunEvent()
    {
        World.objectManager.NewObjectAtOtherScreen("Stair_Lock", localPos, worldPos, elevation);
    }
}

public class RemoveBlockersEvent : QuestEvent
{
    readonly Coord worldPos;
    readonly int elevation;

    public RemoveBlockersEvent(Coord wPos, int ele)
    {
        worldPos = wPos;
        elevation = ele;
    }

    public override void RunEvent()
    {
        List<MapObject> mos = World.objectManager.ObjectsAt(worldPos, elevation);
        List<MapObject> toDelete = mos.FindAll(x => x.objectType == "Stair_Lock");

        while (toDelete.Count > 0)
        {
            World.objectManager.mapObjects.Remove(toDelete[0]);
            toDelete.RemoveAt(0);
        }

        if (World.tileMap.WorldPosition == worldPos && World.tileMap.currentElevation == elevation)
        {
            World.tileMap.HardRebuild();
        }
    }
}

public class BecomeFollowerEvent : QuestEvent
{
    readonly string npcID;

    public BecomeFollowerEvent(string npc)
    {
        npcID = npc;
    }

    public override void RunEvent()
    {
        NPC n = World.objectManager.npcClasses.Find(x => x.ID == npcID);

        if (n != null)
        {
            n.MakeFollower();
            World.tileMap.HardRebuild();
        }
    }
}

public class CompleteStepEvent : QuestEvent
{
    readonly Goal goal;

    public CompleteStepEvent(Goal g)
    {
        goal = g;
    }

    public override void RunEvent()
    {
        goal.Complete();
    }
}

public class RemoveNPCEvent : QuestEvent
{
    readonly string npcID;

    public RemoveNPCEvent(string nID)
    {
        npcID = nID;
    }

    public override void RunEvent()
    {
        NPC n = World.objectManager.npcClasses.Find(x => x.ID == npcID);

        if (n != null)
        {
            ObjectManager.playerJournal.staticNPCKills.Add(n.ID);
            World.objectManager.npcClasses.Remove(n);
        }
    }
}

public class RemoveNPCsAtEvent : QuestEvent
{
    readonly Coord worldPos;
    readonly int elevation;

    public RemoveNPCsAtEvent(Coord wp, int ele)
    {
        worldPos = wp;
        elevation = ele;
    }

    public override void RunEvent()
    {
        List<NPC> npcsAt = World.objectManager.NPCsAt(worldPos, elevation);

        while (npcsAt.Count > 0)
        {
            World.objectManager.npcClasses.Remove(npcsAt[0]);
            npcsAt.RemoveAt(0);
        }
    }
}

public class SetNPCDialogueTree : QuestEvent
{
    readonly string npcID;
    readonly string dialogueID;

    public SetNPCDialogueTree(string nID, string dID)
    {
        npcID = nID;
        dialogueID = dID;
    }

    public override void RunEvent()
    {
        NPC n = World.objectManager.npcClasses.Find(x => x.ID == npcID);

        if (n != null)
        {
            n.dialogueID = dialogueID;
        }
    }
}

public class OpenDialogue : QuestEvent
{
    readonly string speaker;
    readonly string dialogue;

    public OpenDialogue(string spkr, string dia)
    {
        speaker = spkr;
        dialogue = dia;
    }

    public override void RunEvent()
    {
        Alert.CustomAlert_WithTitle(speaker, dialogue);
    }
}

public class CreateLocation : QuestEvent
{
    readonly string zoneID;

    public CreateLocation(string zone)
    {
        zoneID = zone;
    }

    public override void RunEvent()
    {
        ZoneBlueprint zb = World.worldMap.worldMapData.GetZone(zoneID);
        Coord pos = World.worldMap.worldMapData.PlaceZone(zb);

        if (pos != null)
        {
            World.tileMap.DeleteScreen(pos.x, pos.y);
            World.worldMap.worldMapData.NewPostGenLandmark(pos, zoneID);
        }
    }
}

public class RemoveLocation : QuestEvent
{
    readonly string zoneID;

    public RemoveLocation(string zone)
    {
        zoneID = zone;
    }

    public override void RunEvent()
    {
        Coord c = World.worldMap.worldMapData.GetLandmark(zoneID);

        if (c != null)
        {
            World.worldMap.worldMapData.RemoveLandmark(c);
            World.tileMap.DeleteScreen(c.x, c.y);
            World.worldMap.RemoveLandmark(c.x, c.y);
        }
    }
}
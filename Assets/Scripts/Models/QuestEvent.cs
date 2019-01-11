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
    readonly Coord worldPos;
    readonly int elevation;
    readonly Coord localPos;

    public SpawnNPCEvent(string nID, Coord wPos, Coord lPos, int ele, string gItem = "")
    {
        npcID = nID;
        worldPos = wPos;
        localPos = lPos;
        elevation = ele;
        giveItem = gItem;
    }

    public override void RunEvent()
    {
        NPC n = new NPC(EntityList.GetBlueprintByID(npcID), worldPos, localPos, elevation);

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
    readonly Coord worldPos;
    readonly int elevation;
    readonly int amount;

    public SpawnNPCGroupEvent(string group, Coord wPos, int ele, int amt)
    {
        groupID = group;
        worldPos = wPos;
        elevation = ele;
        amount = amt;
    }

    public override void RunEvent()
    {
        List<NPC> ns = SpawnController.SpawnFromGroupNameAt(groupID, amount, worldPos, elevation);

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
    readonly Coord worldPos;
    readonly int elevation;
    readonly Coord localPos;

    public SpawnObjectEvent(string oID, Coord wPos, Coord lPos, int ele, string gItem = "")
    {
        objectID = oID;
        worldPos = wPos;
        localPos = lPos;
        elevation = ele;
        giveItem = gItem;
    }

    public override void RunEvent()
    {
        MapObject m = World.objectManager.NewObjectAtOtherScreen(objectID, localPos, worldPos, elevation);

        if (giveItem != "")
        {
            m.inv.Add(ItemList.GetItemByID(giveItem));
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
    readonly Coord worldPos;
    readonly Coord localPos;
    readonly int elevation;

    public MoveNPCEvent(string nID, Coord wPos, Coord lPos, int ele)
    {
        npcID = nID;
        worldPos = wPos;
        localPos = lPos;
        elevation = ele;
    }

    public override void RunEvent()
    {
        NPC n = World.objectManager.npcClasses.Find(x => x.ID == npcID);

        if (n != null)
        {
            if (worldPos != null)
            {
                n.worldPosition = worldPos;
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
                worldPosition = worldPos
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
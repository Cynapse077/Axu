using System.Collections.Generic;

public class QuestEvent
{
    public Quest myQuest;
    public QuestEvent() { }

    public virtual void RunEvent() { }

    public virtual IEnumerable<string> LoadErrors()
    {
        yield break;
    }

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

    public override IEnumerable<string> LoadErrors()
    {
        if (luaCall == null)
        {
            yield return "LuaQuestEvent - luaCall is null.";
        }
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

    public override IEnumerable<string> LoadErrors()
    {
        if (worldPos == null)
        {
            yield return "WorldPosChangeEvent - worldPos is null.";
        }

        if (elevation > 0)
        {
            yield return "WorldPosChangeEvent - elevation is > 0.";
        }
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
        ObjectManager.playerEntity.ForcePosition(new Coord(localPos));
        ObjectManager.playerEntity.BeamDown();
        World.tileMap.SoftRebuild();
    }

    public override IEnumerable<string> LoadErrors()
    {
        if (localPos == null)
        {
            yield return "WorldPosChangeEvent - localPos is null.";
        }
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

    public override IEnumerable<string> LoadErrors()
    {
        if (elevation > 0)
        {
            yield return "ElevationChangeEvent - elevation is > 0.";
        }
    }
}

public class SpawnNPCEvent : QuestEvent
{
    readonly string npcID;
    readonly List<string> giveItems;
    readonly string giveItem;
    readonly string zone;
    readonly int elevation;
    readonly Coord localPos;

    public SpawnNPCEvent(string nID, string z, Coord lPos, int ele, string gItem, List<string> gItems)
    {
        npcID = nID;
        zone = z;
        localPos = lPos;
        elevation = ele;
        giveItem = gItem;
        giveItems = gItems;
    }

    public override void RunEvent()
    {
        if (GameData.TryGet<NPC_Blueprint>(npcID, out var bp))
        {
            Coord wPos = World.worldMap.worldMapData.GetLandmark(zone);        
            NPC n = new NPC(bp, wPos, localPos, elevation);

            if (!giveItem.NullOrEmpty())
            {
                n.inventory.Add(ItemList.GetItemByID(giveItem));
            }

            if (giveItems != null)
            {
                foreach (string s in giveItems)
                {
                    n.inventory.Add(ItemList.GetItemByID(s));
                }
            }

            myQuest.SpawnNPC(n);
        }
    }

    public override IEnumerable<string> LoadErrors()
    {
        if (npcID.NullOrEmpty())
        {
            yield return "SpawnNPCEvent - npcID is null.";
        }

        if (zone.NullOrEmpty())
        {
            yield return "SpawnNPCEvent - zone is null.";
        }

        if (localPos == null)
        {
            yield return "SpawnNPCEvent - localPos is null.";
        }
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

    public override IEnumerable<string> LoadErrors()
    {
        if (groupID.NullOrEmpty())
        {
            yield return "SpawnNPCGroupEvent - groupID is null.";
        }

        if (zone == null)
        {
            yield return "SpawnNPCGroupEvent - zone is null.";
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
            MapObject m = World.objectManager.NewObjectAtSpecificScreen(objectID, localPos, c, elevation);

            if (!giveItem.NullOrEmpty())
            {
                m.inv.Add(ItemList.GetItemByID(giveItem));
            }
        }        
    }

    public override IEnumerable<string> LoadErrors()
    {
        if (objectID.NullOrEmpty())
        {
            yield return "SpawnObjectEvent - objectID is null.";
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

    public override IEnumerable<string> LoadErrors()
    {
        if (command.NullOrEmpty())
        {
            yield return "ConsoleCommandEvent - command is null.";
        }
    }
}

public class GiveProgressFlagEvent : QuestEvent
{
    readonly string flag;

    public GiveProgressFlagEvent(string flg)
    {
        flag = flg;
    }

    public override void RunEvent()
    {
        if (ObjectManager.playerJournal != null)
        {
            ObjectManager.playerJournal.AddFlag(flag);
        }
    }

    public override IEnumerable<string> LoadErrors()
    {
        if (flag.NullOrEmpty())
        {
            yield return "GiveProgressFlagEvent - flag is null.";
        }
    }
}

public class LogMessageEvent : QuestEvent
{
    readonly string message;

    public LogMessageEvent(string msg)
    {
        message = msg;
    }

    public override void RunEvent()
    {
        if (!message.NullOrEmpty())
        {
            CombatLog.NewMessage(message);
        }        
    }

    public override IEnumerable<string> LoadErrors()
    {
        if (message.NullOrEmpty())
        {
            yield return "LogMessageEvent - message is null.";
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

    public override IEnumerable<string> LoadErrors()
    {
        if (npcID.NullOrEmpty())
        {
            yield return "GiveNPCQuestEvent - npcID is null.";
        }

        if (questID.NullOrEmpty())
        {
            yield return "GiveNPCQuestEvent - questID is null.";
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
            NPC_Blueprint bp = GameData.Get<NPC_Blueprint>(npcID);

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

    public override IEnumerable<string> LoadErrors()
    {
        if (npcID.NullOrEmpty())
        {
            yield return "MoveNPCEvent - npcID is null.";
        }

        if (zone.NullOrEmpty())
        {
            yield return "MoveNPCEvent - zone is null.";
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

    public override IEnumerable<string> LoadErrors()
    {
        if (npcID.NullOrEmpty())
        {
            yield return "ReplaceItemOnNPCEvent - npcID is null.";
        }

        if (itemID.NullOrEmpty())
        {
            yield return "ReplaceItemOnNPCEvent - itemID is null.";
        }

        if (replacementID.NullOrEmpty())
        {
            yield return "ReplaceItemOnNPCEvent - replacementID is null.";
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
        World.objectManager.NewObjectAtSpecificScreen("Stair_Lock", localPos, worldPos, elevation);
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
        List<MapObject> toDelete = mos.FindAll(x => x.blueprint.objectType == "Stair_Lock");

        for (int i = toDelete.Count - 1; i >= 0; i--)
        {
            World.objectManager.mapObjects.Remove(toDelete[i]);
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
    public int npcUIDOverride = -1;

    public BecomeFollowerEvent(string npc)
    {
        npcID = npc;
    }

    public BecomeFollowerEvent(NPC npc)
    {
        npcID = string.Empty;
        npcUIDOverride = npc.UID;
    }

    public override void RunEvent()
    {
        NPC n = npcUIDOverride < 0 ? World.objectManager.GetNPCByUID(npcUIDOverride) : World.objectManager.npcClasses.Find(x => x.ID == npcID);

        if (n != null)
        {
            n.MakeFollower();
            World.tileMap.HardRebuild();
        }
    }

    public override IEnumerable<string> LoadErrors()
    {
        if (npcID.NullOrEmpty() && npcUIDOverride < 0)
        {
            yield return "BecomeFollowerEvent - npcID is null and npcUIDOverride is < 0.";
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

    public override IEnumerable<string> LoadErrors()
    {
        if (goal == null)
        {
            yield return "CompleteStepEvent - goal is null.";
        }
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

    public override IEnumerable<string> LoadErrors()
    {
        if (npcID.NullOrEmpty())
        {
            yield return "RemoveNPCEvent - npcID is null.";
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

    public override IEnumerable<string> LoadErrors()
    {
        if (worldPos == null)
        {
            yield return "RemoveNPCsAtEvent - worldPos is null.";
        }

        if (elevation > 0)
        {
            yield return "RemoveNPCsAtEvent - elevation > 0";
        }
    }
}

public class SetNPCDialogueTreeEvent : QuestEvent
{
    readonly string npcID;
    readonly string dialogueID;

    public SetNPCDialogueTreeEvent(string nID, string dID)
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

    public override IEnumerable<string> LoadErrors()
    {
        if (npcID.NullOrEmpty())
        {
            yield return "SetNPCDialogueTreeEvent - npcID is null.";
        }

        if (dialogueID.NullOrEmpty())
        {
            yield return "SetNPCDialogueTreeEvent - dialogueID is null.";
        }
    }
}

public class OpenDialogueEvent : QuestEvent
{
    readonly string speaker;
    readonly string dialogue;

    public OpenDialogueEvent(string spkr, string dia)
    {
        speaker = spkr;
        dialogue = dia;
    }

    public override void RunEvent()
    {
        string name = "";
        if (speaker == "Stored NPC")
        {
            if (myQuest.StoredNPC != null)
            {
                name = myQuest.StoredNPC.name;
            }
        }
        else
        {
            name = speaker;
        }

        Alert.CustomAlert_WithTitle(name, dialogue);
    }

    public override IEnumerable<string> LoadErrors()
    {
        if (dialogue.NullOrEmpty())
        {
            yield return "OpenDialogueEvent - dialogue is null.";
        }
    }
}

public class CreateLocationEvent : QuestEvent
{
    readonly string zoneID;

    public CreateLocationEvent(string zone)
    {
        zoneID = zone;
    }

    public override void RunEvent()
    {
        Zone_Blueprint zb = World.worldMap.worldMapData.GetZone(zoneID);
        Coord pos = World.worldMap.worldMapData.PlaceZone(zb);

        if (pos != null)
        {
            World.tileMap.DeleteScreen(pos.x, pos.y);
            World.worldMap.worldMapData.NewPostGenLandmark(pos, zoneID);
        }
    }

    public override IEnumerable<string> LoadErrors()
    {
        if (zoneID.NullOrEmpty())
        {
            yield return "CreateLocationEvent - zoneID is null.";
        }
    }
}

public class RemoveLocationEvent : QuestEvent
{
    readonly string zoneID;

    public RemoveLocationEvent(string zone)
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

    public override IEnumerable<string> LoadErrors()
    {
        if (zoneID.NullOrEmpty())
        {
            yield return "CreateLocationEvent - zoneID is null.";
        }
    }
}

public class RemoveSpecificLocationEvent : QuestEvent
{
    readonly Coord zone;

    public RemoveSpecificLocationEvent(Coord zone)
    {
        this.zone = zone;
    }

    public override void RunEvent()
    {
        Coord c = zone;

        if (c != null)
        {
            World.worldMap.worldMapData.RemoveLandmark(c);
            World.tileMap.DeleteScreen(c.x, c.y);
            World.worldMap.RemoveLandmark(c.x, c.y);
        }
    }

    public override IEnumerable<string> LoadErrors()
    {
        if (zone == null)
        {
            yield return "RemoveLocation_SpecificEvent - zone is null.";
        }
    }
}

public class SetItemModifierEvent : QuestEvent
{
    readonly string[] availableEntries = new string[] { "Weapon", "Firearm" }; //For logging purposes
    readonly string itemSlot;
    readonly ItemModifier modifier;

    public SetItemModifierEvent(string itSlot, string modID)
    {
        itemSlot = itSlot;
        modifier = new ItemModifier(GameData.Get<ItemModifier>(modID));
    }

    public override void RunEvent()
    {
        Item item = null;
        Entity entity = ObjectManager.playerEntity;

        switch (itemSlot)
        {
            case "Weapon": //Selects the first available weapon.
                if (!entity.body.MainHand.EquippedItem.HasProp(ItemProperty.Cannot_Remove))
                {
                    item = entity.body.MainHand.EquippedItem;
                }
                else
                {
                    foreach (BodyPart.Hand hand in entity.body.Hands)
                    {
                        if (hand.arm.Attached && !hand.EquippedItem.HasProp(ItemProperty.Cannot_Remove))
                        {
                            item = hand.EquippedItem;
                            break;
                        }
                    }
                }
                
                break;

            case "Firearm":
                item = entity.inventory.firearm;
                break;
        }

        if (item != null && !item.HasProp(ItemProperty.Cannot_Remove) && modifier != null)
        {
            item.AddModifier(modifier);
        }
    }

    public override IEnumerable<string> LoadErrors()
    {
        if (modifier == null)
        {
            yield return "SetItemModifierEvent - modifier is null.";
        }

        if (itemSlot.NullOrEmpty())
        {
            yield return "SetItemModifierEvent - itemEntry is null.";
        }
        else
        {
            bool validEntry = false;

            for (int i = 0; i < availableEntries.Length; i++)
            {
                if (itemSlot == availableEntries[i])
                {
                    validEntry = true;
                    break;
                }
            }

            if (!validEntry)
            {
                yield return string.Format("SetItemModifierEvent - \"{0}\" is not a valid entry.", itemSlot);
            }
        }
    }
}
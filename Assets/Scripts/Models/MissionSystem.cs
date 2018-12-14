using System;
using System.Collections.Generic;
using UnityEngine;

public class EventHandler
{
    public static EventHandler instance { get; protected set; }

    public event Action<NPC> NPCDied;
    public event Action<Coord, int> EnteredScreen;
    public event Action<NPC> TalkedToNPC;
    public event Action<MapObject> InteractedWithObject;

    public EventHandler()
    {
        instance = this;
    }

    public void OnNPCDeath(NPC n)
    {
        if (NPCDied != null)
        {
            NPCDied(n);
        }
    }

    public void OnEnterScreen(TileMap_Data map)
    {
        if (EnteredScreen != null)
        {
            EnteredScreen(map.mapInfo.position, map.elevation);
        }
    }

    public bool OnTalkTo(NPC n)
    {
        if (TalkedToNPC != null)
        {
            TalkedToNPC(n);
            return true;
        }

        return false;
    }

    public void OnInteract(MapObject m)
    {
        if (InteractedWithObject != null)
        {
            InteractedWithObject(m);
        }
    }
}

public class EventContainer
{
    public List<QuestEvent> onStart { get; protected set; }
    public List<QuestEvent> onComplete { get; protected set; }
    public List<QuestEvent> onFail { get; protected set; }

    public void AddEvent(QuestEvent.EventType eventType, QuestEvent questEvent)
    {
        switch (eventType)
        {
            case QuestEvent.EventType.OnStart:
                if (onStart == null)
                    onStart = new List<QuestEvent>();

                onStart.Add(questEvent);
                break;
            case QuestEvent.EventType.OnComplete:
                if (onComplete == null)
                    onComplete = new List<QuestEvent>();

                onComplete.Add(questEvent);
                break;
            case QuestEvent.EventType.OnFail:
                if (onFail == null)
                    onFail = new List<QuestEvent>();

                onFail.Add(questEvent);
                break;
        }
    }

    public void RunEvent(Quest q, QuestEvent.EventType eventType)
    {
        switch (eventType)
        {
            case QuestEvent.EventType.OnStart:
                if (onStart != null)
                {
                    for (int i = 0; i < onStart.Count; i++)
                    {
                        onStart[i].myQuest = q;
                        onStart[i].RunEvent();
                    }
                }

                break;

            case QuestEvent.EventType.OnComplete:
                if (onComplete != null)
                {
                    for (int i = 0; i < onComplete.Count; i++)
                    {
                        onComplete[i].myQuest = q;
                        onComplete[i].RunEvent();
                    }
                }

                break;

            case QuestEvent.EventType.OnFail:
                if (onFail != null)
                {
                    for (int i = 0; i < onFail.Count; i++)
                    {
                        onFail[i].myQuest = q;
                        onFail[i].RunEvent();
                    }
                }

                break;
        }
    }
}

[Serializable]
public class Goal : EventContainer
{
    public bool isComplete = false;
    public int amount = 0;
    public string description;
    protected Quest myQuest;

    public Goal() { }

    public void Setup(Quest q)
    {
        myQuest = q;
    }

    public Goal Clone()
    {
        return (Goal)MemberwiseClone();
    }

    public virtual void Init(bool skipEvent)
    {
        if (!skipEvent)
        {
            RunEvent(myQuest, QuestEvent.EventType.OnStart);
        }
    }

    public virtual bool CanComplete()
    {
        return true;
    }

    public virtual void Complete()
    {
        isComplete = true;

        RunEvent(myQuest, QuestEvent.EventType.OnComplete);

        if (myQuest != null)
        {
            myQuest.CompleteGoal(this);
        }
    }

    public virtual void Fail()
    {
        isComplete = false;

        RunEvent(myQuest, QuestEvent.EventType.OnFail);

        if (myQuest != null)
        {
            myQuest.Fail();
        }
    }

    public virtual Coord Destination()
    {
        return null;
    }

    public override string ToString()
    {
        return "";
    }

    protected NPC CheckNPCValidity(string npcID)
    {
        if (World.objectManager.npcClasses.Find(x => x.ID == npcID) == null)
        {
            NPC_Blueprint bp = EntityList.GetBlueprintByID(npcID);

            if (bp == null)
            {
                Debug.LogError("NPC with ID " + npcID + " does not exist. Fix NewQuests.json");
                return null;
            }

            if (bp.zone != "")
            {
                NPC npc = new NPC(bp, new Coord(0, 0), new Coord(0, 0), 0);

                if (bp.zone.Contains("Random_"))
                {
                    npc.elevation = 0;
                    npc.localPosition = new Coord(UnityEngine.Random.Range(0, Manager.localMapSize.x), UnityEngine.Random.Range(0, Manager.localMapSize.y));

                    string biome = bp.zone.Replace("Random_", "");
                    WorldMap.Biome b = biome.ToEnum<WorldMap.Biome>();
                    npc.worldPosition = World.worldMap.worldMapData.GetRandomFromBiome(b);
                }
                else
                {
                    npc.localPosition = bp.localPosition;
                    npc.elevation = bp.elevation;
                    npc.worldPosition = (bp.zone == "Unassigned") ? new Coord(-1, -1) : World.tileMap.worldMap.GetLandmark(bp.zone);
                }

                World.objectManager.CreateNPC(npc);
                return npc;
            }
            else {
                Debug.LogError("Blueprint zone is null.");
                return null;
            }
        }
        else
        {
            return World.objectManager.npcClasses.Find(x => x.ID == npcID);
        }
    }
}

public class SpecificKillGoal : Goal
{
    public SpecificKillGoal(Quest q)
    {
        myQuest = q;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.NPCDied += NPCKilled;

        for (int i = 0; i < myQuest.spawnedNPCs.Count; i++)
        {
            Coord wp = World.objectManager.GetNPCByUID(myQuest.spawnedNPCs[i]).worldPosition;
            World.objectManager.NewMapIcon(1, wp);
        }
    }

    void NPCKilled(NPC n)
    {
        for (int i = 0; i < myQuest.spawnedNPCs.Count; i++)
        {
            if (n.UID == myQuest.spawnedNPCs[i])
            {
                World.objectManager.RemoveMapIconAt(n.worldPosition);
                myQuest.spawnedNPCs.Remove(n.UID);
                break;
            }
        }

        if (CanComplete())
        {
            Complete();
        }
    }

    public override bool CanComplete()
    {
        return myQuest.spawnedNPCs.Count <= 0;
    }

    public override void Complete()
    {
        EventHandler.instance.NPCDied -= NPCKilled;
        base.Complete();
    }

    public override void Fail()
    {
        EventHandler.instance.NPCDied -= NPCKilled;
        base.Fail();
    }

    public override Coord Destination()
    {
        for (int i = 0; i < myQuest.spawnedNPCs.Count; i++)
        {
            NPC n = World.objectManager.npcClasses.Find(x => x.UID == myQuest.spawnedNPCs[i]);

            if (n != null)
            {
                return n.worldPosition;
            }
        }

        UnityEngine.Debug.LogError("Quest step is either complete, or NPC UID is zero.");
        return null;
    }

    public override string ToString()
    {
        return string.Format("Kill marked targets. ({0} remaining)", myQuest.spawnedNPCs.Count.ToString());
    }
}

public class NPCKillGoal : Goal
{
    readonly string npcID;
    readonly int max;

    public NPCKillGoal(Quest q, string nID, int amt)
    {
        myQuest = q;
        npcID = nID;
        amount = 0;
        max = amt;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        EventHandler.instance.NPCDied += NPCKilled;
        base.Init(skipEvent);
    }

    void NPCKilled(NPC n)
    {
        if (n.ID == npcID)
        {
            amount++;

            if (CanComplete())
            {
                Complete();
            }
        }
    }

    public override bool CanComplete()
    {
        return amount >= max;
    }

    public override void Complete()
    {
        EventHandler.instance.NPCDied -= NPCKilled;
        base.Complete();
    }

    public override void Fail()
    {
        EventHandler.instance.NPCDied -= NPCKilled;
        base.Fail();
    }

    public override string ToString()
    {
        string npcName = EntityList.GetBlueprintByID(npcID).name;
        return string.Format("Kill {0}. ({1} / {2})", npcName, amount.ToString(), max.ToString());
    }
}

public class FactionKillGoal : Goal
{
    readonly string faction;
    readonly int max;

    public FactionKillGoal(Quest q, string _faction, int amt)
    {
        myQuest = q;
        faction = _faction;
        amount = 0;
        max = amt;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.NPCDied += NPCKilled;
    }

    void NPCKilled(NPC n)
    {
        if (n.faction.ID == faction)
        {
            amount++;

            if (CanComplete())
            {
                Complete();
            }
        }
    }

    public override bool CanComplete()
    {
        return amount >= max;
    }

    public override void Complete()
    {
        EventHandler.instance.NPCDied -= NPCKilled;
        base.Complete();
    }

    public override void Fail()
    {
        EventHandler.instance.NPCDied -= NPCKilled;
        base.Fail();
    }

    public override string ToString()
    {
        return string.Format("Kill {0}x members of the {1} faction.", max.ToString(), FactionList.GetFactionByID(faction).Name);
    }
}

public class GoToGoal : Goal
{
    readonly string destination;
    readonly int elevation;
    readonly Coord coordDest;

    public GoToGoal(Quest q, string dest, int ele)
    {
        myQuest = q;
        destination = dest;
        elevation = ele;
        isComplete = false;

        coordDest = q.GetZone(destination);
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.EnteredScreen += EnteredArea;

        World.objectManager.NewMapIcon(0, coordDest);
    }

    void EnteredArea(Coord c, int ele)
    {
        if (c == coordDest && UnityEngine.Mathf.Abs(ele) == UnityEngine.Mathf.Abs(elevation))
        {
            Complete();
        }
    }

    public override bool CanComplete()
    {
        return (World.tileMap.CurrentMap.mapInfo.position == coordDest && UnityEngine.Mathf.Abs(World.tileMap.currentElevation) == UnityEngine.Mathf.Abs(elevation));
    }

    public override void Complete()
    {
        World.objectManager.RemoveMapIconAt(coordDest);
        EventHandler.instance.EnteredScreen -= EnteredArea;
        base.Complete();
    }

    public override void Fail()
    {
        EventHandler.instance.EnteredScreen -= EnteredArea;
        base.Fail();
    }

    public override Coord Destination()
    {
        return coordDest;
    }

    public override string ToString()
    {
        string dest = (elevation == 0) ? "Go to the marked destination." : "Go to the marked destination at elevation " + elevation + ".";
        return dest;
    }
}

public class InteractGoal : Goal
{
    readonly Coord worldPos;
    readonly int elevation;
    readonly string objectType;
    readonly int max;

    public InteractGoal(string objType, Coord wPos, int ele, int amt)
    {
        worldPos = wPos;
        elevation = ele;
        objectType = objType;
        max = amt;
        amount = 0;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.InteractedWithObject += InteractedWithObject;
        World.objectManager.NewMapIcon(0, worldPos);
    }

    void InteractedWithObject(MapObject m)
    {
        if (worldPos == m.worldPosition && elevation == m.elevation && m.objectType == objectType)
        {
            amount++;

            if (CanComplete())
            {
                Complete();
            }
        }
    }

    public override bool CanComplete()
    {
        return amount >= max;
    }

    public override Coord Destination()
    {
        return worldPos;
    }

    public override void Complete()
    {
        World.objectManager.RemoveMapIconAt(worldPos);
        EventHandler.instance.InteractedWithObject -= InteractedWithObject;
        base.Complete();
    }

    public override void Fail()
    {
        World.objectManager.RemoveMapIconAt(worldPos);
        EventHandler.instance.InteractedWithObject -= InteractedWithObject;
        base.Fail();
    }

    public override string ToString()
    {
        string objName = ItemList.GetMOB(objectType).Name;
        string amt = (max > 1) ? (max + "x" + objName) : "the " + objName;
        string ele = elevation == 0 ? "." : " on floor " + elevation + ".";
        return string.Format("Use {0}{1}", amt, ele);
    }
}

public class TalkToGoal : Goal
{
    readonly string npcTarget;

    public TalkToGoal(Quest q, string n)
    {
        myQuest = q;
        npcTarget = n;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.TalkedToNPC += TalkToNPC;
        CheckNPCValidity(npcTarget);
        World.objectManager.NewMapIcon(0, Destination());
    }

    void TalkToNPC(NPC n)
    {
        if (npcTarget == n.ID)
        {
            Complete();
        }
    }

    public override void Complete()
    {
        World.objectManager.RemoveMapIconAt(Destination());
        EventHandler.instance.TalkedToNPC -= TalkToNPC;
        base.Complete();
    }

    public override void Fail()
    {
        EventHandler.instance.TalkedToNPC -= TalkToNPC;
        base.Fail();
    }

    public override Coord Destination()
    {
        NPC n = CheckNPCValidity(npcTarget);

        if (n == null)
        {
            UnityEngine.Debug.LogError("TalkToGoal: NPC Target is null. Cannot get destination position.");
            return null;
        }

        return n.worldPosition;
    }

    public override string ToString()
    {
        return string.Format("Talk to {0}.", EntityList.GetBlueprintByID(npcTarget).name);
    }
}

public class FetchPropertyGoal : Goal
{
    readonly ItemProperty itemProperty;
    readonly string npcTarget;
    readonly int max;

    public FetchPropertyGoal(Quest q, string nid, string prop, int amt)
    {
        myQuest = q;
        npcTarget = nid;
        itemProperty = prop.ToEnum<ItemProperty>();
        max = amt;
        amount = 0;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.TalkedToNPC += TalkToNPC;
        CheckNPCValidity(npcTarget);
        World.objectManager.NewMapIcon(0, Destination());
    }

    void TalkToNPC(NPC n)
    {
        if (npcTarget == n.ID && CanComplete())
        {
            Complete();
        }
    }

    public override bool CanComplete()
    {
        return Current() >= max;
    }

    public int Current()
    {
        amount = 0;
        List<Item> relevantItems = ObjectManager.playerEntity.inventory.items.FindAll(x => x.HasProp(itemProperty));

        for (int i = 0; i < relevantItems.Count; i++)
        {
            amount += relevantItems[i].amount;
        }

        return amount;
    }

    public override void Complete()
    {
        int tmpMax = max;
        List<Item> relevantItems = ObjectManager.playerEntity.inventory.items.FindAll(x => x.HasProp(itemProperty));

        for (int i = 0; i < relevantItems.Count; i++)
        {
            if (tmpMax <= 0)
            {
                break;
            }

            ObjectManager.playerEntity.inventory.RemoveInstance(relevantItems[i]);
            tmpMax--;
        }

        World.objectManager.RemoveMapIconAt(Destination());
        EventHandler.instance.TalkedToNPC -= TalkToNPC;
        base.Complete();
    }

    public override void Fail()
    {
        World.objectManager.RemoveMapIconAt(Destination());
        EventHandler.instance.TalkedToNPC -= TalkToNPC;
        base.Fail();
    }

    public override Coord Destination()
    {
        NPC n = CheckNPCValidity(npcTarget);

        if (n == null)
        {
            UnityEngine.Debug.LogError("FetchPropertyGoal: NPC Target is null. Cannot get destination position.");
            return null;
        }

        return n.worldPosition;
    }

    public override string ToString()
    {
        string npcName = EntityList.GetBlueprintByID(npcTarget).name;

        return string.Format("Give {0} items of type \"{1}\" x{2}.", npcName, itemProperty.ToString(), max.ToString());
    }
}

public class FetchGoal : Goal
{
    readonly string itemID;
    readonly string npcTarget;
    readonly int max;

    public FetchGoal(Quest q, string nid, string id, int amt)
    {
        myQuest = q;
        npcTarget = nid;
        itemID = id;
        max = amt;
        amount = 0;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.TalkedToNPC += TalkToNPC;
        CheckNPCValidity(npcTarget);
        World.objectManager.NewMapIcon(0, Destination());
    }

    void TalkToNPC(NPC n)
    {
        if (npcTarget == n.ID && CanComplete())
        {
            Complete();
        }
    }

    public override bool CanComplete()
    {
        return Current() >= max;
    }

    public int Current()
    {
        amount = 0;
        List<Item> relevantItems = ObjectManager.playerEntity.inventory.items.FindAll(x => x.ID == itemID);

        for (int i = 0; i < relevantItems.Count; i++)
        {
            amount += relevantItems[i].amount;
        }

        return amount;
    }

    public override void Complete()
    {
        int tmpMax = max;
        List<Item> relevantItems = ObjectManager.playerEntity.inventory.items.FindAll(x => x.ID == itemID);

        for (int i = 0; i < relevantItems.Count; i++)
        {
            if (tmpMax <= 0)
            {
                break;
            }

            ObjectManager.playerEntity.inventory.RemoveInstance(relevantItems[i]);
            tmpMax--;
        }

        World.objectManager.RemoveMapIconAt(Destination());
        EventHandler.instance.TalkedToNPC -= TalkToNPC;
        base.Complete();
    }

    public override void Fail()
    {
        World.objectManager.RemoveMapIconAt(Destination());
        EventHandler.instance.TalkedToNPC -= TalkToNPC;
        base.Fail();
    }

    public override Coord Destination()
    {
        NPC n = CheckNPCValidity(npcTarget);

        if (n == null)
        {
            Debug.LogError("FetchGoal: NPC Target is null. Cannot get destination position.");
            return null;
        }

        return n.worldPosition;
    }

    public override string ToString()
    {
        string npcName = EntityList.GetBlueprintByID(npcTarget).name;
        string itemName = ItemList.GetItemByID(itemID).Name;

        return string.Format("Give {0} {1} x{2}.", npcName, itemName, max.ToString());
    }
}


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
        base.RunEvent();
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
        base.RunEvent();
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
        base.RunEvent();
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
        base.RunEvent();
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
        base.RunEvent();

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
        base.RunEvent();

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
        base.RunEvent();

        foreach (int uid in myQuest.spawnedNPCs)
        {
            NPC n = World.objectManager.npcClasses.Find(x => x.UID == uid);

            if (n != null)
                World.objectManager.npcClasses.Remove(n);
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
        base.RunEvent();

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
        base.RunEvent();

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
        base.RunEvent();

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
        base.RunEvent();

        NPC n = World.objectManager.npcClasses.Find(x => x.ID == npcID);

        if (n != null)
        {
            if (worldPos != null)
                n.worldPosition = worldPos;

            if (localPos != null)
                n.localPosition = localPos;

            n.elevation = elevation;

            World.tileMap.HardRebuild();
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
        base.RunEvent();

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
        base.RunEvent();

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
        base.RunEvent();

        List<MapObject> mos = World.objectManager.ObjectsAt(worldPos, elevation);
        List<MapObject> toDelete = mos.FindAll(x => x.objectType == "Stair_Lock");

        while (toDelete.Count > 0)
        {
            World.objectManager.mapObjects.Remove(toDelete[0]);
            toDelete.RemoveAt(0);
        }

        if (World.tileMap.WorldPosition == worldPos && World.tileMap.currentElevation == elevation)
            World.tileMap.HardRebuild();
    }
}
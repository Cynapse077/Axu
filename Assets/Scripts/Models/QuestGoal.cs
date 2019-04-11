using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Goal : EventContainer
{
    public bool isComplete = false;
    public int amount = 0;
    public string description;
    public string goalType { get; protected set; }

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

    public void RemoveQuest()
    {
        myQuest = null;
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
        return description;
    }

    protected NPC CheckNPCValidity(string npcID)
    {
        if (World.objectManager.npcClasses.Find(x => x.ID == npcID) == null)
        {
            NPC_Blueprint bp = EntityList.GetBlueprintByID(npcID);

            if (bp == null)
            {
                Debug.LogError("NPC with ID " + npcID + " does not exist.");
                return null;
            }

            if (bp.zone == "")
            {
                Debug.LogError("Blueprint zone for '" + bp.id + "' is empty.");
                return null;
            }

            NPC npc = new NPC(bp, new Coord(0, 0), new Coord(0, 0), 0)
            {
                localPosition = bp.localPosition,
                elevation = bp.elevation,
                worldPosition = (bp.zone == "Unassigned") ? new Coord(-1, -1) : World.tileMap.worldMap.GetLandmark(bp.zone)
            };

            World.objectManager.CreateNPC(npc);
            return npc;
        }
        else
        {
            return World.objectManager.npcClasses.Find(x => x.ID == npcID);
        }
    }
}

//For simple one-step quests that require a choice, so I don't have to track progress. Otherwise it would break the SQuest stuffs.
public class ChoiceGoal : Goal
{
    Goal[] goals;

    public ChoiceGoal(Quest q, Goal[] gs, string desc)
    {
        goalType = "ChoiceGoal";
        myQuest = q;
        description = desc;

        goals = new Goal[gs.Length];
        for (int i = 0; i < gs.Length; i++)
        {
            goals[i] = gs[i];
            goals[i].AddEvent(QuestEvent.EventType.OnComplete, new CompleteStepEvent(this));
        }
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);

        for (int i = 0; i < goals.Length; i++)
        {
            goals[i].Init(skipEvent);
        }
    }

    public override bool CanComplete()
    {
        for (int i = 0; i < goals.Length; i++)
        {
            if (goals[i].CanComplete())
            {
                return true;
            }
        }

        return false;
    }

    public override void Complete()
    {
        base.Complete();
    }

    public override void Fail()
    {
        for (int i = 0; i < goals.Length; i++)
        {
            goals[i].Fail();
        }

        base.Fail();
    }

    public override Coord Destination()
    {
        //TODO: Uh....? How do I make two destinations?
        return goals[0].Destination();
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        string s = goals[0].ToString();

        for (int i = 1; i < goals.Length; i++)
        {
            s += "\n    OR\n- " + goals[i].ToString();
        }

        return s;
    }
}

public class SpecificKillGoal : Goal
{
    public SpecificKillGoal(Quest q, string desc)
    {
        goalType = "SpecificKillGoal";
        myQuest = q;
        description = desc;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.NPCDied += NPCKilled;

        for (int i = 0; i < myQuest.spawnedNPCs.Count; i++)
        {
            NPC n = World.objectManager.GetNPCByUID(myQuest.spawnedNPCs[i]);

            if (n != null)
            {
                World.objectManager.NewMapIcon(1, n.worldPosition);
            }
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

        Debug.LogError("Quest step is either complete, or NPC UID is zero.");
        return null;
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        string s = "Kill targets: \n";

        for (int i = 0; i < myQuest.spawnedNPCs.Count; i++)
        {
            NPC n = World.objectManager.GetNPCByUID(myQuest.spawnedNPCs[i]);
            s += n.name;

            if (i < myQuest.spawnedNPCs.Count)
            {
                s += "\n";
            }
        }

        return s;
    }
}

public class NPCKillGoal : Goal
{
    readonly string npcID;
    readonly int max;

    public NPCKillGoal(Quest q, string nID, int amt, string desc)
    {
        goalType = "NPCKillGoal";
        myQuest = q;
        npcID = nID;
        amount = 0;
        max = amt;
        description = desc;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        EventHandler.instance.NPCDied += NPCKilled;
        base.Init(skipEvent);

        if (ObjectManager.playerJournal.staticNPCKills.Contains(npcID))
        {
            Complete();
        }
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
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        string npcName = EntityList.GetBlueprintByID(npcID).name;
        return string.Format("Kill {0}. ({1} / {2})", npcName, amount.ToString(), max.ToString());
    }
}

public class FactionKillGoal : Goal
{
    readonly string faction;
    readonly int max;

    public FactionKillGoal(Quest q, string _faction, int amt, string desc)
    {
        goalType = "FactionKillGoal";
        myQuest = q;
        faction = _faction;
        amount = 0;
        max = amt;
        description = desc;
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
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        return string.Format("Kill {0}x members of the {1} faction. ({2}/{1})", max.ToString(), FactionList.GetFactionByID(faction).Name, amount.ToString());
    }
}

public class GoToGoal : Goal
{
    readonly string destination;
    readonly int elevation;
    Coord coordDest;

    public GoToGoal(Quest q, string dest, int ele, string desc)
    {
        goalType = "GoToGoal";
        myQuest = q;
        destination = dest;
        elevation = ele;
        description = desc;
        isComplete = false;

        coordDest = q.GetZone(destination);
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.EnteredScreen += EnteredArea;

        if (coordDest == null)
        {
            coordDest = myQuest.GetZone(destination);
        }

        World.objectManager.NewMapIcon(0, coordDest);
    }

    void EnteredArea(Coord c, int ele)
    {
        if (c == coordDest && Mathf.Abs(ele) == Mathf.Abs(elevation))
        {
            Complete();
        }
    }

    public override bool CanComplete()
    {
        return (World.tileMap.CurrentMap.mapInfo.position == coordDest && Mathf.Abs(World.tileMap.currentElevation) == Mathf.Abs(elevation));
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
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        string s = string.Format("Travel to the {0}", World.tileMap.worldMap.GetZoneNameAt(coordDest.x, coordDest.y, 0));

        if (elevation != 0)
        {
            s += " Floor " + (-elevation).ToString() + ".";
        }

        return s;
    }
}

public class InteractGoal : Goal
{
    readonly string zone;
    readonly int elevation;
    readonly string objectType;
    readonly int max;

    public InteractGoal(Quest q, string objType, string wPos, int ele, int amt, string desc)
    {
        goalType = "InteractGoal";
        myQuest = q;
        zone = wPos;
        elevation = ele;
        objectType = objType;
        max = amt;
        description = desc;
        amount = 0;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.InteractedWithObject += InteractedWithObject;
        World.objectManager.NewMapIcon(0, Destination());
    }

    void InteractedWithObject(MapObject m)
    {
        if (Destination() == m.worldPosition && elevation == m.elevation && m.objectType == objectType)
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
        return myQuest.GetZone(zone);
    }

    public override void Complete()
    {
        World.objectManager.RemoveMapIconAt(Destination());
        EventHandler.instance.InteractedWithObject -= InteractedWithObject;
        base.Complete();
    }

    public override void Fail()
    {
        World.objectManager.RemoveMapIconAt(Destination());
        EventHandler.instance.InteractedWithObject -= InteractedWithObject;
        base.Fail();
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        string objName = ItemList.GetMOB(objectType).Name;
        string amt = (max > 1) ? (max + "x" + objName) : "the " + objName;
        string ele = elevation == 0 ? "." : " on floor " + elevation + ".";
        return string.Format("Use {0}{1}", amt, ele);
    }
}

public class TalkToGoal : Goal
{
    readonly string npcTarget;

    public TalkToGoal(Quest q, string n, string desc)
    {
        goalType = "TalkToGoal";
        myQuest = q;
        npcTarget = n;
        description = desc;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.TalkedToNPC += TalkToNPC;
        EventHandler.instance.NPCDied += NPCDied;
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

    void NPCDied(NPC n)
    {
        if (n.ID == npcTarget)
        {
            Fail();
        }
    }

    public override void Complete()
    {
        World.objectManager.RemoveMapIconAt(Destination());
        EventHandler.instance.TalkedToNPC -= TalkToNPC;
        EventHandler.instance.NPCDied -= NPCDied;
        base.Complete();
    }

    public override void Fail()
    {
        EventHandler.instance.TalkedToNPC -= TalkToNPC;
        EventHandler.instance.NPCDied -= NPCDied;
        base.Fail();
    }

    public override Coord Destination()
    {
        NPC n = CheckNPCValidity(npcTarget);

        if (n == null)
        {
            Debug.LogError("TalkToGoal: NPC Target is null. Cannot get destination position.");
            return null;
        }

        return n.worldPosition;
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        return string.Format("Talk to {0}.", EntityList.GetBlueprintByID(npcTarget).name);
    }
}

public class FetchPropertyGoal : Goal
{
    public readonly ItemProperty itemProperty;
    public readonly string npcTarget;
    readonly int max;

    public FetchPropertyGoal(Quest q, string nid, string prop, int amt, string desc)
    {
        goalType = "FetchPropertyGoal";
        myQuest = q;
        npcTarget = nid;
        itemProperty = prop.ToEnum<ItemProperty>();
        max = amt;
        amount = 0;
        description = desc;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.NPCDied += NPCDied;
        CheckNPCValidity(npcTarget);
        World.objectManager.NewMapIcon(0, Destination());
    }

    void NPCDied(NPC n)
    {
        if (n.ID == npcTarget)
        {
            Fail();
        }
    }

    void TalkToNPC(NPC n)
    {
        if (npcTarget == n.ID && CanComplete())
        {
            Complete();
        }
    }

    public void AddAmount(int amt)
    {
        amount += amt;

        if (amount >= max)
        {
            World.userInterface.CloseWindows();
            Complete();
        }
    }

    public override void Complete()
    {
        EventHandler.instance.NPCDied -= NPCDied;
        World.objectManager.RemoveMapIconAt(Destination());
        base.Complete();
    }

    public override void Fail()
    {
        EventHandler.instance.NPCDied -= NPCDied;
        World.objectManager.RemoveMapIconAt(Destination());
        base.Fail();
    }

    public override Coord Destination()
    {
        NPC n = CheckNPCValidity(npcTarget);

        if (n == null)
        {
            Debug.LogError("FetchPropertyGoal: NPC Target is null. Cannot get destination position.");
            return null;
        }

        return n.worldPosition;
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        string npcName = EntityList.GetBlueprintByID(npcTarget).name;

        return string.Format("Give {0} items of type \"{1}\" x{2}.", npcName, itemProperty.ToString(), (max - amount).ToString());
    }
}

public class Fetch_Homonculus : Goal
{
    public readonly ItemProperty itemProperty;
    public readonly string npcTarget;
    public List<Item> items;
    readonly int max;

    public Fetch_Homonculus(Quest q, string nid, string prop, int amt, string desc)
    {
        goalType = "Fetch_Homonculus";
        myQuest = q;
        npcTarget = nid;
        itemProperty = prop.ToEnum<ItemProperty>();
        items = new List<Item>();
        max = amt;
        amount = 0;
        description = desc;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.NPCDied += NPCDied;
        CheckNPCValidity(npcTarget);
        World.objectManager.NewMapIcon(0, Destination());
    }

    void NPCDied(NPC n)
    {
        if (n.ID == npcTarget)
        {
            Fail();
        }
    }

    void TalkToNPC(NPC n)
    {
        if (npcTarget == n.ID && CanComplete())
        {
            Complete();
        }
    }

    public void AddItem(Item i)
    {
        items.Add(i);

        if (items.Count >= max)
        {
            World.userInterface.CloseWindows();
            Complete();
        }
    }

    public override void Complete()
    {
        EventHandler.instance.NPCDied -= NPCDied;
        World.objectManager.RemoveMapIconAt(Destination());
        List<Coord> possibleCoords = ObjectManager.playerEntity.GetEmptyCoords();
        Coord lp = (possibleCoords.Count > 0) ? possibleCoords.GetRandom() : World.tileMap.CurrentMap.GetRandomFloorTile();

        NPC n = new NPC("homonculus", World.tileMap.WorldPosition, lp, World.tileMap.currentElevation);

        for (int i = 0; i < items.Count; i++)
        {
            bool canAdd = true;
            BodyPart b = new BodyPart("", true);
            CEquipped ce = items[i].GetCComponent<CEquipped>();
            b.equippedItem = (ce == null) ? ItemList.GetNone() : ItemList.GetItemByID(ce.itemID);

            //Add body parts from items here.
            switch (items[i].GetSlot())
            {
                case ItemProperty.Slot_Arm:
                    b.slot = ItemProperty.Slot_Arm;
                    string baseItem = (ce == null) ? "fists" : ce.baseItemID;
                    b.hand = new BodyPart.Hand(b, ItemList.GetItemByID(baseItem), baseItem);
                    break;

                case ItemProperty.Slot_Head:
                    b.slot = ItemProperty.Slot_Head;
                    break;

                case ItemProperty.Slot_Leg:
                    b.slot = ItemProperty.Slot_Leg;
                    break;

                case ItemProperty.Slot_Tail:
                    b.slot = ItemProperty.Slot_Tail;
                    break;

                case ItemProperty.Slot_Wing:
                    b.slot = ItemProperty.Slot_Wing;
                    break;

                default:
                    canAdd = false;
                    break;
            }

            if (canAdd)
            {   
                if (!items[i].HasCComponent<CRot>())
                {
                    FlagsHelper.Set(ref b.flags, BodyPart.BPTags.Synthetic);
                }

                for (int j = 0; j < items[i].statMods.Count; j++)
                {
                    b.AddAttribute(items[i].statMods[j].Stat, items[i].statMods[j].Amount);

                    if (n.Attributes.ContainsKey(items[i].statMods[j].Stat))
                    {
                        n.Attributes[items[i].statMods[j].Stat] += items[i].statMods[j].Amount;
                    }
                }

                n.bodyParts.Add(b);
            }
        }

        World.objectManager.SpawnNPC(n);

        base.Complete();
    }

    public override void Fail()
    {
        EventHandler.instance.NPCDied -= NPCDied;
        World.objectManager.RemoveMapIconAt(Destination());
        base.Fail();
    }

    public override Coord Destination()
    {
        NPC n = CheckNPCValidity(npcTarget);

        if (n == null)
        {
            Debug.LogError("Fetch_KeepInMemGoal: NPC Target is null. Cannot get destination position.");
            return null;
        }

        return n.worldPosition;
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        string npcName = EntityList.GetBlueprintByID(npcTarget).name;

        return string.Format("Give {0} items of type \"{1}\" x{2}.", npcName, itemProperty.ToString(), (max - amount).ToString());
    }
}

public class FetchGoal : Goal
{
    public readonly string itemID;
    public readonly string npcTarget;
    readonly int max;

    public FetchGoal(Quest q, string nid, string id, int amt, string desc)
    {
        goalType = "FetchGoal";
        myQuest = q;
        npcTarget = nid;
        itemID = id;
        max = amt;
        amount = 0;
        description = desc;
        isComplete = false;
    }

    public override void Init(bool skipEvent)
    {
        base.Init(skipEvent);
        EventHandler.instance.NPCDied += NPCDied;
        CheckNPCValidity(npcTarget);
        World.objectManager.NewMapIcon(0, Destination());
    }

    void NPCDied(NPC n)
    {
        if (n.ID == npcTarget)
        {
            Fail();
        }
    }

    public void AddAmount(int amt)
    {
        amount += amt;

        if (amount >= max)
        {
            World.userInterface.CloseWindows();
            Complete();
        }
    }

    public override void Complete()
    {
        EventHandler.instance.NPCDied -= NPCDied;
        World.objectManager.RemoveMapIconAt(Destination());
        base.Complete();
    }

    public override void Fail()
    {
        EventHandler.instance.NPCDied -= NPCDied;
        World.objectManager.RemoveMapIconAt(Destination());
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
        if (!string.IsNullOrEmpty(description))
        {
            return description;
        }

        string npcName = EntityList.GetBlueprintByID(npcTarget).name;
        string itemName = ItemList.GetItemByID(itemID).Name;

        return string.Format("Give {0} {1} x{2}.", npcName, itemName, max.ToString());
    }
}
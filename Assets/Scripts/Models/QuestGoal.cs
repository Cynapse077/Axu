using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
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
        return "";
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

    public ChoiceGoal(Quest q, Goal[] gs)
    {
        myQuest = q;

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

        Debug.LogError("Quest step is either complete, or NPC UID is zero.");
        return null;
    }

    public override string ToString()
    {
        string s = "Kill targets: \n";

        for (int i = 0; i < myQuest.spawnedNPCs.Count; i++)
        {
            NPC n = World.objectManager.GetNPCByUID(myQuest.spawnedNPCs[i]);
            s += "  - " + n.name + " @ " + World.tileMap.worldMap.GetZoneNameAt(n.worldPosition.x, n.worldPosition.y, 0);

            if (n.elevation != 0)
            {
                s += " Floor " + (-n.elevation).ToString() + ".";
            }
        }

        return s;
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
        return string.Format("Kill {0}x members of the {1} faction. ({2}/{1})", max.ToString(), FactionList.GetFactionByID(faction).Name, amount.ToString());
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
    readonly Coord worldPos;
    readonly int elevation;
    readonly string objectType;
    readonly int max;

    public InteractGoal(Quest q, string objType, Coord wPos, int ele, int amt)
    {
        myQuest = q;
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
            Debug.LogError("TalkToGoal: NPC Target is null. Cannot get destination position.");
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
            Debug.LogError("FetchPropertyGoal: NPC Target is null. Cannot get destination position.");
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
using System;
using System.Collections.Generic;
using System.IO;
using LitJson;
using UnityEngine;

namespace MissionSystem
{
    public class EventHandler
    {
        public static EventHandler instance { get; protected set; }

        public event Action<NPC> NPCDied;
        public event Action<Coord, int> EnteredScreen;
        public event Action<NPC> TalkedToNPC;

        public EventHandler()
        {
            instance = this;
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

        public void RunEvent(QuestEvent.EventType eventType)
        {
            switch (eventType)
            {
                case QuestEvent.EventType.OnStart:
                    if (onStart != null)
                    {
                        for (int i = 0; i < onStart.Count; i++)
                        {
                            onStart[i].RunEvent();
                        }
                    }
                    break;

                case QuestEvent.EventType.OnComplete:
                    if (onComplete != null)
                    {
                        for (int i = 0; i < onComplete.Count; i++)
                        {
                            onComplete[i].RunEvent();
                        }
                    }
                    break;

                case QuestEvent.EventType.OnFail:
                    if (onFail != null)
                    {
                        for (int i = 0; i < onFail.Count; i++)
                        {
                            onFail[i].RunEvent();
                        }
                    }
                    break;
            }
        }
    }

    public static class QuestList
    {
        static List<Quest> quests;

        public static void InitializeFromJson()
        {
            quests = new List<Quest>();

            string jsonString = File.ReadAllText(Application.streamingAssetsPath + "/Data/NewQuests.json");

            JsonData data = JsonMapper.ToObject(jsonString);

            for (int i = 0; i < data["Quests"].Count; i++)
            {
                quests.Add(new Quest(data["Quests"][i]));
            }
        }

        public static Quest GetQuest(string id)
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
                    Coord lp = new Coord((int)e["Local Position"][0], (int)e["Local Position"][1]);

                    return new LocalPosChangeEvent(lp);

                case "Set World Position":
                    Coord wp = GetZone(e["Coordinate"].ToString());
                    int ele = (int)e["Elevation"];

                    return new WorldPosChangeEvent(wp, ele);

                case "Set Elevation":
                    int newEle = (int)e["Elevation"];

                    return new ElevationChangeEvent(newEle);

                default:
                    UnityEngine.Debug.LogError("Get Quest Event - No event with ID " + key + ".");
                    return null;
            }
        }
    }

    [Serializable]
    public class SQuest
    {
        public string ID;
        public bool[] comp;
        public int[] sp;

        public SQuest(Quest q)
        {
            ID = q.ID;

            comp = new bool[q.goals.Length];

            for (int i = 0; i < q.goals.Length; i++)
            {
                comp[i] = q.goals[i].isComplete;
            }

            sp = new int[q.spawnedNPCs.Count];

            for (int i = 0; i < q.spawnedNPCs.Count; i++)
            {
                sp[i] = q.spawnedNPCs[i];
            }
        }
    }

    public class Quest : EventContainer
    {
        public string ID { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public Goal[] goals { get; private set; }
        public QuestReward rewards { get; private set; }
        public List<int> spawnedNPCs { get; protected set; }
        public string chainedQuest;
        public string startDialogue;
        public string endDialogue;

        public bool isComplete
        {
            get
            {
                if (goals == null)
                    return false;

                for (int i = 0; i < goals.Length; i++)
                {
                    if (!goals[i].isComplete)
                        return false;
                }

                return true;
            }
        }

        public Goal ActiveGoal
        {
            get
            {
                for (int i = 0; i < goals.Length; i++)
                {
                    if (!goals[i].isComplete)
                        return goals[i];
                }

                return null;
            }
        }

        public Coord GetZone(string zone)
        {
            if (zone == "Destination")
            {
                return ActiveGoal.Destination();
            }

            return World.worldMap.worldMapData.GetLandmark(zone);
        }

        public Quest(Quest other)
        {
            CopyFrom(other);
        }

        public Quest(JsonData dat)
        {
            spawnedNPCs = new List<int>();
            FromJson(dat);
        }

        public Quest(SQuest s)
        {
            CopyFrom(QuestList.GetQuest(s.ID));

            for (int i = 0; i < s.comp.Length; i++)
            {
                goals[i].isComplete = s.comp[i];
            }

            spawnedNPCs.Clear();

            for (int i = 0; i < s.sp.Length; i++)
            {
                spawnedNPCs.Add(s.sp[i]);
            }
        }

        void CopyFrom(Quest other)
        {
            ID = other.ID;
            Name = other.Name;
            Description = other.Description;
            rewards = other.rewards;
            goals = new Goal[other.goals.Length];
            onStart = other.onStart;
            onComplete = other.onComplete;
            onFail = other.onFail;
            spawnedNPCs = other.spawnedNPCs;

            for (int i = 0; i < other.goals.Length; i++)
            {
                goals[i] = other.goals[i].Clone();
            }
        }

        void FromJson(JsonData q)
        {
            Name = q["Name"].ToString();
            ID = q["ID"].ToString();
            Description = q["Description"].ToString();

            goals = new Goal[q["Steps"].Count];

            for (int i = 0; i < q["Steps"].Count; i++)
            {
                goals[i] = GetQuestGoalFromJson(q["Steps"][i]);

                if (goals[i] != null && q["Steps"][i].ContainsKey("Events"))
                {
                    JsonData events = q["Steps"][i]["Events"];

                    for (int j = 0; j < events.Count; j++)
                    {
                        QuestEvent.EventType eventType = (events[j]["Event"].ToString()).ToEnum<QuestEvent.EventType>();
                        List<string> keys = new List<string>(events[j].Keys);

                        for (int k = 1; k < events[j].Count; k++)
                        {
                            QuestEvent questEvent = QuestList.GetEvent(keys[k], events[j][k]);
                            goals[i].AddEvent(eventType, questEvent);
                        }
                    }
                }
            }

            if (q.ContainsKey("Events"))
            {
                for (int i = 0; i < q["Events"].Count; i++)
                {
                    QuestEvent.EventType eventType = (q["Events"][i]["Event"].ToString()).ToEnum<QuestEvent.EventType>();
                    List<string> keys = new List<string>(q["Events"][i].Keys);

                    for (int j = 1; j < q["Events"][i].Count; j++)
                    {
                        QuestEvent questEvent = QuestList.GetEvent(keys[j], q["Events"][i][j]);
                        AddEvent(eventType, questEvent);
                    }
                }
            }

            if (q.ContainsKey("Start Dialogue"))
            {
                startDialogue = q["Start Dialogue"].ToString();
            }

            if (q.ContainsKey("End Dialogue"))
            {
                endDialogue = q["End Dialogue"].ToString();
            }

            if (q.ContainsKey("Chained Quest"))
            {
                chainedQuest = q["Chained Quest"].ToString();
            }

            if (q.ContainsKey("Rewards"))
            {
                int xp = (q["Rewards"].ContainsKey("XP")) ? (int)q["Rewards"]["XP"] : 0;
                int money = (q["Rewards"].ContainsKey("Money")) ? (int)q["Rewards"]["Money"] : 0;
                string[] items = new string[0];

                if (q["Rewards"].ContainsKey("Items"))
                {
                    items = new string[q["Rewards"]["Items"].Count];

                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = q["Rewards"]["Items"][i].ToString();
                    }
                }

                rewards = new QuestReward(xp, money, items);
            }
        }

        Goal GetQuestGoalFromJson(JsonData q)
        {
            string goal = q["Goal"].ToString();

            switch (goal)
            {
                case "Go":
                    string locID = q["To"][0].ToString();
                    int ele = (q["To"].Count > 1) ? (int)q["To"][1] : 0;

                    return new GoToGoal(this, locID, ele);

                case "Fetch":
                    string itemID = q["Item"][0].ToString();
                    string giveTo = q["Give To"].ToString();
                    int fetchAmt = (q["Item"].Count > 1) ? (int)q["Item"][1] : 1;

                    return new FetchGoal(this, giveTo, itemID, fetchAmt);

                case "Talk To":
                    string npcToTalkTo = q["NPC"].ToString();

                    return new TalkToGoal(this, npcToTalkTo);

                case "Kill Spawned":
                    return new SpecificKillGoal(this);

                case "Kill NPC":
                    string npcID = q["NPC"][0].ToString();
                    int killAmt = (q["NPC"].Count > 1) ? (int)q["NPC"][1] : 1;

                    return new NPCKillGoal(this, npcID, killAmt);

                case "Kill Faction":
                    string facID = q["Faction"][0].ToString();
                    int facKillAmt = (q["Faction"].Count > 1) ? (int)q["Faction"][1] : 1;

                    return new FactionKillGoal(this, facID, facKillAmt);

                default:
                    UnityEngine.Debug.LogError("Quest::GetQuestGoalFromJson - No quest goal type " + goal + "!");
                    return null;
            }
        }

        public void Start(bool skipEvent = false)
        {
            if (!isComplete)
            {
                if (goals == null || goals.Length == 0)
                    return;

                ActiveGoal.Init(skipEvent);
            }

            RunEvent(QuestEvent.EventType.OnStart);
        }

        public void CompleteGoal()
        {
            foreach (Goal g in goals)
            {
                if (!g.isComplete)
                {
                    g.Init(false);
                    return;
                }
            }

            Complete();
        }

        public void Complete()
        {
            ObjectManager.playerEntity.inventory.gold += rewards.money;
            ObjectManager.playerEntity.stats.GainExperience(rewards.xp);

            foreach (string i in rewards.itemRewards)
            {
                ObjectManager.playerEntity.inventory.PickupItem(ItemList.GetItemByID(i));
            }

            RunEvent(QuestEvent.EventType.OnComplete);
            ObjectManager.playerJournal.CompleteQuest(this);
        }

        public void Fail()
        {
            RunEvent(QuestEvent.EventType.OnFail);
        }

        public struct QuestReward
        {
            public int money;
            public int xp;
            public string[] itemRewards;

            public QuestReward(int _xp, int _money, string[] _items)
            {
                xp = _xp;
                money = _money;
                itemRewards = _items;
            }
        }
    }

    [Serializable]
    public class Goal : EventContainer
    {
        public bool isComplete = false;
        protected Quest myQuest;

        public Goal() { }

        public Goal Clone()
        {
            return (Goal)MemberwiseClone();
        }

        public virtual void Init(bool skipEvent)
        {
            if (!skipEvent)
            {
                RunEvent(QuestEvent.EventType.OnStart);
            }
        }

        public virtual void Complete()
        {
            isComplete = true;

            RunEvent(QuestEvent.EventType.OnComplete);

            if (myQuest != null)
            {
                myQuest.CompleteGoal();
            }
        }

        public virtual void Fail()
        {
            isComplete = false;

            RunEvent(QuestEvent.EventType.OnFail);

            if (myQuest != null)
            {
                myQuest.Fail();
            }
        }

        //TODO: Make this abstract.
        public virtual Coord Destination()
        {
            UnityEngine.Debug.LogError("Destination call on base Goal class.");
            return new Coord(-1, -1);
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
        }

        void NPCKilled(NPC n)
        {
            for (int i = 0; i < myQuest.spawnedNPCs.Count; i++)
            {
                if (n.UID == myQuest.spawnedNPCs[i])
                {
                    myQuest.spawnedNPCs.Remove(n.UID);
                    break;
                }
            }

            if (myQuest.spawnedNPCs.Count <= 0)
            {
                Complete();
            }
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

            UnityEngine.Debug.LogError("Quest step is either complete, or NPC UIDs is zero.");
            return null;
        }
    }

    public class NPCKillGoal : Goal
    {
        readonly string npcID;
        readonly int max;
        int current;

        public NPCKillGoal(Quest q, string nID, int amt)
        {
            myQuest = q;
            npcID = nID;
            current = 0;
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
                current++;
                if (current >= max)
                    Complete();
            }
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
    }

    public class FactionKillGoal : Goal
    {
        readonly string faction;
        readonly int max;
        int current;

        public FactionKillGoal(Quest q, string _faction, int amt)
        {
            myQuest = q;
            faction = _faction;
            current = 0;
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
                current++;
                if (current >= max)
                    Complete();
            }
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
        }

        void EnteredArea(Coord c, int ele)
        {
            if (c == coordDest && ele == elevation)
                Complete();
        }

        public override void Complete()
        {
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
        }

        void TalkToNPC(NPC n)
        {
            if (npcTarget == n.ID)
                Complete();
        }

        public override void Complete()
        {
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
            NPC n = World.objectManager.npcClasses.Find(x => x.ID == npcTarget);

            if (n == null)
            {
                UnityEngine.Debug.LogError("NPC Target is null. Cannot get destination position.");
                return null;
            }

            return n.worldPosition;
        }
    }

    public class FetchGoal : Goal
    {
        readonly string itemID;
        readonly string npcTarget;
        readonly int amount;

        public FetchGoal(Quest q, string nid, string id, int amt)
        {
            myQuest = q;
            npcTarget = nid;
            itemID = id;
            amount = amt;
            isComplete = false;
        }

        public override void Init(bool skipEvent)
        {
            base.Init(skipEvent);
            EventHandler.instance.TalkedToNPC += TalkToNPC;
        }

        void TalkToNPC(NPC n)
        {
            if (npcTarget == n.ID && Current() >= amount)
            {
                Complete();
            }
        }

        public int Current()
        {
            return ObjectManager.playerEntity.inventory.items.FindAll(x => x.ID == itemID).Count;
        }

        public override void Complete()
        {
            base.Complete();
        }

        public override void Fail()
        {
            base.Fail();
        }

        public override Coord Destination()
        {
            NPC n = World.objectManager.npcClasses.Find(x => x.ID == npcTarget);

            if (n == null)
            {
                UnityEngine.Debug.LogError("NPC Target is null. Cannot get destination position.");
                return null;
            }

            return n.worldPosition;
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

            NPC n = EntityList.GetNPCByID(npcID, worldPos, localPos, elevation);

            if (giveItem != "")
            {
                n.inventory.Add(ItemList.GetItemByID(giveItem));
            }

            World.objectManager.SpawnNPC(n);
            myQuest.spawnedNPCs.Add(n.UID);
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
}
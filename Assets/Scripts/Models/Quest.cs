using System.Collections.Generic;
using LitJson;

public class Quest : EventContainer
{
    public string ID { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Goal[] goals { get; private set; }
    public QuestReward rewards { get; private set; }
    public List<int> spawnedNPCs { get; private set; }

    public string chainedQuest { get; private set; }
    public string startDialogue { get; private set; }
    public string endDialogue { get; private set; }
    public bool failOnDeath { get; private set; }
    public bool sequential { get; private set; }

    public bool isComplete
    {
        get
        {
            if (goals == null)
                return false;

            for (int i = 0; i < goals.Length; i++)
            {
                if (!goals[i].isComplete)
                {
                    return false;
                }
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
                {
                    return goals[i];
                }
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
        CopyFrom(QuestList.GetByID(s.ID));

        for (int i = 0; i < s.comp.Length; i++)
        {
            goals[i].isComplete = s.comp[i].c;
            goals[i].amount = s.comp[i].amt;
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
        startDialogue = other.startDialogue;
        endDialogue = other.endDialogue;
        chainedQuest = other.chainedQuest;
        failOnDeath = other.failOnDeath;
        sequential = other.sequential;

        for (int i = 0; i < other.goals.Length; i++)
        {
            goals[i] = other.goals[i].Clone();
        }
    }

    void FromJson(JsonData q)
    {
        spawnedNPCs = new List<int>();

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

        if (q.ContainsKey("Fail On Death"))
        {
            failOnDeath = (bool)q["Fail On Death"];
        }

        sequential = q.ContainsKey("Sequential") ? (bool)q["Sequential"] : true;

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

            case "Fetch Property":
                string prop = q["ItemProperty"][0].ToString();
                string propGiveTo = q["Give To"].ToString();
                int propAmt = (q["ItemProperty"].Count > 1) ? (int)q["ItemProperty"][1] : 1;

                return new FetchPropertyGoal(this, propGiveTo, prop, propAmt);

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

            case "Interact":
                Coord interactPos = GetZone(q["Coordinate"].ToString());
                int interactEle = (int)q["Elevation"];
                string objType = q["Object Type"].ToString();
                int interactAmount = (int)q["Amount"];

                return new InteractGoal(this, objType, interactPos, interactEle, interactAmount);

            case "Choice":
                List<Goal> goals = new List<Goal>();

                for (int i = 0; i < q["Choices"].Count; i++)
                {
                    Goal g = GetQuestGoalFromJson(q["Choices"][i]);
                    g.RemoveQuest();
                    goals.Add(g);
                }

                return new ChoiceGoal(this, goals.ToArray());

            case "Fetch Homonculus":
                string propH = q["ItemProperty"][0].ToString();
                string propGiveToH = q["Give To"].ToString();
                int propAmtH = (q["ItemProperty"].Count > 1) ? (int)q["ItemProperty"][1] : 1;

                return new Fetch_Homonculus(this, propGiveToH, propH, propAmtH);

            default:
                UnityEngine.Debug.LogError("Quest::GetQuestGoalFromJson - No quest goal type " + goal + "!");
                return null;
        }
    }

    public void SpawnNPC(NPC n)
    {
        spawnedNPCs.Add(n.UID);
        World.objectManager.SpawnNPC(n);
    }

    public void Start(bool skipEvent = false)
    {
        if (!isComplete)
        {
            if (!skipEvent)
            {
                RunEvent(this, QuestEvent.EventType.OnStart);
            }

            if (goals == null || goals.Length == 0)
            {
                return;
            }

            for (int i = 0; i < goals.Length; i++)
            {
                goals[i].Setup(this);
            }

            if (sequential)
            {
                ActiveGoal.Init(skipEvent);
            }
            else
            {
                for (int i = 0; i < goals.Length; i++)
                {
                    goals[i].Init(skipEvent);
                }
            }
        }
    }

    public void CompleteGoal(Goal goal)
    {
        foreach (Goal g in goals)
        {
            if (!g.isComplete)
            {
                if (sequential)
                {
                    g.Init(false);
                }

                return;
            }
        }

        Complete();
    }

    void Complete()
    {
        ObjectManager.playerEntity.stats.GainExperience(rewards.xp);
        ObjectManager.playerEntity.inventory.gold += rewards.money;

        foreach (string i in rewards.itemRewards)
        {
            ObjectManager.playerEntity.inventory.PickupItem(ItemList.GetItemByID(i));
        }

        RunEvent(this, QuestEvent.EventType.OnComplete);
        ObjectManager.playerJournal.CompleteQuest(this);

        if (!string.IsNullOrEmpty(chainedQuest))
        {
            ObjectManager.playerJournal.StartQuest(QuestList.GetByID(chainedQuest), true);
        }

        if (!string.IsNullOrEmpty(endDialogue))
        {
            Alert.CustomAlert_WithTitle("Quest Complete", endDialogue);
        }
    }

    public void Fail()
    {
        RunEvent(this, QuestEvent.EventType.OnFail);
        ObjectManager.playerJournal.FailQuest(this);
    }

    public SQuest ToSQuest()
    {
        return new SQuest(this);
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

[System.Serializable]
public class SQuest
{
    public string ID;
    public SQuestStep[] comp; //Completeness of steps
    public int[] sp; //Spawned npcs

    public SQuest() { }

    public SQuest(Quest q)
    {
        ID = q.ID;

        comp = new SQuestStep[q.goals.Length];

        for (int i = 0; i < q.goals.Length; i++)
        {
            comp[i] = new SQuestStep(q.goals[i].isComplete, q.goals[i].amount);
        }

        sp = new int[q.spawnedNPCs.Count];

        for (int i = 0; i < q.spawnedNPCs.Count; i++)
        {
            sp[i] = q.spawnedNPCs[i];
        }
    }

    public struct SQuestStep
    {
        public bool c; //completeness
        public int amt; //amount

        public SQuestStep(bool complete, int amount)
        {
            c = complete;
            amt = amount;
        }
    }
}
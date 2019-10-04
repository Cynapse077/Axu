using System.Collections.Generic;
using LitJson;

public class Quest : EventContainer, IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public string Name;
    public string Description;
    public Goal[] goals;
    public QuestReward rewards;
    public List<int> spawnedNPCs;
    public int turnsToFail = -999; //If -999, does not fail on turn counter.

    public string chainedQuest;
    public string startDialogue;
    public string endDialogue;
    public bool failOnDeath;
    public bool sequential;

    //Stored objects
    public int storedNPCUID { get; private set; } = -1;
    public int storedObjectUID { get; private set; } = -1;
    NPC_Blueprint storedNPCBlueprint;
    MapObjectBlueprint storedObjectBlueprint;

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

    public NPC StoredNPC
    {
        get
        {
            return World.objectManager.npcClasses.Find(x => x.UID == storedNPCUID) ?? null;
        }
    }

    public MapObject StoredObject
    {
        get
        {
            return World.objectManager.mapObjects.Find(x => x.UID == storedObjectUID) ?? null;
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
        CopyFrom(GameData.Get<Quest>(s.ID));

        //HACK!
        //Override steps and events for a simple "go to point" step.
        if (s.storedGoal != null)
        {
            goals = new Goal[1]
            {
                new GoToGoal_Specific(this, s.storedGoal, 0)
            };

            AddEvent(QuestEvent.EventType.OnFail, new RemoveSpecificLocationEvent(s.storedGoal));
        }
        else
        {
            for (int i = 0; i < s.comp.Length; i++)
            {
                goals[i].isComplete = s.comp[i].c;
                goals[i].amount = s.comp[i].amt;
            }
        }

        spawnedNPCs = new List<int>();

        for (int i = 0; i < s.sp.Length; i++)
        {
            spawnedNPCs.Add(s.sp[i]);
        }

        turnsToFail = s.turnsToFail;
        storedNPCUID = s.storedNPCUID;
        storedObjectUID = s.storedObjectUID;
    }

    void CopyFrom(Quest other)
    {
        if (other == null)
        {
            return;
        }

        ID = other.ID;
        Name = other.Name;
        Description = other.Description;
        rewards = other.rewards;
        onStart = other.onStart;
        onComplete = other.onComplete;
        onFail = other.onFail;
        spawnedNPCs = other.spawnedNPCs;
        startDialogue = other.startDialogue;
        endDialogue = other.endDialogue;
        chainedQuest = other.chainedQuest;
        failOnDeath = other.failOnDeath;
        sequential = other.sequential;
        turnsToFail = other.turnsToFail;
        storedNPCUID = other.storedNPCUID;
        storedObjectUID = other.storedObjectUID;
        storedNPCBlueprint = other.storedNPCBlueprint;
        storedObjectBlueprint = other.storedObjectBlueprint;

        if (other.goals != null)
        {
            goals = new Goal[other.goals.Length];

            for (int i = 0; i < other.goals.Length; i++)
            {
                goals[i] = other.goals[i].Clone();
            }
        }
    }

    public void FromJson(JsonData q)
    {
        spawnedNPCs = new List<int>();

        Name = q["Name"].ToString();
        ID = q["ID"].ToString();
        Description = q["Description"].ToString();

        if (q.ContainsKey("Stored NPC"))
        {
            NewUniqueNPC(q["Stored NPC"]);
        }

        if (q.ContainsKey("Stored Object"))
        {
            NewUniqueObject(q["Stored Object"]);
        }

        if (q.ContainsKey("Steps"))
        {
            goals = new Goal[q["Steps"].Count];

            for (int i = 0; i < q["Steps"].Count; i++)
            {
                goals[i] = GetQuestGoalFromJson(q["Steps"][i]);

                if (goals[i] != null && q["Steps"][i].ContainsKey("Events"))
                {
                    JsonData events = q["Steps"][i]["Events"];

                    for (int j = 0; j < events.Count; j++)
                    {
                        QuestEvent.EventType eventType = events[j]["Event"].ToString().ToEnum<QuestEvent.EventType>();
                        List<string> keys = new List<string>(events[j].Keys);

                        for (int k = 1; k < events[j].Count; k++)
                        {
                            QuestEvent questEvent = GetEvent(keys[k], events[j][k]);
                            goals[i].AddEvent(eventType, questEvent);
                        }
                    }
                }
            }
        }

        if (q.ContainsKey("Events"))
        {
            for (int i = 0; i < q["Events"].Count; i++)
            {
                QuestEvent.EventType eventType = q["Events"][i]["Event"].ToString().ToEnum<QuestEvent.EventType>();
                List<string> keys = new List<string>(q["Events"][i].Keys);

                for (int j = 1; j < q["Events"][i].Count; j++)
                {
                    QuestEvent questEvent = GetEvent(keys[j], q["Events"][i][j]);
                    AddEvent(eventType, questEvent);
                }
            }
        }

        q.TryGetString("Start Dialogue", out startDialogue);
        q.TryGetString("End Dialogue", out endDialogue);
        q.TryGetString("Chained Quest", out chainedQuest);
        q.TryGetBool("Fail On Death", out failOnDeath);
        q.TryGetBool("Sequential", out sequential);
        q.TryGetInt("Turns To Fail", out turnsToFail, -999);

        if (q.ContainsKey("Rewards"))
        {
            string[] items = new string[0];

            q["Rewards"].TryGetInt("XP", out int xp);
            q["Rewards"].TryGetInt("Money", out int money);

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

    //Set storedObjectUID
    public void NewUniqueObject(JsonData dat)
    {
        string id = string.Empty;

        if (dat.ContainsKey("ID"))
        {
            id = dat["ID"].ToString();
        }
        else if (dat.ContainsKey("Random From"))
        {
            int ran = SeedManager.combatRandom.Next(0, dat["Random From"].Count);
            id = dat["Random From"][ran].ToString();
        }

        MapObjectBlueprint bp = new MapObjectBlueprint(GameData.Get<MapObjectBlueprint>(id));

        if (bp == null)
        {
            Log.Error("Quest \"" + ID + "\" - could not spawn object with ID \"" + id + "\"");
            return;
        }

        bp.zone = dat["Zone"].ToString();
        dat.TryGetInt("Elevation", out bp.elevation, 0);

        Coord position = new Coord(SeedManager.combatRandom.Next(0, Manager.localMapSize.x), SeedManager.combatRandom.Next(0, Manager.localMapSize.y));
        dat.TryGetCoord("Position", out position, position);
        bp.localPosition = position;

        List<string> inventory = new List<string>();

        if (dat.ContainsKey("Inventory"))
        {
            for (int i = 0; i < dat["Inventory"].Count; i++)
            {
                Item item = ItemList.GetItemByID(dat["Inventory"][i].ToString());

                if (!item.IsNullOrDefault())
                {
                    inventory.Add(dat["Inventory"][i].ToString());
                }
            }
        }

        bp.inventory = inventory;
        storedObjectBlueprint = bp;
    }

    //Set storedNPCUID
    public void NewUniqueNPC(JsonData dat)
    {
        string id = string.Empty;

        if (dat.ContainsKey("ID"))
        {
            id = dat["ID"].ToString();
        }
        else if (dat.ContainsKey("Random From"))
        {
            int ran = SeedManager.combatRandom.Next(0, dat["Random From"].Count);
            id = dat["Random From"][ran].ToString();
        }

        NPC_Blueprint bp = new NPC_Blueprint(GameData.Get<NPC_Blueprint>(id));

        if (bp == null)
        {
            Log.Error("Quest \"" + ID + "\" - could not spawn NPC with ID \"" + id + "\"");
            return;
        }

        if (dat.ContainsKey("Name"))
        {
            if (dat["Name"].ToString() == "Random")
            {
                bp.flags.Add(NPC_Flags.Named_NPC);
            }
            else
            {
                bp.name = dat["Name"].ToString();
            }
        }

        dat.TryGetString("Zone", out bp.zone, bp.zone);
        dat.TryGetInt("Elevation", out bp.elevation, 0);

        Coord position = new Coord(SeedManager.combatRandom.Next(1, Manager.localMapSize.x), SeedManager.combatRandom.Next(1, Manager.localMapSize.y));
        dat.TryGetCoord("Position", out position, position);
        bp.localPosition = position;

        storedNPCBlueprint = bp;
    }

    Goal GetQuestGoalFromJson(JsonData q)
    {
        string goal = q["Goal"].ToString();
        string desc = q.ContainsKey("Description") ? q["Description"].ToString() : null;

        switch (goal)
        {
            case "Go":
                string locID = q["To"][0].ToString();
                int ele = (q["To"].Count > 1) ? (int)q["To"][1] : 0;

                return new GoToGoal(this, locID, ele, desc);

            case "Fetch":
                string itemID = q["Item"][0].ToString();
                string giveTo = q["Give To"].ToString();
                int fetchAmt = (q["Item"].Count > 1) ? (int)q["Item"][1] : 1;

                return new FetchGoal(this, giveTo, itemID, fetchAmt, desc);

            case "Fetch Property":
                string prop = q["ItemProperty"][0].ToString();
                string propGiveTo = q["Give To"].ToString();
                int propAmt = (q["ItemProperty"].Count > 1) ? (int)q["ItemProperty"][1] : 1;

                return new FetchPropertyGoal(this, propGiveTo, prop, propAmt, desc);

            case "Talk To":
                string npcToTalkTo = q["NPC"].ToString();

                if (npcToTalkTo == "Stored NPC")
                {
                    return new TalkToStoredNPCGoal(this, desc);
                }

                return new TalkToGoal(this, npcToTalkTo, desc);

            case "Kill Spawned":
                return new SpecificKillGoal(this, desc);

            case "Kill NPC":
                string npcID = q["NPC"][0].ToString();
                int killAmt = (q["NPC"].Count > 1) ? (int)q["NPC"][1] : 1;

                return new NPCKillGoal(this, npcID, killAmt, desc);

            case "Kill Faction":
                string facID = q["Faction"][0].ToString();
                int facKillAmt = (q["Faction"].Count > 1) ? (int)q["Faction"][1] : 1;

                return new FactionKillGoal(this, facID, facKillAmt, desc);

            case "Interact":
                int interactEle = (int)q["Elevation"];
                string objType = q["Object Type"].ToString();
                int interactAmount = (int)q["Amount"];

                return new InteractGoal(this, objType, q["Coordinate"].ToString(), interactEle, interactAmount, desc);

            case "Interact With Stored Object":
                return new InteractUIDGoal(this, desc);

            case "Choice":
                List<Goal> goals = new List<Goal>();

                for (int i = 0; i < q["Choices"].Count; i++)
                {
                    Goal g = GetQuestGoalFromJson(q["Choices"][i]);
                    g.RemoveQuest();
                    goals.Add(g);
                }

                return new ChoiceGoal(this, goals.ToArray(), desc);

            case "Fetch Homonculus":
                string propH = q["ItemProperty"][0].ToString();
                string propGiveToH = q["Give To"].ToString();
                int propAmtH = (q["ItemProperty"].Count > 1) ? (int)q["ItemProperty"][1] : 1;

                return new Fetch_Homonculus(this, propGiveToH, propH, propAmtH, desc);

            default:
                UnityEngine.Debug.LogError("Quest::GetQuestGoalFromJson - No quest goal type " + goal + "!");
                return null;
        }
    }

    public QuestEvent GetEvent(string key, JsonData data)
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

            case "Progress Flag":
                return new GiveProgressFlagEvent(data.ToString());

            case "Log Message":
                return new LogMessageEvent(data.ToString());

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

                return new SetNPCDialogueTreeEvent(dialogueNPC, diaID);

            case "Open Dialogue":
                string speaker = data["Speaker"].ToString();
                string dialogue = data["Dialogue"].ToString();

                return new OpenDialogueEvent(speaker, dialogue);

            case "Create Location":
                string zoneID = data["Zone ID"].ToString();

                return new CreateLocationEvent(zoneID);

            case "Remove Location":
                string remID = data["Zone ID"].ToString();

                return new RemoveLocationEvent(remID);

            case "Set Item Modifier":
                string itemSlot = data["Item Slot"].ToString();
                string modID = data["Modifier"].ToString();

                return new SetItemModifierEvent(itemSlot, modID);

            default:
                Log.Error("QuestList::GetEvent() - No event with ID \"" + key + "\".");
                return null;
        }
    }

    public void OnTurn()
    {
        if (turnsToFail > -999)
        {
            turnsToFail--;

            if (turnsToFail <= 0)
            {
                Fail();
            }
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
                //Spawn stored NPC.
                if (storedNPCBlueprint != null)
                {
                    Coord wPos = World.tileMap.worldMap.GetRandomLandmark(storedNPCBlueprint.zone);
                    NPC n = new NPC(storedNPCBlueprint, wPos, storedNPCBlueprint.localPosition, storedNPCBlueprint.elevation);
                    storedNPCUID = n.UID;
                    World.objectManager.SpawnNPC(n);
                }

                //Spawn stored Object
                if (storedObjectBlueprint != null)
                {
                    Coord wPos = World.tileMap.worldMap.GetRandomLandmark(storedObjectBlueprint.zone);
                    MapObject m = new MapObject(storedObjectBlueprint, storedObjectBlueprint.localPosition, wPos, storedObjectBlueprint.elevation);
                    storedObjectUID = m.UID;
                    World.objectManager.SpawnObject(m);
                }

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

        if (!string.IsNullOrEmpty(chainedQuest) && GameData.TryGet(chainedQuest, out Quest q))
        {
            ObjectManager.playerJournal.StartQuest(new Quest(GameData.Get<Quest>(chainedQuest)));
        }

        if (!string.IsNullOrEmpty(endDialogue))
        {
            Alert.CustomAlert_WithTitle("Quest Complete", endDialogue);
        }

        if (StoredNPC != null)
        {
            Entity ent = World.objectManager.GetEntityFromNPC(StoredNPC);
            World.objectManager.DemolishNPC(ent, StoredNPC);
        }
    }

    public void Fail()
    {
        foreach (Goal g in goals)
        {
            if (!g.isComplete)
            {
                g.Fail();
            }
        }

        RunEvent(this, QuestEvent.EventType.OnFail);
        ObjectManager.playerJournal.FailQuest(this);
    }

    public IEnumerable<string> LoadErrors()
    {
        if (Name.NullOrEmpty())
        {
            yield return "Name not set.";
        }

        if (Description.NullOrEmpty())
        {
            yield return "Description not set.";
        }

        //Goals can be null, in instances where we want to fill in the goal list at runtime.
        if (goals != null)
        {
            foreach (Goal g in goals)
            {
                IEnumerable<string> errors = g.LoadErrors();
                foreach (string error in errors)
                {
                    yield return error;
                }

                foreach (string error in g.LoadingEventErrors())
                {
                    yield return error;
                }
            }
        }

        foreach (string error in LoadingEventErrors())
        {
            yield return error;
        }
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
    public Coord storedGoal;
    public SQuestStep[] comp; //Completeness of steps
    public int[] sp; //Spawned npcs
    public int turnsToFail = -999;
    public int storedNPCUID = -1;
    public int storedObjectUID = -1;

    public SQuest() { }

    public SQuest(Quest q)
    {
        ID = q.ID;

        comp = new SQuestStep[q.goals.Length];

        for (int i = 0; i < q.goals.Length; i++)
        {
            if (q.goals[i] is GoToGoal_Specific sp)
            {
                storedGoal = sp.Destination();
            }

            comp[i] = new SQuestStep(q.goals[i].isComplete, q.goals[i].amount);
        }

        sp = new int[q.spawnedNPCs.Count];

        for (int i = 0; i < q.spawnedNPCs.Count; i++)
        {
            sp[i] = q.spawnedNPCs[i];
        }

        turnsToFail = q.turnsToFail;
        storedNPCUID = q.storedNPCUID;
        storedObjectUID = q.storedObjectUID;
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
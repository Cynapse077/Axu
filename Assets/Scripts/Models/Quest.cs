using UnityEngine;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData] 
public class Quest {

	public string Name, ID, Description, startDialogue, endDialogue, chainedQuestID, followRules;
	public QuestType questType;
	public List<QuestStep> steps;
	public List<QuestEvent> events;
	public Coord destination;
    public NPC questGiver;
	public ProgressFlags flag = ProgressFlags.None;
	public QuestRewards rewards;

	public Quest() {
		rewards.items = new List<string>();
		steps = new List<QuestStep>();
		events = new List<QuestEvent>();
		flag = ProgressFlags.None;
	}

	public Quest(string _name, string _id, NPC _questGiver) {
        Name = _name;
		ID = _id;
        questGiver = _questGiver;
		steps = new List<QuestStep>();
		events = new List<QuestEvent>();
		rewards.XP = 0;
		rewards.items = new List<string>();
		destination = null;
		flag = ProgressFlags.None;
    }

	public Quest(Quest other) {
		CopyFrom(other);
	}

	public QuestStep NextStep {
		get { 
			if (steps.Count <= 0)
				return null;
			
			return steps[0]; 
		}
	}

	public void StartQuest() {
		if (questGiver != null) {
			questGiver.onDeath += Fail;

			if (followRules == "Permanent" || followRules == "Until Completion")
				MakeFollower();
		}

		Initialize();
		OnStart();
		OnStepStart();
	}

	public void Initialize() {
		if (steps.Count > 0) {
			for (int i = 0; i < steps.Count; i++) {
                if (!string.IsNullOrEmpty(steps[i].dest))
					steps[i].destination = GetZone(steps[i].dest);
			}

			destination = NextStep.destination;
		}

		if (NextStep == null)
			return;

		if (destination != null) {
            int iconNum = (NextStep.goal == "KillAt" || NextStep.goal == "KillAllAt") ? 1 : 0;
            World.objectManager.NewMapIcon(iconNum, destination);
		} else if (NextStep.goal == "Fetch" && questGiver != null) {
			World.objectManager.NewMapIcon(0, questGiver.worldPosition);
		}
	}

	void OnStart() {
		foreach (QuestEvent e in events) {
			if (e.EventName == QuestEvent.QEventNames.OnStart)
				RunEvent(e);
		}
	}

	void RunEvent(QuestEvent ev) {
		foreach (QuestEvent.SubEvent myEvent in ev.SubEvents) {
			//Run a console command
			if (myEvent.Type == "Console Command") {
				string[] cons = myEvent.Name.Split("|"[0]);

				for (int i = 0; i < cons.Length; i++) {
					World.objectManager.GetComponent<Console>().ParseTextField(cons[i]);
				}
			}

			//Run a Lua script.
			if (myEvent.Type == "Run Lua")
				LuaManager.CallScriptFunction(myEvent.Name, myEvent.GiveItem);

			//Set the player's world position.
			if (myEvent.Type == "WorldPos" && myEvent.WorldPos != null) {
				Coord worldPosition = GetZone(myEvent.WorldPos);
				World.tileMap.worldCoordX = worldPosition.x;
				World.tileMap.worldCoordY = worldPosition.y;
				World.tileMap.HardRebuild();

				if (ev.EventName == QuestEvent.QEventNames.OnStart)
					World.userInterface.CloseWindows();
			}

			//Set the player's local position.
			if (myEvent.Type == "LocalPos" && myEvent.LocalPos != null) {
				ObjectManager.playerEntity.ForcePosition(new Coord(myEvent.LocalPos.x, myEvent.LocalPos.y));
				ObjectManager.playerEntity.BeamDown();
				World.tileMap.SoftRebuild();

				if (ev.EventName == QuestEvent.QEventNames.OnStart)
					World.userInterface.CloseWindows();
			}

			//Spawn an NPC.
			if (myEvent.Type == "NPC") {
				Coord worldPosition = GetZone(myEvent.WorldPos);
				Coord lp = null;

				if (myEvent.LocalPos != null)
					lp = new Coord(myEvent.LocalPos.x, myEvent.LocalPos.y);
				
				NPC n = SpawnController.SpawnNPCByID(myEvent.Name, worldPosition, myEvent.Elevation, lp);
                n.AddFlag(NPC_Flags.SpawnedFromQuest);

				if (Name.Contains("arena"))
					n.inventory.Clear();
				
				if (!string.IsNullOrEmpty(myEvent.GiveItem)) {
					if (myEvent.GiveItem == "SETQGIVER")
						questGiver = n;
					else if (ItemList.GetItemByID(myEvent.GiveItem) != null)
						n.inventory.Add(ItemList.GetItemByID(myEvent.GiveItem));
				}
					
			//Spawns a group of NPCs.
			} else if (myEvent.Type == "SpawnGroup") {
				Coord worldPosition = GetZone(myEvent.WorldPos);
				SpawnController.SpawnFromGroupNameAt(myEvent.Name, myEvent.Elevation, worldPosition);

			//Spawns a specified object.
			} else if (myEvent.Type == "Object") {
				Coord worldPosition = GetZone(myEvent.WorldPos);
				MapObject m = World.objectManager.NewObjectAtOtherScreen(myEvent.Name, myEvent.LocalPos, worldPosition, myEvent.Elevation);
				if (myEvent.GiveItem != null && myEvent.GiveItem != "")
					m.inv.Add(ItemList.GetItemByID(myEvent.GiveItem));				
			}

			//Give an NPC a quest
			if (myEvent.Type == "Give Quest") {
				if (questGiver != null) {
					if (World.objectManager.npcClasses.Find(x => x.ID == myEvent.GiveItem) != null) {
						World.objectManager.npcClasses.Find(x => x.ID == myEvent.GiveItem).questID = myEvent.Name;
						World.tileMap.HardRebuild();
						World.tileMap.LightCheck(ObjectManager.playerEntity);
					}
				}
			}

			//Move a specific NPC to a specific coordinate
			if (myEvent.Type == "Move NPC") {
				if (World.objectManager.npcClasses.Find(x => x.ID == myEvent.Name) != null) {
					NPC n = World.objectManager.npcClasses.Find(x => x.ID == myEvent.Name);
					if (myEvent.WorldPos != null)
						n.worldPosition = GetZone(myEvent.WorldPos);
					if (myEvent.LocalPos != null)
						n.localPosition = new Coord(myEvent.LocalPos.x, myEvent.LocalPos.y);
					n.elevation = myEvent.Elevation;

					World.tileMap.HardRebuild();
				}
			}

			if (myEvent.Type == "Mod Item" && ItemList.GetModByID(myEvent.GiveItem) != null) {
				ObjectManager.playerEntity.body.MainHand.equippedItem.AddModifier(ItemList.GetModByID("xul"));
			}

			//Remove an item from an NPC (In the case that they gave you something special. Replace it with something else.
			if (myEvent.Type == "Remove Item" && questGiver != null) {
				for (int e = 0; e < questGiver.handItems.Count; e++) {
					if (questGiver.handItems[e].ID == myEvent.Name)
						questGiver.handItems[e] = ItemList.GetItemByID(myEvent.GiveItem);
				}

				if (questGiver.firearm.ID == myEvent.Name)
					questGiver.firearm = ItemList.GetNone();

				for (int b = 0; b < questGiver.bodyParts.Count; b++) {
					if (questGiver.bodyParts[b].equippedItem != null && questGiver.bodyParts[b].equippedItem.ID == myEvent.Name)
						questGiver.bodyParts[b].equippedItem = ItemList.GetNone();
				}


				World.tileMap.HardRebuild();
				World.tileMap.LightCheck(ObjectManager.playerEntity);
			}

			//Remove stair blockers. All on one screen.
			if (myEvent.Type == "Remove Blockers") {
				Coord wp = GetZone(myEvent.WorldPos);
				List<MapObject> mos = World.objectManager.ObjectsAt(wp, myEvent.Elevation);
				List<MapObject> toDelete = mos.FindAll(x => x.objectType == "Stair_Lock" && (x.localPosition == myEvent.LocalPos || myEvent.LocalPos == null));

				while (toDelete.Count > 0) {
					World.objectManager.mapObjects.Remove(toDelete[0]);
					toDelete.RemoveAt(0);
				}

				if (World.tileMap.WorldPosition == wp && World.tileMap.currentElevation == myEvent.Elevation)
					World.tileMap.HardRebuild();
			}

            //Creates a blocker object.
			if (myEvent.Type == "Create Blocker") {
				Coord wp = GetZone(myEvent.WorldPos);
				Coord lp = myEvent.LocalPos;
				int ele = myEvent.Elevation;

				World.objectManager.NewObjectAtOtherScreen("Stair_Lock", lp, wp, ele);
			}

            //Sets the current world elevation, bringing the player with it.
			if (myEvent.Type == "Set Elevation") {
				int ele = myEvent.Elevation;
				World.tileMap.currentElevation = ele;

				World.tileMap.HardRebuild();
			}
		}
	}

	void MakeFollower() {
		questGiver.MakeFollower();
		CombatLog.NameMessage("Message_Hire",questGiver.name);
	}

	Coord GetRandomCloseDestination(int radius, int minDistance) {
		List<Coord> possibleCoords = WalkableNearbyTiles(radius, minDistance);
		int rad = radius, mdis = minDistance;

		while (possibleCoords.Count <= 0) {
			rad++;

			if (mdis > 1)
				mdis --;
			
			possibleCoords = WalkableNearbyTiles(rad, mdis);
		}

        if (possibleCoords.Count > 0)
		    return possibleCoords.GetRandom();
        else {
            Debug.LogError("Could not find random close point at destination.");
            return null;
        }
	}

	List<Coord> WalkableNearbyTiles(int radius, int minDistance) {
		Coord currPos = World.tileMap.WorldPosition;
		List<Coord> possibleCoords = new List<Coord>();

		for (int x = -radius; x <= radius; x++) {
			for (int y = -radius; y <= radius; y++) {
                if (currPos.x + x < 0 || currPos.y + y < 0 || currPos.x + x >= Manager.worldMapSize.x || currPos.y >= Manager.worldMapSize.y)
                    continue;

				if (Mathf.Abs(x) + Mathf.Abs(y) < minDistance || World.tileMap.IsOceanWorldTile(currPos.x + x, currPos.y + y))
					continue;
				if (World.tileMap.WalkableWorldTile(currPos.x + x, currPos.y + y))
					possibleCoords.Add(new Coord(currPos.x + x, currPos.y + y));
			}
		}

		return possibleCoords;
	}

    public void Fail() {
		if (!ObjectManager.playerJournal.quests.Contains(this))
			return;
		
		if (destination != null)
			World.objectManager.RemoveMapIconAt(destination);
		
		Alert.NewAlert("Quest_Failed", Name, null);
		ObjectManager.playerJournal.quests.Remove(this);

		UnregisterCallbacks();
		OnFail();
    }

	void OnFail() {
		foreach (QuestEvent e in events) {
			if (e.EventName == QuestEvent.QEventNames.OnFail)
				RunEvent(e);
		}
	}

	bool CanCompleteStep(int i) {
		QuestStep step = steps[i];

		if (step.goal == "Go" || step.goal == "Return")
			return (step.destination == World.tileMap.WorldPosition && step.e == World.tileMap.currentElevation);
		
		if (step.goal == "Fetch") {
			Inventory playerInventory = ObjectManager.player.GetComponent<Inventory>();
			List<Item> relItems = new List<Item>();
			int amountHeld = 0;

			if (ItemList.GetItemByID(step.of) != null) {
				relItems = playerInventory.items.FindAll(x => x.ID == step.of);
			} else {
				ItemProperty prop = step.of.ToEnum<ItemProperty>();
				relItems = playerInventory.items.FindAll(x => x.HasProp(prop));
			}

			if (relItems != null && relItems.Count > 0) {
				for (int j = 0; j < relItems.Count; j++) {
					amountHeld += relItems[j].amount;
				}
			}

			return amountHeld >= step.am;
		}

		if (step.goal == "Give BPs") {
			Inventory playerInventory = ObjectManager.player.GetComponent<Inventory>();
			List<Item> relevantItems = playerInventory.items.FindAll(x => x.HasProp(ItemProperty.Replacement_Limb));
			return (relevantItems.Count >= step.am);
		}

		if (step.goal == "Kill" || step.goal == "KillAt" || NextStep.goal == "Use Terminal" || NextStep.goal == "Destroy Shrine")
			return (step.amC >= step.am);

		return true;
	}


	public bool CanComplete() {
		for (int i = 0; i < steps.Count; i++) {
			if (!CanCompleteStep(i))
				return false;
		}

		return true;
	}

	public bool TryComplete() {
		if (CanComplete()) {
			for (int i = 0; i < steps.Count; i++) {
				CompleteStep(steps[i]);
			}

			ObjectManager.playerJournal.CompleteQuest(this);
			return true;
		}

		return false;
	}

	public void OnComplete() {
		foreach (QuestEvent e in events) {
			if (e.EventName == QuestEvent.QEventNames.OnComplete)
				RunEvent(e);
		}
	}

    public void Complete() {
		if (!string.IsNullOrEmpty(endDialogue))
			Alert.NewAlert("Quest_Complete", null, endDialogue + RewardDescription());
		else
			Alert.NewAlert("Quest_Complete2", Name, RewardDescription());
		
        if (ObjectManager.player != null) {
			ObjectManager.playerEntity.stats.GainExperience(rewards.XP);
			ObjectManager.playerEntity.inventory.gold += rewards.gold;

			if (rewards.items.Count > 0) {
				List<Item> items = new List<Item>();

				for (int i = 0; i < rewards.items.Count; i++) {
					items.Add(ItemList.GetItemByID(rewards.items[i]));
				}

				World.objectManager.NewInventory("Loot", ObjectManager.playerEntity.myPos, World.tileMap.WorldPosition, items);
			}

			if (flag != ProgressFlags.None)
				ObjectManager.playerJournal.AddFlag(flag);

			if (followRules == "Upon Completion" && questGiver != null)
				questGiver.MakeFollower();

			OnComplete();
			 
			if (chainedQuestID != null && chainedQuestID != "") {
				Quest q = QuestList.GetByID(chainedQuestID);

				if (q != null)
					ObjectManager.playerJournal.StartQuest(new Quest(q));
			}
        }

		World.objectManager.UpdateDialogueOptions();
		UnregisterCallbacks();
    }

	public void CompleteStep(QuestStep qs) {
		if (destination != null)
			World.objectManager.RemoveMapIconAt(destination);
		else if (qs.goal == "Return" || qs.goal == "Fetch")
			World.objectManager.RemoveMapIconAt(questGiver.worldPosition);
		else if (qs.goal == "Kill" || qs.goal == "KillAt")
			World.objectManager.RemoveMapIconAt(World.tileMap.WorldPosition);
		
		if (qs.goal == "Fetch" || qs.goal == "Give BPs")
			GiveInventory(qs);

		OnCompleteStep();
		steps.Remove(qs);

		if (steps.Count > 0) {
			destination = NextStep.destination;
			CombatLog.NameMessage("Quest_Update", Name);

			if (destination != null) {
				if (NextStep.goal == "KillAt")
					World.objectManager.NewMapIcon(1, destination);
				else
					World.objectManager.NewMapIcon(0, destination);
			} else if (NextStep.goal == "Kill")
				World.objectManager.NewMapIcon(1, World.tileMap.WorldPosition);
			
			if (NextStep.goal == "Return" || NextStep.goal == "Fetch")
				World.objectManager.NewMapIcon(0, questGiver.worldPosition);

			OnStepStart();
		}
	}

	void GiveInventory(QuestStep qs) {
		Inventory playerInventory = ObjectManager.player.GetComponent<Inventory>();
		List<Item> relevantItems = new List<Item>();
		int amountRemoved = 0;

		if (qs.goal == "Fetch") {
			relevantItems = (ItemList.GetItemByID(qs.of) != null) ? playerInventory.items.FindAll(x => x.ID == qs.of) 
				: playerInventory.items.FindAll(x => x.HasProp(qs.of.ToEnum<ItemProperty>()));
		} else if (qs.goal == "Give BPs") {
			relevantItems = playerInventory.items.FindAll(x => x.HasProp(ItemProperty.Replacement_Limb));
		}

		for (int i = 0; i < relevantItems.Count; i++) {
			Item newItem = new Item(relevantItems[i]);
			newItem.amount = 1;

			for (int j = 0; j < relevantItems[i].amount; j++) {
				playerInventory.RemoveInstance(relevantItems[i]);
				amountRemoved++;

				if (j != 0)
					newItem.amount++;

				if (amountRemoved >= qs.am)
					break;
			}

			CombatLog.NameItemMessage("Quest_GiveItem", questGiver.name, relevantItems[i].Name);

			//Add item to NPC's inventory, on-screen or not.
			Entity qg = World.objectManager.GetEntityFromNPC(questGiver);
			if (qg != null)
				qg.inventory.PickupItem(newItem);
			else
				questGiver.inventory.Add(newItem);

			if (amountRemoved >= qs.am)
				break;
		}

		World.tileMap.HardRebuild();
		World.tileMap.LightCheck(ObjectManager.playerEntity);
	}

	void OnStepStart() {
		if (NextStep != null && NextStep.evnt != null) {
			foreach (QuestEvent e in NextStep.evnt) {
				if (e.EventName == QuestEvent.QEventNames.OnStepStart)
					RunEvent(e);
			}
		}
	}

	void OnCompleteStep() {
		if (NextStep != null && NextStep.evnt != null) {
			foreach (QuestEvent e in NextStep.evnt) {
				if (e.EventName == QuestEvent.QEventNames.OnStepComplete)
					RunEvent(e);
			}
		}
	}

	string RewardDescription() {
		if (rewards.XP == 0 && rewards.gold == 0)
			return (rewards.items.Count > 0) ? "\n<color=yellow>Some items have dropped beneath your feet!</color>" : "";
		
		string des = "\n<color=yellow>You received ";
		if (rewards.XP > 0)
			des +=  rewards.XP + " XP" + ((rewards.gold > 0) ? "" : ".");
		if (rewards.gold > 0)
			des += " and $" + rewards.gold.ToString();

		if (rewards.items.Count > 0)
			des += "\nSome items have dropped beneath your feet!";
		des += "</color>";

		return des;
	}

    public Coord GetZone(string zone) {
        switch (zone) {
            case "Current":
                return World.tileMap.WorldPosition;
            case "Random_Close":
                return World.tileMap.worldMap.GetOpenPosition(World.tileMap.WorldPosition, 10);
            case "Random_Landmark":
                return World.tileMap.GetRandomLandmark().position;
            case "Quest Giver Location":
                return questGiver.worldPosition;
            case "Destination":
                return NextStep.destination;            
        }

        //TODO: Modifiers for closest, random, etc.
        //bool closest = zone.Contains("Closest_");
        //bool random = zone.Contains("Random_");

        Coord dest = World.worldMap.worldMapData.GetLandmark(zone);
        return dest;
    }

	public void PlaceNameInDescription() {
		if (!string.IsNullOrEmpty(Description) && Description.Contains("(QUESTGIVERNAME)"))
			Description = Description.Replace("(QUESTGIVERNAME)", questGiver.name);
	}

	public void ReplaceNameInDialogue() {
		if (!string.IsNullOrEmpty(startDialogue) && startDialogue.Contains("(PLAYERNAME)"))
			startDialogue = startDialogue.Replace("(PLAYERNAME)", Manager.playerName);

		if (!string.IsNullOrEmpty(endDialogue) && endDialogue.Contains("(PLAYERNAME)"))
			endDialogue = endDialogue.Replace("(PLAYERNAME)", Manager.playerName);
			
	}

	void CopyFrom(Quest other) {
		if (other == null) {
			Debug.Log("null quest");
			return;
		}

		Name = other.Name;
		ID = other.ID;
		Description = other.Description;
		startDialogue = other.startDialogue;
		endDialogue = other.endDialogue;
		questGiver = other.questGiver;

		rewards.XP = other.rewards.XP;
		rewards.gold = other.rewards.gold;
		rewards.items = new List<string>(other.rewards.items);

		steps = new List<QuestStep>();
		for (int i = 0; i < other.steps.Count; i++) {
			steps.Add(new QuestStep(other.steps[i]));
		}
			
		events = new List<QuestEvent>(other.events);
		chainedQuestID = other.chainedQuestID;

		questType = other.questType;
		flag = other.flag;
		followRules = other.followRules;
	}

	void UnregisterCallbacks() {
		if (questGiver != null) {
			questGiver.onDeath -= Fail;

			if (followRules != "" && followRules != "Permanent" && followRules != "Upon Completion") {
				Coord empty = new Coord(0, 0);
				NPC template = EntityList.GetNPCByID(questGiver.ID, empty, empty);
				questGiver.faction = template.faction;
				questGiver.flags = template.flags;
			}
		}
	}

	public SQuest ToSQuest() {
		string linked = chainedQuestID;
		string qgname = (questGiver != null) ? questGiver.UID : null;

		return new SQuest(Name, ID, Description, endDialogue, steps, events, qgname, linked, followRules, (int)flag, rewards);
	}

	public enum QuestType {
		Main, Side, Random, Daily
	}

	public struct QuestRewards {
		public int XP;
		public List<string> items;
		public int gold;
	}
}

[System.Serializable]
public class QuestStep {
	public string goal, of;
	public int am, amC, e;
	public string dest;
	public List<QuestEvent> evnt;

    Coord revisedDestination;

    public Coord destination {
        get {
            return revisedDestination;
        }
        set {
            revisedDestination = new Coord(value.x, value.y);
        }
    }

	public QuestStep() {}

	public QuestStep(QuestStep other) {
	    goal = other.goal;
		of = other.of;
		am = other.am;
		amC = other.amC;
		dest = other.dest;
		e = other.e;
		evnt = new List<QuestEvent>(other.evnt);
	}
}

[System.Serializable]
public class QuestEvent {
	public QEventNames EventName;
	public List<SubEvent> SubEvents;

	public QuestEvent(QEventNames eName) {
		EventName = eName;
		SubEvents = new List<SubEvent>();
	}

	public QuestEvent(QEventNames name, List<SubEvent> sEvents) {
		EventName = name;
		SubEvents = sEvents;
	}

	[System.Serializable]
	public class SubEvent {
		public string Type;
		public string Name;
		public Coord LocalPos;
		public string WorldPos;
		public int Elevation;
		public string GiveItem;

		public SubEvent(string ty, string na, string wp, int ele = 0, string item = null, Coord lp = null) {
			Type = ty;
			Name = na;
			WorldPos = wp;
			LocalPos = lp;
			Elevation = ele;
			GiveItem = item;
		}
	}

	[System.Serializable]
	public enum QEventNames {
		OnStart, OnComplete, OnFail, OnStepComplete, OnStepStart
	}
}
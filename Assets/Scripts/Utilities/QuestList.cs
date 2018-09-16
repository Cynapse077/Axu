using UnityEngine;
using LitJson;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public static class QuestList {
	static List<Quest> quests;
	public static bool Initialized = false;
	public static string dataPath;

	public static void Init() {
		Initialized = false;
		quests = new List<Quest>();

		string jsonString = File.ReadAllText(Application.streamingAssetsPath + dataPath);

		if (string.IsNullOrEmpty(jsonString)) {
			Debug.LogError("Quest List null.");
			return;
		}

		JsonData data = JsonMapper.ToObject(jsonString);

		for (int i = 0; i < data["Quests"].Count; i++) {
			quests.Add(GetQuestFromData(data["Quests"][i]));
		}
			
		Initialized = true;
	}

	public static void AddQuest(Quest q) {
		quests.Add(q);
	}

	public static void RemoveQuest(string name) {
		if (quests.Find(x => x.Name == name) != null)
			quests.Remove(quests.Find(x => x.Name == name));
	}

	public static Quest GetQuestFromData(JsonData q) {
		Quest quest = new Quest(q["Name"].ToString(), q["ID"].ToString(), null);

		quest.Description = q["Description"].ToString();
		if (q.ContainsKey("Start Dialogue")) 
			quest.startDialogue = q["Start Dialogue"].ToString();
		if (q.ContainsKey("End Dialogue"))
			quest.endDialogue = q["End Dialogue"].ToString();

		//Rewards
		if (q.ContainsKey("Rewards")) {
			for (int r = 0; r < q["Rewards"].Count; r++) {
				if (q["Rewards"][r].ContainsKey("XP"))
					quest.rewards.XP += (int)q["Rewards"][r]["XP"];
				if (q["Rewards"][r].ContainsKey("Gold"))
					quest.rewards.gold += (int)q["Rewards"][r]["Gold"];
				if (q["Rewards"][r].ContainsKey("Item")) 
					quest.rewards.items.Add(q["Rewards"][r]["Item"].ToString());
			}	
		}

		if (q.ContainsKey("Type")) {
			string qType = q["Type"].ToString();
			quest.questType = qType.ToEnum<Quest.QuestType>();
		}

		List<QuestStep> questSteps = new List<QuestStep>();

		//Steps
		for (int s = 0; s < q["Steps"].Count; s++) {
			JsonData stepData = q["Steps"][s];
			QuestStep newStep = new QuestStep();
			newStep.goal = stepData["Goal"].ToString();

			if (stepData.ContainsKey("Coordinate")) {
				newStep.dest = stepData["Coordinate"].ToString();

				if (stepData.ContainsKey("Elevation"))
					newStep.e = (int)stepData["Elevation"];
			}

			if (stepData["Goal"].ToString() == "Fetch") {
				if (stepData.ContainsKey("ItemProperty"))
					newStep.of = stepData["ItemProperty"].ToString();
				else if (stepData.ContainsKey("Item"))
					newStep.of = stepData["Item"].ToString();
				newStep.am = (stepData.ContainsKey("Amount")) ? (int)stepData["Amount"] : 1;
			}

			if (stepData.ContainsKey("Faction"))
				newStep.of = stepData["Faction"].ToString();
			else if (stepData.ContainsKey("Name"))
				newStep.of = stepData["Name"].ToString();
			
			if (stepData.ContainsKey("Amount"))
				newStep.am = (int)stepData["Amount"];

			newStep.evnt = new List<QuestEvent>();
			if (stepData.ContainsKey("Events"))
				newStep.evnt = GetEvents(stepData);

			questSteps.Add(newStep);
		}

		quest.steps = questSteps;

		if (q.ContainsKey("Chained Quest"))
			quest.chainedQuestID = q["Chained Quest"].ToString();
		if (q.ContainsKey("Quest Giver"))
			quest.questGiver = World.objectManager.npcClasses.Find(x => x.ID == q["Quest Giver"].ToString());

		if (q.ContainsKey("Flag")) {
			string s = q["Flag"].ToString();
			ProgressFlags p = s.ToEnum<ProgressFlags>();
			quest.flag = p;
		} else
			quest.flag = ProgressFlags.None;

		if (q.ContainsKey("Follow")) 
			quest.followRules = q["Follow"].ToString();

		if (q.ContainsKey("Events"))
			quest.events = GetEvents(q);

		quests.Add(quest);

		return quest;
	}

	static List<QuestEvent> GetEvents(JsonData data) {
		List<QuestEvent> events = new List<QuestEvent>();

		for (int i = 0; i < data["Events"].Count; i++) {
			JsonData d = data["Events"][i];
			string eName = d["Event"].ToString();
			QuestEvent.QEventNames n = eName.ToEnum<QuestEvent.QEventNames>();
			QuestEvent newEvent = new QuestEvent(n);

			if (d.ContainsKey("Spawn")) {
				for (int j = 0; j < d["Spawn"].Count; j++) {
					bool isNPC = (d["Spawn"][j].ContainsKey("NPC"));
					string type = (isNPC) ? "NPC" : "Object";
					string name = d["Spawn"][j][type].ToString();
					string wPos = d["Spawn"][j]["Coordinate"].ToString();
					Coord lPos = null;

					if (d["Spawn"][j].ContainsKey("Local Position"))
						lPos = new Coord((int)d["Spawn"][j]["Local Position"][0], (int)d["Spawn"][j]["Local Position"][1]);
					
					int elev = (int)d["Spawn"][j]["Elevation"];

					string giveItem = "";

					if (d["Spawn"][j].ContainsKey("Give Item"))
						giveItem = d["Spawn"][j]["Give Item"].ToString();

					else if (d["Spawn"][j].ContainsKey("Set Quest Giver") && (bool)d["Spawn"][j]["Set Quest Giver"] == true)
						giveItem = "SETQGIVER";
					
					QuestEvent.SubEvent spawn = new QuestEvent.SubEvent(type, name, wPos, elev, giveItem, lPos);
					newEvent.SubEvents.Add(spawn);
				}
			}
			if (d.ContainsKey("Create Blocker")) {
				string wPos = d["Create Blocker"]["Coordinate"].ToString();
				Coord lPos = new Coord((int)d["Create Blocker"]["Local Position"][0], (int)d["Create Blocker"]["Local Position"][1]);
				int ele = (int)d["Create Blocker"]["Elevation"];

				QuestEvent.SubEvent cblock = new QuestEvent.SubEvent("Create Blocker", "", wPos, ele, null, lPos);
				newEvent.SubEvents.Add(cblock);
			}
			if (d.ContainsKey("SpawnGroup")) {
				for (int j = 0; j < d["SpawnGroup"].Count; j++) {
					string type = "SpawnGroup", name = d["SpawnGroup"][j]["Group"].ToString();
					int amount = (int)d["SpawnGroup"][j]["Amount"];
					string wPos = d["SpawnGroup"][j]["Coordinate"].ToString();

					QuestEvent.SubEvent spawnGroup = new QuestEvent.SubEvent(type, name, wPos, amount);
					newEvent.SubEvents.Add(spawnGroup);
				}
			}
			if (d.ContainsKey("Set Local Position")) {
				Coord lPos = new Coord((int)d["Set Local Position"][0], (int)d["Set Local Position"][1]);
				QuestEvent.SubEvent setPos = new QuestEvent.SubEvent("LocalPos", "", null, 0, null, lPos);
				newEvent.SubEvents.Add(setPos);
			}
			if (d.ContainsKey("Set World Position")) {
				string wPos = d["Set World Position"]["Coordinate"].ToString();
				int ele = (int)d["Set World Position"]["Elevation"];
				QuestEvent.SubEvent setPos = new QuestEvent.SubEvent("WorldPos", "", wPos, ele, null, null);
				newEvent.SubEvents.Add(setPos);
			}
			if (d.ContainsKey("Set Elevation")) {
				int ele = (int)d["Set Elevation"];
				QuestEvent.SubEvent setEle = new QuestEvent.SubEvent("Set Elevation", "", null, ele);
				newEvent.SubEvents.Add(setEle);
			}
			if (d.ContainsKey("Give Quest")) {
				string questName = d["Give Quest"]["Quest"].ToString();
				string npcToReceiveQuest = d["Give Quest"]["NPC"].ToString();
				QuestEvent.SubEvent giveQuest = new QuestEvent.SubEvent("Give Quest", questName, null, 0, npcToReceiveQuest, null);
				newEvent.SubEvents.Add(giveQuest);
			}
			if (d.ContainsKey("Move NPC")) {
				string npcToMove = d["Move NPC"]["NPC"].ToString();
				string wPos = d["Move NPC"]["Coordinate"].ToString();
				Coord localPos = new Coord((int)d["Move NPC"]["Local Position"][0], (int)d["Move NPC"]["Local Position"][1]);
				int elevation = (int)d["Move NPC"]["Elevation"];
				QuestEvent.SubEvent moveNPC = new QuestEvent.SubEvent("Move NPC", npcToMove, wPos, elevation, null, localPos);
				newEvent.SubEvents.Add(moveNPC);
			}
			if (d.ContainsKey("Mod Item")) {
				string item = d["Mod Item"]["Item"].ToString();
				string mod = d["Mod Item"]["Mod"].ToString();
				QuestEvent.SubEvent modItem = new QuestEvent.SubEvent("Mod Item", item, null, 0, mod);
				newEvent.SubEvents.Add(modItem);
			}
			if (d.ContainsKey("Console Command")) {
				string consoleCommand = d["Console Command"].ToString();
				QuestEvent.SubEvent consComm = new QuestEvent.SubEvent("Console Command", consoleCommand, null);
				newEvent.SubEvents.Add(consComm);
			}
			if (d.ContainsKey("Run Lua")) {
				string fileName = d["Run Lua"]["File"].ToString();
				string function = d["Run Lua"]["Function"].ToString();
				QuestEvent.SubEvent rlua = new QuestEvent.SubEvent("Run Lua", fileName, null, 0, function);
				newEvent.SubEvents.Add(rlua);
			}
			if (d.ContainsKey("Remove Item")) {
				string itemToRemove = d["Remove Item"]["Item"].ToString();
				string replacement = d["Remove Item"]["Replacement"].ToString();
				QuestEvent.SubEvent ritem = new QuestEvent.SubEvent("Remove Item", itemToRemove, null, 0, replacement);
				newEvent.SubEvents.Add(ritem);
			}
			if (d.ContainsKey("Remove Blockers")) {
				string wPos = d["Remove Blockers"]["Coordinate"].ToString();
				Coord localPos = d["Remove Blockers"].ContainsKey("Local Position") ? new Coord((int)d["Remove Blockers"]["Local Position"][0], (int)d["Remove Blockers"]["Local Position"][1]) : null;
				int elevation = (int)d["Remove Blockers"]["Elevation"];
				QuestEvent.SubEvent rbl = new QuestEvent.SubEvent("Remove Blockers", "", wPos, elevation, null, localPos);
				newEvent.SubEvents.Add(rbl);
			}

			events.Add(newEvent);
		}

		return events;
	}

	public static Quest GetByID(string search) {
		if (quests == null)
			Init();
		if (quests.Find(x => x.ID == search) == null)
			return null;

		return new Quest(quests.Find(x => x.ID == search));
	}

	public static Quest GetRandomQuest() {
		if (quests == null)
			Init();
		
		List<Quest> randomQuests = quests.FindAll(x => x.questType == Quest.QuestType.Random);
		foreach (Quest q in quests) {
			if (ObjectManager.playerJournal != null && ObjectManager.playerJournal.quests.Find(x => x.ID == q.ID) != null)
				randomQuests.Remove(q);
			
		}

		return new Quest(randomQuests.GetRandom());
	}

	public static string GetRandomDailyHuntID() {
		return quests.FindAll(x => x.questType == Quest.QuestType.Daily && x.ID.Contains("hunt_daily")).GetRandom().ID;
	}

	public static string GetRandomDailyArenaID() {
		return quests.FindAll(x => x.questType == Quest.QuestType.Daily && x.ID.Contains("arena_daily")).GetRandom().ID;
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class Journal : MonoBehaviour {

	public List<Quest> quests;
	List<ProgressFlags> progressFlags;
	Quest _trackedQuest;

	public Quest trackedQuest {
		get { return _trackedQuest; }
		set { _trackedQuest = value; }
	}

	void Start() {
		quests = new List<Quest>();
		progressFlags = new List<ProgressFlags>();
		World.tileMap.OnScreenChange += UpdateLocation;
		trackedQuest = null;

		if (!Manager.newGame) {
			foreach (Quest q in Manager.playerBuilder.quests) {
				if (quests.Find(x => x.ID == q.ID) != null)
					continue;

				StartQuest(q, false, true);
				Quest otherQuest = QuestList.GetByID(q.ID);
				List<QuestEvent> events = otherQuest.events;

				for (int e = 0; e < events.Count; e++) {
					q.events.Add(events[e]);
				}
			}

			for (int i = 0; i < Manager.playerBuilder.progressFlags.Count; i++) {
				AddFlag(Manager.playerBuilder.progressFlags[i]);
			}
		}
	}

	public void OnDisable() {
		World.tileMap.OnScreenChange -= UpdateLocation;
	}

	public List<ProgressFlags> AllFlags() {
		return new List<ProgressFlags>(progressFlags);
	}

	public bool HasFlag(ProgressFlags fl) {
		return progressFlags.Contains(fl);
	}

	public void AddFlag(ProgressFlags fl) {
		progressFlags.Add(fl);

		if (fl == ProgressFlags.Hostile_To_Ensis)
			MakeHostile("ensis");
		else if (fl == ProgressFlags.Hostile_To_Kin)
			MakeHostile("kin");
		else if (fl == ProgressFlags.Hostile_To_Oromir)
			MakeHostile("magna");
	}

	void MakeHostile(string facID) {
		Faction f = FactionList.GetFactionByID(facID);

		if (f == null)
			return;

		if (!f.hostileTo.Contains("player"))
			f.hostileTo.Add("player");
		if (!f.hostileTo.Contains("followers"))
			f.hostileTo.Add("followers");

		BreakFollowersInFaction(facID);
	}

	void BreakFollowersInFaction(string facID) {
		foreach (NPC n in World.objectManager.npcClasses) {
			if (n.HasFlag(NPC_Flags.Follower)) {
				string factionID = EntityList.GetBlueprintByID(n.ID).faction.ID;

				if (factionID == facID) {
					Coord empty = new Coord(0, 0);
					NPC template = EntityList.GetNPCByID(n.ID, empty, empty);
					n.faction = template.faction;
					n.flags = template.flags;
				}
			}
		}
	}

	public void StartQuest(Quest q, bool showInLog = true, bool loading = false) {
		if (q == null || (q.questType == Quest.QuestType.Main || q.questType == Quest.QuestType.Side) && quests.Find(x => x.ID == q.ID) != null)
			return;

		q.PlaceNameInDescription();
		q.ReplaceNameInDialogue();

		quests.Add(q);

		if (showInLog)
			CombatLog.NameMessage("Start_Quest", q.Name);	
		
		q.StartQuest();
		trackedQuest = q;
	}

	public void CompleteQuest(Quest q) {
		if (!quests.Contains(q))
			return;
		
		if (q == trackedQuest) {
			if (quests.Count > 1)
				trackedQuest = (quests[0] == q) ? quests[1] : quests[0];
			else
				trackedQuest = null;
		}

		q.Complete();
		quests.Remove(q);
	}

	//Called when an NPC dies, checks all quests to see if their faction or name was in the steps. If so, add one to the amount
	public void OnNPCDeath_CheckQuestProgress(NPC n) {
		foreach (Quest q in quests)
        {
            q.NPCDied(n);
        }
	}

	//Called when the local map changes, checks all quests to see if there is a destination here.
	bool UpdateLocation(TileMap_Data oldMap, TileMap_Data newMap) {
		StartCoroutine(OnVisitArea_CheckQuestProgress(newMap));
		return true;
	}
	IEnumerator OnVisitArea_CheckQuestProgress(TileMap_Data newMap) {
		if (newMap.elevation == 0 && newMap.mapInfo.landmark == "Home") {
			//Found home base
			if (!progressFlags.Contains(ProgressFlags.Found_Base)) {
                Alert.NewAlert("Found_Base");
                progressFlags.Add(ProgressFlags.Found_Base);
                //Spawn chest with return tome in it.
                MapObject moj = new MapObject("Chest", new Coord(Manager.localMapSize.x / 2, Manager.localMapSize.y - 8), newMap.mapInfo.position, newMap.elevation)
                {
                    inv = new List<Item>() { ItemList.GetItemByID("tome_return") }
                };

                World.objectManager.SpawnObject(moj);
			}
		}

		if (quests != null && quests.Count > 0) {
			yield return new WaitForSeconds(0.1f);

			for (int i = 0; i < quests.Count; i++) {
				if (quests[i].steps.Count > 0 && quests[i].NextStep.goal == "Go") {
					if (newMap.mapInfo.position == quests[i].destination && newMap.elevation == Mathf.Abs(quests[i].NextStep.e))
						quests[i].CompleteStep(quests[i].NextStep);
				}
			}
		}
	}

	public bool OnSeen_CheckQuestProgress() {
		bool hasQuest = false;

		for (int i = 0; i < quests.Count; i++) {
			if (quests[i].steps.Count > 0 && quests[i].NextStep.goal == "View Altar") {
				quests[i].NextStep.amC ++;
				quests[i].TryComplete();
				hasQuest = true;
			}
		}

		return hasQuest;
	}

	public bool OnActivateTerminal_CheckQuestProgress() {
		bool hasQuest = false;

		for (int i = 0; i < quests.Count; i++) {
			if (quests[i].steps.Count > 0 && quests[i].NextStep.goal == "Use Terminal") {
				quests[i].NextStep.amC ++;
				quests[i].TryComplete();
				hasQuest = true;
			}
		}

		return hasQuest;
	}

	public bool OnDestroyShrine_CheckQuestProgress() {
		bool hasQuest = false;

		for (int i = 0; i < quests.Count; i++) {
			if (quests[i].steps.Count > 0 && quests[i].NextStep.goal == "Destroy Shrine") {
				quests[i].NextStep.amC ++;

				if (quests[i].NextStep.amC >= quests[i].NextStep.am)
					quests[i].CompleteStep(quests[i].NextStep);

				hasQuest = true;
			}
		}

		return hasQuest;
	}
}

public enum ProgressFlags {
	None,
	Can_Enter_Power_Plant, Can_Enter_Ensis, Can_Open_Prison_Cells, Can_Enter_Magna, Can_Enter_Fab,
	Hostile_To_Kin, Hostile_To_Ensis, Hostile_To_Oromir,
	Hunts_Available, Arena_Available,
	Found_Base, Learned_Butcher
}
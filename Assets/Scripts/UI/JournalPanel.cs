using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class JournalPanel : MonoBehaviour {

	bool initialized;
	Journal journal;
	public Transform journalBase;

	public GameObject questObject;
	public Text tooltip;
	public Text ttTitle;
	public GameObject tooltipGO;

	public void Init() {
		if (journal == null)
			journal = ObjectManager.player.GetComponent<Journal>();

		tooltipGO.SetActive(false);
		
		initialized = true;
		tooltip.text = "";
		OpenJournal();
	}

	public void OpenJournal() {
		journalBase.DestroyChildren();

		for (int i = 0; i < journal.quests.Count; i++) {
			GameObject g = (GameObject)Instantiate(questObject, journalBase);
			g.GetComponentInChildren<Text>().text = (journal.quests[i].ID == journal.trackedQuest.ID) 
				? "<color=magenta>></color> " + journal.quests[i].Name + " <color=magenta><</color>" : journal.quests[i].Name;
			g.GetComponent<Button>().onClick.AddListener(() => World.userInterface.SelectPressed());
		}

		UpdateTooltip();
	}

	public void UpdateTooltip() {
		if (journalBase.childCount > 0 && journalBase.childCount > UserInterface.selectedItemNum) {
			tooltipGO.SetActive(true);
			Quest q = journal.quests[UserInterface.selectedItemNum];

			if (q == null) {
				tooltip.text = "";
				return;
			}

			string desc = "<i>" + q.Description + "</i>";
			string goal = "";

			if (journal.trackedQuest == q) {
				desc = "<color=magenta>> Tracked <</color>\n\n" + desc;
			}

			if (q.steps.Count > 0) {
				QuestStep step = q.NextStep;

				if (step.am > 0) {
					goal = ("<b><color=orange>(" + step.amC + " / " + step.am + ")</color></b>");
				} else {
					Coord dest = q.destination, curr = World.tileMap.WorldPosition;
					int currElev = World.tileMap.currentElevation, toElev = q.NextStep.e;
					string toDestination = Direction.Heading(curr, dest, currElev, toElev);

					if (step.goal == "Go")
						goal = ("<color=lightblue>  To Destination: ") + toDestination + "</color>";
					else if (step.goal == "Return")
						goal = "<color=orange>Return to " + q.questGiver.name + ".</color>";
				}
			}

			ttTitle.text = q.Name;
			tooltip.text = desc + "\n\n" + goal;
		} else {
			tooltipGO.SetActive(false);
		}
	}

	void Update() {
		if (!initialized || World.userInterface.CurrentState() != UIWindow.Journal)
			return;

		if (journalBase.childCount > 0 && journalBase.childCount > UserInterface.selectedItemNum)
			EventSystem.current.SetSelectedGameObject(journalBase.GetChild(UserInterface.selectedItemNum).gameObject);
	}
}

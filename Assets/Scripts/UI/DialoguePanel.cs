using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialoguePanel : MonoBehaviour {

	public GameObject title;
	public GameObject text;
	public GameObject button;

	[HideInInspector]
	public string chatText = "";
	[HideInInspector]
	public int cMax;

	DialogueController controller;
	Text titleText;
	Text dialogueText;

	public void Display(DialogueController dc) {
		transform.DestroyChildren();

		controller = dc;
		cMax = dc.dialogueChoices.Count - 1;

		GameObject t = (GameObject)Instantiate(title, transform);
		titleText = t.GetComponentInChildren<Text>();
		titleText.text = controller.gameObject.name;

		GameObject txt = (GameObject)Instantiate(text, transform);
		dialogueText = txt.GetComponentInChildren<Text>() ;
		dialogueText.text = Dialogue.Greeting(dc.GetComponent<BaseAI>().npcBase.faction);

		for (int i = 0; i < controller.dialogueChoices.Count; i++) {
			GameObject g = (GameObject)Instantiate(button, transform);
			Text gText = g.GetComponentInChildren<Text>();
			gText.text = controller.dialogueChoices[i].text;

			if (controller.dialogueChoices[i].text == "Heal Wounds") {
				Stats playerStats = ObjectManager.player.GetComponent<Stats>();
				gText.text = GetVariableName(gText.text, playerStats.CostToCureWounds() == 0, playerStats.CostToCureWounds());
			} else if (controller.dialogueChoices[i].text == "Replace Limb") {
				Stats playerStats = ObjectManager.player.GetComponent<Stats>();
				gText.text = GetVariableName(gText.text, playerStats.CostToReplaceLimbs() < 0, playerStats.CostToReplaceLimbs());
			}
			g.GetComponent<Button>().onClick.AddListener(() => { World.userInterface.SelectPressed(); } );
			g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(2, UIWindow.Dialogue);
		}

		EventSystem.current.SetSelectedGameObject(transform.GetChild(2).gameObject);
	}

	void Update() {
		if (World.userInterface.CurrentState() == UIWindow.Dialogue)
			EventSystem.current.SetSelectedGameObject(transform.GetChild(UserInterface.selectedItemNum + 2).gameObject);
	}

	public void SetText(string t) {
		dialogueText.text = t;
	}

	public void ChooseDialogue() {
		controller.dialogueChoices[UserInterface.selectedItemNum].callBack();
	}

	string GetVariableName(string input, bool notNeeded, int amount) {
		return (notNeeded) ? (input + "<color=grey>(Not Needed)</color>") : (input + " - <color=yellow>$</color>" + amount);
	}
}

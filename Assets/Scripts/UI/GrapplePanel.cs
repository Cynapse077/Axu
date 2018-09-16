using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GrapplePanel : MonoBehaviour {

	public Transform anchor;
	public GameObject abilityIcon;

	int max = 0;
	int selectedNum = 0;
	List<KeyValuePair<string, int>> actions;
	Body body;

	public void Initialize(Body bod) {
		body = bod;
		anchor.DestroyChildren();
		actions = new List<KeyValuePair<string, int>>();
		max = 0;

		string n = LocalizationManager.GetLocalizedContent("Grapple_Grab")[0];

		if (body.AllGrips().Count <= 0) {
			//GRAB
			GameObject grabGO = (GameObject)Instantiate(abilityIcon, anchor);
			grabGO.GetComponentInChildren<Text>().text = n;
			grabGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
			actions.Add(new KeyValuePair<string, int>("Grab", -1));

			max ++;
		}

		selectedNum = 0;
		int skill = body.GetComponent<EntitySkills>().abilities.Find(x => x.ID == "grapple").level;
			
		for (int i = 0; i < body.bodyParts.Count; i++) {
			if (body.bodyParts[i].grip != null && body.bodyParts[i].grip.HeldPart != null) {

				//PUSH
				GameObject pushGO = (GameObject)Instantiate(abilityIcon, anchor);
				n = LocalizationManager.GetLocalizedContent("Grapple_Push")[0];
				n = n.Replace("[NAME]", body.bodyParts[i].grip.HeldPart.myBody.gameObject.name);
				pushGO.GetComponentInChildren<Text>().text =  n;
				pushGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
				actions.Add(new KeyValuePair<string, int>("Push", i));

				max++;

				//TAKE DOWN
				GameObject takeDownGO = (GameObject)Instantiate(abilityIcon, anchor);
				n = LocalizationManager.GetLocalizedContent("Grapple_TakeDown")[0];
				n = n.Replace("[NAME]", body.bodyParts[i].grip.HeldPart.myBody.gameObject.name);
				takeDownGO.GetComponentInChildren<Text>().text = n;
				takeDownGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
				actions.Add(new KeyValuePair<string, int>("Take Down", i));

				max ++;

				//STRANGLE
				if (skill > 1 && body.bodyParts[i].grip.HeldPart.slot == ItemProperty.Slot_Head) {
					GameObject strangleGO = (GameObject)Instantiate(abilityIcon, anchor);
					n = LocalizationManager.GetLocalizedContent("Grapple_Strangle")[0];
					n = n.Replace("[NAME]", body.bodyParts[i].grip.HeldPart.myBody.gameObject.name);
					strangleGO.GetComponentInChildren<Text>().text = n;
					strangleGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
					actions.Add(new KeyValuePair<string, int>("Strangle", i));

					max ++;
				}

				//YANK OFF LIMBS
				if (body.entity.stats.Strength >= 8 && skill > 3) {
					GameObject pullGO = (GameObject)Instantiate(abilityIcon, anchor);
					n = LocalizationManager.GetLocalizedContent("Grapple_Pull")[0];
					n = n.Replace("[NAME]", body.bodyParts[i].grip.HeldPart.displayName);
					pullGO.GetComponentInChildren<Text>().text = n;
					pullGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
					actions.Add(new KeyValuePair<string, int>("Pull", i));

					max ++;
				}

				//RELEASE GRIP
				GameObject releaseGO = (GameObject)Instantiate(abilityIcon, anchor);
				n = LocalizationManager.GetLocalizedContent("Grapple_Release")[0];
				n = n.Replace("[NAME]", body.bodyParts[i].grip.HeldPart.myBody.gameObject.name);
				releaseGO.GetComponentInChildren<Text>().text = n;
				releaseGO.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
				actions.Add(new KeyValuePair<string, int>("Release", i));

				max++;
			}
		}
	}

	void SelectPressed() {
		if (actions[selectedNum].Key == "Grab") {
			World.userInterface.CloseWindows();
			PlayerInput pi = ObjectManager.player.GetComponent<PlayerInput>();
			pi.grabbing = true;
			pi.ChangeCursorMode(PlayerInput.CursorMode.Direction);
		} else {
			PerformAction();
			ObjectManager.playerEntity.EndTurn(0.01f, 10);
		}
	}

	void PerformAction() {
		EntitySkills skills = ObjectManager.player.GetComponent<EntitySkills>();
        Skill grappleSkill = skills.abilities.Find(x => x.ID == "grapple");
		BodyPart targetLimb = body.bodyParts[actions[selectedNum].Value].grip.HeldPart;

		if (actions[selectedNum].Key == "Push") {
			Entity target = targetLimb.myBody.entity;
			skills.Grapple_Shove(target, grappleSkill);

		} else if (actions[selectedNum].Key == "Take Down") {
			Stats target = targetLimb.myBody.entity.stats;
			skills.Grapple_TakeDown(target, targetLimb.displayName, grappleSkill);

		} else if (actions[selectedNum].Key == "Strangle") {
			Stats target = targetLimb.myBody.entity.stats;
			skills.Grapple_Strangle(target, grappleSkill);

		} else if (actions[selectedNum].Key == "Pull") {
			skills.Grapple_Pull(body.bodyParts[actions[selectedNum].Value].grip, skills.abilities.Find(x => x.ID == "grapple"));

		} else if (actions[selectedNum].Key == "Release") {
			CombatLog.CombatMessage("Gr_ReleaseGrip", ObjectManager.player.name, targetLimb.myBody.gameObject.name, false);
			body.bodyParts[actions[selectedNum].Value].grip.Release();
		}

		World.userInterface.CloseWindows();
	}

	public void Update() {
		if (GameSettings.Keybindings.GetKey("North"))
			selectedNum --;
		else if (GameSettings.Keybindings.GetKey("South"))
			selectedNum++;

		if (selectedNum < 0)
			selectedNum = max - 1;
		else if (selectedNum >= max)
			selectedNum = 0;

		EventSystem.current.SetSelectedGameObject(anchor.GetChild(selectedNum).gameObject);

		if (GameSettings.Keybindings.GetKey("Enter"))
			SelectPressed();
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LevelUpPanel : MonoBehaviour {

	public GameObject selectButton;
	public Transform anchor;

	LevelUpButton[] selections;

	public void Init(List<Trait> levelUpTraits) {
		anchor.DestroyChildren();

		selections = new LevelUpButton[levelUpTraits.Count + 1];

		for (int i = 0; i < levelUpTraits.Count; i++) {
			GameObject g = (GameObject)Instantiate(selectButton, anchor);
			g.GetComponent<Button>().onClick.AddListener(() => { World.userInterface.SelectPressed(); } );
			selections[i] = g.GetComponent<LevelUpButton>();
			selections[i].SetValues(levelUpTraits[i]);
		}

		GameObject noneObject = (GameObject)Instantiate(selectButton, anchor);
		noneObject.GetComponent<Button>().onClick.AddListener(() => { World.userInterface.SelectPressed(); } );
		selections[selections.Length - 1] = noneObject.GetComponent<LevelUpButton>();
		selections[selections.Length - 1].SetValues(TraitList.GetTraitByID("none"));
	}

	void Update() {
		if (selections != null && UserInterface.selectedItemNum < selections.Length)
			EventSystem.current.SetSelectedGameObject(anchor.GetChild(UserInterface.selectedItemNum).gameObject);
	}
}

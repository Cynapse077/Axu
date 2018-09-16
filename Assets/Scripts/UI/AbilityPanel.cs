using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AbilityPanel : MonoBehaviour {

	[Header("Prefabs")]
	public GameObject abilityButton;
	[Header("Children")]
	public Transform abilityBase;
	public Scrollbar scrollBar;
	public AbilityTooltip tooltip;

	EntitySkills skills;

	public void Init() {
		skills = ObjectManager.player.GetComponent<EntitySkills>();
		UpdateAbilities();
	}

	void UpdateAbilities() {
		abilityBase.DestroyChildren();

		for (int i = 0; i < skills.abilities.Count; i++) {
			GameObject g = (GameObject)Instantiate(abilityButton, abilityBase);
			g.GetComponent<AbilityButton>().Setup(skills.abilities[i], i);
			g.GetComponent<Button>().onClick.AddListener(() => { World.userInterface.SelectPressed(g.transform.GetSiblingIndex()); } );
		}

		UpdateTooltip();
	}

	public void UpdateTooltip() {
		if (abilityBase.childCount <= 0) {
			tooltip.gameObject.SetActive(false);
			return;
		}

		tooltip.gameObject.SetActive(true);

		tooltip.UpdateTooltip(skills.abilities[UserInterface.selectedItemNum]);
	}

	void Update() {
		if (abilityBase.childCount > 0  && abilityBase.childCount > UserInterface.selectedItemNum)
			EventSystem.current.SetSelectedGameObject(abilityBase.GetChild(UserInterface.selectedItemNum).gameObject);
		
		if (skills.abilities.Count > 0) {
			scrollBar.value = 1f - ((float)UserInterface.selectedItemNum / (float)skills.abilities.Count);

			if (GameSettings.Keybindings.GetKey("GoUpStairs") && UserInterface.selectedItemNum < skills.abilities.Count - 1) {
				skills.abilities.Move(UserInterface.selectedItemNum, UserInterface.selectedItemNum + 1);
				UserInterface.selectedItemNum ++;
				UpdateAbilities();
			} else if (GameSettings.Keybindings.GetKey("GoDownStairs") && UserInterface.selectedItemNum > 0) {
				skills.abilities.Move(UserInterface.selectedItemNum, UserInterface.selectedItemNum - 1);
				UserInterface.selectedItemNum --;
				UpdateAbilities();
			}
		}
	}
}

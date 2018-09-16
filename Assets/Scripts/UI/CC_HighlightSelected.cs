using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CC_HighlightSelected : MonoBehaviour, IPointerEnterHandler {

	CharacterCreation cc;

	void Start() {
		cc = GameObject.FindObjectOfType<CharacterCreation>();
	}

	public void OnPointerEnter(PointerEventData ped) {
		if (!cc.YNOpen)
			cc.SetSelectedNumber(transform.GetSiblingIndex());
	}
}

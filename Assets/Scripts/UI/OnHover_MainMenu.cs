using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OnHover_MainMenu : MonoBehaviour, IPointerEnterHandler {

	public int index;
	MainMenu mm;

	void Start() {
		mm = GameObject.FindObjectOfType<MainMenu>();
	}

	public void OnPointerEnter(PointerEventData eventData) {
		mm.SetSelectedNum(index);
	}

	void Update() {
		if (mm.currentSelected == index)
			EventSystem.current.SetSelectedGameObject(gameObject);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OnHover_ShowTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	public Transform tooltip;
	public string textToDisplay;

	void OnDisable() {
		tooltip.gameObject.SetActive(false);
	}

	public void OnPointerEnter(PointerEventData ev) {
		if (string.IsNullOrEmpty(textToDisplay)) {
			tooltip.gameObject.SetActive(false);
			return;
		}

		tooltip.gameObject.SetActive(true);
		tooltip.position = Input.mousePosition;
		tooltip.GetComponentInChildren<Text>().text = textToDisplay;
	} 

	public void OnPointerExit(PointerEventData ev) {
		tooltip.gameObject.SetActive(false);
		tooltip.GetComponentInChildren<Text>().text = "";
	}
}

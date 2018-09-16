using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITooltip : MonoBehaviour {

	RectTransform rectTransform;

	void OnEnable() {
		if (rectTransform == null)
			rectTransform = GetComponent<RectTransform>();

		rectTransform.pivot = new Vector2((Input.mousePosition.x > Screen.width * 0.5f ? 1 : 0 ), 1) ;
	}
}

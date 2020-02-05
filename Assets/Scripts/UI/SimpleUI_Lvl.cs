using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleUI_Lvl : MonoBehaviour {

	Text lvlText;

	void Start() {
		lvlText = GetComponent<Text>();
	}

	void Update() {
		if (ObjectManager.playerEntity != null && ObjectManager.playerEntity.stats.level != null)
			lvlText.text = ObjectManager.playerEntity.stats.level.CurrentLevel.ToString();
	}
}

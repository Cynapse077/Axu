﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Options_KeyPanel : MonoBehaviour {

	public GameObject keyPrefab;

	void Start() {
		Redo();
	}

	public void Redo() {
		transform.DestroyChildren();


		foreach (KeyValuePair<string, ShiftKeyCode> kvp in GameSettings.Keybindings.MyKeys) {
			GameObject g = (GameObject)Instantiate(keyPrefab, transform);
			RebindButton rb = g.GetComponent<RebindButton>();
			string buttonName = (kvp.Value.Shift) ? "Shift + " + kvp.Value.keyCode.ToString() : kvp.Value.keyCode.ToString();

			if (buttonName == "None")
				buttonName = "<color=grey>----</color>";

			rb.SetTexts(kvp.Key, buttonName);
			rb.button.onClick.AddListener(() => {
				string n = kvp.Key.Replace(" ", string.Empty);
				SelectedKeyToChange(n);
			});
		}
	}

	void OnGUI() {
		if (WaitingForRebindingInput)
			KeyReplacement();
	}

	void KeyReplacement() {
		Event e = Event.current;

		if (e != null && e.isKey && Event.current.type != EventType.Layout) {
			KeyCode pauseKey = GameSettings.Keybindings.GetKeyCode("Pause");
			KeyCode keyCode = e.keyCode;

			if (!CannotAssign(e.keyCode)) {
				if (e.keyCode == pauseKey)
					WaitingForRebindingInput = false;
				else if (e.keyCode != GameSettings.Keybindings.MyKeys[KeyToChangeName].keyCode && GameSettings.Keybindings.KeyAlreadyUsed(e.keyCode)) {
					Options_KeyPanel.ReplaceKeybind(GameSettings.Keybindings.SearchKey(e.keyCode), KeyCode.None);
				} else
					Options_KeyPanel.ReplaceKeybind(KeyToChangeName, keyCode);

				Redo();
				WaitingForRebindingInput = false;
			}
		}
	}

	bool CannotAssign(KeyCode kc) {
		return (kc == KeyCode.LeftShift || kc == KeyCode.RightShift || kc == KeyCode.KeypadEnter 
			|| kc == KeyCode.UpArrow || kc == KeyCode.DownArrow || kc == KeyCode.RightArrow || kc == KeyCode.LeftArrow);
	}

	public static bool WaitingForRebindingInput = false;
	static string KeyToChangeName = null;
	public static void SelectedKeyToChange(string name) {
		WaitingForRebindingInput = true;
		KeyToChangeName = name;
	}
	public static void ReplaceKeybind(string name, KeyCode key) {
		bool shiftHeld = GameSettings.Keybindings.ShiftHeld();
		ShiftKeyCode skc = GameSettings.Keybindings.MyKeys[name];
		GameSettings.Keybindings.MyKeys[name].keyCode = (key == skc.keyCode && shiftHeld == skc.Shift) ? KeyCode.None : key;

		if (GameSettings.Keybindings.MyKeys[name].keyCode != KeyCode.None)
			GameSettings.Keybindings.MyKeys[name].Shift = shiftHeld;
	}
}

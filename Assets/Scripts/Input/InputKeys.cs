using UnityEngine;
using LitJson;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

[System.Serializable]
public class InputKeys {
	public Dictionary<string, ShiftKeyCode> MyKeys;

	public InputKeys(JsonData data = null) {
		if (data == null)
			Defaults();
		else
			Load(data);
	}
    
	void Load(JsonData data) {
		Defaults();
		List<KeyValuePair<string, ShiftKeyCode>> keyPairList = new List<KeyValuePair<string, ShiftKeyCode>>(MyKeys);

		foreach (KeyValuePair<string, ShiftKeyCode> kvp in keyPairList) {
			MyKeys[kvp.Key] = GetValueFromData(data, kvp.Key);
		}
	}

	ShiftKeyCode GetValueFromData(JsonData data, string keyName) {
		bool sh = (data["Input"]["MyKeys"][keyName].ContainsKey("Shift")) ? (bool)data["Input"]["MyKeys"][keyName]["Shift"] : false;
		int kc = (int)data["Input"]["MyKeys"][keyName]["keyCode"];

		return new ShiftKeyCode(sh,(KeyCode)kc);
	}

	public KeyCode GetKeyCode(string search) {
		return MyKeys[search].keyCode;
	}

	public bool ShiftHeld() {
		return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
	}

	public bool GetKey(string search, KeyPress press = KeyPress.Down) {
		ShiftKeyCode kc1 = MyKeys[search];

		if (press == KeyPress.Held)
			return (Input.GetKey(kc1.keyCode) && ShiftHeld() == kc1.Shift);
		
		else if (press == KeyPress.Down) {
			if (search == "Enter")
				return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift || Input.GetKeyDown(KeyCode.KeypadEnter));
			if (search == "North")
				return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift || Input.GetKeyDown(KeyCode.UpArrow));
			if (search == "South")
				return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift || Input.GetKeyDown(KeyCode.DownArrow));
			if (search == "East")
				return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift || Input.GetKeyDown(KeyCode.RightArrow));
			if (search == "West")
				return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift || Input.GetKeyDown(KeyCode.LeftArrow));
			
			return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift);

		} else if (press == KeyPress.Up)
			return (Input.GetKeyUp(kc1.keyCode) && ShiftHeld() == kc1.Shift);
		
		return false;
	}

	public bool KeyAlreadyUsed(KeyCode search) {
		foreach (ShiftKeyCode keys in MyKeys.Values) {
			if (keys.keyCode == search && ShiftHeld() == keys.Shift)
				return true;
		}

		return false;
	}

	public string SearchKey(KeyCode search) {
		return MyKeys.First(x => (x.Value.keyCode == search)).Key;
	}

	public void Defaults() {
		MyKeys = new Dictionary<string, ShiftKeyCode>();

		MyKeys.Add("North", new ShiftKeyCode(false, KeyCode.Keypad8));
		MyKeys.Add("NorthEast", new ShiftKeyCode(false, KeyCode.Keypad9));
		MyKeys.Add("East", new ShiftKeyCode(false, KeyCode.Keypad6));
		MyKeys.Add("SouthEast", new ShiftKeyCode(false, KeyCode.Keypad3));
		MyKeys.Add("South", new ShiftKeyCode(false, KeyCode.Keypad2));
		MyKeys.Add("SouthWest", new ShiftKeyCode(false, KeyCode.Keypad1));
		MyKeys.Add("West", new ShiftKeyCode(false, KeyCode.Keypad4));
		MyKeys.Add("NorthWest", new ShiftKeyCode(false, KeyCode.Keypad7));
		MyKeys.Add("Wait", new ShiftKeyCode(false, KeyCode.Keypad5));

		MyKeys.Add("GoUpStairs", new ShiftKeyCode(false, KeyCode.KeypadPlus));
		MyKeys.Add("GoDownStairs", new ShiftKeyCode(false, KeyCode.KeypadMinus));

		MyKeys.Add("Enter", new ShiftKeyCode(false, KeyCode.Return));
		MyKeys.Add("Pause", new ShiftKeyCode(false, KeyCode.Escape));

		MyKeys.Add("Pickup", new ShiftKeyCode(false, KeyCode.G));
		MyKeys.Add("Interact", new ShiftKeyCode(false, KeyCode.Space));
        MyKeys.Add("Contextual Actions", new ShiftKeyCode(true, KeyCode.Space));
		MyKeys.Add("Look", new ShiftKeyCode(false, KeyCode.L));
        MyKeys.Add("Switch Target", new ShiftKeyCode(false, KeyCode.Tab));
        MyKeys.Add("ForceAttack", new ShiftKeyCode(false, KeyCode.LeftControl));
		MyKeys.Add("Walk", new ShiftKeyCode(false, KeyCode.W));
		MyKeys.Add("Throw", new ShiftKeyCode(false, KeyCode.T));
		MyKeys.Add("Reload", new ShiftKeyCode(false, KeyCode.R));
		MyKeys.Add("Rest", new ShiftKeyCode(false, KeyCode.E));
		MyKeys.Add("Fire", new ShiftKeyCode(false, KeyCode.F));

		MyKeys.Add("Inventory", new ShiftKeyCode(false, KeyCode.I));
		MyKeys.Add("Character", new ShiftKeyCode(false, KeyCode.C));
		MyKeys.Add("Abilities", new ShiftKeyCode(false, KeyCode.A));
		MyKeys.Add("Map", new ShiftKeyCode(false, KeyCode.M));
		MyKeys.Add("Journal", new ShiftKeyCode(false, KeyCode.J));
		MyKeys.Add("Toggle Mouse", new ShiftKeyCode(false, KeyCode.None));
	}

	public void VIKeys() {
		MyKeys = new Dictionary<string, ShiftKeyCode>();

		MyKeys.Add("North", new ShiftKeyCode(false, KeyCode.K));
		MyKeys.Add("NorthEast", new ShiftKeyCode(false, KeyCode.U));
		MyKeys.Add("East", new ShiftKeyCode(false, KeyCode.L));
		MyKeys.Add("SouthEast", new ShiftKeyCode(false, KeyCode.N));
		MyKeys.Add("South", new ShiftKeyCode(false, KeyCode.J));
		MyKeys.Add("SouthWest", new ShiftKeyCode(false, KeyCode.B));
		MyKeys.Add("West", new ShiftKeyCode(false, KeyCode.H));
		MyKeys.Add("NorthWest", new ShiftKeyCode(false, KeyCode.Y));
		MyKeys.Add("Wait", new ShiftKeyCode(false, KeyCode.Period));

		MyKeys.Add("GoUpStairs", new ShiftKeyCode(true, KeyCode.Comma));
		MyKeys.Add("GoDownStairs", new ShiftKeyCode(true, KeyCode.Period));

		MyKeys.Add("Enter", new ShiftKeyCode(false, KeyCode.Return));
		MyKeys.Add("Pause", new ShiftKeyCode(false, KeyCode.Escape));

		MyKeys.Add("Pickup", new ShiftKeyCode(false, KeyCode.Comma));
		MyKeys.Add("Interact", new ShiftKeyCode(false, KeyCode.Space));
        MyKeys.Add("Contextual Actions", new ShiftKeyCode(true, KeyCode.Space));
        MyKeys.Add("Look", new ShiftKeyCode(false, KeyCode.X));
        MyKeys.Add("Switch Target", new ShiftKeyCode(false, KeyCode.Tab));
        MyKeys.Add("ForceAttack", new ShiftKeyCode(false, KeyCode.LeftControl));
		MyKeys.Add("Walk", new ShiftKeyCode(false, KeyCode.W));
		MyKeys.Add("Throw", new ShiftKeyCode(false, KeyCode.T));
		MyKeys.Add("Reload", new ShiftKeyCode(false, KeyCode.R));
		MyKeys.Add("Rest", new ShiftKeyCode(false, KeyCode.E));
		MyKeys.Add("Fire", new ShiftKeyCode(false, KeyCode.F));

		MyKeys.Add("Inventory", new ShiftKeyCode(false, KeyCode.I));
		MyKeys.Add("Character", new ShiftKeyCode(false, KeyCode.C));
		MyKeys.Add("Abilities", new ShiftKeyCode(false, KeyCode.A));
		MyKeys.Add("Map", new ShiftKeyCode(false, KeyCode.M));
		MyKeys.Add("Journal", new ShiftKeyCode(false, KeyCode.Q));
		MyKeys.Add("Toggle Mouse", new ShiftKeyCode(false, KeyCode.None));
	}
}

public class ShiftKeyCode {

	public bool Shift;
	public KeyCode keyCode;

	public ShiftKeyCode(bool sh, KeyCode kc) {
		Shift = sh;
		keyCode = kc;
	}
}

public enum KeyPress {
	Down, Up, Held
}

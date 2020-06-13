using UnityEngine;
using LitJson;
using System.Linq;
using System.Collections.Generic;

[System.Serializable]
public class InputKeys
{
    public Dictionary<string, ShiftKeyCode> MyKeys;

    public InputKeys(JsonData data = null)
    {
        if (data == null)
        {
            Defaults();
        }
        else
        {
            Load(data);
        }
    }

    void Load(JsonData data)
    {
        Defaults();
        List<KeyValuePair<string, ShiftKeyCode>> keyPairList = new List<KeyValuePair<string, ShiftKeyCode>>(MyKeys);

        foreach (KeyValuePair<string, ShiftKeyCode> kvp in keyPairList)
        {
            ShiftKeyCode kc = GetValueFromData(data, kvp.Key);

            if (kc != null)
            {
                MyKeys[kvp.Key] = GetValueFromData(data, kvp.Key);
            }
        }
    }

    ShiftKeyCode GetValueFromData(JsonData data, string keyName)
    {
        if (data["Input"]["MyKeys"].ContainsKey(keyName))
        {
            bool sh = (data["Input"]["MyKeys"][keyName].ContainsKey("Shift")) ? (bool)data["Input"]["MyKeys"][keyName]["Shift"] : false;
            int kc = (int)data["Input"]["MyKeys"][keyName]["keyCode"];
            return new ShiftKeyCode(sh, (KeyCode)kc);
        }

        return null;
    }

    public KeyCode GetKeyCode(string search)
    {
        return MyKeys[search].keyCode;
    }

    public bool ShiftHeld()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    public bool GetKey(string search, KeyPress press = KeyPress.Down)
    {
        ShiftKeyCode kc1 = MyKeys[search];

        if (press == KeyPress.Held)
        {
            return (Input.GetKey(kc1.keyCode) && ShiftHeld() == kc1.Shift);
        }

        else if (press == KeyPress.Down)
        {
            if (search == "Enter")
            {
                return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift || Input.GetKeyDown(KeyCode.KeypadEnter));
            }
            else if (search == "North")
            {
                return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift || Input.GetKeyDown(KeyCode.UpArrow));
            }
            else if (search == "South")
            {
                return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift || Input.GetKeyDown(KeyCode.DownArrow));
            }
            else if (search == "East")
            {
                return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift || Input.GetKeyDown(KeyCode.RightArrow));
            }
            else if (search == "West")
            {
                return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift || Input.GetKeyDown(KeyCode.LeftArrow));
            }

            return (Input.GetKeyDown(kc1.keyCode) && ShiftHeld() == kc1.Shift);

        }
        else if (press == KeyPress.Up)
        {
            return (Input.GetKeyUp(kc1.keyCode) && ShiftHeld() == kc1.Shift);
        }

        return false;
    }

    public bool KeyAlreadyUsed(KeyCode search)
    {
        foreach (ShiftKeyCode keys in MyKeys.Values)
        {
            if (keys.keyCode == search && ShiftHeld() == keys.Shift)
            {
                return true;
            }
        }

        return false;
    }

    public string SearchKey(KeyCode search)
    {
        return MyKeys.First(x => (x.Value.keyCode == search)).Key;
    }

    public void Defaults()
    {
        MyKeys = new Dictionary<string, ShiftKeyCode>
        {
            { "North", new ShiftKeyCode(false, KeyCode.Keypad8) },
            { "East", new ShiftKeyCode(false, KeyCode.Keypad6) },
            { "South", new ShiftKeyCode(false, KeyCode.Keypad2) },
            { "West", new ShiftKeyCode(false, KeyCode.Keypad4) },
            { "NorthEast", new ShiftKeyCode(false, KeyCode.Keypad9) },
            { "SouthEast", new ShiftKeyCode(false, KeyCode.Keypad3) },
            { "SouthWest", new ShiftKeyCode(false, KeyCode.Keypad1) },
            { "NorthWest", new ShiftKeyCode(false, KeyCode.Keypad7) },
            { "Wait", new ShiftKeyCode(false, KeyCode.Keypad5) },

            { "GoUpStairs", new ShiftKeyCode(false, KeyCode.KeypadPlus) },
            { "GoDownStairs", new ShiftKeyCode(false, KeyCode.KeypadMinus) },

            { "Enter", new ShiftKeyCode(false, KeyCode.Return) },
            { "Pause", new ShiftKeyCode(false, KeyCode.Escape) },

            { "Pickup", new ShiftKeyCode(false, KeyCode.G) },
            { "Interact", new ShiftKeyCode(false, KeyCode.Space) },
            { "Look", new ShiftKeyCode(false, KeyCode.L) },
            { "Switch Target", new ShiftKeyCode(false, KeyCode.Tab) },
            { "AlternateAttack", new ShiftKeyCode(false, KeyCode.LeftControl) },
            { "GrappleAttack", new ShiftKeyCode(false, KeyCode.LeftAlt) },
            { "Walk", new ShiftKeyCode(false, KeyCode.W) },
            { "Throw", new ShiftKeyCode(false, KeyCode.T) },
            { "Reload", new ShiftKeyCode(false, KeyCode.R) },
            { "Rest", new ShiftKeyCode(false, KeyCode.E) },
            { "Fire", new ShiftKeyCode(false, KeyCode.F) },

            { "Inventory", new ShiftKeyCode(false, KeyCode.I) },
            { "Character", new ShiftKeyCode(false, KeyCode.C) },
            { "Abilities", new ShiftKeyCode(false, KeyCode.A) },
            { "Map", new ShiftKeyCode(false, KeyCode.M) },
            { "Journal", new ShiftKeyCode(false, KeyCode.J) },
            { "Contextual Actions", new ShiftKeyCode(false, KeyCode.None) },
            { "Toggle Mouse", new ShiftKeyCode(false, KeyCode.None) },
            { "Toggle Minimap", new ShiftKeyCode(false, KeyCode.None) }
        };
    }

    public void VIKeys()
    {
        MyKeys = new Dictionary<string, ShiftKeyCode>
        {
            { "North", new ShiftKeyCode(false, KeyCode.K) },
            { "East", new ShiftKeyCode(false, KeyCode.L) },
            { "South", new ShiftKeyCode(false, KeyCode.J) },
            { "West", new ShiftKeyCode(false, KeyCode.H) },
            { "NorthEast", new ShiftKeyCode(false, KeyCode.U) },
            { "SouthEast", new ShiftKeyCode(false, KeyCode.N) },
            { "SouthWest", new ShiftKeyCode(false, KeyCode.B) },
            { "NorthWest", new ShiftKeyCode(false, KeyCode.Y) },
            { "Wait", new ShiftKeyCode(false, KeyCode.Period) },

            { "GoUpStairs", new ShiftKeyCode(true, KeyCode.Comma) },
            { "GoDownStairs", new ShiftKeyCode(true, KeyCode.Period) },

            { "Enter", new ShiftKeyCode(false, KeyCode.Return) },
            { "Pause", new ShiftKeyCode(false, KeyCode.Escape) },

            { "Pickup", new ShiftKeyCode(false, KeyCode.Comma) },
            { "Interact", new ShiftKeyCode(false, KeyCode.Space) },
            { "Look", new ShiftKeyCode(false, KeyCode.X) },
            { "Switch Target", new ShiftKeyCode(false, KeyCode.Tab) },
            { "AlternateAttack", new ShiftKeyCode(false, KeyCode.LeftControl) },
            { "GrappleAttack", new ShiftKeyCode(false, KeyCode.LeftAlt) },
            { "Walk", new ShiftKeyCode(false, KeyCode.W) },
            { "Throw", new ShiftKeyCode(false, KeyCode.T) },
            { "Reload", new ShiftKeyCode(false, KeyCode.R) },
            { "Rest", new ShiftKeyCode(false, KeyCode.E) },
            { "Fire", new ShiftKeyCode(false, KeyCode.F) },

            { "Inventory", new ShiftKeyCode(false, KeyCode.I) },
            { "Character", new ShiftKeyCode(false, KeyCode.C) },
            { "Abilities", new ShiftKeyCode(false, KeyCode.A) },
            { "Map", new ShiftKeyCode(false, KeyCode.M) },
            { "Journal", new ShiftKeyCode(false, KeyCode.Q) },
            { "Contextual Actions", new ShiftKeyCode(false, KeyCode.None) },
            { "Toggle Mouse", new ShiftKeyCode(false, KeyCode.None) },
            { "Toggle Minimap", new ShiftKeyCode(false, KeyCode.None) }
        };
    }

    public void WASD()
    {
        MyKeys = new Dictionary<string, ShiftKeyCode>()
        {
            { "North", new ShiftKeyCode(false, KeyCode.W) },
            { "East", new ShiftKeyCode(false, KeyCode.D) },
            { "South", new ShiftKeyCode(false, KeyCode.X) },
            { "West", new ShiftKeyCode(false, KeyCode.A) },
            { "NorthEast", new ShiftKeyCode(false, KeyCode.E) },
            { "SouthEast", new ShiftKeyCode(false, KeyCode.C) },
            { "SouthWest", new ShiftKeyCode(false, KeyCode.Z) },
            { "NorthWest", new ShiftKeyCode(false, KeyCode.Q) },
            { "Wait", new ShiftKeyCode(false, KeyCode.S) },

            { "GoUpStairs", new ShiftKeyCode(false, KeyCode.KeypadPlus) },
            { "GoDownStairs", new ShiftKeyCode(false, KeyCode.KeypadMinus) },

            { "Enter", new ShiftKeyCode(false, KeyCode.Return) },
            { "Pause", new ShiftKeyCode(false, KeyCode.Escape) },

            { "Pickup", new ShiftKeyCode(false, KeyCode.G) },
            { "Interact", new ShiftKeyCode(false, KeyCode.Space) },
            { "Look", new ShiftKeyCode(false, KeyCode.L) },
            { "Switch Target", new ShiftKeyCode(false, KeyCode.Tab) },
            { "AlternateAttack", new ShiftKeyCode(false, KeyCode.LeftControl) },
            { "GrappleAttack", new ShiftKeyCode(false, KeyCode.LeftAlt) },
            { "Walk", new ShiftKeyCode(false, KeyCode.B) },
            { "Throw", new ShiftKeyCode(false, KeyCode.T) },
            { "Reload", new ShiftKeyCode(false, KeyCode.R) },
            { "Rest", new ShiftKeyCode(false, KeyCode.Slash) },
            { "Fire", new ShiftKeyCode(false, KeyCode.F) },

            { "Inventory", new ShiftKeyCode(false, KeyCode.I) },
            { "Character", new ShiftKeyCode(false, KeyCode.H) },
            { "Abilities", new ShiftKeyCode(false, KeyCode.V) },
            { "Map", new ShiftKeyCode(false, KeyCode.M) },
            { "Journal", new ShiftKeyCode(false, KeyCode.J) },
            { "Contextual Actions", new ShiftKeyCode(false, KeyCode.None) },
            { "Toggle Mouse", new ShiftKeyCode(false, KeyCode.None) },
            { "Toggle Minimap", new ShiftKeyCode(false, KeyCode.None) }
        };
    }
}

public class ShiftKeyCode
{

    public bool Shift;
    public KeyCode keyCode;

    public ShiftKeyCode(bool sh, KeyCode kc)
    {
        Shift = sh;
        keyCode = kc;
    }
}

public enum KeyPress
{
    Down, Up, Held
}
using System.Collections.Generic;
using UnityEngine;
using System;

public static class PrettyKeyNames
{
    static Dictionary<KeyCode, string> keys;

    public static string GetPrettifiedKey(KeyCode k)
    {
        if (keys == null)
        {
            Initialize();
        }

        return keys[k];
    }

    static void Initialize()
    {
        keys = new Dictionary<KeyCode, string>();

        foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
        {
            string desc = "";

            switch (kc)
            {
                case KeyCode.RightBracket:
                    desc = "]";
                    break;

                case KeyCode.LeftBracket:
                    desc = "]";
                    break;

                case KeyCode.Comma:
                    desc = ",";
                    break;

                case KeyCode.Period:
                    desc = ".";
                    break;

                case KeyCode.Semicolon:
                    desc = ";";
                    break;

                case KeyCode.Quote:
                    desc = "'";
                    break;

                case KeyCode.KeypadPlus:
                case KeyCode.Plus:
                    desc = "+";
                    break;

                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    desc = "-";
                    break;

                case KeyCode.Equals:
                case KeyCode.KeypadEquals:
                    desc = "=";
                    break;

                case KeyCode.KeypadMultiply:
                    desc = "*";
                    break;

                case KeyCode.KeypadDivide:
                    desc = "/";
                    break;

                case KeyCode.Slash:
                    desc = "/";
                    break;

                case KeyCode.Backslash:
                    desc = "\\";
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    desc = "Enter";
                    break;

                case KeyCode.LeftControl:
                    desc = "L Ctrl";
                    break;

                case KeyCode.RightControl:
                    desc = "R Ctrl";
                    break;

                case KeyCode.RightCommand:
                    desc = "R Cmd";
                    break;

                case KeyCode.LeftCommand:
                    desc = "L Cmd";
                    break;

                case KeyCode.LeftAlt:
                    desc = "L Alt";
                    break;

                case KeyCode.RightAlt:
                    desc = "R Alt";
                    break;

                case KeyCode.Alpha0:
                case KeyCode.Keypad0:
                    desc = "0";
                    break;

                case KeyCode.Alpha1:
                case KeyCode.Keypad1:
                    desc = "1";
                    break;

                case KeyCode.Alpha2:
                case KeyCode.Keypad2:
                    desc = "2";
                    break;

                case KeyCode.Alpha3:
                case KeyCode.Keypad3:
                    desc = "3";
                    break;

                case KeyCode.Alpha4:
                case KeyCode.Keypad4:
                    desc = "4";
                    break;

                case KeyCode.Alpha5:
                case KeyCode.Keypad5:
                    desc = "5";
                    break;

                case KeyCode.Alpha6:
                case KeyCode.Keypad6:
                    desc = "6";
                    break;

                case KeyCode.Alpha7:
                case KeyCode.Keypad7:
                    desc = "7";
                    break;

                case KeyCode.Alpha8:
                case KeyCode.Keypad8:
                    desc = "8";
                    break;

                case KeyCode.Alpha9:
                case KeyCode.Keypad9:
                    desc = "9";
                    break;

                default:
                    desc = kc.ToString();
                    break;
            }

            if (!keys.ContainsKey(kc))
            {
                keys.Add(kc, desc);
            }
        }
    }
}

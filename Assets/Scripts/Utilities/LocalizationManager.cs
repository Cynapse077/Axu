using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LitJson;

public static class LocalizationManager
{
    public static string defaultText = "Untranslated Data";
    public static string filePath = "/Mods/Core/Dialogue/Localization.json";

    static JsonData data;
    static Dictionary<string, string[]> LocalizedContent;
    public static bool done;

    public static void LoadLocalizedData()
    {
        if (LocalizedContent != null)
        {
            return;
        }

        LocalizedContent = new Dictionary<string, string[]>();

        string myFile = File.ReadAllText(Application.streamingAssetsPath + filePath);
        data = JsonMapper.ToObject(myFile);

        QuickAdd_1Param("Titles");
        QuickAdd_1Param("Buttons");
        QuickAdd_1Param("Misc");
        QuickAdd_1Param("Profs");
        QuickAdd_1Param("Dialogue Options");
        QuickAdd_1Param("Options Menu");
        QuickAdd_1Param("Keybinds");
        QuickAdd_1Param("YesNo");
        QuickAdd_1Param("Log");
        QuickAdd_1Param("Item Actions");
        QuickAdd_1Param("Item Tooltip");
        QuickAdd_1Param("Hover Tooltip");
        QuickAdd_1Param("Locations");
        QuickAdd_1Param("Naming");
        QuickAdd_1Param("Map Features");
        QuickAdd_1Param("Help");

        AddWithTooltip("Stats");
        AddWithTooltip("Proficiencies");
        AddWithTooltip("Status Effects");
        AddWithTooltip("Display Radiation");
        AddWithTooltip("Difficulties");

        done = true;
    }

    static void AddWithTooltip(string section)
    {
        for (int i = 0; i < data[section].Count; i++)
        {
            string key = data[section][i]["ID"].ToString();
            string[] value = new string[2] {
                data[section][i]["Display"].ToString(),
                data[section][i]["Tooltip"].ToString()
            };

            if (!LocalizedContent.ContainsKey(key))
            {
                LocalizedContent.Add(key, value);
            }
        }
    }

    static void QuickAdd_1Param(string section)
    {
        if (!data.ContainsKey(section))
            return;

        for (int i = 0; i < data[section].Count; i++)
        {
            string key = data[section][i]["ID"].ToString();
            string[] value = new string[1] { data[section][i]["Display"].ToString() };

            if (!LocalizedContent.ContainsKey(key))
                LocalizedContent.Add(key, value);
            else
                Debug.Log("Contains " + key);
        }
    }

    public static string[] GetLocalizedContent(string key)
    {
        string[] result = new string[2] { defaultText, defaultText };

        if (LocalizedContent == null)
        {
            LoadLocalizedData();
        }

        if (LocalizedContent.ContainsKey(key))
            result = LocalizedContent[key];

        return result;
    }

    public static string GetContent(string key)
    {
        return GetLocalizedContent(key)[0];
    }

    public static string GetRandomHelpMessage()
    {
        List<string> helpMessages = new List<string>();

        if (LocalizedContent == null)
        {
            LoadLocalizedData();
        }

        foreach (string s in LocalizedContent.Keys)
        {
            if (s.Contains("Help"))
            {
                helpMessages.Add(LocalizedContent[s][0]);
            }
        }

        if (helpMessages.Count > 0)
        {
            return helpMessages.GetRandom();
        }
        else
        {
            return " ";
        }
    }
}

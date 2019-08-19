using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LitJson;

public static class LocalizationManager
{
    public static TranslatedText GetLocalizedContent(string key)
    {
        return GameData.Get<TranslatedText>(key) as TranslatedText;
    }

    public static string GetContent(string key)
    {
        return GetLocalizedContent(key).display;
    }

    public static string GetRandomHelpMessage()
    {
        List<string> helpMessages = new List<string>();

        foreach (TranslatedText t in GameData.GetAll<TranslatedText>())
        {
            if (t.ID.Contains("Help"))
            {
                helpMessages.Add(t.display);
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

    public static string Translate(this string s)
    {
        return GetContent(s);
    }
}

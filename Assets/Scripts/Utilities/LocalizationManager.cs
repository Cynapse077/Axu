using System.Collections.Generic;

public static class LocalizationManager
{
    public static TranslatedText GetLocalizedContent(string key)
    {
        return GameData.Get<TranslatedText>(key);
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

        return (helpMessages.Count > 0) ? helpMessages.GetRandom() : "";
    }

    public static string Translate(this string s)
    {
        return GetContent(s);
    }
}

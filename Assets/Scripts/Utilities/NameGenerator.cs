using UnityEngine;
using System.Collections.Generic;
using System.IO;
using LitJson;

public static class NameGenerator
{
    static List<string> Prefixes;
    static List<string> Suffixes;
    static List<string> ArtWords;

    static string[] townEnds = new string[] {
        "ton", "ville", "wood", "dale", "crag", "field", "ham", "hope", "ard"
    };

    public static string CharacterName(System.Random rand)
    {
        if (Prefixes == null || Prefixes.Count <= 0)
            FillSylList(Application.streamingAssetsPath);

        string prefix = Prefixes.GetRandom(SeedManager.textRandom), suffix = Suffixes.GetRandom(SeedManager.textRandom);
        string mid = "";

        int ranNum = rand.Next(100);

        if (ranNum < 10)
            mid = Suffixes.GetRandom(SeedManager.textRandom);

        return prefix + mid + suffix;
    }

    public static string CityName(System.Random rand)
    {
        string name = Prefixes.GetRandom(rand) + Suffixes.GetRandom(rand);

        if (rand.Next(100) < 30)
            name += townEnds.GetRandom(rand);

        return name;
    }

    public static string ArtifactName(System.Random rand)
    {
        string name1 = ArtWords.GetRandom(rand);
        int ranNum = rand.Next(100);

        if (ranNum < 33)
        {
            return "The " + name1;
        }
        else if (ranNum < 66)
        {
            string name2 = ArtWords.GetRandom(rand);
            return name1 + " of " + name2;
        }
        else
        {
            string name2 = ArtWords.GetRandom(rand);
            return name1 + name2.ToLower();
        }
    }

    public static void FillSylList(string appPath)
    {
        if (Prefixes != null && Prefixes.Count > 0)
            return;

        Prefixes = new List<string>();
        Suffixes = new List<string>();
        ArtWords = new List<string>();

        string sylList = File.ReadAllText(appPath + "/Mods/Core/Dialogue/NameSyllables.json");
        JsonData data = JsonMapper.ToObject(sylList);

        if (data.ContainsKey("Prefixes"))
        {
            for (int i = 0; i < data["Prefixes"].Count; i++)
            {
                Prefixes.Add(data["Prefixes"][i].ToString());
            }
        }

        if (data.ContainsKey("Suffixes"))
        {
            for (int i = 0; i < data["Suffixes"].Count; i++)
            {
                Suffixes.Add(data["Suffixes"][i].ToString());
            }
        }

        string wordList = File.ReadAllText(appPath + "/Mods/Core/Dialogue/ArtifactNames.json");
        data = JsonMapper.ToObject(wordList);

        for (int i = 0; i < data["Words"].Count; i++)
        {
            ArtWords.Add(data["Words"][i].ToString());
        }
    }
}

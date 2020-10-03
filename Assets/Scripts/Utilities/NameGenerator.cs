using UnityEngine;
using System.Collections.Generic;
using System.IO;
using LitJson;
using System.Linq;

public static class NameGenerator
{
    static List<string> Prefixes;
    static List<string> Suffixes;
    static List<string> TownSuffixes;
    static List<string> ArtWords;
    static List<string> ArtNamesComplex;

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

        if (rand.Next(100) < 30 && !TownSuffixes.NullOrEmpty())
            name += TownSuffixes.GetRandom(rand);

        return name;
    }

    public static string ArtifactName(System.Random rand)
    {
        string name1 = ArtWords.GetRandom(rand);
        string name2 = ArtWords.GetRandom(rand);

        if (ArtNamesComplex.Any())
        {
            int ranNum = rand.Next(100);

            if (ranNum < 66)
            {
                string chosenName = ArtNamesComplex.GetRandom(rand);
                if (chosenName.Contains("{1}"))
                {
                    return chosenName.Format(name1, name2);
                }

                return chosenName.Format(name1);
            }
        }

        return name1 + name2.ToLower();
    }

    public static void FillSylList(string appPath)
    {
        if (Prefixes != null && Prefixes.Count > 0)
            return;

        Prefixes = new List<string>();
        Suffixes = new List<string>();
        TownSuffixes = new List<string>();
        ArtWords = new List<string>();
        ArtNamesComplex = new List<string>();

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

        if (data.ContainsKey("Town Suffixes"))
        {
            for (int i = 0; i < data["Town Suffixes"].Count; i++)
            {
                TownSuffixes.Add(data["Town Suffixes"][i].ToString());
            }
        }

        string wordList = File.ReadAllText(appPath + "/Mods/Core/Dialogue/ArtifactNames.json");
        data = JsonMapper.ToObject(wordList);

        for (int i = 0; i < data["Words"].Count; i++)
        {
            ArtWords.Add(data["Words"][i].ToString());
        }

        for (int i = 0; i < data["Names Complex"].Count; i++)
        {
            ArtNamesComplex.Add(data["Names Complex"][i].ToString());
        }
    }
}

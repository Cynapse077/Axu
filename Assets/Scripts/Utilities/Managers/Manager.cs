using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class Manager
{
    public static Coord worldMapSize;
    public static Coord localMapSize;

    public static bool lightingOn = true;
    public static bool newGame = true;
    public static bool noEncounters = false;

    public static int worldSeed;
    public static string playerName, profName;

    public static Coord localStartPos = new Coord(22, 2);
    public static int startElevation = 0;
    public static Weather startWeather = Weather.Clear;
    public static PlayerBuilder playerBuilder;
    public const int TileResolution = 16;

    public static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Save");

    public static string SettingsDirectory => Path.Combine(Application.persistentDataPath, "Settings.json");

    public static void DeleteActiveSaveFile()
    {
        //Delete save game
        string[] ss = Directory.GetFiles(SaveDirectory, "*.axu", SearchOption.AllDirectories);
        List<LoadSaveMenu.SaveGameObject> savedGames = new List<LoadSaveMenu.SaveGameObject>();
        LoadSaveMenu.GetDataFromDirectory(ss, savedGames);

        for (int i = 0; i < savedGames.Count; i++)
        {
            if (savedGames[i].charName == playerName && savedGames[i].charProf == profName && savedGames[i].diffName == World.difficulty.Level.ToString())
            {
                File.Delete(Directory.GetFiles(SaveDirectory)[i]);
                break;
            }
        }
    }
}
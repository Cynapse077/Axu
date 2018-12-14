using UnityEngine;
using System.IO;

public static class Manager
{
    public static Coord worldMapSize = new Coord(200, 200);
    public static Coord localMapSize = new Coord(45, 30);

    public static bool lightingOn = true;
    public static bool newGame = true;
    public static bool noEncounters = false;

    public static int worldSeed;
    public static string playerName, profName;

    public static Coord localStartPos = new Coord(22, 2);
    public static int startElevation = 0;
    public static Weather startWeather = Weather.Clear;
    public static PlayerBuilder playerBuilder;
    public static int tileResolution = 16;

    public static void ClearFiles()
    {
        MyConsole.ClearLog();

        startElevation = 0;
        localStartPos = new Coord(22, 2);

        DeleteFileIfExists(SaveDirectory + "/" + playerName + ".axu");
    }

    public static string SaveDirectory
    {
        get { return (Application.persistentDataPath + "/Save"); }
    }
    public static string SettingsDirectory
    {
        get { return (Application.persistentDataPath + "/Settings.json"); }
    }

    static void DeleteFileIfExists(string pathExtension)
    {
        if (File.Exists(pathExtension))
            File.Delete(pathExtension);
    }
}
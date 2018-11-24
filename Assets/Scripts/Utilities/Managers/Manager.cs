using UnityEngine;
using System.IO;

public static class Manager {

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

    public static bool InWorldBounds(Coord pos) {
		return !(pos.x < 0 || pos.x > worldMapSize.y || pos.y > -1 || pos.y < -worldMapSize.y);
    }

    public static bool InLocalBounds(Coord pos) {
		return !(pos.x < 0 || pos.x > localMapSize.y || pos.y > -1 || pos.y < -localMapSize.y);
    }

	public static void ClearFiles() {
        MyConsole.ClearLog();

        startElevation = 0;
        localStartPos = new Coord(22, 2);

		DeleteFileIfExists(SaveDirectory);
    }

	public static string SaveDirectory {
		get { return (Application.persistentDataPath + "/Save01.axu"); }
	}
	public static string SettingsDirectory {
		get { return (Application.persistentDataPath + "/Settings.json"); }
	}

    static void DeleteFileIfExists(string pathExtension) {
        if (File.Exists(pathExtension))
            File.Delete(pathExtension);
    }
}
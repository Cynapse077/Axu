using System.IO;
using UnityEngine;

public static class ModUtility
{
    public const string ItemsFoler = "Items";
    public const string EntitiesFolder = "Entities";
    public const string MapsFolder = "Maps";
    public const string MapFilesFolder = "MapFiles";
    public const string DialogueFilesFoler = "Dialogue";
    public const string ModSettingsFileName = "ModSettings.json";
    public const string CoreModID = "Core";

    static int NextFreeID = -1;
    static int NextLoadID = 1;

    public static string ModFolderPath
    {
        get
        {
            return Path.Combine(Application.streamingAssetsPath, "Mods");
        }
    }

    public static int GetNextFreeID()
    {
        return ++NextFreeID;
    }    

    public static int GetNextLoadOrder(Mod m)
    {
        if (m.IsCore())
        {
            return 0;
        }

        return NextLoadID++;
    }
}
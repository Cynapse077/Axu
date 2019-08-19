using System.IO;
using System.Collections.Generic;

public static class ModManager
{
    public static bool IsInitialized;
    public static List<Mod> mods = new List<Mod>();

    public static void InitializeAllMods()
    {
        if (IsInitialized)
        {
            return;
        }

        string[] modPaths = Directory.GetDirectories(ModUtility.ModFolderPath);

        foreach (string m in modPaths)
        {
            Mod newMod = new Mod(m);

            if (mods.Find(x => x.id == newMod.id) != null)
            {
                newMod.ChangeID(newMod.id + ModUtility.GetNextFreeID());
            }

            mods.Add(newMod);
        }

        mods.Sort((x, y) => x.loadOrder.CompareTo(y.loadOrder));

        foreach (Mod m in mods)
        {
            m.PreLoadData();
        }

        IsInitialized = true;
    }

    public static void ResetAllMods()
    {
        LuaManager.ResetLuaFiles();
        GameData.ResetGameData();
        IsInitialized = false;

        InitializeAllMods();
    }

    public static void PostLoadData()
    {
        foreach (Mod m in mods)
        {
            m.PostLoadData();
        }
    }

    public static Mod GetModByID(string id)
    {
        return mods.Find(x => x.id == id);
    }
}
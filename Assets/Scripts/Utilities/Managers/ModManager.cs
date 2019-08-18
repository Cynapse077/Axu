using System.IO;
using System.Collections.Generic;

public static class ModManager
{
    public static bool IsInitialized;
    static List<Mod> mods = new List<Mod>();

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
                newMod.ChangeID("MOD_" + ModUtility.GetNextFreeID());
            }

            mods.Add(newMod);
        }

        mods.Sort((x, y) => x.loadOrder.CompareTo(y.loadOrder));

        foreach (Mod m in mods)
        {
            m.LoadSprites();
            m.LoadItemFolderData();
            m.LoadLuaFiles();
        }

        IsInitialized = true;
    }

    public static void LoadQuests()
    {
        foreach (Mod m in mods)
        {
            m.LoadQuests();
        }
    }

    public static Mod GetModByID(string id)
    {
        return mods.Find(x => x.id == id);
    }
}
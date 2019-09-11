using UnityEngine;
using System.Text;
using System.IO;
using System.Collections.Generic;

public static class ModManager
{
    public static bool PreInitialized;
    public static bool PostInitialized;
    public static List<Mod> mods = new List<Mod>();

    /// <summary>
    /// Initializes all pre-game content (everything excluding quests for now)
    /// </summary>
    public static void InitializeAllMods()
    {
        if (PreInitialized)
        {
            return;
        }

        if (mods.Empty())
        {
            string[] modPaths = Directory.GetDirectories(ModUtility.ModFolderPath);

            foreach (string m in modPaths)
            {
                Mod newMod = new Mod(m);

                if (mods.Find(x => x.id == newMod.id) != null)
                {
                    newMod.ChangeID(string.Format("{0}_{1}", newMod.id, ModUtility.GetNextFreeID()));
                }

                mods.Add(newMod);
            }

            mods.Sort((x, y) => x.loadOrder.CompareTo(y.loadOrder));
        }
        
        foreach (Mod m in mods)
        {
            if (m.IsActive)
            {
                m.PreLoadData();
            }
        }

        PreInitialized = true;

        // Uncomment to get NPC difficulty data
        //NPC_Blueprint.PrintNPCDifficultyLevels();
    }

    public static void ResetAllMods()
    {
        LuaManager.ResetLuaFiles();
        GameData.ResetGameData();
        SpriteManager.ResetAll();
        PreInitialized = false;
        PostInitialized = false;

        InitializeAllMods();
    }

    /// <summary>
    /// Occurs when a new game is started.
    /// </summary>
    public static void PostLoadData()
    {
        if (PostInitialized)
        {
            return;
        }

        foreach (Mod m in mods)
        {
            if (m.IsActive)
            {
                m.PostLoadData();
            }
        }

        PostInitialized = true;
    }

    public static Mod GetModByID(string id)
    {
        return mods.Find(x => x.id == id);
    }
}
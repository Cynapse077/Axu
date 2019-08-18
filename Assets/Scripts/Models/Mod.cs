using UnityEngine;
using LitJson;
using System;
using System.IO;

public class Mod
{
    public string id;
    public string name;
    public string filePath;
    public int loadOrder = 0;

    public Mod(string filPth)
    {
        Init(filPth);
    }

    void Init(string filPth)
    {
        filePath = filPth;
        string modSettingsPath = Path.Combine(filePath, ModUtility.ModSettingsFileName);

        if (!File.Exists(modSettingsPath))
        {
            Debug.Log("Mod is missing ModSettings.json file. Skipping.");
            return;
        }

        string settingContents = File.ReadAllText(modSettingsPath);
        JsonData dat = JsonMapper.ToObject(settingContents);

        dat.TryGetValue("ID", out id, "MOD_" + ModUtility.GetNextFreeID());
        dat.TryGetValue("Name", out name, "Unnamed");
        dat.TryGetValue("Load Order", out loadOrder, ModUtility.GetNextLoadOrder(this));
    }

    void AddData<T>(string folderPath, string fileName, string key)
    {
        string path = Path.Combine(filePath, folderPath);
        string file = Path.Combine(path, fileName);
        
        if (File.Exists(file))
        {
            string contents = File.ReadAllText(file);
            JsonData data = JsonMapper.ToObject(contents)[key];

            foreach (JsonData dat in data)
            {
                IAsset t = (IAsset)Activator.CreateInstance(typeof(T), dat);
                GameData.Add<T>(t);
            }
        }
    }

    public void LoadItemFolderData()
    {
        //Item folder
        string itemPath = Path.Combine(filePath, ModUtility.ItemsFoler);
        if (Directory.Exists(itemPath))
        {
            AddData<Item>(itemPath, "Items.json", "Items");
            AddData<ItemModifier>(itemPath, "ItemModifiers.json", "ItemModifiers");
            AddData<Liquid>(itemPath, "Liquids.json", "Liquids");

            //Mixing Tables
            string liqFile = Path.Combine(itemPath, "Liquids.json");
            if (File.Exists(liqFile))
            {
                string contents = File.ReadAllText(liqFile);
                JsonData tables = JsonMapper.ToObject(contents);

                if (tables.ContainsKey("Mixing Tables"))
                    Liquid.SetupMixingTables(tables);
            }
        }

        string entityPath = Path.Combine(filePath, ModUtility.EntitiesFolder);
        if (Directory.Exists(entityPath))
        {
            AddData<Skill>(entityPath, "Abilities.json", "Abilities");
            AddData<Trait>(entityPath, "Traits.json", "Traits");
            AddData<Wound>(entityPath, "Wounds.json", "Wounds");
            AddData<Felony>(entityPath, "Felonies.json", "Felonies");
            AddData<MapObjectBlueprint>(entityPath, "Objects.json", "Objects");
            AddData<Faction>(entityPath, "Factions.json", "Factions");
            AddData<NPC_Blueprint>(entityPath, "NPCs.json", "NPCs");
            AddData<GroupBlueprint>(entityPath, "Encounters.json", "Encounters");
        }
    }
    public void LoadLuaFiles()
    {
        string luaPath = Path.Combine(filePath, "Lua");
        if (Directory.Exists(luaPath))
        {
            string[] luaFiles = Directory.GetFiles(luaPath, "*lua", SearchOption.AllDirectories);

            for (int i = 0; i < luaFiles.Length; i++)
            {
                LuaManager.AddFile(luaFiles[i]);
            }
        }
    }

    public void LoadSprites()
    {
        string artPath = Path.Combine(filePath, "Art");
        if (Directory.Exists(artPath))
        {
            string objFilePath = Path.Combine(artPath, "Objects");
            string npcFilePath = Path.Combine(artPath, "NPCs");

            SpriteManager.AddObjectSprites(objFilePath, SpriteType.Object);
            SpriteManager.AddObjectSprites(npcFilePath, SpriteType.NPC);
        }
    }

    public void LoadQuests()
    {
        AddData<Quest>(filePath, "Quests.json", "Quests");
    }

    public bool IsCore()
    {
        return id == ModUtility.CoreModID;
    }

    public void ChangeID(string newID)
    {
        id = newID;
    }
}

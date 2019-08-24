using System;
using System.IO;
using UnityEngine;
using LitJson;

public class Mod
{
    public string id;
    public string name;
    public string description;
    public string creator;
    public int loadOrder = 0;
    string filePath;

    static bool LogAll = false;

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

        dat.TryGetString("ID", out id, "MOD_" + ModUtility.GetNextFreeID());
        dat.TryGetString("Name", out name, "Unnamed");
        dat.TryGetInt("Load Order", out loadOrder, ModUtility.GetNextLoadOrder(this));
        dat.TryGetString("Description", out description, "No description.");
        dat.TryGetString("Creator", out creator, "Unknown");
    }

    bool AddData<T>(string folderPath, string fileName, string key)
    {
        string path = Path.Combine(filePath, folderPath);
        string file = Path.Combine(path, fileName);
        
        if (File.Exists(file))
        {
            if (LogAll)
                Debug.Log("------- Loading <" + typeof(T).ToString() + "> -------");

            string contents = File.ReadAllText(file);
            JsonData data = JsonMapper.ToObject(contents)[key];

            foreach (JsonData dat in data)
            {
                IAsset t = (IAsset)Activator.CreateInstance(typeof(T), dat);
                t.ModID = id;

                if (LogAll)
                    Debug.Log("    " + t.ID + " - Success!");

                GameData.Add<T>(t);
            }

            if (LogAll)
                Debug.Log("------- Finished Loading <" + typeof(T).ToString() + "> -------");

            return true;
        }

        return false;
    }

    public void PreLoadData()
    {
        //Sprites
        string artPath = Path.Combine(filePath, "Art");
        if (Directory.Exists(artPath))
        {
            string objFilePath = Path.Combine(artPath, "Objects");
            string npcFilePath = Path.Combine(artPath, "NPCs");

            SpriteManager.AddObjectSprites(this, objFilePath, SpriteType.Object);
            SpriteManager.AddObjectSprites(this, npcFilePath, SpriteType.NPC);
        }

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
                {
                    Liquid.SetupMixingTables(tables);
                }
            }
        }

        //Entities folder
        string entityPath = Path.Combine(filePath, ModUtility.EntitiesFolder);
        if (Directory.Exists(entityPath))
        {
            AddData<Ability>(entityPath, "Abilities.json", "Abilities");
            AddData<Trait>(entityPath, "Traits.json", "Traits");
            AddData<Wound>(entityPath, "Wounds.json", "Wounds");
            AddData<Felony>(entityPath, "Felonies.json", "Felonies");
            AddData<MapObjectBlueprint>(entityPath, "Objects.json", "Objects");
            AddData<Faction>(entityPath, "Factions.json", "Factions");
            AddData<NPC_Blueprint>(entityPath, "NPCs.json", "NPCs");
            AddData<GroupBlueprint>(entityPath, "Encounters.json", "Encounters");
        }

        //Lua folder
        string luaPath = Path.Combine(filePath, "Lua");
        if (Directory.Exists(luaPath))
        {
            string[] luaFiles = Directory.GetFiles(luaPath, "*lua", SearchOption.AllDirectories);

            foreach (string luaFile in luaFiles)
            {
                LuaManager.AddFile(luaFile);
            }
        }

        //Map folder
        string mapPath = Path.Combine(filePath, ModUtility.MapsFolder);
        if (Directory.Exists(mapPath))
        {
            AddData<ZoneBlueprint>(mapPath, "Locations.json", "Zones");
            AddData<ZoneBlueprint_Underground>(mapPath, "Locations.json", "Undergrounds");

            string tilesPath = Path.Combine(mapPath, "LocalTiles.json");
            if (File.Exists(tilesPath))
            {
                Tile.LoadTiles(tilesPath);
            }
        }

        //Dialogue Folder
        string diaPath = Path.Combine(filePath, ModUtility.DialogueFilesFoler);
        if (Directory.Exists(diaPath))
        {
            AddData<Book>(diaPath, "Books.json", "Books");
            AddData<DialogueNode>(diaPath, "DialogueOptions.json", "Dialogue Options");
            AddData<DialogueSingle>(diaPath, "Dialogue.json", "Dialogue");

            string content = File.ReadAllText(Path.Combine(diaPath, "Localization.json"));
            JsonData dat = JsonMapper.ToObject(content);

            foreach (string s in dat.Keys)
            {
                AddData<TranslatedText>(diaPath, "Localization.json", s);
            }
        }
    }

    public void PostLoadData()
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

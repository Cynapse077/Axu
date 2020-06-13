using System;
using System.IO;
using LitJson;

public class Mod
{
    public string id;
    public string name;
    public string description;
    public string creator;
    public int loadOrder = 0;
    public readonly bool failedToLoad;
    string filePath;
    bool isActive = true;

    const bool LogAll = false;

    public bool IsActive { get { return isActive; } }

    public Mod(string filePath)
    {
        if (!Init(filePath))
        {
            failedToLoad = true;
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
    }

    bool Init(string filPth)
    {
        filePath = filPth;
        string modSettingsPath = Path.Combine(filePath, ModUtility.ModSettingsFileName);

        if (!File.Exists(modSettingsPath))
        {
            Log.Error(string.Format("Mod \"{0}\" is missing ModSettings.json file.", filePath));
            return false;
        }

        string settingContents = File.ReadAllText(modSettingsPath);
        JsonData dat = JsonMapper.ToObject(settingContents);

        dat.TryGetString("ID", out id, "MOD_" + ModUtility.GetNextFreeID());
        dat.TryGetInt("Load Order", out loadOrder, ModUtility.GetNextLoadOrder(this));
        dat.TryGetString("Name", out name, "Unnamed");
        dat.TryGetString("Description", out description, "No description.");
        dat.TryGetString("Creator", out creator, "Unknown");

        return true;
    }

    bool AddData<T>(string folderPath, string fileName, string key)
    {
        string path = Path.Combine(filePath, folderPath);
        string file = Path.Combine(path, fileName);
        
        if (File.Exists(file))
        {
            try
            {
                Log.MessageConditional("<color=grey>------- Loading <" + typeof(T).ToString() + "> -------</color>", LogAll);

                JsonData data = JsonMapper.ToObject(File.ReadAllText(file))[key];
                int total = 0;

                foreach (JsonData dat in data)
                {
                    IAsset t = (IAsset)Activator.CreateInstance(typeof(T), dat);
                    t.ModID = id;

                    Log.MessageConditional("    (" + typeof(T).ToString() + ") " + t.ID + " - <color=green>Success!</color>", LogAll);

                    GameData.Add<T>(t);
                    total++;
                }

                Log.MessageConditional("<color=grey>------- Finished Loading <" + typeof(T).ToString() + "> - Total: " + total + " -------</color>", LogAll);
            }
            catch (Exception e)
            {
                Log.Error("Could not load <" + typeof(T).ToString() + ">: " + e);
                return false;
            }
            
            return true;
        }

        return false;
    }

    bool AddData<T>(string file)
    {
        if (File.Exists(file))
        {
            string contents = File.ReadAllText(file);
            JsonData data = JsonMapper.ToObject(contents);
            IAsset t = (IAsset)Activator.CreateInstance(typeof(T), data);
            t.ModID = id;
            GameData.Add<T>(t);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Occurs upon game startup.
    /// </summary>
    public void PreLoadData()
    {
        //Sprites
        string curPath = Path.Combine(filePath, "Art");
        if (Directory.Exists(curPath))
        {
            string objFilePath = Path.Combine(curPath, "Objects");
            string npcFilePath = Path.Combine(curPath, "NPCs");

            SpriteManager.AddObjectSprites(this, objFilePath, SpriteType.Object);
            SpriteManager.AddObjectSprites(this, npcFilePath, SpriteType.NPC);
        }

        //Item folder
        curPath = Path.Combine(filePath, ModUtility.ItemsFoler);
        if (Directory.Exists(curPath))
        { 
            AddData<Item>(curPath, "Items.json", "Items");
            AddData<ItemModifier>(curPath, "ItemModifiers.json", "ItemModifiers");
            AddData<EquipmentSet>(curPath, "EquipmentSets.json", "EquipmentSets");
            AddData<Liquid>(curPath, "Liquids.json", "Liquids");

            //Mixing Tables
            string liqFile = Path.Combine(curPath, "Liquids.json");
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
        curPath = Path.Combine(filePath, ModUtility.EntitiesFolder);
        if (Directory.Exists(curPath))
        {
            AddData<Ability>(curPath, "Abilities.json", "Abilities");
            AddData<Trait>(curPath, "Traits.json", "Traits");
            AddData<Wound>(curPath, "Wounds.json", "Wounds");
            AddData<Felony>(curPath, "Felonies.json", "Felonies");
            AddData<MapObject_Blueprint>(curPath, "Objects.json", "Objects");
            AddData<Faction>(curPath, "Factions.json", "Factions");
            AddData<NPC_Blueprint>(curPath, "NPCs.json", "NPCs");
            AddData<NPCGroup_Blueprint>(curPath, "Encounters.json", "Encounters");
            AddData<Incident>(curPath, "Incidents.json", "Incidents");
        }

        
        if (Directory.Exists(filePath))
        {
            //Lua files
            string[] luaFiles = Directory.GetFiles(filePath, "*lua", SearchOption.AllDirectories);
            foreach (string luaFile in luaFiles)
            {
                LuaManager.AddFile(id, luaFile);
            }

            //Map files
            string[] mapFiles = Directory.GetFiles(filePath, "*map", SearchOption.AllDirectories);
            foreach (string mapFile in mapFiles)
            {
                AddData<Map>(mapFile);
            }
        }

        //Map folder
        curPath = Path.Combine(filePath, ModUtility.MapsFolder);
        if (Directory.Exists(curPath))
        {
            AddData<Zone_Blueprint>(curPath, "Locations.json", "Zones");
            AddData<Vault_Blueprint>(curPath, "Locations.json", "Undergrounds");

            string tilesPath = Path.Combine(curPath, "LocalTiles.json");
            if (File.Exists(tilesPath))
            {
                TileManager.LoadTiles(tilesPath);
            }
        }

        //Dialogue Folder
        curPath = Path.Combine(filePath, ModUtility.DialogueFilesFoler);
        if (Directory.Exists(curPath))
        {
            AddData<Book>(curPath, "Books.json", "Books");
            AddData<DialogueNode>(curPath, "DialogueOptions.json", "Dialogue Options");
            AddData<DialogueSingle>(curPath, "Dialogue.json", "Dialogue");

            string localizationPath = Path.Combine(curPath, "Localization.json");
            if (File.Exists(localizationPath))
            {
                string content = File.ReadAllText(localizationPath);
                JsonData dat = JsonMapper.ToObject(content);

                foreach (string s in dat.Keys)
                {
                    AddData<TranslatedText>(curPath, "Localization.json", s);
                }
            }            
        }
    }

    /// <summary>
    /// Occurs when a new game is started.
    /// </summary>
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

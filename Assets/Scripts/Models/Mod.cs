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
    string filePath;
    bool isActive = true;

    static readonly bool LogAll = false;

    public bool IsActive { get { return isActive; } }

    public Mod(string filPth)
    {
        Init(filPth);
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
    }

    void Init(string filPth)
    {
        filePath = filPth;
        string modSettingsPath = Path.Combine(filePath, ModUtility.ModSettingsFileName);

        if (!File.Exists(modSettingsPath))
        {
            Log.Message("<color=red>Mod is missing ModSettings.json file. Skipping.</color>");
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
            Log.MessageConditional("<color=grey>------- Loading <" + typeof(T).ToString() + "> -------</color>", LogAll);

            string contents = File.ReadAllText(file);
            JsonData data = JsonMapper.ToObject(contents)[key];

            foreach (JsonData dat in data)
            {
                IAsset t = (IAsset)Activator.CreateInstance(typeof(T), dat);
                t.ModID = id;

                Log.MessageConditional("    " + t.ID + " - <color=green>Success!</color>", LogAll);

                GameData.Add<T>(t);
            }

            Log.MessageConditional("<color=grey>------- Finished Loading <" + typeof(T).ToString() + "> -------</color>", LogAll);

            return true;
        }

        return false;
    }

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
            AddData<MapObjectBlueprint>(curPath, "Objects.json", "Objects");
            AddData<Faction>(curPath, "Factions.json", "Factions");
            AddData<NPC_Blueprint>(curPath, "NPCs.json", "NPCs");
            AddData<GroupBlueprint>(curPath, "Encounters.json", "Encounters");
        }

        //Lua folder
        curPath = Path.Combine(filePath, "Lua");
        if (Directory.Exists(curPath))
        {
            string[] luaFiles = Directory.GetFiles(curPath, "*lua", SearchOption.AllDirectories);

            foreach (string luaFile in luaFiles)
            {
                LuaManager.AddFile(luaFile);
            }
        }

        //Map folder
        curPath = Path.Combine(filePath, ModUtility.MapsFolder);
        if (Directory.Exists(curPath))
        {
            AddData<ZoneBlueprint>(curPath, "Locations.json", "Zones");
            AddData<ZoneBlueprint_Underground>(curPath, "Locations.json", "Undergrounds");

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

            string content = File.ReadAllText(Path.Combine(curPath, "Localization.json"));
            JsonData dat = JsonMapper.ToObject(content);

            foreach (string s in dat.Keys)
            {
                AddData<TranslatedText>(curPath, "Localization.json", s);
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

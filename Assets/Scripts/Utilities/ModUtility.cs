using System.Collections.Generic;
using System.IO;
using LitJson;
using UnityEngine;

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

    public void LoadItemFolderData()
    {
        //Item folder
        string itemPath = Path.Combine(filePath, "Items");
        if (Directory.Exists(itemPath))
        {
            //Items
            string itemFile = Path.Combine(itemPath, "Items.json");
            if (File.Exists(itemFile))
            {
                string contents = File.ReadAllText(itemFile);
                JsonData dat = JsonMapper.ToObject(contents)["Items"];

                for (int i = 0; i < dat.Count; i++)
                {
                    Item item = new Item(dat[i]);
                    GameData.instance.Add<Item>(item);
                }
            }

            //Modifiers
            string modFile = Path.Combine(itemPath, "ItemModifiers.json");
            if (File.Exists(modFile))
            {
                string contents = File.ReadAllText(modFile);
                JsonData dat = JsonMapper.ToObject(contents)["ItemModifiers"];

                for (int i = 0; i < dat.Count; i++)
                {
                    ItemModifier im = new ItemModifier(dat[i]);
                    GameData.instance.Add<ItemModifier>(im);
                }
            }

            //Liquids
            string liqFile = Path.Combine(itemPath, "Liquids.json");
            if (File.Exists(liqFile))
            {
                string contents = File.ReadAllText(liqFile);
                JsonData dat = JsonMapper.ToObject(contents)["Liquids"];

                for (int i = 0; i < dat.Count; i++)
                {
                    Liquid liq = new Liquid(dat[i]);
                    GameData.instance.Add<Liquid>(liq);
                }

                JsonData tables = JsonMapper.ToObject(contents);
                if (tables.ContainsKey("Mixing Tables"))
                    Liquid.SetupMixingTables(tables);
            }
        }
    }

    public void LoadEntityFolderData()
    {
        //Entity folder
        string entityPath = Path.Combine(filePath, "Entities");
        if (Directory.Exists(entityPath))
        {
            //Abilities
            string abFile = Path.Combine(entityPath, "Abilities.json");
            if (File.Exists(abFile))
            {
                string contents = File.ReadAllText(abFile);
                JsonData dat = JsonMapper.ToObject(contents)["Abilities"];

                for (int i = 0; i < dat.Count; i++)
                {
                    Skill s = new Skill(dat[i]);
                    GameData.instance.Add<Skill>(s);
                }
            }

            //Traits
            string trFile = Path.Combine(entityPath, "Traits.json");
            if (File.Exists(trFile))
            {
                string contents = File.ReadAllText(trFile);
                JsonData dat = JsonMapper.ToObject(contents)["Traits"];

                for (int i = 0; i < dat.Count; i++)
                {
                    Trait t = new Trait(dat[i]);
                    GameData.instance.Add<Trait>(t);
                }
            }

            //Wounds
            string wFile = Path.Combine(entityPath, "Wounds.json");
            if (File.Exists(wFile))
            {
                string contents = File.ReadAllText(wFile);
                JsonData dat = JsonMapper.ToObject(contents)["Wounds"];

                for (int i = 0; i < dat.Count; i++)
                {
                    Wound w = new Wound(dat[i]);
                    GameData.instance.Add<Wound>(w);
                }
            }

            //Felonies
            string felFile = Path.Combine(entityPath, "Felonies.json");
            if (File.Exists(felFile))
            {
                string contents = File.ReadAllText(felFile);
                JsonData dat = JsonMapper.ToObject(contents)["Felonies"];

                for (int i = 0; i < dat.Count; i++)
                {
                    Felony f = new Felony(dat[i]);
                    GameData.instance.Add<Felony>(f);
                }
            }

            //Objects
            string obFile = Path.Combine(entityPath, "Objects.json");
            if (File.Exists(obFile))
            {
                string contents = File.ReadAllText(obFile);
                JsonData dat = JsonMapper.ToObject(contents)["Objects"];

                for (int i = 0; i < dat.Count; i++)
                {
                    MapObjectBlueprint bl = new MapObjectBlueprint(dat[i]);
                    GameData.instance.Add<MapObjectBlueprint>(bl);
                }
            }

            //Factions
            string facFile = Path.Combine(entityPath, "Factions.json");
            if (File.Exists(facFile))
            {
                string contents = File.ReadAllText(facFile);
                JsonData dat = JsonMapper.ToObject(contents)["Factions"];

                for (int i = 0; i < dat.Count; i++)
                {
                    Faction f = new Faction(dat[i]);
                    GameData.instance.Add<Faction>(f);
                }
            }

            //NPCs
            string npcFile = Path.Combine(entityPath, "NPCs.json");
            if (File.Exists(npcFile))
            {
                string contents = File.ReadAllText(npcFile);
                JsonData dat = JsonMapper.ToObject(contents)["NPCs"];

                for (int i = 0; i < dat.Count; i++)
                {
                    NPC_Blueprint bp = new NPC_Blueprint(dat[i]);
                    GameData.instance.Add<NPC_Blueprint>(bp);
                }
            }

            //Encounters
            string encounterFile = Path.Combine(entityPath, "Encounters.json");
            if (File.Exists(encounterFile))
            {
                string contents = File.ReadAllText(encounterFile);
                JsonData dat = JsonMapper.ToObject(contents)["Encounters"];

                for (int i = 0; i < dat.Count; i++)
                {
                    GroupBlueprint gb = new GroupBlueprint(dat[i]);
                    GameData.instance.Add<GroupBlueprint>(gb);
                }
            }
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
        string questPath = filePath;
        if (Directory.Exists(questPath))
        {
            string questFile = Path.Combine(questPath, "Quests.json");

            if (File.Exists(questFile))
            {
                string content = File.ReadAllText(questFile);
                JsonData dat = JsonMapper.ToObject(content)["Quests"];

                for (int i = 0; i < dat.Count; i++)
                {
                    Quest q = new Quest(dat[i]);
                    GameData.instance.Add<Quest>(q);
                }
            }
        }

    }

    public void ChangeID(string newID)
    {
        id = newID;
    }
}

public static class ModUtility
{
    public const string ModSettingsFileName = "ModSettings.json";
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
        if (m.id == "Core")
        {
            return 0;
        }

        return NextLoadID++;
    }
}

public static class ModManager
{
    public static bool IsInitialized;
    static List<Mod> mods = new List<Mod>();

    public static void InitializeAllMods()
    {
        if (GameData.instance == null)
        {
            new GameData();

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

            LoadItems();
            LoadEntities();
            IsInitialized = true;
        }
    }

    static void LoadItems()
    {
        foreach (Mod m in mods)
        {
            m.LoadSprites();
            m.LoadItemFolderData();
            m.LoadLuaFiles();
        }
    }

    static void LoadEntities()
    {
        foreach (Mod m in mods)
        {
            m.LoadEntityFolderData();
        }
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

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

[MoonSharpUserData]
public static class LuaManager
{
    static Dictionary<string, Script> scripts;
    static string pathToLua = Application.streamingAssetsPath + "/Mods/Core/Lua";
    static bool assemblyRegistered = false;

    public static void AddFile(string s)
    {
        if (!assemblyRegistered)
        {
            RegisterAssembly();
            scripts = new Dictionary<string, Script>();
            assemblyRegistered = true;
        }

        string rawLua = File.ReadAllText(s);
        string modulePath = pathToLua + "/?.lua";
        Script sc = new Script(CoreModules.Preset_SoftSandbox);

        ((ScriptLoaderBase)sc.Options.ScriptLoader).IgnoreLuaPathGlobal = true;
        ((ScriptLoaderBase)sc.Options.ScriptLoader).ModulePaths = ScriptLoaderBase.UnpackStringPaths(modulePath);
        sc.DoString(rawLua);

        string path = Path.GetFileNameWithoutExtension(s);

        if (scripts.ContainsKey(path))
        {
            scripts[path] = sc;
        }
        else
        {
            scripts.Add(path, sc);
        }
    }

    static void RegisterAssembly()
    {
        UserData.RegisterAssembly();

        //Remember to CREATE A STATIC for the type below too!!
        UserData.RegisterType<DamageTypes>();
        UserData.RegisterType<ItemProperty>();
        UserData.RegisterType<AbilityTags>();
        UserData.RegisterType<CastType>();
        UserData.RegisterType<TraitEffects>();
        UserData.RegisterType<NPC_Flags>();
    }

    static void AddEnumsToGlobals(Script sc)
    {
        //Remember to REGISTER the type above too!!
        sc.Globals["DamageTypes"] = UserData.CreateStatic<DamageTypes>();
        sc.Globals["ItemProperty"] = UserData.CreateStatic<ItemProperty>();
        sc.Globals["CastType"] = UserData.CreateStatic<CastType>();
        sc.Globals["AbilityTags"] = UserData.CreateStatic<AbilityTags>();
        sc.Globals["TraitEffects"] = UserData.CreateStatic<TraitEffects>();
        sc.Globals["NPC_Flags"] = UserData.CreateStatic<NPC_Flags>();
    }

    public static DynValue CallScriptFunction(string fileName, string functionName, params object[] parameters)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(functionName))
            return null;

        if (scripts[fileName] != null)
        {
            Script script = scripts[fileName];

            if (script.Globals[functionName] != null)
            {
                DynValue result = script.Call(script.Globals[functionName], parameters);
                return result;
            }
        }

        return null;
    }

    public static DynValue CallScriptFunction(LuaCall luaCall, params object[] parameters)
    {
        return CallScriptFunction(luaCall.scriptName, luaCall.functionName, parameters);
    }

    public static void SetGlobals()
    {

        foreach (Script sc in scripts.Values)
        {
            //Singletons
            sc.Globals["UI"] = World.userInterface;
            sc.Globals["TileMap"] = World.tileMap;
            sc.Globals["WorldMap"] = World.worldMap;
            sc.Globals["TurnManager"] = World.turnManager;
            sc.Globals["ObjectManager"] = World.objectManager;
            sc.Globals["SoundManager"] = World.soundManager;
            sc.Globals["Input"] = ObjectManager.playerEntity.GetComponent<PlayerInput>();
            sc.Globals["Journal"] = ObjectManager.playerJournal;

            //Entities
            sc.Globals["PlayerEntity"] = ObjectManager.playerEntity;
            sc.Globals["NPCs"] = World.objectManager.onScreenNPCObjects;

            //Functions
            sc.Globals["Random"] = (Func<int, int, int>)SeedManager.combatRandom.Next;
            sc.Globals["LocalRandom"] = (Func<int, int, int>)SeedManager.localRandom.Next;
            sc.Globals["WorldRandom"] = (Func<int, int, int>)SeedManager.worldRandom.Next;
            sc.Globals["Log"] = (Action<string>)PrintMessage;
            sc.Globals["Console"] = (Action<string>)ConsoleCommand;
            sc.Globals["Log_Combat"] = (Action<string, string, string, bool>)CombatMessage;
            sc.Globals["SetTile"] = (Action<int, int, string>)ChangeTileInCurrentMap;
            sc.Globals["LocalPath"] = (Func<Coord, Coord, bool, List<Coord>>)Pathfinding.Path_AStar.GetPath;
            sc.Globals["PositionsInCone"] = (Func<Entity, Coord, Coord, int, List<Coord>>)Utility.Cone;
            sc.Globals["GetTile"] = (Func<string, Tile_Data>)Tile.GetByName;

            //Constants
            sc.Globals["LocalMapSize"] = Manager.localMapSize;
            sc.Globals["WorldMapSize"] = Manager.worldMapSize;

            AddEnumsToGlobals(sc);
            AddStaticsToGlobals(sc);
        }
    }

    static void AddStaticsToGlobals(Script sc)
    {
        sc.Globals["SpawnController"] = typeof(SpawnController);
        sc.Globals["World"] = typeof(World);
        sc.Globals["BLine"] = typeof(LineHelper);
        sc.Globals["EntityList"] = typeof(EntityList);
        sc.Globals["ItemList"] = typeof(ItemList);
        sc.Globals["Alert"] = typeof(Alert);
        sc.Globals["LuaManager"] = typeof(LuaManager);
        sc.Globals["TraitList"] = typeof(TraitList);
        sc.Globals["Tile"] = typeof(Tile);
        sc.Globals["SpriteManager"] = typeof(SpriteManager);

        //Models
        sc.Globals["Coord"] = typeof(Coord);
        sc.Globals["NPC"] = typeof(NPC);
        sc.Globals["Damage"] = typeof(Damage);
        sc.Globals["DiceRoll"] = typeof(DiceRoll);
    }

    static void PrintMessage(object message)
    {
        CombatLog.NewMessage(message.ToString());
    }

    static void CombatMessage(string key, string attacker, string defender, bool isPlayer)
    {
        CombatLog.CombatMessage(key, attacker, defender, isPlayer);
    }

    static void ConsoleCommand(string command)
    {
        World.objectManager.GetComponent<Console>().ParseTextField(command);
    }

    static void ChangeTileInCurrentMap(int x, int y, string key)
    {
        World.tileMap.SetTile(Tile.tiles[key], x, y);
    }
}
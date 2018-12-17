using UnityEngine;
using LitJson;
using System.IO;
using System.Collections.Generic;

public static class NPCGroupList
{
    public static List<GroupBlueprint> groupBlueprints;
    public static string dataPath;

    public static void Init()
    {
        groupBlueprints = new List<GroupBlueprint>();

        string listFromJson = File.ReadAllText(Application.streamingAssetsPath + dataPath);

        if (string.IsNullOrEmpty(listFromJson))
        {
            Debug.LogError("NPC Groups null.");
            return;
        }

        JsonData data = JsonMapper.ToObject(listFromJson);

        for (int i = 0; i < data["Groups"].Count; i++)
        {
            JsonData newData = data["Groups"][i];
            groupBlueprints.Add(LoadFromJson(newData));
        }
    }

    static GroupBlueprint LoadFromJson(JsonData data)
    {
        GroupBlueprint blue = new GroupBlueprint
        {
            Name = data["Name"].ToString(),
            level = (data.ContainsKey("Level")) ? (int)data["Level"] : 1,
            depth = (data.ContainsKey("depth")) ? (int)data["Depth"] : 0
        };

        if (data.ContainsKey("Biomes"))
        {
            blue.biomes = new WorldMap.Biome[data["Biomes"].Count];

            for (int i = 0; i < data["Biomes"].Count; i++)
            {
                string b = data["Biomes"][i].ToString();
                blue.biomes[i] = b.ToEnum<WorldMap.Biome>();
            }
        }

        if (data.ContainsKey("Landmarks"))
        {
            blue.landmarks = new string[data["Landmarks"].Count];

            for (int i = 0; i < data["Landmarks"].Count; i++)
            {
                blue.landmarks[i] = data["Landmarks"][i].ToString();
            }
        }

        if (data.ContainsKey("Vaults"))
        {
            blue.vaultTypes = new string[data["Vaults"].Count];

            for (int i = 0; i < data["Vaults"].Count; i++)
            {
                blue.vaultTypes[i] = data["Vaults"][i].ToString();
            }
        }

        for (int j = 0; j < data["Possibilities"].Count; j++)
        {
            SpawnBlueprint esf = new SpawnBlueprint();

            esf.npcID = data["Possibilities"][j]["Blueprint"].ToString();
            esf.Weight = (int)data["Possibilities"][j]["Weight"];

            string amountString = data["Possibilities"][j]["Amount"].ToString();
            string[] segString = amountString.Split("d"[0]);
            int numDice = int.Parse(segString[0]), diceSides = int.Parse(segString[1]);

            esf.minAmount = numDice;
            esf.maxAmount = numDice * diceSides;

            blue.npcs.Add(esf);
        }

        return blue;
    }

    public static GroupBlueprint GetGroupByName(string search)
    {
        for (int i = 0; i < groupBlueprints.Count; i++)
        {
            if (groupBlueprints[i].Name == search)
            {
                return groupBlueprints[i];
            }
        }

        MyConsole.NewMessageColor("GetGroupByName()::ERROR: Could not find group \"" + search + "\"", Color.red);
        Debug.LogError("GetGroupByName()::ERROR: Could not find group \"" + search + "\"");

        return null;
    }
}

using UnityEngine;
using System.Collections.Generic;
using LitJson;

public class GroupBlueprint : IAsset
{
    public string ID { get; set; }
    public int level;
    public int depth;
    public WorldMap.Biome[] biomes;
    public string[] vaultTypes;
    public string[] landmarks;
    public List<SpawnBlueprint> npcs { get; set; }

    public GroupBlueprint(JsonData dat)
    {
        FromJson(dat);
    }

    public bool CanSpawnHere(TileMap_Data tileMapData)
    {
        if (ObjectManager.playerEntity == null)
            return false;

        //Underground
        if (tileMapData.elevation != 0 && tileMapData.vault != null)
        {
            if (vaultTypes == null || level > ObjectManager.playerEntity.stats.MyLevel.CurrentLevel)
            {
                return false;
            }

            for (int i = 0; i < vaultTypes.Length; i++)
            {
                if (tileMapData.vault.blueprint.id == vaultTypes[i] && depth <= Mathf.Abs(tileMapData.elevation))
                {
                    return true;
                }
            }
        }
        else
        {
            //Above Ground
            if (level > ObjectManager.playerEntity.stats.MyLevel.CurrentLevel)
                return false;

            if (!tileMapData.mapInfo.friendly && tileMapData.mapInfo.biome != WorldMap.Biome.Default && biomes != null)
            {
                for (int i = 0; i < biomes.Length; i++)
                {
                    if (biomes[i] == tileMapData.mapInfo.biome)
                    {
                        return true;
                    }
                }
            }

            if (tileMapData.mapInfo.HasLandmark() && landmarks != null)
            {
                for (int i = 0; i < landmarks.Length; i++)
                {
                    if (landmarks[i] == tileMapData.mapInfo.landmark)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    void FromJson(JsonData dat)
    {
        npcs = new List<SpawnBlueprint>();
        ID = dat["Name"].ToString();
        level = (dat.ContainsKey("Level")) ? (int)dat["Level"] : 1;
        depth = (dat.ContainsKey("depth")) ? (int)dat["Depth"] : 0;

        if (dat.ContainsKey("Biomes"))
        {
            biomes = new WorldMap.Biome[dat["Biomes"].Count];

            for (int i = 0; i < dat["Biomes"].Count; i++)
            {
                string b = dat["Biomes"][i].ToString();
                biomes[i] = b.ToEnum<WorldMap.Biome>();
            }
        }

        if (dat.ContainsKey("Landmarks"))
        {
            landmarks = new string[dat["Landmarks"].Count];

            for (int i = 0; i < dat["Landmarks"].Count; i++)
            {
                landmarks[i] = dat["Landmarks"][i].ToString();
            }
        }

        if (dat.ContainsKey("Vaults"))
        {
            vaultTypes = new string[dat["Vaults"].Count];

            for (int i = 0; i < dat["Vaults"].Count; i++)
            {
                vaultTypes[i] = dat["Vaults"][i].ToString();
            }
        }

        for (int j = 0; j < dat["Possibilities"].Count; j++)
        {
            SpawnBlueprint esf = new SpawnBlueprint();

            esf.npcID = dat["Possibilities"][j]["Blueprint"].ToString();
            esf.Weight = (int)dat["Possibilities"][j]["Weight"];

            string amountString = dat["Possibilities"][j]["Amount"].ToString();
            string[] segString = amountString.Split("d"[0]);
            int numDice = int.Parse(segString[0]), diceSides = int.Parse(segString[1]);

            esf.minAmount = numDice;
            esf.maxAmount = numDice * diceSides;

            npcs.Add(esf);
        }
    }
}

public struct SpawnBlueprint : IWeighted
{
    public string npcID { get; set; }
    public int Weight { get; set; }
    public int minAmount { get; set; }
    public int maxAmount { get; set; }

    public SpawnBlueprint(string _npcID, int _spawnChance, int _minAmount, int _maxAmount)
    {
        npcID = _npcID;
        Weight = _spawnChance;
        minAmount = _minAmount;
        maxAmount = _maxAmount;
    }

    public int AmountToSpawn()
    {
        int max = Mathf.Min(10, maxAmount + World.DangerLevel() / 2);
        int a = (max > minAmount) ? SeedManager.combatRandom.Next(minAmount, max) : minAmount;

        a = Mathf.Clamp(a, 0, 7);
        return a;
    }
}

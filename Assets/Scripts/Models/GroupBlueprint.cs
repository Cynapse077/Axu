using UnityEngine;
using System.Collections.Generic;

public class GroupBlueprint
{
    public string Name;
    public int level;
    public int depth;
    public WorldMap.Biome[] biomes;
    public string[] vaultTypes;
    public string[] landmarks;
    public List<SpawnBlueprint> npcs { get; set; }

    public GroupBlueprint()
    {
        Name = "";
        level = 0;
        depth = 0;
        npcs = new List<SpawnBlueprint>();
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

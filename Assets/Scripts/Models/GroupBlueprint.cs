using UnityEngine;
using System.Collections.Generic;
using LitJson;

public class NPCGroup_Blueprint : IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public int level;
    public Biome[] biomes;
    public string[] vaultTypes;
    public string[] landmarks;
    public NPCSpawn_Blueprint[] npcs;

    public NPCGroup_Blueprint(JsonData dat)
    {
        FromJson(dat);
    }

    public bool CanSpawn(int levelInc = 0)
    {
        return ObjectManager.playerEntity != null && level <= ObjectManager.playerEntity.stats.level.CurrentLevel + levelInc;
    }

    public bool CanSpawnHere(TileMap_Data tileMapData)
    {
        if (ObjectManager.playerEntity == null)
            return false;

        if (!CanSpawn())
            return false;

        //Underground
        if (tileMapData.elevation != 0 && tileMapData.vault != null)
        {
            if (vaultTypes == null || level > ObjectManager.playerEntity.stats.level.CurrentLevel)
            {
                return false;
            }

            for (int i = 0; i < vaultTypes.Length; i++)
            {
                if (tileMapData.vault.blueprint.ID == vaultTypes[i])
                {
                    return true;
                }
            }
        }
        else
        {
            //Above Ground
            if (!tileMapData.mapInfo.friendly && tileMapData.mapInfo.biome != Biome.Default && biomes != null)
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

    public void FromJson(JsonData dat)
    {
        ID = dat["Name"].ToString();
        dat.TryGetInt("Level", out level, 1);

        if (dat.ContainsKey("Biomes"))
        {
            biomes = new Biome[dat["Biomes"].Count];

            for (int i = 0; i < dat["Biomes"].Count; i++)
            {
                string b = dat["Biomes"][i].ToString();
                biomes[i] = b.ToEnum<Biome>();
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

        if (dat.ContainsKey("Possibilities"))
        {
            npcs = new NPCSpawn_Blueprint[dat["Possibilities"].Count];

            for (int i = 0; i < dat["Possibilities"].Count; i++)
            {
                string amountString = dat["Possibilities"][i]["Amount"].ToString();
                string[] segString = amountString.Split('d');
                int numDice = int.Parse(segString[0]), diceSides = int.Parse(segString[1]);

                NPCSpawn_Blueprint esf = new NPCSpawn_Blueprint
                {
                    npcID = dat["Possibilities"][i]["Blueprint"].ToString(),
                    Weight = (int)dat["Possibilities"][i]["Weight"],
                    range = new IntRange(numDice, numDice * diceSides)
                };

                npcs[i] = esf;
            }
        }        
    }

    public IEnumerable<string> LoadErrors()
    {
        if (npcs == null)
        {
            yield return "No NPCs added to encounter.";
        }
    }
}

public struct NPCSpawn_Blueprint : IWeighted
{
    const int AbsMax = 7;

    public string npcID { get; set; }
    public int Weight { get; set; }
    public IntRange range;

    public NPCSpawn_Blueprint(string _npcID, int _spawnChance, int _minAmount, int _maxAmount)
    {
        npcID = _npcID;
        Weight = _spawnChance;
        range = new IntRange(_minAmount, _maxAmount);
    }

    public int AmountToSpawn(System.Random rand)
    {
        int max = Mathf.Min(AbsMax, range.max + World.DangerLevel() / 2);
        int a = (max > range.min) ? rand.Next(range.min, max) : range.min;

        a = Mathf.Clamp(a, 1, AbsMax);
        return a;
    }
}

using UnityEngine;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class SpawnController
{
    static System.Random rng { get { return SeedManager.combatRandom; } }

    public static void SpawnStaticNPCs()
    {
        List<NPC_Blueprint> static_npcs = EntityList.npcs.FindAll(x => x.flags.Contains(NPC_Flags.Static));

        foreach (NPC_Blueprint bp in static_npcs)
        {
            if (bp.zone != "")
            {
                NPC npc = new NPC(bp, new Coord(0, 0), new Coord(0, 0), 0);

                if (bp.zone.Contains("Random_"))
                {
                    npc.elevation = 0;
                    npc.localPosition = new Coord(Random.Range(0, Manager.localMapSize.x), Random.Range(0, Manager.localMapSize.y));

                    string biome = bp.zone.Replace("Random_", "");
                    WorldMap.Biome b = biome.ToEnum<WorldMap.Biome>();
                    npc.worldPosition = World.worldMap.worldMapData.GetRandomFromBiome(b);
                }
                else
                {
                    npc.localPosition = bp.localPosition;
                    npc.elevation = bp.elevation;
                    npc.worldPosition = (bp.zone == "Unassigned") ? new Coord(-1, -1) : World.tileMap.worldMap.GetLandmark(bp.zone);
                }

                World.objectManager.CreateNPC(npc);
            }
        }
    }

    public static void SpawnAttackers()
    {
        List<GroupBlueprint> bps = new List<GroupBlueprint>();

        for (int i = 0; i < NPCGroupList.groupBlueprints.Count; i++)
        {
            GroupBlueprint bp = NPCGroupList.groupBlueprints[i];

            if (bp.Name.Contains("Bandits") && bp.level <= World.DangerLevel())
                bps.Add(bp);
        }

        if (bps.Count <= 0)
        {
            return;
        }

        GroupBlueprint spawn = bps.GetRandom();
        int amount = rng.Next(2, 7);

        SpawnFromGroupName(spawn.Name, amount);

        World.tileMap.CheckNPCTiles();
    }

    public static void BiomeSpawn(int mx, int my, TileMap_Data mapData)
    {
        if (mapData.elevation == 0)
        {
            if (mapData.houses.Count > 0)
            {
                HouseObjects();
            }

            int numSpawns = mapData.mapInfo.friendly ? rng.Next(1, 4) : rng.Next(0, 4);

            if (!mapData.mapInfo.friendly)
            {
                if (rng.Next(100) < 60)
                {
                    numSpawns = 0;
                }

                Encounter();
            }

            if (numSpawns > 0)
            {
                List<GroupBlueprint> gbs = GroupsThatCanSpawnHere(mapData);

                if (gbs.Count <= 0)
                {
                    return;
                }

                GroupBlueprint gb = gbs.GetRandom(rng);

                for (int i = 0; i < numSpawns; i++)
                {
                    SpawnBlueprint s = Utility.WeightedChoice(gb.npcs);
                    int amount = s.AmountToSpawn();

                    for (int j = 0; j < amount; j++)
                    {
                        Coord c = World.tileMap.CurrentMap.GetRandomFloorTile();

                        if (c != null)
                        {
                            NPC n = new NPC(s.npcID, mapData.mapInfo.position, c, mapData.elevation);
                            World.objectManager.SpawnNPC(n);
                        }
                    }
                }
            }
        }
        else
        {
            SpawnUndergroundEnemies(mapData);
        }

        World.tileMap.CheckNPCTiles();
    }

    static List<GroupBlueprint> GroupsThatCanSpawnHere(TileMap_Data mapData)
    {
        return NPCGroupList.groupBlueprints.FindAll(x => x.CanSpawnHere(mapData));
    }

    static void Encounter()
    {
        //crystal
        if (rng.Next(1000) <= 2)
        {
            Coord c = World.tileMap.CurrentMap.GetRandomFloorTile();

            if (c != null)
            {
                World.objectManager.NewObjectAtOtherScreen("Crystal", c, World.tileMap.CurrentMap.mapInfo.position, 0);
                return;
            }
        }

        //Random minibosses.
        if (World.DangerLevel() >= 6 && rng.Next(1000) < 5)
        {
            List<GroupBlueprint> bps = new List<GroupBlueprint>();

            for (int i = 0; i < NPCGroupList.groupBlueprints.Count; i++)
            {
                GroupBlueprint bp = NPCGroupList.groupBlueprints[i];

                if (bp.Name.Contains("Minibosses") && bp.level <= ObjectManager.playerEntity.stats.MyLevel.CurrentLevel)
                {
                    bps.Add(bp);
                }
            }

            if (bps.Count > 0)
            {
                SpawnFromGroupName(bps.GetRandom().Name);
            }
        }
    }

    static void HouseObjects()
    {
        if (World.tileMap.CurrentMap.loadedFromData)
            return;

        int numHouses = World.tileMap.CurrentMap.houses.Count;

        for (int r = 0; r < numHouses; r++)
        {
            string npcID = "villager";

            switch (World.tileMap.CurrentMap.houses[r].houseType)
            {
                case House.HouseType.Villager:
                    npcID = "villager";
                    break;
                case House.HouseType.Merchant:
                    npcID = "merch";
                    break;
                case House.HouseType.Doctor:
                    npcID = "doctor";
                    break;
            }

            World.objectManager.BuildingObjects(World.tileMap.CurrentMap.houses[r], World.tileMap.CurrentMap.mapInfo.position, npcID);
        }
    }

    //Summon a random minion from a particular group
    public static void SummonFromGroup(string groupName, Coord localPosition)
    {
        GroupBlueprint gbp = NPCGroupList.GetGroupByName(groupName);
        SpawnBlueprint chosenSpawn = Utility.WeightedChoice(gbp.npcs);

        NPC n = EntityList.GetNPCByID(chosenSpawn.npcID, World.tileMap.CurrentMap.mapInfo.position, localPosition);
        n.elevation = World.tileMap.currentElevation;

        if (n.HasFlag(NPC_Flags.Summon_Adds))
        {
            n.flags.Remove(NPC_Flags.Summon_Adds);
        }

        n.inventory = new List<Item>();
        n.isHostile = true;
        n.hasSeenPlayer = true;
        World.objectManager.SpawnNPC(n);
    }

    static void SpawnUndergroundEnemies(TileMap_Data map)
    {
        if (map.visited)
        {
            return;
        }

        Vault v = World.tileMap.GetVaultAt(World.tileMap.WorldPosition);

        //Don't do random spawns on these floors. Will need a way to determine this in the json file
        if (v.blueprint.id == "Prison")
        {
            int ele = Mathf.Abs(map.elevation);

            if (ele >= 5 || ele == 1)
            {
                return;
            }
        }

        List<GroupBlueprint> gbs = GroupsThatCanSpawnHere(map);

        if (gbs.Count > 0)
        {
            GroupBlueprint gb = gbs.GetRandom(rng);
            int amountSpawned = 0;
            int maxSpawns = 15;

            for (int i = 0; i < rng.Next(1, 5); i++)
            {
                if (amountSpawned >= maxSpawns)
                {
                    break;
                }

                SpawnBlueprint s = Utility.WeightedChoice(gb.npcs);
                int amount = Mathf.Clamp(s.AmountToSpawn(), 1, 7);

                for (int j = 0; j < amount; j++)
                {
                    if (amountSpawned >= maxSpawns)
                    {
                        break;
                    }

                    Coord c = World.tileMap.CurrentMap.GetRandomFloorTile();

                    if (c != null)
                    {
                        NPC n = new NPC(s.npcID, map.mapInfo.position, c, -map.elevation);
                        World.objectManager.SpawnNPC(n);
                        amountSpawned++;
                    }
                }
            }
        }

        if (v != null && v.blueprint.id == "Cave_Ice")
        {
            SpawnObject("Ore", SeedManager.localRandom.Next(2, 5));
        }

        if (rng.Next(100) < 5)
        {
            SpawnObject("Chest", (SeedManager.combatRandom.Next(100) < 10) ? 2 : 1);
        }
    }

    public static void SpawnObject(string obT, int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            Coord pos = World.tileMap.CurrentMap.GetRandomFloorTile();

            if (pos != null)
            {
                World.objectManager.NewObject(obT, pos);
            }
        }
    }

    public static NPC SpawnNPCByID(string npcID, Coord worldPos, int elevation = 0, Coord localPos = null)
    {
        NPC npc;
        Coord defaultCoord = new Coord(SeedManager.combatRandom.Next(1, Manager.localMapSize.x - 2), SeedManager.combatRandom.Next(3, Manager.localMapSize.y - 2));

        //Move NPC, or create new if they have not been spawned yet.
        if (World.objectManager.npcClasses.Find(x => x.ID == npcID && x.HasFlag(NPC_Flags.Static)) != null)
        {
            npc = World.objectManager.npcClasses.Find(x => x.ID == npcID && x.HasFlag(NPC_Flags.Static));
            npc.localPosition = localPos ?? defaultCoord;

            npc.worldPosition = worldPos;
            npc.elevation = elevation;
        }
        else
        {
            npc = EntityList.GetNPCByID(npcID, worldPos, localPos ?? defaultCoord);
            npc.elevation = elevation;
            World.objectManager.CreateNPC(npc);
        }

        return npc;
    }

    public static bool HasFoundEncounter(float chanceMultiplier = 1.0f)
    {
        if (Manager.noEncounters || Random.value >= (0.015f * chanceMultiplier))
        {
            return false;
        }

        MapInfo mi = World.tileMap.worldMap.GetTileAt(World.tileMap.worldCoordX, World.tileMap.worldCoordY);

        return (mi.biome != WorldMap.Biome.Ocean && !mi.friendly);
    }

    public static void SpawnFromGroupName(string name, int amount = 1)
    {
        GroupBlueprint gbp = NPCGroupList.GetGroupByName(name);
        SpawnSingleGroup(gbp, amount);
    }

    public static List<NPC> SpawnFromGroupNameAt(string name, int amount, Coord position, int elevation)
    {
        GroupBlueprint gbp = NPCGroupList.GetGroupByName(name);
        SpawnBlueprint chosenSpawn = Utility.WeightedChoice(gbp.npcs);
        List<NPC> spawned = new List<NPC>();

        if (amount == 0)
        {
            amount = chosenSpawn.AmountToSpawn();
        }

        for (int j = 0; j < amount; j++)
        {
            chosenSpawn = Utility.WeightedChoice(gbp.npcs);
            Coord stPos = World.tileMap.CurrentMap.GetRandomFloorTile();

            if (stPos == null)
            {
                stPos = new Coord(rng.Next(0, Manager.localMapSize.x - 1), rng.Next(0, Manager.localMapSize.y - 1));
            }

            if (stPos != null)
            {
                NPC n = EntityList.GetNPCByID(chosenSpawn.npcID, position, stPos, elevation);
                World.objectManager.SpawnNPC(n);
                spawned.Add(n);
            }
        }

        return spawned;
    }

    static void SpawnSingleGroup(GroupBlueprint gbp, int amount = 1)
    {
        SpawnBlueprint chosenSpawn = Utility.WeightedChoice(gbp.npcs);

        if (amount == 0)
        {
            amount = chosenSpawn.AmountToSpawn();
        }

        for (int j = 0; j < amount; j++)
        {
            chosenSpawn = Utility.WeightedChoice(gbp.npcs);
            NPC npcToSpawn = EntityList.GetNPCByID(chosenSpawn.npcID, World.tileMap.CurrentMap.mapInfo.position, World.tileMap.CurrentMap.GetRandomFloorTile());
            World.objectManager.SpawnNPC(npcToSpawn);
        }
    }
}

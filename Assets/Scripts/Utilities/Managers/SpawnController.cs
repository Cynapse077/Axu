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
                    Biome b = biome.ToEnum<Biome>();
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

    static void SpawnBanditAmbush()
    {
        Item item = null;
        Entity entity = ObjectManager.playerEntity;
        int goldAmount = (entity.inventory.gold > 0) ? Random.Range(entity.inventory.gold / 2, entity.inventory.gold + 1) : 100;

        if (entity.inventory.items.Count > 0 && rng.CoinFlip())
        {
            item = entity.inventory.items.GetRandom();
        }

        if (item != null && SeedManager.combatRandom.CoinFlip())
        {
            World.userInterface.YesNoAction("YN_BanditAmbush_Item".Translate(), () =>
            {
                World.userInterface.BanditYes(goldAmount, item);
                entity.inventory.RemoveInstance_All(item);
            }, () => World.userInterface.BanditNo(), item.DisplayName());
        }
        else
        {
            World.userInterface.YesNoAction("YN_BanditAmbush".Translate(), () =>
            {
                World.userInterface.BanditYes(goldAmount, item);
                entity.inventory.gold -= goldAmount;
            }, () => World.userInterface.BanditNo(), goldAmount.ToString());
        }

        List<NPCGroup_Blueprint> bps = new List<NPCGroup_Blueprint>();

        foreach (NPCGroup_Blueprint gb in GameData.GetAll<NPCGroup_Blueprint>())
        {
            if (gb.ID.Contains("Bandits") && gb.level <= World.DangerLevel())
            {
                bps.Add(gb);
            }
        }

        if (bps.Count <= 0)
        {
            return;
        }

        NPCGroup_Blueprint spawn = bps.GetRandom();
        int amount = rng.Next(2, 7);

        SpawnFromGroupName(spawn.ID, amount);
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

            //Bandit ambush in village
            if (!mapData.mapInfo.landmark.NullOrEmpty() && mapData.mapInfo.landmark.Contains("Village"))
            {
                if (rng.OneIn(300))
                {
                    CombatLog.NewMessage("You hear a commition...");
                    NPCGroup_Blueprint gb = GameData.GetRandom<NPCGroup_Blueprint>((a) => {
                        if (!(a is NPCGroup_Blueprint asset))
                            return false;

                        return asset.ID.Contains("Bandit") && asset.CanSpawn(2);
                    });

                    if (gb != null)
                    {
                        for (int i = 0; i < rng.Next(2, 6); i++)
                        {
                            NPCSpawn_Blueprint s = Utility.WeightedChoice(gb.npcs);
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
            }

            if (numSpawns > 0)
            {
                List<NPCGroup_Blueprint> gbs = GroupsThatCanSpawnHere(mapData);

                if (gbs.Count <= 0)
                {
                    return;
                }

                NPCGroup_Blueprint gb = gbs.GetRandom(rng);

                for (int i = 0; i < numSpawns; i++)
                {
                    NPCSpawn_Blueprint s = Utility.WeightedChoice(gb.npcs);
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

    static List<NPCGroup_Blueprint> GroupsThatCanSpawnHere(TileMap_Data mapData)
    {
        return GameData.GetAll<NPCGroup_Blueprint>().FindAll(x => x.CanSpawnHere(mapData));
    }

    static void Encounter()
    {
        //crystal
        if (rng.Next(1000) <= 2)
        {
            Coord c = World.tileMap.CurrentMap.GetRandomFloorTile();

            if (c != null)
            {
                World.objectManager.NewObjectAtSpecificScreen("Crystal", c, World.tileMap.CurrentMap.mapInfo.position, 0);
                return;
            }
        }

        //Random minibosses.
        if (World.DangerLevel() >= 6 && rng.Next(1000) < ObjectManager.playerEntity.stats.level.CurrentLevel + 1)
        {
            List<NPCGroup_Blueprint> bps = new List<NPCGroup_Blueprint>();

            foreach (NPCGroup_Blueprint gb in GameData.GetAll<NPCGroup_Blueprint>())
            {
                if (gb.ID.Contains("Minibosses") && gb.level <= ObjectManager.playerEntity.stats.level.CurrentLevel)
                {
                    bps.Add(gb);
                }
            }

            if (bps.Count > 0)
            {
                SpawnFromGroupName(bps.GetRandom().ID);
            }
        }
    }

    public static bool SetupOverworldEncounter()
    {
        Entity entity = ObjectManager.playerEntity;

        bool canSpawnBanditAmbush = GameData.Get<Faction>("bandits") != null && !ObjectManager.playerEntity.inventory.DisguisedAs(GameData.Get<Faction>("bandits"));

        if (CanSpawnIncident() && rng.Next(100) < 50)
        {
            bool p(IAsset asset)
            {
                if (asset is Incident inc)
                {
                    return inc.CanSpawn();
                }

                return false;
            }

            var incident = GameData.Get<Incident>(p).GetRandom();
            incident.Spawn();
        }
        else if (canSpawnBanditAmbush)
        {
            SpawnBanditAmbush();
        }
        else
        {
            return false;
        }

        World.tileMap.CheckNPCTiles();
        World.tileMap.HardRebuild();
        World.objectManager.NoStickNPCs();
        return true;
    }

    static bool CanSpawnIncident()
    {
        var incidents = GameData.GetAll<Incident>();

        if (incidents.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < incidents.Count; i++)
        {
            if (incidents[i].CanSpawn())
            {
                return true;
            }
        }

        return false;
    }

    static void HouseObjects()
    {
        if (World.tileMap.CurrentMap.loadedFromData)
        {
            return;
        }

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
        NPCGroup_Blueprint gbp = GameData.Get<NPCGroup_Blueprint>(groupName);
        NPCSpawn_Blueprint chosenSpawn = Utility.WeightedChoice(gbp.npcs);

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
        if (v.blueprint.ID == "Prison")
        {
            int ele = Mathf.Abs(map.elevation);

            if (ele >= 5 || ele == 1)
            {
                return;
            }
        }

        List<NPCGroup_Blueprint> gbs = GroupsThatCanSpawnHere(map);

        if (gbs.Count > 0)
        {
            NPCGroup_Blueprint gb = gbs.GetRandom(rng);
            int amountSpawned = 0;
            int maxSpawns = 15;

            for (int i = 0; i < rng.Next(1, 5); i++)
            {
                if (amountSpawned >= maxSpawns)
                {
                    break;
                }

                NPCSpawn_Blueprint s = Utility.WeightedChoice(gb.npcs);
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

        if (v != null && v.blueprint.ID == "Cave_Ice")
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
                World.objectManager.NewObjectOnCurrentScreen(obT, pos);
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

        return (mi.biome != Biome.Ocean && !mi.friendly);
    }

    public static void SpawnFromGroupName(string name, int amount = 1)
    {
        NPCGroup_Blueprint gbp = GameData.Get<NPCGroup_Blueprint>(name);

        if (gbp != null)
        {
            SpawnSingleGroup(gbp, amount);
        }
    }

    public static List<NPC> SpawnFromGroupNameAt(string name, int amount, Coord position, int elevation)
    {
        NPCGroup_Blueprint gbp = GameData.Get<NPCGroup_Blueprint>(name);
        NPCSpawn_Blueprint chosenSpawn = Utility.WeightedChoice(gbp.npcs);
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

    static void SpawnSingleGroup(NPCGroup_Blueprint gbp, int amount = 1)
    {
        NPCSpawn_Blueprint chosenSpawn = Utility.WeightedChoice(gbp.npcs);

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

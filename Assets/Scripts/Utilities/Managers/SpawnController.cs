using UnityEngine;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class SpawnController
{
    public static void SpawnStaticNPCs()
    {
        List<NPC_Blueprint> static_npcs = EntityList.npcs.FindAll(x => x.flags.Contains(NPC_Flags.Static));
        foreach (NPC_Blueprint bp in static_npcs)
        {
            if (!bp.zone.NullOrEmpty())
            {
                NPC npc = new NPC(bp, new Coord(), new Coord(), 0);
                if (bp.zone.Contains("Random_"))
                {
                    npc.elevation = 0;
                    npc.localPosition = Coord.RandomInLocalBounds();

                    string biome = bp.zone.Replace("Random_", "");
                    Biome b = biome.ToEnum<Biome>();
                    npc.worldPosition = World.worldMap.worldMapData.GetRandomFromBiome(b);
                }
                else
                {
                    npc.localPosition = bp.localPosition;
                    npc.elevation = bp.elevation;
                    npc.worldPosition = (bp.zone == "Unassigned") ? new Coord(-1) : World.tileMap.worldMap.GetLandmark(bp.zone);
                }

                World.objectManager.CreateNPC(npc);
            }
        }
    }

    static void SpawnBanditAmbush()
    {
        Item item = null;
        Entity entity = ObjectManager.playerEntity;
        int goldAmount = (entity.inventory.gold > 0) ? RNG.Next(entity.inventory.gold / 2, entity.inventory.gold + 1) : 100;

        if (entity.inventory.items.Count > 0 && RNG.CoinFlip())
        {
            item = entity.inventory.items.GetRandom();
        }

        if (item != null && RNG.CoinFlip())
        {
            World.userInterface.YesNoAction("YN_BanditAmbush_Item".Localize(), () =>
            {
                World.userInterface.BanditYes(goldAmount, item);
                entity.inventory.RemoveInstance_All(item);
            }, () => World.userInterface.BanditNo(), item.DisplayName());
        }
        else
        {
            World.userInterface.YesNoAction("YN_BanditAmbush".Localize(), () =>
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
        int amount = RNG.Next(2, 7);
        SpawnFromGroupName(spawn.ID, amount);

        World.tileMap.CheckNPCTiles();
        World.tileMap.HardRebuild();
        World.objectManager.NoStickNPCs();
    }

    public static void BiomeSpawn(TileMap_Data mapData)
    {
        if (mapData.elevation == 0)
        {
            if (mapData.houses.Count > 0)
            {
                HouseObjects();
            }

            int numSpawns = mapData.mapInfo.friendly ? SeedManager.localRandom.Next(1, 4) : SeedManager.localRandom.Next(0, 4);

            if (!mapData.mapInfo.friendly)
            {
                if (SeedManager.localRandom.Next(100) < 55)
                {
                    numSpawns = 0;
                }

                Encounter();
            }

            //Bandit ambush in village
            if (!mapData.mapInfo.landmark.NullOrEmpty() && mapData.mapInfo.landmark.Contains("Village"))
            {
                if (RNG.OneIn(300))
                {
                    CombatLog.NewMessage("You hear a commition...");

                    NPCGroup_Blueprint gb = GameData.GetRandom<NPCGroup_Blueprint>((a) => a is NPCGroup_Blueprint asset && asset.ID.Contains("Bandit") && asset.CanSpawn(2));
                    if (gb != null)
                    {
                        for (int i = 0; i < RNG.Next(2, 6); i++)
                        {
                            NPCSpawn_Blueprint s = Utility.WeightedChoice(gb.npcs);

                            int amount = s.AmountToSpawn(SeedManager.combatRandom);
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

                NPCGroup_Blueprint gb = gbs.GetRandom();
                for (int i = 0; i < numSpawns; i++)
                {
                    NPCSpawn_Blueprint s = Utility.WeightedChoice(gb.npcs);

                    int amount = s.AmountToSpawn(SeedManager.combatRandom);
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
        if (RNG.OneIn(500))
        {
            Coord c = World.tileMap.CurrentMap.GetRandomFloorTile();

            if (c != null)
            {
                World.objectManager.NewObjectAtSpecificScreen("Crystal", c, World.tileMap.CurrentMap.mapInfo.position, 0);
                return;
            }
        }

        //Random minibosses.
        if (World.DangerLevel() >= 3 && RNG.Next(500) < ObjectManager.playerEntity.stats.level.CurrentLevel + 1)
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

    public static void SetupOverworldEncounter(Incident forcedIncident = null)
    {
        Entity entity = ObjectManager.playerEntity;

        bool canSpawnBanditAmbush = GameData.TryGet("bandits", out Faction bandits) && !ObjectManager.playerEntity.inventory.DisguisedAs(bandits);

        if (CanSpawnIncident() && RNG.CoinFlip())
        {
            var incidents = GameData.Get<Incident>((IAsset asset) => asset is Incident inc && inc.CanSpawn());
            if (incidents.Count == 0 && canSpawnBanditAmbush)
            {
                SpawnBanditAmbush();
            }
            else
            {
                DoIncident(incidents.WeightedChoice());
            }
        }
        else if (canSpawnBanditAmbush)
        {
            SpawnBanditAmbush();
        }
    }

    public static void DoIncident(Incident incident)
    {
        if (incident == null)
        {
            return;
        }

        incident.Spawn();

        World.tileMap.HardRebuild();
        World.tileMap.CheckNPCTiles();
        World.objectManager.NoStickNPCs();
        World.tileMap.LightCheck();
    }

    static bool CanSpawnIncident()
    {
        var incidents = GameData.GetAll<Incident>();
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
        if (map.visited && World.turnManager.turn > map.lastTurnSeen + GameSettings.RespawnTime)
        {
            return;
        }

        //Don't do random spawns on these floors.
        Vault v = World.tileMap.GetVaultAt(World.tileMap.WorldPosition);
        if (v.blueprint.excludeSpawnsOn != null)
        {
            int ele = Mathf.Abs(map.elevation);
            for (int i = 0; i < v.blueprint.excludeSpawnsOn.Length; i++)
            {
                if (ele == v.blueprint.excludeSpawnsOn[i])
                {
                    return;
                }
            }
        }

        List<NPCGroup_Blueprint> gbs = GroupsThatCanSpawnHere(map);
        if (gbs.Count > 0)
        {
            NPCGroup_Blueprint gb = gbs.GetRandom();
            int amountSpawned = 0;
            int maxSpawns = 15;

            for (int i = 0; i < RNG.Next(1, 5); i++)
            {
                if (amountSpawned >= maxSpawns)
                {
                    break;
                }

                NPCSpawn_Blueprint s = Utility.WeightedChoice(gb.npcs);
                int amount = Mathf.Clamp(s.AmountToSpawn(SeedManager.localRandom), 1, 7);

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

        if (SeedManager.localRandom.Next(100) < 5)
        {
            SpawnObject("Chest", (SeedManager.localRandom.Next(100) < 10) ? 2 : 1);
        }
        else if (SeedManager.localRandom.Next(200) == 0)
        {
            SpawnObject("Chest_Large");
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
        Coord defaultCoord = new Coord(RNG.Next(1, Manager.localMapSize.x - 2), RNG.Next(3, Manager.localMapSize.y - 2));

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

        return mi.biome != Biome.Ocean && !mi.friendly;
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
            amount = chosenSpawn.AmountToSpawn(SeedManager.localRandom);
        }

        for (int j = 0; j < amount; j++)
        {
            chosenSpawn = Utility.WeightedChoice(gbp.npcs);
            Coord stPos = World.tileMap.CurrentMap.GetRandomFloorTile();

            if (stPos == null)
            {
                stPos = new Coord(RNG.Next(0, Manager.localMapSize.x - 1), RNG.Next(0, Manager.localMapSize.y - 1));
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
            amount = chosenSpawn.AmountToSpawn(SeedManager.localRandom);
        }

        for (int j = 0; j < amount; j++)
        {
            chosenSpawn = Utility.WeightedChoice(gbp.npcs);
            NPC npcToSpawn = EntityList.GetNPCByID(chosenSpawn.npcID, World.tileMap.CurrentMap.mapInfo.position, World.tileMap.CurrentMap.GetRandomFloorTile());
            World.objectManager.SpawnNPC(npcToSpawn);
        }
    }
}

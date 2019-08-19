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

    public static void SpawnBanditAmbush()
    {
        List<GroupBlueprint> bps = new List<GroupBlueprint>();

        foreach (GroupBlueprint gb in GameData.GetAll<GroupBlueprint>())
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

        GroupBlueprint spawn = bps.GetRandom();
        int amount = rng.Next(2, 7);

        SpawnFromGroupName(spawn.ID, amount);

        World.tileMap.CheckNPCTiles();
    }

    public static void SpawnMerchant()
    {
        Coord c = World.tileMap.CurrentMap.GetRandomFloorTile();

        if (c != null)
        {
            SpawnNPCByID("merch", World.tileMap.CurrentMap.mapInfo.position, 0, c);
            return;
        }

        int guards = 0;

        for (int x = -3; x <= 3; x++)
        {
            for (int y = -3; y <= 3; y++)
            {
                if (World.OutOfLocalBounds(c.x + x, c.y + y) || guards > 5)
                {
                    continue;
                }

                if (World.tileMap.GetCellAt(c.x + x, c.y + y).Walkable && rng.Next(100) < 30)
                {
                    Coord guardPos = new Coord(c.x + x, c.y + y);

                    SpawnNPCByID("guard", World.tileMap.CurrentMap.mapInfo.position, 0, guardPos);
                    guards++;
                }
            }
        }
    }

    public static void SpawnEversightAmbush()
    {
        List<GroupBlueprint> bps = new List<GroupBlueprint>();

        foreach (GroupBlueprint gb in GameData.GetAll<GroupBlueprint>())
        {
            if (gb.ID.Contains("Eversight") && gb.level <= World.DangerLevel())
            {
                bps.Add(gb);
            }
        }

        if (bps.Count <= 0)
        {
            return;
        }

        GroupBlueprint spawn = bps.GetRandom();
        int amount = rng.Next(2, 5);

        SpawnFromGroupName(spawn.ID, amount);

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
        return GameData.GetAll<GroupBlueprint>().FindAll(x => x.CanSpawnHere(mapData));
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

        //Spawn a merchant and his guards.
        if (rng.Next(1000) <= 2)
        {
            SpawnMerchant();
            return;
        }

        //Random minibosses.
        if (World.DangerLevel() >= 6 && rng.Next(1000) < 5)
        {
            List<GroupBlueprint> bps = new List<GroupBlueprint>();

            foreach (GroupBlueprint gb in GameData.GetAll<GroupBlueprint>())
            {
                if (gb.ID.Contains("Minibosses") && gb.level <= ObjectManager.playerEntity.stats.MyLevel.CurrentLevel)
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

    public static void SetupOverworldEncounter()
    {
        Entity entity = ObjectManager.playerEntity;

        if (World.difficulty.Level == Difficulty.DiffLevel.Hunted && rng.Next(500) < World.DangerLevel())
        {
            //Spawn Eversight Assassins
            SpawnEversightAmbush();
            Alert.CustomAlert_WithTitle("Ambush!", "A group of Eversight Assassins have snuck up on you. Prepare to fight!");
        }
        else
        {
            //Merchant
            if (rng.Next(100) < 10)
            {
                Alert.NewAlert("FoundMerchant");
                SpawnMerchant();              
            }
            else
            {
                //Spawn Bandits
                Item item = null;
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

                SpawnBanditAmbush();
            }
        }

        World.tileMap.HardRebuild();
        World.objectManager.NoStickNPCs();
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
        GroupBlueprint gbp = GameData.Get<GroupBlueprint>(groupName) as GroupBlueprint;
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
        if (v.blueprint.ID == "Prison")
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
        GroupBlueprint gbp = GameData.Get<GroupBlueprint>(name) as GroupBlueprint;
        SpawnSingleGroup(gbp, amount);
    }

    public static List<NPC> SpawnFromGroupNameAt(string name, int amount, Coord position, int elevation)
    {
        GroupBlueprint gbp = GameData.Get<GroupBlueprint>(name) as GroupBlueprint;
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

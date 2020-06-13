using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Console : MonoBehaviour
{
    Entity playerEntity;
    Stats playerStats;
    Inventory playerInventory;
    string textField = "";

    void Update()
    {
        if (ObjectManager.doneLoading && playerInventory == null)
        {
            if (ObjectManager.player != null)
            {
                if (playerEntity == null)
                    playerEntity = ObjectManager.playerEntity;
                if (playerStats == null)
                    playerStats = playerEntity.gameObject.GetComponent<Stats>();
                if (playerInventory == null)
                    playerInventory = playerEntity.gameObject.GetComponent<Inventory>();
            }
        }

        if ((Input.GetKeyUp(KeyCode.End) || Input.GetKeyUp(KeyCode.BackQuote)) && !UserInterface.showConsole && GameSettings.Allow_Console)
        {
            UserInterface.showConsole = true;
        }
    }

    void OnGUI()
    {
        if (UserInterface.showConsole)
        {
            if (Event.current.type == EventType.Layout)
            {
                if (Event.current.keyCode == KeyCode.Return)
                    SendToParse();
                if (Event.current.keyCode == KeyCode.Escape)
                    UserInterface.showConsole = false;
                if (Event.current.keyCode == KeyCode.BackQuote)
                    UserInterface.showConsole = false;
            }

            Rect consoleOutline = new Rect(5, 5, 420, 470);
            GUI.Box(consoleOutline, "");
            GUI.Label(new Rect(110, 5, 200, 25), "<b><color=#ffa500ff>[ CONSOLE ]</color></b>");
            MyConsole.DrawConsole(new Rect(15, 30, 400, 400));
            GUI.SetNextControlName("TextField");
            textField = GUI.TextField(new Rect(15, 435, 350, 25), textField);
            GUI.FocusControl("TextField");

            if (GUI.Button(new Rect(367, 435, 50, 25), "Enter"))
            {
                SendToParse();
            }
            if (GUI.Button(new Rect(387, 8, 25, 18), "x"))
            {
                UserInterface.showConsole = false;
            }
        }
    }

    void SendToParse()
    {
        if (textField != "" && textField != " ")
        {
            MyConsole.NewMessage("> <i>" + textField + "</i>");
            ParseTextField(textField);
            textField = "";
        }
    }

    //Input string parsing for debug console
    public void ParseTextField(string textToParse)
    {
        string[] parsedText = textToParse.Split(" "[0]);

        switch (parsedText[0])
        {
            case "load":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Speify a map name.");
                    return;
                }

                World.objectManager.DeleteNPCsAt(World.tileMap.WorldPosition, World.tileMap.currentElevation);
                World.objectManager.DeleteObjectsAt(World.tileMap.WorldPosition, World.tileMap.currentElevation);
                World.tileMap.LoadMap(parsedText[1]);
                MyConsole.NewMessage("Map " + parsedText[1] + " loaded.");
                break;

            case "bodydrop":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Speify a drop chance (integer).");
                    return;
                }

                if (int.TryParse(parsedText[1], out int o))
                {
                    Inventory.BodyDropChance = o;
                    MyConsole.NewMessage("Set body drop chance to " + parsedText[1] + "%");
                }

                break;

            case "5k":
                World.turnManager.IncrementTime(5000);
                World.tileMap.HardRebuild();
                break;

            case "10k":
                World.turnManager.IncrementTime(10000);
                World.tileMap.HardRebuild();
                break;

            case "alltraits":
                for (int i = 0; i < GameData.GetAll<Trait>().Count; i++)
                {
                    playerEntity.stats.GiveTrait(GameData.GetAll<Trait>()[i].ID);
                }
                break;

            case "set":
                int pOut = 0;

                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify what to set, then add a value.");
                }
                else if (parsedText.Length < 3)
                {
                    MyConsole.Error("Specify a value.");
                }
                else if (int.TryParse(parsedText[2], out pOut))
                {
                    if (parsedText[1] == "level")
                        playerStats.level.CurrentLevel = pOut;
                    else if (parsedText[1] == "hp" || parsedText[1] == "HP" || parsedText[1] == "Health")
                        playerStats.SetAttribute("Health", pOut);
                    else if (parsedText[1] == "st" || parsedText[1] == "ST" || parsedText[1] == "Stamina")
                        playerStats.SetAttribute("Stamina", pOut);
                    else if (parsedText[1] == "heatresist")
                        playerStats.Attributes["Heat Resist"] = pOut;
                    else if (parsedText[1] == "coldresist")
                        playerStats.Attributes["Cold Resist"] = pOut;
                    else if (parsedText[1] == "energyresist")
                        playerStats.Attributes["Energy Resist"] = pOut;
                    else if (parsedText[1] == "radiation")
                        playerStats.radiation = pOut;
                    else if (playerStats.Attributes.ContainsKey(parsedText[1]))
                        playerStats.Attributes[parsedText[1]] = pOut;
                    else if (playerStats.proficiencies.Profs[parsedText[1]] != null)
                        playerStats.proficiencies.Profs[parsedText[1]].level = pOut;
                    else
                    {
                        MyConsole.Error("No such stat. Available ones to set: level, hp, st, heatresist, coldresist, energyresist, radiation, and all other attributes/proficiencies.");
                        return;
                    }
                }
                else
                {
                    MyConsole.Error("Use a number to set the stat to.");
                    return;
                }

                MyConsole.NewMessage("    " + parsedText[1] + " set to " + pOut.ToString());
                break;

            case "removeblockers":
                List<MapObject> toDelete = World.objectManager.ObjectsAt(World.tileMap.WorldPosition, World.tileMap.currentElevation).FindAll(x => x.blueprint.objectType == "Stair_Lock");

                while (toDelete.Count > 0)
                {
                    World.objectManager.mapObjects.Remove(toDelete[0]);
                    toDelete.RemoveAt(0);
                }

                World.tileMap.HardRebuild();
                MyConsole.NewMessage("All blockers on this map removed.");
                break;

            case "opendoors":
                for (int i = 0; i < World.objectManager.onScreenMapObjects.Count; i++)
                {
                    MapObjectSprite mos = World.objectManager.onScreenMapObjects[i].GetComponent<MapObjectSprite>();

                    if (mos.isDoor_Closed)
                        mos.ForceOpen();
                }

                World.tileMap.SoftRebuild();
                MyConsole.NewMessage("All doors opened.");
                break;

            case "closedoors":
                for (int i = 0; i < World.objectManager.onScreenMapObjects.Count; i++)
                {
                    MapObjectSprite mos = World.objectManager.onScreenMapObjects[i].GetComponent<MapObjectSprite>();

                    if (mos.isDoor_Open)
                        mos.Interact();
                }

                World.tileMap.SoftRebuild();
                MyConsole.NewMessage("All doors closed.");
                break;

            case "followme":
                foreach (Entity e in World.objectManager.onScreenNPCObjects)
                {
                    e.AI.npcBase.MakeFollower();
                }

                MyConsole.NewMessage("    Everyone Loves you. <color=magenta><3</color>");
                break;

            case "log":
                string message = textToParse.Replace("log ", "");
                CombatLog.NewMessage(message);
                break;

            case "danger":
                if (parsedText.Length < 2)
                {
                    MyConsole.NewMessage("Base Danger: " + World.BaseDangerLevel + ". Total: " + World.DangerLevel());
                }
                else
                {
                    World.BaseDangerLevel = int.Parse(parsedText[1]);
                    MyConsole.NewMessage("Base Danger Level set to : " + World.BaseDangerLevel + ". Total danger level is : " + World.DangerLevel());
                    MyConsole.NewMessage("World Danger Level: " + World.DangerLevel().ToString());
                }
                
                break;

            case "gold":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify an amount.");
                    return;
                }
                int gAmount = int.Parse(parsedText[1]);
                playerInventory.gold += gAmount;
                MyConsole.NewMessage("Added " + gAmount.ToString() + " $.");
                break;

            case "reveal":
            case "revealall":
            case "revealmap":
                TileMap tileMap = World.tileMap;
                tileMap.RevealMap();
                tileMap.SoftRebuild();
                MyConsole.NewMessage("    Map revealed.");
                break;

            case "restart":
                SceneManager.LoadScene(0);
                break;

            case "wizard":
                World.tileMap.RevealMap();
                GameObject[] entities = GameObject.FindGameObjectsWithTag("Entity");

                for (int i = 0; i < entities.Length; i++)
                {
                    entities[i].GetComponent<Stats>().SimpleDamage(1000);
                }
                break;

            case "setpos":
                int o1 = -1, o2 = -1;
                if (int.TryParse(parsedText[1], out o1) && int.TryParse(parsedText[2], out o2))
                {
                    World.tileMap.worldCoordX = o1;
                    World.tileMap.worldCoordY = o2;
                    World.tileMap.HardRebuild();
                }
                else
                {
                    MyConsole.Error("Unable to parse position.");
                }
                break;

            case "startquest":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("No quest specified");
                    return;
                }

                if (GameData.TryGet(parsedText[1], out Quest q))
                {
                    Quest quest = q.Clone();
                    ObjectManager.playerJournal.StartQuest(quest);
                    MyConsole.NewMessage("Added the quest \"" + quest.Name + "\" to the journal.");
                }
                else
                {
                    MyConsole.Error("No quest with the ID \"" + parsedText[1] + "\"");
                }
                break;

            case "spawn":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Spawn not specified.");
                    return;
                }
                else if (parsedText.Length < 3)
                {
                    MyConsole.Error("Spawn needs coordinates.\nEg: '3 -5' for spawning +3 tiles away on x axis, and -5 tiles away on y axis. \"random\" can also be input.");
                    return;
                }
                if (parsedText.Length < 4 && parsedText[3] != "random")
                {
                    MyConsole.Error("Invalid coordinates. Use a space between the x and y numbers.");
                    return;
                }

                Coord wp = World.tileMap.WorldPosition;
                Coord lp = new Coord(0, 0);
                string spawnName = parsedText[2];

                if (parsedText[parsedText.Length - 1] == "random")
                {
                    lp = World.tileMap.GetRandomPosition();
                }
                else
                {
                    int x = int.Parse(parsedText[parsedText.Length - 2]), y = int.Parse(parsedText[parsedText.Length - 1]);
                    lp = new Coord(playerEntity.posX + x, playerEntity.posY + y);

                    if (World.OutOfLocalBounds(lp.x, lp.y))
                    {
                        MyConsole.Error("Coordinate out of bounds.");
                        return;
                    }
                } 

                if (parsedText[1] == "npc")
                {
                    NPC_Blueprint bp = GameData.Get<NPC_Blueprint>(spawnName);

                    if (bp == null)
                    {
                        MyConsole.Error("No NPC with ID \"" + spawnName + "\".");
                    }
                    else
                    {
                        NPC npc = new NPC(bp, wp, lp, World.tileMap.currentElevation);
                        World.objectManager.SpawnNPC(npc);
                        MyConsole.NewMessage("    " + npc.name + " spawned at (" + lp.ToString());
                    }
                }
                else if (parsedText[1] == "object")
                {
                    MapObject_Blueprint bp = ItemList.GetMOB(spawnName);

                    if (bp == null)
                    {
                        MyConsole.Error("No Object with ID \"" + spawnName + "\".");
                    }
                    else
                    {
                        MapObject m = new MapObject(bp, lp, wp, World.tileMap.currentElevation);
                        World.objectManager.SpawnObject(m);
                        MyConsole.NewMessage("    " + m.Name + " spawned at " + lp.ToString());
                    }
                }

                World.tileMap.CheckNPCTiles();
                break;

            case "godmode":
            case "gm":
                if (parsedText.Length < 2 || parsedText.Length > 2)
                {
                    MyConsole.Error("Invalid. 0 for off, 1 for on.");
                    return;
                }

                playerStats.invincible = parsedText[1] == "on" || parsedText[1] != "0";
                MyConsole.NewMessage("    God mode " + ((parsedText[1] == "0") ? "disabled" : "enabled"));
                break;

            case "explore":
                if (parsedText.Length < 2 || parsedText.Length > 2)
                {
                    MyConsole.Error("Invalid. 0 for off, 1 for on.");
                    return;
                }

                bool toggleOn = parsedText[1] == "on" || parsedText[1] != "0";
                Manager.noEncounters = toggleOn;
                MyConsole.NewMessage("    Encounters " + (toggleOn ? "disabled" : "enabled"));
                break;

            case "fov":
            case "lighting":
                if (parsedText.Length < 2 || parsedText.Length > 2)
                {
                    MyConsole.Error("Invalid. 0 for off, 1 for on.");
                    return;
                }
                Manager.lightingOn = parsedText[1] != "0";
                MyConsole.NewMessage("    FOV/Lighting " + ((parsedText[1] == "0") ? "disabled" : "enabled") + " Act to reset.");
                break;

            case "weather":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Use a number 0-3 to specify which weather type.");
                    return;
                }
                int weatherNum = int.Parse(parsedText[1]);
                World.turnManager.ChangeWeather((Weather)weatherNum);
                break;

            case "location":
                MyConsole.NewMessage("Current Coordinate: " + World.tileMap.WorldPosition.ToString());
                break;

            case "go":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Give a destination.");
                    return;
                }

                if (parsedText[1] == "surface")
                {
                    ObjectManager.playerEntity.TeleportToSurface();
                    return;
                }
                else if (parsedText[1] == "down")
                {
                    World.tileMap.currentElevation -= 1;
                    MyConsole.NewMessage("    Going down.");
                }
                else if (parsedText[1] == "up")
                {
                    World.tileMap.currentElevation += 1;
                    MyConsole.NewMessage("    Going up.");
                }
                else if (parsedText[1] == "north")
                {
                    World.tileMap.worldCoordY++;
                    MyConsole.NewMessage("    Going North.");
                }
                else if (parsedText[1] == "south")
                {
                    World.tileMap.worldCoordY--;
                    MyConsole.NewMessage("    Going South.");
                }
                else if (parsedText[1] == "east")
                {
                    World.tileMap.worldCoordX++;
                    MyConsole.NewMessage("    Going East.");
                }
                else if (parsedText[1] == "west")
                {
                    World.tileMap.worldCoordX--;
                    MyConsole.NewMessage("    Going West.");
                }
                else if (parsedText[1] == "elevation")
                {
                    if (parsedText.Length < 3)
                    {
                        MyConsole.Error("Specify an elevation.");
                        return;
                    }

                    if (int.TryParse(parsedText[2], out int oo))
                    {
                        World.tileMap.currentElevation = oo;
                        World.tileMap.HardRebuild();
                    }
                    else
                    {
                        MyConsole.Error("Invalid elevation input.");
                    }

                    return;
                }
                else if (parsedText[1] == "home")
                {
                    World.tileMap.GoToArea("Home");
                    MyConsole.NewMessage("    Going home.");
                }
                else
                {
                    string newArea = textToParse.Replace("go ", "");
                    Zone_Blueprint zb = World.tileMap.worldMap.GetZone(newArea);

                    if (zb == null)
                    {
                        MyConsole.Error("No such place");
                        return;
                    }
                    else
                    {
                        Coord newLocation = World.tileMap.worldMap.GetLandmark(zb.ID);
                        World.tileMap.worldCoordX = newLocation.x;
                        World.tileMap.worldCoordY = newLocation.y;
                        World.tileMap.currentElevation = 0;

                        MyConsole.NewMessage("    Entering " + newArea + ".");
                    }
                }

                World.tileMap.HardRebuild();
                break;

            case "modwep":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify an ID for the modifier.");
                    return;
                }

                ItemModifier im = ItemList.GetModByID(parsedText[1]);
                if (im == null)
                {
                    MyConsole.Error("No modifier with the id \"" + parsedText[1] + "\".");
                    return;
                }

                ObjectManager.playerEntity.body.MainHand.EquippedItem.AddModifier(im);
                MyConsole.NewMessage("Mod added.");
                break;
            case "givetrait":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify a name for the trait to give. Type 'traits' to get a list of all traits for their names.");
                    return;
                }

                string n = parsedText[1];

                playerStats.GiveTrait(n);
                MyConsole.NewMessage("Player has been given the trait \"" + n + "\"");
                break;

            case "removetrait":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify a name for the trait to give. Type 'traits' to get a list of all traits for their names.");
                    return;
                }

                string traitID = parsedText[1];
                playerStats.RemoveTrait(traitID);

                MyConsole.NewMessage("Removed the trait \"" + traitID + "\"");
                break;

            case "mutate":
                if (parsedText.Length < 2)
                {
                    playerStats.Mutate();
                }
                else
                {
                    string mut = parsedText[1];

                    if (TraitList.GetTraitByID(mut) == null)
                    {
                        MyConsole.Error("No mutation with the ID \"" + mut + "\".");
                        return;
                    }

                    playerStats.Mutate(mut);
                }
                break;

            case "unmutate":
                if (parsedText.Length < 2)
                {
                    if (playerStats.Mutations.Count > 0)
                    {
                        playerStats.CureRandomMutations(1);
                        MyConsole.NewMessage("Cured a random mutation.");
                    }
                }
                else if (parsedText[1] == "all")
                {
                    playerStats.CureAllMutations();
                    MyConsole.NewMessage("All mutations cleared.");
                }
                else
                {
                    MyConsole.Error("Unrecognized command.");
                }
                break;

            case "giveability":
            case "giveskill":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify an ID for the ability to give.");
                    return;
                }

                string skillID = parsedText[1];
                Ability s = new Ability(GameData.Get<Ability>(skillID));

                if (s != null)
                {
                    ObjectManager.player.GetComponent<EntitySkills>().AddSkill(s, Ability.AbilityOrigin.Natrual);
                    MyConsole.NewMessage("Gave the player the ability \"" + s.Name + "\".");
                }
                else
                {
                    MyConsole.Error("No ability with this ID");
                }
                break;

            case "levelabilities":
                EntitySkills esk = ObjectManager.playerEntity.GetComponent<EntitySkills>();

                for (int i = 0; i < esk.abilities.Count; i++)
                {
                    esk.abilities[i].AddXP(1000 - (int)esk.abilities[i].XP);
                }

                MyConsole.NewMessage("All abilities given XP.");
                break;

            case "give":
            case "grant":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Need to provide an item name/ID.");
                    return;
                }

                string itemID = parsedText[1];
                Item item = null;

                if (parsedText.Length == 2 && !ItemList.GetItemByID(itemID).IsNullOrDefault())
                {
                    item = ItemList.GetItemByID(itemID);
                }
                else
                {
                    if (parsedText.Length > 2)
                        itemID += " " + parsedText[2];
                    if (parsedText.Length > 3)
                        itemID += " " + parsedText[3];
                    if (parsedText.Length > 4)
                        itemID += " " + parsedText[4];

                    item = ItemList.GetItemByName(itemID);
                }

                if (!item.IsNullOrDefault() && playerInventory != null)
                {
                    playerInventory.PickupItem(item);
                    MyConsole.NewMessage("    Gave 1x " + item.Name);
                }
                else
                {
                    MyConsole.NewMessage("    No such item as \"" + itemID + "\".");
                }
                break;

            case "multigive":
            case "multigrant":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Need to provide an item id.");
                    return;
                }
                int ou = 1;

                if (int.TryParse(parsedText[1], out ou))
                {
                    string i = parsedText[2];
                    Item grantedItem = ItemList.GetItemByID(i);

                    if (grantedItem != null)
                    {
                        if (grantedItem.stackable)
                        {
                            grantedItem.amount = ou;
                            playerInventory.PickupItem(grantedItem);
                        }
                        else
                        {
                            for (int x = 0; x < ou; x++)
                            {
                                playerInventory.PickupItem(grantedItem);
                            }
                        }

                        MyConsole.NewMessage("    Gave " + ou.ToString() + "x " + grantedItem.Name);
                    }
                }
                else
                {
                    MyConsole.Error("Specify an amount before typing the name of the item.");
                }
                break;

            case "killme":
                playerStats.StatusEffectDamage(1000, DamageTypes.Blunt);
                break;

            case "injure":
            case "hurt":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Need to specify amount as an integer.");
                    return;
                }

                int damage = int.Parse(parsedText[1]);
                playerStats.StatusEffectDamage(damage, DamageTypes.Blunt);
                MyConsole.NewMessage("Dealt " + damage + " to the player.");
                break;

            case "woundme":
                playerEntity.body.bodyParts.GetRandom().InflictPhysicalWound();
                MyConsole.NewMessage("Wounded the player.");
                break;

            case "givestatus":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify a status effect to give.");
                    return;
                }
                else if (parsedText.Length < 3)
                {
                    MyConsole.Error("Specify the number of turns for the effect.");
                    return;
                }

                int ot;

                if (int.TryParse(parsedText[2], out ot))
                {
                    playerStats.AddStatusEffect(parsedText[1], ot);
                    MyConsole.NewMessage("The status effect \"" + parsedText[1] + "\" has been given to the player.");
                }
                else
                {
                    MyConsole.Error("Duration not able to be parsed. Must be a number.");
                }
                break;

            case "xp":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Invalid. Choose an amount for xp.");
                    return;
                }

                playerStats.GainExperience(int.Parse(parsedText[1]));
                MyConsole.NewMessage("You gained " + parsedText[1] + " XP.");
                break;

            case "sever":
                if (parsedText.Length < 2 || parsedText[1] == "random")
                {
                    int limbIndex = UnityEngine.Random.Range(0, playerEntity.body.bodyParts.Count);

                    if (playerEntity.body.bodyParts[limbIndex].Severable)
                    {
                        playerEntity.body.RemoveLimb(limbIndex);
                        MyConsole.NewMessage("    Removed Limb " + limbIndex.ToString());
                    }

                    return;
                }

                int ln = int.Parse(parsedText[1]);
                playerEntity.body.RemoveLimb(ln);
                MyConsole.NewMessage("    Removed Limb " + ln.ToString());
                break;

            case "reattach":
                if (parsedText.Length < 2 || parsedText[1] == "all")
                {
                    playerEntity.body.RegrowLimbs();
                    MyConsole.NewMessage("    All limbs re-attached.");
                    return;
                }
                int limbNum = int.Parse(parsedText[1]);
                playerEntity.body.AttachLimb(limbNum);
                MyConsole.NewMessage("    Attached Limb " + limbNum.ToString());
                break;

            case "heal":
            case "fullheal":
                playerStats.Heal(playerStats.MaxHealth);
                MyConsole.NewMessage("    Fully healed player.");
                break;

            case "detonate":
            case "killall":
                GameObject[] ents = GameObject.FindGameObjectsWithTag("Entity");

                for (int i = 0; i < ents.Length; i++)
                {
                    ents[i].GetComponent<Stats>().SimpleDamage(1000);
                }
                MyConsole.NewMessage("    All on-screen NPCs killed.");
                break;

            case "questflag":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify the flag to give.");
                    return;
                }

                ObjectManager.playerJournal.AddFlag(parsedText[1]);
                MyConsole.NewMessage("Gave the flag \"" + parsedText[1] + "\" to the journal.");
                break;

            case "completequest":
                if (ObjectManager.playerJournal.trackedQuest != null)
                {
                    Quest tr = ObjectManager.playerJournal.trackedQuest;

                    for (int i = 0; i < tr.goals.Length; i++)
                    {
                        if (!tr.goals[i].isComplete)
                        {
                            tr.goals[i].Complete();
                        }
                    }
                }
                break;

            case "unstick":
            case "unstuck":
                playerEntity.ForcePosition(World.tileMap.CurrentMap.GetRandomFloorTile());
                MyConsole.NewMessage("Player position set to random floor tile.");
                break;

            case "addlocation":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify a location ID to add.");
                    return;
                }

                string zoneID = "";

                for (int i = 1; i < parsedText.Length; i++)
                {
                    zoneID += parsedText[i];

                    if (i < parsedText.Length - 1)
                    {
                        zoneID += " ";
                    }
                }

                new CreateLocationEvent(zoneID).RunEvent();
                break;

            case "removelocation":
                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify a location ID to remove.");
                    return;
                }

                string zID = "";

                for (int i = 1; i < parsedText.Length; i++)
                {
                    zID += parsedText[i];

                    if (i < parsedText.Length - 1)
                    {
                        zID += " ";
                    }
                }

                new RemoveLocationEvent(zID).RunEvent();
                break;

            case "startincident":
                if (!PlayerInput.fullMap)
                {
                    MyConsole.Error("Must be done from the world map.");
                    return;
                }

                if (parsedText.Length < 2)
                {
                    MyConsole.Error("Specify an incident ID.");
                    return;
                }

                if (!GameData.TryGet(parsedText[1], out Incident incident))
                {
                    MyConsole.Error("Could not find incident with that id.");
                    return;
                }

                World.playerInput.TriggerLocalOrWorldMap();
                World.tileMap.HardRebuild();
                incident.Spawn();

                break;

            case "?":
            case "help":
            case "commands":
                MyConsole.DoubleLine();
                MyConsole.NewMessage("");

                MyConsole.NewMessageColor("<b>INFO:</b>", Color.red);
                MyConsole.NewMessage(" Note: Spaces are used to parse the string.");
                MyConsole.NewMessageColor("  <b>Commands:</b>", Color.red);

                MyConsole.NewMessage("  - <b>unstuck/unstick</b>\n    Teleports you to a random floor tile if stuck.");
                MyConsole.NewHelpLine("location", "Displays the current world coordinate.");
                MyConsole.NewHelpLine("go [area/direction]", "Travel one screen in a direction. [direction] = \"up\", \"down\", \"north\", \"south\", \"east\", \"west\", \"surface\".\n" +
                    "You can also travel to any landmark by typing its ID, or any elevation by typing \"go elevation [e]\" where \"[e]\" is an integer.");
                MyConsole.NewHelpLine("  - <b>setpos [x] [y]</b>", "Travel to a specific world position. Constraints: 0-199 on each axis.");
                MyConsole.NewMessage("  - <b>godmode</b> <i>[0-1]</i>\n      0 = off  1 = on");
                MyConsole.NewMessage("  - <b>fov</b> <i>[0-1]</i>\n      Whether to show fog of war or not. \n      0 = off  1 = on");
                MyConsole.NewMessage("  - <b>explore</b> <i>[0-1]</i>\n      0 = off  1 = on\n      Enables or disables map encounters.");
                MyConsole.NewMessage("  - <b>gold</b> <i>[amount]</i>\n      Gives [amount] gold to the player.");
                MyConsole.NewMessage("  - <b>spawn npc</b> <i>[ID] [x] [y]</i>\n      Spawns an NPC at a position relative to the player.");
                MyConsole.NewMessage("  - <b>spawn object</b> <i>[ID] [x] [y]</i>\n      Spawns an object at a position relative to the player.");

                MyConsole.NewMessage("  - <b>set <i>[stat] [value]</i></b>\n      Sets a specific stat to the selected value.");
                MyConsole.NewMessage("  - <b>xp <i>[amount]</i></b>\n      gain [amount] XP.");
                MyConsole.NewMessage("  - <b>sever</b> <i>[limb index] or \"random\"</i>\n      Severs numbered limb, or random.");
                MyConsole.NewMessage("  - <b>reattach</b> <i>[limb index] or \"all\"</i>\n      Re-attaches limb at index, or all.");

                MyConsole.NewMessage("  - <b>give/grant</b> <i>[item name/ID]</i>\n      Give a specified item to the player.");
                MyConsole.NewMessage("  - <b>multigive/multigrant</b> <i>[amount] [item name/ID]</i>\n      Give a specific number of a specified item to the player.");
                MyConsole.NewMessage("  - <b>mods</b>\n    Lists all the item modifiers.");
                MyConsole.NewMessage("  - <b>modwep</b> <i>[mod ID]</i>\n    Modifies the first non-severed hand's equipped weapon with the selected modifier ID.");

                MyConsole.NewMessage("  - <b>levelabilities</b>\n    Gives all current abilities enough XP to level up. Does not work on abilities that do not gain XP.");
                MyConsole.NewMessage("  - <b>givetrait</b> <i>[trait ID]</i>\n      Give a specified trait or mutation to the player.");
                MyConsole.NewMessage("  - <b>removetrait</b> <i>[trait ID]</i>\n      Remove a specified trait or mutation from the player.");
                MyConsole.NewMessage("  - <b>mutate</b> <i>[mutation ID]</i>\n      Gives the player the specified mutation.");
                MyConsole.NewMessage("  - <b>unmutate</b>\n      Removes a random mutation.");
                MyConsole.NewMessage("  - <b>unmutate all</b>\n      Removes all mutations.");
                MyConsole.NewMessage("  - <b>giveskill/giveability</b> <i>[ability ID]</i>\n      Give a specified ability to the player.");
                MyConsole.NewMessage("  - <b>levelabilities</b>\n    Increases the level of all abilities by one.");

                MyConsole.NewMessage("  - <b>danger</b>\n      Displays the current world danger level.");
                MyConsole.NewMessage("  - <b>killme</b>\n      Kills your character.");
                MyConsole.NewMessage("  - <b>heal</b>\n      Heals the player fully.");
                MyConsole.NewMessage("  - <b>injure</b> <i>[amount]</i>\n      Injures the player by a certain amount.");
                MyConsole.NewMessage("  - <b>givestatus</b> <i>[effect name] [amount]</i>\n    Gives the player a particular status effect. Poison, Blind, Bleed, Haste, Regen, etc...");
                MyConsole.NewMessage("  - <b>followme</b>\n    All on-screen NPCs become followers.");

                MyConsole.NewMessage("  - <b>removeblockers</b>\n    Removes all blockers from the current screen.");
                MyConsole.NewMessage("  - <b>opendoors</b>\n    Opens all doors on the current screen, regardless of permissions.");
                MyConsole.NewMessage("  - <b>closedoors</b>\n    Closes all doors on the current screen, regardless of permissions.");

                MyConsole.NewMessage("  - <b>load <i>[map ID]</i></b>\n    Loads a map by its id.");
                MyConsole.NewMessage("  - <b>addlocation <i>[zone ID]</i></b>\n    Adds the specified zone to the world map.");
                MyConsole.NewMessage("  - <b>removelocation <i>[zone ID]</i></b>\n    Removes the specified zone from the world map.");
                MyConsole.NewMessage("  - <b>startincident <i>[incident ID]</i></b>\n    Sets up the specified incident. Can only be done on the world map.");
                MyConsole.NewMessage("  - <b>detonate</b>\n      Kills all NPCs on the screen.");
                MyConsole.NewMessage("  - <b>reveal</b>\n      Reveals all tiles on the map");
                MyConsole.NewMessage("  - <b>wizard</b>\n      Combines the previous two commands. Kills all NPCs on screen, and reveals the map. Cynapse is too lazy to type two commands.");
                MyConsole.NewMessage("  - <b>completequest</b>\n    Completes the current tracked quest.");
                MyConsole.NewMessage("  - <b>startquest</b> <i>[ID]</i>\n    Starts the quest with the input ID.");
                MyConsole.NewMessage("  - <b>questflag <i>[flag]</i></b>\n    Gives the player the input quest flag. Possibilities: " +
                    "\n\tCan_Enter_Ensis, Can_Open_Prison_Cells, Can_Enter_Magna, Can_Enter_Fab, HostileTo_[factionID]");
                MyConsole.NewMessage("  - <b>weather <i>[amount (0-3)]</i></b>\n    Sets the world weather to the appropriate number.");
                MyConsole.NewMessage("  - <b>5k</b>\n    Increases the turn counter by 5000.");
                MyConsole.NewMessage("  - <b>10k</b>\n    Increases the turn counter by 10000.");
                MyConsole.NewMessage("  - <b>log</b>\n    Write a message to the combat log.");
                MyConsole.NewMessage("  - <b>bodydrop <i>[percent]</i></b>\n    Sets the chance to drop bodies upon death.");

                MyConsole.DoubleLine();
                break;

            default:
                MyConsole.Error("'" + textToParse + "' is not a valid command");
                break;
        }

        textField = "";
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

//CONSOLE COMMANDS
public class ConsoleCommand
{
    protected const char splitChar = ' ';
    public string command;
    public string alternateCommand;
    public string description;
    readonly Action runAction;

    public ConsoleCommand(string cmd, string desc, Action action = null)
    {
        command = cmd;
        description = desc;
        runAction = action;
    }

    public virtual bool RunIfMatch(string cmd)
    {
        if (CommandMatches(cmd))
        {
            RunAction();
            return true;
        }

        return false;
    }

    public bool CommandMatches(string cmd)
    {
        return cmd == command || (cmd == alternateCommand && !alternateCommand.NullOrEmpty());
    }

    protected virtual void RunAction()
    {
        runAction?.Invoke();
    }

    public override string ToString()
    {
        if (alternateCommand.NullOrEmpty())
            return string.Format("  - <b>{0}</b>\n    {1}", command, description);
        return string.Format("  - <b>{0}/{2}</b>\n    {1}", command, description, alternateCommand);
    }
}

public class ConsoleCommand_Toggle : ConsoleCommand
{
    bool tmpOn;
    readonly Action<bool> runAction;

    public ConsoleCommand_Toggle(string cmd, string desc, Action<bool> action) 
        : base(cmd, desc)
    {
        runAction = action;
    }

    public override bool RunIfMatch(string cmd)
    {
        string[] inputs = cmd.Split(splitChar);

        if (inputs.Length < 2 || !CommandMatches(inputs[0]))
        {
            return false;
        }

        if (inputs[1] == "0" || inputs[1] == "off" || inputs[1] == "false")
        {
            tmpOn = false;
            RunAction();
            return true;

        }
        else if (inputs[1] == "1" || inputs[1] == "on" || inputs[1] == "true")
        {
            tmpOn = true;
            RunAction();
            return true;
        }

        return false;
    }

    protected override void RunAction()
    {
        runAction?.Invoke(tmpOn);
    }

    public override string ToString()
    {
        if (alternateCommand.NullOrEmpty())
            return string.Format("  - <b>{0} <i>[0/1]</i></b>\n    {1}", command, description);
        return string.Format("  - <b>{0}/{2} <i>[0/1]</i></b>\n    {1}", command, description, alternateCommand);
    }
}

public class ConsoleCommand_Offset : ConsoleCommand
{
    int tmpX, tmpY;
    readonly Action<int, int> runAction;

    public ConsoleCommand_Offset(string cmd, string desc, Action<int, int> action) 
        : base(cmd, desc)
    {
        runAction = action;
    }

    public override bool RunIfMatch(string cmd)
    {
        string[] inputs = cmd.Split(splitChar);

        if (inputs.Length < 3 || !CommandMatches(inputs[0]))
        {
            return false;
        }

        if (int.TryParse(inputs[1], out tmpX) && int.TryParse(inputs[2], out tmpY))
        {
            RunAction();
            return true;
        }

        return false;
    }

    protected override void RunAction()
    {
        runAction?.Invoke(tmpX, tmpY);
    }

    public override string ToString()
    {
        if (alternateCommand.NullOrEmpty())
            return string.Format("  - <b>{0} <i>[x] [y]</i></b>\n    {1}", command, description);
        return string.Format("  - <b>{0}/{2} <i>[x] [y]</i></b>\n    {1}", command, description, alternateCommand);
    }
}

public class ConsoleCommand_Int : ConsoleCommand
{
    int tmpInt;
    readonly Action<int> runAction;
    readonly string inputDescription;

    public ConsoleCommand_Int(string cmd, string desc, Action<int> action, string inputDesc)
        : base(cmd, desc)
    {
        runAction = action;
        inputDescription = inputDesc;
    }

    public override bool RunIfMatch(string cmd)
    {
        string[] inputs = cmd.Split(splitChar);

        if (inputs.Length < 2 || !CommandMatches(inputs[0]) || !int.TryParse(inputs[1], out tmpInt))
        {
            return false;
        }

        RunAction();
        return true;
    }

    protected override void RunAction()
    {
        runAction?.Invoke(tmpInt);
    }

    public override string ToString()
    {
        if (alternateCommand.NullOrEmpty())
            return string.Format("  - <b>{0} <i>[{1}]</i></b>\n    {2}", command, inputDescription, description);
        return string.Format("  - <b>{0}/{1} <i>[{2}]</i></b>\n    {3}", command, alternateCommand, inputDescription, description);
    }
}

public class ConsoleCommand_String : ConsoleCommand
{
    string tmpString;
    readonly Action<string> runAction;
    readonly string inputDescription;

    public ConsoleCommand_String(string cmd, string desc, Action<string> action, string inputDesc) 
        : base(cmd, desc)
    {
        runAction = action;
        inputDescription = inputDesc;
    }

    public override bool RunIfMatch(string cmd)
    {
        string[] inputs = cmd.Split(splitChar);

        if (inputs.Length < 2 || CommandMatches(inputs[0]))
        {
            return false;
        }

        tmpString = inputs[1];
        RunAction();
        return true;
    }

    protected override void RunAction()
    {
        runAction?.Invoke(tmpString);
    }

    public override string ToString()
    {
        if (alternateCommand.NullOrEmpty())
            return string.Format("  - <b>{0} <i>[{1}]</i></b>\n    {2}", command, inputDescription, description);
        return string.Format("  - <b>{0}/{1} <i>[{2}]</i></b>\n    {3}", command, alternateCommand, inputDescription, description);
    }
}
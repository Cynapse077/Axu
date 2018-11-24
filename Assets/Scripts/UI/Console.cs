using UnityEngine;
using System.Collections.Generic;
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

        if ((Input.GetKeyUp(KeyCode.End) || Input.GetKeyUp(KeyCode.BackQuote))
            && !UserInterface.showConsole && GameSettings.Allow_Console)
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
        if (parsedText[0] == "load")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Speify a map name.");
                return;
            }

            World.objectManager.DeleteNPCsAt(World.tileMap.WorldPosition, World.tileMap.currentElevation);
            World.objectManager.DeleteObjectsAt(World.tileMap.WorldPosition, World.tileMap.currentElevation);
            World.tileMap.LoadMap(parsedText[1]);
            MyConsole.NewMessage("Map " + parsedText[1] + " loaded.");
            return;
        }
        else if (parsedText[0] == "5k")
        {
            World.turnManager.IncrementTime(5000);
            World.tileMap.HardRebuild();
            return;
        }
        else if (parsedText[0] == "set")
        {
            int o = 0;
            if (parsedText.Length < 2)
                MyConsole.Error("Specify what to set, then add a value.");
            else if (parsedText.Length < 3)
                MyConsole.Error("Specify a value.");
            else if (int.TryParse(parsedText[2], out o))
            {
                if (parsedText[1] == "level")
                    playerStats.MyLevel.CurrentLevel = o;
                else if (parsedText[1] == "hp" || parsedText[1] == "HP" || parsedText[1] == "Health")
                    playerStats.health = playerStats.maxHealth = o;
                else if (parsedText[1] == "st" || parsedText[1] == "ST" || parsedText[1] == "Stamina")
                    playerStats.stamina = playerStats.maxStamina = o;
                else if (parsedText[1] == "heatresist")
                    playerStats.Attributes["Heat Resist"] = o;
                else if (parsedText[1] == "coldresist")
                    playerStats.Attributes["Cold Resist"] = o;
                else if (parsedText[1] == "energyresist")
                    playerStats.Attributes["Energy Resist"] = o;
                else if (parsedText[1] == "radiation")
                    playerStats.radiation = o;
                else if (playerStats.Attributes.ContainsKey(parsedText[1]))
                    playerStats.Attributes[parsedText[1]] = o;
                else if (playerStats.proficiencies.Profs[parsedText[1]] != null)
                    playerStats.proficiencies.Profs[parsedText[1]].level = o;
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

            MyConsole.NewMessage("    " + parsedText[1] + " set to " + o.ToString());
            return;
        }
        else if (parsedText[0] == "removeblockers")
        {
            List<MapObject> toDelete = World.objectManager.ObjectsAt(World.tileMap.WorldPosition, World.tileMap.currentElevation).FindAll(x => x.objectType == "Stair_Lock");

            while (toDelete.Count > 0)
            {
                World.objectManager.mapObjects.Remove(toDelete[0]);
                toDelete.RemoveAt(0);
            }

            World.tileMap.HardRebuild();
            MyConsole.NewMessage("All blockers on this map removed.");
            return;
        }
        else if (parsedText[0] == "opendoors")
        {
            for (int i = 0; i < World.objectManager.onScreenMapObjects.Count; i++)
            {
                MapObjectSprite mos = World.objectManager.onScreenMapObjects[i].GetComponent<MapObjectSprite>();

                if (mos.isDoor_Closed)
                    mos.ForceOpen();
            }

            World.tileMap.SoftRebuild();
            MyConsole.NewMessage("All doors opened.");
            return;
        }
        else if (parsedText[0] == "closedoors")
        {
            for (int i = 0; i < World.objectManager.onScreenMapObjects.Count; i++)
            {
                MapObjectSprite mos = World.objectManager.onScreenMapObjects[i].GetComponent<MapObjectSprite>();

                if (mos.isDoor_Open)
                    mos.Interact();
            }

            World.tileMap.SoftRebuild();
            MyConsole.NewMessage("All doors closed.");
            return;
        }
        else if (parsedText[0] == "followme")
        {
            foreach (Entity e in World.objectManager.onScreenNPCObjects)
            {
                e.AI.npcBase.MakeFollower();
            }

            MyConsole.NewMessage("    Everyone Loves you. <color=magenta><3</color>");
            return;
        }
        else if (parsedText[0] == "log")
        {
            string message = textToParse.Replace("log ", "");
            CombatLog.NewMessage(message);
            return;
        }
        else if (parsedText[0] == "danger")
        {
            MyConsole.NewMessage("World Danger Level: " + World.DangerLevel().ToString());
            return;
        }
        else if (parsedText[0] == "gold")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Specify an amount.");
                return;
            }
            int gAmount = int.Parse(parsedText[1]);
            playerInventory.gold += gAmount;
            MyConsole.NewMessage("Added " + gAmount.ToString() + " $.");
            return;
        }

        else if (parsedText[0] == "revealall" || parsedText[0] == "reveal" || parsedText[0] == "revealmap")
        {
            TileMap tileMap = World.tileMap;
            tileMap.RevealMap();
            tileMap.SoftRebuild();
            MyConsole.NewMessage("    Map revealed.");
            return;
        }
        else if (parsedText[0] == "restart")
        {
            SceneManager.LoadScene(0);
            return;
        }
        else if (parsedText[0] == "wizard")
        {
            World.tileMap.RevealMap();
            GameObject[] entities = GameObject.FindGameObjectsWithTag("Entity");

            for (int i = 0; i < entities.Length; i++)
            {
                entities[i].GetComponent<Stats>().SimpleDamage(1000);
            }

            return;
        }
        else if (parsedText[0] == "setpos")
        {
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
            return;
        }
        else if (parsedText[0] == "startquest")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("No quest specified");
                return;
            }

            if (QuestList.GetByID(parsedText[1]) != null)
            {
                Quest q = QuestList.GetByID(parsedText[1]);
                ObjectManager.playerJournal.StartQuest(q);
                MyConsole.NewMessage("Added the quest \"" + q.Name + "\" to the journal.");
            }
            else
            {
                MyConsole.Error("No quest with the ID \"" + parsedText[1] + "\"");
            }

            return;
        }

        else if (parsedText[0] == "spawn")
        {
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
            }
            

            if (lp.x < 0 || lp.x >= Manager.localMapSize.x)
            {
                MyConsole.Error("X coordinate out of bounds.");

                if (lp.y < 0 || lp.y >= Manager.localMapSize.y)
                    MyConsole.Error("Y coordinate out of bounds.");

                return;
            }
            if (lp.y < 0 || lp.y >= Manager.localMapSize.y)
            {
                MyConsole.Error("Y coordinate out of bounds.");
                return;
            }

            if (parsedText[1] == "npc")
            {
                NPC_Blueprint bp = EntityList.GetBlueprintByID(spawnName);

                if (bp == null)
                    MyConsole.Error("No NPC with ID \"" + spawnName + "\".");
                else
                {
                    NPC n = new NPC(bp, wp, lp, World.tileMap.currentElevation);
                    World.objectManager.SpawnNPC(n);
                    MyConsole.NewMessage("    " + spawnName + " spawned at (" + lp.x + "," + lp.y + ") relative to player.");
                }
            }
            else if (parsedText[1] == "object")
            {
                MapObjectBlueprint bp = ItemList.GetMOB(spawnName);

                if (bp == null)
                    MyConsole.Error("No NPC with ID \"" + spawnName + "\".");
                else
                {
                    MapObject m = new MapObject(bp, lp, wp, World.tileMap.currentElevation);
                    World.objectManager.SpawnObject(m);
                    MyConsole.NewMessage("    " + spawnName + " spawned at (" + lp.x + "," + lp.y + ") relative to player.");
                }
            }

            World.tileMap.CheckNPCTiles();
            return;
        }

        else if (parsedText[0] == "godmode" || parsedText[0] == "gm")
        {
            if (parsedText.Length < 2 || parsedText.Length > 2)
            {
                MyConsole.Error("Invalid. 0 for off, 1 for on.");
                return;
            }
            playerStats.invincible = (parsedText[1] != "0");
            MyConsole.NewMessage("    God mode " + ((parsedText[1] == "0") ? "disabled" : "enabled"));
            return;
        }

        else if (parsedText[0] == "explore")
        {
            if (parsedText.Length < 2 || parsedText.Length > 2)
            {
                MyConsole.Error("Invalid. 0 for off, 1 for on.");
                return;
            }

            bool toggleOn = parsedText[1] == "1";
            Manager.noEncounters = toggleOn;
            MyConsole.NewMessage("    Encounters " + (toggleOn ? "disabled" : "enabled"));
            return;
        }

        else if (parsedText[0] == "fov" || parsedText[0] == "lighting")
        {
            if (parsedText.Length < 2 || parsedText.Length > 2)
            {
                MyConsole.Error("Invalid. 0 for off, 1 for on.");
                return;
            }
            Manager.lightingOn = (parsedText[1] != "0");
            MyConsole.NewMessage("    FOV/Lighting " + ((parsedText[1] == "0") ? "disabled" : "enabled") + " Act to reset.");
            return;
        }
        //Weather
        else if (parsedText[0] == "weather")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Use a number 0-3 to specify which weather type.");
                return;
            }
            int weatherNum = int.Parse(parsedText[1]);
            World.turnManager.ChangeWeather((Weather)weatherNum);
            return;
        }


        //TRAVEL
        else if (parsedText[0] == "location")
        {
            MyConsole.NewMessage("Current Coordinate: " + World.tileMap.WorldPosition.ToString());
            return;
        }
        else if (parsedText[0] == "go")
        {
            TileMap tileMap = GameObject.FindWithTag("TileMap").GetComponent<TileMap>();

            if (parsedText.Length < 2)
            {
                MyConsole.Error("Give a direction to travel. down/up.");
                return;
            }
            else if (parsedText[1] == "surface")
            {
                ObjectManager.playerEntity.TeleportToSurface();
                return;
            }
            else if (parsedText[1] == "down")
            {
                tileMap.currentElevation -= 1;
                MyConsole.NewMessage("    Going down.");
            }
            else if (parsedText[1] == "up")
            {
                tileMap.currentElevation += 1;
                MyConsole.NewMessage("    Going up.");
            }
            else if (parsedText[1] == "north")
            {
                tileMap.worldCoordY++;
                MyConsole.NewMessage("    Going North.");
            }
            else if (parsedText[1] == "south")
            {
                tileMap.worldCoordY--;
                MyConsole.NewMessage("    Going South.");
            }
            else if (parsedText[1] == "east")
            {
                tileMap.worldCoordX++;
                MyConsole.NewMessage("    Going East.");
            }
            else if (parsedText[1] == "west")
            {
                tileMap.worldCoordX--;
                MyConsole.NewMessage("    Going West.");
            }

            else if (parsedText[1] == "powerplant")
            {
                tileMap.GoToArea("Power Plant");
                MyConsole.NewMessage("    Going to the Power Plant.");
            }

            else if (parsedText[1] == "volcano")
            {
                tileMap.GoToArea("Volcano");
                MyConsole.NewMessage("    Going to the Volcano.");
            }

            else if (parsedText[1] == "magna")
            {
                tileMap.GoToArea("Magna");
                MyConsole.NewMessage("    Going to the scary place.");
            }

            else if (parsedText[1] == "magna_center")
            {
                tileMap.GoToArea("Magna_Center");
                MyConsole.NewMessage("    Going to the scary place.");
            }

            else if (parsedText[1] == "ensis")
            {
                tileMap.GoToArea("Ensis");
                MyConsole.NewMessage("    Going to the Ensis Base.");
            }
            else if (parsedText[1] == "frostborne")
            {
                tileMap.GoToArea("Frostborne");
                MyConsole.NewMessage("    Going to the Frostborne Village.");
            }
            else if (parsedText[1] == "cult")
            {
                tileMap.GoToArea("Cult");
                MyConsole.NewMessage("    Going to the Kindred Hideout.");
            }
            else if (parsedText[1] == "cathedral")
            {
                tileMap.GoToArea("Cathedral");
                MyConsole.NewMessage("    Going to the Cathedral.");
            }
            else if (parsedText[1] == "arena")
            {
                tileMap.GoToArea("Arena");
                MyConsole.NewMessage("    Going to the Arena");
            }
            else if (parsedText[1] == "oasis")
            {
                tileMap.GoToArea("Oasis");
                MyConsole.NewMessage("    Going to the Oasis");
            }
            else if (parsedText[1] == "xul")
            {
                tileMap.GoToArea("Xul");
                MyConsole.NewMessage("    Going to the Xul Encampment.");
            }
            else if (parsedText[1] == "workshop")
            {
                tileMap.GoToArea("Workshop");
                MyConsole.NewMessage("  Going to Ka-Nil");

            }
            else if (parsedText[1] == "home")
            {
                tileMap.GoToArea("Home");
                MyConsole.NewMessage("    Going home.");
            }
            else
            {
                MyConsole.Error("No such place");
                return;
            }

            tileMap.HardRebuild();
            tileMap.LightCheck(playerEntity);

            return;
        }
        else if (parsedText[0] == "danger")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.NewMessage("Base Danger: " + World.BaseDangerLevel + ". Total: " + World.DangerLevel());
                //MyConsole.Error("You need to put in a number for the danger level.");
                return;
            }
            World.BaseDangerLevel = int.Parse(parsedText[1]);
            MyConsole.NewMessage("Base Danger Level set to : " + World.BaseDangerLevel + ". Total danger level is : " + World.DangerLevel());
            return;
        }

        else if (parsedText[0] == "modwep")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Specify an ID for the mod. Use the \"mods\" command to see them all.");
                return;
            }

            ItemModifier im = ItemList.GetModByID(parsedText[1]);
            if (im == null)
            {
                MyConsole.Error("No mod with the id of \"" + parsedText[1] + "\".");
                return;
            }

            ObjectManager.playerEntity.body.MainHand.EquippedItem.AddModifier(im);
            MyConsole.NewMessage("Mod added.");
            return;
        }

        else if (parsedText[0] == "mods")
        {
            List<ItemModifier> mods = ItemList.modifiers;
            MyConsole.NewMessage("<b><color=yellow>--- MODS ---</color></b>");

            for (int i = 0; i < mods.Count; i++)
            {
                MyConsole.NewMessage("[" + mods[i].ID + "] " + mods[i].name);
            }
            return;
        }

        //TRAITS
        else if (parsedText[0] == "traits")
        {
            List<Trait> traits = TraitList.traits;
            MyConsole.NewMessage("<b><color=yellow>--- TRAITS ---</color></b>");

            for (int i = 0; i < traits.Count; i++)
            {
                MyConsole.NewMessage("[" + traits[i].ID + "] " + traits[i].name);
            }
            return;
        }

        else if (parsedText[0] == "randommutation")
        {
            playerStats.Radiate(100);
            return;
        }

        else if (parsedText[0] == "givetrait")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Specify a name for the trait to give. Type 'traits' to get a list of all traits for their names.");
                return;
            }

            string n = parsedText[1];

            playerStats.GiveTrait(n);
            MyConsole.NewMessage("Player has been given the trait \"" + n + "\"");
            return;
        }
        else if (parsedText[0] == "removetrait")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Specify a name for the trait to give. Type 'traits' to get a list of all traits for their names.");
                return;
            }

            string n = parsedText[1];
            playerStats.RemoveTrait(n);

            MyConsole.NewMessage("Removed the trait \"" + n + "\"");
            return;
        }
        else if (parsedText[0] == "giveability" || parsedText[0] == "giveskill")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Specify an ID for the ability to give.");
                return;
            }

            string n = parsedText[1];

            if (SkillList.GetSkillByID(n) != null)
            {
                Skill s = new Skill(SkillList.GetSkillByID(n));
                ObjectManager.player.GetComponent<EntitySkills>().AddSkill(s);
                MyConsole.NewMessage("Gave the player the ability \"" + s.Name + "\".");
            }
            else
            {
                MyConsole.Error("No ability with this ID");
            }
            return;
        }

        else if (parsedText[0] == "levelabilities")
        {
            EntitySkills esk = ObjectManager.playerEntity.GetComponent<EntitySkills>();

            for (int i = 0; i < esk.abilities.Count; i++)
            {
                esk.abilities[i].AddXP(1000 - (int)esk.abilities[i].XP);
            }

            MyConsole.NewMessage("All abilities given XP.");
            return;
        }

        else if (parsedText[0] == "radiate")
        {
            playerStats.Mutate();
            return;
        }

        else if (parsedText[0] == "mutate")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Specify a mutation ID");
                return;
            }

            string mut = parsedText[1];

            if (TraitList.GetTraitByID(mut) == null)
            {
                MyConsole.Error("No mutation with the ID \"" + mut + "\".");
                return;
            }

            playerStats.Mutate(mut);
            return;
        }

        else if (parsedText[0] == "unmutate")
        {
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

            return;
        }

        else if (parsedText[0] == "give" || parsedText[0] == "grant")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Need to provide an item name.");
                return;
            }

            string n = parsedText[1];
            Item item = null;

            if (parsedText.Length == 2 && ItemList.GetItemByID(n) != null)
            {
                item = ItemList.GetItemByID(n);
            }
            else
            {
                if (parsedText[1] == "randart")
                {
                    item = ItemList.GetRandart(ItemList.items.FindAll(x => (x.HasProp(ItemProperty.Weapon) || x.HasProp(ItemProperty.Armor) && x.lootable && x.rarity < 100)).GetRandom(SeedManager.combatRandom));
                }
                else
                {
                    if (parsedText.Length > 2)
                        n += " " + parsedText[2];
                    if (parsedText.Length > 3)
                        n += " " + parsedText[3];
                    if (parsedText.Length > 4)
                        n += " " + parsedText[4];

                    item = ItemList.GetItemByName(n);
                }
            }

            if (item != null)
            {
                playerInventory.PickupItem(item);
                MyConsole.NewMessage("    Gave 1x " + item.Name);
            }
            else
            {
                MyConsole.NewMessage("    No such item as \"" + n + "\".");
            }

            return;
        }

        else if (parsedText[0] == "multigive" || parsedText[0] == "multigrant")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Need to provide an item name.");
                return;
            }
            int o = 1;

            if (int.TryParse(parsedText[1], out o))
            {
                string n = parsedText[2];
                if (parsedText.Length > 3)
                    n += " " + parsedText[3];
                if (parsedText.Length > 4)
                    n += " " + parsedText[4];
                if (parsedText.Length > 5)
                    n += " " + parsedText[5];

                if (ItemList.GetItemByName(n) != null)
                {
                    Item newItem = ItemList.GetItemByName(n);
                    if (newItem.stackable)
                    {
                        newItem.amount = o;
                        playerInventory.PickupItem(newItem);
                    }
                    else
                    {
                        for (int x = 0; x < o; x++)
                        {
                            playerInventory.PickupItem(newItem);
                        }
                    }

                    MyConsole.NewMessage("    Gave " + o.ToString() + "x " + newItem.Name);
                }
            }
            else
            {
                MyConsole.Error("Specify an amount before typing the name of the item.");
            }

            return;
        }

        else if (parsedText[0] == "killme")
        {
            playerStats.StatusEffectDamage(1000, DamageTypes.Blunt);
            return;
        }
        else if (parsedText[0] == "injure" || parsedText[0] == "hurt")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Need to specify amount as an integer.");
                return;
            }

            int damage = int.Parse(parsedText[1]);
            playerStats.StatusEffectDamage(damage, DamageTypes.Blunt);
            MyConsole.NewMessage("Dealt " + damage + " to the player.");
            return;
        }
        else if (parsedText[0] == "woundme")
        {
            playerEntity.body.bodyParts.GetRandom().InflictPhysicalWound();
            MyConsole.NewMessage("Wounded the player.");
            return;
        }

        else if (parsedText[0] == "givestatus")
        {
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

            int o;

            if (int.TryParse(parsedText[2], out o))
            {
                playerStats.AddStatusEffect(parsedText[1], o);
                MyConsole.NewMessage("The status effect \"" + parsedText[1] + "\" has been given to the player.");
            }
            else
            {
                MyConsole.Error("Duration not able to be parsed. Must be a number.");
            }

            return;
        }

        else if (parsedText[0] == "xp")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Invalid. Choose an amount for xp.");
                return;
            }
            playerStats.GainExperience(int.Parse(parsedText[1]));
            MyConsole.NewMessage("You gained " + parsedText[1] + " XP.");
            return;
        }

        else if (parsedText[0] == "sever")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Select a body part integer to sever.");
                return;
            }
            if (parsedText[1] == "random")
            {
                int limbNum = Random.Range(0, playerEntity.body.bodyParts.Count);
                if (playerEntity.body.bodyParts[limbNum].severable)
                {
                    playerEntity.body.RemoveLimb(limbNum);
                    MyConsole.NewMessage("    Removed Limb " + limbNum.ToString());
                }
                return;
            }
            int ln = int.Parse(parsedText[1]);
            playerEntity.body.RemoveLimb(ln);
            MyConsole.NewMessage("    Removed Limb " + ln.ToString());
            return;
        }

        else if (parsedText[0] == "limbtest")
        {
            ParseTextField("give arm_ensistic");
            ParseTextField("give arm_ensistic");
            ParseTextField("gold 10000");
            ParseTextField("spawn Doctor 1 0");

            MyConsole.NewMessage("    Limb test set up.");
            return;
        }

        else if (parsedText[0] == "reattach")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("No body part specified. Use an index or \"all\".");
                return;
            }
            if (parsedText[1] == "all")
            {
                playerEntity.body.RegrowLimbs();
                MyConsole.NewMessage("    All limbs re-attached.");
                return;
            }
            int limbNum = int.Parse(parsedText[1]);
            playerEntity.body.AttachLimb(limbNum);
            MyConsole.NewMessage("    Attached Limb " + limbNum.ToString());
            return;
        }

        else if (parsedText[0] == "fullheal" || parsedText[0] == "heal")
        {
            playerStats.Heal(playerStats.maxHealth);
            MyConsole.NewMessage("    Fully healed player.");
            return;
        }
        else if (parsedText[0] == "detonate" || parsedText[0] == "killall")
        {
            GameObject[] entities = GameObject.FindGameObjectsWithTag("Entity");
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i].GetComponent<Stats>().StatusEffectDamage(1000, DamageTypes.Blunt);
            }
            MyConsole.NewMessage("    All on-screen NPCs killed.");
            return;
        }

        else if (parsedText[0] == "items")
        {
            if (ItemList.items.Count <= 0)
            {
                MyConsole.Error("No items in directory");
                return;
            }
            MyConsole.NewMessage("<b><color=yellow>--- ITEMS: ---</color></b>");
            for (int i = 0; i < ItemList.items.Count; i++)
            {
                MyConsole.NewMessage("    [ " + ItemList.items[i].ID + " ]  " + ItemList.items[i].Name);
            }
            return;
        }

        else if (parsedText[0] == "abilities")
        {
            if (SkillList.skills.Count <= 0)
            {
                MyConsole.Error("No abilities in directory.");
                return;
            }
            MyConsole.NewMessage("<b><color=yellow>--- ABILITIES: ---</color></b>");
            for (int i = 0; i < SkillList.skills.Count; i++)
            {
                if (SkillList.skills[i].luaAction != null && SkillList.skills[i].luaAction.functionName != "")
                {
                    MyConsole.NewMessage("    [ " + SkillList.skills[i].ID + " ] " + SkillList.skills[i].Name);
                }
            }
            return;
        }

        else if (parsedText[0] == "questflag")
        {
            if (parsedText.Length < 2)
            {
                MyConsole.Error("Specify the flag to give.");
                return;
            }

            ProgressFlags flag = parsedText[1].ToEnum<ProgressFlags>();
            ObjectManager.playerJournal.AddFlag(flag);
            MyConsole.NewMessage("Gave the flag \"" + parsedText[1] + "\" to the journal.");
            return;
        }

        else if (parsedText[0] == "completequest")
        {
            if (ObjectManager.playerJournal.trackedQuest != null)
                ObjectManager.playerJournal.CompleteQuest(ObjectManager.playerJournal.trackedQuest);
            return;
        }

        else if (parsedText[0] == "unstuck" || parsedText[0] == "unstick")
        {
            playerEntity.ForcePosition(World.tileMap.CurrentMap.GetRandomFloorTile());
            MyConsole.NewMessage("Player position set to random floor tile.");
            return;
        }

        else if (parsedText[0] == "?" || parsedText[0] == "help" || parsedText[0] == "commands")
        {
            MyConsole.DoubleLine();
            MyConsole.NewMessage("");

            MyConsole.NewMessageColor("<b><i>INFO:</i></b>", Color.red);
            MyConsole.NewMessage(" Note: Spaces are used to parse the string.");
            MyConsole.NewMessageColor("  <b>Commands:</b>", Color.red);

            MyConsole.NewMessage("  -<b>unstuck/unstick</b>\n    Teleports you to a random floor tile if stuck.");
            MyConsole.NewHelpLine("location", "Displays the current world coordinate.");
            MyConsole.NewHelpLine("go [direction]", "Travel one screen in a direction. [direction] = \"up\", \"down\", \"north\", \"south\", \"east\", \"west\", \"surface\".\n" +
                "You can also travel to any landmark. Use: \"arena\", \"ensis\", \"cult\", \"cathedral\", \"powerplant\", \"xul\", \"volcano\", \"frostborne\", \"oasis\", \"magna\". or specify a biome ID.");
            MyConsole.NewHelpLine("setpos [x] [y]", "Travel to a specific world position. Constraints: 0-199 on each axis.");
            MyConsole.NewMessage("  -<b>godmode</b> <i>[0-1]</i>\n      [0] = off\n      [1] = on");
            MyConsole.NewMessage("  -<b>fov</b> <i>[0-1]</i>\n      Whether to show fog of war or not. \n0 = off  1 = on");
            MyConsole.NewMessage("  -<b>explore</b> <i>[0-1]</i>\n      [0] = off\n      [1] = on\n      Enables or disables map encounters.");
            MyConsole.NewMessage("  -<b>gold</b> <i>[amount]</i>\n      Gives [amount] gold to the player.");
            MyConsole.NewMessage("  -<b>spawn npc</b> <i>[ID] [x] [y]</i>\n      Spawns an NPC at a position relative to the player.");
            MyConsole.NewMessage("  -<b>spawn object</b> <i>[ID] [x] [y]</i>\n      Spawns an object at a position relative to the player.");

            MyConsole.NewMessage("  -<b>set <i>[stat] [value]</i></b>\n      Sets a specific stat to the selected value.");
            MyConsole.NewMessage("  -<b>xp <i>[amount]</i></b>\n      gain [amount] XP.");
            MyConsole.NewMessage("  -<b>sever</b> <i>[limb index] or \"random\"</i>\n      Severs numbered limb, or random.");
            MyConsole.NewMessage("  -<b>reattach</b> <i>[limb index] or \"all\"</i>\n      Re-attaches limb at index, or all.");

            MyConsole.NewMessage("  -<b>items</b>\n      Displays all item names with their IDs.");
            MyConsole.NewMessage("  -<b>give/grant</b> <i>[item name]</i>\n      Give a specified item to the player.");
            MyConsole.NewMessage("  -<b>multigive/multigrant</b> <i>[amount] [item name]</i>\n      Give a specific number of a specified item to the player.");
            MyConsole.NewMessage("  -<b>mods</b>\n    Lists all the item modifiers.");
            MyConsole.NewMessage("  -<b>modwep</b> <i>[mod ID]</i>\n    Modifies the first non-severed hand's equipped weapon with the selected modifier ID.");

            MyConsole.NewMessage("  -<b>abilities</b>\n      Displays all ability names with their IDs.");
            MyConsole.NewMessage("  -<b>levelabilities</b>\n    Gives all current abilities enough XP to level up. Does not work on abilities that do not gain XP.");
            MyConsole.NewMessage("  -<b>traits</b>\n      Displays all the trait names with their IDs.");
            MyConsole.NewMessage("  -<b>givetrait</b> <i>[trait ID]</i>\n      Give a specified trait or mutation to the player.");
            MyConsole.NewMessage("  -<b>removetrait</b> <i>[trait ID]</i>\n      Remove a specified trait or mutation from the player.");
            MyConsole.NewMessage("  -<b>mutate</b> <i>[mutation ID]</i>\n      Gives the player the specified mutation.");
            MyConsole.NewMessage("  -<b>unmutate</b>\n      Removes a random mutation.");
            MyConsole.NewMessage("  -<b>unmutate all</b>\n      Removes all mutations.");
            MyConsole.NewMessage("  -<b>giveskill/giveability</b> <i>[ability ID]</i>\n      Give a specified ability to the player.");
            MyConsole.NewMessage("  -<b>levelabilities</b>\n    Increases the level of all abilities by one.");

            MyConsole.NewMessage("  -<b>danger</b>\n      Displays the current world danger level.");
            MyConsole.NewMessage("  -<b>killme</b>\n      Kills your character.");
            MyConsole.NewMessage("  -<b>heal</b>\n      Heals the player fully.");
            MyConsole.NewMessage("  -<b>injure</b> <i>[amount]</i>\n      Injures the player by a certain amount.");
            MyConsole.NewMessage("  -<b>givestatus</b> <i>[effect name] [amount]</i>\n    Gives the player a particular status effect. Poison, Blind, Bleed, Haste, Regen, etc...");
            MyConsole.NewMessage("  -<b>followme</b>\n    All on-screen NPCs become followers.");

            MyConsole.NewMessage("  -<b>removeblockers<b>\n    Removes all blockers from the current screen.");
            MyConsole.NewMessage("  -<b>opendoors<b>\n    Opens all doors on the current screen, regardless of permissions.");
            MyConsole.NewMessage("  -<b>closedoors<b>\n    Closes all doors on the current screen, regardless of permissions.");

            MyConsole.NewMessage("  -<b>load <i>[map name]</i></b>\n    Loads a map by its name.");
            MyConsole.NewMessage("  -<b>detonate</b>\n      Kills all NPCs on the screen.");
            MyConsole.NewMessage("  -<b>reveal</b>\n      Reveals all tiles on the map");
            MyConsole.NewMessage("  -<b>wizard</b>\n      Combines the previous two commands. Kills all NPCs on screen, and reveals the map. Cuz... Cynapse is lazy.");
            MyConsole.NewMessage("  -<b>completequest</b>\n    Completes the current tracked quest.");
            MyConsole.NewMessage("  -<b>startquest</b> <i>[ID]</i>\n    Starts the quest with the input ID.");
            MyConsole.NewMessage("  -<b>questflag <i>[flag]</i></b>\n    Gives the player the input quest flag. Possibilities: " +
                "\n\tCan_Enter_Ensis, Can_Open_Prison_Cells, Can_Enter_Magna, Break_Prisoner_Inhibitor, Hostile_To_Kin, Hostile_To_Ensis, Hunts_Available, Arena_Available");
            MyConsole.NewMessage("  -<b>weather <i>[amount (0-3)]</i></b>\n    Sets the world weather to the appropriate number.");
            MyConsole.NewMessage("  -<b>5k</b>\n    Increases the turn counter by 5000.");
            MyConsole.NewMessage("  -<b>log</b>\n    Write a message to the combat log.");
            MyConsole.NewMessage("  -<b>limbtest</b>\n    Test for severing/attaching limbs.");

            MyConsole.DoubleLine();
            return;
        }

        //if no match. return all calls to not get this message
        MyConsole.Error("'" + textToParse + "' is not a valid command");
        textField = "";
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

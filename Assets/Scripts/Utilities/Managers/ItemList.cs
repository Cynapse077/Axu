using UnityEngine;
using System.Collections.Generic;
using System.IO;
using LitJson;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class ItemList
{
    const int modChance = 5;
    public static List<Item> items;
    public static List<Item> artifacts;
    public static List<ItemModifier> modifiers;
    public static List<Liquid> liquids;

    public static string itemDataPath;
    public static string natItemDataPath;
    public static string artDataPath;
    public static string modDataPath;
    public static string objDataPath;
    public static string liqDataPath;

    public static List<MapObjectBlueprint> mapObjectBlueprints;

    public static Item noneItem;
    public static int MaxRarity = 0;

    public static void CreateItems()
    {
        if (items != null)
        {
            return;
        }

        noneItem = new Item("<color=grey>None</color>")
        {
            ID = "none",
            lootable = false,
            itemType = Proficiencies.Unarmed,
            rarity = 100,
            flavorText = "There is nothing here."
        };

        liquids = GetLiquidsFromData();
        items = ItemsFromData();
        artifacts = ArtifactsFromData();
        modifiers = ModifiersFromData();
        mapObjectBlueprints = FillMOBlueprintList();
    }

    public static void RemoveItem(string name)
    {
        if (items.Find(x => x.Name == name) != null)
        {
            items.Remove(items.Find(x => x.Name == name));
        }
    }

    public static Item GetNone()
    {
        return noneItem;
    }

    public static Item GetRandart(Item i)
    {
        Item newItem = new Item(i);

        if ((i.HasProp(ItemProperty.Weapon) || i.HasProp(ItemProperty.Armor) || i.HasProp(ItemProperty.Ranged)) && !i.stackable && i.lootable)
        {
            newItem.RemoveModifier();

            newItem.displayName = "<color=magenta>" + NameGenerator.ArtifactName(SeedManager.textRandom) + "</color>";
            newItem.AddProperty(ItemProperty.Randart);

            int threshold = 25;

            if (i.HasProp(ItemProperty.Weapon))
            {
                newItem.damage += new Damage(SeedManager.combatRandom.Next(-1, 3), SeedManager.combatRandom.Next(-1, 3), SeedManager.combatRandom.Next(-2, 4));
                if (newItem.damage.Num <= 0)
                    newItem.damage.Num = 1;
                if (newItem.damage.Sides <= 0)
                    newItem.damage.Sides = 1;

                if (SeedManager.combatRandom.Next(100) <= threshold)
                {
                    if (newItem.ContainsDamageType(DamageTypes.Slash))
                        newItem.AddDamageType(DamageTypes.Cleave);
                    else
                    {
                        DamageTypes[] dt = new DamageTypes[] { DamageTypes.Cold, DamageTypes.Heat, DamageTypes.Energy, DamageTypes.Bleed };
                        newItem.AddDamageType(dt.GetRandom());
                    }

                    threshold /= 2;
                }

                if (SeedManager.combatRandom.Next(100) <= threshold)
                {
                    CItemLevel ci = new CItemLevel();
                    newItem.AddComponent(ci);
                    threshold /= 2;
                }
            }
            else if (i.HasProp(ItemProperty.Armor))
            {
                newItem.armor += SeedManager.combatRandom.Next(1, 4);

                int ranNum = SeedManager.combatRandom.Next(100);

                if (ranNum < 33)
                    newItem.statMods.Add(new Stat_Modifier("Cold Resist", SeedManager.combatRandom.Next(1, 5) * 5));
                else if (ranNum < 66)
                    newItem.statMods.Add(new Stat_Modifier("Heat Resist", SeedManager.combatRandom.Next(1, 5) * 5));
                else
                    newItem.statMods.Add(new Stat_Modifier("Energy Resist", SeedManager.combatRandom.Next(1, 5) * 5));

                threshold /= 2;
            }

            if (SeedManager.combatRandom.Next(100) <= threshold)
            {
                ItemProperty[] props = new ItemProperty[] {
                    ItemProperty.Knockback, ItemProperty.Confusion, ItemProperty.Radiate,
                    ItemProperty.Quick, ItemProperty.Shock_Nearby, ItemProperty.Slow,
                    ItemProperty.Stun, ItemProperty.Explosive, ItemProperty.DrainHealth
                };

                ItemProperty p = props.GetRandom(SeedManager.combatRandom);

                if (!newItem.ContainsProperty(p))
                {
                    newItem.AddProperty(p);

                    if (p == ItemProperty.Shock_Nearby)
                        newItem.AddCComponent<CLuaEvent>("OnHit", "ItemActions", "ShockAdjacent", "");
                    else if (p == ItemProperty.DrainHealth)
                        newItem.AddCComponent<CLuaEvent>("OnHit", "ItemActions", "DrainHealth", "");

                    threshold /= 2;
                }
            }

            if (SeedManager.combatRandom.Next(100) <= threshold)
            {
                string[] sk = new string[] {
                    "juke", "charge", "energize", "firestream", "icestream", "radblast", "firewall",
                    "confsquare", "blink", "sprint"
                };

                if (!newItem.HasCComponent<CAbility>())
                {
                    CAbility cab = new CAbility(sk.GetRandom(), SeedManager.combatRandom.Next(1, 4));
                    newItem.AddComponent(cab);

                    threshold /= 2;
                }
            }

            if (threshold >= 20)
                return i;
        }

        return newItem;
    }

    public static Item GetItemByID(string id)
    {
        if (string.IsNullOrEmpty(id) || id == "none" || id == "None" || id == GetNone().ID)
            return GetNone();
        else if (id.Contains("Random"))
            return GetRarityByName(id);
        else if (id == "randomartifact")
            return GetRandomArtifact();
        else if (id.Contains("/"))
        {
            string[] ss = id.Split("/"[0]);

            if (ss.Length < 2)
                return null;

            Item newItem = GetItemByID(ss[0]);

            if (newItem == null || !newItem.HasCComponent<CLiquidContainer>())
                return null;

            CLiquidContainer cl = newItem.GetCComponent<CLiquidContainer>();
            if (ss[1] == "liquid_random")
            {
                Liquid liq = new Liquid(liquids.GetRandom(SeedManager.combatRandom), cl.capacity);
                cl.Fill(liq);
            }
            else if (GetLiquidByID(ss[1]) != null)
            {
                Liquid liq = GetLiquidByID(ss[1], cl.capacity);
                cl.Fill(liq);
            }

            return newItem;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].ID == id)
            {
                Item item = new Item(items[i]);
                return item;
            }
        }

        for (int a = 0; a < artifacts.Count; a++)
        {
            if (artifacts[a].ID == id)
            {
                Item arti = new Item(artifacts[a]);
                return arti;
            }
        }

        return null;
    }

    //Really only used in console
    public static Item GetItemByName(string nam)
    {
        if (nam == "None" || nam == GetNone().Name)
            return GetNone();

        if (nam.Contains("Random"))
            return GetRarityByName(nam);

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Name == nam)
            {
                Item item = new Item(items[i]);
                return item;
            }
        }

        for (int a = 0; a < artifacts.Count; a++)
        {
            if (artifacts[a].Name == nam)
            {
                Item arti = new Item(artifacts[a]);
                return arti;
            }
        }

        return null;
    }

    static Item GetRarityByName(string rar)
    {
        if (rar.Contains("Random_R"))
        {
            string s = rar.Replace("Random_R", "");
            int rarity = int.Parse(s);
            return GetItemByRarity(rarity);
        }
        if (rar.Contains("Random_Weapon_R"))
        {
            string s = rar.Replace("Random_Weapon_R", "");
            int rarity = int.Parse(s);
            return GetWeaponByRarity(rarity);
        }
        if (rar.Contains("Random_Book"))
            return GetRandomBook();

        return null;
    }

    public static Item GetRandomArtifact()
    {
        List<Item> availableArtifacts = artifacts.FindAll(x => !x.HasProp(ItemProperty.Unique));

        if (availableArtifacts.Count > 0)
        {
            Item it = availableArtifacts.GetRandom();
            return new Item(it);
        }
        else
        {
            return GetItemByRarity(MaxRarity);
        }
    }

    public static ItemModifier GetModByID(string search)
    {
        if (search == "")
            return ItemModifier.Empty();

        return (modifiers.Find(x => x.ID == search));
    }

    public static int TimedDropRarity(int maxRarity)
    {
        int rarity = 1;
        float divisible = 5f + (World.turnManager.Day - 1);

        for (int r = 1; r < maxRarity; r++)
        {
            if (SeedManager.combatRandom.Next(100) < divisible / r)
                rarity++;
        }

        return rarity;
    }

    //These functions get items by matching params
    public static Item GetItemByRarity(int rar)
    {
        List<Item> itemsWithRarity = items.FindAll(x => x.rarity == rar);

        if (itemsWithRarity.Count <= 0 && rar > 1)
            return GetItemByRarity(rar - 1);

        Item item = new Item(itemsWithRarity.GetRandom(SeedManager.combatRandom));

        if (SeedManager.combatRandom.Next(1000) <= 7)
            return GetRandart(item);

        if (item.HasProp(ItemProperty.Ammunition))
            item.amount = SeedManager.combatRandom.Next(1, 6);

        if (item.HasCComponent<CLiquidContainer>() && SeedManager.combatRandom.Next(100) < 40)
        {
            CLiquidContainer cl = item.GetCComponent<CLiquidContainer>();
            cl.liquid = GetRandomLiquid(SeedManager.combatRandom.Next(1, cl.capacity + 1));
        }

        //We only check for adding modifiers if we are getting the item via rarity drops. AKA, it is a new instance of an item.
        TryAddMod(ref item, modChance);

        return item;
    }

    public static Item GetWeaponByRarity(int rar)
    {
        List<Item> itemsWithRarity = items.FindAll(x => x.rarity == rar && x.HasProp(ItemProperty.Weapon));
        Item item = new Item(itemsWithRarity.GetRandom(SeedManager.combatRandom));

        TryAddMod(ref item, modChance);

        return item;
    }

    public static Item GetItemWithSpecificProperties(params ItemProperty[] p)
    {
        List<Item> matchingItems = new List<Item>();

        for (int i = 0; i < p.Length; i++)
        {
            matchingItems.AddRange(items.FindAll(x => x.HasProp(p[i])));
        }

        return new Item(matchingItems.GetRandom(SeedManager.combatRandom));
    }

    public static Item GetRandomBook()
    {
        List<Item> possibleArtifacts = artifacts.FindAll(x => x.HasProp(ItemProperty.Tome));
        Item selection = possibleArtifacts.GetRandom(SeedManager.combatRandom);

        return new Item(selection);
    }

    public static void TryAddMod(ref Item i, int chance)
    {
        if (SeedManager.combatRandom.Next(100) <= chance)
        {
            Item newItem = new Item(i);
            List<ItemModifier> ms = modifiers.FindAll(m => !m.unique && m.CanAddToItem(newItem));

            if (ms.Count > 0)
            {
                ItemModifier m = new ItemModifier(ms.GetRandom(SeedManager.combatRandom));
                i.AddModifier(m);
            }
        }
    }

    public static Item GetSeveredBodyPart(BodyPart part)
    {
        switch (part.slot)
        {
            case ItemProperty.Slot_Head: return GetItemByID("severed_head");
            case ItemProperty.Slot_Tail: return GetItemByID("severed_tail");
            case ItemProperty.Slot_Wing: return GetItemByID("severed_wing");
            case ItemProperty.Slot_Arm: return GetItemByID("severed_arm");
            case ItemProperty.Slot_Leg: return GetItemByID("severed_leg");

            default: return null;
        }
    }

    //Items
    static List<Item> ItemsFromData()
    {
        string jsonString = File.ReadAllText(Application.streamingAssetsPath + itemDataPath);
        JsonData data = JsonMapper.ToObject(jsonString);
        List<Item> itemList = GetFromData(data, "Items");

        string jsonString2 = File.ReadAllText(Application.streamingAssetsPath + natItemDataPath);
        JsonData data2 = JsonMapper.ToObject(jsonString2);
        itemList.AddRange(GetFromData(data2, "Natural Items"));

        return itemList;
    }

    //Artifacts
    static List<Item> ArtifactsFromData()
    {
        string jsonString = File.ReadAllText(Application.streamingAssetsPath + artDataPath);

        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("Artifacts null.");
            return new List<Item>();
        }

        JsonData data = JsonMapper.ToObject(jsonString);

        return GetFromData(data, "Artifacts");
    }

    //Mods
    static List<ItemModifier> ModifiersFromData()
    {
        List<ItemModifier> modList = new List<ItemModifier>();
        string jsonString = File.ReadAllText(Application.streamingAssetsPath + modDataPath);
        JsonData data = JsonMapper.ToObject(jsonString);

        for (int i = 0; i < data["Mods"].Count; i++)
        {
            JsonData newData = data["Mods"][i];
            ItemModifier m = new ItemModifier
            {
                name = newData["Name"].ToString(),
                ID = newData["ID"].ToString(),
                description = newData["Description"].ToString()
            };

            string mType = newData["Mod Type"].ToString();
            m.modType = mType.ToEnum<ItemModifier.ModType>();
            m.cost = (int)newData["Cost"];

            if (newData.ContainsKey("Armor"))
                m.armor = (int)newData["Armor"];

            if (newData.ContainsKey("Unique"))
                m.unique = (bool)newData["Unique"];

            if (newData.ContainsKey("Damage Modifier"))
            {
                string mDamage = newData["Damage Modifier"].ToString();
                m.damage = Damage.GetByString(mDamage);
            }
            if (newData.ContainsKey("Properties"))
            {
                for (int p = 0; p < newData["Properties"].Count; p++)
                {
                    string tag = newData["Properties"][p].ToString();
                    m.properties.Add(tag.ToEnum<ItemProperty>());
                }
            }
            if (newData.ContainsKey("Damage Type"))
            {
                string tag = newData["Damage Type"].ToString();
                m.damageType = (tag.ToEnum<DamageTypes>());
            }
            if (newData.ContainsKey("Stat Mods"))
            {
                for (int p = 0; p < newData["Stat Mods"].Count; p++)
                {
                    string tag = newData["Stat Mods"][p]["Stat"].ToString();
                    int amount = (int)newData["Stat Mods"][p]["Amount"];
                    m.statMods.Add(new Stat_Modifier(tag, amount));
                }
            }

            if (newData.ContainsKey("Components"))
            {
                m.components = GetComponentsFromData(newData["Components"]);
            }

            modList.Add(m);
        }
        return modList;
    }

    //Item/Artifact
    public static List<Item> GetFromData(JsonData data, string listName)
    {
        List<Item> itemList = new List<Item>();
        JsonData jsonItemList = data[listName];

        for (int i = 0; i < jsonItemList.Count; i++)
        {
            JsonData currentItem = jsonItemList[i];
            Item it = new Item();
            if (currentItem.ContainsKey("ID"))
                it.ID = currentItem["ID"].ToString();
            it.Name = currentItem["Name"].ToString();

            if (currentItem.ContainsKey("TileID"))
                it.tileID = (int)currentItem["TileID"];

            if (currentItem.ContainsKey("Type"))
            {
                string type = currentItem["Type"].ToString();
                it.itemType = type.ToEnum<Proficiencies>();
            }

            if (currentItem.ContainsKey("Damage"))
            {
                string dmgString = currentItem["Damage"].ToString();
                it.damage = Damage.GetByString(dmgString);
            }
            else
                it.damage = new Damage(1, 3, 0, DamageTypes.Blunt);

            it.rarity = (currentItem.ContainsKey("Rarity")) ? (int)currentItem["Rarity"] : 100;
            if (it.rarity < 100 && it.rarity > MaxRarity)
                MaxRarity = it.rarity;

            if (currentItem.ContainsKey("Cost"))
                it.SetBaseCost((int)currentItem["Cost"]);

            it.lootable = (currentItem.ContainsKey("Lootable")) ? (bool)currentItem["Lootable"] : true;
            it.stackable = (currentItem.ContainsKey("Stackable")) ? (bool)currentItem["Stackable"] : false;

            if (currentItem.ContainsKey("Armor"))
                it.armor = (int)currentItem["Armor"];
            if (currentItem.ContainsKey("Accuracy"))
                it.accuracy = (int)currentItem["Accuracy"];
            if (currentItem.ContainsKey("FlavorText"))
                it.flavorText = currentItem["FlavorText"].ToString();

            if (currentItem.ContainsKey("Components"))
                it.SetComponentList(GetComponentsFromData(currentItem["Components"]));

            //Properties
            if (currentItem.ContainsKey("Properties"))
            {
                for (int p = 0; p < currentItem["Properties"].Count; p++)
                {
                    string prop = currentItem["Properties"][p].ToString();
                    ItemProperty pr = prop.ToEnum<ItemProperty>();
                    it.properties.Add(pr);
                }
            }

            //Damage Types
            if (currentItem.ContainsKey("DmgTypes"))
            {
                if (currentItem["DmgTypes"].Count > 0)
                    it.damageTypes.Clear();
                for (int d = 0; d < currentItem["DmgTypes"].Count; d++)
                {
                    string dmg = currentItem["DmgTypes"][d].ToString();
                    DamageTypes dt = dmg.ToEnum<DamageTypes>();
                    it.damageTypes.Add(dt);
                }
            }

            if (currentItem.ContainsKey("Attack Type"))
            {
                string atype = currentItem["Attack Type"].ToString();
                it.attackType = atype.ToEnum<Item.AttackType>();
            }
            else
            {
                if (it.ContainsDamageType(DamageTypes.Claw))
                    it.attackType = Item.AttackType.Claw;
                else
                    it.attackType = Item.AttackType.Bash;
            }


            //Stat Modifiers
            it.statMods = new List<Stat_Modifier>();
            if (currentItem.ContainsKey("Stat Mods"))
            {
                for (int s = 0; s < currentItem["Stat Mods"].Count; s++)
                {
                    string statName = currentItem["Stat Mods"][s]["Stat"].ToString();
                    int amount = (int)currentItem["Stat Mods"][s]["Amount"];

                    it.statMods.Add(new Stat_Modifier(statName, amount));
                }
            }

            if (currentItem.ContainsKey("Display"))
            {
                string ground = (currentItem["Display"].ContainsKey("On Ground")) ? currentItem["Display"]["On Ground"].ToString() : "";
                string player = (currentItem["Display"].ContainsKey("On Player")) ? currentItem["Display"]["On Player"].ToString() : "";
                string slot = (currentItem["Display"].ContainsKey("Layer")) ? currentItem["Display"]["Layer"].ToString() : "";
                it.renderer = new Item.ItemRenderer(GetSlot(slot), ground, player);
            }

            itemList.Add(it);
        }
        return itemList;
    }

    static int GetSlot(string slot)
    {
        if (slot == "Back") return 0;
        else if (slot == "Offhand") return 1;
        else if (slot == "Torso") return 2;
        else if (slot == "Leg") return 3;
        else if (slot == "Arm") return 4;
        else if (slot == "Cloak") return 5;
        else if (slot == "Head") return 6;
        else if (slot == "Neck") return 7;
        else if (slot == "Weapon") return 8;

        return -1;
    }

    public static List<CComponent> GetComponentsFromData(JsonData data)
    {
        List<CComponent> comps = new List<CComponent>();

        for (int i = 0; i < data.Count; i++)
        {
            string ID = data[i]["ID"].ToString();

            if (ID == "Charges")
            {
                int charges = (int)data[i]["Max"];
                CCharges cc = new CCharges(charges, charges);
                comps.Add(cc);

            }
            else if (ID == "Firearm")
            {
                int shots = (int)data[i]["ShotsPerTurn"];
                int max = (int)data[i]["Max"];
                string ammoID = data[i]["AmmoID"].ToString();
                CFirearm cc = new CFirearm(max, max, shots, ammoID);
                comps.Add(cc);

            }
            else if (ID == "Rot")
            {
                int charges = (int)data[i]["Max"];
                CRot cc = new CRot(charges);
                comps.Add(cc);

            }
            else if (ID == "Ability")
            {
                int lvl = (data[i].ContainsKey("Level") ? (int)data[i]["Level"] : 1);
                CAbility cc = new CAbility(data[i]["Ability"].ToString(), lvl);
                comps.Add(cc);
            }
            else if (ID == "Coordinate")
            {
                CCoordinate cc = new CCoordinate(new Coord(0, 0), new Coord(0, 0), 0, "", false);
                comps.Add(cc);
            }
            else if (ID == "Console")
            {
                CConsole cc = new CConsole(data[i]["Action"].ToString(), data[i]["Command"].ToString());
                comps.Add(cc);
            }
            else if (ID == "LuaEvent")
            {
                string exP = (data[i].ContainsKey("Param")) ? data[i]["Param"].ToString() : "";
                CLuaEvent cl = new CLuaEvent(data[i]["Trigger"].ToString(), data[i]["File"].ToString(), data[i]["Function"].ToString(), exP);
                comps.Add(cl);
            }
            else if (ID == "LiquidContainer")
            {
                int cap = (int)data[i]["Capacity"];
                CLiquidContainer cl = new CLiquidContainer(cap);
                comps.Add(cl);
            }
            else if (ID == "Block")
            {
                int amt = (int)data[i]["Level"];
                CBlock cb = new CBlock(amt);
                comps.Add(cb);
            }
            else if (ID == "Coat")
            {
                //TODO: What do?
            }
            else if (ID == "ModKit")
            {
                string modID = data[i]["Mod ID"].ToString();
                CModKit cm = new CModKit(modID);
                comps.Add(cm);
            }
            else if (ID == "ItemLevel")
            {
                CItemLevel ci = new CItemLevel();
                comps.Add(ci);
            }
            else
            {
                Debug.LogError("Need to assign the new component to ItemList::GetComponentsFromData() - " + ID);
            }
        }

        return comps;
    }

    public static MapObjectBlueprint GetMOB(string objType)
    {
        for (int i = 0; i < mapObjectBlueprints.Count; i++)
        {

            if (objType == mapObjectBlueprints[i].objectType)
                return mapObjectBlueprints[i];
        }

        MyConsole.NewMessageColor("MapObjectBlueprint()::ERROR: Could not find blueprint with type " + objType.ToString(), Color.red);
        return null;
    }

    static List<MapObjectBlueprint> FillMOBlueprintList()
    {
        List<MapObjectBlueprint> bps = mapObjectBlueprints = new List<MapObjectBlueprint>();

        string jsonString = File.ReadAllText(Application.streamingAssetsPath + objDataPath);
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("Object Blueprints null.");
            return bps;
        }

        JsonData data = JsonMapper.ToObject(jsonString);

        for (int i = 0; i < data["Objects"].Count; i++)
        {
            JsonData newData = data["Objects"][i];
            MapObjectBlueprint mob = new MapObjectBlueprint
            {
                Name = newData["Name"].ToString(),
                objectType = newData["ObjectType"].ToString(),

                spriteID = newData["Sprite"].ToString(),
                description = newData["Description"].ToString()
            };

            if (newData.ContainsKey("Container"))
                mob.container = new MapObjectBlueprint.Container((int)newData["Container"]["Capacity"]);

            if (newData.ContainsKey("Tint"))
            {
                double x = (double)newData["Tint"][0], y = (double)newData["Tint"][1];
                double z = (double)newData["Tint"][2], w = (double)newData["Tint"][3];
                mob.tint = new Vector4((float)x, (float)y, (float)z, (float)w);
            }

            if (newData.ContainsKey("Path Cost"))
                mob.pathCost = (int)newData["Path Cost"];

            if (newData.ContainsKey("Physics"))
            {
                string phys = newData["Physics"].ToString();
                mob.solid = phys.ToEnum<MapObjectBlueprint.MapOb_Interactability>();
            }

            if (newData.ContainsKey("Opaque"))
                mob.opaque = (bool)newData["Opaque"];
            if (newData.ContainsKey("Autotile")) 
                mob.autotile = (bool)newData["Autotile"];
            if (newData.ContainsKey("Render In Back"))
                mob.renderInBack = (bool)newData["Render In Back"];
            if (newData.ContainsKey("Render In Front"))
                mob.renderInFront = (bool)newData["Render In Front"];
            if (newData.ContainsKey("Random Rotation"))
                mob.randomRotation = (bool)newData["Random Rotation"];
            if (newData.ContainsKey("Light"))
                mob.light = (int)newData["Light"];

            if (newData.ContainsKey("Pulse"))
            {
                if (newData["Pulse"].ContainsKey("Send"))
                {
                    mob.pulseInfo.send = (bool)newData["Pulse"]["Send"];
                }

                if (newData["Pulse"].ContainsKey("Receive"))
                {
                    mob.pulseInfo.receive = (bool)newData["Pulse"]["Receive"];
                }

                if (newData["Pulse"].ContainsKey("Reverse"))
                {
                    mob.pulseInfo.reverse = (bool)newData["Pulse"]["Reverse"];
                }
            }

            if (newData.ContainsKey("LuaEvents"))
            {
                for (int j = 0; j < newData["LuaEvents"].Count; j++)
                {
                    JsonData luaEvent = newData["LuaEvents"][j];
                    string key = luaEvent["Event"].ToString();
                    LuaCall lc = new LuaCall(luaEvent["File"].ToString(), luaEvent["Function"].ToString());

                    mob.luaEvents.Add(key, lc);
                }
            }

            bps.Add(mob);
        }

        return bps;
    }

    public static Liquid GetLiquidByID(string search, int amount = -1)
    {
        for (int i = 0; i < liquids.Count; i++)
        {
            if (liquids[i].ID == search)
                return new Liquid(liquids[i], (amount == -1 ? 1 : amount));
        }

        return null;
    }

    public static Liquid GetRandomLiquid(int amount = -1)
    {
        Liquid l = Utility.WeightedChoice(liquids);

        return new Liquid(l, (amount == -1) ? 1 : amount);
    }

    static List<Liquid> GetLiquidsFromData()
    {
        List<Liquid> liquidList = new List<Liquid>();
        string jsonString = File.ReadAllText(Application.streamingAssetsPath + liqDataPath);
        JsonData data = JsonMapper.ToObject(jsonString);

        for (int i = 0; i < data["Liquids"].Count; i++)
        {
            liquidList.Add(new Liquid(data["Liquids"][i]));
        }

        if (data.ContainsKey("Mixing Tables"))
            Liquid.SetupMixingTables(data);

        return liquidList;
    }
}
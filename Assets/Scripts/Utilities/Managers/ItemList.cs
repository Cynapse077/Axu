using UnityEngine;
using System.Collections.Generic;
using System;
using LitJson;
using System.Linq;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class ItemList
{
    const int modChance = 5;

    public static Item GetNone()
    {
        return new Item(GameData.Get<Item>("none") as Item);
    }

    public static Item GetRandart(Item i)
    {
        Item newItem = new Item(i);

        if ((i.HasProp(ItemProperty.Weapon) || i.HasProp(ItemProperty.Armor) || i.HasProp(ItemProperty.Ranged)) && !i.stackable && i.lootable)
        {
            newItem.Reset();

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
                int amt = SeedManager.combatRandom.Next(1, 5) * 5;

                if (ranNum < 33)
                    newItem.statMods.Add(new Stat_Modifier("Cold Resist", amt));
                else if (ranNum < 66)
                    newItem.statMods.Add(new Stat_Modifier("Heat Resist", amt));
                else
                    newItem.statMods.Add(new Stat_Modifier("Energy Resist", amt));

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
                    "juke", "charge", "firestream", "icestream", "radblast", "firewall", "confsquare", "sprint"
                };

                if (!newItem.HasCComponent<CAbility>())
                {
                    CAbility cab = new CAbility(sk.GetRandom(), SeedManager.combatRandom.Next(1, 4));
                    newItem.AddComponent(cab);

                    threshold /= 2;
                }
            }

            if (threshold >= 20)
            {
                return i;
            }
        }

        return newItem;
    }

    public static Item GetItemByID(string id)
    {
        if (string.IsNullOrEmpty(id) || id == GetNone().ID)
            return GetNone();
        else if (id.Contains("Random"))
        {
            return GetRarityByName(id);
        }
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
                Liquid liq = GetRandomLiquid(cl.capacity);
                cl.Fill(liq);
            }
            else if (GetLiquidByID(ss[1]) != null)
            {
                Liquid liq = GetLiquidByID(ss[1], cl.capacity);
                cl.Fill(liq);
            }

            return newItem;
        }

        return new Item(GameData.Get<Item>(id) as Item);
    }

    //Really only used in console
    public static Item GetItemByName(string nam)
    {
        if (nam == "None" || nam == GetNone().Name)
            return GetNone();

        if (nam.Contains("Random"))
            return GetRarityByName(nam);

        Predicate<IAsset> p = (IAsset asset) => {
            Item i = asset as Item;

            if (i != null)
            {
                return i.Name == nam;
            }

            return false;
        };

        List<IAsset> ass = GameData.Get<Item>(p);

        if (ass.Count > 0)
        {
            return new Item(ass[0] as Item);
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
        else if (rar.Contains("Random_Weapon_R"))
        {
            string s = rar.Replace("Random_Weapon_R", "");
            int rarity = int.Parse(s);
            return GetWeaponByRarity(rarity);
        }
        else if (rar.Contains("Random_Book"))
        {
            Predicate<IAsset> p = (IAsset asset) => {
                Item i = asset as Item;

                if (i != null)
                {
                    return i.HasProp(ItemProperty.Tome);
                }

                return false;
            };

            return GameData.Get<Item>(p).Cast<Item>().ToList().GetRandom();
        }

        return null;
    }

    public static ItemModifier GetModByID(string search)
    {
        if (string.IsNullOrEmpty(search))
        {
            return ItemModifier.Empty();
        }

        return new ItemModifier(GameData.Get<ItemModifier>(search) as ItemModifier);
    }

    public static int TimedDropRarity(int maxRarity)
    {
        int rarity = 1;
        float divisible = 5f + (World.turnManager.Day - 1);

        for (int r = 1; r < maxRarity; r++)
        {
            if (SeedManager.combatRandom.Next(100) < divisible / r)
            {
                rarity++;
            }
        }

        return rarity;
    }

    //These functions get items by matching params
    public static Item GetItemByRarity(int rar)
    {
        Predicate<IAsset> p = (IAsset asset) => {
            Item it = (asset as Item);

            if (it != null)
            {
                return it.rarity == rar;
            }

            return false;
        };

        return GetRandomPredicatedItem(p);
    }

    static Item GetRandomPredicatedItem(Predicate<IAsset> p, bool addMod = true)
    {
        Item item = new Item(GameData.Get<Item>(p).GetRandom() as Item);

        if (addMod)
        {
            TryAddMod(ref item, modChance);
        }

        return item;
    }

    public static Item GetWeaponByRarity(int rar)
    {
        Predicate<IAsset> p = (IAsset asset) => {
            Item it = asset as Item;

            if (it != null)
            {
                return it.HasProp(ItemProperty.Weapon) && it.rarity == rar;
            }

            return false;
        };

        return GetRandomPredicatedItem(p);
    }

    public static void TryAddMod(ref Item i, int chance)
    {
        if (SeedManager.combatRandom.Next(100) <= chance)
        {
            Item newItem = new Item(i);
            Predicate<IAsset> p = (IAsset asset) => {
                ItemModifier mod = asset as ItemModifier;

                if (mod != null)
                {
                    return !mod.unique && mod.CanAddToItem(newItem);
                }

                return false;
            };
            List<IAsset> ms = GameData.Get<ItemModifier>(p);

            if (ms.Count > 0)
            {
                ItemModifier m = new ItemModifier(ms.GetRandom(SeedManager.combatRandom) as ItemModifier);
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

    public static MapObjectBlueprint GetMOB(string objType)
    {
        return GameData.Get<MapObjectBlueprint>(objType) as MapObjectBlueprint;
    }

    public static Liquid GetLiquidByID(string search, int amount = -1)
    {
        return new Liquid(GameData.Get<Liquid>(search) as Liquid, amount < 0 ? 1 : amount);
    }

    public static Liquid GetRandomLiquid(int amount = -1)
    {
        return new Liquid(Utility.WeightedChoice(GameData.GetAll<Liquid>()), amount < 0 ? 1 : amount);
    }
}

public static class ItemUtility
{
    public static int MaxRarity = 0;

    public static List<CComponent> GetComponentsFromData(JsonData data)
    {
        List<CComponent> comps = new List<CComponent>();

        for (int i = 0; i < data.Count; i++)
        {
            string ID = data[i]["ID"].ToString();

            if (ID == "Charges")
            {
                int charges = (int)data[i]["Max"];
                CCharges cc = new CCharges(charges);
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
                //Empty
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
            else if (ID == "Requirement")
            {
                CRequirement cr = new CRequirement();

                for (int j = 0; j < data[i]["Requirements"].Count; j++)
                {
                    cr.req.Add(new StringInt(data[i]["Requirements"][j]["Stat"].ToString(), (int)data[i]["Requirements"][j]["Amount"]));
                }

                comps.Add(cr);
            }
            else
            {
                Debug.LogError("Need to assign the new component to ItemList::GetComponentsFromData() - " + ID);
            }
        }

        return comps;
    }

    public static int GetSlot(string slot)
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
}
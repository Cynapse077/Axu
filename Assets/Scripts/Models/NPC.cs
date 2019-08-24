using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[Serializable]
[MoonSharpUserData]
public class NPC
{
    public string name = "", ID, spriteID;
    public int UID;
    public Coord worldPosition, localPosition;
    public int elevation;
    public Faction faction;
    public bool isHostile, hostilityOverride, onScreen = false, isAlive, hasSeenPlayer;
    public Dictionary<string, int> Attributes;
    public List<Item> handItems;
    public Item firearm;
    public List<Item> inventory = new List<Item>();
    public List<BodyPart> bodyParts = new List<BodyPart>();
    public List<NPC_Flags> flags = new List<NPC_Flags>();
    public List<string> traits = new List<string>();
    public string corpseItem;
    public string questID, dialogueID;
    public int maxHealth, maxStamina;
    public int weaponSkill;

    Random RNG
    {
        get { return SeedManager.combatRandom; }
    }

    public NPC(string _npcID, Coord wPos, Coord lPos, int ele)
    {
        Attributes = NPC_Blueprint.DefaultAttributes();
        FromBlueprint(EntityList.GetBlueprintByID(_npcID));

        worldPosition = wPos;
        localPosition = lPos;
        elevation = ele;
    }

    public NPC(NPC_Blueprint bp, Coord wPos, Coord lPos, int ele)
    {
        Attributes = NPC_Blueprint.DefaultAttributes();
        FromBlueprint(bp);

        worldPosition = wPos;
        localPosition = lPos;
        elevation = ele;
    }

    public void AssignStats()
    {
        if (maxHealth < 0)
        {
            maxHealth = 0;
        }

        isHostile = faction.HostileToPlayer();
    }

    public bool CanSpawnThisNPC(Coord pos, int elev)
    {
        if (!isAlive || onScreen)
        {
            return false;
        }

        return (pos == worldPosition && elev == elevation);
    }

    public bool HasFlag(NPC_Flags flag)
    {
        return (flags.Contains(flag));
    }

    public bool IsFollower()
    {
        return HasFlag(NPC_Flags.Follower) || faction.ID == "followers";
    }

    public bool ShouldShuffleInventory()
    {
        return HasFlag(NPC_Flags.Merchant) || HasFlag(NPC_Flags.Book_Merchant) || HasFlag(NPC_Flags.Doctor);
    }

    public SStats GetSimpleStats()
    {
        return new SStats(maxHealth, maxStamina, new Dictionary<string, int>(Attributes), new Dictionary<string, int>(), 0, null);
    }

    void FromBlueprint(NPC_Blueprint blueprint)
    {
        name = blueprint.name;
        ID = blueprint.ID;
        faction = blueprint.faction;
        dialogueID = blueprint.dialogue;
        maxHealth = blueprint.health;
        maxStamina = blueprint.stamina;
        AssignStats();

        Attributes = new Dictionary<string, int>(blueprint.attributes);
        bodyParts = new List<BodyPart>(blueprint.bodyParts);
        flags = new List<NPC_Flags>(blueprint.flags);
        spriteID = blueprint.spriteIDs.GetRandom(SeedManager.combatRandom);
        weaponSkill = blueprint.weaponSkill;

        string wepID = blueprint.weaponPossibilities.GetRandom(SeedManager.combatRandom);
        Item wep = ItemList.GetItemByID(wepID);

        if (wep == null)
        {
            UnityEngine.Debug.LogError("Weapon with ID \"" + wepID + "\" not found. From NPC Blueprint \"" + blueprint.ID + "\".");
            wep = ItemList.GetItemByID("fists");
        }

        firearm = string.IsNullOrEmpty(blueprint.firearm) ? ItemList.GetNone() : ItemList.GetItemByID(blueprint.firearm);

        handItems = new List<Item> { wep };

        if (!wep.lootable)
        {
            handItems.Add(new Item(wep));
        }

        ShuffleInventory(blueprint);

        if (!string.IsNullOrEmpty(blueprint.quest))
        {
            questID = blueprint.quest;
        }

        if (blueprint.Corpse_Item != null)
            corpseItem = blueprint.Corpse_Item;

        if (HasFlag(NPC_Flags.Named_NPC))
        {
            name = NameGenerator.CharacterName(SeedManager.textRandom);
        }
        else if (!HasFlag(NPC_Flags.Static) && SeedManager.combatRandom.Next(500) < World.DangerLevel() + 1)
        {
            List<Trait> muts = GameData.GetAll<Trait>().FindAll(x => x.effects.Contains(TraitEffects.Mutation));

            for (int i = 0; i < SeedManager.combatRandom.Next(1, 4); i++)
            {
                if (muts.Count > 0)
                {
                    Trait t = muts.GetRandom();
                    traits.Add(t.ID);
                }
            }
        }

        isAlive = true;
        onScreen = false;

        UID = ObjectManager.SpawnedNPCs;
        ObjectManager.SpawnedNPCs++;
    }

    void ShuffleInventory(NPC_Blueprint blueprint)
    {
        inventory = new List<Item>();

        if (blueprint.maxItems > 0)
        {
            if (RNG.Next(1000) <= (1.25f * blueprint.maxItemRarity))
            {
                //Get random artifact
                Predicate<IAsset> p = (IAsset asset) => {
                    Item i = asset as Item;

                    if (i != null)
                        return i.HasProp(ItemProperty.Artifact) && !i.HasProp(ItemProperty.Unique) && !i.HasProp(ItemProperty.Quest_Item);

                    return false;
                };
                Item item = GameData.Get<Item>(p).GetRandom() as Item;

                if (item != null)
                {
                    inventory.Add(new Item(item));
                }
            }

            int numItems = RNG.Next(HasFlag(NPC_Flags.Merchant) ? 4 : 0, blueprint.maxItems + 2);

            for (int i = 0; i < numItems; i++)
            {
                inventory.Add(ItemList.GetItemByRarity(ItemList.TimedDropRarity(blueprint.maxItemRarity)));
            }

            if (RNG.Next(100) < 10)
            {
                Item ammo = ItemList.GetItemByID("arrow");
                ammo.amount = RNG.Next(5, 25);
                inventory.Insert(0, ammo);
            }

            if (RNG.Next(100) < 8)
            {
                Item ammo = ItemList.GetItemByID("bolt");
                ammo.amount = RNG.Next(5, 25);
                inventory.Insert(0, ammo);
            }

            if (RNG.Next(100) < 5)
            {
                Item ammo = ItemList.GetItemByID("bullet");
                ammo.amount = RNG.Next(1, 25);
                inventory.Insert(0, ammo);
            }
        }

        if (blueprint.inventory.Length > 0)
        {
            foreach (KeyValuePair<string, Coord> kvp in blueprint.inventory)
            {
                Item i = ItemList.GetItemByID(kvp.Key);
                int amount = RNG.Next(kvp.Value.x, kvp.Value.y + 1);

                if (amount > 0)
                {
                    if (i.stackable)
                    {
                        i.amount = amount;
                    }
                    else
                    {
                        for (int t = 1; t < amount; t++)
                        {
                            Item i2 = ItemList.GetItemByID(kvp.Key);
                            inventory.Add(new Item(i2));
                        }
                    }

                    inventory.Add(i);
                }
            }
        }

        if (HasFlag(NPC_Flags.Doctor))
        {
            Predicate<IAsset> p = (IAsset asset) => {
                Item i = asset as Item;

                if (i != null)
                    return i.HasProp(ItemProperty.ReplaceLimb) && !i.HasCComponent<CRot>();

                return false;
            };

            List<IAsset> items = GameData.Get<Item>(p);

            for (int i = 0; i < RNG.Next(0, 2); i++)
            {
                inventory.Add(items.GetRandom(RNG) as Item);
            }
        }
    }

    public void MakeFollower()
    {
        faction = GameData.Get<Faction>("followers") as Faction;
        flags.Add(NPC_Flags.Follower);

        if (flags.Contains(NPC_Flags.Stationary_While_Passive))
        {
            flags.Remove(NPC_Flags.Stationary_While_Passive);
        }
    }

    public void AddFlag(NPC_Flags fl)
    {
        flags.Add(fl);
    }

    public void ReshuffleInventory()
    {
        ShuffleInventory(EntityList.GetBlueprintByID(ID));
    }

    public bool CanDiscard()
    {
        if (worldPosition == World.tileMap.CurrentMap.mapInfo.position)
        {
            return false;
        }

        if (HasFlag(NPC_Flags.Static) || HasFlag(NPC_Flags.Merchant) || HasFlag(NPC_Flags.Book_Merchant) 
            || HasFlag(NPC_Flags.Doctor) || HasFlag(NPC_Flags.Follower) || HasFlag(NPC_Flags.Named_NPC))
        {
            return false;
        }

        List<Quest> quests = ObjectManager.playerJournal.quests;

        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i].spawnedNPCs.Contains(UID))
            {
                return false;
            }
        }

        return true;
    }
}

[Serializable]
public enum NPC_Flags
{
    Static, Stationary, Stationary_While_Passive, Merchant,
    Follower, At_Home,
    Deteriortate_HP, Mercenary, Boss, Arena_Master,
    Flying, Aquatic, No_Blood, No_Body, Named_NPC, Human, Doctor, Radiation, Quantum_Locked, Book_Merchant,

    Skills_Leprosy, Summon_Adds, OnDeath_Explode, OnDeath_PoisonGas, Hit_And_Run, Inactive,

    Can_Speak, Can_Open_Doors, Solid_Limbs, No_Melee, RPois, RBleed, Resist_Webs, OnDisable_Regen, No_Strangle, No_TakeDown,
    NO_XP, Vampire
}

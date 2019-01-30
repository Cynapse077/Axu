using System.Collections.Generic;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Trait
{
    public string _name { get; protected set; }
    public string ID { get; protected set; }
    public string description;

    public bool stackable;
    public int tier, turnAcquired;
    public int maxStacks = 1;
    public string prerequisite, nextTier, slot;

    public List<Stat_Modifier> stats = new List<Stat_Modifier>();
    public List<TraitEffects> effects = new List<TraitEffects>();
    public List<string> abilityIDs = new List<string>();
    public ReplaceBodyPart replaceBodyPart;
    public LuaCall luaCall;

    public string name
    {
        get
        {
            if (effects.Contains(TraitEffects.Mutation))
                return "<color=magenta>" + _name + "</color>";
            else if (effects.Contains(TraitEffects.Disease))
                return "<color=red>" + _name + "</color>";

            return _name;
        }
        set { _name = value; }
    }

    public Trait(string _name, string _id)
    {
        name = _name;
        ID = _id;

        stats = new List<Stat_Modifier>();
        effects = new List<TraitEffects>();
        abilityIDs = new List<string>();
        replaceBodyPart = null;
    }

    public Trait(Trait other)
    {
        name = other._name;
        ID = other.ID;
        description = other.description;
        nextTier = other.nextTier;
        prerequisite = other.prerequisite;
        tier = other.tier;

        stackable = other.stackable;

        stats = other.stats;
        effects = other.effects;
        abilityIDs = other.abilityIDs;
        slot = other.slot;

        replaceBodyPart = other.replaceBodyPart;
        turnAcquired = other.turnAcquired;
    }

    public int GetStatIncrease(string name)
    {
        if (stats == null || stats.Count <= 0 || stats.Find(x => x.Stat == name) == null)
        {
            return 0;
        }

        return stats.Find(x => x.Stat == name).Amount;
    }

    //Assign new stats for mutation on player's stats.
    public void Initialize(Stats playerStats, bool remove = false)
    {
        if (!effects.Contains(TraitEffects.Addiction))
        {
            for (int i = 0; i < stats.Count; i++)
            {
                if (remove)
                {
                    int statAmount = stats[i].Amount * -1;
                    stats[i].Amount = statAmount;
                }

                if (playerStats.proficiencies.Profs.ContainsKey(stats[i].Stat))
                    playerStats.proficiencies.Profs[stats[i].Stat].level += stats[i].Amount;
                else if (stats[i].Stat == "Health")
                {
                    playerStats.maxHealth += stats[i].Amount;
                    playerStats.health += stats[i].Amount;
                }
                else if (stats[i].Stat == "Stamina")
                {
                    playerStats.maxStamina += stats[i].Amount;
                    playerStats.stamina += stats[i].Amount;
                }
                else if (stats[i].Stat == "Storage Capacity")
                    continue;
                else if (playerStats.Attributes.ContainsKey(stats[i].Stat))
                    playerStats.Attributes[stats[i].Stat] += stats[i].Amount;
            }
        }

        if (remove)
        {
            playerStats.traits.Remove(this);
        }

        if (replaceBodyPart != null)
        {
            ReplaceBodyParts(playerStats.entity, remove);
        }
    }

    void RemoveOtherMuts(Stats stats)
    {
        List<Trait> playerMuts = stats.Mutations;

        for (int m = 0; m < playerMuts.Count; m++)
        {
            if (playerMuts[m].ID == ID || playerMuts[m] == this)
                continue;

            if (playerMuts[m].replaceBodyPart != null && replaceBodyPart.slot == playerMuts[m].replaceBodyPart.slot
                && playerMuts[m].replaceBodyPart.allOfType)
            {
                playerMuts[m].Initialize(stats, true);
            }
        }
    }

    public void OnTurn_Update(Entity entity)
    {
        if (luaCall != null && !string.IsNullOrEmpty(luaCall.functionName) && !string.IsNullOrEmpty(luaCall.scriptName))
        {
            LuaManager.CallScriptFunction(luaCall.scriptName, luaCall.functionName, entity, this);
        }
    }

    //For MUTATIONS ONLY:
    //This replaces body parts with other types, deals with whether or not
    //they can equip gear, and destroys the old gear if necessary.
    //It can also grow new limbs and remove old ones.
    void ReplaceBodyParts(Entity entity, bool remove = false)
    {
        if (!remove)
        {
            //Remove other mutations that affected the character's same body parts.
            if (replaceBodyPart.allOfType)
            {
                RemoveOtherMuts(entity.stats);
            }

            List<BodyPart> bps = entity.body.GetBodyPartsBySlot(replaceBodyPart.slot);

            for (int i = 0; i < bps.Count; i++)
            {
                //If it's not organic, skip it.
                if (FlagsHelper.IsSet(bps[i].flags, BodyPart.BPTags.Synthetic) || FlagsHelper.IsSet(bps[i].flags, BodyPart.BPTags.External))
                {
                    continue;
                }

                //Remove all parts of type (for serpentine)
                if (replaceBodyPart.removeAll)
                {
                    entity.body.bodyParts.Remove(bps[i]);
                    bps[i].Remove(entity.stats);
                    continue;
                }

                //Rename parts
                if (replaceBodyPart.allOfType)
                {
                    FlagsHelper.Toggle(ref bps[i].flags, BodyPart.BPTags.CannotWearGear, !replaceBodyPart.canWearGear);

                    if (!replaceBodyPart.canWearGear)
                    {
                        entity.inventory.PickupItem(bps[i].equippedItem);
                        bps[i].equippedItem = ItemList.GetNone();
                    }

                    if (replaceBodyPart.newEquippedItem != null)
                    {
                        bps[i].equippedItem = ItemList.GetItemByID(replaceBodyPart.newEquippedItem);
                    }
                }
                else
                {
                    if (i == 0)
                    {
                        FlagsHelper.Toggle(ref bps[i].flags, BodyPart.BPTags.CannotWearGear, !replaceBodyPart.canWearGear);
                    }
                }
            }

            //Adding extra limbs
            if (replaceBodyPart.extraLimbs != null)
            {
                for (int j = 0; j < replaceBodyPart.extraLimbs.Count; j++)
                {
                    BodyPart additionalPart = new BodyPart(replaceBodyPart.extraLimbs[j]);

                    if (additionalPart.slot == ItemProperty.Slot_Arm)
                    {
                        additionalPart.hand = new BodyPart.Hand(additionalPart, new Item(additionalPart.equippedItem), additionalPart.equippedItem.ID);
                    }

                    entity.body.bodyParts.Add(additionalPart);
                    additionalPart.Attach(entity.stats);
                }
            }
        }
        else
        {
            Remove(entity);
        }

        //Setup Hands' equipped items
        if (replaceBodyPart.slot == ItemProperty.Slot_Arm && replaceBodyPart.extraLimbs == null)
        {
            List<BodyPart.Hand> hands = entity.body.Hands;

            for (int i = 0; i < hands.Count; i++)
            {
                if (hands[i].EquippedItem.ID == hands[i].baseItem)
                {
                    hands[i].SetEquippedItem(ItemList.GetItemByID(replaceBodyPart.newEquippedItem), entity);
                }

                hands[i].baseItem = replaceBodyPart.newEquippedItem;
            }
        }

        //Sort limbs neatly.
        List<BodyPart> bodyParts = new List<BodyPart>(entity.body.bodyParts);
        entity.body.Categorize(bodyParts);
    }

    void Remove(Entity entity)
    {
        //Remove extra limbs
        if (replaceBodyPart.extraLimbs != null)
        {
            for (int j = 0; j < replaceBodyPart.extraLimbs.Count; j++)
            {
                if (entity.body.bodyParts.Find(x => x.name == replaceBodyPart.extraLimbs[j].name) != null)
                {
                    BodyPart bp = entity.body.bodyParts.Find(x => x.name == replaceBodyPart.extraLimbs[j].name);
                    entity.body.bodyParts.Remove(bp);
                    bp.Remove(entity.stats);
                }
            }

            //Attach old parts
            List<BodyPart> targetParts = EntityList.DefaultBodyStructure();

            if (replaceBodyPart.removeAll)
            {
                List<BodyPart> unremoval = targetParts.FindAll(x => x.slot == replaceBodyPart.slot);

                for (int i = 0; i < unremoval.Count; i++)
                {
                    entity.body.bodyParts.Add(unremoval[i]);
                    unremoval[i].Attach(entity.stats);
                }
            }
        }

        for (int i = 0; i < entity.body.bodyParts.Count; i++)
        {
            BodyPart part = entity.body.bodyParts[i];

            //Set the Can Wear Gear flag back to default.
            if (!replaceBodyPart.canWearGear)
            {
                FlagsHelper.UnSet(ref entity.body.bodyParts[i].flags, BodyPart.BPTags.CannotWearGear);
            }

            //Reset equipment
            if (replaceBodyPart.newEquippedItem != null && part.slot == replaceBodyPart.slot && part.equippedItem.ID == replaceBodyPart.newEquippedItem)
            {
                entity.body.bodyParts[i].equippedItem.OnUnequip(entity, false);
                entity.body.bodyParts[i].equippedItem = ItemList.GetNone();
            }
        }
    }

    public bool ContainsEffect(TraitEffects te)
    {
        return (effects != null && effects.Contains(te));
    }
}
[System.Serializable]
public enum TraitEffects
{
    None, Random_Trait, Mutation, Disease,
    Poison_Resist, Bleed_Resist, Cleave_Resist, Confusion_Resist, Stun_Resist, Resist_Webs,
    Fast_Swimming, Faster_Swimming, Fast_Metabolism, Slow_Metabolism, Zap_On_Hit,
    Leprosy, Crystallization, Vampirism, PreVamp, Drain_Int, No_Cure, Addiction, Rad_Resist
}

public class ReplaceBodyPart
{
    public ItemProperty slot;
    public string bodyPartName;
    public string newEquippedItem;
    public List<BodyPart> extraLimbs;

    public bool canWearGear;
    public bool allOfType;
    public bool removeAll;

    public ReplaceBodyPart(ItemProperty _slot, string _bodyPartName, string _newEquippedItem, bool _canWearGear, bool _allOfType)
    {
        slot = _slot;
        bodyPartName = _bodyPartName;
        newEquippedItem = _newEquippedItem;
        canWearGear = _canWearGear;
        allOfType = _allOfType;

    }
}

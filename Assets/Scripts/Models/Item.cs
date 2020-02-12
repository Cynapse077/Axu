using System.Collections.Generic;
using System.Linq;
using LitJson;
using MoonSharp.Interpreter;

[System.Serializable]
[MoonSharpUserData]
public class Item : ComponentHolder<CComponent>, IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public string Name, displayName = "", flavorText;
    public Proficiencies itemType;
    public int armor, amount = 1, accuracy, rarity, tileID = -1;
    public bool lootable, stackable = false;

    public HashSet<ItemProperty> properties = new HashSet<ItemProperty>();
    public HashSet<DamageTypes> damageTypes = new HashSet<DamageTypes>();
    public List<Stat_Modifier> statMods = new List<Stat_Modifier>();
    public AttackType attackType;
    public ItemModifier modifier;
    public ItemRenderer renderer;
    public Damage damage;

    int cost;

    public Damage TotalDamage()
    {
        Damage dmg = new Damage(damage + modifier.damage);

        if (HasCComponent<CItemLevel>())
        {
            dmg.Sides += GetCComponent<CItemLevel>().DamageBonus();
        }

        return dmg;
    }

    public int Accuracy
    {
        get { return accuracy + modifier.accuracy; }
    }

    public DamageTypes[] dTypeList
    {
        get { return new List<DamageTypes>(damageTypes).ToArray(); }
    }

    #region Constructors
    public Item()
    {
        Defaults();
    }

    public Item(Item other)
    {
        Defaults();
        CopyFrom(other);
    }

    public Item(JsonData dat)
    {
        Defaults();
        FromJson(dat);
    }

    void Defaults()
    {
        Name = "";
        lootable = true;
        amount = 1;
        damage = new Damage(1, 2, 0, DamageTypes.Blunt);
        modifier = new ItemModifier();
        itemType = Proficiencies.Misc_Object;
        attackType = AttackType.Bash;

        components = new List<CComponent>();
    }
    #endregion

    
    public void SetBaseCost(int amount)
    {
        cost = amount;
    }

    public bool PhysicalDamage()
    {
        return ContainsDamageType(DamageTypes.Slash) || ContainsDamageType(DamageTypes.Blunt) || ContainsDamageType(DamageTypes.Pierce);
    }

    public void UpdateUserSprite(Stats stats, bool wieldMainHand, bool remove)
    {
        if (stats.entity.isPlayer && !renderer.onPlayer.NullOrEmpty())
        {
            DynamicSpriteController dsc = stats.GetComponent<DynamicSpriteController>();

            if (HasProp(ItemProperty.Weapon))
            {
                if (wieldMainHand || itemType == Proficiencies.Shield)
                {
                    dsc.SetSprite(renderer, remove);
                }
            }
            else
            {
                dsc.SetSprite(renderer, remove);
            }
        }
    }

    #region On___ Functions
    public void OnEquip(Stats stats, bool wieldMainHand)
    {
        if (!OnEquipReject())
        {
            ChangeStats(stats, false);
        }

        UpdateUserSprite(stats, wieldMainHand, false);

        if (HasCComponent<CAbility>() && !HasProp(ItemProperty.Tome))
        {
            Ability sk = new Ability(GameData.Get<Ability>(GetCComponent<CAbility>().abID));

            if (sk != null)
            {
                stats.entity.skills.AddSkill(sk, Ability.AbilityOrigin.Item);
            }
        }

        RunCommands(wieldMainHand ? "OnWield" : "OnEquip", stats.entity);
    }

    public void OnUnequip(Entity entity, bool inMainHand)
    {
        if (!OnEquipReject())
        {
            ChangeStats(entity.stats, true);
        }

        UpdateUserSprite(entity.stats, inMainHand, true);

        if (HasCComponent<CAbility>() && !HasProp(ItemProperty.Tome))
        {
            CAbility cab = GetCComponent<CAbility>();

            //Remove the ability if it is not present on other equipment.
            if (!entity.inventory.EquippedItems().Any(x => x.HasCComponent<CAbility>() && x.GetCComponent<CAbility>().abID == cab.abID && x != this))
            {
                entity.skills.RemoveSkill(cab.abID, Ability.AbilityOrigin.Item);
            }
        }

        RunCommands(inMainHand ? "OnUnWield" : "OnUnequip", entity);
    }

    public void OnHit(Entity myEntity, Entity attackedEntity)
    {
        RunCommands("OnHit", attackedEntity, myEntity);

        if (attackedEntity == null)
        {
            return;
        }

        if (myEntity.isPlayer && HasProp(ItemProperty.OnAttack_Radiation) && RNG.OneIn(20))
        {
            myEntity.stats.Radiate(1);
        }

        //Weapon coatings
        if (HasCComponent<CCoat>())
        {
            CCoat cc = GetCComponent<CCoat>();
            cc.OnStrike(attackedEntity.stats);

            if (cc.strikes <= 0)
            {
                components.Remove(cc);
            }
        }

        //Status effects
        foreach (CComponent c in CComponentsOfType<COnHitAddStatus>())
        {
            if (c is COnHitAddStatus cOnHit)
            {
                cOnHit.TryAddToEntity(attackedEntity);
            }
        }

        //Item level
        if (HasCComponent<CItemLevel>())
        {
            GetCComponent<CItemLevel>().AddXP(SeedManager.combatRandom.NextDouble() * 8.0);
        }

        if (HasProp(ItemProperty.Knockback) && RNG.Next(100) <= 5)
        {
            if (attackedEntity != null && attackedEntity.myPos.DistanceTo(myEntity.myPos) < 2f)
            {
                attackedEntity.ForceMove(attackedEntity.posX - myEntity.posX, attackedEntity.posY - myEntity.posY, myEntity.stats.Strength - 1);
            }
        }
    }

    public void OnBlock(Entity myEntity, Entity targetEntity)
    {
        RunCommands("OnBlock", targetEntity, myEntity);
    }

    public void OnThrow(Entity myEntity, Coord destination)
    {
        RunCommands("OnThrow", myEntity, destination);
    }

    public void OnConsume(Stats stats)
    {
        if (HasProp(ItemProperty.Replacement_Limb)) 
        {
            for (int i = 0; i < 10; i++)
            {
                stats.entity.body.TrainLimbOfType(properties.ToArray());
            }
        }
        else
        {
            ChangeStats(stats, false);
        }

        ApplyEffects(stats);
        RunCommands("OnConsume", stats.entity);

        if (HasCComponent<CLiquidContainer>())
        {
            GetCComponent<CLiquidContainer>().Drink(stats.entity);
        }

        if (HasCComponent<CCoat>())
        {
            GetCComponent<CCoat>().OnStrike(stats);
        }

        if (World.difficulty.AddictionsActive && ContainsProperty(ItemProperty.Addictive))
        {
            stats.ConsumedAddictiveSubstance(ID, false);
        }
    }
    #endregion

    public void RunCommands(string action, params object[] obj)
    {
        //Loop in case we have multiple of the same component type.
        foreach (CComponent comp in components)
        {
            switch (comp.ID)
            {
                case "Console":
                    if (comp is CConsole cc)
                    {
                        cc.RunCommand(action);
                    }
                    break;

                case "LuaEvent":
                    if (comp is CLuaEvent cl)
                    {
                        cl.CallEvent(action, obj);
                    }
                    break;
            }
        }
    }

    public int GetAttackAPCost()
    {
        if (HasProp(ItemProperty.Very_Quick)) return 5;
        if (HasProp(ItemProperty.Quick)) return 8;
        if (HasProp(ItemProperty.Slow)) return 12;
        if (HasProp(ItemProperty.Very_Slow)) return 15;

        return 10;
    }

    public bool AttackCrits(int chance)
    {
        return (RNG.Next(100) <= chance);
    }

    public int CalculateDamage(int strength, int proficiency)
    {
        Damage nDmg = new Damage(damage + modifier.damage);

        if (HasCComponent<CItemLevel>())
        {
            nDmg.Sides += GetCComponent<CItemLevel>().DamageBonus();
        }

        return nDmg.Roll() + (strength / 2 - 1) + proficiency + 1;
    }

    public int ThrownDamage(int proficiency, int dex)
    {
        Damage nDmg = (HasProp(ItemProperty.Throwing_Wep)) ? damage : new Damage(1, 3, 0, DamageTypes.Blunt);
        int totalDmg = nDmg.Roll() + (dex / 2 - 1) + proficiency;

        if (HasProp(ItemProperty.Throwing_Wep))
        {
            totalDmg += proficiency;
        }

        return totalDmg;
    }

    void ChangeStats(Stats stats, bool reverseEffect = false)
    {
        int multiplier = (reverseEffect) ? -1 : 1;

        foreach (Stat_Modifier mod in statMods)
        {
            int am = mod.Amount * multiplier;

            if (stats.Attributes.ContainsKey(mod.Stat))
            {
                stats.Attributes[mod.Stat] += am;
            }
            if (stats.proficiencies != null && stats.proficiencies.Profs.ContainsKey(mod.Stat))
            {
                stats.proficiencies.Profs[mod.Stat].level += am;
            }

            switch (mod.Stat)
            {
                case "Haste":
                    if (!reverseEffect)
                    {
                        stats.AddStatusEffect("Haste", mod.Amount);
                    }
                    break;
                case "Health":
                    stats.health += am;
                    break;
                case "Max Health":
                    stats.ChangeAttribute("Health", am);
                    break;
                case "Stamina":
                    stats.stamina += am;
                    break;
                case "Max Stamina":
                    stats.ChangeAttribute("Stamina", am);
                    break;
                case "Storage Capacity":
                    if (stats.entity.inventory != null)
                    {
                        stats.entity.inventory.AddRemoveStorage(am);
                    }
                    else
                    {
                        stats.GetComponent<Inventory>().AddRemoveStorage(am);
                    }
                    break;
                case "Light":
                    if (stats.entity.isPlayer && World.tileMap != null)
                    {
                        World.tileMap.LightCheck();
                    }
                    break;
                case "Accuracy":
                    if (stats.Attributes.ContainsKey("Accuracy"))
                    {
                        stats.Attributes["Accuracy"] += am;
                    }
                    break;
            }
        }
    }

    public void ApplyEffects(Stats stats)
    {
        if (HasProp(ItemProperty.Selected_Tele) && HasCComponent<CCoordinate>())
        {
            GetCComponent<CCoordinate>().Activate(stats.entity);
        }

        if (HasProp(ItemProperty.ReplaceLimb))
        {
            World.userInterface.Dialogue_ReplaceLimb(false);
            return;
        }

        if (HasProp(ItemProperty.Poison) || ContainsDamageType(DamageTypes.Venom))
        {
            CombatLog.SimpleMessage("Drink_Poison");
            stats.AddStatusEffect("Poison", RNG.Next(6, 10));
        }

        if (HasProp(ItemProperty.Radiate))
        {
            stats.Radiate(RNG.Next(10, 40));
        }

        if (HasProp(ItemProperty.Cure_Radiation))
        {
            stats.radiation = 0;

            if (stats.hasTrait("RadPoison"))
            {
                stats.RemoveTrait("RadPoison");
            }

            CombatLog.SimpleMessage("Reduce_Rad");
        }

        if (HasProp(ItemProperty.Cannibalism) && !stats.hasTrait("cannibal"))
        {
            //Check if there are villagers in sight.
            foreach (Entity e in World.objectManager.onScreenNPCObjects)
            {
                if (e.AI.npcBase.HasFlag(NPC_Flags.Human) && e.AI.InSightOfPlayer() && !e.AI.isHostile)
                {
                    stats.InitializeNewTrait(TraitList.GetTraitByID("cannibal"));
                    Alert.NewAlert("Became_Cannibal", UIWindow.Inventory);
                    break;
                }
            }
        }

        if (HasProp(ItemProperty.OnAttach_Leprosy) && !stats.hasTraitEffect(TraitEffects.Leprosy))
        {
            Alert.NewAlert("Dis_Lep_Eat");
            stats.InitializeNewTrait(TraitList.GetTraitByID("leprosy"));
            CombatLog.SimpleMessage("Eat_Lep");
        }
    }

    #region Add things
    public void AddProperty(ItemProperty property)
    {
        if (!properties.Contains(property))
        {
            properties.Add(property);
        }
    }
    public void AddDamageType(DamageTypes dt)
    {
        if (!damageTypes.Contains(dt))
        {
            damageTypes.Add(dt);
        }
    }

    public void AddModifier(ItemModifier mod)
    {
        if (modifier == null || mod == null || mod.name == "" || mod.ID == "")
        {
            return;
        }

        modifier = mod;

        if (!damageTypes.Contains(mod.damageType))
        {
            AddDamageType(mod.damageType);
        }

        armor += mod.armor;

        for (int i = 0; i < mod.properties.Count; i++)
        {
            if (!properties.Contains(mod.properties[i]))
            {
                AddProperty(mod.properties[i]);
            }
        }

        for (int i = 0; i < mod.statMods.Count; i++)
        {
            bool incremented = false;

            for (int j = 0; j < statMods.Count; j++)
            {
                if (mod.statMods[i].Stat == statMods[j].Stat)
                {
                    statMods[j].Amount += mod.statMods[i].Amount;
                    incremented = true;
                    break;
                }
            }

            if (!incremented)
            {
                statMods.Add(new Stat_Modifier(mod.statMods[i]));
            }
        }

        for (int i = 0; i < mod.components.Count; i++)
        {
            components.Add(mod.components[i]);
        }
    }
    #endregion

    public void Reset()
    {
        CopyFrom(ItemList.GetItemByID(ID));
    }

    //Check for things
    public bool ContainsProperty(ItemProperty property)
    {
        return HasProp(property);
    }
    public bool HasProp(ItemProperty property)
    {
        return (properties.Contains(property));
    }
    public bool ContainsDamageType(DamageTypes search)
    {
        return (damageTypes.Contains(search));
    }

    public int buyCost(int bonus)
    {
        int totCost = (cost + modifier.cost > 0) ? (cost + modifier.cost) : cost;

        if (HasProp(ItemProperty.Randart))
        {
            totCost *= 5;
        }

        if (HasCComponent<CLiquidContainer>())
        {
            CLiquidContainer cl = GetCComponent<CLiquidContainer>();

            if (!cl.isEmpty())
            {
                Liquid liquid = ItemList.GetLiquidByID(cl.sLiquid.ID, cl.sLiquid.units);
                totCost += liquid.pricePerUnit * liquid.units;
            }
        }

        return (int)((100f - (bonus * 3f)) / 100f * totCost * 2f);
    }
    public int sellCost(int bonus)
    {
        int totCost = (cost + modifier.cost > 0) ? (cost + modifier.cost) : cost;

        if (HasProp(ItemProperty.Randart))
        {
            totCost *= 5;
        }

        if (HasCComponent<CLiquidContainer>())
        {
            CLiquidContainer cl = GetCComponent<CLiquidContainer>();

            if (!cl.isEmpty())
            {
                Liquid liquid = ItemList.GetLiquidByID(cl.sLiquid.ID, cl.sLiquid.units);
                totCost += liquid.pricePerUnit * liquid.units;
            }
        }

        return UnityEngine.Mathf.FloorToInt((100.0f + (bonus * 3.0f)) / 100.0f * totCost * 0.5f);
    }

    public int Charges()
    {
        if (HasProp(ItemProperty.Ranged)) return (!HasCComponent<CFirearm>()) ? 1 : GetCComponent<CFirearm>().curr;
        else if (HasCComponent<CRot>()) return GetCComponent<CRot>().current;
        else if (HasCComponent<CCharges>()) return GetCComponent<CCharges>().current;
        else return 0;
    }

    public void Preserve()
    {
        if (HasCComponent<CRot>())
        {
            GetCComponent<CRot>().OnRemove();
            RemoveCComponent<CRot>();
            AddProperty(ItemProperty.Preserved);
        }
    }

    public void Unload()
    {
        CFirearm cf = GetCComponent<CFirearm>();

        if (cf != null)
        {
            cf.Unload();
        }
    }

    public bool UseCharge()
    {
        CCharges cc = GetCComponent<CCharges>();

        if (cc == null)
        {
            CRot cr = GetCComponent<CRot>();

            if (cr == null || cr.current <= 0)
            {
                return false;
            }

            cr.current--;

            if (cr.current == 200)
            {
                CombatLog.NewMessage("<color=grey>Your " + displayName + " is about to spoil.</color>");
            }

            return true;
        }

        if (cc.current <= 0)
        {
            return false;
        }

        cc.current--;
        return true;
    }

    public bool MagFull()
    {
        CFirearm cf = GetCComponent<CFirearm>();

        if (cf == null)
        {
            return true;
        }

        return (cf.curr == cf.max);
    }

    public bool Fire()
    {
        if (HasProp(ItemProperty.Cannot_Remove))
        {
            return true;
        }

        CFirearm fi = GetCComponent<CFirearm>();

        if (fi == null || fi.curr <= 0)
        {
            return false;
        }

        fi.Shoot(1);
        return true;
    }

    public int Reload(int amount, Item ammoID)
    {
        CFirearm fi = GetCComponent<CFirearm>();

        if (fi == null)
        {
            return 0;
        }

        return fi.Reload(amount, ammoID);
    }

    public ItemProperty GetSlot()
    {
        if (properties.Contains(ItemProperty.Slot_Head)) return ItemProperty.Slot_Head;
        else if (properties.Contains(ItemProperty.Slot_Back)) return ItemProperty.Slot_Back;
        else if (properties.Contains(ItemProperty.Slot_Chest)) return ItemProperty.Slot_Chest;
        else if (properties.Contains(ItemProperty.Slot_Arm)) return ItemProperty.Slot_Arm;
        else if (properties.Contains(ItemProperty.Slot_Leg)) return ItemProperty.Slot_Leg;
        else if (properties.Contains(ItemProperty.Slot_Wing)) return ItemProperty.Slot_Wing;
        else if (properties.Contains(ItemProperty.Slot_Tail)) return ItemProperty.Slot_Tail;

        return ItemProperty.None;
    }

    #region "Display functions"

    public string DisplayName()
    {
        string baseName = (string.IsNullOrEmpty(displayName) ? Name : displayName);

        if (modifier != null && !string.IsNullOrEmpty(modifier.name))
            baseName = modifier.name + " " + baseName;
        else if (HasCComponent<CLiquidContainer>())
        {
            CLiquidContainer cl = GetCComponent<CLiquidContainer>();
            string liqName = LocalizationManager.GetContent("IT_LiquidUnits_Empty");

            if (!cl.isEmpty())
            {
                liqName = ItemList.GetLiquidByID(cl.sLiquid.ID).Name;
            }

            baseName += " (" + liqName + ")";
        }

        if (HasProp(ItemProperty.Preserved))
        {
            baseName += " <color=silver>(Preserved)</color>";
        }

        return baseName;
    }

    public string InvDisplay(string natWep, bool forceArmor = false, bool forceWeapon = false, bool ranged = false)
    {
        if (modifier == null)
        {
            modifier = new ItemModifier();
        }

        if (ID == natWep)
        {
            return "<color=grey>" + Name + "</color>";
        }

        if (ID == "stump")
        {
            string disp = "<color=red>" + Name + "</color>";

            if (forceWeapon)
            {
                disp += " " + DisplayDamage();
            }
            else if (forceArmor)
            {
                disp += " " + DisplayArmor();
            }

            return disp;
        }

        string baseName = DisplayName();

        if (amount > 1)
        {
            if (ranged && HasProp(ItemProperty.Throwing_Wep))
            {
                return (baseName + " x" + amount);
            }

            return (HasProp(ItemProperty.Weapon)) ? baseName + " x" + amount + DisplayDamage() : baseName + " x" + amount;
        }
        else if (amount == 0)
        {
            return (HasProp(ItemProperty.Weapon)) ? baseName + " x" + amount + DisplayDamage() : baseName + " x" + amount;
        }

        if (forceWeapon)
        {
            return baseName + " " + DisplayDamage();
        }

        if (forceArmor)
        {
            return baseName + " " + DisplayArmor();
        }

        if (HasProp(ItemProperty.Weapon))
        {
            return baseName + " " + ((armor == 0) ? DisplayDamage() : DisplayArmor());
        }

        if (HasProp(ItemProperty.Ranged))
        {
            int sh = GetCComponent<CFirearm>().shots;
            return baseName + " " + DisplayDamage() + "<color=olive>(x" + sh + ")</color>";
        }

        if (HasProp(ItemProperty.Armor))
        {
            return baseName + " " + DisplayArmor();
        }

        return baseName;
    }


    public string DisplayDamage()
    {
        if (damage == null)
        {
            return "";
        }

        return "<color=silver> (" + TotalDamage().ToString() + ")</color>";
    }

    public string DisplayArmor()
    {
        string s = "<color=silver>[";

        return s + armor.ToString() + "]</color>";
    }

    //used only to check differences in names between items. items with modifiers cannot stack.
    public string ItemName()
    {
        return modifier.name + " " + Name;
    }
    #endregion

    public SItem ToSerializedItem()
    {
        if (modifier == null)
        {
            modifier = new ItemModifier();
        }

        Item baseItem = ItemList.GetItemByID(ID);
        List<ItemProperty> props = new List<ItemProperty>();

        foreach (ItemProperty p in properties)
        {
            if (!baseItem.HasProp(p))
            {
                props.Add(p);
            }
        }

        if (props.Count == 0)
        {
            props = null;
        }

        return new SItem(ID, modifier.ID, amount, displayName, props, damage, armor, components, statMods);
    }

    public bool HasTag(string tag)
    {
        CTags cTags = GetCComponent<CTags>();

        if (cTags == null)
        {
            return false;
        }

        return cTags.HasTag(tag);
    }

    public void AddComponent(CComponent comp)
    {
        components.Add(comp);
    }

    public void CopyFrom(Item other)
    {
        if (other == null)
        {
            return;
        }

        ID = other.ID;
        ModID = other.ModID;
        Name = other.Name;
        itemType = other.itemType;
        damage = other.damage;
        armor = other.armor;
        rarity = other.rarity;
        lootable = other.lootable;
        cost = other.cost;
        flavorText = other.flavorText;
        stackable = other.stackable;
        amount = other.amount;
        accuracy = other.accuracy;
        modifier = other.modifier;
        tileID = other.tileID;
        attackType = other.attackType;
        renderer = other.renderer;
        displayName = other.displayName;

        CopyLists(other);
    }

    void CopyLists(Item other)
    {
        components = new List<CComponent>();

        foreach (CComponent c in other.components)
        {
            components.Add(c.Clone());
        }

        statMods = new List<Stat_Modifier>();

        foreach (Stat_Modifier sm in other.statMods)
        {
            statMods.Add(new Stat_Modifier(sm));
        }

        properties.Clear();

        foreach (ItemProperty pr in other.properties)
        {
            AddProperty(pr);
        }

        damageTypes.Clear();

        foreach (DamageTypes d in other.damageTypes)
        {
            AddDamageType(d);
        }

        if (damageTypes.Count <= 0)
        {
            AddDamageType(DamageTypes.Blunt);
        }
    }

    public bool CanReplaceBP()
    {
        if (HasProp(ItemProperty.Armor))
        {
            return false;
        }

        return (HasProp(ItemProperty.Severed_BodyPart) || HasProp(ItemProperty.Slot_Arm) || HasProp(ItemProperty.Slot_Back)
            || HasProp(ItemProperty.Slot_Chest) || HasProp(ItemProperty.Slot_Head) || HasProp(ItemProperty.Slot_Leg)
            || HasProp(ItemProperty.Slot_Tail) || HasProp(ItemProperty.Slot_Wing));
    }

    bool OnEquipReject()
    {
        return !HasProp(ItemProperty.Weapon) && !HasProp(ItemProperty.Armor) && !HasProp(ItemProperty.Ranged);
    }

    public enum AttackType
    {
        Bash, Slash, Sweep, Spear, Claw, Psy, Knife, Bite
    }

    public void FromJson(JsonData dat)
    {
        if (dat.ContainsKey("Base"))
        {
            string baseID = dat["Base"].ToString();

            Item baseItem = GameData.Get<Item>(baseID);

            if (!baseItem.IsNullOrDefault())
            {
                CopyFrom(baseItem);
            }
        }

        if (dat.ContainsKey("ID"))
        {
            ID = dat["ID"].ToString();
        }

        if (dat.ContainsKey("Name"))
        {
            Name = dat["Name"].ToString();
        }
        else
        {
            Name = ID;
        }

        dat.TryGetInt("TileID", out tileID, tileID);
        dat.TryGetEnum("Type", out itemType, itemType);
        dat.TryGetInt("Rarity", out rarity, rarity);
        dat.TryGetBool("Lootable", out lootable, lootable);
        dat.TryGetBool("Stackable", out stackable, stackable);
        dat.TryGetInt("Armor", out armor, armor);
        dat.TryGetInt("Accuracy", out accuracy, accuracy);
        dat.TryGetString("FlavorText", out flavorText);
        dat.TryGetDamage("Damage", out damage, damage);

        if (rarity < 100 && rarity > ItemUtility.MaxRarity)
        {
            ItemUtility.MaxRarity = rarity;
        }

        if (dat.ContainsKey("Cost"))
        {
            SetBaseCost((int)dat["Cost"]);
        }

        if (dat.ContainsKey("Components"))
        {
            SetComponentList(ItemUtility.GetComponentsFromData(dat["Components"]));
        }

        //Properties
        if (dat.ContainsKey("Properties"))
        {
            for (int p = 0; p < dat["Properties"].Count; p++)
            {
                string prop = dat["Properties"][p].ToString();
                ItemProperty pr = prop.ToEnum<ItemProperty>();
                properties.Add(pr);
            }
        }

        //Damage Types
        if (dat.ContainsKey("Damage Types"))
        {
            if (dat["Damage Types"].Count > 0)
            {
                damageTypes.Clear();
            }

            for (int d = 0; d < dat["Damage Types"].Count; d++)
            {
                string dmg = dat["Damage Types"][d].ToString();
                DamageTypes dt = dmg.ToEnum<DamageTypes>();
                damageTypes.Add(dt);
            }
        }

        if (dat.ContainsKey("Attack Type"))
        {
            string atype = dat["Attack Type"].ToString();
            attackType = atype.ToEnum<AttackType>();
        }
        else
        {
            attackType = ContainsDamageType(DamageTypes.Claw) ? AttackType.Claw : AttackType.Bash;
        }


        //Stat Modifiers
        if (dat.ContainsKey("Stat Mods"))
        {
            for (int s = 0; s < dat["Stat Mods"].Count; s++)
            {
                string statName = dat["Stat Mods"][s]["Stat"].ToString();
                int amount = (int)dat["Stat Mods"][s]["Amount"];

                statMods.Add(new Stat_Modifier(statName, amount));
            }
        }

        if (dat.ContainsKey("Display"))
        {
            string ground = (dat["Display"].ContainsKey("On Ground")) ? dat["Display"]["On Ground"].ToString() : "";
            string player = (dat["Display"].ContainsKey("On Player")) ? dat["Display"]["On Player"].ToString() : "";
            string slot = (dat["Display"].ContainsKey("Layer")) ? dat["Display"]["Layer"].ToString() : "";
            renderer = new ItemRenderer(ItemUtility.GetRenderLayer(slot), ground, player);
        }

        if (dat.ContainsKey("Tags"))
        {
            CTags tags = new CTags();
            for (int i = 0; i < dat["Tags"].Count; i++)
            {
                tags.AddTag(dat["Tags"][i].ToString());
            }

            AddComponent(tags);
        }
    }

    public IEnumerable<string> LoadErrors()
    {
        yield break;
    }


    public struct ItemRenderer
    {
        public string onGround;
        public string onPlayer;
        public int slot;

        public ItemRenderer(int _slot, string _ground, string _player)
        {
            onGround = _ground;
            onPlayer = _player;
            slot = _slot;
        }

        public override string ToString()
        {
            return "Renderer: (" + "OnGround: " + onGround + ", OnPlayer: " + onPlayer + ", Slot: " + slot + ")";  
        }
    }
}

[System.Serializable]
public enum ItemProperty
{
    None, Artifact, Ammunition,
    Stop_Bleeding, Stun, Poison, Cure_Radiation, OnAttack_Radiation,
    OnAttach_Leprosy, OnAttach_Crystallization,
    Armor, Slot_Head, Slot_Back, Slot_Chest, Slot_Tail, Slot_Wing, Slot_Arm, Slot_Leg, Slot_Misc,
    Cannot_Remove, Degrade, DestroyOnZeroCharges, ReplaceLimb,
    Reveal_Map, Blink, Surface_Tele, Selected_Tele, Knockback,
    Weapon, Two_Handed, Ranged, Dig, Quick, Very_Quick, Slow, Very_Slow, Throwing_Wep, Disarm, Shock_Nearby, Burst, Quick_Reload,
    Legible, Explosive, Edible, Severed_BodyPart, Radiate, Cannibalism, Corpse,
    Tome, Replacement_Limb, Flying,
    Quest_Item, Randart, Unique, Addictive, Bow, DrainHealth, Pool, NoMods, Preserved, Proc_Attack, OnAttach_Vampirism
}

[System.Serializable]
public enum Proficiencies
{
    Unarmed, Misc_Object, Blade, Axe, Blunt, Polearm, Firearm, Throw, Armor, Butchery, Shield, MartialArts
}

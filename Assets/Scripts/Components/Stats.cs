using UnityEngine;
using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Stats : MonoBehaviour
{
    [Header("Main Stats")]
    public XPLevel MyLevel;
    public int maxHealth, maxStamina, radiation;

    [Space(5)]
    [Header("Prefabs")]
    public GameObject characterSpriteObject;
    public GameObject bleedEffectObject;
    public GameObject poisonEffectObject;
    public GameObject stunEffectObject;
    public GameObject confuseEffectObject;
    public GameObject freezeEffect;

    [HideInInspector] public Entity entity;
    [HideInInspector] public List<Trait> traits;
    [HideInInspector] public List<Addiction> addictions;
    public PlayerProficiencies proficiencies;
    public Dictionary<string, int> statusEffects;
    public Dictionary<string, int> Attributes;

    public event Action<Entity> onDeath;
    public event Action hpChanged;
    public event Action stChanged;
    [HideInInspector] public Entity lastHit = null;


    [HideInInspector] public bool dead = false;
    [HideInInspector] public bool invincible = false;

    int _health, _stamina;
    int healTimer, restoreTimer;

    System.Random RNG
    {
        get { return SeedManager.combatRandom; }
    }

    public List<Trait> Mutations
    {
        get
        {
            if (traits.FindAll(x => x.ContainsEffect(TraitEffects.Mutation)).Count <= 0)
                return new List<Trait>();

            return (traits.FindAll(x => x.ContainsEffect(TraitEffects.Mutation)));
        }
    }

    public int Strength
    {
        get
        {
            int str = Mathf.Max(Attributes["Strength"], 1);
            return Mathf.Min(str, 50);
        }
    }
    public int Dexterity
    {
        get
        {
            int dex = Mathf.Max(Attributes["Dexterity"], 1);
            return Mathf.Min(dex, 50);
        }
    }
    public int Intelligence
    {
        get
        {
            int intel = Mathf.Max(Attributes["Intelligence"], 1);
            return Mathf.Min(intel, 50);
        }
    }
    public int Endurance
    {
        get
        {
            int end = Mathf.Max(Attributes["Endurance"], 1);
            return Mathf.Min(end, 50);
        }
    }

    public int Speed
    {
        get
        {
            int spd = (HasEffect("Haste")) ? Attributes["Speed"] * 2 : Attributes["Speed"];
            return Mathf.Min(spd, 50);
        }
    }
    public int Accuracy
    {
        get { return Attributes["Accuracy"]; }
    }
    public int AttackDelay
    {
        get { return Attributes["Attack Delay"] - (Dexterity / 2) + 3; }
    }
    public int Defense
    {
        get { return Attributes["Defense"]; }
    }

    public int HeatResist
    {
        get { return Mathf.Min(Attributes["Heat Resist"], 90); }
    }
    public int ColdResist
    {
        get { return Mathf.Min(Attributes["Cold Resist"], 90); }
    }
    public int EnergyResist
    {
        get { return Mathf.Min(Attributes["Energy Resist"], 90); }
    }
    public int HPRegen
    {
        get { return Attributes["HP Regen"]; }
    }
    public int STRegen
    {
        get { return Attributes["ST Regen"]; }
    }

    Inventory MyInventory
    {
        get { return entity.inventory; }
    }
    Body MyBody
    {
        get { return entity.body; }
    }

    public void Init()
    {
        proficiencies = new PlayerProficiencies();
        traits = new List<Trait>();

        InitializeFromData();

        World.turnManager.incrementTurnCounter += UpdateStatusEffects;
    }

    void OnEnable()
    {
        Attributes = new Dictionary<string, int>() {
            { "Strength", 1 }, { "Dexterity", 1 }, { "Intelligence", 1 }, { "Endurance", 1 }
        };
        statusEffects = new Dictionary<string, int>();
    }

    void OnDisable()
    {
        World.turnManager.incrementTurnCounter -= UpdateStatusEffects;
    }

    public void ChangeAttribute(string attribute, int amount)
    {
        Attributes[attribute] += amount;
    }

    public void ConsumedAddictiveSubstance(string id, bool liquid)
    {
        if (addictions.Find(x => x.addictedID == id) != null)
            addictions.Find(x => x.addictedID == id).ItemUse(this);
        else
            addictions.Add(new Addiction(id, (liquid ? ItemList.GetLiquidByID(id).addictiveness : 5)));
    }

    public bool SkipTurn()
    {
        return HasEffect("Frozen") || HasEffect("Stun") || HasEffect("Unconscious");
    }

    public void AddStatusEffect(string effectName, int turns)
    {
        if (effectName == "Stun" && hasTraitEffect(TraitEffects.Stun_Resist) || effectName == "Confuse" && hasTraitEffect(TraitEffects.Confusion_Resist))
        {
            if (SeedManager.combatRandom.Next(100) > 20)
                turns /= 2;
            else if (SeedManager.combatRandom.Next(100) < 2)
                turns = 0;
        }

        if (turns > 0)
        {
            if (statusEffects.ContainsKey(effectName))
            {
                if (effectName != "Unconscious")
                {
                    statusEffects[effectName] += turns - 1;
                }
            }
            else
            {
                statusEffects.Add(effectName, turns - 1);

                if (LocalizationManager.GetContent("Message_SE_" + effectName) != LocalizationManager.defaultText)
                {
                    CombatLog.NameMessage("Message_SE_" + effectName, entity.MyName);
                }
            }

            if (effectName == "Slow" && statusEffects["Slow"] > Speed - 3)
            {
                statusEffects["Slow"] = 0;
                AddStatusEffect("Frozen", SeedManager.combatRandom.Next(2, 6));
            }
        }

        CheckEffectsObjects();
        UpdateRotation();
    }

    public void RemoveStatusEffect(string effectName)
    {
        if (statusEffects.ContainsKey(effectName))
            statusEffects.Remove(effectName);
    }

    public bool HasEffect(string effect)
    {
        return statusEffects.ContainsKey(effect);
    }

    void CheckEffectsObjects()
    {
        bleedEffectObject.SetActive(HasEffect("Bleed"));
        poisonEffectObject.SetActive(HasEffect("Poison"));
        stunEffectObject.SetActive(HasEffect("Stun"));
        confuseEffectObject.SetActive(HasEffect("Confuse"));
        freezeEffect.SetActive(HasEffect("Frozen"));

        if (entity.isPlayer)
            World.userInterface.UpdateStatusEffects(this);
    }

    public void GainExperience(int amount)
    {
        if (MyLevel != null && !hasTraitEffect(TraitEffects.Vampirism))
        {
            MyLevel.AddXP(amount);
        }
    }

    public void LevelUp(int currentLevel)
    {
        if (entity.isPlayer)
            CombatLog.NameMessage("Message_LevelUp", currentLevel.ToString());

        if (currentLevel % 2 == 0)
        {
            maxHealth += Mathf.Max((Endurance / 3) + 1, 1);
            maxStamina += Mathf.Max((Endurance / 3) - 2, 1);
        }

        health = maxHealth;
        stamina += maxStamina;

        if (entity.isPlayer && currentLevel % 3 == 0)
            World.userInterface.LevelUp();
    }

    public int MissChance(Item wep)
    {
        int hitChance = 45;
        int profLevel = (entity.isPlayer) ? CheckProficiencies(wep).level : entity.AI.npcBase.weaponSkill;

        hitChance += (profLevel * 3) + (Accuracy * 5) + (wep.Accuracy * 5);
        return (100 - hitChance);
    }

    public void Miss(Entity attacker, Item weapon)
    {
        if (entity == null || attacker == null || dead)
            return;

        if (entity.isPlayer || attacker.isPlayer || GetComponentInChildren<SpriteRenderer>().enabled)
            CombatLog.Combat_Full(entity.isPlayer, 0, false, gameObject.name, true, attacker.name, "", weapon.DisplayName());
    }

    bool BlockedWithShield(Entity attacker)
    {
        List<BodyPart.Hand> hands = MyBody.Hands;

        for (int i = 0; i < hands.Count; i++)
        {
            int ranNum = RNG.Next(100);
            float chance = proficiencies.Shield.level + 1;

            if (hands[i].EquippedItem.HasCComponent<CBlock>())
            {
                chance += (hands[i].EquippedItem.GetCComponent<CBlock>().level * 5);

                if (ranNum <= chance)
                {
                    AddProficiencyXP(proficiencies.Shield, Intelligence / 2 + 1);
                    hands[i].EquippedItem.OnBlock(entity, attacker);
                    return true;
                }
            }
        }

        return false;
    }

    public bool TakeDamage(Item weapon, int amount, Entity attacker)
    {
        return TakeDamage(weapon, amount, weapon.damageTypes, attacker);
    }

    public bool TakeDamage(Item weapon, int amount, HashSet<DamageTypes> damTypes, Entity attacker, bool crit = false, BodyPart targetPart = null, int sevChance = 0)
    {
        if (invincible || dead)
            return false;

        if (!CanHit(attacker))
        {
            Miss(attacker, weapon);
            return false;
        }

        if (attacker != null)
            lastHit = attacker;
        if (targetPart == null)
            targetPart = Utility.WeightedChoice(MyBody.TargetableBodyParts());
        if (targetPart.slot == ItemProperty.Slot_Head && RNG.CoinFlip())
            crit = true;

        //Blocking with shields
        if (BlockedWithShield(attacker))
            amount = 0;

        //Calculate damage
        int damage = CalculateDamage(amount, damTypes, crit, targetPart);

        if (damage > 0)
        {
            if (hasTraitEffect(TraitEffects.Zap_On_Hit) && RNG.Next(100) < 5)
                attacker.stats.IndirectAttack(RNG.Next(1, 6), DamageTypes.Energy, entity, LocalizationManager.GetContent("Shock"), true);

            HandleSeverence(damTypes, targetPart, sevChance);
            weapon.OnHit(attacker, entity);

            if (SeedManager.combatRandom.Next(100) < 20)
                MyBody.TrainLimbOfType(new ItemProperty[] { ItemProperty.Slot_Back, ItemProperty.Slot_Chest });

        }
        else if (!damTypes.Contains(DamageTypes.Cold) && damTypes.Contains(DamageTypes.Heat) && !damTypes.Contains(DamageTypes.Energy))
            World.soundManager.BlockSound();

        if (entity.isPlayer || entity.AI.InSightOfPlayer())
        {
            bool displayOrange = entity.isPlayer;

            if (damage <= 0)
                displayOrange = !displayOrange;

            CombatLog.Combat_Full(entity.isPlayer, damage, crit, gameObject.name, false, attacker.name, targetPart.displayName, weapon.DisplayName());
        }

        PostDamage(attacker, damage, damTypes, targetPart);

        return (damage > 0);
    }

    bool CanHit(Entity attacker)
    {
        //Toppled vs Flying enemy. If grip exists, can still hit.
        if (MyInventory.CanFly() && attacker.stats.HasEffect("Topple"))
        {
            bool willMiss = true;

            if (MyBody.AllGripsAgainst().Count > 0)
            {
                foreach (BodyPart.Grip g in MyBody.AllGripsAgainst())
                {
                    if (g.myPart.myBody == attacker.body)
                    {
                        willMiss = false;
                        break;
                    }
                }
            }
            if (willMiss && MyBody.AllGrips().Count > 0)
            {
                foreach (BodyPart.Grip g in MyBody.AllGrips())
                {
                    if (g.HeldBody == attacker.body)
                    {
                        willMiss = false;
                        break;
                    }
                }
            }

            return !willMiss;
        }

        return true;
    }

    void HandleSeverence(HashSet<DamageTypes> damageType, BodyPart targetPart, int sevChance = 0)
    {
        bool sever = false;

        if (entity.isPlayer)
        {
            if (targetPart.effect == TraitEffects.Crystallization && damageType.Contains(DamageTypes.Blunt) && RNG.Next(10) == 0 || 
                damageType.Contains(DamageTypes.Cleave) || damageType.Contains(DamageTypes.Slash) && RNG.Next(10) == 0)
                sever = true;
        }
        else
        {
            if (entity.AI.npcBase.HasFlag(NPC_Flags.Solid_Limbs) && damageType.Contains(DamageTypes.Blunt) || 
                damageType.Contains(DamageTypes.Cleave) || damageType.Contains(DamageTypes.Slash) && RNG.Next(10) == 0)
                sever = true;
        }

        if (sever && RNG.Next(100) < (3 + sevChance))
        {
            if (targetPart.slot == ItemProperty.Slot_Head)
            {
                if (MyBody.bodyParts.FindAll(x => x.slot == ItemProperty.Slot_Head).Count > 1 || !entity.isPlayer)
                {
                    MyBody.RemoveLimb(targetPart);
                    CombatLog.NameItemMessage("Sever_Limb", gameObject.name, targetPart.displayName);
                }
            }
            else
            {
                MyBody.RemoveLimb(targetPart);
                CombatLog.NameItemMessage("Sever_Limb", gameObject.name, targetPart.displayName);
            }
        }
    }

    //has a source, just not from an entity or its weapon. from a string instead.
    public int IndirectAttack(int amount, HashSet<DamageTypes> dTypes, Entity attacker, string sourceName, bool ignoreArmor, bool crit = false, bool ignoreResists = false)
    {
        if (invincible || dead)
            return 0;

        if (attacker != null)
            lastHit = attacker;

        BodyPart targetPart = Utility.WeightedChoice(MyBody.TargetableBodyParts());
        int damage = CalculateDamage(amount, dTypes, crit, targetPart, ignoreArmor, ignoreResists);

        if (damage <= 0)
        {
            PostDamage(attacker, damage, dTypes, targetPart);
            return 0;
        }

        CombatLog.NewIndirectCombat("Damage_Indirect", damage, sourceName, entity.MyName, targetPart.displayName, entity.isPlayer);
        PostDamage(attacker, damage, dTypes, targetPart);
        return damage;
    }
    public int IndirectAttack(int amount, DamageTypes damageType, Entity attacker, string sourceName, bool ignoreArmor, bool crit = false, bool ignoreResists = false)
    {
        return IndirectAttack(amount, new HashSet<DamageTypes>() { damageType }, attacker, sourceName, ignoreArmor, crit, ignoreResists);
    }

    public void SimpleDamage(int amount)
    {
        if (invincible || dead)
            return;

        BodyPart targetPart = Utility.WeightedChoice(MyBody.TargetableBodyParts());
        HashSet<DamageTypes> dTypes = new HashSet<DamageTypes>() { DamageTypes.Blunt };
        int damage = CalculateDamage(amount, dTypes, false, targetPart, true, true);

        CombatLog.NewSimpleCombat("Damage_Simplified", damage, gameObject.name, entity.isPlayer);
        PostDamage(null, damage, dTypes, targetPart);
    }

    void PostDamage(Entity attacker, int damage, HashSet<DamageTypes> dt, BodyPart hitBodyPart)
    {
        if (damage > 0)
            entity.CancelWalk();

        if (health <= 0)
        {
            CombatLog.NameMessage("Message_Die", gameObject.name);
            return;
        }

        if (attacker != null && !entity.isPlayer)
        {
            if (attacker == ObjectManager.playerEntity)
            {
                if (!entity.AI.isFollower() && entity.AI.HasDetectedPlayer(5))
                {
                    entity.AI.BecomeHostile();
                }
            }

            entity.AI.SetTarget(attacker);
        }

        if (dt.Contains(DamageTypes.Blunt) || dt.Contains(DamageTypes.Slash) || dt.Contains(DamageTypes.Pierce))
            IncreaseArmorProfs(hitBodyPart);
    }

    //The Damage Calculation
    int CalculateDamage(int amount, HashSet<DamageTypes> damageTypes, bool crit, BodyPart targetPart, bool ignoreArmor = false, bool ignoreResists = false)
    {
        if (amount <= 0)
            return 0;

        int damage = 0;
        int targettedPartArmor = (targetPart == null) ? 0 : targetPart.equippedItem.armor + targetPart.armor;
        int def = targettedPartArmor + Defense;

        if (ignoreArmor || damageTypes.Contains(DamageTypes.Heat) || damageTypes.Contains(DamageTypes.Cold) || damageTypes.Contains(DamageTypes.Energy))
        {
            def = 0;
        }
        else if (HasEffect("Shield"))
        {
            def *= 2;
        }

        if (!entity.isPlayer && !entity.AI.HasSeenPlayer())
        {
            crit = true;
        }

        damage = amount - def + (crit ? (damage / 2) : 0);
        damage = Mathf.Clamp(damage, 0, 999);

        if (damage > 0 && !ignoreArmor)
        {
            if (RNG.Next(100) < MyInventory.ArmorProfLevelFromBP(targetPart))
            {
                return 0;
            }

            //resistances.
            if (!ignoreResists)
            {
                if (damageTypes.Contains(DamageTypes.Heat))
                {
                    ApplyResist(ref damage, HeatResist);

                    if (SeedManager.combatRandom.Next(100) < 3)
                    {
                        AddStatusEffect("Aflame", SeedManager.combatRandom.Next(2, 6));
                    }
                }
                else if (damageTypes.Contains(DamageTypes.Cold))
                {
                    ApplyResist(ref damage, ColdResist);

                    if (SeedManager.combatRandom.Next(100) < 3)
                    {
                        AddStatusEffect("Slow", SeedManager.combatRandom.Next(2, 5));
                    }
                }
                else if (damageTypes.Contains(DamageTypes.Energy))
                {
                    ApplyResist(ref damage, EnergyResist);

                    if (SeedManager.combatRandom.Next(100) < 3)
                    {
                        AddStatusEffect("Stun", SeedManager.combatRandom.Next(1, 3));
                    }
                }
            }
        }

        if (damage > 0)
        {
            if (entity.isPlayer && damage >= maxHealth / 5 && SeedManager.combatRandom.Next(1000) < 5)
            {
                targetPart.WoundMe(damageTypes);
            }

            ApplyDamage(damage, crit, Color.white);
        }

        return damage;
    }

    int ApplyResist(ref int damage, int resist)
    {
        float dm = damage;
        dm *= (resist * -0.01f);
        return damage + Mathf.RoundToInt(dm);
    }

    //The actual application of damage, from Damage()
    void ApplyDamage(int amount, bool crit, Color col, bool bloodstain = false, bool noNumber = false)
    {
        health -= amount;

        if (!noNumber)
        {
            Color color = (crit) ? Color.yellow : Color.red;

            if (col != Color.white)
                color = col;

            CreateDamageNumber(amount.ToString(), color);
        }

        if (RNG.Next(100) < 20 && bloodstain)
        {
            entity.CreateBloodstain();
        }

        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (onDeath != null)
        {
            onDeath(entity);
        }

        entity.fighter.Die();
    }

    string BloodType()
    {
        string bloodType = "liquid_blood";

        if (HasEffect("Poison"))
            bloodType = "liquid_blood_pois";

        if (entity.isPlayer)
        {
            if (hasTraitEffect(TraitEffects.Leprosy))
                bloodType = "liquid_blood_lep";
            if (hasTraitEffect(TraitEffects.Vampirism) || hasTraitEffect(TraitEffects.PreVamp))
                bloodType = "liquid_blood_vamp";
        }
        else
        {
            if (entity.AI.npcBase.HasFlag(NPC_Flags.Skills_Leprosy))
                bloodType = "liquid_blood_lep";
            if (entity.AI.npcBase.ID == "vampire")
                bloodType = "liquid_blood_vamp";
        }

        return bloodType;
    }

    public void StatusEffectDamage(int amount, DamageTypes damageType)
    {
        if (invincible || amount <= 0)
        {
            return;
        }

        Color c = Color.red;

        if (damageType == DamageTypes.Venom)
        {
            c = Color.green;
        }
        else if (damageType == DamageTypes.Bleed)
        {
            c = Color.magenta;
            
            if (entity.isPlayer || !entity.AI.npcBase.HasFlag(NPC_Flags.No_Blood))
            {
                string bloodType = BloodType();

                if (bloodType != "liquid_bloow")
                {
                    World.objectManager.CreatePoolOfLiquid(entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, bloodType, 1);
                }
                else
                {
                    entity.CreateBloodstain(false, 30);
                }
            }
        }

        if (entity.isPlayer)
        {
            entity.walkDirection = null;
            entity.resting = false;
            entity.path = null;

            if (damageType == DamageTypes.Venom)
                CombatLog.SimpleMessage("Damage_Poison");
            else if (damageType == DamageTypes.Bleed)
                CombatLog.SimpleMessage("Damage_Bleed");
            else if (damageType == DamageTypes.Hunger)
                CombatLog.SimpleMessage("Damage_Hunger");
            else if (damageType == DamageTypes.Heat)
                CombatLog.SimpleMessage("Damage_Heat");

        }

        ApplyDamage(amount, false, c, damageType == DamageTypes.Bleed);
    }

    public void RestoreStamina(int amount)
    {
        stamina += amount;
    }
    public void UseStamina(int amount)
    {
        stamina -= amount;
    }

    public void Heal(int amount)
    {
        health += amount;

        if (entity.isPlayer)
            CombatLog.NameMessage("Message_Heal", amount.ToString());
    }

    public void RestHP()
    {
        if (health < maxHealth)
        {
            int amount = maxHealth / 30;
            amount = Mathf.Clamp(amount, 1, 50);

            if (entity.isPlayer || entity.AI.isFollower())
                health += amount;
            else if (World.turnManager.turn % 3 == 0)
                health += amount;
        }
    }

    public void RestST()
    {
        if (stamina < maxStamina)
        {
            int amount = maxStamina / 30;
            amount = Mathf.Clamp(amount, 1, 50);
            stamina += amount;
        }
    }


    void UpdateRotation()
    {
        bool onGround = (HasEffect("Topple") || HasEffect("Unconscious"));
        float zRot = (onGround) ? 90f : 0f;
        float offset = (onGround) ? 0.5f : 0f;

        characterSpriteObject.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
        characterSpriteObject.transform.localPosition = new Vector3(offset + 0.5f, offset, 0f);
    }

    public void InitializeNewTrait(Trait t)
    {
        if (t == null || traits.Contains(t) && !t.stackable)
            return;

        t.turnAcquired = World.turnManager.turn;
        AddTrait(t);
        t.Initialize(this);
    }

    public void Radiate(int amount)
    {
        if (!entity.isPlayer)
            return;

        if (hasTraitEffect(TraitEffects.Rad_Resist) && amount > 1)
            amount /= 2;

        radiation += amount;

        if (radiation >= 100)
            Mutate();
    }

    public void GiveTrait(string traitID)
    {
        Trait t = TraitList.GetTraitByID(traitID);

        if (t != null)
        {
            InitializeNewTrait(t);
        }
    }

    public void ForceMutation()
    {
        Mutate();
    }

    public void Mutate(string mutID = "")
    {
        if (mutID == "" && TraitList.GetAvailableMutations(this).Count <= 0)
        {
            radiation = 0;
            return;
        }

        if (hasTraitEffect(TraitEffects.Rad_Resist) && RNG.Next(100) < 10)
        {
            radiation /= 2;
            CombatLog.SimpleMessage("Message_MutFail");
            return;
        }

        if (string.IsNullOrEmpty(mutID))
        {
            List<Trait> availableMuts = TraitList.GetAvailableMutations(this);

            if (availableMuts.Count <= 0)
            {
                radiation = 0;
                return;
            }
            else
            {
                mutID = availableMuts.GetRandom(RNG).ID;
            }
        }

        Trait mutation = TraitList.GetTraitByID(mutID);

        if (mutation == null || !mutation.stackable && hasTrait(mutation.ID) && string.IsNullOrEmpty(mutation.nextTier) 
            || mutation.stackable && TraitStacks(mutation.ID) >= mutation.maxStacks)
        {
            radiation = 0;
            return;
        }

        if (!mutation.stackable && hasTrait(mutation.ID) && !string.IsNullOrEmpty(mutation.nextTier))
        {
            //Evolve mutation
            Trait newMut = TraitList.GetTraitByID(mutation.nextTier);
            RemoveTrait(mutation.ID);

            mutation = new Trait(newMut);
        }

        CheckMutationIntegrity(mutation);
        InitializeNewTrait(mutation);
        radiation = RNG.Next(2, 7);

        CombatLog.NameMessage("Message_Mutate", mutation.name);
    }

    public int TraitStacks(string id)
    {
        int amt = 0;

        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].ID == id)
            {
                amt++;
            }
        }

        return amt;
    }

    void CheckMutationIntegrity(Trait newMut)
    {
        if (string.IsNullOrEmpty(newMut.slot))
            return;

        List<Trait> traitsToRemove = new List<Trait>();

        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].slot == newMut.slot)
                traitsToRemove.Add(traits[i]);
        }

        for (int i = 0; i < traitsToRemove.Count; i++)
        {
            RemoveTrait(traitsToRemove[i].ID);
        }
    }

    public void RemoveTrait(string id)
    {
        Trait t = traits.Find(x => x.ID == id);

        if (t != null)
        {
            EntitySkills skills = GetComponent<EntitySkills>();

            for (int i = 0; i < t.abilityIDs.Count; i++)
            {
                if (SkillList.GetSkillByID(t.abilityIDs[i]) != null && skills.abilities.Find(x => x.ID == t.abilityIDs[i]) != null)
                {
                    skills.RemoveSkill(t.abilityIDs[i], Skill.AbilityOrigin.Trait);
                }
            }

            t.Initialize(this, true);
        }
    }

    public void CureAllMutations()
    {
        while (Mutations.Count > 0)
        {
            RemoveTrait(Mutations[0].ID);
        }
    }

    public void CureRandomMutations(int amount)
    {
        if (Mutations.Count <= 0)
            CombatLog.SimpleMessage("No_Effect");

        for (int i = 0; i < amount; i++)
        {
            if (Mutations.Count > 0)
            {
                RemoveTrait(Mutations.GetRandom().ID);
            }
        }
    }

    public void CureAllWounds()
    {
        List<Trait> traitsToRemove = new List<Trait>();

        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].ContainsEffect(TraitEffects.Disease) && !traits[i].ContainsEffect(TraitEffects.No_Cure))
            {
                traitsToRemove.Add(traits[i]);
            }
        }

        for (int i = 0; i < traitsToRemove.Count; i++)
        {
            traits.Remove(traitsToRemove[i]);
        }

        for (int i = 0; i < MyBody.bodyParts.Count; i++)
        {
            if (MyBody.bodyParts[i].effect == TraitEffects.Leprosy || MyBody.bodyParts[i].effect == TraitEffects.PreVamp || MyBody.bodyParts[i].effect == TraitEffects.Crystallization)
            {
                MyBody.bodyParts[i].effect = TraitEffects.None;
            }

            for (int j = 0; j < MyBody.bodyParts[i].wounds.Count; j++)
            {
                MyBody.bodyParts[i].wounds[j].Cure(MyBody.bodyParts[i]);
            }
        }
    }

    public void ReplaceOneLimbByDoctor(int limbIndex, int itemIndex, bool fromDoctor)
    {
        if (MyBody.bodyParts[limbIndex].isAttached)
        {
            MyBody.RemoveLimb(limbIndex);
        }

        if (fromDoctor)
        {
            MyInventory.gold -= CostToReplaceLimbs();
        }

        Item replacementLimb = MyInventory.items[itemIndex];
        string newLimbName = (replacementLimb.displayName != "") ? replacementLimb.displayName : replacementLimb.Name;

        BodyPart newPart = new BodyPart(MyBody.bodyParts[limbIndex].name, true, MyBody.bodyParts[limbIndex].slot, false, replacementLimb.HasCComponent<CRot>())
        {
            Attributes = replacementLimb.statMods,
            armor = replacementLimb.armor
        };
        

        if (replacementLimb.HasProp(ItemProperty.OnAttach_Crystallization))
            newPart.effect = TraitEffects.Crystallization;
        else if (replacementLimb.HasProp(ItemProperty.OnAttach_Leprosy))
            newPart.effect = TraitEffects.Leprosy;

        CombatLog.NameItemMessage("Replace_Limb", MyBody.bodyParts[limbIndex].displayName, newLimbName);
        MyBody.bodyParts[limbIndex] = newPart;

        CEquipped ce = replacementLimb.GetCComponent<CEquipped>();

        if (ce != null && ItemList.GetItemByID(ce.itemID) != null && ce.itemID != "stump")
        {
            Item item = ItemList.GetItemByID(ce.itemID);

            MyBody.bodyParts[limbIndex].equippedItem = item;
            MyBody.bodyParts[limbIndex].equippedItem.OnEquip(this, false);

            if (MyBody.bodyParts[limbIndex].slot == ItemProperty.Slot_Arm)
            {
                if (MyBody.bodyParts[limbIndex].hand == null)
                    MyBody.bodyParts[limbIndex].hand = new BodyPart.Hand(MyBody.bodyParts[limbIndex], ItemList.GetItemByID(item.ID), ce.baseItemID);
                else
                    MyBody.bodyParts[limbIndex].hand.SetEquippedItem(ItemList.GetItemByID(item.ID), entity);
            }
        }
        else
        {
            MyBody.bodyParts[limbIndex].equippedItem = ItemList.GetNone();
        }


        if (MyBody.bodyParts[limbIndex].slot == ItemProperty.Slot_Arm)
        {
            if (MyBody.bodyParts[limbIndex].hand == null)
            {
                MyBody.bodyParts[limbIndex].hand = new BodyPart.Hand(MyBody.bodyParts[limbIndex], ItemList.GetItemByID("fists"), "fists");
            }
            else
            {
                MyBody.bodyParts[limbIndex].hand.SetEquippedItem(ItemList.GetItemByID(MyBody.bodyParts[limbIndex].hand.baseItem), entity);
            }
        }

        newPart.Attach(this);
        MyInventory.RemoveInstance(replacementLimb);

        MyBody.Categorize(MyBody.bodyParts);
    }

    public void PostTurn()
    {
        healTimer--;
        restoreTimer--;

        if ((entity.resting || ObjectManager.playerEntity.resting && !entity.isPlayer && entity.AI.isFollower()) && healTimer % 3 == 0)
        {
            healTimer--;
        }

        if (healTimer <= 0)
        {
            healTimer = TurnsToHeal();
            RestHP();
        }

        if (restoreTimer <= 0)
        {
            restoreTimer = TurnsToRestore();
            RestST();
        }
    }

    public void UpdateStatusEffects()
    {
        Dictionary<string, int> temp = new Dictionary<string, int>(statusEffects);

        foreach (KeyValuePair<string, int> kvp in temp)
        {
            if (kvp.Key == "Regen")
            {
                Heal(Endurance + 1);

            }
            else if (kvp.Key == "Poison")
            {
                int amount = RNG.Next(1, 5);

                if (hasTraitEffect(TraitEffects.Poison_Resist))
                {
                    amount = (SeedManager.combatRandom.Next(100) < 20) ? 0 : amount / 2;
                }

                if (health > amount)
                {
                    StatusEffectDamage(amount, DamageTypes.Venom);
                }

            }
            else if (kvp.Key == "Bleed")
            {
                int amount = RNG.Next(2, 5);

                if (hasTraitEffect(TraitEffects.Bleed_Resist))
                    amount = (SeedManager.combatRandom.Next(100) < 20) ? 0 : amount / 2;

                if (health > amount)
                {
                    StatusEffectDamage(amount, DamageTypes.Bleed);
                }

            }
            else if (kvp.Key == "Sick" && RNG.Next(100) < 5)
            {
                int amount = RNG.Next(1, 3);
                World.objectManager.CreatePoolOfLiquid(entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, "liquid_vomit", amount);
            }
            else if (kvp.Key == "Aflame")
            {
                int tileID = World.tileMap.CurrentMap.GetTileNumAt(entity.posX, entity.posY);

                if (tileID == Tile.tiles["Water"].ID || tileID == Tile.tiles["Water_Swamp"].ID)
                {
                    statusEffects[kvp.Key] = 0;
                }
                else
                {
                    int damage = RNG.Next(3, 6);
                    float dm = damage;
                    dm *= (-HeatResist * 0.01f);
                    damage += (int)dm;
                    StatusEffectDamage(damage, DamageTypes.Heat);

                }
            }

            if (HasEffect(kvp.Key))
            {
                statusEffects[kvp.Key]--;

                if (kvp.Value <= 0)
                {
                    RemoveStatusEffect(kvp.Key);
                    continue;
                }
            }
        }

        if (!entity.isPlayer && entity.AI.npcBase.HasFlag(NPC_Flags.Deteriortate_HP))
        {
            ApplyDamage(1, false, Color.white, false, true);
        }

        TerrainEffects();
        UpdateDiseases();
        CheckEffectsObjects();
        UpdateRotation();
    }

    void UpdateDiseases()
    {
        for (int i = 0; i < traits.Count; i++)
        {
            traits[i].OnTurn_Update(entity);
        }
    }

    public List<BodyPart> UnCrystallizedParts()
    {
        return MyBody.bodyParts.FindAll(x => x.effect != TraitEffects.Crystallization && x.organic);
    }

    public bool IsFlying()
    {
        if (MyInventory == null)
            return false;

        return (MyInventory.CanFly());
    }

    void TerrainEffects()
    {
        int tileNum = World.tileMap.GetTileID(entity.posX, entity.posY);

        if (Attributes.ContainsKey("Heat Resist") && HeatResist < 80 && tileNum == Tile.tiles["Lava"].ID && !MyInventory.CanFly())
            IndirectAttack(RNG.Next(5), DamageTypes.Heat, null, "<color=orange>lava</color>", true, false, false);
    }

    public int StealthCheck()
    {
        int currentStealth = Attributes["Stealth"];

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                if (!World.tileMap.PassThroughableTile(entity.posX + x, entity.posY + y))
                    currentStealth++;
            }
        }

        return currentStealth;
    }

    public int IllumunationCheck()
    {
        int light = 0;

        for (int i = 0; i < traits.Count; i++)
        {
            for (int j = 0; j < traits[i].stats.Count; j++)
            {
                if (traits[i].stats[j].Stat == "Light")
                    light += traits[i].stats[j].Amount;
            }
        }

        return light;
    }

    public string RadiationDesc()
    {
        if (radiation <= 0)
            return "Rad_1";
        else if (radiation <= 25)
            return "Rad_2";
        else if (radiation <= 50)
            return "Rad_3";
        else if (radiation <= 75)
            return "Rad_4";
        else
            return "Rad_5";
    }

    public WeaponProficiency CheckProficiencies(Item item)
    {
        return proficiencies.GetProficiencyFromItem(item);
    }

    //Via item
    public void AddProficiencyXP(Item item, int amount)
    {
        AddProficiencyXP(CheckProficiencies(item), amount);
    }
    //Directly to Prof
    public void AddProficiencyXP(WeaponProficiency prof, int amount)
    {
        if (!entity.isPlayer)
            return;

        double divisible = (prof.level < 1) ? 1.0 : prof.level;
        double amt = amount / divisible;

        if (prof.level <= 1)
            amt *= 2;

        if (prof.AddXP(amt))
            CombatLog.NameItemMessage("Inc_Prof", prof.name, (prof.level - 1).ToString());
    }

    public void IncreaseArmorProfs(BodyPart part)
    {
        if (!entity.isPlayer)
            return;

        if (part != null && entity.isPlayer && part.isAttached && part.equippedItem.armor > 0)
            AddProficiencyXP(part.equippedItem, RNG.Next(5, 21));
    }

    public void AddTrait(Trait t)
    {
        if (t == null || traits.Contains(t) && !t.stackable)
            return;

        traits.Add(t);

        for (int i = 0; i < t.stats.Count; i++)
        {
            if (t.stats[i].Stat == "Cold Resist")
                Attributes["Cold Resist"] += t.stats[i].Amount;
            else if (t.stats[i].Stat == "Heat Resist")
                Attributes["Heat Resist"] += t.stats[i].Amount;
            else if (t.stats[i].Stat == "Energy Resist")
                Attributes["Energy Resist"] += t.stats[i].Amount;
            else if (t.stats[i].Stat == "Storage Capacity")
                MyInventory.AddRemoveStorage(t.stats[i].Amount);
        }

        EntitySkills skills = GetComponent<EntitySkills>();

        for (int i = 0; i < t.abilityIDs.Count; i++)
        {
            Skill s = SkillList.GetSkillByID(t.abilityIDs[i]);

            if (s != null)
            {
                s.SetFlag(Skill.AbilityOrigin.Trait);
                skills.AddSkill(SkillList.GetSkillByID(t.abilityIDs[i]), Skill.AbilityOrigin.Trait);
            }
        }
    }

    public bool hasTraitEffect(TraitEffects t)
    {
        return (traits.Find(trait => trait.ContainsEffect(t)) != null);
    }
    public bool hasTrait(string id)
    {
        if (id == "" || id == null)
            return false;

        return (traits.Find(x => x.ID == id) != null);
    }
    public string FindRandomUnheldMutation(string[] _traits)
    {
        List<string> newTraits = new List<string>();

        for (int i = 0; i < _traits.Length; i++)
        {
            if (!hasTrait(_traits[i]))
                newTraits.Add(_traits[i]);
        }

        return newTraits.GetRandom();
    }

    public int FirearmRange
    {
        get
        {
            return (Dexterity * 2) - 1 + proficiencies.Firearm.level;
        }
    }

    public int health
    {
        get { return _health; }
        set
        {
            _health = value;

            if (hpChanged != null)
                hpChanged();

            _health = Mathf.Clamp(_health, 0, maxHealth);
        }
    }

    public int stamina
    {
        get { return _stamina; }
        set
        {
            _stamina = value;

            if (stChanged != null)
                stChanged();

            _stamina = Mathf.Clamp(_stamina, 0, maxStamina);
        }
    }

    public bool FastSwimmer()
    {
        if (traits == null)
            return false;

        return (traits.Find(x => x.ContainsEffect(TraitEffects.Fast_Swimming)) != null);
    }

    public bool FasterSwimmer()
    {
        if (traits == null)
            return false;

        return (traits.Find(x => x.ContainsEffect(TraitEffects.Faster_Swimming)) != null);
    }

    int TurnsToHeal()
    {
        int turns = 22 - Endurance;

        turns -= HPRegen;

        if (entity != null && entity.isPlayer && hasTraitEffect(TraitEffects.Fast_Metabolism))
        {
            turns -= 3;
        }

        return Mathf.Clamp(turns, 3, 50);
    }

    int TurnsToRestore()
    {
        int turns = 22 - Endurance;

        turns -= STRegen;

        return Mathf.Clamp(turns, 3, 50);
    }

    public int CostToCureWounds()
    {
        int cost = 0;

        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].ContainsEffect(TraitEffects.Disease) && !traits[i].ContainsEffect(TraitEffects.No_Cure))
            {
                cost += 500 - (Attributes["Charisma"] * 2);
            }
        }

        for (int i = 0; i < MyBody.bodyParts.Count; i++)
        {
            for (int j = 0; j < MyBody.bodyParts[i].wounds.Count; j++)
            {
                cost += 35 - (Attributes["Charisma"] / 2);
            }
        }

        return cost;
    }

    public int CostToReplaceLimbs()
    {
        return 550 - (Attributes["Charisma"] * 2);
    }

    void InitializeFromData()
    {
        if (entity.isPlayer)
        {
            World.turnManager.ChangeWeather(Manager.startWeather);

            radiation = Manager.playerBuilder.radiation;
            addictions = Manager.playerBuilder.addictions;

            UpdateStatusEffects();
        }
        else
        {
            NPC npc = entity.AI.npcBase;
            StatInitializer.GetNPCStats(npc, this);
        }

        healTimer = 10;
        restoreTimer = 10;
    }

    void CreateDamageNumber(string text, Color col)
    {
        if (!GameSettings.Particle_Effects || !entity.isPlayer && !GetComponentInChildren<SpriteRenderer>().enabled)
            return;

        GameObject t = SimplePool.Spawn(World.poolManager.damageEffect, transform.position + new Vector3(0.5f, 0.5f, -1));
        t.transform.parent = transform;
        t.GetComponentInChildren<DamageText>().DisplayText(col, text);
    }

    public SStats ToSimpleStats()
    {
        return new SStats(new Coord(_health, maxHealth), new Coord(_stamina, maxStamina), 
            new Dictionary<string, int>(Attributes), new Dictionary<string, int>(statusEffects), radiation, addictions);
    }
}
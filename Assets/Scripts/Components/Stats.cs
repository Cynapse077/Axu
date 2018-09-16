﻿using UnityEngine;
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

    int _health, _stamina, _hunger;

    System.Random rng
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
        get { return Mathf.Max(Attributes["Strength"], 1); }
    }
    public int Dexterity
    {
        get { return Mathf.Max(Attributes["Dexterity"], 1); }
    }
    public int Intelligence
    {
        get { return Mathf.Max(Attributes["Intelligence"], 1); }
    }
    public int Endurance
    {
        get { return Mathf.Max(Attributes["Endurance"], 1); }
    }

    public int Speed
    {
        get
        {
            return (HasEffect("Haste")) ? Attributes["Speed"] * 2 : Attributes["Speed"];
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
        get { return Attributes["Heat Resist"]; }
    }
    public int ColdResist
    {
        get { return Attributes["Cold Resist"]; }
    }
    public int EnergyResist
    {
        get { return Attributes["Energy Resist"]; }
    }
    public int HPRegen
    {
        get { return Attributes["HP Regen"]; }
    }
    public int STRegen
    {
        get { return Attributes["ST Regen"]; }
    }

    Inventory inventory
    {
        get { return entity.inventory; }
    }
    Body body
    {
        get { return entity.body; }
    }

    public void Init()
    {
        proficiencies = new PlayerProficiencies();
        traits = new List<Trait>();

        InitializeFromData();

        World.turnManager.incrementTurnCounter += this.UpdateStatusEffects;
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
        World.turnManager.incrementTurnCounter -= this.UpdateStatusEffects;
    }

    public void ChangeAttribute(string attribute, int amount)
    {
        if (Attributes.ContainsKey(attribute))
            Attributes[attribute] += amount;
        else
            Attributes.Add(attribute, amount);
    }

    public void ConsumedAddictiveSubstance(string id, bool liquid)
    {
        if (addictions.Find(x => x.addictedID == id) != null)
            addictions.Find(x => x.addictedID == id).ItemUse(this);
        else
            addictions.Add(new Addiction(id, (liquid ? ItemList.GetLiquidByID(id).addictiveness : 5)));
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
                    statusEffects[effectName] += turns - 1;
            }
            else
            {
                statusEffects.Add(effectName, turns - 1);

                if (LocalizationManager.GetContent("Message_SE_" + effectName) != LocalizationManager.defaultText)
                    CombatLog.NameMessage("Message_SE_" + effectName, entity.MyName);
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
        freezeEffect.SetActive(Frozen());

        if (entity.isPlayer)
            World.userInterface.UpdateStatusEffects(this);
    }

    public void GainExperience(int amount)
    {
        if (MyLevel != null && !hasTraitEffect(TraitEffects.Vampirism))
            MyLevel.AddXP(amount);
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
        int hitChance = (entity.isPlayer) ? 45 : 50;

        hitChance += (CheckProficiencies(wep).level * 3) + (Accuracy * 5) + (wep.Accuracy * 5);
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
        List<BodyPart.Hand> hands = body.Hands;

        for (int i = 0; i < hands.Count; i++)
        {
            int ranNum = rng.Next(100);
            float chance = proficiencies.Shield.level + 1;

            if (hands[i].equippedItem == null)
                hands[i].SetEquippedItem(ItemList.GetItemByID(inventory.baseWeapon), entity);

            if (hands[i].equippedItem.GetItemComponent<CBlock>() != null)
            {
                chance += (hands[i].equippedItem.GetItemComponent<CBlock>().level * 5);

                if (ranNum <= chance)
                {
                    AddProficiencyXP(proficiencies.Shield, Intelligence / 2 + 1);
                    hands[i].equippedItem.OnBlock(entity, attacker);
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

        if (targetPart == null)
            targetPart = Utility.WeightedChoice(body.TargetableBodyParts());
        if (targetPart.slot == ItemProperty.Slot_Head && rng.CoinFlip())
            crit = true;

        //Blocking with shields
        if (BlockedWithShield(attacker))
            amount = 0;

        //Calculate damage
        int damage = CalculateDamage(amount, damTypes, crit, targetPart);

        if (damage > 0)
        {
            if (hasTraitEffect(TraitEffects.Zap_On_Hit) && rng.Next(100) < 5)
                attacker.stats.IndirectAttack(rng.Next(1, 6), DamageTypes.Energy, entity, LocalizationManager.GetContent("Shock"), true);

            HandleSeverence(damTypes, targetPart, sevChance);
            weapon.OnHit(attacker, entity);

            if (SeedManager.combatRandom.Next(100) < 20)
                body.TrainLimbOfType(new ItemProperty[] { ItemProperty.Slot_Back, ItemProperty.Slot_Chest });

        }
        else if (!damTypes.Contains(DamageTypes.Cold) && damTypes.Contains(DamageTypes.Heat) && !damTypes.Contains(DamageTypes.Energy))
            World.soundManager.BlockSound();

        if (entity.isPlayer || entity.AI.InSightOfPlayer())
        {
            bool displayOrange = entity.isPlayer;

            if (damage <= 0)
                displayOrange = !displayOrange;
        }

        CombatLog.Combat_Full(entity.isPlayer, damage, crit, gameObject.name, false, attacker.name, targetPart.displayName, weapon.DisplayName());
        PostDamage(attacker, damage, damTypes, targetPart);

        return (damage > 0);
    }

    bool CanHit(Entity attacker)
    {
        //Toppled vs Flying enemy. If grip exists, can still hit.
        if (inventory.CanFly() && attacker.stats.HasEffect("Topple"))
        {
            bool willMiss = true;

            if (body.AllGripsAgainst().Count > 0)
            {
                foreach (BodyPart.Grip g in body.AllGripsAgainst())
                {
                    if (g.myPart.myBody == attacker.body)
                    {
                        willMiss = false;
                        break;
                    }
                }
            }
            if (willMiss && body.AllGrips().Count > 0)
            {
                foreach (BodyPart.Grip g in body.AllGrips())
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
            if (targetPart.effect == TraitEffects.Crystallization)
            {
                if (damageType.Contains(DamageTypes.Blunt) && rng.Next(10) == 0)
                    sever = true;
            }
            else if (damageType.Contains(DamageTypes.Cleave) || damageType.Contains(DamageTypes.Slash) && rng.Next(10) == 0)
                sever = true;
        }
        else
        {
            if (GetComponent<BaseAI>().npcBase.HasFlag(NPC_Flags.Solid_Limbs))
            {
                if (damageType.Contains(DamageTypes.Blunt))
                    sever = true;
            }
            else if (damageType.Contains(DamageTypes.Cleave) || damageType.Contains(DamageTypes.Slash) && rng.Next(10) == 0)
                sever = true;
        }

        if (sever && rng.Next(100) < (3 + sevChance))
        {
            if (targetPart.slot == ItemProperty.Slot_Head)
            {
                if (body.bodyParts.FindAll(x => x.slot == ItemProperty.Slot_Head).Count > 1)
                {
                    body.RemoveLimb(targetPart);
                    CombatLog.NameItemMessage("Sever_Limb", gameObject.name, targetPart.displayName);
                }
            }
            else
            {
                body.RemoveLimb(targetPart);
                CombatLog.NameItemMessage("Sever_Limb", gameObject.name, targetPart.displayName);
            }
        }
    }

    //has a source, just not from an entity or its weapon. from a string instead.
    public int IndirectAttack(int amount, HashSet<DamageTypes> dTypes, Entity attacker, string sourceName, bool ignoreArmor, bool crit = false, bool ignoreResists = false)
    {
        if (invincible || dead)
            return 0;

        BodyPart targetPart = Utility.WeightedChoice(body.TargetableBodyParts());
        int damage = CalculateDamage(amount, dTypes, crit, targetPart, ignoreArmor, ignoreResists);

        if (damage <= 0)
        {
            PostDamage(attacker, damage, dTypes, targetPart);
            return 0;
        }

        CombatLog.NewIndirectCombat("Damage_Indirect", damage, sourceName, gameObject.name, targetPart.displayName, entity.isPlayer);
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

        BodyPart targetPart = Utility.WeightedChoice(body.TargetableBodyParts());
        HashSet<DamageTypes> dTypes = new HashSet<DamageTypes>() { DamageTypes.Blunt };
        int damage = CalculateDamage(amount, dTypes, false, targetPart, true, true);

        CombatLog.NewSimpleCombat("Damage_Simplified", damage, gameObject.name, entity.isPlayer);
        PostDamage(null, damage, dTypes, targetPart);
    }

    void PostDamage(Entity attacker, int damage, HashSet<DamageTypes> dt, BodyPart hitBodyPart)
    {
        if (attacker != null)
            lastHit = attacker;

        if (damage > 0)
            entity.CancelWalk();

        if (health <= 0)
        {
            CombatLog.NameMessage("Message_Die", gameObject.name);
            return;
        }

        if (attacker != null && !entity.isPlayer)
        {
            entity.AI.SetTarget(attacker);

            if (attacker == ObjectManager.playerEntity)
            {
                if (!entity.AI.npcBase.HasFlag(NPC_Flags.Follower) && entity.AI.HasDetectedPlayer(5))
                    entity.AI.BecomeHostile();
            }
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
        int def = (ignoreArmor) ? 0 : targettedPartArmor + Defense;

        if (damageTypes.Contains(DamageTypes.Heat) || damageTypes.Contains(DamageTypes.Cold) || damageTypes.Contains(DamageTypes.Energy))
            def = 0;

        if (!entity.isPlayer && !entity.AI.HasSeenPlayer())
            crit = true;

        if (HasEffect("Shield"))
            def *= 2;

        damage = amount - def + (crit ? (damage / 2) : 0);
        damage = Mathf.Clamp(damage, 0, 999);

        if (damage > 0 && !ignoreArmor)
        {
            if (rng.Next(100) < inventory.ArmorProfLevelFromBP(targetPart))
                return 0;

            //resistances.
            if (!ignoreResists)
            {
                if (damageTypes.Contains(DamageTypes.Heat))
                {
                    float dm = damage;
                    dm *= (-HeatResist * 0.01f);
                    damage += (int)dm;

                    if (SeedManager.combatRandom.Next(100) < 3)
                        AddStatusEffect("Aflame", SeedManager.combatRandom.Next(2, 6));
                }
                else if (damageTypes.Contains(DamageTypes.Cold))
                {
                    float dm = damage;
                    dm *= (-ColdResist * 0.01f);
                    damage += (int)dm;

                    if (SeedManager.combatRandom.Next(100) < 3)
                        AddStatusEffect("Slow", SeedManager.combatRandom.Next(2, 5));
                }
                else if (damageTypes.Contains(DamageTypes.Energy))
                {
                    float dm = damage;
                    dm *= (-EnergyResist * 0.01f);
                    damage += (int)dm;

                    if (SeedManager.combatRandom.Next(100) < 3)
                        AddStatusEffect("Stun", SeedManager.combatRandom.Next(1, 3));
                }
            }
        }

        if (damage > 0)
        {
            if (entity.isPlayer && damage > 5 && SeedManager.combatRandom.Next(1000) < 5)
                targetPart.WoundMe(damageTypes);

            ApplyDamage(damage, crit, Color.white);
        }

        return damage;
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

        if (rng.Next(100) < 20 && bloodstain)
            entity.CreateBloodstain();

        if (health <= 0)
            Die();
    }

    public void Die()
    {
        if (onDeath != null)
            onDeath(entity);

        entity.fighter.Die();
    }

    public void StatusEffectDamage(int amount, DamageTypes damageType)
    {
        if (invincible || amount <= 0)
            return;

        Color c = Color.red;

        if (damageType == DamageTypes.Venom)
            c = Color.green;
        else if (damageType == DamageTypes.Bleed)
        {
            c = Color.magenta;
            string bloodType = "liquid_blood";

            if (entity.isPlayer)
            {
                if (HasEffect("Poison"))
                    bloodType = "liquid_blood_pois";
                if (hasTraitEffect(TraitEffects.Leprosy))
                    bloodType = "liquid_blood_lep";
                if (hasTraitEffect(TraitEffects.Vampirism) || hasTraitEffect(TraitEffects.PreVamp))
                    bloodType = "liquid_blood_vamp";

                World.objectManager.CreatePoolOfLiquid(entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, bloodType, 1);
            }
            else
            {
                if (entity.AI.npcBase.HasFlag(NPC_Flags.Skills_Leprosy))
                    bloodType = "liquid_blood_lep";

                if (!entity.AI.npcBase.HasFlag(NPC_Flags.No_Blood))
                    World.objectManager.CreatePoolOfLiquid(entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, bloodType, 1);
            }

            entity.CreateBloodstain(false, 30);
        }
        else if (damageType == DamageTypes.Hunger)
            c = Color.yellow;

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

            if (entity.isPlayer || entity.AI.npcBase.HasFlag(NPC_Flags.Follower))
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

    public bool Frozen()
    {
        return (HasEffect("Slow") && statusEffects["Slow"] >= Speed - 3);
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

        if (hasTraitEffect(TraitEffects.Rad_Resist))
        {
            if (amount <= 1)
                return;

            amount /= 2;
        }

        radiation += amount;

        if (radiation >= 100 || radiation >= 85 && rng.Next(100) < 5)
            Mutate();
    }

    public void GiveTrait(string traitID)
    {
        Trait t = TraitList.GetTraitByID(traitID);

        if (t != null)
            InitializeNewTrait(t);
    }

    public void ForceMutation()
    {
        Mutate();
    }

    //Cause a random mutation to occur. Less likely as you have more.
    public void Mutate(string mutID = "")
    {
        if (mutID == "" && TraitList.GetAvailableMutations(this).Count <= 0)
        {
            radiation = 0;
            return;
        }

        if (hasTraitEffect(TraitEffects.Rad_Resist) && rng.Next(100) < 10)
        {
            radiation /= 2;
            return;
        }

        Trait mutation = (mutID == "") ? TraitList.GetAvailableMutations(this).GetRandom(SeedManager.combatRandom) : TraitList.GetTraitByID(mutID);

        if (mutation == null)
        {
            radiation = 0;
            return;
        }

        if (!mutation.stackable && hasTrait(mutation.ID) && string.IsNullOrEmpty(mutation.nextTier))
        {
            radiation = rng.Next(2, 31);
            return;
        }
        else

        //Evolve mutation
        if (!mutation.stackable && hasTrait(mutation.ID) && !string.IsNullOrEmpty(mutation.nextTier))
        {
            Trait newMut = TraitList.GetTraitByID(mutation.nextTier);
            RemoveTrait(mutation.ID);

            mutation = new Trait(newMut);
        }

        CheckMutationIntegrity(mutation);
        InitializeNewTrait(mutation);
        radiation = rng.Next(2, 31);

        CombatLog.NameMessage("Message_Mutate", mutation.name);
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
                    skills.RemoveSkill(t.abilityIDs[i]);
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
                RemoveTrait(Mutations.GetRandom().ID);
        }
    }

    public void CureAllWounds()
    {
        List<Trait> traitsToRemove = new List<Trait>();

        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].ContainsEffect(TraitEffects.Disease) && !traits[i].ContainsEffect(TraitEffects.No_Cure))
                traitsToRemove.Add(traits[i]);
        }

        for (int i = 0; i < traitsToRemove.Count; i++)
        {
            traits.Remove(traitsToRemove[i]);
        }

        for (int i = 0; i < body.bodyParts.Count; i++)
        {
            if (body.bodyParts[i].effect == TraitEffects.Leprosy || body.bodyParts[i].effect == TraitEffects.PreVamp || body.bodyParts[i].effect == TraitEffects.Crystallization)
                body.bodyParts[i].effect = TraitEffects.None;

            for (int j = 0; j < body.bodyParts[i].wounds.Count; j++)
            {
                body.bodyParts[i].wounds[j].Cure(body.bodyParts[i]);
            }
        }
    }

    public void ReplaceOneLimbByDoctor(int limbIndex, int itemIndex, bool fromDoctor)
    {
        if (body.bodyParts[limbIndex].isAttached)
            body.RemoveLimb(limbIndex);

        if (fromDoctor)
            inventory.gold -= CostToReplaceLimbs();

        Item replacementLimb = inventory.items[itemIndex];
        string newLimbName = (replacementLimb.displayName != "") ? replacementLimb.displayName : replacementLimb.Name;

        BodyPart newPart = new BodyPart(body.bodyParts[limbIndex].name, true, body.bodyParts[limbIndex].slot, false, replacementLimb.GetItemComponent<CRot>() != null);
        newPart.Attributes = replacementLimb.statMods;
        newPart.armor = replacementLimb.armor;

        if (replacementLimb.HasProp(ItemProperty.OnAttach_Crystallization))
            newPart.effect = TraitEffects.Crystallization;
        else if (replacementLimb.HasProp(ItemProperty.OnAttach_Leprosy))
            newPart.effect = TraitEffects.Leprosy;

        CombatLog.NameItemMessage("Replace_Limb", body.bodyParts[limbIndex].displayName, newLimbName);
        body.bodyParts[limbIndex] = newPart;

        CEquipped ce = replacementLimb.GetItemComponent<CEquipped>();

        if (ce != null && ItemList.GetItemByID(ce.itemID) != null && ce.itemID != "stump")
        {
            Item item = ItemList.GetItemByID(ce.itemID);

            body.bodyParts[limbIndex].equippedItem = item;
            body.bodyParts[limbIndex].equippedItem.OnEquip(this, false);

            if (body.bodyParts[limbIndex].slot == ItemProperty.Slot_Arm)
            {
                if (body.bodyParts[limbIndex].hand == null)
                    body.bodyParts[limbIndex].hand = new BodyPart.Hand(body.bodyParts[limbIndex], ItemList.GetItemByID(item.ID));
                else
                    body.bodyParts[limbIndex].hand.SetEquippedItem(ItemList.GetItemByID(item.ID), entity);
            }
        }
        else
            body.bodyParts[limbIndex].equippedItem = ItemList.GetNone();


        if (body.bodyParts[limbIndex].slot == ItemProperty.Slot_Arm)
        {
            if (body.bodyParts[limbIndex].hand == null)
            {
                body.bodyParts[limbIndex].hand = new BodyPart.Hand(body.bodyParts[limbIndex], ItemList.GetItemByID(inventory.baseWeapon));
            }
            else
            {
                body.bodyParts[limbIndex].hand.SetEquippedItem(ItemList.GetItemByID(inventory.baseWeapon), entity);
            }
        }

        newPart.Attach(this);
        inventory.RemoveInstance(replacementLimb);

        body.Categorize(body.bodyParts);
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
                int amount = rng.Next(1, 4);

                if (hasTraitEffect(TraitEffects.Poison_Resist))
                    amount = (SeedManager.combatRandom.Next(100) < 20) ? 0 : amount / 2;

                if (health > amount)
                    StatusEffectDamage(amount, DamageTypes.Venom);

            }
            else if (kvp.Key == "Bleed")
            {
                int amount = rng.Next(2, 4);

                if (hasTraitEffect(TraitEffects.Bleed_Resist))
                    amount = (SeedManager.combatRandom.Next(100) < 20) ? 0 : amount / 2;

                if (health > amount)
                    StatusEffectDamage(amount, DamageTypes.Bleed);

            }
            else if (kvp.Key == "Sick" && rng.Next(100) < 5)
            {
                int amount = rng.Next(1, 4);
                World.objectManager.CreatePoolOfLiquid(entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, "liquid_vomit", amount);
                Hunger -= (amount * 100);

            }
            else if (kvp.Key == "Aflame")
            {
                int damage = rng.Next(3, 6);
                float dm = (float)damage;
                dm *= (-HeatResist * 0.01f);
                damage += (int)dm;
                StatusEffectDamage(damage, DamageTypes.Heat);
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

        if (entity.isPlayer)
        {
            if (_hunger <= 0 && World.turnManager.turn % 3 == 0)
                StatusEffectDamage(1, DamageTypes.Hunger);
        }
        else if (entity.AI.npcBase.HasFlag(NPC_Flags.Deteriortate_HP))
            ApplyDamage(1, false, Color.white, false, true);

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
        return body.bodyParts.FindAll(x => x.effect != TraitEffects.Crystallization && x.organic);
    }

    public bool IsFlying()
    {
        if (inventory == null)
            return false;

        return (inventory.CanFly());
    }

    void TerrainEffects()
    {
        int tileNum = World.tileMap.CheckTileID(entity.posX, entity.posY);

        if (Attributes.ContainsKey("Heat Resist") && HeatResist < 80 && tileNum == Tile.tiles["Lava"].ID && !inventory.CanFly())
            IndirectAttack(rng.Next(5), DamageTypes.Heat, null, "<color=orange>lava</color>", true, false, false);
    }

    public void HungerTick()
    {
        int amount = 1;

        if (World.turnManager.turn % 3 == 0)
        {
            if (hasTraitEffect(TraitEffects.Fast_Metabolism))
                amount++;
            if (hasTraitEffect(TraitEffects.Slow_Metabolism))
                amount--;
        }

        if (hasTraitEffect(TraitEffects.Vampirism) && World.turnManager.turn % 4 == 0)
            amount++;

        Hunger -= amount;
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

    public string HungerString()
    {
        string title = hasTraitEffect(TraitEffects.Vampirism) ? "Thirst_" : "Food_";

        if (_hunger >= Globals.Satiated)
            return LocalizationManager.GetLocalizedContent(title + "1")[0];
        if (_hunger < Globals.Satiated && _hunger >= Globals.Hungry)
            return LocalizationManager.GetLocalizedContent(title + "2")[0];
        if (_hunger < Globals.Hungry && _hunger >= Globals.VHungry)
            return LocalizationManager.GetLocalizedContent(title + "3")[0];
        if (_hunger < Globals.VHungry && _hunger > Globals.Starving)
            return LocalizationManager.GetLocalizedContent(title + "4")[0];

        return LocalizationManager.GetLocalizedContent(title + "5")[0];
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

        double divisible = (prof.level < 1) ? 1.0 : (double)prof.level;
        double amt = (double)amount / divisible;

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
            AddProficiencyXP(part.equippedItem, rng.Next(5, 21));
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
                inventory.AddRemoveStorage(t.stats[i].Amount);
        }

        EntitySkills skills = GetComponent<EntitySkills>();

        for (int i = 0; i < t.abilityIDs.Count; i++)
        {
            if (SkillList.GetSkillByID(t.abilityIDs[i]) != null)
                skills.AddSkill(SkillList.GetSkillByID(t.abilityIDs[i]));
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

    public int Hunger
    {
        get { return _hunger; }
        set
        {
            if (Manager.noHunger)
                return;
            _hunger = value;
            _hunger = Mathf.Clamp(_hunger, 0, Globals.Full);
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

    public int TurnsToHeal()
    {
        int turns = 20 - Endurance;

        turns -= HPRegen;
        if (entity != null && entity.isPlayer)
        {
            if (hasTraitEffect(TraitEffects.Fast_Metabolism))
                turns -= 3;
            if (hasTraitEffect(TraitEffects.Slow_Metabolism))
                turns += 2;
        }

        return Mathf.Clamp(turns, 1, 50);
    }

    public int TurnsToRestore()
    {
        int turns = 20 - Endurance;

        turns -= STRegen;
        return Mathf.Clamp(turns, 1, 50);
    }

    public int CostToCureWounds()
    {
        int cost = 0;

        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].ContainsEffect(TraitEffects.Disease) && !traits[i].ContainsEffect(TraitEffects.No_Cure))
                cost += 500 - (Attributes["Charisma"] * 2);
        }

        for (int i = 0; i < body.bodyParts.Count; i++)
        {
            for (int j = 0; j < body.bodyParts[i].wounds.Count; j++)
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

            if (Manager.newGame)
                Manager.playerBuilder.hunger = Globals.Full;

            Hunger = Manager.playerBuilder.hunger;
            radiation = Manager.playerBuilder.radiation;
            addictions = Manager.playerBuilder.addictions;

            UpdateStatusEffects();
        }
        else
        {
            NPC npc = entity.AI.npcBase;
            StatInitializer.GetNPCStats(npc, this);
        }
    }

    void CreateDamageNumber(string text, Color col)
    {
        if (!GameSettings.Particle_Effects || !entity.isPlayer && !GetComponentInChildren<SpriteRenderer>().enabled)
            return;

        GameObject t = SimplePool.Spawn(World.poolManager.damageEffect, transform.position + new Vector3(0.5f, 0.5f, -1), Quaternion.identity);
        t.transform.parent = transform;
        t.GetComponentInChildren<DamageText>().DisplayText(col, text);
    }

    public SStats ToSimpleStats()
    {
        return new SStats(new Coord(_health, maxHealth), new Coord(_stamina, maxStamina), new Dictionary<string, int>(Attributes), new Dictionary<string, int>(statusEffects), radiation, addictions);
    }
}

public static class Globals
{
    public const int Full = 8000, Satiated = 7500, Hungry = 5000, VHungry = 3000, Starving = 500;
}
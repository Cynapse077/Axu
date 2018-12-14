using System.Collections.Generic;
using UnityEngine;

[MoonSharp.Interpreter.MoonSharpUserData]
public class CombatComponent
{
    public Item itemForThrowing;
    public Entity lastTarget;

    Entity entity;

    Stats MyStats
    {
        get { return entity.stats; }
    }
    Inventory MyInventory
    {
        get { return entity.inventory; }
    }
    Body MyBody
    {
        get { return entity.body; }
    }

    public CombatComponent(Entity e)
    {
        entity = e;
    }

    public void Attack(Stats target, bool freeAction = false, BodyPart targetPart = null, int sevChance = 0)
    {
        if (entity.isPlayer && target.entity.AI.npcBase.HasFlag(NPC_Flags.Follower))
            return;

        //Main weapon
        PerformAttack(target, MyBody.MainHand, targetPart, sevChance);

        //Attack with all other arms.
        List<BodyPart.Hand> hands = MyBody.Hands;

        for (int i = 0; i < hands.Count; i++)
        {
            if (hands[i] != MyBody.MainHand && hands[i].IsAttached && SeedManager.combatRandom.Next(80) <= (MyStats.Dexterity + 1))
                PerformAttack(target, hands[i]);
        }

        //TODO: Add extra attacks for bites, kicks, headbutts, etc.

        lastTarget = target.entity;

        if (!freeAction)
            entity.EndTurn(0.1f, AttackAPCost());
    }

    bool PerformAttack(Stats target, BodyPart.Hand hand, BodyPart targetPart = null, int sevChance = 0)
    {
        if (hand == null || target == null || hand.EquippedItem == null)
            return false;

        Item wep = hand.EquippedItem;
        bool twoHandPenalty = wep.HasProp(ItemProperty.Two_Handed) && MyInventory.TwoHandPenalty(hand);
        HashSet<DamageTypes> dt = wep.damageTypes;
        int missChance = MyStats.MissChance(wep);
        int profLevel = (entity.isPlayer) ? MyStats.CheckProficiencies(wep).level : entity.AI.npcBase.weaponSkill;

        if (MyStats.HasEffect("Topple"))
            missChance += 5;
        if (twoHandPenalty)
            missChance += 5;

        if (SeedManager.combatRandom.Next(100) < missChance)
        {
            target.Miss(entity, wep);
            return false;
        }

        int damage = wep.CalculateDamage(MyStats.Strength, profLevel);

        //firearms have reduced physical damage.
        if (wep.HasProp(ItemProperty.Ranged))
        {
            dt = new HashSet<DamageTypes>() { DamageTypes.Blunt };
            Damage d = new Damage(1, 3, 0);
            damage = d.Roll() + (MyStats.Strength / 2 - 1) + MyStats.proficiencies.Misc.level;
        }

        if (targetPart == null)
            targetPart = target.entity.body.TargetableBodyParts().GetRandom(SeedManager.combatRandom);

        if (target.TakeDamage(wep, damage, dt, entity, wep.AttackCrits((profLevel - 1 + MyStats.Accuracy + wep.Accuracy) / 2), targetPart, sevChance))
        {
            if (entity.isPlayer)
            {
                World.soundManager.PlayAttackSound(wep);

                //if it's a physical attack from a firearm, use misc prof
                if (wep.itemType == Proficiencies.Firearm || wep.itemType == Proficiencies.Armor || wep.itemType == Proficiencies.Butchery)
                    MyStats.AddProficiencyXP(MyStats.proficiencies.Misc, MyStats.Intelligence);
                else
                    MyStats.AddProficiencyXP(wep, MyStats.Intelligence);

                if (hand.arm != null)
                    MyBody.TrainLimb(hand.arm);

            }
            else
                ApplyNPCEffects(target, damage);

            ApplyWeaponEffects(target, wep);
        }

        return true;
    }

    void ApplyWeaponEffects(Stats target, Item wep)
    {
        if (wep.HasProp(ItemProperty.OnAttack_Radiation))
        {
            if (SeedManager.combatRandom.Next(100) < 5)
                MyStats.Radiate(1);
        }

        if (target == null || target.health <= 0 || target.dead)
            return;

        if (wep.damageTypes.Contains(DamageTypes.Venom) && SeedManager.combatRandom.Next(100) <= 3)
            target.AddStatusEffect("Poison", SeedManager.combatRandom.Next(2, 8));
        if (wep.ContainsDamageType(DamageTypes.Bleed) && SeedManager.combatRandom.Next(100) <= 5)
            target.AddStatusEffect("Bleed", SeedManager.combatRandom.Next(2, 8));
        if (wep.HasProp(ItemProperty.Stun) && SeedManager.combatRandom.Next(100) <= 5)
            target.AddStatusEffect("Stun", SeedManager.combatRandom.Next(1, 4));
        if (wep.HasProp(ItemProperty.Confusion) && SeedManager.combatRandom.Next(100) <= 5)
            target.AddStatusEffect("Confuse", SeedManager.combatRandom.Next(2, 8));
        if (wep.HasProp(ItemProperty.Knockback) && SeedManager.combatRandom.Next(100) <= 5)
        {
            Entity otherEntity = target.entity;

            if (otherEntity != null && otherEntity.myPos.DistanceTo(entity.myPos) < 2f)
                otherEntity.ForceMove(otherEntity.posX - entity.posX, otherEntity.posY - entity.posY, MyStats.Strength - 1);
        }
    }

    void ApplyNPCEffects(Stats target, int damage)
    {
        entity.AI.OnHitEffects(target, damage);
    }

    public int AttackAPCost()
    {
        int timeCost = MyBody.MainHand.EquippedItem.GetAttackAPCost() + MyStats.AttackDelay;

        if (MyStats.HasEffect("Topple"))
            timeCost += 7;
        if (MyInventory.TwoHandPenalty(MyBody.MainHand))
            timeCost += 4;

        return timeCost;
    }

    //Makes the current item to throw this item. 
    public void SelectItemToThrow(Item i)
    {
        itemForThrowing = i;
    }
    //Actually does the throw baloney
    public void ThrowItem(Coord destination, Explosive damageScript)
    {
        if (itemForThrowing == null || destination == null || damageScript == null)
            return;

        bool miss = (SeedManager.combatRandom.Next(100) > 40 + MyStats.proficiencies.Throwing.level + MyStats.Accuracy);

        if (miss)
        {
            Coord newPos = AdjacentCoord(destination);

            if (newPos != null)
                destination = newPos;
        }

        if (entity.isPlayer || entity.AI.InSightOfPlayer())
            CombatLog.NameItemMessage("Message_ThrowItem", entity.MyName, itemForThrowing.DisplayName());

        //Instantiate
        entity.InstatiateThrowingEffect(destination, 1.0f);

        if (!itemForThrowing.HasProp(ItemProperty.Explosive))
        {
            int throwingLevel = (entity.isPlayer) ? MyStats.proficiencies.Throwing.level : Mathf.Clamp((World.DangerLevel() / 10) + 1, 0, 3);
            damageScript.damage = itemForThrowing.ThrownDamage(throwingLevel, MyStats.Dexterity);
            damageScript.SetName(itemForThrowing.DisplayName());
        }

        damageScript.localPosition = destination;
        MyInventory.ThrowItem(destination, itemForThrowing, damageScript);
        itemForThrowing = null;

        if (entity.isPlayer)
        {
            MyStats.AddProficiencyXP(MyStats.proficiencies.Throwing, MyStats.Intelligence);
        }

        entity.EndTurn(0.3f);
    }

    public void ShootFireArm(Coord targetPos, int iteration)
    {
        if (World.tileMap.WalkableTile(targetPos.x, targetPos.y))
        {
            Cell c = World.tileMap.GetCellAt(targetPos.x, targetPos.y);

            if (c != null && c.entity != null)
                lastTarget = c.entity;
        }

        TileDamage td = new TileDamage(entity, targetPos, entity.inventory.firearm.damageTypes);

        if (FirearmMiss(targetPos, iteration))
        {
            td.pos.x += SeedManager.combatRandom.Next(-1, 2);
            td.pos.y += SeedManager.combatRandom.Next(-1, 2);
        }

        //entity.BulletTrail(entity.myPos.toVector2(), td.pos.toVector2());
        entity.InstatiateThrowingEffect(td.pos, 2.0f);

        td.damage = entity.inventory.firearm.CalculateDamage(entity.stats.Dexterity - 4, entity.stats.CheckProficiencies(entity.inventory.firearm).level);
        td.crit = entity.inventory.firearm.AttackCrits(entity.stats.proficiencies.Firearm.level + 1);
        td.myName = LocalizationManager.GetContent("Bullet");
        td.ApplyDamage();

        entity.stats.AddProficiencyXP(entity.inventory.firearm, entity.stats.Dexterity);
    }

    bool FirearmMiss(Coord targetPos, int iteration)
    {
        float denom = (float)entity.stats.proficiencies.Firearm.level + 2 + entity.stats.Accuracy / 2 - entity.inventory.firearm.Accuracy;

        if (denom <= 0)
            denom += 0.05f;

        float missChance = (1.0f / denom) * 100f;
        float maxMiss = SeedManager.combatRandom.Next(100);

        if (entity.myPos.DistanceTo(targetPos) >= entity.stats.FirearmRange)
            missChance *= 1.5f;

        if (entity.inventory.firearm.Accuracy < 0)
            missChance *= 2.0f;

        if (!entity.inventory.firearm.HasProp(ItemProperty.Burst))
            maxMiss -= (3 * iteration);

        return maxMiss <= missChance;
    }

    Coord AdjacentCoord(Coord destination)
    {
        List<Coord> nearbyCoords = new List<Coord>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) >= 2 || destination.x + x < 0 || destination.x >= Manager.localMapSize.x
                    || destination.y + y < 0 || destination.y + y >= Manager.localMapSize.y)
                    continue;

                if (World.tileMap.PassThroughableTile(destination.x + x, destination.y + y) && destination.x != entity.posX && destination.y != entity.posY)
                    nearbyCoords.Add(new Coord(destination.x + x, destination.y + y));
            }
        }

        return nearbyCoords.GetRandom(SeedManager.combatRandom);
    }

    public int CheckWepType()
    {
        switch (MyBody.MainHand.EquippedItem.attackType)
        {
            case Item.AttackType.Slash: return 0;
            case Item.AttackType.Spear: return 1;
            case Item.AttackType.Bash: default: return 2;
            case Item.AttackType.Sweep: return 3;
            //4 is corner variation of sweep.
            case Item.AttackType.Claw: case Item.AttackType.Psy: return 5;
            case Item.AttackType.Knife: return 6;
            case Item.AttackType.Bite: return 7;

        }
    }

    public void Die()
    {
        entity.canAct = false;
        MyStats.dead = true;
        entity.CreateBloodstain(true);

        if (entity.cell != null)
            entity.cell.UnSetEntity(entity);

        if (entity.isPlayer)
        {
            World.userInterface.PlayerDied(((MyStats.lastHit == null) ? "<color=yellow>???</color>" : MyStats.lastHit.name));

            if (World.difficulty.Level == Difficulty.DiffLevel.Scavenger && MyInventory.items.Count > 0)
            {
                int numToDrop = SeedManager.combatRandom.Next(0, 3);

                for (int i = 0; i < numToDrop; i++)
                {
                    Item iToDrop = MyInventory.items.GetRandom();

                    if (iToDrop.HasProp(ItemProperty.Quest_Item))
                            continue;

                    MyInventory.Drop(iToDrop);
                }
            }

            //Fail all arena quests
            List<Quest> questsToFail = ObjectManager.playerJournal.quests.FindAll(x => x.failOnDeath);

            for (int i = 0; i < questsToFail.Count; i++)
            {
                questsToFail[i].Fail();
            }
        }
        else
        {
            MyInventory.DropAll();

            if (MyStats.lastHit != null)
            {
                //Add relevant XP to the character that struck this NPC last
                //Maybe should add all XP to player unless it's a friendly NPC.
                MyStats.lastHit.stats.GainExperience((MyStats.Strength + MyStats.Dexterity + MyStats.Intelligence + MyStats.Endurance * 2) / 2 + 1);
            }

            World.objectManager.DemolishNPC(entity, entity.AI.npcBase);
            GameObject.Destroy(entity.gameObject);
        }
    }

    public void Remove()
    {
        entity.canAct = false;
        MyStats.dead = true;

        if (entity.cell != null)
            entity.cell.UnSetEntity(entity);

        World.objectManager.DemolishNPC(entity, entity.AI.npcBase);
        GameObject.Destroy(entity.gameObject);
    }
}

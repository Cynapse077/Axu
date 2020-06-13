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

    int ExtraAttackChance
    {
        get { return (MyStats.Dexterity * 2) - 4; }
    }

    public CombatComponent(Entity e)
    {
        entity = e;
    }

    public void Attack(Stats target, bool freeAction = false, BodyPart targetPart = null, int sevChance = 0)
    {
        if (entity.isPlayer && target.entity.AI.isFollower())
        {
            return;
        }

        bool attacked = false;
        Item soundItem = null;

        int diffX = target.entity.posX - entity.posX;
        int diffY = target.entity.posY - entity.posY;

        //Main weapon
        if (MyBody.MainHand.arm != null && !MyBody.MainHand.arm.FreeToMove())
        {
            MyBody.MainHand.arm.TryBreakGrips();
        }
        else if (PerformAttack(target, MyBody.MainHand, targetPart, sevChance))
        {
            if (InRange(MyBody.MainHand.EquippedItem, diffX, diffY))
            {
                attacked = true;
            }

            soundItem = MyBody.MainHand.EquippedItem;
        }

        if (target.dead)
        {
            return;
        }

        lastTarget = target.entity;

        //Attack with all other arms.
        List<BodyPart.Hand> hands = MyBody.Hands;

        for (int i = 0; i < hands.Count; i++)
        {
            if (hands[i] != null && hands[i] != MyBody.MainHand && hands[i].IsAttached && RNG.Chance(ExtraAttackChance) || !attacked)
            {
                if (!hands[i].arm.FreeToMove())
                {
                    hands[i].arm.TryBreakGrips();
                }
                else if (PerformAttack(target, hands[i]))
                {
                    if (InRange(hands[i].EquippedItem, diffX, diffY))
                    {
                        attacked = true;
                    }

                    soundItem = hands[i].EquippedItem;
                }
            }
        }

        if (target.dead)
        {
            return;
        }

        //Attack with proc weapons.
        for (int i = 0; i < MyBody.bodyParts.Count; i++)
        {
            if (MyBody.bodyParts[i].Attached && MyBody.bodyParts[i].equippedItem.HasProp(ItemProperty.Proc_Attack) && RNG.Chance(ExtraAttackChance))
            {
                if (ExtraProcAttack(target, MyBody.bodyParts[i].equippedItem))
                {
                    soundItem = MyBody.bodyParts[i].equippedItem;
                }
            }
        }

        if (entity.isPlayer && soundItem != null)
        {
            World.soundManager.PlayAttackSound(soundItem);
        }

        if (!freeAction)
        {
            entity.EndTurn(0.1f, AttackAPCost());
        }
    }

    bool InRange(Item wep, int diffX, int diffY)
    {
        if (Mathf.Abs(diffX) == 2 || Mathf.Abs(diffY) == 2)
        {
            return wep.attackType == Item.AttackType.Spear;
        }

        return true;
    }

    public float MissChance(BodyPart.Hand hand, Stats target, BodyPart targetPart)
    {
        if (hand == null || hand.EquippedItem == null || target == null || targetPart == null)
        {
            return 1.0f;
        }

        if (!target.entity.isPlayer && !target.entity.AI.HasSeenPlayer())
        {
            return 0f;
        }

        int miss = MyStats.MissChance(hand.EquippedItem);
        float percentage = 1f + (targetPart.Weight / (float)targetPart.myBody.TotalBodyWeight());

        if (MyInventory.TwoHandPenalty(hand))
        {
            miss += 5;
        }

        return Mathf.Floor(miss * percentage);
    }

    bool PerformAttack(Stats target, BodyPart.Hand hand, BodyPart targetPart = null, int sevChance = 0)
    {
        if (target == null || hand == null || target == null || hand.EquippedItem == null)
        {
            return false;
        }

        if (targetPart == null)
        {
            targetPart = target.entity.body.TargetableBodyParts().GetRandom(SeedManager.combatRandom);
        }

        if (targetPart == null)
        {
            return false;
        }

        Item wep = hand.EquippedItem;
        HashSet<DamageTypes> dt = wep.damageTypes;
        int missChance = Mathf.FloorToInt(MissChance(hand, target, targetPart));

        if (RNG.Chance(missChance))
        {
            target.Miss(entity, wep);

            if (entity.isPlayer)
            {
                World.soundManager.Miss();
            }

            return false;
        }

        int profLevel = (entity.isPlayer) ? MyStats.CheckProficiencies(wep).level : entity.AI.npcBase.weaponSkill;
        int damage = wep.CalculateDamage(wep.GetDamageStats(MyStats), profLevel);
        bool crit = wep.AttackCrits((profLevel - 1 + MyStats.Accuracy + wep.Accuracy) / 2);

        //firearms have reduced physical damage.
        if (wep.HasProp(ItemProperty.Ranged))
        {
            dt = new HashSet<DamageTypes>() { DamageTypes.Blunt };
            Damage d = new Damage(1, 3, 0);
            damage = d.Roll() + (MyStats.Strength / 2 - 1) + MyStats.proficiencies.Misc.level;
        }

        if (target.TakeDamage(wep, damage, dt, entity, crit, targetPart, sevChance))
        {
            if (entity.isPlayer)
            {
                //if it's a physical attack from a firearm, use misc prof
                if (wep.itemType == Proficiencies.Firearm || wep.itemType == Proficiencies.Armor || wep.itemType == Proficiencies.Butchery)
                {
                    MyStats.AddProficiencyXP(MyStats.proficiencies.Misc, MyStats.Intelligence);
                }
                else
                {
                    MyStats.AddProficiencyXP(wep, MyStats.Intelligence);
                }

                if (hand.arm != null)
                {
                    MyBody.TrainLimb(hand.arm);
                }
            }
        }
        else if (entity.isPlayer)
        {
            World.soundManager.Block();
        }

        return true;
    }

    bool ExtraProcAttack(Stats target, Item wep)
    {
        if (target == null || wep == null)
        {
            return false;
        }

        if (RNG.Chance(45))
        {
            target.Miss(entity, wep);
            return false;
        }

        int profLevel = entity.isPlayer ? MyStats.CheckProficiencies(wep).level : entity.AI.npcBase.weaponSkill;
        int damage = wep.CalculateDamage(wep.GetDamageStats(MyStats), profLevel);

        return target.TakeDamage(wep, damage, wep.damageTypes, entity);
    }

    public int AttackAPCost()
    {
        int timeCost = MyBody.MainHand.EquippedItem.GetAttackAPCost() + MyStats.AttackDelay;

        if (MyStats.HasEffect("Topple"))
        {
            timeCost += 7;
        }

        if (MyInventory.TwoHandPenalty(MyBody.MainHand))
        {
            timeCost += 4;
        }

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
        {
            return;
        }

        bool miss = RNG.Next(100) > 40 + MyStats.proficiencies.Throwing.level + MyStats.Accuracy;

        if (miss)
        {
            Coord newPos = AdjacentCoord(destination);

            if (newPos != null)
            {
                destination = newPos;
            }
        }

        if (entity.isPlayer || entity.AI.InSightOfPlayer())
        {
            CombatLog.NameItemMessage("Message_ThrowItem", entity.Name, itemForThrowing.DisplayName());
        }

        InstatiateThrowingEffect(destination, 1.0f);

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
            {
                lastTarget = c.entity;
            }
        }

        Item ammo = entity.inventory.firearm.TryGetCComponent(out CFirearm fComp) ? ItemList.GetItemByID(fComp.currentAmmo) : null;
        TileDamage td = new TileDamage(entity, targetPos, entity.inventory.firearm.damageTypes);

        if (FirearmMiss(targetPos, iteration))
        {
            td.pos.x += RNG.Next(-1, 2);
            td.pos.y += RNG.Next(-1, 2);
        }

        InstatiateThrowingEffect(td.pos, 2.0f);

        td.damage = entity.inventory.firearm.CalculateDamage(entity.stats.Dexterity - 4, entity.stats.CheckProficiencies(entity.inventory.firearm).level);

        var ammoComp = ammo?.GetCComponent<CAmmo>();
        if (ammoComp != null)
        {
            //Add extra damage from ammunition
            td.damage += ammoComp.extraDamage.Roll();
        }

        td.crit = entity.inventory.firearm.AttackCrits(entity.stats.proficiencies.Firearm.level + 1);
        td.myName = ammo == null ? "Bullet".Localize() : ammo.DisplayName();
        td.ApplyDamage();

        if (ammoComp != null)
        {
            //Apply ammunition effects
            ammoComp.OnHit(entity, td.pos);
        }

        entity.stats.AddProficiencyXP(entity.inventory.firearm, entity.stats.Dexterity);
    }

    public void InstatiateThrowingEffect(Coord destination, float spdMul)
    {
        GameObject lo = GameObject.Instantiate(World.poolManager.throwEffect, entity.gameObject.transform.position, Quaternion.identity);
        lo.GetComponent<LerpPos>().Init(destination, spdMul);
    }

    bool FirearmMiss(Coord targetPos, int iteration)
    {
        float denom = (float)entity.stats.proficiencies.Firearm.level + 2 + entity.stats.Accuracy / 2 - entity.inventory.firearm.Accuracy;
        denom = Mathf.Clamp(denom, 0.05f, 100.0f);
        float missChance = 1f / denom * 100f, maxMiss = RNG.Next(100);

        if (entity.myPos.DistanceTo(targetPos) >= entity.stats.FirearmRange)
        {
            missChance *= 1.5f;
        }

        if (entity.inventory.firearm.Accuracy < 0)
        {
            missChance *= 2f;
        }

        if (!entity.inventory.firearm.HasProp(ItemProperty.Burst))
        {
            maxMiss -= 3f * iteration;
        }

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
                {
                    continue;
                }

                if (World.tileMap.PassThroughableTile(destination.x + x, destination.y + y) 
                    && destination.x != entity.posX && destination.y != entity.posY)
                {
                    nearbyCoords.Add(new Coord(destination.x + x, destination.y + y));
                }
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
        entity.UnSetCell();

        if (entity.isPlayer)
        {
            World.userInterface.PlayerDied((MyStats.lastHit == null) ? "<color=yellow>???</color>" : MyStats.lastHit.name);

            if (World.difficulty.Level == Difficulty.DiffLevel.Scavenger && MyInventory.items.Count > 0)
            {
                int numToDrop = RNG.Next(3);

                for (int i = 0; i < numToDrop; i++)
                {
                    Item iToDrop = MyInventory.items.GetRandom();

                    if (iToDrop.HasProp(ItemProperty.Quest_Item))
                    {
                        continue;
                    }

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

            if (MyStats.lastHit != null && (MyStats.lastHit.isPlayer || MyStats.lastHit.AI.isFollower()))
            {
                if (!entity.AI.npcBase.HasFlag(NPC_Flags.NO_XP))
                {
                    int xp = (MyStats.Strength + MyStats.Dexterity + MyStats.Intelligence + MyStats.Endurance * 2) / 2 + 1;
                    xp += entity.AI.npcBase.bonusXP;
                    ObjectManager.playerEntity.stats.GainExperience(xp);
                }
            }

            World.objectManager.DemolishNPC(entity, entity.AI.npcBase);
            GameObject.Destroy(entity.gameObject);
        }
    }

    public void Remove()
    {
        entity.canAct = false;
        MyStats.dead = true;
        entity.UnSetCell();

        World.objectManager.DemolishNPC(entity, entity.AI.npcBase);
        GameObject.Destroy(entity.gameObject);
    }
}

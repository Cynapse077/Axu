using System.Collections.Generic;
using UnityEngine;

[MoonSharp.Interpreter.MoonSharpUserData]
public class CombatComponent {

	public Item itemForThrowing;
	public Entity lastTarget;

	Entity entity;

	Stats stats {
		get { return entity.stats; }
	}
	Inventory inventory {
		get { return entity.inventory; }
	}
	Body body {
		get { return entity.body; }
	}

	public CombatComponent(Entity e) {
		entity = e;
	}

	public void Attack(Stats target, bool freeAction = false, BodyPart targetPart = null, int accPenalty = 0) {
		if (entity.isPlayer && target.GetComponent<BaseAI>().npcBase.HasFlag(NPC_Flags.Follower))
			return;

		lastTarget = target.entity;

		//Main weapon
		PerformAttack(target, body.MainHand, targetPart, accPenalty);

		//Attack with all other arms.
		List<BodyPart.Hand> hands = body.Hands;

		for (int i = 0; i < hands.Count; i++) {
			if (hands[i] != body.MainHand && hands[i].isAttached && SeedManager.combatRandom.Next(80) <= (stats.Dexterity + 1))
				PerformAttack(target, hands[i]);
		}

        //TODO: Add extra attacks for bites, kicks, headbutts, etc.

		if (!freeAction)
			entity.EndTurn(0.1f, AttackAPCost());
	}
 
	bool PerformAttack(Stats target, BodyPart.Hand hand, BodyPart targetPart = null, int accPenalty = 0) {
		if (hand == null || target == null || hand.equippedItem == null)
			return false;

		Item wep = hand.equippedItem;
		bool twoHandPenalty = wep.HasProp(ItemProperty.Two_Handed) && inventory.TwoHandPenalty(hand);
		HashSet<DamageTypes> dt = wep.damageTypes;
		WeaponProficiency prof = stats.CheckProficiencies(wep);
		int missChance = stats.MissChance(wep) + accPenalty;
		int damage = wep.CalculateDamage(stats.Strength, prof.level);

		if (stats.HasEffect("Topple"))
			missChance += 5;
		if (twoHandPenalty)
			missChance += 5;

		//firearms have reduced physical damage.
		if (wep.HasProp(ItemProperty.Ranged)) {
			dt = new HashSet<DamageTypes>() { DamageTypes.Blunt };
			Damage d = new Damage(1, 3, 0);
			damage = d.Roll() + (stats.Strength / 2 - 1) + stats.proficiencies.Misc.level;
		}

		if (SeedManager.combatRandom.Next(100) < missChance) {
			target.Miss(entity, wep);
			return false;
		}

		if (targetPart == null)
			targetPart = target.entity.body.TargetableBodyParts().GetRandom(SeedManager.combatRandom);

		if (target.TakeDamage(wep, damage, dt, entity, wep.AttackCrits((prof.level - 1 + stats.Accuracy + wep.Accuracy) / 2), targetPart)) {
			if (entity.isPlayer) {
				if (wep.attackType == Item.AttackType.Bash || wep.attackType == Item.AttackType.Sweep)
					World.soundManager.AttackSound2();
				else
					World.soundManager.AttackSound();

				//if it's a physical attack from a firearm, use misc prof
				if (wep.itemType == Proficiencies.Firearm || wep.itemType == Proficiencies.Armor || wep.itemType == Proficiencies.Butchery)
					stats.AddProficiencyXP(stats.proficiencies.Misc, stats.Intelligence);
				else
					stats.AddProficiencyXP(wep, stats.Intelligence);

			} else
				ApplyNPCEffects(target, damage);

			ApplyWeaponEffects(target, wep);
		}

		if (entity.isPlayer) {
			stats.AddProficiencyXP(wep, stats.Intelligence);

			if (hand.arm != null)
				body.TrainLimb(hand.arm);
		}

		return true;
	}

	void ApplyWeaponEffects(Stats target, Item wep) {
		if (wep.HasProp(ItemProperty.OnAttack_Radiation)) {
			if (SeedManager.combatRandom.Next(100) < 5)
				stats.Radiate(1);
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
		if (wep.HasProp(ItemProperty.Knockback) && SeedManager.combatRandom.Next(100) <= 5) {
			Entity otherEntity = target.entity;

			if (otherEntity != null && otherEntity.myPos.DistanceTo(entity.myPos) < 2f)
				otherEntity.ForceMove(otherEntity.posX - entity.posX, otherEntity.posY - entity.posY, stats.Strength - 1);
		}
	}

	void ApplyNPCEffects(Stats target, int damage) {
		entity.AI.OnHitEffects(target, damage);
	}

	public int AttackAPCost() {
		int timeCost = body.MainHand.equippedItem.GetAttackAPCost() + stats.AttackDelay;

		if (stats.HasEffect("Topple"))
			timeCost += 7;
		if (inventory.TwoHandPenalty(body.MainHand))
			timeCost += 4;

		return timeCost;
	}

	//Makes the current item to throw this item. 
	public void SelectItemToThrow(Item i) {
		itemForThrowing = i;
	}
	//Actually does the throw baloney
	public void ThrowItem(Coord destination, Explosive damageScript) {
		if (itemForThrowing == null || destination == null || damageScript == null)
			return;

		bool miss = (SeedManager.combatRandom.Next(100) > 40 + stats.proficiencies.Throwing.level + stats.Accuracy);

		if (miss) {
            Coord newPos = AdjacentCoord(destination);

            if (newPos != null)
                destination = newPos;
        }
            

		//Instantiate
		entity.InstatiateThrowingEffect(destination);

		if (itemForThrowing.HasProp(ItemProperty.Explosive)) {
			damageScript.destroy = true;
			damageScript.damage = SeedManager.combatRandom.Next(12, 31);
			damageScript.nameOfDamage = "Explosion";
		} else {
			int throwingLevel = (entity.isPlayer) ? stats.proficiencies.Throwing.level : (World.DangerLevel() / 10) + 1;
			damageScript.damage = itemForThrowing.ThrownDamage(throwingLevel, stats.Dexterity);
			damageScript.nameOfDamage = itemForThrowing.Name;
		}

		damageScript.localPosition = destination;
		inventory.ThrowItem(destination, itemForThrowing, damageScript);
		itemForThrowing = null;

		if (entity.isPlayer)
			stats.AddProficiencyXP(stats.proficiencies.Throwing, stats.Intelligence);

		entity.EndTurn(0.3f);
	}

    Coord AdjacentCoord(Coord destination) {
        List<Coord> nearbyCoords = new List<Coord>();

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (Mathf.Abs(x) + Mathf.Abs(y) >= 2 || destination.x + x < 0 || destination.x >= Manager.localMapSize.x 
                    || destination.y + y < 0 || destination.y + y >= Manager.localMapSize.y)
                    continue;

                if (World.tileMap.PassThroughableTile(destination.x + x, destination.y + y) && destination.x != entity.posX && destination.y != entity.posY)
                    nearbyCoords.Add(new Coord(destination.x + x, destination.y + y));
            }
        }

        return nearbyCoords.GetRandom(SeedManager.combatRandom);
    }

	public int CheckWepType() {
        switch (body.MainHand.equippedItem.attackType) {
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

	public void Die() {
		entity.canAct = false;
		stats.dead = true;
		entity.CreateBloodstain(true);

		if (entity.cell != null)
			entity.cell.UnSetEntity(entity);

		if (entity.isPlayer) {
			World.userInterface.PlayerDied(((stats.lastHit == null) ? "<color=yellow>Hubris</color>" : stats.lastHit.name));

			if (World.difficulty.Level == Difficulty.DiffLevel.Scavenger) {
				int numToDrop = SeedManager.combatRandom.Next(0, 3);

				for (int i = 0; i < numToDrop; i++) {
					if (inventory.items.Count > 0) {
						Item iToDrop = inventory.items.GetRandom();

						if (iToDrop.HasProp(ItemProperty.Quest_Item))
							continue;

						inventory.Drop(iToDrop);
					}
				}
			}
		} else {
			inventory.DropAll();

			if (stats.lastHit != null) {
				//Add relevant XP to the character that struck this NPC last
                //Maybe should add all XP to player unless it's a friendly NPC.
				stats.lastHit.stats.GainExperience((stats.Strength + stats.Dexterity + stats.Intelligence + stats.Endurance * 2) / 2 + 1);
			}

			World.objectManager.DemolishNPC(entity, entity.AI.npcBase);
			GameObject.Destroy(entity.gameObject);
		}
	}

	public void Remove() {
		entity.canAct = false;
		stats.dead = true;

		if (entity.cell != null)
			entity.cell.UnSetEntity(entity);

		World.objectManager.DemolishNPC(entity, entity.AI.npcBase);
		GameObject.Destroy(entity.gameObject);
	}
}

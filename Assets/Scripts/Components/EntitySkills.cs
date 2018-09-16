using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class EntitySkills : MonoBehaviour {
	public List<Skill> abilities;
	public List<string> innateAbilities;
	public Entity entity;

	int grappleLevel {
		get {
			int lvl = 0;

			if (abilities.Find(x => x.ID == "grapple") != null)
				lvl = abilities.Find(x => x.ID == "grapple").level;

			return lvl;
		}
	}

	void Start() {
		entity = GetComponent<Entity>();
		innateAbilities = new List<string>();

		if (!entity.isPlayer) {
			if (entity.AI == null)
				entity.AI = GetComponent<BaseAI>();
			
			List<KeyValuePair<string, int>> sks = EntityList.GetBlueprintByID(entity.AI.npcBase.ID).skills;

			for (int i = 0; i < sks.Count; i++) {
				Skill s = SkillList.GetSkillByID(sks[i].Key);
				s.level = sks[i].Value;
				AddSkill(s);
			}
		} else {
			for (int i = 0; i < Manager.playerBuilder.skills.Count; i++) {
				AddSkill(new Skill(Manager.playerBuilder.skills[i]));
			}
		}
	}

	void OnDisable() {
		for (int i = 0; i < abilities.Count; i++) {
			abilities[i].UnregisterCallbacks();
		}
	}

	public bool HasAndCanUseSkill(string id) {
		Skill s = abilities.Find(x => x.ID == id);
		return (s != null && s.cooldown <= 0 && CanUseSkill(s.staminaCost));
	}

	public void AddSkill(Skill s) {
        if (s == null) {
            Debug.LogError("Null Ability!");
            return;
        }

        if (abilities.Find(x => x.ID == s.ID) == null) {
			abilities.Add(s);
			s.Init();

			if (!s.fromItem)
				innateAbilities.Add(s.ID);
		}
	}

    public void AddSkill_Item(Skill s) {
        if (s == null) {
            Debug.LogError("Null Ability!");
            return;
        }

        if (abilities.Find(x => x.ID == s.ID) == null) {
            abilities.Add(s);
            s.Init();

            if (!s.fromItem)
                innateAbilities.Add(s.ID);
        } else {
            Skill skill = abilities.Find(x => x.ID == s.ID);

            if (skill.level < Skill.maxLvl && skill.CanLevelUp) {
                skill.level++;
                skill.XP = 0;
                CombatLog.NameMessage("Increase_Skill", skill.Name);
            }
        }
    }

	public void RemoveSkill(string id) {
		if (abilities.Find(x => x.ID == id) != null && !innateAbilities.Contains(id)) {
			Skill s = abilities.Find(x => x.ID == id);
			s.UnregisterCallbacks();

            if (innateAbilities.Contains(s.ID))
                innateAbilities.Remove(s.ID);

			abilities.Remove(s);
		}
	}

	public void Grapple_GrabPart(BodyPart targetLimb, Skill grapp) {
        if (entity.body.GrippableLimbs().Count > 0) {
            entity.body.GrippableLimbs().GetRandom().GrabPart(targetLimb);

            if (entity.isPlayer && grapp != null)
                grapp.AddXP(entity.stats.Intelligence / 4);
        } else
            Alert.CustomAlert_WithTitle("No Grippable Limbs", "You have no parts to grab with!");
	}

	public void Grapple_TakeDown(Stats target, string limbName, Skill grapp) {
		int skill = entity.stats.Strength - 1;
        skill += (entity.isPlayer ? grappleLevel : SeedManager.combatRandom.Next(-1, 3));

		if (SeedManager.combatRandom.Next(30) <= entity.stats.Strength + skill * 2) {
			string message = LocalizationManager.GetContent("Gr_TakeDown");
			message = message.Replace("[ATTACKER]", ObjectManager.player.gameObject.name);
			message = message.Replace("[DEFENDER]", target.gameObject.name);
			message = message.Replace("[DEFENDER_LIMB]", limbName);
			CombatLog.NewMessage(message);

			entity.body.ReleaseAllGrips(true);
			target.AddStatusEffect("Topple", SeedManager.combatRandom.Next(3, 8));
			target.IndirectAttack(SeedManager.combatRandom.Next(1, 6), DamageTypes.Blunt, entity, LocalizationManager.GetContent("Takedown_Name"), true, false, false);
		}


		if (entity.isPlayer) {
			target.entity.AI.BecomeHostile();
			entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            if (grapp != null)
			    grapp.AddXP(entity.stats.Intelligence);
		}

		entity.EndTurn(0.02f, 15);
	}

	public void Grapple_Shove(Entity target, Skill grapp) {
		int skill = entity.stats.Strength;

		if (entity.isPlayer)
			skill += grappleLevel;

		if (SeedManager.combatRandom.Next(20) <= skill) {
			string message = LocalizationManager.GetContent("Gr_Shove");
			message = message.Replace("[ATTACKER]", gameObject.name);
			message = message.Replace("[DEFENDER]", target.gameObject.name);
			CombatLog.NewMessage(message);

			target.ForceMove(target.posX - entity.posX, target.posY - entity.posY, entity.stats.Strength);
			entity.body.ReleaseAllGrips(true);
		}

		if (entity.isPlayer) {
			target.AI.BecomeHostile();
			entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            if (grapp != null)
			    grapp.AddXP(entity.stats.Intelligence);
		}

		entity.stats.UseStamina(2);
		entity.EndTurn(0.02f, 10);
	}

	public void Grapple_Strangle(Stats target, Skill grapp) {
		int skill = entity.stats.Strength;

		if (entity.isPlayer)
			skill += grappleLevel;
		
		if (SeedManager.combatRandom.Next(15) <= skill) {
			target.AddStatusEffect("Unconscious", SeedManager.combatRandom.Next(5, 11));
			string message = LocalizationManager.GetContent("Gr_Strangle");
			message = message.Replace("[ATTACKER]", ObjectManager.player.gameObject.name);
			message = message.Replace("[DEFENDER]", target.gameObject.name);
			CombatLog.NewMessage(message);
		} else {
			string message = LocalizationManager.GetContent("Gr_Strangle_Fail");
			message = message.Replace("[ATTACKER]", ObjectManager.player.gameObject.name);
			message = message.Replace("[DEFENDER]", target.gameObject.name);
			CombatLog.NewMessage(message);
		} 
				
		if (entity.isPlayer) {
			target.entity.AI.BecomeHostile();

            if (grapp != null)
                grapp.AddXP(entity.stats.Intelligence);
        }

		entity.stats.UseStamina(2);
		entity.EndTurn(0.02f, 10);
	}

	public void Grapple_Pull(BodyPart.Grip grip, Skill grapp) {
		Body otherBody = grip.HeldBody;
		BodyPart targetLimb = grip.HeldPart;
		string message = "";

		int pullStrength = entity.stats.Strength + grappleLevel;

		if (targetLimb.severable && targetLimb.isAttached && SeedManager.combatRandom.Next(100) <= pullStrength) {
			otherBody.RemoveLimb(targetLimb);
			grip.Release();
			otherBody.entity.stats.AddStatusEffect("Stun", 4);

			message = LocalizationManager.GetContent("Gr_Pull_Success");
		} else {
			message = LocalizationManager.GetContent("Gr_Pull_Fail");
		}

		message = message.Replace("[ATTACKER]", ObjectManager.player.name);
		message = message.Replace("[DEFENDER]", otherBody.gameObject.name);
		message = message.Replace("[DEFENDER_LIMB]", targetLimb.displayName);
		CombatLog.NewMessage(message);

		if (entity.isPlayer) {
			otherBody.entity.AI.BecomeHostile();
			entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            if (grapp != null)
                grapp.AddXP(entity.stats.Intelligence);
        }

		entity.stats.UseStamina(2);
		entity.EndTurn(0.02f, 10);
	}

	public void CallForHelp() {
		if (entity.AI.InSightOfPlayer()) {
			Instantiate(World.poolManager.roarEffect, transform.position, Quaternion.identity);
			CombatLog.NameMessage("Message_Call", gameObject.name);
		}

		foreach (Entity e in World.objectManager.onScreenNPCObjects) {
            e.AI.AnswerCallForHelp(entity.AI);
		}

		UseStaminaIfNotPlayer(2);
	}
		
	//Random teleport -- Enemies only
	public void Teleport() {
		entity.BeamDown();
		entity.cell.UnSetEntity(entity);
		entity.myPos = World.tileMap.InSightCoords().GetRandom(SeedManager.combatRandom);

		World.soundManager.TeleportSound();
		entity.ForcePosition();
		entity.SetCell();
		CombatLog.NameMessage("Message_Teleport", gameObject.name);

		if (entity.isPlayer) {
			World.tileMap.SoftRebuild();

			foreach (Entity e in World.objectManager.onScreenNPCObjects) {
				if (SeedManager.combatRandom.CoinFlip())
					e.AI.ForgetPlayer();
			}
		}
	}

	public bool PassiveDisarm(Entity attacker) {
		Entity ent = attacker;

		if (!ent)
			return false;

		Inventory otherInventory = ent.inventory;

		if (otherInventory == null || !ent.body.MainHand.equippedItem.lootable)
			return false;

		CombatLog.CombatMessage("Message_Disarm", attacker.name, gameObject.name, entity.isPlayer);
		otherInventory.Disarm();

		if (entity.isPlayer)
			entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

		return true;
	}

	//Asks if the cost by int is okay to use.
	public bool CanUseSkill(int cost) {
		return (entity.stats.stamina >= cost);
	}

	void UseStaminaIfNotPlayer(int cost) {
		if (!entity.isPlayer)
			entity.stats.UseStamina(cost);
	}

	bool TargetAvailable(Coord direction) {
		return (World.tileMap.WalkableTile(entity.posX + direction.x, entity.posY + direction.y) && World.tileMap.GetCellAt(entity.myPos + direction).entity != null);
	}
}

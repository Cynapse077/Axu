using UnityEngine;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class EntitySkills : MonoBehaviour
{
    public List<Skill> abilities;
    public Entity entity;

    int grappleLevel
    {
        get { return entity.stats.proficiencies.MartialArts.level + 1; }
    }

    void Start()
    {
        entity = GetComponent<Entity>();

        if (!entity.isPlayer)
        {
            KeyValuePair<string, int>[] sks = EntityList.GetBlueprintByID(entity.AI.npcBase.ID).skills;

            for (int i = 0; i < sks.Length; i++)
            {
                Skill s = SkillList.GetSkillByID(sks[i].Key);
                s.level = sks[i].Value;
                AddSkill(s, Skill.AbilityOrigin.Book);
            }
        }
        else
        {
            for (int i = 0; i < Manager.playerBuilder.skills.Count; i++)
            {
                AddSkill(new Skill(Manager.playerBuilder.skills[i]), Manager.playerBuilder.skills[i].origin);
            }
        }
    }

    void OnDisable()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            abilities[i].UnregisterCallbacks();
        }
    }

    public bool HasAndCanUseSkill(string id)
    {
        Skill s = abilities.Find(x => x.ID == id);
        return (s != null && s.cooldown <= 0 && CanUseSkill(s.staminaCost));
    }

    public void AddSkill(Skill s, Skill.AbilityOrigin origin)
    {
        if (s == null)
        {
            return;
        }

        if (!FlagsHelper.IsSet(s.origin, origin))
        {
            s.SetFlag(origin);
        }


        if (abilities.Find(x => x.ID == s.ID) == null)
        {
            abilities.Add(s);
            s.Init();
        }
        else
        {
            abilities.Find(x => x.ID == s.ID).SetFlag(origin);
        }
    }

    public void RemoveSkill(string id, Skill.AbilityOrigin origin)
    {
        Skill s = abilities.Find(x => x.ID == id);

        if (s != null)
        {
            s.RemoveFlag(origin);

            if (s.origin == Skill.AbilityOrigin.None)
            {
                s.UnregisterCallbacks();
                abilities.Remove(s);
            }
        }
    }

    public void Grapple_GrabPart(BodyPart targetLimb)
    {
        if (entity.body.GrippableLimbs().Count > 0)
        {
            entity.body.GrippableLimbs().GetRandom().GrabPart(targetLimb);

            if (entity.isPlayer)
                entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence + 1);
        }
        else
            Alert.CustomAlert_WithTitle("No Grippable Limbs", "You have no parts to grab with!");
    }

    public void Grapple_TakeDown(Stats target, string limbName)
    {
        int skill = entity.stats.Strength - 1;
        skill += (entity.isPlayer ? grappleLevel : SeedManager.combatRandom.Next(-1, 3));

        if (SeedManager.combatRandom.Next(50) <= entity.stats.Strength + skill * 2)
        {
            string message = LocalizationManager.GetContent("Gr_TakeDown");
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.entity.MyName);
            message = message.Replace("[DEFENDER_LIMB]", limbName);
            CombatLog.NewMessage(message);

            entity.body.ReleaseAllGrips(true);
            target.AddStatusEffect("Topple", SeedManager.combatRandom.Next(3, 8));
            target.IndirectAttack(SeedManager.combatRandom.Next(1, 6), DamageTypes.Blunt, entity, LocalizationManager.GetContent("Takedown_Name"), true, false, false);
        }
        else
        {
            string message = LocalizationManager.GetContent("Gr_TakeDown_Fail");
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.entity.MyName);
            CombatLog.NewMessage(message);
        }

        if (!target.entity.isPlayer)
        {
            target.entity.AI.SetTarget(entity);
        }

        if (entity.isPlayer)
        {
            target.entity.AI.BecomeHostile();
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence / 2.0);
        }

        entity.EndTurn(0.02f, 10);
    }

    public void Grapple_Shove(Entity target)
    {
        int skill = entity.stats.Strength;

        if (entity.isPlayer)
        {
            skill += grappleLevel;
        }

        if (SeedManager.combatRandom.Next(20) <= skill)
        {
            string message = LocalizationManager.GetContent("Gr_Shove");
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.MyName);
            CombatLog.NewMessage(message);

            target.ForceMove(target.posX - entity.posX, target.posY - entity.posY, entity.stats.Strength);
            entity.body.ReleaseAllGrips(true);

            if (SeedManager.localRandom.Next(100) < 1)
            {
                target.stats.AddStatusEffect("Topple", 2);
            }
        }
        else
        {
            string message = LocalizationManager.GetContent("Gr_Shove_Fail");
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.MyName);
            CombatLog.NewMessage(message);
        }

        if (!target.isPlayer)
        {
            target.AI.SetTarget(entity);
        }

        if (entity.isPlayer)
        {
            target.AI.BecomeHostile();
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence + 1);
        }

        entity.EndTurn(0.02f, 10);
    }

    public void Grapple_Strangle(Stats target)
    {
        int skill = entity.stats.Strength;

        if (entity.isPlayer)
            skill += grappleLevel;

        if (SeedManager.combatRandom.Next(30) <= skill)
        {
            target.AddStatusEffect("Unconscious", SeedManager.combatRandom.Next(5, 11));
            string message = LocalizationManager.GetContent("Gr_Strangle");
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.entity.MyName);
            CombatLog.NewMessage(message);
        }
        else
        {
            string message = LocalizationManager.GetContent("Gr_Strangle_Fail");
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.entity.MyName);
            CombatLog.NewMessage(message);
        }

        if (!target.entity.isPlayer)
            target.entity.AI.SetTarget(entity);

        if (entity.isPlayer)
        {
            target.entity.AI.BecomeHostile();
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence + 1);
        }

        entity.EndTurn(0.02f, 10);
    }

    public void Grapple_Pull(BodyPart.Grip grip)
    {
        Body otherBody = grip.HeldBody;
        BodyPart targetLimb = grip.heldPart;
        string message = "";

        int pullStrength = entity.stats.Strength + grappleLevel;

        if (targetLimb.severable && targetLimb.isAttached && SeedManager.combatRandom.Next(100) <= pullStrength)
        {
            otherBody.RemoveLimb(targetLimb);
            grip.Release();
            otherBody.entity.stats.AddStatusEffect("Stun", 3);

            message = LocalizationManager.GetContent("Gr_Pull_Success");
        }
        else
        {
            message = LocalizationManager.GetContent("Gr_Pull_Fail");
        }

        message = message.Replace("[ATTACKER]", entity.MyName);
        message = message.Replace("[DEFENDER]", otherBody.entity.MyName);
        message = message.Replace("[DEFENDER_LIMB]", targetLimb.displayName);
        CombatLog.NewMessage(message);

        if (!otherBody.entity.isPlayer)
            otherBody.entity.AI.SetTarget(entity);

        if (entity.isPlayer)
        {
            otherBody.entity.AI.BecomeHostile();
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence + 1);
        }

        entity.EndTurn(0.02f, 10);
    }

    public void Grapple_Pressure(BodyPart.Grip grip)
    {
        int pullStrength = entity.stats.Strength + grappleLevel;

        if (SeedManager.combatRandom.Next(100) < pullStrength)
        {
            grip.heldPart.InflictPhysicalWound();
        }

        if (!grip.HeldBody.entity.isPlayer)
            grip.HeldBody.entity.AI.SetTarget(entity);

        if (entity.isPlayer)
        {
            grip.HeldBody.entity.AI.BecomeHostile();
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence + 1);
        }
    }

    public void CallForHelp()
    {
        if (entity.AI.InSightOfPlayer())
        {
            Instantiate(World.poolManager.roarEffect, transform.position, Quaternion.identity);
            CombatLog.NameMessage("Message_Call", gameObject.name);
        }

        foreach (Entity e in World.objectManager.onScreenNPCObjects)
        {
            e.AI.AnswerCallForHelp(entity.AI);
        }

        UseStaminaIfNotPlayer(2);
    }

    //Random teleport -- Enemies only
    public void Teleport()
    {
        entity.BeamDown();
        entity.cell.UnSetEntity(entity);
        entity.myPos = World.tileMap.InSightCoords().GetRandom(SeedManager.combatRandom);

        World.soundManager.TeleportSound();
        entity.ForcePosition();
        entity.SetCell();
        CombatLog.NameMessage("Message_Teleport", gameObject.name);

        if (entity.isPlayer)
        {
            World.tileMap.SoftRebuild();

            foreach (Entity e in World.objectManager.onScreenNPCObjects)
            {
                if (SeedManager.combatRandom.CoinFlip())
                    e.AI.ForgetPlayer();
            }
        }
    }

    public bool PassiveDisarm(Entity attacker)
    {
        Entity ent = attacker;

        if (!ent)
            return false;

        Inventory otherInventory = ent.inventory;

        if (otherInventory == null || !ent.body.MainHand.EquippedItem.lootable)
            return false;

        CombatLog.CombatMessage("Message_Disarm", attacker.name, gameObject.name, entity.isPlayer);
        otherInventory.Disarm();

        if (entity.isPlayer)
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

        return true;
    }

    //Asks if the cost by int is okay to use.
    public bool CanUseSkill(int cost)
    {
        return (entity.stats.stamina >= cost);
    }

    void UseStaminaIfNotPlayer(int cost)
    {
        if (!entity.isPlayer)
            entity.stats.UseStamina(cost);
    }

    bool TargetAvailable(Coord direction)
    {
        return (World.tileMap.WalkableTile(entity.posX + direction.x, entity.posY + direction.y) && World.tileMap.GetCellAt(entity.myPos + direction).entity != null);
    }
}

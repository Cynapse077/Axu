using UnityEngine;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class EntitySkills : MonoBehaviour
{
    public List<Ability> abilities;
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
            if (GameData.TryGet(entity.AI.npcBase.ID, out NPC_Blueprint bp))
            {
                KeyValuePair<string, int>[] sks = bp.skills;

                for (int i = 0; i < sks.Length; i++)
                {
                    if (GameData.TryGet(sks[i].Key, out Ability ab))
                    {
                        var ability = ab.Clone();
                        ability.level = sks[i].Value;
                        AddSkill(ability, Ability.AbilityOrigin.Natrual);
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < Manager.playerBuilder.abilities.Count; i++)
            {
                var ab = new Ability(Manager.playerBuilder.abilities[i]);
                for (int j = 0; j < Manager.playerBuilder.abilities[i].origin.Count; j++)
                {
                    ab.SetFlag(Manager.playerBuilder.abilities[i].origin[j]);
                }

                AddSkill(ab, Ability.AbilityOrigin.Natrual);
            }
        }
    }

    private void OnDisable()
    {
        RemoveCallbacks();
    }

    public void RemoveCallbacks()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            abilities[i].UnregisterCallbacks();
        }
    }

    public bool HasAndCanUseSkill(string id)
    {
        Ability s = abilities.Find(x => x.ID == id);
        return (s != null && s.cooldown <= 0 && CanUseSkill(s.staminaCost));
    }

    public void AddSkill(Ability s, Ability.AbilityOrigin origin)
    {
        if (s == null)
        {
            return;
        }

        if (abilities.Find(x => x.ID == s.ID) == null)
        {
            s.SetFlag(origin);
            abilities.Add(s);
            s.Init();
        }
        else
        {
            abilities.Find(x => x.ID == s.ID).SetFlag(origin);
        }

        ResetHotkeyManager();
    }

    void ResetHotkeyManager()
    {
        if (entity.isPlayer)
        {
            HotkeyManager hkm = GameObject.FindObjectOfType<HotkeyManager>();
            hkm.Initialize();

            for (int i = 0; i < abilities.Count; i++)
            {
                int index = i;
                hkm.AssignAction(abilities[i], i, () => { abilities[index].Cast(ObjectManager.playerEntity); });
            }
        }
    }

    public void RemoveSkill(string id, Ability.AbilityOrigin origin)
    {
        Ability s = abilities.Find(x => x.ID == id);

        if (s != null)
        {
            s.RemoveFlag(origin);

            if (s.origin.Count == 0)
            {
                s.UnregisterCallbacks();
                abilities.Remove(s);
            }
        }

        ResetHotkeyManager();
    }

    public void Grapple_GrabPart(BodyPart targetLimb)
    {
        var grippableLimbs = entity.body.GrippableLimbs();
        if (grippableLimbs.Count > 0)
        {
            grippableLimbs.GetRandom().GrabPart(targetLimb);

            if (entity.isPlayer)
            {
                entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence / 2);
            }
        }
        else
        {
            Alert.CustomAlert_WithTitle("No Grippable Limbs", "You have no available parts to grab with! Make sure your off hands are empty.");
        }
    }

    public void Grapple_TakeDown(Stats target, string limbName)
    {
        int chance = entity.stats.Strength + (entity.isPlayer ? grappleLevel : SeedManager.combatRandom.Next(-1, 3));

        if (target.HasEffect("OffBalance"))
        {
            chance += 3;
        }

        bool success = RNG.Next(40) <= chance;

        if (!target.entity.isPlayer && target.entity.AI.npcBase.HasFlag(NPC_Flags.No_TakeDown))
        {
            success = false;
        }

        if (success)
        {
            string message = LocalizationManager.GetContent("Gr_TakeDown");
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.entity.MyName);
            message = message.Replace("[DEFENDER_LIMB]", limbName);
            CombatLog.NewMessage(message);

            entity.body.ReleaseAllGrips(true);
            target.AddStatusEffect("Topple", RNG.Next(3, 8));
            target.IndirectAttack(RNG.Next(1, 6), DamageTypes.Blunt, entity, "Takedown_Name".Localize(), true, false, false);
        }
        else
        {
            string message = LocalizationManager.GetContent("Gr_TakeDown_Fail");
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.entity.MyName);
            CombatLog.NewMessage(message);

            if (RNG.Chance(10))
            {
                entity.stats.AddStatusEffect("OffBalance", RNG.Next(1, 4));
            }
        }

        if (!target.entity.isPlayer)
        {
            target.entity.AI.SetTarget(entity);
        }

        if (entity.isPlayer)
        {
            target.entity.AI.BecomeHostile();
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence / 2);
        }

        entity.EndTurn(0.02f, 10);
    }

    public void Grapple_Shove(BodyPart.Grip grip)
    {
        int chance = entity.stats.Strength;
        Entity target = grip.HeldBody.entity;

        if (target == null)
        {
            return;
        }

        if (entity.isPlayer)
        {
            chance += grappleLevel;

            if (!target.AI.hasSeenPlayer)
            {
                chance += 3;
            }
        }

        if (target.stats.HasEffect("OffBalance"))
        {
            chance += 4;
        }

        if (RNG.Next(20 + target.stats.Strength) <= chance)
        {
            string message = "Gr_Shove".Localize();
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.MyName);
            CombatLog.NewMessage(message);

            target.ForceMove(target.posX - entity.posX, target.posY - entity.posY, entity.stats.Strength);
            entity.body.ReleaseAllGrips(true);

            if (RNG.Chance(1))
            {
                target.stats.AddStatusEffect("Topple", RNG.Next(2, 4));
            }
        }
        else
        {
            string message = LocalizationManager.GetContent("Gr_Shove_Fail");
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.MyName);
            CombatLog.NewMessage(message);

            if (RNG.OneIn(10))
            {
                target.stats.AddStatusEffect("OffBalance", RNG.Next(1, 4));
            }
            else if (RNG.Chance(6))
            {
                entity.stats.AddStatusEffect("OffBalance", RNG.Next(1, 4));
            }
        }

        if (!target.isPlayer)
        {
            target.AI.SetTarget(entity);
        }

        if (entity.isPlayer)
        {
            target.AI.BecomeHostile();
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence / 2);
        }

        entity.EndTurn(0.02f, 10);
    }

    public void Grapple_Strangle(Stats target)
    {
        int skill = entity.stats.Strength;

        if (entity.isPlayer)
        {
            skill += grappleLevel;
        }

        bool success = SeedManager.combatRandom.Next(30) <= skill;

        if (!target.entity.isPlayer && target.entity.AI.npcBase.HasFlag(NPC_Flags.No_Strangle))
        {
            success = false;
        }

        if (success)
        {
            string message = "Gr_Strangle".Localize();
            message = message.Replace("[ATTACKER]", entity.MyName);
            message = message.Replace("[DEFENDER]", target.entity.MyName);
            CombatLog.NewMessage(message);

            target.AddStatusEffect("Strangle", entity.stats.Strength * 2);
            target.SimpleDamage(RNG.Next(1, 4));
        }
        else
        {
            string message = "Gr_Strangle_Fail".Localize();
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

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence / 2);
        }

        entity.EndTurn(0.02f, 10);
    }

    public void Grapple_Pull(BodyPart.Grip grip)
    {
        Body otherBody = grip.HeldBody;
        BodyPart targetLimb = grip.heldPart;
        string message = "";
        int pullStrength = entity.stats.Strength + (grappleLevel * 2);
        bool success = SeedManager.combatRandom.Next(grip.heldPart.myBody.entity.stats.Strength + 90) <= pullStrength;

        if (success)
        {
            if (targetLimb.Severable && entity.stats.Strength > otherBody.entity.stats.Strength * 2)
            {
                otherBody.RemoveLimb(targetLimb);
                grip.Release();
                otherBody.entity.stats.AddStatusEffect("Stun", 3);

                message = "Gr_Pull_Success".Localize();
            }
            else
            {
                targetLimb.WoundMe(new HashSet<DamageTypes>() { DamageTypes.Pull });
                otherBody.entity.stats.SimpleDamage(SeedManager.combatRandom.Next(1, 5));
                otherBody.entity.stats.AddStatusEffect("OffBalance", RNG.Next(2, 5));
            }
        }
        else
        {
            message = "Gr_Pull_Fail".Localize();

            if (RNG.Chance(5))
            {
                entity.stats.AddStatusEffect("OffBalance", RNG.Next(1, 4));
            }
        }

        message = message.Replace("[ATTACKER]", entity.MyName);
        message = message.Replace("[DEFENDER]", otherBody.entity.MyName);
        message = message.Replace("[DEFENDER_LIMB]", targetLimb.displayName);
        CombatLog.NewMessage(message);

        if (!otherBody.entity.isPlayer)
        {
            otherBody.entity.AI.SetTarget(entity);
        }

        if (entity.isPlayer)
        {
            otherBody.entity.AI.BecomeHostile();
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence / 2);
        }

        entity.EndTurn(0.02f, 10);
    }

    public void Grapple_Pressure(BodyPart.Grip grip)
    {
        int pressure = entity.stats.Strength + grappleLevel;

        if (RNG.Next(grip.heldPart.armor + 90) < pressure)
        {
            grip.heldPart.InflictPhysicalWound();
        }
        else
        {
            CombatLog.NewMessage(entity.MyName + " fails to apply pressure to " + grip.HeldBody.entity.MyName + "'s " + grip.heldPart.displayName + ".");
            entity.EndTurn(0.02f, 10);
            return;
        }

        if (entity.isPlayer)
        {
            grip.HeldBody.entity.AI.BecomeHostile();
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence / 2);
        }
        if (!grip.HeldBody.entity.isPlayer)
        {
            grip.HeldBody.entity.AI.SetTarget(entity);
        }

        entity.EndTurn(0.02f, 10);
    }

    public void Grapple_Disarm(BodyPart.Grip grip)
    {
        bool success = RNG.Chance(entity.stats.Strength + grappleLevel);

        if (success)
        {
            Item item = new Item(grip.heldPart.hand.EquippedItem);
            grip.heldPart.hand.SetEquippedItem(ItemList.GetItemByID(grip.heldPart.hand.baseItem), grip.HeldBody.entity);

            if (item.lootable)
            {
                World.objectManager.NewInventory("Loot", grip.HeldBody.entity.GetEmptyCoords().GetRandom(), World.tileMap.WorldPosition, World.tileMap.currentElevation, new List<Item>() { item });
            }

            CombatLog.NewMessage(grip.HeldBody.entity.MyName + "'s " + item.DisplayName() + " is torn from their hand by " + entity.MyName + "!");
        }
        else
        {
            CombatLog.NewMessage(entity.MyName + " fails to disarm " + grip.HeldBody.entity.MyName + ".");
        }


        if (entity.isPlayer)
        {
            grip.HeldBody.entity.AI.BecomeHostile();
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);

            entity.stats.proficiencies.MartialArts.AddXP(entity.stats.Intelligence + 1);
        }

        entity.EndTurn(0.01f, 10);
    }

    public void CallForHelp()
    {
        if (entity.AI.InSightOfPlayer())
        {
            Instantiate(World.poolManager.roarEffect, transform.position, Quaternion.identity);
            CombatLog.NameMessage("Message_Call", entity.Name);
        }

        foreach (Entity e in World.objectManager.onScreenNPCObjects)
        {
            e.AI.AnswerCallForHelp(entity.AI, entity.AI.target);
        }

        UseStaminaIfNotPlayer(2);
    }

    //Random teleport -- Enemies only
    public void Teleport()
    {
        entity.BeamDown();
        entity.UnSetCell();

        var possCoords = World.tileMap.InSightCoords();
        if (possCoords.Count > 0)
        {
            entity.myPos = possCoords.GetRandom();
        }

        World.soundManager.TeleportSound();
        entity.ForcePosition();
        CombatLog.NameMessage("Message_Teleport", entity.Name);
    }

    public bool PassiveDisarm(Entity attacker)
    {
        if (attacker == null)
        {
            return false;
        }

        Inventory otherInventory = attacker.inventory;

        if (otherInventory == null || !attacker.body.MainHand.EquippedItem.lootable)
        {
            return false;
        }

        CombatLog.CombatMessage("Message_Disarm", attacker.Name, entity.Name, entity.isPlayer);
        otherInventory.Disarm();

        if (entity.isPlayer)
        {
            entity.body.TrainLimbOfType(ItemProperty.Slot_Arm);
        }

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
        {
            entity.stats.UseStamina(cost);
        }
    }
}

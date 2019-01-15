using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using Pathfinding;

public static class AIManager
{
    public static void MakeDecision(BaseAI ai)
    {
        if (!CanAct(ai))
        {
            ai.entity.Wait();
            return;
        }

        if (ai.npcBase.HasFlag(NPC_Flags.Quantum_Locked))
        {
            QuantumLockedAction(ai);
            return;
        }

        if (ai.isHostile)
            HostileAction(ai);
        else if (ai.npcBase.HasFlag(NPC_Flags.Follower))
            FollowerAction(ai);
        else
            PassiveAction(ai);
    }

    static void QuantumLockedAction(BaseAI ai)
    {
        if (TryUseSkills(ai, ai.target))
            return;

        if (ai.target != null)
        {

        }
        else
        {
            Path_AStar path = new Path_AStar(ai.entity.myPos, ai.target.myPos, ai.entity.inventory.CanFly());
            ai.SetPath(path);
        }

        ConfirmAction(ai);
    }

    static void PassiveAction(BaseAI ai)
    {
        if (ai.target != null)
        {
            HostileAction(ai);
            return;
        }

        Coord targetPos = World.tileMap.CurrentMap.GetRandomFloorTile();
        ai.SetPath(new Path_AStar(ai.entity.myPos, targetPos, ai.entity.inventory.CanFly()));
        ConfirmAction(ai);
    }

    static void HostileAction(BaseAI ai)
    {
        if (TryUseSkills(ai, ai.target))
            return;

        if (ai.target == null)
            PickTarget(ai);

        if (ai.target != null)
        {
            ai.SetPath(new Path_AStar(ai.entity.myPos, ai.target.myPos, ai.entity.inventory.CanFly()));
            ConfirmAction(ai);
        }
        else
        {
            PassiveAction(ai);
        }
    }

    static void FollowerAction(BaseAI ai)
    {
        if (TryUseSkills(ai, ai.target))
            return;

        if (ai.target == ObjectManager.playerEntity)
        {
            if (ai.entity.myPos.DistanceTo(ai.target.myPos) > 3)
            {
                ai.SetPath(new Path_AStar(ai.entity.myPos, ai.target.myPos, ai.entity.inventory.CanFly()));
            }
            else
            {
                Coord targetPos = World.tileMap.CurrentMap.GetRandomFloorTile();
                ai.SetPath(new Path_AStar(ai.entity.myPos, targetPos, ai.entity.inventory.CanFly()));
            }

            ConfirmAction(ai);
        }
        else
        {
            HostileAction(ai);
        }
    }

    static void ConfirmAction(BaseAI ai)
    {
        if (ai.path == null)
        {
            ai.entity.Wait();
        }
        else
        {
            ai.FollowPath();
        }
    }

    static bool CanAct(BaseAI ai)
    {
        if (ai.npcBase.HasFlag(NPC_Flags.Stationary))
        {
            if (ai.target == null)
                return false;
            if (ai.entity.myPos.DistanceTo(ai.target.myPos) >= 2)
                return false;
        }

        if (ai.npcBase.HasFlag(NPC_Flags.Quantum_Locked))
        {
            if (ai.InSightOfPlayer())
                return false;
        }

        return !ai.entity.stats.SkipTurn();
    }

    static void PickTarget(BaseAI ai)
    {
        List<Entity> possibleTargets = new List<Entity>();

        if (ai.npcBase.faction.isHostileTo("player") || ai.isHostile)
            possibleTargets.Add(ObjectManager.playerEntity);

        foreach (Entity ent in World.objectManager.onScreenNPCObjects)
        {
            if (ai.ShouldAttack(ent.AI) && ai.entity.inSight(ent.myPos))
                possibleTargets.Add(ent);
        }

        ai.SetTarget((possibleTargets.Count == 0) ? null : GetClosestTarget(ai, possibleTargets));
    }

    static Entity GetClosestTarget(BaseAI ai, List<Entity> entities)
    {
        Entity closest = ObjectManager.playerEntity;
        float distance = Mathf.Infinity;

        foreach (Entity en in entities)
        {
            if (en.isPlayer && !en.AI.InSightOfPlayer())
                continue;
            float dist = ai.entity.myPos.DistanceTo(en.myPos);

            if (closest == null || dist < distance)
            {
                closest = en;
                distance = dist;
            }
        }

        if (closest == null)
            return ai.entity;

        return closest;
    }

    public static bool TryUseSkills(BaseAI ai, Entity target)
    {
        if (target == null || ai.entitySkills.abilities.Count == 0 || ai.entity.stats.HasEffect("Blind") || ai.entity.stats.SkipTurn())
        {
            return false;
        }

        List<Skill> possible = new List<Skill>();

        for (int i = 0; i < ai.entitySkills.abilities.Count; i++)
        {
            Skill skill = ai.entitySkills.abilities[i];

            if (skill.aiAction == null || skill.cooldown > 0 || skill.staminaCost > ai.entity.stats.stamina ||
                skill.castType == CastType.Target && ai.entity.myPos.DistanceTo(target.myPos) > skill.range)
                continue;

            DynValue result = LuaManager.CallScriptFunction("AbilityAI", skill.aiAction.functionName, new object[] { skill, ai.entity });
            bool canUse = result.Boolean;

            if (canUse)
            {
                possible.Add(skill);
            }
        }

        if (possible.Count > 0)
        {
            Skill choice = possible.GetRandom(SeedManager.combatRandom);
            choice.Cast(ai.entity);
            return true;
        }

        return false;
    }
}

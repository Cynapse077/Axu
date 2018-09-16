using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public static class AIManager
{
    public static void MakeDecision(BaseAI ai)
    {
        if (!CanAct(ai))
        {
            ai.entity.Wait();
            return;
        }

        if (ai.isHostile)
            HostileAction(ai);
        else if (ai.npcBase.HasFlag(NPC_Flags.Follower))
            FollowerAction(ai);
        else
            PassiveAction(ai);
    }

    static void PassiveAction(BaseAI ai)
    {

    }

    static void HostileAction(BaseAI ai)
    {
        
    }

    static void FollowerAction(BaseAI ai)
    {
        
    }

    static bool CanAct(BaseAI ai)
    {
        Stats s = ai.entity.stats;

        if (s.HasEffect("Stun") || s.HasEffect("Unconscious") || s.Frozen())
            return false;

        return true;
    }

    static void PickTarget(BaseAI ai)
    {
        List<Entity> possibleTargets = new List<Entity>();

        if (ai.npcBase.faction.isHostileTo("player") || ai.isHostile)
            possibleTargets.Add(ObjectManager.playerEntity);

        foreach (Entity ent in World.objectManager.onScreenNPCObjects)
        {
            if (ai.ShouldAttack(ent.AI))
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
            if (!en.isPlayer && !en.AI.InSightOfPlayer())
                continue;

            float dist = ai.entity.myPos.DistanceTo(en.myPos);

            if (closest == null)
            {
                distance = dist;
                closest = en;
            }
            else if (dist < distance)
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
        if (ai.entitySkills.abilities.Count == 0 || ai.entity.stats.HasEffect("Blind"))
            return false;

        List<Skill> possible = new List<Skill>();

        for (int i = 0; i < ai.entitySkills.abilities.Count; i++)
        {
            Skill skill = ai.entitySkills.abilities[i];

            if (skill.aiAction == null || skill.cooldown > 0 || skill.staminaCost > ai.entity.stats.stamina)
                continue;

            if (skill.castType == CastType.Target && ai.entity.myPos.DistanceTo(target.myPos) > skill.range)
                continue;

            DynValue result = LuaManager.CallScriptFunction("AbilityAI", skill.aiAction.functionName, new object[] { skill, ai.entity });
            bool canUse = result.Boolean;

            if (canUse)
                possible.Add(skill);
        }

        if (possible.Count > 0)
        {
            Skill choice = possible.GetRandom();
            choice.Cast(ai.entity);
            return true;
        }

        return false;
    }
}

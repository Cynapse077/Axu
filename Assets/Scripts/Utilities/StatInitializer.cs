using System.Collections.Generic;

public static class StatInitializer
{
    public static void GetPlayerStats(Stats s, PlayerBuilder builder)
    {
        s.MyLevel = new XPLevel(s, builder.level.CurrentLevel, builder.level.XP, builder.level.XPToNext);
        s.gameObject.name = Manager.playerName;
        s.maxHealth = builder.maxHP;
        s.maxStamina = builder.maxST;
        s.health = builder.hp;
        s.stamina = builder.st;
        s.Attributes = new Dictionary<string, int>(builder.attributes);

        s.statusEffects = new Dictionary<string, int>(builder.statusEffects);
        s.proficiencies = builder.proficiencies;

        for (int i = 0; i < builder.traits.Count; i++)
        {
            s.AddTrait(builder.traits[i]);
        }
    }

    public static void GetNPCStats(NPC npc, Stats s)
    {
        s.maxHealth = npc.maxHealth;
        s.health = npc.health;
        s.maxStamina = npc.maxStamina;
        s.stamina = npc.stamina;

        s.Attributes = new Dictionary<string, int>(npc.Attributes);
        s.statusEffects = new Dictionary<string, int>();

        if (npc.HasFlag(NPC_Flags.RPois))
        {
            Trait t = TraitList.GetTraitByID("rpois");

            if (t != null)
            {
                s.AddTrait(t);
            }
        }
        if (npc.HasFlag(NPC_Flags.RBleed))
        {
            Trait t = TraitList.GetTraitByID("rbleed");

            if (t != null)
            {
                s.AddTrait(t);
            }
        }
    }
}

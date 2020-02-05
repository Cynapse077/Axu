using System.Collections.Generic;

public static class StatInitializer
{
    public static void GetPlayerStats(Stats s, PlayerBuilder builder)
    {
        s.level = new XPLevel(s, builder.level.CurrentLevel, builder.level.XP, builder.level.XPToNext);
        s.gameObject.name = Manager.playerName;
        s.Attributes = new Dictionary<string, int>(builder.attributes);
        s.SetAttribute("Health", builder.maxHP);
        s.SetAttribute("Stamina", builder.maxST);
        s.health = s.MaxHealth;
        s.stamina = s.MaxStamina;

        s.statusEffects = new Dictionary<string, int>(builder.statusEffects);
        s.proficiencies = builder.proficiencies;

        for (int i = 0; i < builder.traits.Count; i++)
        {
            s.AddTrait(builder.traits[i]);
        }
    }

    public static void GetNPCStats(NPC npc, Stats s)
    {
        s.Attributes = new Dictionary<string, int>(npc.Attributes);
        s.SetAttribute("Health", npc.maxHealth);
        s.SetAttribute("Stamina", npc.maxStamina);
        s.health = s.MaxHealth;
        s.stamina = s.MaxStamina;

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

using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class TraitList
{
    public static Trait GetTraitByID(string id)
    {
        return new Trait(GameData.Get<Trait>(id) as Trait);
    }

    public static List<Trait> GetAvailableMutations(Stats stats)
    {
        List<Trait> muts = GameData.GetAll<Trait>().FindAll(x => x.ContainsEffect(TraitEffects.Mutation) && x.tier < 2);
        List<Trait> possibilities = new List<Trait>();

        foreach (Trait mut in muts)
        {
            //Check if the character has this mutation, or if it can/will stack
            if (stats.hasTrait(mut.ID) && !mut.stackable || stats.hasTrait(mut.nextTier) || stats.hasTrait(mut.prerequisite))
                continue;

            //Check overriding slots.
            if (mut.slot != string.Empty && stats.traits.Find(x => x.slot == mut.slot) != null)
                continue;

            if (mut.stackable && stats.TraitStacks(mut.ID) >= mut.maxStacks)
                continue;

            possibilities.Add(new Trait(mut));
        }

        return possibilities;
    }

    public static List<Wound> GetAvailableWounds(BodyPart bp, HashSet<DamageTypes> dts)
    {
        List<Wound> ws = new List<Wound>();

        foreach (Wound w in GameData.GetAll<Wound>())
        {
            if (w == null || w.slot != ItemProperty.None && w.slot != bp.slot || bp.wounds.Find(x => x.ID == w.ID) != null)
            {
                continue;
            }

            bool canAdd = false;

            for (int j = 0; j < w.damTypes.Count; j++)
            {
                if (dts.Contains(w.damTypes[j]))
                {
                    canAdd = true;
                    break;
                }
            }

            if (canAdd)
            {
                ws.Add(new Wound(w));
            }
        }

        return ws;
    }
}

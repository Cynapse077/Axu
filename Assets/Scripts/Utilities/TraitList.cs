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

        for (int i = 0; i < muts.Count; i++)
        {
            if (stats.hasTrait(muts[i].ID) && !muts[i].stackable || stats.hasTrait(muts[i].nextTier) || stats.hasTrait(muts[i].prerequisite))
                continue;

            if (muts[i].slot != "" && stats.traits.Find(x => x.slot == muts[i].slot) != null)
                continue;

            if (muts[i].stackable && stats.TraitStacks(muts[i].ID) >= muts[i].maxStacks)
                continue;

            possibilities.Add(new Trait(muts[i]));
        }

        return possibilities;
    }

    public static List<Wound> GetAvailableWounds(BodyPart bp, HashSet<DamageTypes> dts)
    {
        List<Wound> ws = new List<Wound>();

        foreach (Wound w in GameData.GetAll<Wound>())
        {
            if (w.slot != ItemProperty.None && w.slot != bp.slot || bp.wounds.Find(x => x.ID == w.ID) != null)
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

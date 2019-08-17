using UnityEngine;
using System.Collections.Generic;
using System.IO;
using LitJson;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class TraitList
{
    public static List<Trait> traits
    {
        get
        {
            return GameData.instance.GetAll<Trait>();
        }
    }

    public static Trait GetTraitByID(string id)
    {
        return GameData.instance.Get<Trait>(id) as Trait;
    }

    public static List<Trait> GetAvailableMutations(Stats stats)
    {
        List<Trait> muts = traits.FindAll(x => x.ContainsEffect(TraitEffects.Mutation) && x.tier < 2);
        List<Trait> possibilities = new List<Trait>();

        for (int i = 0; i < muts.Count; i++)
        {
            if (stats.hasTrait(muts[i].ID) && !muts[i].stackable || stats.hasTrait(muts[i].nextTier) || stats.hasTrait(muts[i].prerequisite))
                continue;

            if (muts[i].slot != "" && stats.traits.Find(x => x.slot == muts[i].slot) != null)
                continue;

            if (muts[i].stackable && stats.TraitStacks(muts[i].ID) >= muts[i].maxStacks)
                continue;

            possibilities.Add(muts[i]);
        }

        return possibilities;
    }

    public static List<Wound> GetAvailableWounds(BodyPart bp, HashSet<DamageTypes> dts)
    {
        List<Wound> ws = new List<Wound>();
        List<Wound> wounds = GameData.instance.GetAll<Wound>();

        for (int i = 0; i < wounds.Count; i++)
        {
            if (wounds[i].slot != ItemProperty.None && wounds[i].slot != bp.slot || bp.wounds.Find(x => x.ID == wounds[i].ID) != null)
            {
                continue;
            }

            bool canAdd = false;

            for (int j = 0; j < wounds[i].damTypes.Count; j++)
            {
                if (dts.Contains(wounds[i].damTypes[j]))
                {
                    canAdd = true;
                    break;
                }
            }

            if (canAdd)
            {
                ws.Add(new Wound(wounds[i]));
            }
        }

        return ws;
    }
}

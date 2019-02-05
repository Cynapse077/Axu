using UnityEngine;
using System.Collections.Generic;
using System.IO;
using LitJson;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class TraitList
{
    public static List<Trait> traits;
    public static List<Wound> wounds;

    public static string woundPath;
    public static string traitPath;

    public static void FillTraitsFromData()
    {
        traits = new List<Trait>();
        string listFromJson = File.ReadAllText(Application.streamingAssetsPath + traitPath);

        if (string.IsNullOrEmpty(listFromJson))
            Debug.LogError("Traits null.");

        JsonData data = JsonMapper.ToObject(listFromJson);

        for (int i = 0; i < data["Traits"].Count; i++)
        {
            traits.Add(GetTraitFromData(data["Traits"][i]));
        }

        wounds = new List<Wound>();
        listFromJson = File.ReadAllText(Application.streamingAssetsPath + woundPath);

        if (string.IsNullOrEmpty(listFromJson))
            Debug.LogError("Wounds null");

        data = JsonMapper.ToObject(listFromJson);

        for (int i = 0; i < data["Wounds"].Count; i++)
        {
            wounds.Add(GetWoundFromData(data["Wounds"][i]));
        }
    }

    public static Wound GetWoundFromData(JsonData wData)
    {
        ItemProperty ip = (wData["Slot"].ToString()).ToEnum<ItemProperty>();
        List<DamageTypes> dts = new List<DamageTypes>();

        if (wData.ContainsKey("Damage Types"))
        {
            for (int i = 0; i < wData["Damage Types"].Count; i++)
            {
                DamageTypes dt = (wData["Damage Types"][i].ToString()).ToEnum<DamageTypes>();
                dts.Add(dt);
            }
        }

        Wound w = new Wound(wData["Name"].ToString(), wData["ID"].ToString(), ip, dts)
        {
            Desc = wData["Description"].ToString()
        };

        List<Stat_Modifier> sm = new List<Stat_Modifier>();

        if (wData.ContainsKey("Stats"))
        {
            for (int i = 0; i < wData["Stats"].Count; i++)
            {
                Stat_Modifier s = new Stat_Modifier(wData["Stats"][i]["Stat"].ToString(), (int)wData["Stats"][i]["Amount"]);
                sm.Add(s);
            }

            w.statMods = sm;
        }

        return w;
    }

    public static Trait GetTraitFromData(JsonData tData)
    {
        Trait t = new Trait(tData["Name"].ToString(), tData["ID"].ToString())
        {
            description = tData["Description"].ToString()
        };

        //Stat changes
        if (tData.ContainsKey("Stats"))
        {
            for (int s = 0; s < tData["Stats"].Count; s++)
            {
                string name = tData["Stats"][s]["Stat"].ToString();
                int amount = (int)tData["Stats"][s]["Amount"];
                t.stats.Add(new Stat_Modifier(name, amount));
            }
        }

        //Limb replacement via Mutations.
        if (tData.ContainsKey("Replace_Body_Part"))
        {
            JsonData newData = tData["Replace_Body_Part"];

            string slotString = newData["Slot"].ToString();
            ItemProperty slot = slotString.ToEnum<ItemProperty>();
            string bpName = newData.ContainsKey("BodyPartName") ? newData["BodyPartName"].ToString() : "";
            string newItem = (newData.ContainsKey("NewEquippedItem")) ? newData["NewEquippedItem"].ToString() : null;
            bool cwg = newData.ContainsKey("CanWearGear") ? (bool)newData["CanWearGear"] : true;
            bool aot = newData.ContainsKey("AllOfType") ? (bool)newData["AllOfType"] : false;

            t.replaceBodyPart = new ReplaceBodyPart(slot, bpName, newItem, cwg, aot)
            {
                removeAll = newData.ContainsKey("RemoveAll") ? (bool)newData["RemoveAll"] : false
            };

            //Add in extra parts.
            if (newData.ContainsKey("ExtraParts"))
            {
                t.replaceBodyPart.extraLimbs = new List<BodyPart>();
                for (int j = 0; j < newData["ExtraParts"].Count; j++)
                {
                    string bName = newData["ExtraParts"][j]["Name"].ToString();
                    BodyPart bp = EntityList.GetBodyPart(bName);

                    t.replaceBodyPart.extraLimbs.Add(bp);
                }
            }
        }

        //Effects (enum/string)
        if (tData.ContainsKey("Tags"))
        {
            for (int e = 0; e < tData["Tags"].Count; e++)
            {
                string effect = tData["Tags"][e].ToString();
                t.effects.Add(effect.ToEnum<TraitEffects>());
            }
        }

        //Cancellations
        t.slot = (tData.ContainsKey("Cancels")) ? tData["Cancels"].ToString() : "";

        //Abilities
        if (tData.ContainsKey("Abilities"))
        {
            for (int a = 0; a < tData["Abilities"].Count; a++)
            {
                string abName = tData["Abilities"][a].ToString();
                t.abilityIDs.Add(abName);
            }
        }

        //Scripts
        if (tData.ContainsKey("Scripts"))
        {
            for (int s = 0; s < tData["Scripts"].Count; s++)
            {
                if (tData["Scripts"][s]["Action"].ToString() == "OnTurn")
                {
                    t.luaCall = new LuaCall(tData["Scripts"][s]["File"].ToString(), tData["Scripts"][s]["Function"].ToString());
                }
            }
        }

        //Prerequisites and Next Tiers
        t.tier = (tData.ContainsKey("Tier")) ? (int)tData["Tier"] : 0;
        if (tData.ContainsKey("Prerequisite"))
            t.prerequisite = tData["Prerequisite"].ToString();
        if (tData.ContainsKey("Next Tier"))
            t.nextTier = tData["Next Tier"].ToString();

        if (tData.ContainsKey("Stackable"))
            t.stackable = (bool)tData["Stackable"];

        t.maxStacks = (tData.ContainsKey("Max Stacks")) ? (int)tData["Max Stacks"] : 1;

        return t;
    }

    public static Trait GetTraitByID(string id)
    {
        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].ID == id)
            {
                return traits[i];
            }
        }

        return null;
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

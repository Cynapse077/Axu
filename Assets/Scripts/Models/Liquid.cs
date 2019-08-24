using UnityEngine;
using System.Collections.Generic;
using LitJson;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Liquid : IWeighted, IAsset
{
    static List<MixingOutput> mixingOutputs;

    public string ID { get; set; }
    public string ModID { get; set; }
    public string Name, Description;
    public int units;
    public int pricePerUnit;
    public int addictiveness;
    public Color32 color;
    public List<CLuaEvent> events;
    public int Weight { get; set; }

    public Liquid()
    {
        events = new List<CLuaEvent>();
    }

    public Liquid(Liquid other, int un)
    {
        CopyFrom(other);
        units = un;
    }

    public Liquid(JsonData d)
    {
        FromJson(d);
    }

    void CopyFrom(Liquid other)
    {
        ID = other.ID;
        Name = other.Name;
        units = other.units;
        Description = other.Description;
        pricePerUnit = other.pricePerUnit;
        addictiveness = other.addictiveness;
        events = new List<CLuaEvent>();
        color = other.color;

        for (int i = 0; i < other.events.Count; i++)
        {
            events.Add((CLuaEvent)other.events[i].Clone());
        }
    }

    void FromJson(JsonData d)
    {
        ID = d["ID"].ToString();
        Name = d["Name"].ToString();
        Description = d["Description"].ToString();
        Weight = 101 - (int)d["Frequency"];
        pricePerUnit = (int)d["Price Per Unit"];
        addictiveness = d.ContainsKey("Addictiveness") ? (int)d["Addictiveness"] : 0;
        byte r = (byte)((int)d["Color"][0]), g = (byte)((int)d["Color"][1]), b = (byte)((int)d["Color"][2]);
        color = new Color32(r, g, b, 255);

        events = new List<CLuaEvent>();

        if (d.ContainsKey("LuaEvents"))
        {
            for (int j = 0; j < d["LuaEvents"].Count; j++)
            {
                CLuaEvent cl = new CLuaEvent(d["LuaEvents"][j]["Event"].ToString(), d["LuaEvents"][j]["Script"].ToString());
                events.Add(cl);
            }
        }
    }

    public void Drink(Stats stats)
    {
        if (events.Find(x => x.evName == "OnDrink") != null)
        {
            events.Find(x => x.evName == "OnDrink").CallEvent_Params("OnDrink", stats);
        }

        if (addictiveness > 0 && (World.difficulty.Level == Difficulty.DiffLevel.Rogue || World.difficulty.Level == Difficulty.DiffLevel.Hunted))
        {
            stats.ConsumedAddictiveSubstance(ID, true);
        }
    }

    public void Splash(Stats stats)
    {
        if (events.Find(x => x.evName == "OnSplash") != null)
        {
            events.Find(x => x.evName == "OnSplash").CallEvent_Params("OnSplash", stats);
        }
    }

    public void Coat(Item item)
    {
        if (events.Find(x => x.evName == "OnCoat") != null)
        {
            events.Find(x => x.evName == "OnCoat").CallEvent_Params("OnCoat", item);
        }
    }

    public SLiquid ToSLiquid()
    {
        return new SLiquid(ID, units);
    }

    struct MixingOutput
    {
        public string Input1, Input2;
        public string Output;

        public MixingOutput(string in1, string in2, string otp)
        {
            Input1 = in1;
            Input2 = in2;
            Output = otp;
        }
    }

    public static void SetupMixingTables(JsonData data)
    {
        mixingOutputs = new List<MixingOutput>();

        for (int i = 0; i < data["Mixing Tables"].Count; i++)
        {
            MixingOutput mo = JsonMapper.ToObject<MixingOutput>(data["Mixing Tables"][i].ToJson());
            mixingOutputs.Add(mo);
        }
    }

    public static Liquid Mix(Liquid input1, Liquid input2)
    {
        int totalUnits = input1.units + input2.units;

        if (input1.ID == input2.ID)
            return new Liquid(input1, totalUnits);

        Liquid liq = ItemList.GetLiquidByID("liquid_default", totalUnits);

        for (int i = 0; i < mixingOutputs.Count; i++)
        {
            if (input1.ID == mixingOutputs[i].Input1 || input1.ID == mixingOutputs[i].Input2)
            {
                if (input2.ID == mixingOutputs[i].Input1 || input2.ID == mixingOutputs[i].Input2)
                    return ItemList.GetLiquidByID(mixingOutputs[i].Output, totalUnits);
            }
        }

        return liq;
    }
}

[System.Serializable]
public class SLiquid
{
    public string ID;
    public int units;

    public SLiquid() { }

    public SLiquid(string id, int amount)
    {
        ID = id;
        units = amount;
    }
}
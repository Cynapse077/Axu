using System;
using System.Collections.Generic;
using LitJson;
using MoonSharp.Interpreter;

/// <summary>
/// This class is used to store the names of items in save file data. Keeps the amount stored low.
/// </summary>
[Serializable]
public class SItem
{
    public string ID { get; set; }
    public string DName { get; set; }
    public int[] Dmg { get; set; }
    public int Ar { get; set; }
    public string MID { get; set; }
    public int Am { get; set; }
    public List<ItemProperty> Props { get; set; }
    public List<CComponent> Com { get; set; }
    public List<Stat_Modifier> Sm;

    public SItem() { }
    public SItem(string _id, string _modName, int amount, string dName, List<ItemProperty> props, Damage dmg, int armor, List<CComponent> cc, List<Stat_Modifier> sm)
    {
        ID = _id;

        if (_modName != null)
            MID = _modName;

        Am = amount;
        DName = dName;
        Props = props;

        Dmg = new int[] { dmg.Num, dmg.Sides, dmg.Inc };
        Ar = armor;
        Com = cc;
        Sm = sm;
    }
}

[Serializable]
public struct StringInt
{
    public string A;
    public int B;

    public string String
    {
        get { return A; }
    }

    public int Int
    {
        get { return B; }
    }

    public StringInt(string a, int b)
    {
        A = a;
        B = b;
    }
}

[Serializable]
public struct STrait
{
    public string id;
    public int tAc;

    public STrait(string _id, int turnAcquired)
    {
        id = _id;
        tAc = turnAcquired;
    }
}

[Serializable]
[MoonSharpUserData]
public class Stat_Modifier
{
    public string Stat { get; set; }
    public int Amount { get; set; }

    public Stat_Modifier()
    {
        Stat = "";
        Amount = 0;
    }

    public Stat_Modifier(string name, int amt)
    {
        Stat = name;
        Amount = amt;
    }

    public Stat_Modifier(Stat_Modifier other)
    {
        Stat = other.Stat;
        Amount = other.Amount;
    }
}
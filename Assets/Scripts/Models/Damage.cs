using UnityEngine;
using System.Collections;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Damage
{
    public int Num;
    public int Sides;
    public int Inc;
    public DamageTypes Type;

    public bool IsEmpty
    {
        get
        {
            return Num == 0 && Sides == 0 && Inc == 0;
        }
    }

    public Damage() { }

    public Damage(Damage other)
    {
        Num = other.Num;
        Sides = other.Sides;
        Inc = other.Inc;
        Type = other.Type;
    }

    public Damage(int numDice, int diceSides, int increase, DamageTypes type = DamageTypes.Blunt)
    {
        Num = numDice;
        Sides = diceSides;
        Inc = increase;
        Type = type;
    }

    public override string ToString()
    {
        if (GameSettings.SimpleDamage)
            return Simplified();

        if (Inc > 0)
            return string.Format("{0}d{1}+{2}", Num, Sides, Inc);
        if (Inc < 0)
            return string.Format("{0}d{1}{2}", Num, Sides, Inc);

        return string.Format("{0}d{1}", Num, Sides);
    }

    public string Simplified()
    {
        int min = Inc, max = Inc;

        for (int i = 0; i < Num; i++)
        {
            min++;
            max += Sides;
        }

        return min + "-" + max;
    }

    public void ChangeValues(int numDice, int diceSides, int increase)
    {
        Num += numDice;
        Sides += diceSides;
        Inc += increase;
    }

    public int Roll()
    {
        int damage = Inc;

        for (int i = 0; i < Num; i++)
        {
            damage += SeedManager.combatRandom.Next(Sides) + 1;
        }

        return damage;
    }

    public static Damage operator +(Damage d1, Damage d2)
    {
        if (d2 == null)
            return d1;
        return new Damage(d1.Num + d2.Num, d1.Sides + d2.Sides, d1.Inc + d2.Inc, d1.Type);
    }
    public static Damage operator -(Damage d1, Damage d2)
    {
        if (d2 == null)
            return d1;
        return new Damage(d1.Num - d2.Num, d1.Sides - d2.Sides, d1.Inc - d2.Inc, d1.Type);
    }

    public static Damage GetByString(string dmgString)
    {
        int dice = 0, sides = 0, inc = 0;
        string[] ss = dmgString.Split('d');

        if (ss.Length < 2)
        {
            Log.Error("Damage.GetByString - Could not parse string \"" + dmgString + "\"");
            return new Damage();
        }

        if (ss[1].Contains("+"))
        {
            string[] ss2 = ss[1].Split("+"[0]);
            sides = int.Parse(ss2[0]);
            inc = int.Parse(ss2[1]);
        }
        else if (ss[1].Contains("-"))
        {
            string[] ss2 = ss[1].Split("-"[0]);
            sides = int.Parse(ss2[0]);
            inc = int.Parse(ss2[1]) * -1;
        }
        else
        {
            inc = 0;
            sides = int.Parse(ss[1]);
        }

        dice = int.Parse(ss[0]);

        return new Damage(dice, sides, inc, DamageTypes.Blunt);
    }
}

[System.Serializable]
public enum DamageTypes
{
    None, Blunt, Slash, Pierce, Cleave, Energy, Heat, Cold, Venom, Claw, Bleed, Hunger, Corruption, NonLethal, Radiation, Pull
}

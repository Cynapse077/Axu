using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RNG
{
    public static int Next(int max)
    {
        return SeedManager.combatRandom.Next(max);
    }

    public static int Next(int min, int max)
    {
        return SeedManager.combatRandom.Next(min, max);
    }

    public static bool Chance(int chance)
    {
        return Next(100) < chance;
    }

    public static bool Chance(double chance)
    {
        return Next(100) < chance;
    }

    public static bool OneIn(int num)
    {
        return SeedManager.combatRandom.Next(0, num) == 0;
    }

    public static bool CoinFlip()
    {
        return SeedManager.combatRandom.Next(100) < 50;
    }

    public static int NegOneOrOne()
    {
        return CoinFlip() ? -1 : 1;
    }

    public static double NextDouble()
    {
        return SeedManager.combatRandom.NextDouble();
    }

    public static float NextFloat()
    {
        return (float)SeedManager.combatRandom.NextDouble();
    }
}

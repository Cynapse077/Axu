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
}

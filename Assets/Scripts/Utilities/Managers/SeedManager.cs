using System;

public static class SeedManager {
	public static Random combatRandom;
	public static Random textRandom;
	public static Random localRandom;
	public static Random worldRandom;

    public static void SetSeedFromName(int seed) {
		worldRandom = new Random(seed);

        CoordinateSeed(0, 0, 0);
    }

	public static void InitializeSeeds() {
		combatRandom = new Random(DateTime.Now.GetHashCode());
		textRandom = new Random(DateTime.Now.GetHashCode());
	}

    public static void CoordinateSeed(int x, int y, int z = 0) {
		int seed = (x * 105701) + (y * 15486491) + (z * 105907);
		localRandom = new Random(seed);
    }

    public static void NPCSeed(string name)
    {
        int seed = name.GetHashCode();
        localRandom = new Random(seed);
    }
}
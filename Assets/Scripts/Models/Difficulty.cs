public class Difficulty
{
    public DiffLevel Level = DiffLevel.Rogue;
    public string descTag;

    public Difficulty(DiffLevel dl, string desc)
    {
        Level = dl;
        descTag = desc;
    }

    public float EncounterRate()
    {
        switch (Level)
        {
            case DiffLevel.Adventurer:
                return 0.8f;
            case DiffLevel.Scavenger:
            case DiffLevel.Rogue:
                return 1.0f;
            case DiffLevel.Hunted:
                return 1.2f;
            default:
                return 1.0f;
        }
    }

    public bool AddictionsActive
    {
        get
        {
            return Level == DiffLevel.Rogue || Level == DiffLevel.Hunted;
        }
    }

    public float AddictionDivisible()
    {
        if (Level == DiffLevel.Hunted)
            return 2f;

        return 4f;
    }

    public bool Permadeath
    {
        get
        {
            return Level == DiffLevel.Rogue || Level == DiffLevel.Hunted;
        }
    }

    public enum DiffLevel
    {
        Adventurer,
        Scavenger,
        Rogue,
        Hunted
    }
}

public class Difficulty
{
    public DiffLevel Level = DiffLevel.Rogue;
    public string descTag;

    public Difficulty(DiffLevel dl, string desc)
    {
        Level = dl;
        descTag = desc;
    }

    public enum DiffLevel
    {
        Adventurer,
        Scavenger,
        Rogue,
        Hunted
    }
}

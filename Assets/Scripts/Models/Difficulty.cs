public class Difficulty
{
    public DiffLevel Level = DiffLevel.Rogue;
    public double XPScale = 1.0;
    public string descTag;

    public Difficulty(DiffLevel dl, double scale, string desc)
    {
        Level = dl;
        XPScale = scale;
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

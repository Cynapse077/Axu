
[MoonSharp.Interpreter.MoonSharpUserData]
public class XPLevel
{
    public int CurrentLevel { get; set; }
    public int XP { get; set; }
    public int XPToNext { get; set; }
    public const int MaxLevel = 50;
    const int Lvl_1_XP_Next = 100;
    private Stats myStats;

    public XPLevel(Stats stats)
    {
        CurrentLevel = 1;
        XP = 0;
        XPToNext = Lvl_1_XP_Next;
        myStats = stats;
    }

    public XPLevel(Stats stats, int lvl, int xp, int xpToNext)
    {
        this.CurrentLevel = lvl;
        this.XP = xp;
        this.XPToNext = xpToNext;
        myStats = stats;
    }

    public void AddXP(int amount)
    {
        if (CurrentLevel < MaxLevel)
        {
            XP += amount;

            while (XP >= XPToNext)
            {
                if (CurrentLevel >= MaxLevel)
                {
                    XP = 0;
                    break;
                }

                CurrentLevel++;
                myStats.LevelUp(CurrentLevel);
                XP -= XPToNext;
                XPToNext += (XPToNext / 4);
            }
        }
        else
        {
            XP = 0;
        }
    }
}

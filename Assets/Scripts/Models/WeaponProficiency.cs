using UnityEngine;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class WeaponProficiency
{
    public string name;
    public int level;
    Proficiencies prof;
    double _xp;
    const int xpToNext = 1000;
    const int MaxLevel = 10;

    public double xp
    {
        get
        {
            _xp = System.Math.Round(_xp, 2);
            return _xp;
        }
        set { _xp = value; }
    }

    public WeaponProficiency(string nam)
    {
        name = nam;
        level = 0;
        xp = 0;
    }

    public WeaponProficiency(string nm, Proficiencies p)
    {
        name = nm;
        prof = p;
        level = 0;
        xp = 0;
    }

    public WeaponProficiency(string nam, int lvl, double exp)
    {
        name = nam;
        level = lvl;
        xp = exp;
    }

    public bool AddXP(double amount)
    {
        if (amount <= 0)
            return false;

        bool leveled = false;

        if (level < 1)
            amount *= 2.0;

        if (level < MaxLevel)
        {
            xp += amount;

            while (xp >= xpToNext)
            {
                xp -= xpToNext;
                LevelUp();
                leveled = true;
            }
        }
        else
            xp = 0;

        return leveled;
    }

    void LevelUp()
    {
        level++;
    }

    //Used for character creation screen.
    public string CCLevelName()
    {
        string myLvl = "<color=orange>" + (level).ToString() + "</color> - ";
        int lvl = Mathf.Min(level, 10);

        return myLvl + LocalizationManager.GetContent(("Prof_L" + lvl.ToString()));
    }

    public string LevelName()
    {
        int lvl = Mathf.Min(level - 1, 10);

        return LocalizationManager.GetContent(("Prof_L" + lvl.ToString()));
    }

    public void SetProficiency(Proficiencies p) { prof = p; }
    public Proficiencies GetProficiency() { return prof; }
}

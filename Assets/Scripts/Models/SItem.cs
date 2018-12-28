﻿using System;
using System.Collections.Generic;
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
public class SStats
{
    public int[] HP { get; set; }
    public int[] ST { get; set; }

    public int STR { get; set; }
    public int DEX { get; set; }
    public int INT { get; set; }
    public int END { get; set; }

    public int DEF { get; set; }
    public int SPD { get; set; }
    public int ACC { get; set; }
    public int STLH { get; set; }
    public int Rad { get; set; } //Radiation
    public List<StringInt> SE { get; set; }
    public List<Addiction> Adcts { get; set; }

    public SStats(Coord hp, Coord st, Dictionary<string, int> attributes, Dictionary<string, int> statusEffects, int radiation, List<Addiction> adc)
    {
        HP = new int[2] { hp.x, hp.y };
        ST = new int[2] { st.x, st.y };

        STR = attributes["Strength"];
        DEX = attributes["Dexterity"];
        INT = attributes["Intelligence"];
        END = attributes["Endurance"];
        DEF = attributes["Defense"];
        SPD = attributes["Speed"];
        ACC = attributes["Accuracy"];
        Adcts = adc;

        SE = new List<StringInt>();
        foreach (KeyValuePair<string, int> kvp in statusEffects)
        {
            SE.Add(new StringInt(kvp.Key, kvp.Value));
        }

        if (attributes.ContainsKey("Stealth"))
            STLH = attributes["Stealth"];

        Rad = radiation;
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

[Serializable]
public class SSettings
{
    public double Master_Volume { get; set; }
    public double Music_Volume { get; set; }
    public double SFX_Volume { get; set; }
    public double Animation_Speed { get; set; }
    public bool Mute { get; set; }
    public bool Fullscreen { get; set; }
    public Coord ScreenSize { get; set; }
    public bool UseMouse { get; set; }
    public bool Weather { get; set; }
    public bool Particle_Effects { get; set; }
    public bool SimpleDmg { get; set; }

    public InputKeys Input;

    public SSettings() { }

    public SSettings(bool fullscreen, Coord scSize, double masvol, double musvol, double sfxvol, bool mute,
        bool mouse, InputKeys keys, double animspeed, bool wea, bool part, bool sdmg)
    {
        Fullscreen = fullscreen;
        ScreenSize = scSize;
        Master_Volume = masvol;
        Music_Volume = musvol;
        SFX_Volume = sfxvol;
        Mute = mute;
        UseMouse = mouse;
        Input = keys;
        Animation_Speed = animspeed;
        Weather = wea;
        Particle_Effects = part;
        SimpleDmg = sdmg;
    }
}

[Serializable]
public class SSkill
{
    public string Name { get; protected set; }
    public int Lvl { get; protected set; }
    public double XP { get; protected set; }
    public int Flg { get; protected set; }

    public SSkill(string _name, int lvl, double xp, int origin)
    {
        Name = _name;
        Lvl = lvl;
        XP = xp;
        Flg = origin;
    }
}
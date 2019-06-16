using System.Collections.Generic;
using MoonSharp.Interpreter;

[System.Serializable]
[MoonSharpUserData]
public class Skill
{
    public const int maxLvl = 10;
    public const int XPToNext = 1000;

    public string Name, ID, Description;
    public int staminaCost, cooldown, maxCooldown, level, range = 20;
    public CastType castType;
    public DamageTypes damageType;
    public bool CanLevelUp = true;
    public LuaCall luaAction;
    public LuaCall aiAction;
    public int timeCost;
    public DiceRoll dice;
    public DiceRoll dicePerLevel;
    public AbilityOrigin origin;
    
    List<AbilityTags> tags;
    double _xp;

    public DiceRoll totalDice
    {
        get
        {
            if (dice == null)
                return new DiceRoll(0, 0, 0);

            DiceRoll roll = dice;

            if (dicePerLevel != null)
            {
                for (int i = 1; i < level; i++)
                {
                    roll += dicePerLevel;
                }
            }

            return roll;
        }
    }

    public double XP
    {
        get
        {
            return System.Math.Round(_xp, 2);
        }
        set
        {
            _xp = value;
        }
    }

    public Skill()
    {
        Name = "";
        ID = "";
        Description = "";
        castType = CastType.Instant;
        tags = new List<AbilityTags>();
        damageType = DamageTypes.None;
        origin = AbilityOrigin.None;
        level = 1;
        timeCost = 10;
    }

    public Skill(Skill other)
    {
        CopyFrom(other);
        origin = AbilityOrigin.None;
    }

    void CopyFrom(Skill other)
    {
        Name = other.Name;
        ID = other.ID;
        staminaCost = other.staminaCost;
        cooldown = 0;
        maxCooldown = other.maxCooldown;
        Description = other.Description;
        castType = other.castType;
        tags = new List<AbilityTags>(other.tags);
        damageType = other.damageType;
        level = other.level;
        XP = other.XP;
        CanLevelUp = other.CanLevelUp;
        luaAction = other.luaAction;
        aiAction = other.aiAction;
        timeCost = other.timeCost;
        dice = other.dice;
        dicePerLevel = other.dicePerLevel;
    }

    public void Init()
    {
        if (World.turnManager != null)
        {
            World.turnManager.incrementTurnCounter += ReduceCooldown;
        }
    }

    public void UnregisterCallbacks()
    {
        if (World.turnManager != null)
        {
            World.turnManager.incrementTurnCounter -= ReduceCooldown;
        }
    }

    public void AddTag(AbilityTags se)
    {
        tags.Add(se);
    }

    void ReduceCooldown()
    {
        if (cooldown > 0)
        {
            cooldown--;
        }
    }

    public void AddXP(int amount)
    {
        if (level >= maxLvl || !CanLevelUp || !FlagsHelper.IsSet(origin, AbilityOrigin.Book))
        {
            XP = 0;
        }
        else
        {
            XP += amount * 20 / level;

            while (XP >= XPToNext)
            {
                if (level >= maxLvl || !CanLevelUp)
                {
                    XP = 0;
                    break;
                }

                level++;
                XP -= XPToNext;
            }
        }      
    }

    //Called in Lua 
    public bool HasTag(AbilityTags se)
    {
        return tags.Contains(se);
    }

    public void Cast(Entity caster)
    {
        LuaManager.CallScriptFunction("Abilities", "Cast", caster, this);
    }

    public void ActivateCoordinateSkill(EntitySkills skills, Coord pos)
    {
        LuaManager.CallScriptFunction("Abilities", "Cast_Coordinate", skills.entity, pos, this);
    }

    public void CallScriptFunction(params object[] p)
    {
        LuaManager.CallScriptFunction(luaAction.scriptName, luaAction.functionName, p);
    }

    public void InitializeCooldown()
    {
        cooldown = maxCooldown;
    }

    public SSkill ToSerializedSkill()
    {
        return new SSkill(ID, level, XP, origin);
    }

    public void RemoveFlag(AbilityOrigin ab)
    {
        FlagsHelper.UnSet(ref origin, ab);
    }

    public void SetFlag(AbilityOrigin ab)
    {
        FlagsHelper.Set(ref origin, ab);
    }

    [System.Flags]
    public enum AbilityOrigin
    {
        None = 0,
        Book = 1,
        Trait = 2,
        Item = 4
    }
}

public enum CastType
{
    Instant, Target, Direction
}

public enum AbilityTags
{
    OpensNewWindow, Radiate_Self, Small_Square, Blind, Summon
}

[System.Serializable]
public class SSkill
{
    public string Name { get; protected set; }
    public int Lvl { get; protected set; }
    public double XP { get; protected set; }
    public Skill.AbilityOrigin Flg { get; protected set; }

    public SSkill(string _name, int lvl, double xp, Skill.AbilityOrigin origin)
    {
        Name = _name;
        Lvl = lvl;
        XP = xp;
        Flg = origin;
    }
}
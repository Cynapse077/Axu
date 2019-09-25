using System.IO;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using LitJson;
using UnityEngine;

[System.Serializable]
[MoonSharpUserData]
public class Ability : IAsset
{
    public const int maxLvl = 10;
    public const int XPToNext = 1000;

    public string ID { get; set; }
    public string ModID { get; set; }
    public string Name, Description;
    public string iconPath;
    public int staminaCost, cooldown, maxCooldown, level = 1, range = 20;
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
    Sprite cachedSprite;

    public DiceRoll totalDice
    {
        get
        {
            if (dice == null)
            {
                return new DiceRoll(0, 0, 0);
            }

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

    public Sprite IconSprite
    {
        get
        {
            if (cachedSprite != null)
                return cachedSprite;

            Texture2D icon = new Texture2D(0, 0);
            string path = Path.Combine(Application.streamingAssetsPath, iconPath);

            if (!File.Exists(path))
            {
                Debug.LogError(path + " has no sprite.");
                return null;
            }

            byte[] imageBytes = File.ReadAllBytes(path);
            icon.LoadImage(imageBytes);
            cachedSprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));

            return cachedSprite;
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

    public Ability()
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

    public Ability(JsonData dat)
    {
        tags = new List<AbilityTags>();
        FromJson(dat);
    }

    public Ability(Ability other)
    {
        CopyFrom(other);
        origin = AbilityOrigin.None;
    }

    void CopyFrom(Ability other)
    {
        Name = other.Name;
        ID = other.ID;
        iconPath = other.iconPath;
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

    public void FromJson(JsonData dat)
    {
        if (dat.ContainsKey("Name"))
            Name = dat["Name"].ToString();
        if (dat.ContainsKey("ID"))
            ID = dat["ID"].ToString();
        if (dat.ContainsKey("Description"))
            Description = dat["Description"].ToString();

        dat.TryGetString("Icon", out iconPath);
        dat.TryGetInt("Stamina Cost", out staminaCost, staminaCost);
        dat.TryGetInt("Time Cost", out timeCost, timeCost);
        dat.TryGetInt("Cooldown", out maxCooldown, maxCooldown);
        dat.TryGetEnum("Damage Type", out damageType, damageType);
        dat.TryGetEnum("Cast Type", out castType, castType);
        dat.TryGetBool("Levels Up", out CanLevelUp, CanLevelUp);
        dat.TryGetInt("Range", out range, range);

        if (dat.ContainsKey("Dice"))
        {
            dice = DiceRoll.GetByString(dat["Dice"].ToString());
        }

        if (dat.ContainsKey("Dice Scale"))
        {
            dicePerLevel = DiceRoll.GetByString(dat["Dice Scale"].ToString());
        }
        
        if (dat.ContainsKey("Tags"))
        {
            tags = new List<AbilityTags>();

            for (int j = 0; j < dat["Tags"].Count; j++)
            {
                string ef = dat["Tags"][j].ToString();
                AddTag(ef.ToEnum<AbilityTags>());
            }
        }

        if (dat.ContainsKey("Script"))
        {
            luaAction = new LuaCall(dat["Script"].ToString());
        }

        if (dat.ContainsKey("AI"))
        {
            aiAction = new LuaCall(dat["AI"].ToString());
        }
    }

    public IEnumerable<string> LoadErrors()
    {
        yield break;
    }

    public SSkill ToSerializedSkill()
    {
        return new SSkill(ID, level, XP, cooldown, origin);
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
        Book = 1 << 0,
        Trait = 1 << 1,
        Item = 1 << 2, 
        Cybernetic = 1 << 3
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
    public Ability.AbilityOrigin Flg { get; protected set; }
    public int CD;

    public SSkill(string _name, int lvl, double xp, int cooldown, Ability.AbilityOrigin origin)
    {
        Name = _name;
        Lvl = lvl;
        XP = xp;
        Flg = origin;
        CD = cooldown;
    }
}
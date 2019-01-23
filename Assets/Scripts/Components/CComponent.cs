﻿using System;
using System.Collections.Generic;
using LitJson;

[Serializable]
public class CComponent
{
    public string ID;

    public CComponent() { }
    public CComponent(string _id)
    {
        ID = _id;
    }

    public CComponent Clone()
    {
        return (CComponent)MemberwiseClone();
    }

    public static CComponent FromJson(JsonData data)
    {
        string id = data["ID"].ToString();
        JsonReader reader = new JsonReader(data.ToJson());

        switch (id)
        {
            case "Charges": return JsonMapper.ToObject<CCharges>(reader);
            case "Rot": return JsonMapper.ToObject<CRot>(reader);
            case "Corpse": return JsonMapper.ToObject<CCorpse>(reader);
            case "Ability": return JsonMapper.ToObject<CAbility>(reader);
            case "Equipped": return JsonMapper.ToObject<CEquipped>(reader);
            case "Firearm": return JsonMapper.ToObject<CFirearm>(reader);
            case "Coordinate": return JsonMapper.ToObject<CCoordinate>(reader);
            case "Console": return JsonMapper.ToObject<CConsole>(reader);
            case "LuaEvent": return JsonMapper.ToObject<CLuaEvent>(reader);
            case "LiquidContainer": return JsonMapper.ToObject<CLiquidContainer>(reader);
            case "Block": return JsonMapper.ToObject<CBlock>(reader);
            case "Coat": return JsonMapper.ToObject<CCoat>(reader);
            case "ModKit": return JsonMapper.ToObject<CModKit>(reader);
            case "ItemLevel": return JsonMapper.ToObject<CItemLevel>(reader);
            case "Cybernetic": return JsonMapper.ToObject<CCybernetic>(reader);

            default: return null;
        }
    }
}

[Serializable]
public class CCharges : CComponent
{
    public int current;
    public int max;

    public CCharges()
    {
        ID = "Charges";
    }

    public CCharges(int _cur, int _max)
    {
        ID = "Charges";
        current = _cur;
        max = _max;
    }
}

[Serializable]
public class CRot : CComponent
{
    public int current;

    public CRot()
    {
        ID = "Rot";
    }
    public CRot(int _cur)
    {
        ID = "Rot";
        current = _cur;
    }
}

[Serializable]
public class CCorpse : CComponent
{
    public List<SBodyPart> parts;
    public string owner;
    public bool cann, rad, lep, vamp;

    public CCorpse()
    {
        ID = "Corpse";
    }
    public CCorpse(List<BodyPart> _parts, string _owner, bool _cannibalism, bool _rad, bool _lep, bool _vamp)
    {
        ID = "Corpse";
        owner = _owner;
        cann = _cannibalism;
        rad = _rad;
        lep = _lep;
        vamp = _vamp;

        parts = new List<SBodyPart>();

        for (int i = 0; i < _parts.Count; i++)
        {
            parts.Add(_parts[i].ToSimpleBodyPart());
        }
    }

    public CCorpse(List<SBodyPart> _parts, string _owner, bool _cannibalism)
    {
        ID = "Corpse";
        owner = _owner;
        cann = _cannibalism;

        parts = new List<SBodyPart>(_parts);
    }
}

[Serializable]
public class CEquipped : CComponent
{
    public string itemID;
    public string baseItemID;

    public CEquipped()
    {
        ID = "Equipped";
    }
    public CEquipped(string _itemID, string _baseID)
    {
        ID = "Equipped";
        itemID = _itemID;
        baseItemID = _baseID;
    }
}

[Serializable]
public class CFirearm : CComponent
{
    public int curr, max, shots;
    public string ammoID;

    public CFirearm()
    {
        ID = "Firearm";
    }

    public CFirearm(int _cur, int _max, int _shots, string _ammoID)
    {
        ID = "Firearm";
        curr = _cur;
        max = _max;
        shots = _shots;
        ammoID = _ammoID;
    }

    public int Reload(int amount)
    {
        int maxAmmo = max;
        int amountInMag = curr;
        int amountUsed = 0;

        while (amountInMag < maxAmmo)
        {
            if (amount > 0)
            {
                amountUsed++;
                amountInMag++;
                amount--;
            }
            else
            {
                curr = amountInMag;
                return amountUsed;
            }
        }

        curr = amountInMag;
        return amountUsed;
    }
}

[Serializable]
public class CAbility : CComponent
{
    public string abID;
    public int abLvl;

    public CAbility() { ID = "Ability"; }
    public CAbility(string _abID, int _abLvl)
    {
        ID = "Ability";
        abID = _abID;
        abLvl = _abLvl;
    }
}

[Serializable]
public class CCoordinate : CComponent
{
    public Coord wPos;
    public Coord lPos;
    public int Ele;
    public string aNa;
    public bool isSet;

    public CCoordinate() { ID = "Coordinate"; }
    public CCoordinate(Coord _wPos, Coord _lPos, int _elev, string _areaName, bool _isSet)
    {
        ID = "Coordinate";
        wPos = _wPos;
        lPos = _lPos;
        Ele = _elev;
        aNa = _areaName;
        isSet = _isSet;
    }

    public string GetInfo()
    {
        string s = (isSet) ? (aNa + " - \n@ " + lPos.ToString()) : LocalizationManager.GetLocalizedContent("IT_NotSet")[0];
        return s;
    }

    public void Activate(Entity entity)
    {
        if (isSet)
        {
            entity.ForcePosition(new Coord(lPos.x, lPos.y));

            World.tileMap.worldCoordX = wPos.x;
            World.tileMap.worldCoordY = wPos.y;
            World.tileMap.currentElevation = Ele;

            World.tileMap.HardRebuild();
            World.tileMap.SoftRebuild();

            CombatLog.SimpleMessage("Return_Tele");
            World.userInterface.CloseWindows();
            entity.BeamDown();
        }
        else if (World.objectManager.SafeToRest())
        {
            wPos = World.tileMap.WorldPosition;
            Ele = World.tileMap.currentElevation;
            aNa = World.tileMap.TileName();
            lPos = new Coord(entity.posX, entity.posY);

            CombatLog.SimpleMessage("Return_Link");
            isSet = true;
        }

        ObjectManager.player.GetComponent<PlayerInput>().CheckMinimap();
    }
}

[Serializable]
public class CConsole : CComponent
{
    public string action;
    public string command;

    public CConsole() { ID = "Console"; }
    public CConsole(string _action, string _command)
    {
        ID = "Console";
        action = _action;
        command = _command;
    }

    public void RunCommand(string actionName)
    {
        if (action == actionName)
        {
            World.objectManager.GetComponent<Console>().ParseTextField(command);
            World.userInterface.CloseWindows();
        }
    }
}

[Serializable]
public class CLuaEvent : CComponent
{
    public string evName;
    public string file;
    public string func;
    public string xprm;

    public CLuaEvent() { ID = "LuaEvent"; }
    public CLuaEvent(string eventName, string fle, string fnc, string xp)
    {
        ID = "LuaEvent";
        evName = eventName;
        file = fle;
        func = fnc;
        xprm = xp;
    }

    public void CallEvent(string eventToCall)
    {
        if (eventToCall == evName)
        {
            if (string.IsNullOrEmpty(xprm))
            {
                LuaManager.CallScriptFunction(file, func, ObjectManager.playerEntity);
            }
            else
            {
                LuaManager.CallScriptFunction(file, func, ObjectManager.playerEntity, xprm);
            }
        }
    }

    public void CallEvent_Params(string eventToCall, params object[] obj)
    {
        if (eventToCall == evName)
        {
            LuaManager.CallScriptFunction(file, func, obj);
        }
    }
}

[Serializable]
public class CBlock : CComponent
{
    public int level;

    public CBlock()
    {
        ID = "Block";
        level = 1;
    }

    public CBlock(int lvl)
    {
        ID = "Block";
        level = lvl;
    }
}

[Serializable]
public class CCoat : CComponent
{
    public int strikes;
    public Liquid liquid;

    public CCoat() { ID = "Coat"; }
    public CCoat(int s, Liquid l)
    {
        ID = "Coat";
        strikes = s;
        liquid = l;
    }

    public void OnStrike(Stats stats)
    {
        if (strikes > 0)
        {
            liquid.Splash(stats);
            strikes--;
        }
    }
}

[Serializable]
public class CLiquidContainer : CComponent
{
    public int capacity;
    public SLiquid liq {
        get
        {
            if (liquid == null)
            {
                return null;
            }
            return liquid.ToSLiquid();
        }
        set
        {
            if (value != null)
            {
                liquid = ItemList.GetLiquidByID(value.ID, value.units);
            }
        }
    }

    Liquid liquid;

    public CLiquidContainer()
    {
        ID = "LiquidContainer";
        capacity = 1;
        liquid = null;
    }

    public CLiquidContainer(int cap)
    {
        ID = "LiquidContainer";
        capacity = cap;
        liquid = null;
    }

    public void SetLiquidVolume(int amt)
    {
        liquid.units = amt;
    }

    public int Fill(Liquid l)
    {
        if (currentAmount() >= capacity)
        {
            return 0;
        }

        if (liquid == null)
        {
            liquid = new Liquid(l, 0);
        }

        int amount = l.units;
        int poured = 0;

        for (int i = 0; i < amount; i++)
        {
            liquid.units++;
            l.units--;
            poured++;

            if (roomLeft() <= 0 || currentAmount() >= capacity)
            {
                break;
            }
        }

        if (l.ID != liquid.ID)
        {
            liquid = Liquid.Mix(new Liquid(l, 0), new Liquid(liquid, currentAmount()));
        }

        return poured;
    }

    public void Pour(Entity ent)
    {
        if (liquid != null)
        {
            liquid.Splash(ent.stats);
            liquid = null;
        }
    }

    public void Drink(Entity ent)
    {
        if (liquid != null)
        {
            liquid.Drink(ent.stats);
            liquid.units--;
        }

        CheckLiquid();
    }

    public void CheckLiquid()
    {
        if (liquid != null && liquid.units <= 0)
        {
            liquid = null;
        }
    }

    public void SetLiquid(Liquid l)
    {
        liquid = l;
    }

    public int roomLeft()
    {
        return (liquid != null) ? capacity - liquid.units : capacity;
    }

    public int currentAmount()
    {
        return (liquid != null) ? liquid.units : 0;
    }

    public bool isFull()
    {
        return currentAmount() >= capacity;
    }

    public bool isEmpty()
    {
        return liquid == null || liquid.units <= 0;
    }

    public string GetInfo()
    {
        string s = (isEmpty()) ? LocalizationManager.GetContent("IT_LiquidUnits_Empty") : LocalizationManager.GetContent("IT_LiquidUnits") + "\n(" + liquid.Description + ")";

        if (s.Contains("[INPUT1]"))
        {
            s = s.Replace("[INPUT1]", liquid.units.ToString());
        }

        if (s.Contains("[INPUT2]"))
        {
            s = s.Replace("[INPUT2]", liquid.Name);
        }

        return s;
    }
}

[Serializable]
public class CModKit : CComponent
{
    public string modID;

    public CModKit() { ID = "ModKit"; }
    public CModKit(string mID)
    {
        ID = "ModKit";
        modID = mID;
    }

    public List<Item> ItemsICanAddTo(List<Item> it)
    {
        ItemModifier mod = ItemList.GetModByID(modID);

        if (mod == null)
            return null;

        List<Item> newList = new List<Item>();

        for (int i = 0; i < it.Count; i++)
        {
            if (mod.CanAddToItem(it[i]))
            {
                newList.Add(it[i]);
            }
        }

        return newList;
    }
}

[Serializable]
public class CItemLevel : CComponent
{
    private const double xpToNext = 1000.0;
    private const int maxLevel = 5;

    public int level = 1;
    public double xp = 0.0;

    public CItemLevel()
    {
        ID = "ItemLevel";
        level = 1;
        xp = 0.0;
    }

    public CItemLevel(int _lvl, int _xp)
    {
        ID = "ItemLevel";
        level = _lvl;
        xp = _xp;
    }

    public void AddXP(double _amount)
    {
        if (level < maxLevel)
        {
            double amount = (_amount / level) + 0.1;
            xp += amount;

            while (xp >= xpToNext)
            {
                if (level >= maxLevel)
                {
                    xp = 0;
                    break;
                }

                xp -= xpToNext;
                level++;
            }

        }
        else
        {
            level = maxLevel;
            xp = 0;
        }
    }

    public int DamageBonus()
    {
        return level - 1;
    }

    public string Display()
    {      
        if (level < maxLevel)
        {
            double xpPercent = Math.Round(xp / 10.0, 2);
            return string.Format("<color=cyan>Level</color> <color=yellow>{0}</color> <color=grey>({1} %xp)</color>", level, xpPercent);
        }
        else
        {
            return "Level " + level.ToString() + "(MAX)";
        }
    }
}

[Serializable]
public class CCybernetic : CComponent
{
    public string CID;

    public CCybernetic()
    {
        ID = "Cybernetic";
    }

    public CCybernetic(string id)
    {
        ID = "Cybernetic";
        CID = id;
    }
}
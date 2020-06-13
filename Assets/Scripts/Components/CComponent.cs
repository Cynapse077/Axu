using System;
using System.Linq;
using System.Collections.Generic;
using LitJson;

[Serializable]
public class CComponent
{
    public string ID;

    public CComponent() { }
    public CComponent(string id)
    {
        ID = id;
    }

    public CComponent Clone()
    {
        return (CComponent)MemberwiseClone();
    }

    public virtual void OnRemove() { }

    public virtual string ExtraInfo()
    {
        return string.Empty;
    }

    public static CComponent FromJson(JsonData data)
    {
        if (!data.ContainsKey("ID"))
        {
            return null;
        }

        string id = data["ID"].ToString();
        JsonReader reader = new JsonReader(data.ToJson());

        switch (id)
        {
            case "Charges": return JsonMapper.ToObject<CCharges>(reader);
            case "RechargeTurns": return JsonMapper.ToObject<CRechargeTurns>(reader);
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
            case "Requirement": return JsonMapper.ToObject<CRequirement>(reader);
            case "DNAHolder": return JsonMapper.ToObject<CDNAHolder>(reader);
            case "OnHitAddStatus": return JsonMapper.ToObject<COnHitAddStatus>(reader);
            case "LocationMap": return JsonMapper.ToObject<CLocationMap>(reader);
            case "Ammo": return JsonMapper.ToObject<CAmmo>(reader);
            case "Disguise": return JsonMapper.ToObject<CDisguise>(reader);
            case "Tags": return JsonMapper.ToObject<CTags>(reader);

            default: return null;
        }
    }
}

[Serializable]
public class CCharges : CComponent
{
    public int current;
    public int max;

    public CCharges() : base("Charges") { }

    public CCharges(int _max) : base("Charges")
    {
        current = max = _max;
    }

    public CCharges(int _cur, int _max) : base("Charges")
    {
        current = _cur;
        max = _max;
    }
}

[Serializable]
public class CRechargeTurns : CComponent
{
    public int current;
    public int max;

    public CRechargeTurns() : base("RechargeTurns")
    {
        World.turnManager.incrementTurnCounter += OnTurn;
    }

    public CRechargeTurns(int current, int max) : base("RechargeTurns")
    {
        this.current = current;
        this.max = max;

        if (current > max)
        {
            current = max;
        }

        World.turnManager.incrementTurnCounter += OnTurn;
    }

    void OnTurn()
    {
        if (current < max)
        {
            current++;
        }

        if (current > max)
        {
            current = max;
        }
    }

    public override void OnRemove()
    {
        World.turnManager.incrementTurnCounter -= OnTurn;
    }

    public override string ExtraInfo()
    {
        if (max <= 0)
        {
            return "Charge: 0%";
        }

        int percent = (int)(current / (float)max * 100f);

        return "Charge: " + UserInterface.ColorByPercent(percent.ToString() + "%", percent);
    }
}

[Serializable]
public class CRot : CComponent
{
    public int current;

    public CRot() : base("Rot") { }
    public CRot(int _cur) : base("Rot")
    {
        current = _cur;
    }

    public override string ExtraInfo()
    {
        return string.Format("IT_Spoils".Localize(), current);
    }
}

[Serializable]
public class CCorpse : CComponent
{
    public List<SBodyPart> parts;
    public string owner;
    public bool cann, rad, lep, vamp;

    public CCorpse() : base("Corpse") { }
    public CCorpse(List<BodyPart> _parts, string _owner, bool _cannibalism, bool _rad, bool _lep, bool _vamp) : base("Corpse")
    {
        owner = _owner;
        cann = _cannibalism;
        rad = _rad;
        lep = _lep;
        vamp = _vamp;

        parts = new List<SBodyPart>();

        for (int i = 0; i < _parts.Count; i++)
        {
            parts.Add(_parts[i].ToSerializedBodyPart());
        }
    }

    public CCorpse(List<SBodyPart> _parts, string _owner, bool _cannibalism) : base("Corpse")
    {
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

    public CEquipped() : base("Equipped") { }
    public CEquipped(string _itemID, string _baseID) : base("Equipped")
    {
        itemID = _itemID;
        baseItemID = _baseID;
    }

    public override string ExtraInfo()
    {
        if (!baseItemID.NullOrEmpty())
        {
            return "Equipped: " + ItemList.GetItemByID(baseItemID).DisplayName();
        }

        return base.ExtraInfo();
    }
}

[Serializable]
public class CFirearm : CComponent
{
    public readonly int max;
    public readonly int shots;
    public readonly string ammoID;
    public int curr;
    public string currentAmmo;

    public CFirearm() : base("Firearm") { }
    public CFirearm(int _cur, int _max, int _shots, string _ammoID) : base("Firearm")
    {
        curr = _cur;
        max = _max;
        shots = _shots;
        ammoID = _ammoID;
        currentAmmo = _ammoID;
    }

    public void Shoot(int amt)
    {
        curr -= amt;

        if (curr <= 0)
        {
            currentAmmo = null;
        }
    }

    public void Unload()
    {
        curr = 0;
        currentAmmo = null;
    }

    public int Reload(int amount, Item ammo)
    {
        int maxAmmo = max;
        int amountInMag = curr;
        int amountUsed = 0;

        if (amount > 0)
        {
            currentAmmo = ammo.ID;
        }

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

    public override string ExtraInfo()
    {
        if (currentAmmo.NullOrEmpty())
        {
            return "Loaded: <color=grey>None</color>";
        }

        return "Loaded: " + ItemList.GetItemByID(currentAmmo).DisplayName();
    }
}

[Serializable]
public class CAbility : CComponent
{
    public string abID;
    public int abLvl;

    public CAbility() : base("Ability") { }
    public CAbility(string _abID, int _abLvl) : base("Ability")
    {
        abID = _abID;
        abLvl = _abLvl;
    }

    public override string ExtraInfo()
    {
        Ability skill = GameData.Get<Ability>(abID);

        if (skill != null)
        {
            string s = "IT_Ability".Localize();
            return s.Replace("[INPUT]", skill.Name);
        }

        return base.ExtraInfo();
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

    public CCoordinate() : base("Coordinate") { }
    public CCoordinate(Coord _wPos, Coord _lPos, int _elev, string _areaName, bool _isSet) : base("Coordinate")
    {
        wPos = _wPos;
        lPos = _lPos;
        Ele = _elev;
        aNa = _areaName;
        isSet = _isSet;
    }

    public override string ExtraInfo()
    {
        return isSet ? (aNa + " - \n@ " + lPos.ToString()) : "IT_NotSet".Localize();
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

        World.playerInput.CheckMinimap();
    }
}

[Serializable]
public class CConsole : CComponent
{
    public string action;
    public string command;

    public CConsole() : base("Console") { }
    public CConsole(string _action, string _command) : base("Console")
    {
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
    public LuaCall luaCall;

    public CLuaEvent() : base("LuaEvent") { }
    public CLuaEvent(string eventName, string script) : base("LuaEvent")
    {
        evName = eventName;
        luaCall = new LuaCall(script);
    }

    public void CallEvent(string eventToCall, params object[] obj)
    {
        if (eventToCall == evName)
        {
            LuaManager.CallScriptFunction(luaCall, obj);
        }
    }
}

[Serializable]
public class CBlock : CComponent
{
    public int level = 1;

    public CBlock() : base("Block") { }
    public CBlock(int lvl) : base("Block")
    {
        level = lvl;
    }

    public override string ExtraInfo()
    {
        string s = "IT_Block".Localize();

        if (s.Contains("[INPUT]"))
        {
            s = s.Replace("[INPUT]", (level * 5).ToString());
        }

        return s;
    }
}

[Serializable]
public class CCoat : CComponent
{
    public int strikes;
    public Liquid liquid;

    public CCoat() : base("Coat") { }
    public CCoat(int s, Liquid l) : base("Coat")
    {
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

    public override string ExtraInfo()
    {
        return string.Format("IT_Coat".Localize(), liquid.Name, strikes);
    }
}

[Serializable]
public class CLiquidContainer : CComponent
{
    public int capacity;
    Liquid liquid;

    public SLiquid sLiquid
    {
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
        if (FilledUnits() >= capacity)
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

            if (RoomLeft() <= 0 || FilledUnits() >= capacity)
            {
                break;
            }
        }

        if (l.ID != liquid.ID)
        {
            liquid = Liquid.Mix(new Liquid(l, 0), new Liquid(liquid, FilledUnits()));
        }

        return poured;
    }

    public void Pour(Entity ent)
    {
        if (liquid != null)
        {
            liquid.Splash(ent.stats);
            liquid.units--;

            CheckLiquid();
        }
    }

    public void Drink(Entity ent)
    {
        if (liquid != null && !IsEmpty())
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

    public int RoomLeft()
    {
        return (liquid != null) ? capacity - liquid.units : capacity;
    }

    public int FilledUnits()
    {
        return (liquid != null) ? liquid.units : 0;
    }

    public bool IsFull()
    {
        return FilledUnits() >= capacity;
    }

    public bool IsEmpty()
    {
        return liquid == null || liquid.units <= 0;
    }

    public override string ExtraInfo()
    {
        string s = IsEmpty() ? "IT_LiquidUnits_Empty".Localize() : "IT_LiquidUnits".Localize() + "\n(" + liquid.Description + ")";

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
        {
            return null;
        }

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

    public override string ExtraInfo()
    {
        ItemModifier m = ItemList.GetModByID(modID);

        if (m != null)
        {
            return m.name + ": " + m.description;
        }

        return base.ExtraInfo();
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

    public void AddXP(double amt)
    {
        if (level < maxLevel)
        {
            double amount = (amt / level) + 0.1;
            xp += amount;

            while (xp >= xpToNext)
            {
                if (level >= maxLevel)
                {
                    xp = 0.0;
                    break;
                }

                xp -= xpToNext;
                level++;
            }
        }
        else
        {
            level = maxLevel;
            xp = 0.0;
        }
    }

    public int DamageBonus()
    {
        return level - 1;
    }

    public override string ExtraInfo()
    {      
        if (level < maxLevel)
        {
            return string.Format("LivingWeaponLevel".Localize(), level, Math.Round(xp / 10.0, 2));
        }
        else
        {
            return "LevelMax".Localize().Format(level.ToString());
        }
    }
}

[Serializable]
public class CRequirement : CComponent
{
    public List<StringInt> req;

    public CRequirement()
    {
        ID = "Requirement";
        req = new List<StringInt>();
    }

    public bool CanUse(Stats stats)
    {
        if (!stats.entity.isPlayer)
        {
            return true;
        }

        for (int i = 0; i < req.Count; i++)
        {
            if (!CanUse(stats, i))
            {
                return false;
            }
        }

        return true;
    }

    public bool CanUse(Stats stats, int i)
    {
        if (!stats.entity.isPlayer)
        {
            return true;
        }

        if (stats.Attributes.ContainsKey(req[i].String))
        {
            if (stats.Attributes[req[i].String] < req[i].Int)
            {
                return false;
            }
            else if (stats.proficiencies != null)
            {
                List<WeaponProficiency> profs = stats.proficiencies.GetProfs();

                if (profs.Any(x => x.name == req[i].String))
                {
                    if (profs.Find(x => x.name == req[i].String).level < req[i].Int)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}

[Serializable]
public class CDNAHolder : CComponent
{
    string npc;

    public CDNAHolder()
    {
        ID = "DNAHolder";
        npc = null;
    }

    public CDNAHolder(NPC n)
    {
        ID = "DNAHolder";
        npc = n.ID;
    }

    public void SetNPC(NPC n)
    {
        npc = n.ID;
    }

    public NPC_Blueprint GetNPC()
    {
        if (IsEmpty())
        {
            return null;
        }

        return GameData.Get<NPC_Blueprint>(npc);
    }

    public bool IsEmpty()
    {
        return npc.NullOrEmpty();
    }

    public override string ExtraInfo()
    {
        if (IsEmpty())
        {
            return "DNA: <color=grey>(Empty)</color>";
        }

        NPC_Blueprint bp = GameData.Get<NPC_Blueprint>(npc);
        if (bp == null)
        {
            return "DNA: <color=grey>(Empty)</color>";
        }

        return "DNA: " + bp.name;
    }
}

public class COnHitAddStatus : CComponent
{
    readonly string statusID;
    readonly IntRange turns;
    readonly int chance;

    public COnHitAddStatus()
    {
        ID = "OnHitAddStatus";
        statusID = null;
        turns = new IntRange(0, 0);
        chance = 0;
    }

    public COnHitAddStatus(string _statusID, IntRange _turns, int _chance)
    {
        ID = "OnHitAddStatus";
        statusID = _statusID;
        turns = _turns;
        chance = _chance;
    }

    public void TryAddToEntity(Entity target)
    {
        if (!statusID.NullOrEmpty() && target != null)
        {
            if (RNG.Chance(chance))
            {
                target.stats.AddStatusEffect(statusID, turns.GetRandom());
            }
        }
    }

    public override string ExtraInfo()
    {
        if (statusID.NullOrEmpty())
        {
            return null;
        }

        return "ChanceToAddStatus".Localize().Format(chance.ToString(), statusID);
    }
}

[Serializable]
public class CLocationMap : CComponent
{
    readonly string zoneID;
    readonly string questID;

    public CLocationMap()
    {
        ID = "LocationMap";
    }

    public CLocationMap(string zID, string qID)
    {
        ID = "LocationMap";
        zoneID = zID;
        questID = qID;
    }

    public void OnUse()
    {
        if (zoneID.NullOrEmpty())
        {
            CombatLog.SimpleMessage("Map_Fail");
            return;
        }

        Zone_Blueprint zb = World.worldMap.worldMapData.GetZone(zoneID);

        if (zb == null)
        {
            CombatLog.SimpleMessage("Map_Fail");
            return;
        }

        Coord pos = World.worldMap.worldMapData.PlaceZone(zb);

        if (pos != null)
        {
            World.tileMap.DeleteScreen(pos.x, pos.y);
            World.worldMap.worldMapData.NewPostGenLandmark(pos, zoneID);

            if (!questID.NullOrEmpty())
            {
                if (GameData.TryGet(questID, out Quest q))
                {
                    Quest newQuest = q.Clone();
                    newQuest.goals = new Goal[1] { new GoToGoal_Specific(newQuest, pos, 0) };
                    newQuest.AddEvent(QuestEvent.EventType.OnFail, new RemoveSpecificLocationEvent(pos));
                    ObjectManager.playerJournal.StartQuest(newQuest);
                }
                else
                {
                    CombatLog.SimpleMessage("Map_Fail");
                }
            }
        }
    }
}

[Serializable]
public class CDisguise : CComponent
{
    public readonly string factionID;
    public readonly int strength;

    public CDisguise()
    {
        ID = "Disguise";
    }

    public CDisguise(string factionID, int strength)
    {
        ID = "Disguise";
        this.factionID = factionID;
        this.strength = strength;
    }

    public override string ExtraInfo()
    {
        Faction faction = GameData.Get<Faction>(factionID);

        if (faction == null)
        {
            return string.Empty;
        }

        return "IT_Disguise".Localize().Format(faction.name);
    }
}

[Serializable]
public class CAmmo : CComponent
{
    public readonly string ammoType;
    public readonly Damage extraDamage;
    readonly LuaCall onHit;

    public string AmmoType
    {
        get { return ammoType; }
    }

    public CAmmo()
    {
        ID = "Ammo";
    }

    public CAmmo(string ammoType, LuaCall onHit, Damage extraDamage)
    {
        ID = "Ammo";
        this.ammoType = ammoType;
        this.onHit = onHit;
        this.extraDamage = extraDamage;
    }

    public bool CanUseOn(Item item)
    {
        if (item.TryGetCComponent(out CFirearm cf))
        {
            return cf.ammoID == ammoType;
        }

        return false;
    }

    public void OnHit(Entity source, Coord c)
    {
        if (onHit != null)
        {
            LuaManager.CallScriptFunction(onHit, source, c);
        }
    }

    public override string ExtraInfo()
    {
        if (extraDamage != null && !extraDamage.IsEmpty)
        {
            return "ShotDamageBonus".Localize() + extraDamage.ToString();
        }

        return string.Empty;
    }
}

[Serializable]
public class CTags : CComponent
{
    public List<string> tags;

    public CTags()
    {
        ID = "Tags";
        tags = new List<string>();
    }

    public CTags(List<string> tags)
    {
        ID = "Tags";
        this.tags = tags;
    }

    public bool HasTag(string tag)
    {
        return tags.Contains(tag);
    }

    public void AddTag(string tag)
    {
        tags.Add(tag);
    }

    public void RemoveTag(string tag)
    {
        tags.Remove(tag);
    }
}
using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using Augments;

[MoonSharpUserData]
[Serializable]
public class BodyPart : IWeighted
{
    const int maxLevel = 5;

    public Body myBody;
    public string name;
    public string displayName;
    public int armor, level = 0;
    public ItemProperty slot;
    public BPTags flags;
    public List<Stat_Modifier> Attributes;
    public List<Wound> wounds;
    public Grip grip;
    public Hand hand;
    public Cybernetic cybernetic;
    public List<Grip> holdsOnMe = new List<Grip>();

    double currXP = 0.0, maxXP = 1500.0;
    int _weight;
    public Item equippedItem;
    string _baseName;
    bool _attached = true;

    public int Weight
    {
        get { return 30 - _weight; }
        set { _weight = value; }
    }

    public bool isAttached
    {
        get { return _attached; }
        protected set { _attached = value; }
    }

    public bool Crippled
    {
        get { return wounds != null && wounds.Count > 0; }
    }

    public bool external
    {
        get { return FlagsHelper.IsSet(flags, BPTags.External); }
    }

    public bool organic
    {
        get { return !FlagsHelper.IsSet(flags, BPTags.Synthetic);  }
    }

    public bool canWearGear
    {
        get { return !FlagsHelper.IsSet(flags, BPTags.CannotWearGear); }
    }

    public bool severable
    {
        get { return !FlagsHelper.IsSet(flags, BPTags.NonSeverable); }
    }

    public void SetXP(double xp, double max)
    {
        currXP = xp;
        maxXP = max;
    }

    public BodyPart(string na, ItemProperty itemSlot)
    {
        Attributes = new List<Stat_Modifier>();
        _baseName = na;
        name = na;
        _attached = true;
        equippedItem = ItemList.GetNone();
        slot = itemSlot;
        armor = 1;
        displayName = name;
        wounds = new List<Wound>();
    }

    public BodyPart(string na, bool att)
    {
        Attributes = new List<Stat_Modifier>();
        name = na;
        _attached = att;
        equippedItem = ItemList.GetNone();
        armor = 1;
        displayName = name;
        wounds = new List<Wound>();
    }

    public BodyPart(BodyPart other)
    {
        CopyFrom(other);
    }

    public void InflictPhysicalWound()
    {
        HashSet<DamageTypes> dts = new HashSet<DamageTypes>() { DamageTypes.Blunt };
        WoundMe(dts);
    }

    public void WoundMe(HashSet<DamageTypes> dts)
    {
        List<Wound> ws = TraitList.GetAvailableWounds(this, dts);

        if (ws.Count > 0)
        {
            Wound w = ws.GetRandom(SeedManager.combatRandom);
            w.Inflict(this);
        }
    }

    public Stat_Modifier GetStatMod(string search)
    {
        if (Attributes.Find(x => x.Stat == search) == null)
        {
            Attributes.Add(new Stat_Modifier(search, 0));
        }

        return Attributes.Find(x => x.Stat == search);
    }

    public void AddAttribute(string id, int amount)
    {
        if (Attributes.Find(x => x.Stat == id) != null)
            Attributes.Find(x => x.Stat == id).Amount += amount;
        else
            Attributes.Add(new Stat_Modifier(id, amount));
    }

    public void AddXP(Entity entity, double amount)
    {
        if (myBody == null)
        {
            myBody = entity.body;
        }

        if (level >= 5 || FlagsHelper.IsSet(flags, BPTags.Synthetic) || !isAttached || FlagsHelper.IsSet(flags, BPTags.External))
        {
            return;
        }

        currXP += amount;

        while (currXP > maxXP)
        {
            LevelUp(entity);
            currXP -= maxXP;
            maxXP *= 1.25;
        }
    }

    public void LevelUp(Entity entity)
    {
        if (level >= 4)
        {
            return;
        }

        level++;
        CombatLog.NameMessage("Limb_Stat_Gain", name);
        string stat = "";

        switch (slot)
        {
            case ItemProperty.Slot_Arm:
                stat = (level % 2 == 0) ? "Dexterity" : "Strength";
                break;
            case ItemProperty.Slot_Chest:
            case ItemProperty.Slot_Back:
                entity.stats.maxHealth += 3;
                entity.stats.maxStamina += 2;
                break;
            case ItemProperty.Slot_Head:
                stat = "Intelligence";
                break;
            case ItemProperty.Slot_Leg:
            case ItemProperty.Slot_Wing:
                stat = "Speed";
                break;
            case ItemProperty.Slot_Tail:
                stat = (level % 2 == 0) ? "Dexterity" : "Strength";
                break;
        }

        if (!string.IsNullOrEmpty(stat))
        {
            GetStatMod(stat).Amount++;
            entity.stats.Attributes[stat]++;
        }
    }

    public void Sever(Entity entity)
    {
        _attached = false;
        equippedItem.OnUnequip(entity, false);
        wounds.Clear();
        Remove(entity.stats);
    }

    public void Attach(Stats stats, bool showMessage = true)
    {
        _attached = true;
        name = _baseName;
        myBody = stats.entity.body;
        wounds.Clear();

        if (FlagsHelper.IsSet(flags, BPTags.Leprosy) && !stats.hasTrait("leprosy"))
        {
            if (showMessage)
            {
                Alert.NewAlert("Dis_Lep_Attach");
            }

            stats.InitializeNewTrait(TraitList.GetTraitByID("leprosy"));
        }
        else if (FlagsHelper.IsSet(flags, BPTags.Crystal) && !stats.hasTrait("crystal"))
        {
            if (showMessage)
            {
                Alert.NewAlert("Dis_Cry_Attach");
            }

            stats.InitializeNewTrait(TraitList.GetTraitByID("crystal"));
        }
        else if (FlagsHelper.IsSet(flags, BPTags.Vampire) && !stats.hasTrait("pre-vamp") && !stats.hasTrait("vmap"))
        {
            if (showMessage)
            {
                Alert.NewAlert("Dis_Vamp_Attach");
            }

            stats.InitializeNewTrait(TraitList.GetTraitByID("pre_vamp"));
        }

        for (int i = 0; i < Attributes.Count; i++)
        {
            stats.Attributes[Attributes[i].Stat] += Attributes[i].Amount;
        }
    }

    public void Remove(Stats stats)
    {
        wounds.Clear();
        FlagsHelper.UnSet(ref flags, BPTags.Synthetic);

        for (int i = 0; i < Attributes.Count; i++)
        {
            if (Attributes[i].Stat != "Hunger")
            {
                stats.Attributes[Attributes[i].Stat] -= Attributes[i].Amount;
            }
        }

        if (cybernetic != null)
        {
            cybernetic.Remove();
        }
    }

    public SBodyPart ToSerializedBodyPart()
    {
        if (equippedItem == null || equippedItem.ID == "none")
        {
            equippedItem = ItemList.GetNone();
        }

        SHand hnd = null;

        if (hand != null)
        {
            string baseItem = (!string.IsNullOrEmpty(hand.baseItem)) ? "fists" : hand.baseItem;

            hnd = new SHand(hand.EquippedItem.ToSerializedItem(), baseItem);
        }

        string cybID = (cybernetic == null) ? "" : cybernetic.ID;

        SItem equipped = equippedItem.ToSerializedItem();
        SBodyPart simple = new SBodyPart(name, flags, equipped, _attached, slot, Weight, Attributes, armor, level, currXP, maxXP, wounds, hnd, cybID);

        return simple;
    }

    public bool FreeToMove()
    {
        return (holdsOnMe.Count == 0);
    }

    public void TryBreakGrips()
    {
        List<Grip> gripsToBreak = new List<Grip>();

        foreach (Grip g in holdsOnMe)
        {
            if (g.GripBroken())
            {
                string message = LocalizationManager.GetContent("Gr_BreakGrip");
                message = message.Replace("[ATTACKER]", g.myPart.myBody.entity.MyName);
                message = message.Replace("[DEFENDER]", myBody.entity.MyName);
                message = (myBody.entity.isPlayer ? "<color=cyan>" : "<color=orange>") + message;
                CombatLog.NewMessage(message + "</color>");
                gripsToBreak.Add(g);
            }
        }

        foreach (Grip g in gripsToBreak)
        {
            g.myPart.ReleaseGrip(true);
        }
    }

    #region Grappling
    public void GrabPart(BodyPart part)
    {
        if (part == null || part.myBody == null || myBody == null)
        {
            return;
        }

        if (grip == null)
        {
            grip = new Grip(part, this);
        }
        else
        {
            grip.Release();
            grip.Grab(part);
        }

        string message = LocalizationManager.GetContent("Gr_Grab");
        message = message.Replace("[ATTACKER]", myBody.entity.MyName);
        message = message.Replace("[DEFENDER]", part.myBody.entity.MyName);
        message = message.Replace("[ATTACKER_LIMB]", displayName);
        message = message.Replace("[DEFENDER_LIMB]", part.displayName);
        message = (myBody.entity.isPlayer ? "<color=cyan>" : "<color=orange>") + message + "</color>";
        CombatLog.NewMessage(message);
    }

    public void ReleaseGrip(bool forced)
    {
        if (grip == null)
        {
            grip = new Grip(null, this);
        }

        if (!forced)
        {
            string message = LocalizationManager.GetContent("Gr_Release");
            message = message.Replace("[ATTACKER]", myBody.entity.MyName);
            message = message.Replace("[DEFENDER]", grip.heldPart.myBody.entity.MyName);
            message = message.Replace("[DEFENDER_LIMB]", grip.heldPart.name);
            message = (myBody.entity.isPlayer ? "<color=cyan>" : "<color=orange>") + message + "</color>";
            CombatLog.NewMessage(message);
        }

        grip.Release();
    }
    #endregion


    void CopyFrom(BodyPart other)
    {
        Attributes = other.Attributes;
        _baseName = other._baseName;
        _weight = other._weight;
        slot = other.slot;
        name = other.name;
        _attached = other.isAttached;
        equippedItem = new Item(other.equippedItem);
        armor = other.armor;
        displayName = name;
        myBody = other.myBody;
        wounds = other.wounds;
        flags = other.flags;
        cybernetic = other.cybernetic;

        if (other.hand != null)
        {
            hand = new Hand(other.hand);
        }

    }

    [MoonSharpUserData]
    public class Hand
    {
        public BodyPart arm;
        public Item EquippedItem;
        public string baseItem = "fists";

        public bool IsAttached
        {
            get { return (arm != null && arm.isAttached); }
        }

        public Hand(BodyPart _arm, Item _item, string _baseItem)
        {
            arm = _arm;
            EquippedItem = _item;
            baseItem = _baseItem;
        }

        public Hand(Hand other)
        {
            arm = other.arm;
            EquippedItem = other.EquippedItem;
            baseItem = other.baseItem;
        }

        public void SetEquippedItem(Item i, Entity entity)
        {
            if (EquippedItem != null && entity != null)
            {
                EquippedItem.OnUnequip(entity, this == entity.body.MainHand);
            }

            EquippedItem = i;

            if (entity != null && EquippedItem != null)
            {
                EquippedItem.OnEquip(entity.stats, this == entity.body.MainHand);
            }
        }

        public void RevertToBase(Entity entity)
        {
            if (EquippedItem != null && entity != null)
            {
                EquippedItem.OnUnequip(entity, this == entity.body.MainHand);
            }

            EquippedItem = ItemList.GetItemByID(baseItem);

            if (entity != null && EquippedItem != null)
            {
                EquippedItem.OnEquip(entity.stats, this == entity.body.MainHand);
            }
        }
    }

    public class Grip
    {
        public BodyPart myPart;
        public BodyPart heldPart;

        public Body HeldBody
        {
            get { return heldPart.myBody; }
        }

        public Grip(BodyPart part, BodyPart me)
        {
            myPart = me;
            Grab(part);
        }

        int GripStrength()
        {
            int str = 2;

            Body b = myPart.myBody;

            for (int i = 0; i < b.bodyParts.Count; i++)
            {
                if (b.bodyParts[i].hand != null && b.bodyParts[i].hand.EquippedItem.ID == b.bodyParts[i].hand.baseItem)
                {
                    str += b.bodyParts[i].GetStatMod("Strength").Amount;
                }
            }

            return str;
        }

        public bool GripBroken(int strPenalty = 0)
        {
            if (heldPart == null)
            {
                return true;
            }

            int rollFor = SeedManager.combatRandom.Next(0, GripStrength()) - strPenalty;

            if (myPart.Crippled)
            {
                rollFor -= 3;
            }

            int rollAgainst = SeedManager.combatRandom.Next(-2, HeldBody.entity.stats.Strength - 1);

            if (heldPart.Crippled)
            {
                rollAgainst -= 3;
            }

            return (rollAgainst > rollFor);
        }

        public void Release()
        {
            if (heldPart != null)
            {
                if (heldPart.holdsOnMe.Contains(this))
                {
                    heldPart.holdsOnMe.Remove(this);
                }

                heldPart = null;
            }
        }

        public void Grab(BodyPart part)
        {
            heldPart = part;

            if (heldPart != null)
            {
                heldPart.holdsOnMe.Add(this);
            }
        }
    }

    [Flags]
    public enum BPTags
    {
        None = 0,
        Synthetic = 1,
        External = 1 << 1,
        Crystal = 1 << 2,
        Vampire = 1 << 3,
        Leprosy = 1 << 4, 
        NonSeverable = 1 << 5,
        CannotWearGear = 1 << 6
    }
}

[Serializable]
public class SBodyPart
{
    public string Name { get; set; }
    public string Cyb { get; set; }
    public BodyPart.BPTags Flgs;
    public SItem item { get; set; }
    public int Lvl;
    public double[] XP;
    public int Ar { get; set; } //armor
    public int Size { get; set; }
    public bool Att { get; set; } //attached
    public ItemProperty Slot { get; set; }
    public SHand Hnd;
    public List<Stat_Modifier> Stats { get; set; }
    public List<Wound> Wounds { get; set; }

    public SBodyPart() { }
    public SBodyPart(string name, BodyPart.BPTags flags, SItem _item, bool attached, ItemProperty slot, int size, List<Stat_Modifier> stats, int armor,
        int level, double currXP, double maxXP, List<Wound> wnds, SHand hand, string cyberneticID)
    {
        Name = name;
        Flgs = flags;
        item = _item;
        Att = attached;
        Slot = slot;
        Size = size;
        Stats = stats;
        Ar = armor;
        Wounds = wnds;
        Lvl = level;
        Hnd = hand;
        Cyb = cyberneticID;
        XP = new double[] { currXP, maxXP };
    }
}

[Serializable]
public class SHand
{
    public SItem item;
    public string bItem;

    public SHand()
    {
        item = ItemList.GetItemByID("fists").ToSerializedItem();
        bItem = "fists";
    }

    public SHand(SItem it, string bi)
    {
        item = it;
        bItem = bi;
    }
}

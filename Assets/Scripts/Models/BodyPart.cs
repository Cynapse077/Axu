using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using Augments;
using Axu.Constants;

[MoonSharpUserData]
[Serializable]
public class BodyPart : IWeighted
{
    const int MaxLevel = 8;

    public Body myBody;
    public string name;
    public string displayName;
    public int armor, level = 0;
    public ItemProperty slot;
    public List<BPFlags> flags = new List<BPFlags>();
    public List<Stat_Modifier> Attributes = new List<Stat_Modifier>();
    public List<Wound> wounds = new List<Wound>();
    public Grip grip;
    public Hand hand;
    public Cybernetic cybernetic;
    public List<Grip> holdsOnMe = new List<Grip>();
    public Item equippedItem;

    double currXP = 0.0, maxXP = 1500.0;
    int _weight;
    string _baseName;
    bool _attached = true;

    public int Weight
    {
        get { return 30 - _weight; }
        set { _weight = value; }
    }

    public bool Attached
    {
        get { return _attached; }
        protected set { _attached = value; }
    }

    public bool Crippled => !wounds.NullOrEmpty();
    public bool Organic => !HasFlag(BPFlags.Synthetic);
    public bool CanWearGear => !HasFlag(BPFlags.CannotWearGear);
    public bool Severable => !HasFlag(BPFlags.NonSeverable) && Attached;

    public void SetXP(double xp, double max)
    {
        currXP = xp;
        maxXP = max;
    }

    public BodyPart(string na, ItemProperty itemSlot)
    {
        Attributes = new List<Stat_Modifier>();
        _baseName = name = na;
        _attached = true;
        equippedItem = ItemList.NoneItem;
        slot = itemSlot;
        armor = 1;
        displayName = name;
        wounds = new List<Wound>();
        flags = new List<BPFlags>();
    }

    public BodyPart(string na, bool att)
    {
        Attributes = new List<Stat_Modifier>();
        name = na;
        _attached = att;
        equippedItem = ItemList.NoneItem;
        armor = 1;
        displayName = name;
        wounds = new List<Wound>();
        flags = new List<BPFlags>();
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
            Wound w = ws.GetRandom();
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
        {
            Attributes.Find(x => x.Stat == id).Amount += amount;
        }
        else
        {
            Attributes.Add(new Stat_Modifier(id, amount));
        }
    }

    public void AddXP(Entity entity, double amount)
    {
        if (myBody == null)
        {
            myBody = entity.body;
        }

        if (level >= MaxLevel || !Organic || !Attached)
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
        if (level >= MaxLevel)
        {
            return;
        }

        level++;
        CombatLog.NameMessage("Limb_Stat_Gain", name);
        string stat = "";

        switch (slot)
        {
            case ItemProperty.Slot_Arm:
                stat = level % 2 == 0 ? C_Attributes.Dexterity : C_Attributes.Strength;
                break;
            case ItemProperty.Slot_Chest:
            case ItemProperty.Slot_Back:
                entity.stats.ChangeAttribute(C_Attributes.Health, 3);
                entity.stats.ChangeAttribute(C_Attributes.Stamina, 2);
                break;
            case ItemProperty.Slot_Head:
                stat = C_Attributes.Intelligence;
                break;
            case ItemProperty.Slot_Leg:
            case ItemProperty.Slot_Wing:
                stat = C_Attributes.MoveSpeed;
                break;
            case ItemProperty.Slot_Tail:
                stat = level % 2 == 0 ? C_Attributes.Dexterity : C_Attributes.MoveSpeed;
                break;
        }

        if (!stat.NullOrEmpty())
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

        if (!stats.entity.isPlayer)
        {
            showMessage = false;
        }

        if (HasFlag(BPFlags.Leprosy) && !stats.hasTrait(C_Traits.Leprosy))
        {
            if (showMessage)
            {
                Alert.NewAlert("Dis_Lep_Attach");
            }

            stats.InitializeNewTrait(TraitList.GetTraitByID(C_Traits.Leprosy));
        }
        else if (HasFlag(BPFlags.Crystal) && !stats.hasTrait(C_Traits.Crystalization))
        {
            if (showMessage)
            {
                Alert.NewAlert("Dis_Cry_Attach");
            }

            stats.InitializeNewTrait(TraitList.GetTraitByID(C_Traits.Crystalization));
        }
        else if (HasFlag(BPFlags.Vampire) && !stats.hasTrait(C_Traits.Vampirism) && !stats.hasTrait(C_Traits.Fledgling))
        {
            if (showMessage)
            {
                Alert.NewAlert("Dis_Vamp_Attach");
            }

            stats.InitializeNewTrait(TraitList.GetTraitByID(C_Traits.Fledgling));
        }

        for (int i = 0; i < Attributes.Count; i++)
        {
            stats.Attributes[Attributes[i].Stat] += Attributes[i].Amount;
        }
    }

    public void Remove(Stats stats)
    {
        wounds.Clear();
        RemoveFlag(BPFlags.Synthetic);

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

    public void AddFlag(BPFlags flag)
    {
        if (!flags.Contains(flag))
        {
            flags.Add(flag);
        }
    }

    public void RemoveFlag(BPFlags flag)
    {
        if (flags.Contains(flag))
        {
            flags.Remove(flag);
        }
    }

    public bool HasFlag(BPFlags flag)
    {
        return flags.Contains(flag);
    }

    public SBodyPart ToSerializedBodyPart()
    {
        if (equippedItem == null || equippedItem.ID == C_Items.None)
        {
            equippedItem = ItemList.NoneItem;
        }

        SHand hnd = null;

        if (hand != null)
        {
            string baseItem = !string.IsNullOrEmpty(hand.baseItem) ? C_Items.Fist : hand.baseItem;
            hnd = new SHand(hand.EquippedItem.ToSerializedItem(), baseItem);
        }

        string cybID = cybernetic != null ? cybernetic.ID : "";

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
                string message = "Gr_BreakGrip".Localize();
                message = message.Replace("[ATTACKER]", g.myPart.myBody.entity.MyName);
                message = message.Replace("[DEFENDER]", myBody.entity.MyName);
                CombatLog.NewMessage(message.Color(myBody.entity.isPlayer ? UnityEngine.Color.cyan : AxuColor.Orange));
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

        string message = "Gr_Grab".Localize();
        message = message.Replace("[ATTACKER]", myBody.entity.MyName);
        message = message.Replace("[DEFENDER]", part.myBody.entity.MyName);
        message = message.Replace("[ATTACKER_LIMB]", displayName);
        message = message.Replace("[DEFENDER_LIMB]", part.displayName);
        CombatLog.NewMessage(message.Color(myBody.entity.isPlayer ? UnityEngine.Color.cyan : AxuColor.Orange));
    }

    public void ReleaseGrip(bool forced)
    {
        if (grip == null)
        {
            grip = new Grip(null, this);
        }

        if (!forced)
        {
            string message = "Gr_Release".Localize();
            message = message.Replace("[ATTACKER]", myBody.entity.MyName);
            message = message.Replace("[DEFENDER]", grip.heldPart.myBody.entity.MyName);
            message = message.Replace("[DEFENDER_LIMB]", grip.heldPart.name);
            CombatLog.NewMessage(message.Color(myBody.entity.isPlayer ? UnityEngine.Color.cyan : AxuColor.Orange));
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
        _attached = other.Attached;
        equippedItem = new Item(other.equippedItem);
        armor = other.armor;
        displayName = name;
        myBody = other.myBody;
        wounds = other.wounds;
        flags = other.flags;
        cybernetic = other.cybernetic;

        if (other.hand != null)
        {
            hand = new Hand(other.hand) { arm = this };
        }
    }

    [MoonSharpUserData]
    public class Hand
    {
        public BodyPart arm;
        public Item EquippedItem;
        public string baseItem = C_Items.Fist;

        public bool IsAttached => arm != null && arm.Attached;
        public bool IsMainHand
        {
            get
            {
                if (arm == null || arm.myBody == null)
                {
                    return false;
                }

                return this == arm.myBody.MainHand;
            }
        }

        public Hand(BodyPart _arm, Item _item, string _baseItem)
        {
            arm = _arm;
            EquippedItem = new Item(_item);
            baseItem = _baseItem;
        }

        public Hand(Hand other)
        {
            arm = other.arm;
            EquippedItem = new Item(other.EquippedItem);
            baseItem = other.baseItem;
        }

        public void SetEquippedItem(Item i, Entity entity)
        {
            if (i == null)
            {
                i = ItemList.NoneItem;
            }

            if (arm.myBody == null)
            {
                arm.myBody = entity.body;
            }

            if (EquippedItem != null)
            {
                EquippedItem.OnUnequip(entity, IsMainHand);
            }

            EquippedItem = i;
            EquippedItem.OnEquip(entity.stats, IsMainHand);
        }

        public void RevertToBase(Entity entity)
        {
            if (arm.myBody == null)
            {
                arm.myBody = entity.body;
            }

            Item i = ItemList.GetItemByID(baseItem);

            if (EquippedItem != null)
            {
                EquippedItem.OnUnequip(entity, IsMainHand);
            }

            EquippedItem = i;
            EquippedItem.OnEquip(entity.stats, IsMainHand);
        }
    }

    public class Grip
    {
        public BodyPart myPart;
        public BodyPart heldPart;

        public Body HeldBody => heldPart.myBody;

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
                    str += b.bodyParts[i].GetStatMod(C_Attributes.Strength).Amount;
                }
            }

            return str;
        }

        public bool GripBroken(int strPenalty = 0)
        {
            const int penaltyForInjured = 3;

            if (heldPart == null)
            {
                return true;
            }

            int rollFor = RNG.Next(0, GripStrength()) - strPenalty;
            int rollAgainst = RNG.Next(-2, HeldBody.entity.stats.Strength - 1);

            if (myPart.Crippled)
            {
                rollFor -= penaltyForInjured;
            }

            if (heldPart.Crippled)
            {
                rollAgainst -= penaltyForInjured;
            }

            return rollAgainst > rollFor;
        }

        public bool CanStrangle()
        {
            return heldPart.slot == ItemProperty.Slot_Head && RNG.Chance(20) && myPart.myBody.entity.stats.Strength > 6;
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
    public enum BPFlags
    {
        Synthetic,
        Crystal,
        Vampire,
        Leprosy, 
        NonSeverable,
        CannotWearGear
    }
}

[Serializable]
public class SBodyPart
{
    public string Name { get; set; }
    public string Cyb { get; set; }
    public List<BodyPart.BPFlags> Flgs;
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
    public SBodyPart(string name, List<BodyPart.BPFlags> flags, SItem _item, bool attached, ItemProperty slot, int size, List<Stat_Modifier> stats, int armor,
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
        item = ItemList.GetItemByID(C_Items.Fist).ToSerializedItem();
        bItem = C_Items.Fist;
    }

    public SHand(SItem it, string bi)
    {
        item = it;
        bItem = bi;
    }
}

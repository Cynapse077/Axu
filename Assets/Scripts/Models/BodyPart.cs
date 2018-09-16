using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
[System.Serializable]
public class BodyPart : IWeighted {
	const int maxLevel = 5;

	public Body myBody;
	public string name;
	public string displayName;
	public ItemProperty slot;
	public List<Stat_Modifier> Attributes;
	public int armor, level = 0; 
	public bool held, external, organic, canWearGear = true;
	public TraitEffects effect; //Leprosy or Crystalization.
	public List<BPTags> bpTags;
	public List<Wound> wounds;
	public Grip grip;
	public Hand hand;
	public List<Grip> holdsOnMe = new List<Grip>();

	double currXP = 0.0,  maxXP = 1500.0;
	int _weight;
	Item _equippedItem;
	string _baseName;
	bool _attached = true, _severable = false;

	public int Weight {
		get { return 30 - _weight; }
		set { _weight = value; }
	}

	public bool isAttached {
		get { return _attached; }
		protected set { _attached = value; }
	}

	public Item equippedItem {
		get { return _equippedItem; }
		set { _equippedItem = value; }
	}

	public bool severable {
		get { return _severable; }
		set { _severable = value; }
	}

	public void SetXP(double xp, double max) {
		currXP = xp;
		maxXP = max;
	}
	
	public BodyPart(string na, bool severable, ItemProperty itemSlot, bool _external = false, bool _organic = true) {
		Attributes = new List<Stat_Modifier>();
		_baseName = na;
		name = na;
		_attached = true;
		_severable = severable;
		equippedItem = ItemList.GetNone();
		slot = itemSlot;
		external = _external;
		organic = _organic;
		armor = 1;
		held = false;
		displayName = name;
		bpTags = new List<BPTags>();
		wounds = new List<Wound>();
	}
	
	public BodyPart(string na, bool att) {
		Attributes = new List<Stat_Modifier>();
		name = na;
		_attached = att;
		equippedItem = ItemList.GetNone();
		armor = 1;
		organic = true;
		held = false;
		displayName = name;
		bpTags = new List<BPTags>();
		wounds = new List<Wound>();
	}

	public BodyPart(BodyPart other) {
		CopyFrom(other);
	}

	public void WoundMe(HashSet<DamageTypes> dts) {
		if (World.difficulty.Level == Difficulty.DiffLevel.Hunted || World.difficulty.Level == Difficulty.DiffLevel.Rogue) {
			List<Wound> ws = TraitList.GetAvailableWounds(this, dts);

			if (ws == null || ws.Count == 0)
				return;

			Wound w = ws.GetRandom(SeedManager.combatRandom);
			w.Inflict(this);
		}
	}

	public Stat_Modifier GetStatMod(string search) {
		if (Attributes.Find(x => x.Stat == search) == null)
			Attributes.Add(new Stat_Modifier(search, 0));

		return Attributes.Find(x => x.Stat == search);
	}

	public void AddAttribute(string id, int amount) {
		if (Attributes.Find(x => x.Stat == id) != null)
			Attributes.Find(x => x.Stat == id).Amount += amount;
		else 
			Attributes.Add(new Stat_Modifier(id, amount));
	}

	public void AddXP(Entity entity, double amount) {
		if (myBody == null)
			myBody = entity.body;
		
		if (level >= 5 || !organic || !isAttached || external)
			return;
		
		currXP += amount;

		while (currXP > maxXP) {
			LevelUp(entity);
			currXP -= maxXP;
			maxXP *= 1.25;
		}
	}

	public void LevelUp(Entity entity) {
		if (level >= 5)
			return;
		
		level ++;
		CombatLog.NameMessage("Limb_Stat_Gain", name);

		if (slot == ItemProperty.Slot_Arm) {
			if (level % 2 == 0) {
				GetStatMod("Dexterity").Amount ++;
				entity.stats.Attributes["Dexterity"] ++;
			} else {
				GetStatMod("Strength").Amount ++;
				entity.stats.Attributes["Strength"] ++;
			}
		} else if (slot == ItemProperty.Slot_Tail) {
			if (level % 2 == 0) {
				GetStatMod("Speed").Amount ++;
				entity.stats.Attributes["Speed"] ++;
			} else if (entity.isPlayer) {
				GetStatMod("Stealth").Amount ++;
				entity.stats.Attributes["Stealth"] ++;
			}
		} else if (slot == ItemProperty.Slot_Chest || slot == ItemProperty.Slot_Back) {
			GetStatMod("Endurance").Amount ++;
			entity.stats.Attributes["Endurance"] ++;

			entity.stats.maxHealth += 3;
			entity.stats.maxStamina += 2;
		} else if (slot == ItemProperty.Slot_Leg || slot == ItemProperty.Slot_Wing) {
			GetStatMod("Speed").Amount ++;
			entity.stats.Attributes["Speed"] ++;
		} else if (slot == ItemProperty.Slot_Head) {
			GetStatMod("Intelligence").Amount ++;
			entity.stats.Attributes["Intelligence"] ++;
		}
	}

	public void Sever(Entity entity) {
		_attached = false;
		_equippedItem.OnUnequip(entity, false);
		Remove(entity.stats);
	}

	public void Attach(Stats stats, bool showMessage = true) {
		_attached = true;
		_severable = true;
		name = _baseName;
		wounds.Clear();

		if (effect == TraitEffects.Leprosy && !stats.hasTrait("leprosy")) {
			if (showMessage)
				Alert.NewAlert("Dis_Lep_Attach");
			
			stats.InitializeNewTrait(TraitList.GetTraitByID("leprosy"));
		} else if (effect == TraitEffects.Crystallization && !stats.hasTrait("crystal")) {
			if (showMessage)
				Alert.NewAlert("Dis_Cry_Attach");
			
			stats.InitializeNewTrait(TraitList.GetTraitByID("crystal"));
		}

		for (int i = 0; i < Attributes.Count; i++) {
			if (Attributes[i].Stat != "Hunger")
				stats.Attributes[Attributes[i].Stat] += Attributes[i].Amount;
		}
	}

	public void Remove(Stats stats) {
		wounds.Clear();
		organic = true;

		for (int i = 0; i < Attributes.Count; i++) {
			if (Attributes[i].Stat != "Hunger")
				stats.Attributes[Attributes[i].Stat] -= Attributes[i].Amount;
		}
	}

	public SBodyPart ToSimpleBodyPart() {
		if (_equippedItem == null || _equippedItem.ID == "none") 
			_equippedItem = ItemList.GetNone();
		
		SItem equipped = _equippedItem.ToSimpleItem();
		SBodyPart simple = new SBodyPart(name, equipped, severable, _attached, slot, canWearGear, 
			Weight, Attributes, armor, effect, external, organic, level, currXP, maxXP, wounds);

		return simple;
	}

	public bool FreeToMove() {
		return (holdsOnMe.Count == 0);
	}

	public void TryBreakGrips() {
		List<Grip> gripsToBreak = new List<Grip>();

		foreach (Grip g in holdsOnMe) {
			if (g.GripBroken()) {
				string message = LocalizationManager.GetLocalizedContent("Gr_BreakGrip")[0];
				message = message.Replace("[ATTACKER]", g.myPart.myBody.gameObject.name);
				message = message.Replace("[DEFENDER]", myBody.gameObject.name);
				message = (myBody.entity.isPlayer ? "<color=cyan>" : "<color=orange>") + message;
				CombatLog.NewMessage(message + "</color>");
				gripsToBreak.Add(g);
			}
		}

		foreach (Grip g in gripsToBreak) {
			g.myPart.ReleaseGrip(true);
		}
	}

	#region Grappling
	public void GrabPart(BodyPart part) {
		if (part == null || part.myBody == null || myBody == null)
			return;

		if (grip == null)
			grip = new Grip(part, this);
		else {
			grip.Release();
			grip.Grab(part);
		}

		string message = LocalizationManager.GetLocalizedContent("Gr_Grab")[0];
		message = message.Replace("[ATTACKER]", myBody.gameObject.name);
		message = message.Replace("[DEFENDER]", part.myBody.gameObject.name);
		message = message.Replace("[ATTACKER_LIMB]", displayName);
		message = message.Replace("[DEFENDER_LIMB]", part.displayName);
		message = (myBody.entity.isPlayer ? "<color=cyan>" : "<color=orange>") + message + "</color>";
		CombatLog.NewMessage(message);
	}

	public void ReleaseGrip(bool forced) {
		if (grip == null)
			grip = new Grip(null, this);

		if (!forced) {
			string message = LocalizationManager.GetLocalizedContent("Gr_Release")[0];
			message = message.Replace("[ATTACKER]", myBody.gameObject.name);
			message = message.Replace("[DEFENDER]", grip.HeldPart.myBody.gameObject.name);
			message = message.Replace("[DEFENDER_LIMB]", grip.HeldPart.name);
			message = (myBody.entity.isPlayer ? "<color=cyan>" : "<color=orange>") + message + "</color>";
			CombatLog.NewMessage(message);
		}

		grip.Release();
	}
	#endregion


	void CopyFrom(BodyPart other) {
		Attributes = other.Attributes;
		_baseName = other._baseName;
		_weight = other._weight;
		slot = other.slot;
		name = other.name;
		_attached = other.isAttached;
		_equippedItem = other.equippedItem;
		armor = other.armor;
		external = other.external;
		organic = other.organic;
		_severable = other.severable;
		held = other.held;
		displayName = name;
		bpTags = new List<BPTags>(other.bpTags);
		hand = other.hand;
		myBody = other.myBody;
		wounds = other.wounds;
	}

    [MoonSharpUserData]
	public class Hand {
		public BodyPart arm;
		Item _equippedItem;

		public bool isMainHand {
			get { return (arm.myBody.MainHand == this); }
		}

		public bool isAttached {
			get { return (arm != null && arm.isAttached); }
		}

		public Item equippedItem {
			get { return _equippedItem; }
		}

		public void SetEquippedItem(Item i, Entity entity) {
			if (_equippedItem != null && entity != null)
				_equippedItem.OnUnequip(entity, this == entity.body.MainHand);

			_equippedItem = i;

			if (entity != null && _equippedItem != null)
				_equippedItem.OnEquip(entity.stats, this == entity.body.MainHand);
		}

		public Hand(BodyPart _arm, Item _item) {
			arm = _arm;
			_equippedItem = _item;
		}

		public Hand(Hand other) {
			arm = other.arm;
			_equippedItem = other.equippedItem;
		}

		public SHand toSHand() {
			return new SHand(_equippedItem.ToSimpleItem());
		}
	}

	public enum BPTags {
		None, Grip, OnSeverLast_Die, Synthetic, External
	}

	public class Grip {
		BodyPart _held;
		public BodyPart myPart;

		public Grip(BodyPart part, BodyPart me) {
			myPart = me;
			Grab(part);
		}

		public BodyPart HeldPart {
			get { return _held; }
		}

		public Body HeldBody {
			get { return HeldPart.myBody; }
		}

		public int GripStrength() {
			return myPart.myBody.entity.stats.Strength;
		}

		public bool GripBroken(int strPenalty = 0) {
			if (HeldPart == null)
				return true;
			
			return (SeedManager.combatRandom.Next(-2, HeldBody.entity.stats.Strength) > SeedManager.combatRandom.Next(GripStrength() / 2, GripStrength() + 1) - strPenalty);
		}

		public void Release() {
			if (_held == null)
				return;

			if (_held.holdsOnMe.Contains(this))
				_held.holdsOnMe.Remove(this);
			
			_held = null;
		}

		public void Grab(BodyPart part) {
			_held = part;

			if (_held != null)
				_held.holdsOnMe.Add(this);
		}
	}
}



using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[System.Serializable]
[MoonSharpUserData]
public class NPC {
	public string name = "", ID, UID, spriteID;
	public Coord worldPosition, localPosition;
	public int elevation;

	public Faction faction;
	public bool isHostile, onScreen = false, isAlive, hasSeenPlayer;
	int _health, _stamina;
	public int maxHealth, maxStamina;
	public Dictionary<string, int> Attributes;

	public List<Item> handItems;
	public Item firearm;
	public List<Item> inventory = new List<Item>();
	public List<BodyPart> bodyParts = new List<BodyPart>();
	public List<NPC_Flags> flags = new List<NPC_Flags>();
	public string corpseItem;
	public string questID;

	Random RNG {
		get { return SeedManager.combatRandom; }
	}

	public event Action onDeath;

    public int health
    {
        get { return _health; }
        set { _health = value; }
    }

    public int stamina
    {
        get { return _stamina; }
        set { _stamina = value; }
    }

    public NPC(string _npcID, Coord _wp, Coord _lp, int _elev) {
		InitializeAttributeDictionary();
		this.elevation = _elev;
		this.worldPosition = _wp;
		this.localPosition = _lp;

		FromBlueprint(EntityList.GetBlueprintByID(_npcID));
	}

	public NPC(Coord _worldPos, Coord _localPos, int elev) {
		InitializeAttributeDictionary();

		this.worldPosition = _worldPos;
		this.localPosition = _localPos;
		this.elevation = elev;
		questID = "";

		Init();
	}

	public void AssignStats() {
		_health = maxHealth;
		_stamina = maxStamina;

		if (maxHealth <= 0)
			maxHealth = 0;
		
		isHostile = faction.HostileToPlayer();
	}

	public void Init() {
		isAlive = true;
        onScreen = false;

		if (HasFlag(NPC_Flags.Named_NPC))
			name = NameGenerator.CharacterName(SeedManager.textRandom);

		UID = ObjectManager.SpawnedNPCs.ToString();
		ObjectManager.SpawnedNPCs ++;
	}

	public bool CanSpawnThisNPC(Coord pos, int elev) {
		if (!isAlive || onScreen)
			return false;
		
		return (pos == worldPosition && elev == elevation);
	}

	public bool HasFlag(NPC_Flags flag) {
		return (flags.Contains(flag));
	}

	public SStats GetSimpleStats() {
		return new SStats(new Coord(health, maxHealth), new Coord(stamina, maxStamina), new Dictionary<string, int>(Attributes), new Dictionary<string, int>(), 0, null);
	}

	public void FromBlueprint(NPC_Blueprint blueprint) {
		this.name = blueprint.name;
		this.ID = blueprint.id;
		this.faction = blueprint.faction;
		this.maxHealth = blueprint.health;
		this.maxStamina = blueprint.stamina;
		AssignStats();

		Attributes = new Dictionary<string, int>(blueprint.attributes);
		bodyParts = new List<BodyPart>(blueprint.bodyParts);
		flags = new List<NPC_Flags>(blueprint.flags);
		spriteID = blueprint.spriteIDs.GetRandom(SeedManager.combatRandom);

        string wepID = blueprint.weaponPossibilities.GetRandom();
		Item wep = ItemList.GetItemByID(wepID);

        if (wep == null) {
            UnityEngine.Debug.LogError("Weapon with ID \"" + wepID + "\" not found. From NPC Blueprint \"" + blueprint.id + "\".");
            wep = ItemList.GetItemByID("fists");
        }

		firearm = string.IsNullOrEmpty(blueprint.firearm) ? ItemList.GetNone() : ItemList.GetItemByID(blueprint.firearm);

        handItems = new List<Item>
        {
            wep
        };

        if (!wep.lootable)
			handItems.Add(new Item(wep));

		ShuffleInventory(blueprint);

		if (!string.IsNullOrEmpty(blueprint.quest))
			questID = blueprint.quest;
		
		if (blueprint.Corpse_Item != null)
			corpseItem = blueprint.Corpse_Item;

		Init();
	}

	void ShuffleInventory(NPC_Blueprint blueprint) {
		this.inventory = new List<Item>();

		if (blueprint.maxItems > 0) {
			if (RNG.Next(1000) <= (1.2f * blueprint.maxItemRarity))
				inventory.Add(ItemList.GetRandomArtifact());
			
			int numItems = RNG.Next(HasFlag(NPC_Flags.Merchant) ? 4 : 0, blueprint.maxItems + 2);

			for (int i = 0; i < numItems; i++) {
				inventory.Add(ItemList.GetItemByRarity(ItemList.TimedDropRarity(blueprint.maxItemRarity)));
			}

			if (RNG.Next(100) < 10) {
				Item ammo = ItemList.GetItemByID("bullet");
				ammo.amount = RNG.Next(1, 25);
				inventory.Insert(0, ammo);
			}
		}

		if (blueprint.inventory.Count > 0) {
			foreach (KeyValuePair<string, Coord> kvp in blueprint.inventory) {
				Item i = ItemList.GetItemByID(kvp.Key);
				int amount = RNG.Next(kvp.Value.x, kvp.Value.y + 1);

				if (amount > 0) {
					if (i.stackable)
						i.amount = amount;
					else {
						for (int t = 1; t < amount; t++) {
							Item i2 = ItemList.GetItemByID(kvp.Key);
							inventory.Add(new Item(i2));
						}
					}

					inventory.Add(i);
				}
			}
		}

		if (HasFlag(NPC_Flags.Doctor)) {
			List<Item> items = ItemList.items.FindAll(x => x.ContainsProperty(ItemProperty.Replacement_Limb) && x.GetItemComponent<CRot>() == null);

			for (int i = 0; i < RNG.Next(0, 2); i++) {
				inventory.Add(items.GetRandom(RNG));
			}
		}
	}

	public void MakeFollower() {
		faction = FactionList.GetFactionByID("followers");
		flags.Add(NPC_Flags.Follower);

		if (flags.Contains(NPC_Flags.Stationary_While_Passive))
			flags.Remove(NPC_Flags.Stationary_While_Passive);
	}

	public void AddFlag(NPC_Flags fl) {
		flags.Add(fl);
	}

	public void ReshuffleInventory() {
		ShuffleInventory(EntityList.GetBlueprintByID(ID));
	}

	void InitializeAttributeDictionary() {
		Attributes = new Dictionary<string, int>() {
			{ "Strength", 1 }, { "Dexterity", 1 }, { "Intelligence", 1 }, { "Endurance", 1 },
			{ "Accuracy", 1 }, { "Speed", 10 }, { "Perception", 10 }, { "Defense", 0 }, 
			{ "Heat Resist", 0 }, { "Cold Resist", 0 }, { "Energy Resist", 0 }, { "Attack Delay", 0 }
		};
	}
}

[System.Serializable]
public enum NPC_Flags {
	Static, Stationary, Stationary_While_Passive, Merchant, 
	Follower, At_Home,
	Deteriortate_HP, Mercenary, Boss, Arena_Master,
	Flying, Aquatic, No_Blood, No_Body, Named_NPC, Human, Doctor, Radiation, Quantum_Locked, Book_Merchant,

	Skills_Leprosy, Summon_Adds, OnDeath_Explode, OnDeath_PoisonGas, Hit_And_Run, Inactive,

	Can_Speak, Can_Open_Doors, Solid_Limbs, No_Melee, RPois, RBleed, Resist_Webs, OnDisable_Regen, SpawnedFromQuest
}

using System.Collections.Generic;
using MoonSharp.Interpreter;

[System.Serializable]
[MoonSharpUserData]
public class Item {

	public string ID, Name, displayName = "", flavorText;
    public Proficiencies itemType;
	public int armor, amount = 1, accuracy, rarity, tileID = -1;
	public bool lootable, stackable = false;

	public HashSet<ItemProperty> properties = new HashSet<ItemProperty>();
	public HashSet<DamageTypes> damageTypes = new HashSet<DamageTypes>();
	public List<Stat_Modifier> statMods = new List<Stat_Modifier>();
    public List<CComponent> components;
	public AttackType attackType;

	public Damage damage;
	public ItemModifier modifier;
	public ItemRenderer renderer;

	int cost;

	public int Accuracy {
		get { return accuracy + modifier.accuracy; }
	}

	public DamageTypes[] dTypeList {
		get { return new List<DamageTypes>(damageTypes).ToArray(); }
	}

	#region Constructors
	public Item() {
		Defaults();
	}

	public Item(Item other) {
		Defaults();
		CopyFrom(other);
	}

	public Item(string name) {
		Defaults();
		Name = name;
	}

	void Defaults() {
		Name = "";
		lootable = true;
		amount = 1;
		damage = new Damage(1, 2, 0, DamageTypes.Blunt);
		modifier = new ItemModifier();
		itemType = Proficiencies.Misc_Object;
		attackType = AttackType.Bash;

		components = new List<CComponent>();
	}
    #endregion

    public T GetItemComponent<T>() where T : CComponent {
        return (T)components.Find(x => x.GetType() == typeof(T));
    }

    public bool HasComponent<T>() where T : CComponent {
        return components.Find(x => x.GetType() == typeof(T)) != null;
    }

    public void SetBaseCost(int amount) {
		cost = amount;
	}

	public bool PhysicalDamage() {
		return (!ContainsDamageType(DamageTypes.Slash) || !ContainsDamageType(DamageTypes.Blunt) || !ContainsDamageType(DamageTypes.Pierce));
	}

	public void UpdateUserSprite(Stats stats, bool wield) {
		if (!string.IsNullOrEmpty(renderer.onPlayer) && stats.entity.isPlayer) {
			DynamicSpriteController dsc = stats.GetComponent<DynamicSpriteController>();

			if (HasProp(ItemProperty.Weapon)) {
				if (wield || itemType == Proficiencies.Shield)
					dsc.SetSprite(renderer);
			} else {
				dsc.SetSprite(renderer);
			}
		}
	}

	#region On___ Functions
	public void OnEquip(Stats stats, bool wield = false) {
		if (!OnUseReject()) {
			ChangeStats(stats, false);
			UpdateUserSprite(stats, wield);
		}

		if (HasComponent<CAbility>()) {
			CAbility cab = GetItemComponent<CAbility>();
			Skill sk = SkillList.GetSkillByID(cab.abID);
			sk.fromItem = true;
			stats.entity.skills.AddSkill_Item(sk);
		}

		RunCommands("OnEquip");
	}

	public void OnUnequip(Entity entity, bool wield = false) {
		if (!OnUseReject()) {
			ChangeStats(entity.stats, true);

			if (entity.isPlayer && !string.IsNullOrEmpty(renderer.onPlayer)) {
				DynamicSpriteController dsc = entity.GetComponent<DynamicSpriteController>();

				if (HasProp(ItemProperty.Weapon)) {
					if (wield || HasComponent<CBlock>())
						dsc.SetSprite(renderer, true);
				} else {
					dsc.SetSprite(renderer, true);
				}
			}
		}

		if (HasComponent<CAbility>()) {
			CAbility cab = GetItemComponent<CAbility>();

            //Remove the ability if it is not present on other equipment.
			if (entity.inventory.EquippedItems().Find(x => x.HasComponent<CAbility>() && x.GetItemComponent<CAbility>().abID ==  cab.abID && x != this) == null)
				entity.skills.RemoveSkill(cab.abID);
		}

        RunCommands("OnUnequip");
    }

	public void OnHit(Entity myEntity, Entity attackedEntity) {
        RunCommands_Params("OnHit", attackedEntity, myEntity);

		//Weapon coatings
		if (HasComponent<CCoat>()) {
			CCoat cc = GetItemComponent<CCoat>();

			if (cc.strikes > 0)
				cc.OnStrike(attackedEntity.stats);
			else
				components.Remove(cc);
		}

		//Item level
		if (HasComponent<CItemLevel>())
            GetItemComponent<CItemLevel>().AddXP(this, SeedManager.combatRandom.Next(3, 11));
	}

	public void OnBlock(Entity myEntity, Entity targetEntity) {
        RunCommands_Params("OnBlock", targetEntity, myEntity);
	}

	public void OnConsume(Stats stats) {
		ChangeStats(stats, false);
		ApplyEffects(stats);
        RunCommands("OnConsume");

        if (HasComponent<CLiquidContainer>() && GetItemComponent<CLiquidContainer>().liquid != null) {
			GetItemComponent<CLiquidContainer>().Drink(stats.entity);
		} else if (HasComponent<CCoat>()) {
			GetItemComponent<CCoat>().OnStrike(stats);
		}

		if (World.difficulty.Level == Difficulty.DiffLevel.Hunted || World.difficulty.Level == Difficulty.DiffLevel.Rogue) {
			if (ContainsProperty(ItemProperty.Addictive))
				stats.ConsumedAddictiveSubstance(ID, false);
		}

        if (HasProp(ItemProperty.Cannibalism))
			World.EatHuman();
	}
    #endregion

    public void RunCommands(string action) {
        if (HasComponent<CConsole>() || HasComponent<CLuaEvent>()) {
            for (int i = 0; i < components.Count; i++) {
                if (components[i].ID == "Console") {
                    CConsole cc = (CConsole)components[i];

                    cc.RunCommand(action);
                } else if (components[i].ID == "LuaEvent") {
                    CLuaEvent cl = (CLuaEvent)components[i];

                    cl.CallEvent(action);
                }
            }
        }
    }

    public void RunCommands_Params(string action, params object[] obj) {
        if (HasComponent<CConsole>() || HasComponent<CLuaEvent>()) {
            for (int i = 0; i < components.Count; i++) {
                if (components[i].ID == "Console") {
                    CConsole cc = (CConsole)components[i];
                    cc.RunCommand("OnBlock");
                } else if (components[i].ID == "LuaEvent") {
                    CLuaEvent cl = (CLuaEvent)components[i];
                    cl.CallEvent_Params("OnBlock", obj);
                }
            }
        }
    }

	public int GetAttackAPCost() {
		if (HasProp(ItemProperty.Very_Quick))
			return 5;
		if (HasProp(ItemProperty.Quick))
			return 8;
		if (HasProp(ItemProperty.Slow)) 
			return 12;
		if (HasProp(ItemProperty.Very_Slow))
			return 15;
		
		return 10;
	}

	public bool AttackCrits(int chance) {
		return (SeedManager.combatRandom.Next(100) <= chance);
	}

	public int CalculateDamage(int strength, int proficiency) {
		Damage nDmg = damage + modifier.damage;

		int newDamage = nDmg.Roll() + (strength / 2 - 1) + proficiency + 1;
		return newDamage;
	}

	public int ThrownDamage(int proficiency, int dex) {
		Damage nDmg = (HasProp(ItemProperty.Throwing_Wep)) ? damage : new Damage(1, 3, 0, DamageTypes.Blunt);

		int newDamage = nDmg.Roll() + (dex / 2 - 1) + proficiency;

		if (HasProp(ItemProperty.Throwing_Wep))
			newDamage += proficiency;
		
		return newDamage;
	}

	void ChangeStats(Stats stats, bool reverseEffect = false) {
		int multiplier = (reverseEffect) ? -1 : 1;

		foreach (Stat_Modifier mod in statMods) {
			int am = mod.Amount * multiplier;

			if (!HasProp(ItemProperty.Edible)) {
				if (stats.Attributes.ContainsKey(mod.Stat))
					stats.Attributes[mod.Stat] += am;
				if (stats.proficiencies != null && stats.proficiencies.Profs.ContainsKey(mod.Stat))
					stats.proficiencies.Profs[mod.Stat].level += am;
			}

			switch (mod.Stat) {
			case "Hunger":
				stats.Hunger += am;
				break;
			case "Haste":
				if (!reverseEffect)
					stats.AddStatusEffect("Haste", mod.Amount);
				break;
			case "Health":
				stats.health += am;
				break;
			case "Max Health":
				stats.maxHealth += am;
				stats.health += am;
				break;
			case "Stamina":
				stats.stamina += am;
				break;
			case "Max Stamina":
				stats.maxStamina += am;
				stats.stamina += am;
				break;
			case "Storage Capacity":
				if (stats.entity.inventory != null)
					stats.entity.inventory.AddRemoveStorage(am);
				else
					stats.GetComponent<Inventory>().AddRemoveStorage(am);
				break;
			case "Light":
				if (World.tileMap != null && ObjectManager.playerEntity != null)
					World.tileMap.LightCheck(ObjectManager.playerEntity);
				break;
			case "Accuracy":
				if (stats.Attributes.ContainsKey("Accuracy"))
					stats.Attributes["Accuracy"] += am;
				break;
			case "Endurance":
				stats.maxHealth += (am * 3);
				stats.maxStamina += am;
				break;
			}
		}
	}

	public void ApplyEffects(Stats stats) {
		if (HasProp(ItemProperty.Selected_Tele)) {
			Entity entity = stats.entity;

			if (HasComponent<CCoordinate>()) {
                CCoordinate ccord = GetItemComponent<CCoordinate>();

                if (ccord.isSet) {
					entity.ForcePosition(new Coord(ccord.lPos.x, ccord.lPos.y));

					World.tileMap.worldCoordX = ccord.wPos.x;
					World.tileMap.worldCoordY = ccord.wPos.y;
					World.tileMap.currentElevation = ccord.Ele;

					World.tileMap.HardRebuild();
					World.tileMap.SoftRebuild();

					CombatLog.SimpleMessage("Return_Tele");
					World.userInterface.CloseWindows();
					entity.BeamDown();
				} else {
					ccord.wPos = World.tileMap.WorldPosition;
					ccord.Ele = World.tileMap.currentElevation;
					ccord.aNa = World.tileMap.TileName();
					ccord.lPos = new Coord(entity.posX, entity.posY);

					CombatLog.SimpleMessage("Return_Link");
					ccord.isSet = true;
				}

				ObjectManager.player.GetComponent<PlayerInput>().CheckMinimap();
			}
		}

		if (HasProp(ItemProperty.ReplaceLimb))
			World.userInterface.Dialogue_ReplaceLimb(false);

		if (HasProp(ItemProperty.Poison) || ContainsDamageType(DamageTypes.Venom)) {
			CombatLog.SimpleMessage("Drink_Poison");
			stats.AddStatusEffect("Poison", SeedManager.combatRandom.Next(6, 10));
		}
		if (HasProp(ItemProperty.Confusion))
			stats.AddStatusEffect("Confuse", SeedManager.combatRandom.Next(5, 11));
		if (HasProp(ItemProperty.Stun))
			stats.AddStatusEffect("Stun", SeedManager.combatRandom.Next(1, 3));

		if (HasProp(ItemProperty.Radiate))
			stats.Radiate(SeedManager.combatRandom.Next(10, 40));
		if (HasProp(ItemProperty.Cure_Radiation)) {
			stats.radiation = 0;
			CombatLog.SimpleMessage("Reduce_Rad");
		}

		if (HasProp(ItemProperty.Cannibalism) && !stats.hasTrait("cannibal")) {
			//Check if there are villagers in sight.
			foreach (Entity e in World.objectManager.onScreenNPCObjects) {
				if (e.AI.npcBase.HasFlag(NPC_Flags.Human) && e.AI.InSightOfPlayer() && !e.AI.isHostile) {
					stats.InitializeNewTrait(TraitList.GetTraitByID("cannibal"));
					Alert.NewAlert("Became_Cannibal", UIWindow.Inventory);
					break;
				}
			}
		}

		if (HasProp(ItemProperty.OnAttach_Leprosy) && !stats.hasTraitEffect(TraitEffects.Leprosy)) {
			Alert.NewAlert("Dis_Lep_Eat");
			stats.InitializeNewTrait(TraitList.GetTraitByID("leprosy"));
			CombatLog.SimpleMessage("Eat_Lep");
		}
	}

	#region Add things
	public void AddProperty(ItemProperty property) {
		if (!properties.Contains(property))
			properties.Add(property);
	}
	public void AddDamageType(DamageTypes dt) {
		if (!damageTypes.Contains(dt))
			damageTypes.Add(dt);
	}

	public void AddModifier(ItemModifier mod) {
		if (mod == null || mod.name == "" || mod.ID == "")
			return;
		
		RemoveModifier();
		modifier = mod;

		if (!damageTypes.Contains(mod.damageType))
			AddDamageType(mod.damageType);
		
		armor += mod.armor;

		for (int i = 0; i < mod.properties.Count; i++) {
			if (!properties.Contains(mod.properties[i]))
				AddProperty(mod.properties[i]);
		}

		for (int i = 0; i < mod.statMods.Count; i++) {
			bool added = false;

			for (int j = 0; j < statMods.Count; j++) {
				if (mod.statMods[i].Stat == statMods[j].Stat) {
					statMods[j].Amount += mod.statMods[i].Amount;
					added = true;
				}
			}

			if (!added)
				statMods.Add(new Stat_Modifier(mod.statMods[i]));
		}

		for (int i = 0; i < mod.components.Count; i++) {
			components.Add(mod.components[i]);
		}
	}
	#endregion

	//Actually completely resets the item to default values.
	public void RemoveModifier() {
		CopyFrom(ItemList.GetItemByID(ID));
	}

	//Check for things
	public bool ContainsProperty(ItemProperty property) {
		return HasProp(property);
	}
	public bool HasProp(ItemProperty property) {
		return (properties.Contains(property));
	}
	public bool ContainsDamageType(DamageTypes search) {
		return (damageTypes.Contains(search));
	}

	public int buyCost(int bonus) {
		int totCost = (cost + modifier.cost > 0) ? (cost + modifier.cost) : cost;
		if (HasProp(ItemProperty.Randart))
			totCost *= 5;

		if (HasComponent<CLiquidContainer>()) {
			CLiquidContainer cl = GetItemComponent<CLiquidContainer>();

			if (cl.liquid != null) {
				totCost += cl.liquid.pricePerUnit * cl.liquid.units;
			}
		}
		
		return (int)((100f - (bonus * 3)) / 100f * totCost) * 2;
	}
	public int sellCost(int bonus) {
		int totCost = (cost + modifier.cost > 0) ? (cost + modifier.cost) : cost;
		if (HasProp(ItemProperty.Randart))
			totCost *= 5;

		if (HasComponent<CLiquidContainer>()) {
			CLiquidContainer cl = GetItemComponent<CLiquidContainer>();

			if (cl.liquid != null) {
				totCost += cl.liquid.pricePerUnit * cl.liquid.units;
			}
		}
		
		return (int)(((100f + (bonus * 3)) / 100f * totCost) * 0.6f);
	}

	public int Charges() {
		if (HasProp(ItemProperty.Ranged))
			return (!HasComponent<CFirearm>()) ? 1 : GetItemComponent<CFirearm>().curr;
		else if (HasComponent<CRot>())
			return GetItemComponent<CRot>().current;
		else if (HasComponent<CCharges>())
			return GetItemComponent<CCharges>().current;
		else 
			return 0;
	}

	public void Unload() {
		CFirearm cf = GetItemComponent<CFirearm>();

		if (cf == null)
			return;

		cf.curr = 0;
	}

	public bool UseCharge() {
		CCharges cc = GetItemComponent<CCharges>();

		if (cc == null) {
			CRot cr = GetItemComponent<CRot>();

			if (cr == null || cr.current <= 0)
				return false;

			cr.current --;
			return true;
		}

		if (cc.current <= 0)
			return false;

		cc.current --;
		return true;
	}

	public bool MagFull() {
		CFirearm cf = GetItemComponent<CFirearm>();

		if (cf == null)
			return true;

		return (cf.curr == cf.max);
	}

	public bool Fire() {
		if (HasProp(ItemProperty.Cannot_Remove))
			return true;

		CFirearm fi = GetItemComponent<CFirearm>();

		if (fi == null || fi.curr <= 0)
			return false;

		fi.curr --;
		return true;
	}

	public int Reload(int amount) {
		CFirearm fi = GetItemComponent<CFirearm>();

		if (fi == null)
			return 0;
		
		int maxAmmo = fi.max;
		int amountInMag = fi.curr;
		int amountUsed = 0;

		while (amountInMag < maxAmmo) {
			if (amount > 0) {
				amountUsed ++;
				amountInMag ++;
				amount --;
			} else {
				fi.curr = amountInMag;
				return amountUsed;
			}
		}

		fi.curr = amountInMag;
		return amountUsed;
	}

	public ItemProperty GetSlot() {
		if (properties.Contains(ItemProperty.Slot_Head))
			return ItemProperty.Slot_Head;
		else if (properties.Contains(ItemProperty.Slot_Back))
			return ItemProperty.Slot_Back;
		else if (properties.Contains(ItemProperty.Slot_Chest))
			return ItemProperty.Slot_Chest;
		else if (properties.Contains(ItemProperty.Slot_Arm))
			return ItemProperty.Slot_Arm;
		else if (properties.Contains(ItemProperty.Slot_Leg))
			return ItemProperty.Slot_Leg;
		else if (properties.Contains(ItemProperty.Slot_Wing))
			return ItemProperty.Slot_Wing;
		else if (properties.Contains(ItemProperty.Slot_Tail))
			return ItemProperty.Slot_Tail;

		//Arbitrary catch
		return ItemProperty.None;
	}

	#region "Display functions"

	public string DisplayName() {
		string baseName = (string.IsNullOrEmpty(displayName) ? Name : displayName);

		if (modifier != null && !string.IsNullOrEmpty(modifier.name))
			baseName = modifier.name + " " + baseName;
		else if (HasComponent<CLiquidContainer>()) {
			CLiquidContainer cl = GetItemComponent<CLiquidContainer>();

			string addition = " (" + ((cl.liquid != null) ? cl.liquid.Name : LocalizationManager.GetContent("IT_LiquidUnits_Empty")) + ")";
			baseName += addition;
		}

		return baseName;
	}

	public string InvDisplay(string natWep, bool forceArmor = false, bool forceWeapon = false, bool ranged = false) {
		if (modifier == null)
			modifier = new ItemModifier();

		if (ID == ItemList.GetNone().ID || ID == natWep)
			return "<color=grey>" + Name + "</color>";
		
		if (ID == "stump") {
			string disp = "<color=red>" + Name + "</color>";

			if (forceWeapon)
				disp +=  " " + DisplayDamage();
			else if (forceArmor)
				disp += " " + DisplayArmor();
			
			return disp;
		}

		string baseName = DisplayName();

		if (amount > 1) {
			if (ranged && HasProp(ItemProperty.Throwing_Wep))
				return (baseName + " x" + amount);
			return (HasProp(ItemProperty.Weapon)) ? baseName + " x" + amount + DisplayDamage() : baseName + " x" + amount;
		}

		if (forceWeapon)
			return baseName + " " + DisplayDamage();
		if (forceArmor)
			return baseName + " " + DisplayArmor();
		if (HasProp(ItemProperty.Weapon))
			return baseName + " " + ((armor == 0) ?  DisplayDamage() : DisplayArmor()) ;
		if (HasProp(ItemProperty.Ranged)) {
			int sh = GetItemComponent<CFirearm>().shots;
			return baseName + " " + DisplayDamage() + "<color=olive>(x" + sh + ")</color>";
		}
		if (HasProp(ItemProperty.Armor))
			return baseName + " " + DisplayArmor();

		return baseName;
	}


	public string DisplayDamage() {
		Damage d = damage + modifier.damage;

		if (damage != null) 
			return "<color=silver>(" + d.ToString() + ")</color>";
		
		return "";
	}


	public string DisplayArmor() {
		string s = "<color=silver>[";

		if (armor > -1)
			s += "+";
		
		return s + armor.ToString() + "]</color>";

	}
		
	//used only to check differences in names between items. items with mods cannot stack
	public string ItemName() {
		return modifier.name + " " + Name;
	}

	#endregion

	public SItem ToSimpleItem() {
		if (modifier == null)
			modifier = new ItemModifier();
		
		Item baseItem = ItemList.GetItemByID(ID);
		List<ItemProperty> props = new List<ItemProperty>();

		foreach (ItemProperty p in properties) {
			if (!baseItem.HasProp(p))
				props.Add(p);
		}

		if (props.Count == 0)
			props = null;

		return new SItem(ID, modifier.ID, amount, displayName, props, damage, armor, components, statMods);
	}

	public void AddComponent(CComponent comp) {
		components.Add(comp);
	}

	public void CopyFrom(Item other) {
		if (other == null)
			return;
		
		ID = other.ID;
		Name = other.Name;
		itemType = other.itemType;
		damage = other.damage;
		armor = other.armor;
		rarity = other.rarity;
		lootable = other.lootable;
		cost = other.cost;
		flavorText = other.flavorText;
		stackable = other.stackable;
		amount = other.amount;
		accuracy = other.accuracy;
		modifier = other.modifier;
		tileID = other.tileID;
		attackType = other.attackType;
		renderer = other.renderer;
		displayName = other.displayName;

		CopyLists(other);
	}

	void CopyLists(Item other) {
		components = new List<CComponent>();

		foreach (CComponent c in other.components) {
			components.Add(c.Clone());
		}

		statMods = new List<Stat_Modifier>();

		foreach (Stat_Modifier sm in other.statMods) {
			statMods.Add(new Stat_Modifier(sm));
		}

		properties.Clear();
		foreach (ItemProperty pr in other.properties) {
			AddProperty(pr);
		}

		damageTypes.Clear();

		foreach (DamageTypes d in other.damageTypes) {
			AddDamageType(d);
		}

		if (damageTypes.Count <= 0)
			AddDamageType(DamageTypes.Blunt);
	}

	public bool CanReplaceBP() {
		if (HasProp(ItemProperty.Armor))
			return false;
		return (HasProp(ItemProperty.Severed_BodyPart) || HasProp(ItemProperty.Slot_Arm) || HasProp(ItemProperty.Slot_Back)
			|| HasProp(ItemProperty.Slot_Chest) || HasProp(ItemProperty.Slot_Head) || HasProp(ItemProperty.Slot_Leg)
			|| HasProp(ItemProperty.Slot_Tail) || HasProp(ItemProperty.Slot_Wing));
	}

	bool OnUseReject() {
		return (!HasProp(ItemProperty.Weapon) && !HasProp(ItemProperty.Armor) && !HasProp(ItemProperty.Ranged));
	}
		
	public enum AttackType {
		Bash, Slash, Sweep, Spear, Claw, Psy, Knife, Bite
	}


	public struct ItemRenderer {
		public string onGround;
		public string onPlayer;
		public int slot;

		public ItemRenderer(int _slot, string _ground, string _player) {
			onGround = _ground;
			onPlayer = _player;
			slot = _slot;
		}
	}
}

[System.Serializable]
public enum ItemProperty {
	None,  Artifact, Ammunition,
	Confusion, Stop_Bleeding, Stun, Poison, Cure_Radiation, OnAttack_Radiation,
	OnAttach_Leprosy, OnAttach_Crystallization,
	Armor, Slot_Head, Slot_Back, Slot_Chest, Slot_Tail, Slot_Wing, Slot_Arm, Slot_Leg, Slot_Misc,
	Cannot_Remove, Degrade, DestroyOnZeroCharges, ReplaceLimb,
	Reveal_Map, Blink, Teleport, Surface_Tele, Selected_Tele, Knockback,
	Weapon, Two_Handed, Ranged, Dig, Quick, Very_Quick, Slow, Very_Slow, Throwing_Wep, Disarm, Shock_Nearby, Burst, Quick_Reload,
	Legible, Explosive, Edible, Severed_BodyPart, Radiate, Cannibalism, Corpse,
	Tome, Replacement_Limb, Flying,
	Quest_Item, Randart, Unique, Addictive, Bow, DrainHealth, Pool, NoMods
}

[System.Serializable]
public enum Proficiencies {
	Unarmed, Misc_Object, Blade, Axe, Blunt, Polearm, Firearm, Throw, Armor, Butchery, Shield
}
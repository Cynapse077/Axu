using System.Collections.Generic;

public class ItemModifier {

	string _name;
	string _ID;

	public Damage damage;
	public int armor, accuracy, cost;
	public bool unique = false;
	public string description;
	public List<ItemProperty> properties = new List<ItemProperty>();
	public DamageTypes damageType;
	public ModType modType;
    public List<Stat_Modifier> statMods = new List<Stat_Modifier>();
	public List<CComponent> components = new List<CComponent>();

	public string name {
		get { return _name; }
		set { _name = value; }
	}

	public string ID {
		get { return _ID; }
		set { _ID = value; }
	}


    public static ItemModifier Empty() {
        return new ItemModifier("", "");
    }

	public ItemModifier() { 
		_name = "";
		_ID = "";
		damage = new Damage(0, 0, 0, DamageTypes.Blunt);
		damageType = DamageTypes.Blunt;
		description = "";
	}
	public ItemModifier(string nam, string id) { 
		_name = nam;
		_ID = id;
		damage = new Damage(0, 0, 0, DamageTypes.Blunt);
		damageType = DamageTypes.Blunt;
		description = "";
	}

	public ItemModifier(ItemModifier other) {
		_name = other.name;
		_ID = other.ID;
		damage = other.damage;
		damageType = other.damageType;
		description = other.description;
		statMods = other.statMods;
		accuracy = other.accuracy;
		cost = other.cost;
		properties = other.properties;
		modType = other.modType;
		unique = other.unique;
		components = other.components;
	}

	public bool CanAddToItem(Item i) {
		if (i.HasProp(ItemProperty.NoMods) || i.stackable || !i.lootable)
			return false;
		
		if (modType == ModType.Weapon && i.HasProp(ItemProperty.Weapon))
			return true;
		if (modType == ModType.Armor && i.HasProp(ItemProperty.Armor))
			return true;
		if (modType == ModType.All && (i.HasProp(ItemProperty.Weapon) || i.HasProp(ItemProperty.Armor) || i.HasProp(ItemProperty.Ranged)))
			return true;
		if (modType == ModType.Ranged && i.HasProp(ItemProperty.Ranged))
			return true;
		if (modType == ModType.Shield && i.itemType == Proficiencies.Shield)
			return true;

		return false;
	}

	public enum ModType {
		All, Weapon, Armor, Ranged, Shield
	}
}

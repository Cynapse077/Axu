using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class PlayerProficiencies {
    public Dictionary<string, WeaponProficiency> Profs;

	public PlayerProficiencies() {
        Profs = new Dictionary<string, WeaponProficiency>();

		Profs.Add("Blade", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Blade")[0]));
		Profs.Add("Blunt", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Blunt")[0]));
		Profs.Add("Polearm", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Polearm")[0]));
		Profs.Add("Axe", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Axe")[0]));
		Profs.Add("Firearm", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Firearm")[0]));
		Profs.Add("Throwing", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Throwing")[0]));
		Profs.Add("Unarmed", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Unarmed")[0]));
		Profs.Add("Misc", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Misc")[0]));

		Profs.Add("Armor", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Armor")[0]));
		Profs.Add("Shield", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Shield")[0]));
		Profs.Add("Butchery", new WeaponProficiency(LocalizationManager.GetLocalizedContent("Butchery")[0]));
	}

	public WeaponProficiency Blade {
		get { return Profs["Blade"]; }
		set { Profs["Blade"] = value; }
	}
	public WeaponProficiency Blunt {
		get { return Profs["Blunt"]; }
		set { Profs["Blunt"] = value; }
	}
	public WeaponProficiency Polearm {
		get { return Profs["Polearm"]; }
		set { Profs["Polearm"] = value; }
	}
	public WeaponProficiency Axe {
		get { return Profs["Axe"]; }
		set { Profs["Axe"] = value; }
	}
	public WeaponProficiency Firearm {
		get { return Profs["Firearm"]; }
		set { Profs["Firearm"] = value; }
	}
	public WeaponProficiency Throwing {
		get { return Profs["Throwing"]; }
		set { Profs["Throwing"] = value; }
	}
	public WeaponProficiency Unarmed {
		get { return Profs["Unarmed"]; }
		set { Profs["Unarmed"] = value; }
	}
	public WeaponProficiency Misc {
		get { return Profs["Misc"]; }
		set { Profs["Misc"] = value; }
	}
	public WeaponProficiency Armor {
		get { return Profs["Armor"]; }
		set { Profs["Armor"] = value; }
	}
	public WeaponProficiency Shield {
		get { return Profs["Shield"]; }
		set { Profs["Shield"] = value; }
	}
	public WeaponProficiency Butchery {
		get { return Profs["Butchery"]; }
		set { Profs["Butchery"] = value; }
	}

	public WeaponProficiency GetProficiencyFromItem(Item item) {
		switch (item.itemType) {
			case Proficiencies.Blade:
				return Blade;
			case Proficiencies.Blunt:
				return Blunt;
			case Proficiencies.Axe:
				return Axe;
			case Proficiencies.Polearm:
				return Polearm;
			case Proficiencies.Firearm:
				return Firearm;
			case Proficiencies.Throw:
				return Throwing;
			case Proficiencies.Unarmed:
				return Unarmed;
			case Proficiencies.Armor:
				return Armor;
			case Proficiencies.Shield:
				return Shield;
			case Proficiencies.Butchery:
				return Butchery;

			default:
				return Misc;	
		}
	}

	public List<WeaponProficiency> GetProfs() {
		List<WeaponProficiency> profs = new List<WeaponProficiency>();

		foreach (WeaponProficiency p in Profs.Values) {
            profs.Add(p);
        }

		return profs;
	}
}

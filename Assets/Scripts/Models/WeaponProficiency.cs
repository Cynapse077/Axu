using UnityEngine;
using System.Collections;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class WeaponProficiency {
	
	public string name;
	public int level;
	Proficiencies prof;
	double _xp;
	public string desc;
	const int xpToNext = 1000;
	static int MaxLevel = 10;

	public double xp {
		get {
			_xp = System.Math.Round(_xp, 2);
			return _xp;
		}
		set { _xp = value; }
	}

	public WeaponProficiency(string nam) {
		this.name = nam;
		this.level = 0;
		this.xp = 0;
	}

	public WeaponProficiency(string nm, Proficiencies p) {
		this.name = nm;
		this.prof = p;
		this.level = 0;
		this.xp = 0;
	}

	public WeaponProficiency(string nam, int lvl, double exp) {
		name = nam;
		level = lvl;
		xp = exp;
	}

	public bool AddXP(double amount) {
		if (amount <= 0) 
			return false;
		
		if (level < MaxLevel) {
			xp += amount * 0.5;
			bool leveled = false;

			while (xp >= xpToNext) {
				xp -= xpToNext;
				LevelUp();
				leveled = true;
			}

			return leveled;
		} else
			xp = 0;
		
		return false;
	}

	void LevelUp() {
		level++;
	}

	//Used for character creation screen.
	public string CCLevelName() {
		string myLvl = "<color=orange>" + (level).ToString() + "</color> - ";
		int lvl = Mathf.Min(level, 10);

		return myLvl + LocalizationManager.GetContent(("Prof_L" + lvl.ToString()));
	}

	public string LevelName() {
		int lvl = Mathf.Min(level - 1, 10);

		return LocalizationManager.GetContent(("Prof_L" + lvl.ToString()));
	}

	public void SetProficiency(Proficiencies p) { prof = p; }
	public Proficiencies GetProficiency() { return prof; }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class DiceRoll {
	public int Num;
	public int Sides;
	public int Inc;

	public DiceRoll() {}

	public DiceRoll(int numDice, int diceSides, int increase) {
		Num = numDice;
		Sides = diceSides;
		Inc = increase;
	}

	public override string ToString() {
		if (GameSettings.SimpleDamage)
			return Simplified();

		if (Inc > 0)
			return string.Format("({0}d{1} +{2})", Num, Sides, Inc);
		if (Inc < 0)
			return string.Format("{0}d{1} {2}", Num, Sides, Inc);

		return string.Format("({0}d{1})", Num, Sides);
	}

	public string Simplified() {
		int min = Inc, max = Inc;

		for (int i = 0; i < Num; i++) {
			min++;
			max += Sides;
		}

		return min + "-" + max;
	}

	public void ChangeValues(int numDice, int diceSides, int increase) {
		Num += numDice;
		Sides += diceSides;
		Inc += increase;
	}

	public int Roll() {
		int damage = Inc;

		for (int i = 0; i < Num; i++) {
			damage += SeedManager.combatRandom.Next(Sides) + 1;
		}

		return damage;
	}

	public static DiceRoll operator + (DiceRoll d1, DiceRoll d2) {
		return new DiceRoll(d1.Num + d2.Num, d1.Sides + d2.Sides, d1.Inc + d2.Inc); 
	}
	public static DiceRoll operator - (DiceRoll d1, DiceRoll d2) {
		return new DiceRoll(d1.Num - d2.Num, d1.Sides - d2.Sides, d1.Inc - d2.Inc); 
	}

	public static DiceRoll GetByString(string dmgString) {
		int dice = 0, sides = 0, inc = 0;
		string[] ss = dmgString.Split("d"[0]);

		if (ss[1].Contains("+")) {
			string[] ss2 = ss[1].Split("+"[0]);
			sides = int.Parse(ss2[0]);
			inc = int.Parse(ss2[1]);
		} else if (ss[1].Contains("-")) {
			string[] ss2 = ss[1].Split("-"[0]);
			sides = int.Parse(ss2[0]);
			inc = int.Parse(ss2[1]) * -1;
		} else {
			inc = 0;
			sides = int.Parse(ss[1]);
		}

		dice = int.Parse(ss[0]);

		return new DiceRoll(dice, sides, inc);
	}
}

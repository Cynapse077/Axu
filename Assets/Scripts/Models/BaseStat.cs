using System.Collections.Generic;
using UnityEngine;

public struct BaseStat {
	public string ID;
	int norm, min, max;
	List<Buff> buffs;

	public BaseStat(string _id, int _norm, int _min = 0, int _max = 999) {
		ID = _id;
		norm = _norm;
		min = _min;
		max = _max;
		buffs = new List<Buff>();
	}

	public int Norm {
		get {
			return norm;
		}
		set {
			norm = value;
			norm = Mathf.Clamp(norm, min, max);
		}
	}

	public int Total {
		get {
			UnregisterUnused();

			int tot = Norm;

			foreach (Buff b in buffs) {
				tot += b.amount;
			}

			return Mathf.Clamp(norm, min, max);
		}
	}

	void UnregisterUnused() {
		List<Buff> bs = new List<Buff>();

		foreach (Buff b in buffs) {
			if (b.turns <= 0)
				bs.Add(b);
		}

		foreach (Buff b in bs) {
			buffs.Remove(b);
		}
	}

	public void AddBuff(int amt, int t) {
		buffs.Add(new Buff(amt, t));
	}

	struct Buff {
		public int amount;
		public int turns;

		public Buff(int _amount, int _turns) {
			amount = _amount;
			turns = _turns;
			World.turnManager.incrementTurnCounter += UpdateTurn;
		}

		void UpdateTurn() {
			turns--;

			if (turns <= 0)
				Unregister();
		}

		void Unregister() {
			World.turnManager.incrementTurnCounter += UpdateTurn;
		}
	}
}

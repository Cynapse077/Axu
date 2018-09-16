using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Liquid : IWeighted {
	public static List<MixingOutput> mixingOutputs;

	public string ID, Name, Description;
	public int units;
	public int pricePerUnit;
	public int addictiveness;
	public int Weight { get; set; }
	public Color32 color;
	public List<CLuaEvent> events;

	public Liquid() {}

	public Liquid(string id, string name, string desc, int rar, int ppu, int adct, List<CLuaEvent> ev, Color32 col) {
		this.ID = id;
		this.Name = name;
		this.Description = desc;
		this.Weight = rar;
		this.pricePerUnit = ppu;
		this.addictiveness = adct;
		this.events = ev;
		color = col;
		this.units = 0;
	}

	public Liquid(Liquid other) {
		CopyFrom(other);
	}

	public Liquid(Liquid other, int un) {
		CopyFrom(other);
		units = un;
	}

	public static Liquid Mix(Liquid input1, Liquid input2) {
		int totalUnits = input1.units + input2.units;

		if (input1.ID == input2.ID)
			return new Liquid(input1, totalUnits);

		Liquid liq = ItemList.GetLiquidByID("liquid_default", totalUnits);

		for (int i = 0; i < mixingOutputs.Count; i++) {
			if (input1.ID == mixingOutputs[i].Input1 || input1.ID == mixingOutputs[i].Input2) {
				if (input2.ID == mixingOutputs[i].Input1 || input2.ID == mixingOutputs[i].Input2) {
					return ItemList.GetLiquidByID(mixingOutputs[i].Output, totalUnits);
				}
			}
		}

		return liq;
	}

	public void Drink(Stats stats) {
		if (events.Find(x => x.evName == "OnDrink") != null)
			events.Find(x => x.evName == "OnDrink").CallEvent_Params("OnDrink", stats);

		if (addictiveness > 0 && (World.difficulty.Level == Difficulty.DiffLevel.Rogue || World.difficulty.Level == Difficulty.DiffLevel.Hunted)) {
			stats.ConsumedAddictiveSubstance(ID, true);
		}
	}

	public void Splash(Stats stats) {
		if (events.Find(x => x.evName == "OnSplash") != null)
			events.Find(x => x.evName == "OnSplash").CallEvent_Params("OnSplash", stats);
	}

	void CopyFrom(Liquid other) {
		this.ID = other.ID;
		this.Name = other.Name;
		this.units = other.units;
		this.Description = other.Description;
		this.pricePerUnit = other.pricePerUnit;
		this.addictiveness = other.addictiveness;
		this.events = new List<CLuaEvent>();
		this.color = other.color;

		for (int i = 0; i < other.events.Count; i++) {
			events.Add((CLuaEvent)other.events[i].Clone());
		}
	}

	public class MixingOutput {
		public string Input1;
		public string Input2;
		public string Output;

		public MixingOutput() {}

		public MixingOutput(string in1, string in2, string otp) {
			Input1 = in1;
			Input2 = in2;
			Output = otp;
		}
	}
}
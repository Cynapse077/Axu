using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using System.IO;

public class SaveData : MonoBehaviour {
	TileMap tileMap;
	WorldMap worldMap;
	Entity playerEntity;
	Stats playerStats;
	JsonData playerJson;

	public void SaveNPCs(List<NPC> npcs, bool correctOrientation) {
		List<NPCCharacter> chars = new List<NPCCharacter>();

		for (int i = 0; i < npcs.Count; i++) {
			if (npcs[i] == null || !npcs[i].isAlive) 
				continue;

			NPC n = npcs[i];

			List<NPCItem> items = new List<NPCItem>();
			if (n.inventory != null && n.inventory.Count > 0) {
				for (int it = 0; it < n.inventory.Count; it++) {
					if (n.inventory[it] != null && n.inventory[it].ID != null)
						items.Add(new NPCItem(new string[2] { n.inventory[it].ID, n.inventory[it].modifier.ID }));
				}
			}

			List<SItem> handItems = new List<SItem>();
			for (int j = 0; j < n.handItems.Count; j++) {
				handItems.Add(n.handItems[j].ToSimpleItem());
			}

			if (n.firearm == null)
				n.firearm = ItemList.GetNone();

			NPCCharacter character = new NPCCharacter(n.name, n.ID, n.UID, n.worldPosition, n.localPosition, n.elevation, items, handItems, n.firearm.ToSimpleItem(),
				n.isHostile, n.spriteID, n.faction.ID, n.flags, n.questID);
			
			chars.Add(character);
		}

		int currentTurn = World.turnManager.turn;
	//save the map data
		new NewWorld(chars, currentTurn);
	}

	/// <summary>
	/// Loads the player.
	/// </summary>
	public void LoadPlayer() {
		Manager.playerBuilder = new PlayerBuilder();

		string jsonString = File.ReadAllText(Manager.SaveDirectory);
		playerJson = JsonMapper.ToObject(jsonString)["Player"];

	//Name
		Manager.worldSeed = (int)playerJson["WorldSeed"];
		Manager.playerName = playerJson["Name"].ToString();
		Manager.profName = playerJson["ProfName"].ToString();

	//Level
		Manager.playerBuilder.level = new XPLevel(
			null,
			(int)playerJson["xpLevel"]["CurrentLevel"],
			(int)playerJson["xpLevel"]["XP"],
			(int)playerJson["xpLevel"]["XPToNext"] );

	//Positions
		Manager.localStartPos.x = (int)playerJson["LP"][0];
		Manager.localStartPos.y = (int)playerJson["LP"][1];
		Manager.startElevation = (int)playerJson["WP"][2];

	//Health/Stamina
		Manager.playerBuilder.hp = (int)playerJson["Stats"]["HP"][0];
		Manager.playerBuilder.maxHP = (int)playerJson["Stats"]["HP"][1];
		Manager.playerBuilder.st = (int)playerJson["Stats"]["ST"][0];
		Manager.playerBuilder.maxST = (int)playerJson["Stats"]["ST"][1];
	//Attributes
		Manager.playerBuilder.attributes["Strength"] = (int)playerJson["Stats"]["STR"];
		Manager.playerBuilder.attributes["Dexterity"] = (int)playerJson["Stats"]["DEX"];
		Manager.playerBuilder.attributes["Intelligence"] = (int)playerJson["Stats"]["INT"];
		Manager.playerBuilder.attributes["Endurance"] = (int)playerJson["Stats"]["END"];
		Manager.playerBuilder.attributes["Defense"] = (int)playerJson["Stats"]["DEF"];
		Manager.playerBuilder.attributes["Speed"] = (int)playerJson["Stats"]["SPD"];
		Manager.playerBuilder.attributes["Stealth"] = (int)playerJson["Stats"]["STLH"];
		Manager.playerBuilder.attributes["Charisma"] = (int)playerJson["Charisma"];
		Manager.playerBuilder.attributes["Accuracy"] = (int)playerJson["Stats"]["ACC"];

	//Hunger
		Manager.playerBuilder.hunger = (int)playerJson["Hunger"];
	//Radiation
		Manager.playerBuilder.radiation = (int)playerJson["Stats"]["Rad"];

	//Status Effects
		Manager.playerBuilder.statusEffects = new Dictionary<string, int>();
		if (playerJson["Stats"].ContainsKey("SE") && playerJson["Stats"]["SE"].Count > 0) {
			for (int i = 0; i < playerJson["Stats"]["SE"].Count; i++) {
				Manager.playerBuilder.statusEffects.Add(playerJson["Stats"]["SE"][i]["A"].ToString(), (int)playerJson["Stats"]["SE"][i]["B"]);
			}
		}

	//weapon profs
		Manager.playerBuilder.proficiencies = new PlayerProficiencies();
		Manager.playerBuilder.proficiencies.Blade = SetProf("Blade", 0);
		Manager.playerBuilder.proficiencies.Blunt = SetProf("Blunt", 1);
		Manager.playerBuilder.proficiencies.Polearm = SetProf("Polearm", 2);
		Manager.playerBuilder.proficiencies.Axe = SetProf("Axe", 3);
		Manager.playerBuilder.proficiencies.Firearm = SetProf("Firearm", 4);
		Manager.playerBuilder.proficiencies.Throwing = SetProf("Thrown", 5);
		Manager.playerBuilder.proficiencies.Unarmed = SetProf("Unarmed", 6);
		Manager.playerBuilder.proficiencies.Misc = SetProf("Misc", 7);
	//other
		Manager.playerBuilder.proficiencies.Armor = SetProf("Armor", 8);
		Manager.playerBuilder.proficiencies.Shield = SetProf("Shield", 9);
		Manager.playerBuilder.proficiencies.Butchery = SetProf("Butchery", 10);

	//Traits
		Manager.playerBuilder.traits = new List<Trait>();
		for (int i = 0; i < playerJson["Traits"].Count; i++) {
            string tName = playerJson["Traits"][i]["id"].ToString();
            Trait nTrait = TraitList.GetTraitByID(tName);
			nTrait.turnAcquired = (int)playerJson["Traits"][i]["tAc"];
			Manager.playerBuilder.traits.Add(nTrait);
		}

	//Addictions
		Manager.playerBuilder.addictions = new List<Addiction>();
		if (playerJson["Stats"].ContainsKey("Adcts")) {
			for (int i = 0; i <  playerJson["Stats"]["Adcts"].Count; i++) {
				JsonData j = playerJson["Stats"]["Adcts"][i];

				string itemID = j["addictedID"].ToString();
				bool addicted = (bool)j["addicted"], withdrawal = (bool)j["withdrawal"];

				Addiction a = new Addiction(itemID, addicted, withdrawal, (int)j["lastTurnTaken"], (int)j["chanceToAddict"], (int)j["currUse"]);
				Manager.playerBuilder.addictions.Add(a);
			}
		}

	//create an item out of the Json data
		SetUpInventory();
		SetUpSkills();

		Manager.playerBuilder.money = (int)playerJson["Gold"];
		int weatherInt = (int)playerJson["CWeather"];
		Manager.startWeather = (Weather)weatherInt;

		if (playerJson.ContainsKey("HumEat"))
			World.HumanCorpsesEaten = (int)playerJson["HumEat"];
	}

	WeaponProficiency SetProf(string name, int id) {
		return new WeaponProficiency(name, (int)playerJson["WepProf"][id]["level"], (double)playerJson["WepProf"][id]["xp"]);
	}

	void SetUpInventory() {
	//items
		JsonData invJson = playerJson["Inv"];
		List<Item> items = new List<Item>();

		for(int i = 0; i < invJson.Count; i++) {
			items.Add(GetItemFromJsonData(invJson[i]));
		}

		Manager.playerBuilder.items = items;
		Manager.playerBuilder.baseWeapon = playerJson["BW"].ToString();

	//body parts
		JsonData bpJson = playerJson["BodyParts"];
		Manager.playerBuilder.bodyParts = new List<BodyPart>();

		for (int i = 0; i < bpJson.Count; i++) {
			BodyPart bp = new BodyPart(bpJson[i]["Name"].ToString(), (bool)bpJson[i]["Att"]);
			bp.severable = (bool)bpJson[i]["Sev"];
			int slot = (int)bpJson[i]["Slot"];
			bp.slot = (ItemProperty)slot;
			bp.canWearGear = (bool)bpJson[i]["CWG"];
			bp.armor = (int)bpJson[i]["Ar"];

			if (bpJson[i].ContainsKey("Org"))
				bp.organic = (bool)bpJson[i]["Org"];
			if (bpJson[i].ContainsKey("Ext"))
				bp.external = (bool)bpJson[i]["Ext"];

			if (bp.slot == ItemProperty.Slot_Arm)
				bp.hand = new BodyPart.Hand(bp, null);

			bp.Attributes = new List<Stat_Modifier>();
			if (bpJson[i].ContainsKey("Stats") && bpJson[i]["Stats"].Count > 0) {
				for (int j = 0; j < bpJson[i]["Stats"].Count; j++) {
					bp.Attributes.Add(new Stat_Modifier(bpJson[i]["Stats"][j]["Stat"].ToString(), (int)bpJson[i]["Stats"][j]["Amount"]));
				}
			}
			//Diseases
			if (bpJson[i].ContainsKey("Dis")) {
				int teffect = (int)bpJson[i]["Dis"];
				bp.effect = (TraitEffects)teffect;
			}

			//body part levels/xp
			if (bpJson[i].ContainsKey("Lvl"))
				bp.level = (int)bpJson[i]["Lvl"];
			if (bpJson[i].ContainsKey("XP")) {
				bp.SetXP((double)bpJson[i]["XP"][0], (double)bpJson[i]["XP"][1]);
			}

			//Wounds
			bp.wounds = new List<Wound>();
			if (bpJson[i].ContainsKey("Wounds")) {
				for (int j = 0; j < bpJson[i]["Wounds"].Count; j++) {
					ItemProperty ip = (bpJson[i]["Wounds"][j]["slot"].ToString()).ToEnum<ItemProperty>();
					List<DamageTypes> dts = new List<DamageTypes>();

					for (int k = 0; k < bpJson[i]["Wounds"][j]["damTypes"].Count; k++) {
						dts.Add((bpJson[i]["Wounds"][j]["damTypes"][k].ToString()).ToEnum<DamageTypes>());
					}

					Wound w = new Wound(bpJson[i]["Wounds"][j]["Name"].ToString(), bpJson[i]["Wounds"][j]["ID"].ToString(), ip, dts);
					w.Desc = bpJson[i]["Wounds"][j]["Desc"].ToString();

					for (int k = 0; k < bpJson[i]["Wounds"][j]["statMods"].Count; k++) {
						w.statMods.Add(new Stat_Modifier(bpJson[i]["Wounds"][j]["statMods"][k]["Stat"].ToString(), (int)bpJson[i]["Wounds"][j]["statMods"][k]["Amount"]));
					}

					bp.wounds.Add(w);
				}
			}

			bp.equippedItem = GetItemFromJsonData(bpJson[i]["item"]);
			Manager.playerBuilder.bodyParts.Add(bp);
		}

		for (int i = 0; i < playerJson["HIt"].Count; i++) {
			Manager.playerBuilder.handItems.Add(GetItemFromJsonData(playerJson["HIt"][i]));
		}

		Manager.playerBuilder.firearm = GetItemFromJsonData(playerJson["F"]);
	}


	public void SetUpJournal() {
		for (int i = 0; i < playerJson["Quests"].Count; i++) {
			JsonData qData = playerJson["Quests"][i];
			Manager.playerBuilder.quests.Add(GetQuest(qData));
		}

		Manager.playerBuilder.progressFlags = new List<ProgressFlags>();
		if (playerJson.ContainsKey("Flags")) {
			for (int i = 0; i < playerJson["Flags"].Count; i++) {
				int s = (int)playerJson["Flags"][i];
				ProgressFlags p = (ProgressFlags)s;
				Manager.playerBuilder.progressFlags.Add(p);
			}
		}
	}

	Quest GetQuest(JsonData qData) {
		List<string> qirewards = new List<string>();

		for (int j = 0; j < qData["iRe"].Count; j++) {
			qirewards.Add(qData["iRe"][j].ToString());
		}

		List<QuestStep> steps = new List<QuestStep>();

		for (int j = 0; j < qData["Steps"].Count; j++) {
			QuestStep qstep = new QuestStep();
			qstep.goal = qData["Steps"][j]["goal"].ToString();

			if (qData["Steps"][j].ContainsKey("of"))
				qstep.of = qData["Steps"][j]["of"].ToString();

			if (qData["Steps"][j].ContainsKey("am"))
				qstep.am = (int)qData["Steps"][j]["am"];

			if (qData["Steps"][j].ContainsKey("amC"))
				qstep.amC = (int)qData["Steps"][j]["amC"];

			if (qData["Steps"][j].ContainsKey("dest")) {
				int destX = (int)qData["Steps"][j]["destination"][0];
				int destY = (int)qData["Steps"][j]["destination"][1];

				qstep.destination = new Coord(destX, destY);
			}

			if (qData["Steps"][j].ContainsKey("e"))
				qstep.e = (int)qData["Steps"][j]["e"];

			steps.Add(qstep);
		}

		NPC questGiver = null;
		if (qData.ContainsKey("QG"))
			questGiver = World.objectManager.npcClasses.Find(x => x.UID == qData["QG"].ToString());

		Quest newQuest = new Quest(qData["Name"].ToString(), qData["ID"].ToString(), questGiver);
		newQuest.Description = qData["Desc"].ToString();

		if (qData.ContainsKey("eDia"))
			newQuest.endDialogue = qData["eDia"].ToString();

		if (qData.ContainsKey("Flag")) {
			int flagnum = (int)qData["Flag"];
			newQuest.flag = (ProgressFlags)flagnum;
		}

		newQuest.rewards.gold = (int)qData["Gold"];
		newQuest.rewards.XP = (int)qData["XP"];
		newQuest.rewards.items = qirewards;
		newQuest.steps = steps;

		if (qData["LQ"] != null)
			newQuest.chainedQuestID = qData["LQ"].ToString();
		if (qData["FRules"] != null)
			newQuest.followRules = qData["FRules"].ToString();

		return newQuest;
	}

	void SetUpSkills() {
		List<Skill> skills = new List<Skill>();

		for (int i = 0; i < playerJson["Skills"].Count; i++) {
			string sID = playerJson["Skills"][i]["Name"].ToString();
			Skill s = SkillList.GetSkillByID(sID);
			s.level = (int)playerJson["Skills"][i]["Lvl"];
			s.XP = (double)playerJson["Skills"][i]["XP"];

			skills.Add(s);
		}

		Manager.playerBuilder.skills = skills;
	}
		
	public static Item GetItemFromJsonData(JsonData data) {
		string iID = data["ID"].ToString();
		string mName = data["MID"].ToString();

		Item it = ItemList.GetItemByID(iID);

		it.amount = (int)data["Am"];

		if (data.ContainsKey("DName"))
			it.displayName = data["DName"].ToString();

		if (data.ContainsKey("Props")) {
			for (int i = 0; i < data["Props"].Count; i++) {
				int iPro = (int)data["Props"][i];
				it.AddProperty((ItemProperty)iPro);
			}
		}

		if (data.ContainsKey("Dmg"))
			it.damage = new Damage((int)data["Dmg"][0], (int)data["Dmg"][1], (int)data["Dmg"][2], it.damage.Type);
		if (data.ContainsKey("Ar"))
			it.armor = (int)data["Ar"];

		it.AddModifier(ItemList.GetModByID(mName));

		if (data.ContainsKey("Com"))
			it.components = GetComponentsFromData(data["Com"]);
		
		it.statMods = new List<Stat_Modifier>();

		if (data.ContainsKey("Sm")) {
			for (int i = 0; i < data["Sm"].Count; i++) {
				Stat_Modifier sm = new Stat_Modifier(data["Sm"][i]["Stat"].ToString(), (int)data["Sm"][i]["Amount"]);
				it.statMods.Add(sm);
			}
		}
			
		return it;
	}

	public static List<CComponent> GetComponentsFromData(JsonData data) {
		List<CComponent> comps = new List<CComponent>();

		for (int i = 0; i < data.Count; i++) {
			object o = CComponent.FromJson(data[i]);
			comps.Add((CComponent)o);
		}

		return comps;
	}

}

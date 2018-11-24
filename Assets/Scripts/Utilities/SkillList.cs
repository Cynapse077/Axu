using UnityEngine;
using LitJson;
using System.IO;
using System.Collections.Generic;

public static class SkillList {

    public static List<Skill> skills = new List<Skill>();
	public static string dataPath;

    public static void FillList() {
		if (skills != null && skills.Count > 0)
			return;
		
        skills = new List<Skill>();

		string listFromJson = File.ReadAllText(Application.streamingAssetsPath + dataPath);

		if (string.IsNullOrEmpty(listFromJson)) {
			Debug.LogError("Ability List null.");
		}

		JsonData data = JsonMapper.ToObject(listFromJson);

		for (int i = 0; i < data["Abilities"].Count; i++) {
			JsonData sd = data["Abilities"][i];
			Skill s = new Skill();

			s.Name = sd["Name"].ToString();
			s.ID = sd["ID"].ToString();
			s.Description = sd["Description"].ToString();

			if (sd.ContainsKey("Stamina Cost"))
				s.staminaCost = (int)sd["Stamina Cost"];
			if (sd.ContainsKey("Hunger Cost"))
				s.hungerCost = (int)sd["Hunger Cost"];
			if (sd.ContainsKey("Time Cost"))
				s.timeCost = (int)sd["Time Cost"];
			if (sd.ContainsKey("Cooldown"))
				s.maxCooldown = (int)sd["Cooldown"];
			if (sd.ContainsKey("Damage Type")) {
				string dType = sd["Damage Type"].ToString();
				s.damageType = dType.ToEnum<DamageTypes>();
			}

			if (sd.ContainsKey("Dice")) {
				s.dice = DiceRoll.GetByString(sd["Dice"].ToString());
			}

			if (sd.ContainsKey("Dice Scale")) {
				s.dicePerLevel = DiceRoll.GetByString(sd["Dice Scale"].ToString());
			}

			string castType = sd["Cast Type"].ToString();
			s.castType = castType.ToEnum<CastType>();
	
			if (sd.ContainsKey("Tags")) {
				for (int j = 0; j < sd["Tags"].Count; j++) {
					string ef = sd["Tags"][j].ToString();
					s.AddTag(ef.ToEnum<AbilityTags>());
				}
			}

			if (sd.ContainsKey("Script")) {
				if (sd["Script"].Count > 2)
					s.luaAction = new LuaCall(sd["Script"][0].ToString(), sd["Script"][1].ToString(), sd["Script"][2].ToString());
				else
					s.luaAction = new LuaCall(sd["Script"][0].ToString(), sd["Script"][1].ToString());
			}

            if (sd.ContainsKey("AI")) {
                s.aiAction = new LuaCall(sd["AI"][0].ToString(), sd["AI"][1].ToString());
            }

			if (sd.ContainsKey("Levels Up"))
				s.CanLevelUp = (bool)sd["Levels Up"];
			if (sd.ContainsKey("Range"))
				s.range = (int)sd["Range"];

			skills.Add(s);
		}
    }

	public static Skill GetSkillByID(string id) {
		if (skills.Find(s => s.ID == id) == null)
			return null;

		return new Skill(skills.Find(s => s.ID == id));
	}
}

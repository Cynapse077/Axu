using UnityEngine;
using System.IO;
using LitJson;
using System.Collections;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class EntityList {

	public static List<NPC_Blueprint> npcs;
	static List<BodyPart> humanoidStructure;
	static JsonData bodyPartJson;
	public static string dataPath;

	static TileMap tileMap {
		get { return World.tileMap; }
	}

	public static void FillListFromData() {
		npcs = GrabBlueprintsFromData(Application.streamingAssetsPath + dataPath);

		TraitList.FillTraitsFromData();
        NPCGroupList.Init();
	}

	public static void RemoveNPC(string id) {
		if (npcs.Find(x => x.id == id) != null)
			npcs.Remove(npcs.Find(x => x.id == id));
	}

    public static List<NPC_Blueprint> GrabBlueprintsFromData(string fileName) {
        List<NPC_Blueprint> list = new List<NPC_Blueprint>();

        string listFromJson = File.ReadAllText(fileName);

		if (string.IsNullOrEmpty(listFromJson)) {
			Debug.LogError("Entity Blueprints null.");
			return list;
		}

        JsonData data = JsonMapper.ToObject(listFromJson);
		humanoidStructure = GetBodyStructure("Humanoid");

        for (int i = 0; i < data["NPCs"].Count; i++) {
			JsonData npcJson = data["NPCs"][i];

            NPC_Blueprint blueprint = new NPC_Blueprint();

			blueprint.name = npcJson["Name"].ToString();
			blueprint.id = npcJson["ID"].ToString();
			blueprint.faction = FactionList.GetFactionByID(npcJson["Faction"].ToString());

			blueprint.spriteIDs = new List<string>();
			if (npcJson.ContainsKey("Sprite")) {
				string s = npcJson["Sprite"].ToString();

				if (s.Contains("|")) {
					string[] ids = s.Split("|"[0]);

					for (int j = 0; j < ids.Length; j++) {
						blueprint.spriteIDs.Add(ids[j]);
					}
				} else {
					blueprint.spriteIDs.Add(s);
				}
			}

			string bodyType = npcJson["Body Structure"].ToString();
            blueprint.bodyParts = GetBodyStructure(bodyType);

            for (int f = 0; f < npcJson["Flags"].Count; f++) {
                string flag = npcJson["Flags"][f].ToString();
                blueprint.flags.Add(flag.ToEnum<NPC_Flags>());
            }

            blueprint.health = (int)npcJson["Stats"]["Health"];
            blueprint.stamina = (int)npcJson["Stats"]["Stamina"];

			List<string> keys = new List<string>(blueprint.attributes.Keys);
			foreach (string a in keys) {
				if (npcJson["Stats"].ContainsKey(a))
					blueprint.attributes[a] = (int)npcJson["Stats"][a];
			}

			if (npcJson.ContainsKey("Skills")) {
				for (int s = 0; s < npcJson["Skills"].Count; s++) {
					blueprint.skills.Add(new KeyValuePair<string, int>(npcJson["Skills"][s]["ID"].ToString(), (int)npcJson["Skills"][s]["Level"]));
				}
			}

			if (npcJson.ContainsKey("Inventory")) {
				for (int k = 0; k < npcJson["Inventory"].Count; k++) {
					Coord amt = new Coord(1, (int)npcJson["Inventory"][k]["Max"]);

					if (npcJson["Inventory"][k].ContainsKey("Min"))
						amt.x = (int)npcJson["Inventory"][k]["Min"];
					
					blueprint.inventory.Add(new KeyValuePair<string, Coord>(npcJson["Inventory"][k]["Item"].ToString(), amt));
				}
			}

            for (int w = 0; w < npcJson["Weapon_Choices"].Count; w++) {
                blueprint.weaponPossibilities.Add(npcJson["Weapon_Choices"][w].ToString());
            }

			if (npcJson.ContainsKey("Firearm"))
				blueprint.firearm = npcJson["Firearm"].ToString();
				
			blueprint.maxItems = (npcJson.ContainsKey("MaxItems")) ? (int)npcJson["MaxItems"] : 0;
			blueprint.maxItemRarity = (npcJson.ContainsKey("MaxItemRarity")) ? (int)npcJson["MaxItemRarity"] : 0;

			if (npcJson.ContainsKey("Corpse_Item"))
                blueprint.Corpse_Item = npcJson["Corpse_Item"].ToString();
			if (npcJson.ContainsKey("Quest"))
				blueprint.quest = npcJson["Quest"].ToString();

			if (blueprint.flags.Contains(NPC_Flags.Static)) {
				blueprint.localPosition = new Coord((int)npcJson["Position"]["x"], (int)npcJson["Position"]["y"]);
				blueprint.elevation = (int)npcJson["Position"]["z"];
				blueprint.zone = npcJson["Position"]["Zone"].ToString();
			}

            list.Add(blueprint);
        }

        return list;
    }

	public static NPC_Blueprint GetBlueprintByID(string search) {
		return (npcs.Find(x => x.id == search));
	}

	public static NPC GetNPCByID(string id, Coord worldPos, Coord localPos, int elevation = 0) {
		if (elevation == 0)
			elevation = tileMap.currentElevation;

		for (int i = 0; i < npcs.Count; i++) {
			if (npcs[i].id == id) {
				NPC n = new NPC(worldPos, localPos, elevation);
				n.FromBlueprint(npcs[i]);
				return n;
			}
		}
			
		Debug.LogError("No NPC with the ID of '" + id + "'.");
		return null;
	}

	public static NPC GetNPCByName(string name, Coord worldPos, Coord localPos) {
		int elevation = tileMap.currentElevation;

		for (int i = 0; i < npcs.Count; i++) {
			if (npcs[i].name == name) {
				NPC n = new NPC(worldPos, localPos, elevation);
				n.FromBlueprint(npcs[i]);
				return n;
			}
		}

		MyConsole.NewMessageColor("No NPC with the name of '" + name + "'.", Color.red);
		return null;
	}

	public static List<BodyPart> DefaultBodyStructure() {
		List<BodyPart> bps = new List<BodyPart>();

		foreach (BodyPart bp in humanoidStructure) {
			bps.Add(new BodyPart(bp));
		}

		return bps;
    }

	public static string bodyDataPath;

	public static List<BodyPart> GetBodyStructure(string search) {
		List<BodyPart> parts = new List<BodyPart>();

		string file = File.ReadAllText(Application.streamingAssetsPath + bodyDataPath);
		bodyPartJson = JsonMapper.ToObject(file);

		for (int a = 0; a < bodyPartJson["Body Structures"].Count; a++) {
			if (bodyPartJson["Body Structures"][a]["Name"].ToString() == search) {
				for (int b = 0; b < bodyPartJson["Body Structures"][a]["Parts"].Count; b++) {
					string bpName = bodyPartJson["Body Structures"][a]["Parts"][b].ToString();
					BodyPart bpart = GetBodyPart(bpName);
					parts.Add(bpart);
				}

				return parts;
			}
		}

		return parts;
	}

	public static BodyPart GetBodyPart(string bodyPartID) {
		for (int i = 0; i < bodyPartJson["Body Parts"].Count; i++) {
			JsonData bpData = bodyPartJson["Body Parts"][i];

			if (bpData["ID"].ToString() == bodyPartID) {
				string bpName = bpData["Name"].ToString();
				string slotName = bpData["Slot"].ToString();
				ItemProperty slot = slotName.ToEnum<ItemProperty>();

				bool severable = (bool)bpData["Severable"];
				bool wearGear = (bool)bpData["Can Wear Gear"];

				BodyPart bodyPart = new BodyPart(bpName, severable, slot);
				bodyPart.canWearGear = wearGear;
				bodyPart.Weight = (int)bpData["Size"];
				bodyPart.organic = true;

				if (bpData.ContainsKey("Stats")) {
					bodyPart.Attributes = new List<Stat_Modifier>();
					for (int j = 0; j < bpData["Stats"].Count; j++) {
						bodyPart.Attributes.Add(new Stat_Modifier(bpData["Stats"][j]["Stat"].ToString(), (int)bpData["Stats"][j]["Amount"]));
					}
				}

				if (bpData.ContainsKey("Tags")) {
					bodyPart.bpTags = new List<BodyPart.BPTags>();

					for (int j = 0; j < bpData["Tags"].Count; j++) {
						string txt = bpData["Tags"][j].ToString();

						if (txt == "Synthetic")
							bodyPart.organic = false;
						else if (txt == "External")
							bodyPart.external = true;
						else {
							BodyPart.BPTags tag = txt.ToEnum<BodyPart.BPTags>();
							bodyPart.bpTags.Add(tag);

							if (tag == BodyPart.BPTags.Grip && bodyPart.slot == ItemProperty.Slot_Arm) {
								bodyPart.hand = new BodyPart.Hand(bodyPart, ItemList.GetItemByID("fists"));
							}
						}
					}
				}

				if (bpData.ContainsKey("Default Equipped"))
					bodyPart.equippedItem = ItemList.GetItemByID(bpData["Default Equipped"].ToString());

				return bodyPart;
			}
		}

		Debug.LogError("Could not find body part: " + bodyPartID);
		return null;
	}

	public static BodyPart GetRandomExtremety() {
		List<BodyPart> parts = new List<BodyPart>() {
			GetBodyPart("head"), GetBodyPart("arm"), GetBodyPart("leg"), GetBodyPart("leg"), GetBodyPart("tail"), 
			GetBodyPart("wing"), GetBodyPart("foreleg"), GetBodyPart("hindleg"), GetBodyPart("hindleg"), GetBodyPart("tail")
		};

		BodyPart bodyPart = parts.GetRandom();

		return bodyPart;
	}
}
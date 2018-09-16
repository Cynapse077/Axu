using UnityEngine;
using LitJson;
using System.IO;
using System.Collections.Generic;

public static class FactionList {

    static List<Faction> factions;
	public static string dataPath;

    public static void InitializeFactionList() {
		if (factions != null)
			return;
		
        factions = new List<Faction>();
		string jsonString = File.ReadAllText(Application.streamingAssetsPath + dataPath);

		if (string.IsNullOrEmpty(jsonString)) {
			Debug.LogError("Faction List null.");
		}

        JsonData data = JsonMapper.ToObject(jsonString);

        for (int i = 0; i < data["Factions"].Count; i++) {
			JsonData factionJson = data["Factions"][i];

			factions.Add(FactionFromData(factionJson));
        }
    }

	public static Faction FactionFromData(JsonData factionJson) {
		Faction newFaction = new Faction(factionJson["Name"].ToString(), factionJson["ID"].ToString());

		if (factionJson.ContainsKey("Hostile To")) {
			for(int j = 0; j < factionJson["Hostile To"].Count; j++) {
				newFaction.hostileTo.Add(factionJson["Hostile To"][j].ToString());
			}
		}

		return newFaction;
	}

	public static Faction GetFactionByID(string id) {
		for (int i = 0; i < factions.Count; i++) {
			if (id == factions[i].ID)
				return factions[i];
		}

		return factions[0];
	}
}

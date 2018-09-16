using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using LitJson;

public static class Dialogue {
    static string[] greetings;
	static Dictionary<string, string[]> factionDialogues;
	static Dictionary<string, string[]> staticNPCDialogues;

	public static Dictionary<string, string> LogMessages;

	public static string Chat(Faction faction, string npcID) {
		if (factionDialogues == null)
            LoadDialogue();

		if (!string.IsNullOrEmpty(npcID) && staticNPCDialogues.ContainsKey(npcID))
			return staticNPCDialogues[npcID].GetRandom();

		if (faction != null)
			return factionDialogues[faction.ID].GetRandom();

		return "...";
    }

	public static string Greeting(Faction f) {
        if (greetings == null || greetings.Length <= 0)
            LoadDialogue();
		
		if (f.ID == "mutants")
			return "Uraglub.";
		else if (f.ID == "spirits")
			return "Kor nekkhem, beresh";
		
		return greetings.GetRandom(SeedManager.textRandom);
    }

    static void LoadDialogue() {
		string myFile = File.ReadAllText(Application.streamingAssetsPath + "/Data/Dialogue/Dialogue.json");
        JsonData data = JsonMapper.ToObject(myFile);

        greetings = new string[data["Greetings"].Count];
		staticNPCDialogues = new Dictionary<string, string[]>();
		factionDialogues = new Dictionary<string, string[]>();

        for (int i = 0; i < data["Greetings"].Count; i++) {
            greetings[i] = data["Greetings"][i].ToString();
        }

		if (data.ContainsKey("Factions")) {
			for (int i = 0; i < data["Factions"].Count; i++) {
				string key = data["Factions"][i]["ID"].ToString();
				string[] values = new string[data["Factions"][i]["Dialogues"].Count];

				for (int j = 0; j < values.Length; j++) {
					values[j] = data["Factions"][i]["Dialogues"][j].ToString();
				}

				factionDialogues.Add(key, values);
			}
		}

		if (data.ContainsKey("Static NPCs")) {
			for (int i = 0; i < data["Static NPCs"].Count; i++) {
				string key = data["Static NPCs"][i]["ID"].ToString();
				string[] values = new string[data["Static NPCs"][i]["Dialogues"].Count];

				for (int j = 0; j < values.Length; j++) {
					values[j] = data["Static NPCs"][i]["Dialogues"][j].ToString();
				}

				staticNPCDialogues.Add(key, values);
			}
		}
    }

	public static void SelectPressed(DialogueController.DialogueChoice choice) {
		choice.callBack();
	}
}

using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public static class DialogueList {
	public static List<NewDialogue> dialogues;

	public static void Init() {
		dialogues = new List<NewDialogue>();

		string listToJson = File.ReadAllText(Application.streamingAssetsPath + "/Data/Dialogue/DialogueOptions.json");
		JsonData dat = JsonMapper.ToObject(listToJson);

		for (int i = 0; i < dat.Count; i++) {
			GetDialogue(dat[i]);
		}
	}

	static NewDialogue GetDialogue(JsonData dat) {
		string id = dat["ID"].ToString();
		string display = dat["Display"].ToString();
		List<DialogueResponse> resp = new List<DialogueResponse>();

		for (int i = 0; i < dat["Responses"].Count; i++) {
			string disp = dat["Responses"][i]["Display"].ToString();
			string nextID = dat["Responses"][i]["GoTo"].ToString();
			resp.Add(new DialogueResponse(disp, nextID));
		}


		NewDialogue d = new NewDialogue(id, display, resp);

		return d;
	}

	public struct NewDialogue {
		public string id;
		public string display;
		public List<DialogueResponse> options;

		public NewDialogue(string _id, string _display, List<DialogueResponse> dops) {
			id = _id;
			display = _display;
			options = dops;
		}
	}

	public struct DialogueResponse {
		public string display;
		public string nextID;

		public DialogueResponse(string dis, string next) {
			display = dis;
			nextID = next;
		}
	}
}

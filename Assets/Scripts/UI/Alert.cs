using UnityEngine;
using System.IO;
using LitJson;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class Alert {
	static Dictionary<string, string[]> Alerts;
	static UIWindow previousWindow = UIWindow.None;

	static UserInterface userInterface {
		get { return World.userInterface; }
	}

	public static void LoadAlerts() {
		Alerts = new Dictionary<string, string[]>();

		string myFile = File.ReadAllText(Application.streamingAssetsPath + LocalizationManager.filePath);
		JsonData data = JsonMapper.ToObject(myFile);

		for (int i = 0; i < data["Alerts"].Count; i++) {
			string key = data["Alerts"][i]["ID"].ToString();
			string[] value = new string[2] { data["Alerts"][i]["Title"].ToString(), data["Alerts"][i]["Message"].ToString() };

			Alerts.Add(key, value);
		}
	}

	public static void CustomAlert(string content) {
		userInterface.NewAlert("", content);
		previousWindow = UIWindow.None;
	}

	public static void CustomAlert_WithTitle(string title, string content) {
		userInterface.NewAlert(title, content);
		previousWindow = UIWindow.None;
	}

	public static void NewAlert(string alertKey, UIWindow _previousWindow = UIWindow.None) {
		if (Alerts == null)
			LoadAlerts();

		if (Alerts.ContainsKey(alertKey)) {
			string title = Alerts[alertKey][0];
			string message = Alerts[alertKey][1];
				
			userInterface.NewAlert(title, message);
			previousWindow = _previousWindow;
		} else
			Debug.LogError("No alert with key : " + alertKey);
	}

	public static void NewAlert(string alertKey, string name, string input) {
		if (Alerts == null)
			LoadAlerts();

		if (Alerts.ContainsKey(alertKey)) {
			string title = Alerts[alertKey][0];
			string message = Alerts[alertKey][1];

			if (message.Contains("[NAME]")) {
				if (name != null && name != "")
					message = message.Replace("[NAME]", name);
			}
			if (message.Contains("[INPUT]")){
				if (input != null && input != "")
					message = message.Replace("[INPUT]", input);
			}

			userInterface.NewAlert(title, message);
			previousWindow = UIWindow.None;
		} else
			Debug.Log("No alert with key : " + alertKey);
	}

	public static void CloseAlert() {
		userInterface.CloseAlert();
		userInterface.OpenRelevantWindow(previousWindow);
		previousWindow = UIWindow.None;

		if (!World.userInterface.pickedTrait)
			World.userInterface.LevelUp();
	}
}

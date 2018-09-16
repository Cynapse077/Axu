using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class MyConsole {
	private static List<string> messages = new List<string>();
	private static Vector2 viewPoint;
	
	public static void DrawConsole(Rect area){
		GUI.Box(area,"");
		GUILayout.BeginArea(area);
		viewPoint = GUILayout.BeginScrollView(viewPoint);
		GUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		for(int i = 0; i < messages.Count; i++) {
			GUILayout.Label(messages[i]);
		}
		GUILayout.EndScrollView();
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

	public static void ClearLog() {
		messages.Clear();
	}
	
	public static void NewMessage(string message){
        if (message == "")
            return;
		
        messages.Add(message);
		viewPoint.y = Mathf.Infinity;
	}

	public static void Error(string message){
		string myMessage = "<color=red><b>[Error: " + message + "]</b></color>";
		messages.Add(myMessage);
		viewPoint.y = Mathf.Infinity;
	}

	public static void NewMessageColor(string message, Color col) {
		string m = "<color=";

		if(col == Color.red) {
			m += "red>";
		}
		if(col == Color.blue) {
			m += "blue>";
		}
		if(col == Color.cyan) {
			m += "cyan>";
		}
		if(col == Color.yellow) {
			m += "yellow>";
		}
		if(col == Color.black) {
			m += "black>";
		}
		if(col == Color.green) {
			m += "green>";
		}
		if(col == Color.grey) {
			m += "grey>";
		}


		m += message;
		m += "</color>";
		messages.Add(m);
		viewPoint.y = Mathf.Infinity;
	}

	public static void DoubleLine() {
		string s = "";
		for(int i = 0; i < 16; i++) {
			s += "<color=aqua>=</color>";
			s += "<color=orange>=</color>";
		}
		messages.Add(s);
		viewPoint.y = Mathf.Infinity;
	}

    public static void NewHelpLine(string title, string desc) {
        NewMessage("-<b>" + title + "</b>\n      " + desc + ".");
    }
}

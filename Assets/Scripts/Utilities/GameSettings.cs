using UnityEngine;
using System.IO;
using LitJson;

public static class GameSettings {
	public static InputKeys Keybindings;

	public static string LastName;
	public static double SE_Volume, Mus_Volume, Master_Volume, Animation_Speed;
	public static bool SimpleDamage, MuteAll;
	public static bool UseMouse, Fullscreen, Enable_Weather;
	public static bool Allow_Console, Particle_Effects;
	public static Coord ScreenSize;

	public static string version {
		get {
			return "0.7.1";
		}
	}

	public static void InitializeFromFile() {
		Allow_Console = true;

		if (File.Exists(Manager.SettingsDirectory)) {
			string jsonString = File.ReadAllText(Manager.SettingsDirectory);

			if (string.IsNullOrEmpty(jsonString)) {
				Debug.LogError("Settings file null.");
				return;
			}

			JsonData dat = JsonMapper.ToObject(jsonString);

			if (dat.Keys.Contains("LastName") && dat["LastName"] != null)
				LastName = dat["LastName"].ToString();
			else
				LastName = "";
			
			if (dat.Keys.Contains("ScreenSize") && dat["ScreenSize"].Count == 2)
				ScreenSize = new Coord((int)dat["ScreenSize"][0], (int)dat["ScreenSize"][1]);
			else
				ScreenSize = new Coord(1600, 900);
			
			if (dat.Keys.Contains("Master_Volume"))
				Master_Volume = (dat["Master_Volume"].IsDouble) ? (double)dat["Master_Volume"] : 1.0;
			else
				Master_Volume = 1.0;
			
			if (dat.Keys.Contains("Music_Volume"))
				Mus_Volume = (dat["Music_Volume"].IsDouble) ? (double)dat["Music_Volume"] : 1.0;
			else
				Mus_Volume = 1.0;
			
			if (dat.Keys.Contains("SFX_Volume"))
				SE_Volume = (dat["SFX_Volume"].IsDouble) ? (double)dat["SFX_Volume"] : 1.0;
			else
				SE_Volume = 1.0;
			
			if (dat.Keys.Contains("Mute"))
				MuteAll = (bool)dat["Mute"];
			else
				MuteAll = false;
			
			if (dat.Keys.Contains("Fullscreen"))
				Fullscreen = (bool)dat["Fullscreen"];
			else
				Fullscreen = false;
			
			if (dat.Keys.Contains("UseMouse"))
				UseMouse = (bool)dat["UseMouse"];
			else
				UseMouse = false;
			
			if (dat.ContainsKey("Weather"))
				Enable_Weather = (bool)dat["Weather"];
			else
				Enable_Weather = true;
			
			if (dat.Keys.Contains("Animation_Speed"))
				Animation_Speed = (double)dat["Animation_Speed"];
			else
				Animation_Speed = 40.0;
			
			if (dat.ContainsKey("Particle_Effects"))
				Particle_Effects = (bool)dat["Particle_Effects"];
			else
				Particle_Effects = true;
			
			if (dat.ContainsKey("SimpleDmg"))
				SimpleDamage = (bool)dat["SimpleDmg"];
			else
				SimpleDamage = false;

			if (dat.Keys.Contains("Input"))
				Keybindings = new InputKeys(dat);
			else {
				Keybindings = new InputKeys();

				if (ObjectManager.player != null)
					ObjectManager.player.GetComponent<PlayerInput>().NewKeybindingClass();
			}
			
		} else {
			Defaults();
		}

		Save();
	}

	public static void Save() {
		SSettings settings = new SSettings(LastName, Fullscreen, ScreenSize, Master_Volume, Mus_Volume, SE_Volume, MuteAll, 
			UseMouse, Keybindings, Animation_Speed, Enable_Weather, Particle_Effects, SimpleDamage);

		JsonData data = JsonMapper.ToJson(settings);
		string prettyData = data.ToString();
		prettyData = JsonHelper.PrettyPrint(prettyData);
		File.WriteAllText(Manager.SettingsDirectory, prettyData);

		Screen.SetResolution(ScreenSize.x, ScreenSize.y, Fullscreen);
	}

	public static void Defaults() {
		SE_Volume = 1.0;
		Mus_Volume = 1.0;
		Master_Volume = 1.0;

		MuteAll = false;
		Fullscreen = false;
		ScreenSize = new Coord(1600, 900);
		UseMouse = false;
		Allow_Console = true;
		Animation_Speed = 40.0;
		Enable_Weather = true;
		Particle_Effects = true;
		SimpleDamage = false;
		LastName = "";

		Keybindings = new InputKeys();

		if (ObjectManager.player != null)
			ObjectManager.player.GetComponent<PlayerInput>().NewKeybindingClass();
	}
}
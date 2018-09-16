using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Options_GamePanel : MonoBehaviour {

	public Dropdown resoDropdown;
	[Space(5)]
	public Toggle fullToggle;
	public Toggle muteToggle;
	public Toggle mouseToggle;
	public Toggle simpleDmgToggle;
	public Toggle consoleToggle;
	public Toggle weatherToggle;
	public Toggle particleToggle;
	[Space(5)]
	public Slider animSlider;
	public Slider masterSlider;
	public Slider musSlider;
	public Slider sfxSlider;

	public void Initialize() {
		ResolutionDropDownSelect();

		fullToggle.isOn = GameSettings.Fullscreen;
		muteToggle.isOn = GameSettings.MuteAll;
		mouseToggle.isOn = GameSettings.UseMouse;
		simpleDmgToggle.isOn = GameSettings.SimpleDamage;
		consoleToggle.isOn = GameSettings.Allow_Console;
		weatherToggle.isOn = GameSettings.Enable_Weather;
		particleToggle.isOn = GameSettings.Particle_Effects;

		animSlider.minValue = 10;
		animSlider.maxValue = 50;
		animSlider.value = (float)GameSettings.Animation_Speed;

		masterSlider.value = (float)GameSettings.Master_Volume / 2f;
		musSlider.value = (float)GameSettings.Mus_Volume / 2f;
		sfxSlider.value = (float)GameSettings.SE_Volume / 2f;
	}

	public void ResolutionDropDownSelect() {
		if (GameSettings.ScreenSize == null) {
			resoDropdown.value = 0;
			return;
		}
		if (GameSettings.ScreenSize.x <= 1280)
			resoDropdown.value = 0;
		else if (GameSettings.ScreenSize.x <= 1366)
			resoDropdown.value = 1;
		else if (GameSettings.ScreenSize.x <= 1600)
			resoDropdown.value = 2;
		else if (GameSettings.ScreenSize.x <= 1920)
			resoDropdown.value = 3;
		else
			resoDropdown.value = 3;
	}

	public void ResolutionChange() {
		if (resoDropdown.value == 0) {
			GameSettings.ScreenSize.x = 1280;
			GameSettings.ScreenSize.y = 720;
		} 
		else if (resoDropdown.value == 1) {
			GameSettings.ScreenSize.x = 1366;
			GameSettings.ScreenSize.y = 786;
		} 
		else if (resoDropdown.value == 2) {
			GameSettings.ScreenSize.x = 1600;
			GameSettings.ScreenSize.y = 900;
		} 
		else if (resoDropdown.value == 3) {
			GameSettings.ScreenSize.x = 1920;
			GameSettings.ScreenSize.y = 1080;
		}
		Screen.SetResolution(GameSettings.ScreenSize.x, GameSettings.ScreenSize.y, GameSettings.Fullscreen);
	}

	public void SetFullscreen() {
		if (GameSettings.ScreenSize != null) {
			GameSettings.Fullscreen = fullToggle.isOn;
			Screen.SetResolution(GameSettings.ScreenSize.x, GameSettings.ScreenSize.y, GameSettings.Fullscreen);
		}
	}
	public void SetMute() {
		GameSettings.MuteAll = muteToggle.isOn;
	}
	public void SetMouse() {
		GameSettings.UseMouse = mouseToggle.isOn;
	}
	public void SetSimpleDmg() {
		GameSettings.SimpleDamage = simpleDmgToggle.isOn;
	}
	public void SetConsole() {
		GameSettings.Allow_Console = consoleToggle.isOn;
	}
	public void SetWeather() {
		GameSettings.Enable_Weather = weatherToggle.isOn;
	}
	public void SetParticles() {
		GameSettings.Particle_Effects = particleToggle.isOn;
	}

	public void SetAnimSpeed() {
		GameSettings.Animation_Speed = (double)animSlider.value;
	}
	public void SetMasterVolume() {
		GameSettings.Master_Volume = (double)masterSlider.value * 2;
	}
	public void SetMusicVolume() {
		GameSettings.Mus_Volume = (double)musSlider.value * 2;
	}
	public void SetSFXrVolume() {
		GameSettings.SE_Volume = (double)sfxSlider.value * 2;
	}
}

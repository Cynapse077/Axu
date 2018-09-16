using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class MainMenuPanel : MonoBehaviour {

	public Button continueButton;
	public Text versionText;
	public GameObject titleAndButtons;
	public GameObject optionsMenu;
	public GameObject loadingObject;

	void Start () {
		loadingObject = GameObject.Find("LoadingText");
		loadingObject.SetActive(false);
		versionText.text = "(v. <color=cyan>" + GameSettings.version + "</color>)";
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.Escape))
			CloseOptionsMenu();
	}

	public void DisableContinueButton() {
		continueButton.gameObject.SetActive(false);
	}

	public void StartGame() {
		titleAndButtons.SetActive(false);
	}

	public void GoToURL(string url) {
		Application.OpenURL(url);
	}

	public void OpenMapEditor() {
		loadingObject.SetActive(true);
		UnityEngine.SceneManagement.SceneManager.LoadScene(3);
	}

	public void DeleteSaveData() {
		File.Delete(Manager.SaveDirectory);
		File.Delete(Manager.SettingsDirectory);
		UnityEngine.SceneManagement.SceneManager.LoadScene(0);
	}

	public void ContinuePressed() {
		GameObject.FindObjectOfType<MainMenu>().Continue();
	}

	public void NewGamePressed() {
		GameObject.FindObjectOfType<MainMenu>().New();
	}

	public void QuitPressed() {
		GameObject.FindObjectOfType<MainMenu>().Quit();
	}

	public void OpenOptions() {
		optionsMenu.SetActive(true);
	}

	public void CloseOptionsMenu() {
		optionsMenu.SetActive(false);
	}
}

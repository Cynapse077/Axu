using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class YesNoPanel : MonoBehaviour
{
	public Text question;
	public Button yesButton;
	public Button noButton;

	Action YesCallback;
	Action NoCallback;
	bool canInput = false;
	int column = 1;

	public void Display(string text, Action ycb, Action ncb, string input)
    {
		if (LocalizationManager.GetContent(text) != LocalizationManager.defaultText)
			GetLocalizedContent(text, input);
		else
			question.text = text;
		
		YesCallback = ycb;
		NoCallback = ncb;

		Setup();
	}

	void GetLocalizedContent(string qs, string input)
    {
		string s = LocalizationManager.GetContent(qs);

		if (s.Contains("[INPUT]") && !string.IsNullOrEmpty(input))
        {
			s = s.Replace("[INPUT]", input);
        }

		question.text = s;
	}

	void Setup()
    {
		canInput = true;
		column = 1;
		yesButton.onClick.RemoveAllListeners();
		yesButton.onClick.AddListener(() => { YesCallback(); });
		noButton.onClick.RemoveAllListeners();
		noButton.onClick.AddListener(() => { NoCallback(); });
		EventSystem.current.SetSelectedGameObject(noButton.gameObject);
	}

	void Update()
    {
		HandleKeys();
	}

	void HandleKeys()
    {
		if (!canInput)
			return;
		
		if (GameSettings.Keybindings.GetKey("East") && column < 1)
        {
			column++;
			World.soundManager.MenuTick();
			EventSystem.current.SetSelectedGameObject(noButton.gameObject);
		}

		if (GameSettings.Keybindings.GetKey("West") && column > 0)
        {
			column--;
			World.soundManager.MenuTick();
			EventSystem.current.SetSelectedGameObject(yesButton.gameObject);
		}

		if (GameSettings.Keybindings.GetKey("Enter"))
			Callback(column == 0 ? YesCallback : NoCallback);
		else if (Input.GetKeyDown(KeyCode.Y))
			Callback(YesCallback);
		else if (Input.GetKeyDown(KeyCode.N) || GameSettings.Keybindings.GetKey("Pause"))
			Callback(NoCallback);
	}

	void Yes()
    {
		if (YesCallback != null)
			Callback(YesCallback);
	}

	void No()
    {
		if (NoCallback != null)
			Callback(NoCallback);
	}

	void Callback(Action callback)
    {
		canInput = false;
		callback();
	}
}

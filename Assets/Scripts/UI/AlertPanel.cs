using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class AlertPanel : MonoBehaviour {

	public Text title;
	public Text content;
	public Button continueButton;

	public void NewAlert(string _title, string _content) {
		title.text = _title;
		content.text = _content;
		EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
	}
}

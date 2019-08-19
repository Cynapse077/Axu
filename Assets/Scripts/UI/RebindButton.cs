using UnityEngine;
using UnityEngine.UI;

public class RebindButton : MonoBehaviour {

	public Text KeyNameText;
	public Text KeyCodeText;
	public Button button;

	public void SetTexts(string bindingName, string keyName) {
		KeyNameText.text = LocalizationManager.GetContent(bindingName);
		KeyCodeText.text = keyName;
	}
}

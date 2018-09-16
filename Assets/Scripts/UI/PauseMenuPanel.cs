using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenuPanel : MonoBehaviour {

	public GameObject[] buttons;

	void OnEnable() {
		EventSystem.current.SetSelectedGameObject(buttons[0]);
	}

    void Update() {

    }

	public void UpdateSelected(int index) {
        if (index < 0 || index > buttons.Length - 1)
            return;

		EventSystem.current.SetSelectedGameObject(buttons[index]);
	}
}

using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CloseWindowsButton : MonoBehaviour {

	void Start() {
		GetComponent<Button>().onClick.AddListener(() => { World.userInterface.CloseWindows(); });
	}
}

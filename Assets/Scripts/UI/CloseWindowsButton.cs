using UnityEngine;
using UnityEngine.UI;

public class CloseWindowsButton : MonoBehaviour
{
	void Start()
    {
		GetComponent<Button>().onClick.AddListener(() => { World.userInterface.CloseWindows(); });
	}
}

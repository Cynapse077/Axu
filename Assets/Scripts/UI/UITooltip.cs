using UnityEngine;
using UnityEngine.UI;

public class UITooltip : MonoBehaviour
{
    public static UITooltip instance;

	RectTransform rectTransform;
    Vector3 hiddenPos = new Vector3(0, 0, -300);
    Text text;

	void OnEnable()
    {
        instance = this;
        text = GetComponentInChildren<Text>();
        rectTransform = GetComponent<RectTransform>();
	}

    public void Hide()
    {
        transform.position = hiddenPos;
    }

    public void Show(Vector3 pos, string txt)
    {
        transform.position = pos;
        rectTransform.pivot = new Vector3(pos.x > Screen.width * 0.5f ? 1 : 0, 1, pos.z);
        text.text = txt;
    }
}

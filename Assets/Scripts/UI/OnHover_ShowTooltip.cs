using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OnHover_ShowTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string textToDisplay;

    Transform tooltip;
    bool hovering = false;

    void Start()
    {
        tooltip = GameObject.FindObjectOfType<UITooltip>().transform;
    }

    void OnDisable()
    {
        if (hovering)
        {
            tooltip.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData ev)
    {
        hovering = true;

        if (string.IsNullOrEmpty(textToDisplay))
        {
            tooltip.gameObject.SetActive(false);
        }
        else
        {
            tooltip.gameObject.SetActive(true);
            tooltip.position = Input.mousePosition;
            tooltip.GetComponentInChildren<Text>().text = textToDisplay;
        }
    }

    public void OnPointerExit(PointerEventData ev)
    {
        hovering = false;
        tooltip.GetComponentInChildren<Text>().text = "";
        tooltip.gameObject.SetActive(false);
    }
}

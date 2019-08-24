using UnityEngine;
using UnityEngine.EventSystems;

public class OnHover_ShowTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string textToDisplay;

    bool hovering = false;

    void OnDisable()
    {
        if (hovering)
        {
            UITooltip.instance.Hide();
        }
    }

    public void OnPointerEnter(PointerEventData ev)
    {
        hovering = true;

        if (string.IsNullOrEmpty(textToDisplay))
        {
            UITooltip.instance.Hide();
        }
        else
        {
            UITooltip.instance.Show(Input.mousePosition, textToDisplay);
        }
    }

    public void OnPointerExit(PointerEventData ev)
    {
        hovering = false;
        UITooltip.instance.Hide();
    }
}

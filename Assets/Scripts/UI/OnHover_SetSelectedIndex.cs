using UnityEngine;
using UnityEngine.EventSystems;

public class OnHover_SetSelectedIndex : MonoBehaviour, IPointerEnterHandler
{
    public int column = 0;
    public int offset = 0;
    public UIWindow window;
    public bool selectBodyPart;
    public bool selectAction;

    public void SetHoverMode(int col, UIWindow win, bool bp = false, bool action = false)
    {
        column = col;
        window = win;
        selectBodyPart = bp;
        selectAction = action;
    }

    public void OnPointerEnter(PointerEventData ped)
    {
        if (window != UIWindow.None && World.userInterface.CurrentState() != window)
            return;
        if (selectAction != World.userInterface.SelectItemActions || selectBodyPart != World.userInterface.SelectBodyPart)
            return;

        if (column > 1)
        {
            if (World.userInterface.SelectBodyPart)
                World.userInterface.SSPanel.SetSelectedNum(transform.GetSiblingIndex() + offset);
            else if (World.userInterface.SelectItemActions)
                World.userInterface.IAPanel.SetSelectedNum(transform.GetSiblingIndex() + offset);
            else if (World.userInterface.DPanel.gameObject.activeSelf || World.userInterface.pausePanel.gameObject.activeSelf)
                World.userInterface.SetSelectedNumber(transform.GetSiblingIndex() + offset);
        }
        else
        {
            World.userInterface.column = column;
            World.userInterface.SetSelectedNumber(transform.GetSiblingIndex() + offset);
        }

        if (window == UIWindow.Inventory)
        {
            if (World.userInterface.column == 0)
                World.userInterface.EqPanel.ChangeSelectedNum(transform.GetSiblingIndex() + offset);
            else if (World.userInterface.column == 1)
                World.userInterface.InvPanel.ChangeSelectedNum(transform.GetSiblingIndex() + offset);

            World.userInterface.InvPanel.UpdateTooltip();
        }

        if (window == UIWindow.ReplacePartWithItem)
        {
            World.userInterface.RLPanel.SetSelectedNum(transform.GetSiblingIndex() + offset);
        }
    }
}
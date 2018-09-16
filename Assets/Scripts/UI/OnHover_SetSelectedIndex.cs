using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class OnHover_SetSelectedIndex : MonoBehaviour, IPointerEnterHandler {

	public int column = 0;
	public int offset = 0;
	public UIWindow window;
	public bool selectBodyPart;
	public bool selectAction;

	public void SetHoverMode(int col, UIWindow win, bool bp = false, bool action = false) {
		column = col;
		window = win;
		selectBodyPart = bp;
		selectAction = action;
	}

	public void OnPointerEnter(PointerEventData ped) {
		if (window != UIWindow.None && World.userInterface.CurrentState() != window)
			return;
		if (window == UIWindow.Inventory) {
			if (selectAction != World.userInterface.selectedItemActions || selectBodyPart != World.userInterface.selectBodyPart)
				return;
		}
		if (column > 1) {
			if (World.userInterface.selectBodyPart)
				World.userInterface.SSPanel.SetSelectedNum(transform.GetSiblingIndex() + offset);
			else if (World.userInterface.selectedItemActions)
				World.userInterface.IAPanel.SetSelectedNum(transform.GetSiblingIndex() + offset);
			else if (World.userInterface.DPanel.gameObject.activeSelf || World.userInterface.pausePanel.gameObject.activeSelf)
				World.userInterface.SetSelectedNumber(transform.GetSiblingIndex() + offset);
		} else {
			World.userInterface.column = column;
			World.userInterface.SetSelectedNumber(transform.GetSiblingIndex() + offset);
		}
	}
}
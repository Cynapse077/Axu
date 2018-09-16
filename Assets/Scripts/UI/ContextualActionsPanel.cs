using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ContextualActionsPanel : MonoBehaviour {
    public GameObject buttonPrefab;
    public Transform anchor;

    int currIndex = 0;
    List<ContextualMenu.ContextualAction> actions;

    int selectedMax {
        get { return anchor.childCount - 1; }
    }

    public void Refresh(List<ContextualMenu.ContextualAction> ac) {
        while (anchor.childCount > 0) {
            RemoveChild(anchor.GetChild(0));
        }

        actions = new List<ContextualMenu.ContextualAction>(ac);

        for (int i = 0; i < actions.Count; i++) {
            GameObject g = SimplePool.Spawn(buttonPrefab, anchor);
            g.GetComponent<Button>().onClick.AddListener(() => DoAction(i));
            g.GetComponentInChildren<Text>().text = actions[i].actionName;
        }
    }

    void DoAction(int index) {
        World.userInterface.CloseWindows();
        actions[index].myAction();
    }

    void RemoveChild(Transform c) {
        c.SetParent(null);
        c.GetComponent<Button>().onClick.RemoveAllListeners();
        SimplePool.Despawn(c.gameObject);
    }
	
	void Update () {
        if (selectedMax <= 0)
            return;

        if (GameSettings.Keybindings.GetKey("North")) {
            currIndex--;

            if (currIndex < 0)
                currIndex = selectedMax;
        } else if (GameSettings.Keybindings.GetKey("South")) {
            currIndex++;

            if (currIndex > selectedMax)
                currIndex = 0;
        }

        if (GameSettings.Keybindings.GetKey("Enter")) {
            DoAction(currIndex);
        }

        if (anchor.childCount > 0)
            EventSystem.current.SetSelectedGameObject(anchor.GetChild(currIndex).gameObject);
	}
}

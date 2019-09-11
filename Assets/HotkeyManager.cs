using System;
using System.Collections.Generic;
using UnityEngine;

public class HotkeyManager : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform anchor;
    List<HotkeyButton> buttons;

    void Start()
    {
        anchor.DestroyChildren();
        buttons = new List<HotkeyButton>();
    }

    public void Initialize()
    {
        anchor.DestroyChildren();
        buttons = new List<HotkeyButton>();

        int amt = Mathf.Min(ObjectManager.playerEntity.skills.abilities.Count, 10);

        for (int i = 0; i < amt; i++)
        {
            GameObject g = Instantiate(buttonPrefab, anchor);
            HotkeyButton hkb = g.GetComponent<HotkeyButton>();
            buttons.Add(hkb);
            hkb.Initialize(i);
        }
    }

    public void AssignAction(Ability ability, int id, Action action)
    {
        if (id < 0 || id >= buttons.Count)
        {
            return;
        }

        buttons[id].Setup(ability, action);
    }

    void Update()
    {
        foreach (HotkeyButton h in buttons)
        {
            if (Input.GetKeyDown(h.keyCode))
            {
                h.DoAction();
                break;
            }
        }
    }
}

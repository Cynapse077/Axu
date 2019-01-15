﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemActionsPanel : MonoBehaviour
{
    public GameObject Title;
    public GameObject ActionButton;
    public int selectedNum = 0;

    [HideInInspector]
    public Item selectedItem;

    int numActions;
    Inventory curInv;

    public void Display(Item item, Inventory inv)
    {
        if (!gameObject.activeSelf)
            return;

        curInv = inv;

        transform.DespawnChildren();
        Instantiate(Title, transform);

        selectedItem = item;
        ItemActions actions = new ItemActions(selectedItem);
        numActions = actions.Actions.Count;

        for (int i = 0; i < numActions; i++)
        {
            GameObject g = SimplePool.Spawn(ActionButton, transform);

            g.GetComponentInChildren<Text>().text = LocalizationManager.GetLocalizedContent(actions.Actions[i].Display)[0];
            g.GetComponent<Button>().onClick.AddListener(() => { SelectPressed(i); });
            g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(2, UIWindow.Inventory, false, true);
        }

        SetSelectedNum(0);
    }

    void Update()
    {
        if (World.playerInput.keybindings.GetKey("Enter"))
        {
            SelectPressed(UserInterface.selectedItemNum);
        }
    }

    void SelectPressed(int index)
    {
        Item item = World.userInterface.IAPanel.selectedItem;
        ItemActions actions = new ItemActions(item);
        string actionName = actions.Actions[World.userInterface.IAPanel.selectedNum].Key;

        if (actionName == "Throw")
        {
            if (curInv == ObjectManager.playerEntity.inventory)
            {
                ObjectManager.playerEntity.fighter.SelectItemToThrow(item);
                World.playerInput.ToggleThrow();
            }

            World.userInterface.CloseWindows();
            return;
        }
        else if (actionName == "Equip" && curInv.entity.body.GetBodyPartsBySlot(item.GetSlot()).Count > 1)
        {
            World.userInterface.SSPanel.gameObject.SetActive(true);
            World.userInterface.SSPanel.Display(item, curInv);
        }
        else if (actionName == "Wield")
        {
            int armok = curInv.entity.body.AttachedArms().Count;

            if (armok > 1)
            {
                World.userInterface.SSPanel.gameObject.SetActive(true);
                World.userInterface.SSPanel.Wield(item, curInv);
            }
            else if (armok > 0)
            {
                curInv.Wield(item, curInv.entity.body.MainHandIndex);
            }
        }
        else if (actionName == "Drop All")
        {
            curInv.DropAllOfType(item);
        }
        else
        {
            System.Reflection.MethodInfo mi = curInv.GetType().GetMethod(actionName);

            if (mi != null)
            {
                mi.Invoke(curInv, new object[] { item });
            }
        }

        if (actionName == "Use" && curInv == ObjectManager.playerEntity.inventory)
        {
            if (item.HasProp(ItemProperty.Blink) || item.HasProp(ItemProperty.Surface_Tele))
            {
                World.userInterface.CloseWindows();
                return;
            }
        }

        World.userInterface.InitializeAllWindows(curInv);
        gameObject.SetActive(false);
    }

    public void SetSelectedNum(int num)
    {
        selectedNum = num;

        if (selectedNum < 0)
            selectedNum = numActions - 1;

        else if (selectedNum >= numActions)
            selectedNum = 0;

        EventSystem.current.SetSelectedGameObject(transform.GetChild(selectedNum + 1).gameObject);
    }

    public void SwitchSelectedNum(int amount)
    {
        selectedNum += amount;

        if (selectedNum < 0)
            selectedNum = numActions - 1;
        else if (selectedNum >= numActions)
            selectedNum = 0;

        EventSystem.current.SetSelectedGameObject(transform.GetChild(selectedNum + 1).gameObject);
    }
}

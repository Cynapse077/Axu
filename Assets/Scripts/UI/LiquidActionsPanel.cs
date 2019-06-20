﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LiquidActionsPanel : MonoBehaviour
{
    public Button onGround;
    public Button onSelf;
    public Button onWeapon;
    public Button cancel;
    public Transform anchor;
    Item cont;

    public void Init(Item i)
    {
        cont = i;

        CLiquidContainer cl = cont.GetCComponent<CLiquidContainer>();

        if (cl == null)
        {
            World.userInterface.CloseWindows();
            return;
        }

        onGround.onClick.RemoveAllListeners();
        onGround.onClick.AddListener(OnGround);
        onSelf.onClick.RemoveAllListeners();
        onSelf.onClick.AddListener(OnSelf);
        onWeapon.onClick.RemoveAllListeners();
        onWeapon.onClick.AddListener(OnWeapon);
    }

    void OnGround()
    {
        CLiquidContainer cl = cont.GetCComponent<CLiquidContainer>();
        World.objectManager.CreatePoolOfLiquid(ObjectManager.playerEntity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, cl.sLiquid.ID, cl.sLiquid.units);

        cl.SetLiquidVolume(0);
        cl.CheckLiquid();
        World.userInterface.CloseWindows();
    }

    void OnSelf()
    {
        CLiquidContainer cl = cont.GetCComponent<CLiquidContainer>();
        Liquid liquid = ItemList.GetLiquidByID(cl.sLiquid.ID, cl.sLiquid.units);
        liquid.Splash(ObjectManager.playerEntity.stats);
        cl.SetLiquidVolume(cl.sLiquid.units - 1);

        cl.CheckLiquid();
        World.userInterface.CloseWindows();
    }

    void OnWeapon()
    {
        World.userInterface.ItemOnItem_Coat(cont, ObjectManager.playerEntity.inventory);
    }

    void Cancel()
    {
        World.userInterface.CloseWindows();
    }

    void SelectPressed(int index)
    {
        switch (index)
        {
            case 0: OnGround(); break;
            case 1: OnSelf(); break;
            case 2: OnWeapon(); break;
            case 3: Cancel(); break;
        }
    }

    void Update()
    {
        EventSystem.current.SetSelectedGameObject(anchor.GetChild(UserInterface.selectedItemNum).gameObject);

        if (GameSettings.Keybindings.GetKey("Enter"))
        {
            SelectPressed(UserInterface.selectedItemNum);
        }
    }
}

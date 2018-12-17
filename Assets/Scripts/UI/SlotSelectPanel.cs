using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SlotSelectPanel : MonoBehaviour
{
    public GameObject title;
    public GameObject button;
    Inventory curInv;
    List<BodyPart> parts;
    public int selectedNum = 0;
    public bool displayingBP = true;

    [HideInInspector]
    public Item selectedItem;

    int numActions;

    public void Display(Item it, Inventory inv)
    {
        displayingBP = true;

        if (!gameObject.activeSelf)
            return;

        selectedItem = it;
        curInv = inv;

        transform.DestroyChildren();

        Instantiate(title, transform);
        parts = curInv.entity.body.GetBodyPartsBySlot(selectedItem.GetSlot());
        numActions = parts.Count;

        for (int i = 0; i < numActions; i++)
        {
            GameObject g = Instantiate(button, transform);
            g.GetComponent<Button>().onClick.AddListener(() => { SelectPressed(i); });
            g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(2, UIWindow.Inventory, true, false);
            g.GetComponentInChildren<Text>().text = string.Format("{0} - {1}", parts[i].displayName, parts[i].equippedItem.InvDisplay(inv.baseWeapon, true));
        }

        EventSystem.current.SetSelectedGameObject(null);
        SetSelectedNum(0);
    }

    void Update()
    {
        if (World.playerInput.keybindings.GetKey("Enter"))
            SelectPressed(UserInterface.selectedItemNum);

        EventSystem.current.SetSelectedGameObject(transform.GetChild(selectedNum + 1).gameObject);
    }

    void SelectPressed(int index)
    {
        Inventory curInv = World.userInterface.InvPanel.curInv;

        if (displayingBP)
        {
            List<BodyPart> bps = curInv.entity.body.GetBodyPartsBySlot(selectedItem.GetSlot());
            curInv.EquipDirectlyToBodyPart(selectedItem, bps[selectedNum]);
        }
        else
            curInv.Wield(selectedItem, selectedNum);

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

        if (transform.childCount > 1)
            EventSystem.current.SetSelectedGameObject(transform.GetChild(selectedNum + 1).gameObject);
    }

    public void Wield(Item it, Inventory inv)
    {
        displayingBP = false;

        if (!gameObject.activeSelf)
        {
            return;
        }

        selectedItem = it;
        curInv = inv;

        transform.DestroyChildren();

        Instantiate(title, transform);
        parts = curInv.entity.body.GetBodyPartsBySlot(selectedItem.GetSlot());
        numActions = inv.entity.body.Hands.Count;

        for (int i = 0; i < numActions; i++)
        {
            GameObject g = Instantiate(button, transform);
            g.GetComponent<Button>().onClick.AddListener(() => { SelectPressed(i); });
            g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(2, UIWindow.Inventory, true, false);

            string n = LocalizationManager.GetContent("Slot_Hand") + " " + ((i % 2 == 0) ? LocalizationManager.GetContent("Limb_Right") : LocalizationManager.GetContent("Limb_Left"));

            n = ((curInv.entity.body.Hands[i] == curInv.entity.body.MainHand) ? "<color=yellow>" : "<color=white>") + n + "</color>";

            g.GetComponentInChildren<Text>().text = n + " - " + inv.entity.body.Hands[i].EquippedItem.InvDisplay(inv.baseWeapon);
        }
    }
}

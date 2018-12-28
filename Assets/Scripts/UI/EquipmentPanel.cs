using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentPanel : UIPanel
{
    [Header("Prefabs")]
    public GameObject equipmentButton;
    [Header("Children")]
    public Transform equipmentBase;
    public Scrollbar scrollBar;

    public Inventory curInv;


    public void Init(Inventory inv)
    {
        curInv = inv;
        UpdateEquipment();
        initialized = true;
    }

    void UpdateEquipment()
    {
        equipmentBase.DespawnChildren();

        SelectedMax = 0;

        List<BodyPart.Hand> hands = curInv.entity.body.Hands;

        for (int i = 0; i < hands.Count; i++)
        {
            GameObject wep = SimplePool.Spawn(equipmentButton, equipmentBase);
            wep.GetComponentInChildren<Text>().text = hands[i].EquippedItem.InvDisplay(hands[i].baseItem, false, true, false);

            string n = LocalizationManager.GetContent("Slot_Hand") + " " + ((i % 2 == 0) ? LocalizationManager.GetContent("Limb_Right") : LocalizationManager.GetContent("Limb_Left"));

            n = ((hands[i] == curInv.entity.body.MainHand) ? "<color=yellow>" : "<color=orange>") + n + "</color>";

            wep.transform.GetChild(1).GetComponent<Text>().text = n;
            wep.GetComponent<Button>().onClick.AddListener(() => OnSelect(wep.transform.GetSiblingIndex()));
            wep.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(0, UIWindow.Inventory);
            SelectedMax++;
        }

        GameObject fire = SimplePool.Spawn(equipmentButton, equipmentBase);
        fire.GetComponentInChildren<Text>().text = (curInv.firearm == null) ? ItemList.GetNone().Name : curInv.firearm.InvDisplay("none", false, true, true);
        fire.transform.GetChild(1).GetComponent<Text>().text = "<color=orange>" + LocalizationManager.GetContent("TT_Ranged") + "</color>";
        fire.GetComponent<Button>().onClick.AddListener(() => OnSelect(fire.transform.GetSiblingIndex()));
        fire.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(0, UIWindow.Inventory);
        SelectedMax++;

        foreach (BodyPart bp in curInv.entity.body.bodyParts)
        {
            GameObject g = SimplePool.Spawn(equipmentButton, equipmentBase);
            g.GetComponentInChildren<Text>().text = bp.equippedItem.InvDisplay("none", true, false);
            g.transform.GetChild(1).GetComponent<Text>().text = bp.displayName;
            g.GetComponent<Button>().onClick.AddListener(() => OnSelect(g.transform.GetSiblingIndex()));
            g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(0, UIWindow.Inventory);
            SelectedMax++;
        }
    }

    public override void Update()
    {
        if (World.userInterface.column != 0 || World.userInterface.CurrentState() != UIWindow.Inventory || 
            World.userInterface.SelectBodyPart || World.userInterface.SelectItemActions)
            return;

        if (SelectedMax > 0)
        {
            EventSystem.current.SetSelectedGameObject(equipmentBase.GetChild(SelectedNum).gameObject);
        }

        base.Update();
    }

    public override void ChangeSelectedNum(int newIndex)
    {
        if (!World.userInterface.SelectItemActions && !World.userInterface.SelectBodyPart && World.userInterface.column == 0)
        {
            base.ChangeSelectedNum(newIndex);

            if (SelectedMax > 0)
                scrollBar.value = 1f - (SelectedNum / (float)SelectedMax);

            World.userInterface.InvPanel.UpdateTooltip(SelectedNum);
        }
    }

    protected override void OnSelect(int index)
    {
        if (World.userInterface.column != 0 || World.userInterface.SelectBodyPart || World.userInterface.SelectItemActions)
            return;

        List<BodyPart.Hand> hands = curInv.entity.body.Hands;

        if (index < hands.Count)
            curInv.UnEquipWeapon(hands[index].EquippedItem, index);
        else if (index == hands.Count)
            curInv.UnEquipFirearm(true);
        else
            curInv.UnEquipArmor(curInv.entity.body.bodyParts[index - curInv.entity.body.Hands.Count - 1], true);

        World.userInterface.InitializeAllWindows(curInv);
        World.userInterface.InvPanel.UpdateTooltip(SelectedNum);

        base.OnSelect(index);
    }
}

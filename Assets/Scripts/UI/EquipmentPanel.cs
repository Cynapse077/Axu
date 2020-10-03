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
            Color col = hands[i] == curInv.entity.body.MainHand ? Color.yellow : AxuColor.Orange;

            string n = (i % 2 == 0 ? "Limb_Right" : "Limb_Left").Localize("Slot_Hand".Localize()).Color(col);

            wep.transform.GetChild(1).GetComponent<Text>().text = n;
            wep.GetComponent<Button>().onClick.AddListener(() => OnSelect(wep.transform.GetSiblingIndex()));
            wep.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(0, UIWindow.Inventory);
            wep.GetComponent<ItemButton>().icon.sprite = SwitchSprite(hands[i].EquippedItem);
            SelectedMax++;
        }

        GameObject fire = SimplePool.Spawn(equipmentButton, equipmentBase);
        fire.GetComponentInChildren<Text>().text = (curInv.firearm == null) ? ItemList.NoneItem.Name : curInv.firearm.InvDisplay("none", false, true, true);
        fire.transform.GetChild(1).GetComponent<Text>().text = "TT_Ranged".Localize().Color(AxuColor.Orange);
        fire.GetComponent<Button>().onClick.AddListener(() => OnSelect(fire.transform.GetSiblingIndex()));
        fire.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(0, UIWindow.Inventory);
        fire.GetComponent<ItemButton>().icon.sprite = SwitchSprite(curInv.firearm);
        SelectedMax++;

        foreach (BodyPart bp in curInv.entity.body.bodyParts)
        {
            GameObject g = SimplePool.Spawn(equipmentButton, equipmentBase);
            g.GetComponentInChildren<Text>().text = bp.equippedItem.InvDisplay("none", true, false);
            g.transform.GetChild(1).GetComponent<Text>().text = bp.displayName;
            g.GetComponent<Button>().onClick.AddListener(() => OnSelect(g.transform.GetSiblingIndex()));
            g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(0, UIWindow.Inventory);
            g.GetComponent<ItemButton>().icon.sprite = SwitchSprite(bp.equippedItem);
            SelectedMax++;
        }
    }

    public static Sprite SwitchSprite(Item item)
    {
        string id = item.IsNullOrDefault() || item.renderer.onGround.NullOrEmpty() ? "item-empty.png" : item.renderer.onGround;

        return SpriteManager.GetObjectSprite(id);
    }

    public override void Update()
    {
        for (int i = 0; i < equipmentBase.childCount; i++)
        {
            equipmentBase.GetChild(i).GetComponent<ItemButton>().selected = (i == SelectedNum && World.userInterface.column == 0);
        }

        if (World.userInterface.column != 0 || World.userInterface.CurrentState != UIWindow.Inventory || 
            World.userInterface.SelectBodyPart || World.userInterface.SelectItemActions)
            return;

        if (SelectedMax > 0)
        {
            EventSystem.current.SetSelectedGameObject(equipmentBase.GetChild(SelectedNum).gameObject);
        }

        base.Update();
    }

    public override void ChangeSelectedNum(int newIndex, bool scroll)
    {
        if (!World.userInterface.SelectItemActions && !World.userInterface.SelectBodyPart && World.userInterface.column == 0)
        {
            base.ChangeSelectedNum(newIndex, scroll);

            if (SelectedMax > 0 && scroll)
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
        {
            if (!curInv.UnEquipWeapon(hands[index]))
            {
                return;
            }
        }
        else if (index == hands.Count)
        {
            curInv.UnEquipFirearm(true);
        }
        else
        {
            curInv.UnEquipArmor(curInv.entity.body.bodyParts[index - curInv.entity.body.Hands.Count - 1], true);
        }

        World.userInterface.InitializeAllWindows(curInv);
        World.userInterface.InvPanel.UpdateTooltip(SelectedNum);
    }
}

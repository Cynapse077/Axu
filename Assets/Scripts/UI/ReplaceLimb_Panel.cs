using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ReplaceLimb_Panel : MonoBehaviour
{
    public Text bpTitle;
    public GameObject bodyPartObject;
    public Transform bpAnchor;
    public Text itTitle;
    public Transform itAnchor;
    public Scrollbar bpScroll;
    public Scrollbar itScroll;

    Inventory inv;
    bool FromDoctor;
    int selectedNum, bpID = 0;
    List<BodyPart> severableBodyParts;
    List<Item> items;
    Mode mode = Mode.BodyPart;

    public void ReplaceLimb(Inventory inventory, bool fromDoctor)
    {
        bpAnchor.DestroyChildren();
        inv = inventory;
        FromDoctor = fromDoctor;
        bpID = -1;
        selectedNum = 0;
        mode = Mode.BodyPart;

        bpTitle.text = LocalizationManager.GetContent("Title_ReplaceLimb");
        itTitle.text = LocalizationManager.GetContent("Title_ReplaceLimb_Item");

        severableBodyParts = inv.entity.body.bodyParts.FindAll(x => x.severable && x.slot != ItemProperty.Slot_Head && !x.external);

        foreach (BodyPart b in severableBodyParts)
        {
            GameObject bp = Instantiate(bodyPartObject, bpAnchor);
            string myText = (b.isAttached ? "<color=green>" : "<color=red>") + b.displayName + "</color>";

            if (b.isAttached)
            {
                myText += " : ";

                for (int i = 0; i < b.Attributes.Count; i++)
                {
                    if (b.Attributes[i].Stat != "Hunger")
                        myText += LocalizationManager.GetContent(b.Attributes[i].Stat) + " <color=orange>(" + b.Attributes[i].Amount + ")</color> ";
                }
            }

            bp.GetComponentInChildren<Text>().text = myText;
            bp.GetComponent<ItemButton>().icon.enabled = false;
            bp.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
            bp.GetComponent<OnHover_SetSelectedIndex>().window = UIWindow.ReplacePartWithItem;
        }

        EventSystem.current.SetSelectedGameObject(bpAnchor.GetChild(selectedNum).gameObject);
        SetSelectedNum(0);
    }

    void UpdateItemPanel()
    {
        itAnchor.DestroyChildren();

        BodyPart part = severableBodyParts[selectedNum];

        if (part == null)
            return;

        items = inv.items.FindAll(x => x.HasProp(ItemProperty.Replacement_Limb) && x.GetSlot() == part.slot);

        foreach (Item i in items)
        {
            GameObject it = Instantiate(bodyPartObject, itAnchor);
            string myText = i.Name + " : ";

            for (int j = 0; j < i.statMods.Count; j++)
            {
                if (i.statMods[j].Stat != "Hunger" && i.statMods[j].Amount < 999)
                    myText += LocalizationManager.GetLocalizedContent(i.statMods[j].Stat)[0] + " (" + i.statMods[j].Amount + ") ";
            }

            it.GetComponent<ItemButton>().icon.sprite = SwitchSprite(i);
            it.GetComponentInChildren<Text>().text = myText;
            it.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
        }
    }

    Sprite SwitchSprite(Item item)
    {
        string id = (string.IsNullOrEmpty(item.renderer.onGround)) ? "item-empty.png" : item.renderer.onGround;
        return SpriteManager.GetObjectSprite(id);
    }

    public void SwitchMode(int newMode)
    {
        if (newMode == 0 && mode == Mode.Item)
        {
            bpID = 0;
            selectedNum = 0;
            mode = Mode.BodyPart;
            EventSystem.current.SetSelectedGameObject(bpAnchor.GetChild(selectedNum).gameObject);
            UpdateItemPanel();
        }
        else if (newMode == 1 && mode == Mode.BodyPart)
        {
            SelectPressed();
        }
    }

    public void SetSelectedNum(int num)
    {
        selectedNum = num;

        if (mode == Mode.BodyPart)
        {
            selectedNum = Mathf.Clamp(selectedNum, 0, severableBodyParts.Count - 1);

            EventSystem.current.SetSelectedGameObject(bpAnchor.GetChild(selectedNum).gameObject);
            UpdateItemPanel();
            bpScroll.value = 1f / (selectedNum / (float)severableBodyParts.Count);
        }
        else if (mode == Mode.Item)
        {
            selectedNum = Mathf.Clamp(selectedNum, 0, items.Count - 1);
            EventSystem.current.SetSelectedGameObject(itAnchor.GetChild(selectedNum).gameObject);
        }

        itScroll.value = 1f / (selectedNum / (float)items.Count);
    }

    public void SwitchSelectedNum(int num)
    {
        selectedNum += num;

        if (mode == Mode.BodyPart)
        {
            selectedNum = Mathf.Clamp(selectedNum, 0, severableBodyParts.Count - 1);

            EventSystem.current.SetSelectedGameObject(bpAnchor.GetChild(selectedNum).gameObject);
            UpdateItemPanel();
            bpScroll.value = 1f / (selectedNum / (float)severableBodyParts.Count);
        }
        else if (mode == Mode.Item)
        {
            selectedNum = Mathf.Clamp(selectedNum, 0, items.Count - 1);
            EventSystem.current.SetSelectedGameObject(itAnchor.GetChild(selectedNum).gameObject);
        }

        itScroll.value = 1f / (selectedNum / (float)items.Count);
    }

    public void SelectPressed()
    {
        if (mode == Mode.BodyPart)
        {
            bpID = selectedNum;

            selectedNum = 0;
            mode = Mode.Item;
            EventSystem.current.SetSelectedGameObject(itAnchor.GetChild(selectedNum).gameObject);
        }
        else if (mode == Mode.Item)
        {
            ReplaceBodyPart(bpID, selectedNum);
        }
    }

    void ReplaceBodyPart(int bpID, int itemID)
    {
        ObjectManager.playerEntity.stats.ReplaceOneLimbByDoctor(inv.entity.body.bodyParts.IndexOf(severableBodyParts[bpID]), 
            inv.items.IndexOf(items[itemID]), FromDoctor);
        World.userInterface.CloseWindows();
    }

    enum Mode
    {
        BodyPart, Item
    }
}

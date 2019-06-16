﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Augments;

public class CyberneticsPanel : UIPanel
{
    public Text bpTitle;
    public Text cybTitle;

    public Transform bpAnchor;
    public Transform cybAnchor;
    public GameObject cybButton;

    public Scrollbar bpScroll;
    public Scrollbar cybScroll;

    int bpIndex = 0;
    Mode mode;
    List<Cybernetic> availableCybernetics;
    List<BodyPart> augmentableBodyParts;
    const string purchaseItemID = "bottlecap";

    public override void Initialize()
    {
        mode = Mode.BodyPart;
        bpIndex = 0;
        mode = Mode.BodyPart;
        bpTitle.text = LocalizationManager.GetContent("Title_CybLimb");
        cybTitle.text = LocalizationManager.GetContent("Title_CybLimb_Cyb");

        base.Initialize();
    }

    public void SetupLists(Body bod)
    {
        Initialize();

        bpAnchor.DestroyChildren();
        augmentableBodyParts = bod.bodyParts.FindAll(x => x.organic && x.isAttached);

        foreach (BodyPart b in augmentableBodyParts)
        {
            GameObject g = Instantiate(cybButton, bpAnchor);
            string buttonText = b.displayName;

            if (b.cybernetic != null)
            {
                buttonText += " <color=silver>(" + b.cybernetic.Name + ")</color>"; 
            }

            g.GetComponent<Button>().onClick.AddListener(() => OnSelect(g.transform.GetSiblingIndex()));
            g.GetComponentInChildren<Text>().text = buttonText;
            g.GetComponent<ItemButton>().icon.enabled = false;
            g.GetComponent<OnHover_SetSelectedIndex>().window = UIWindow.Cybernetics;
        }

        SelectedMax = augmentableBodyParts.Count;

        ChangeSelectedNum(0);
        SetupCybernetics();
    }

    void SetupCybernetics()
    {
        cybAnchor.DestroyChildren();
        availableCybernetics = Cybernetic.GetCyberneticsForLimb(augmentableBodyParts[bpIndex]);

        foreach (Cybernetic c in availableCybernetics)
        {
            GameObject g = Instantiate(cybButton, cybAnchor);
            string buttonText = c.Name;

            g.GetComponent<Button>().onClick.AddListener(() => OnSelect(g.transform.GetSiblingIndex()));
            g.GetComponentInChildren<Text>().text = buttonText;
            g.GetComponent<ItemButton>().icon.enabled = false;
            g.GetComponent<OnHover_SetSelectedIndex>().window = UIWindow.Cybernetics;
        }
    }

    public override void ChangeSelectedNum(int newIndex)
    {
        base.ChangeSelectedNum(newIndex);
        
        switch (mode)
        {
            case Mode.BodyPart:
                bpIndex = newIndex;
                SelectedNum = Mathf.Clamp(SelectedNum, 0, augmentableBodyParts.Count - 1);
                SetupCybernetics();
                EventSystem.current.SetSelectedGameObject(bpAnchor.GetChild(SelectedNum).gameObject);
                bpScroll.value = 1f / (SelectedNum / (float)augmentableBodyParts.Count);
                break;

            case Mode.Cybernetic:
                SelectedNum = Mathf.Clamp(SelectedNum, 0, availableCybernetics.Count - 1);
                EventSystem.current.SetSelectedGameObject(cybAnchor.GetChild(SelectedNum).gameObject);
                cybScroll.value = 1f / (SelectedNum / (float)availableCybernetics.Count);
                break;
        }
    }

    protected override void OnSelect(int index)
    {
        switch (mode)
        {
            case Mode.BodyPart:
                mode = Mode.Cybernetic;
                SelectedMax = availableCybernetics.Count;
                ChangeSelectedNum(0);
                break;

            case Mode.Cybernetic:
                if (ObjectManager.playerEntity.inventory.HasItem(purchaseItemID))
                {
                    BodyPart curBP = augmentableBodyParts[bpIndex];

                    if (curBP.cybernetic != null)
                    {
                        CombatLog.NewMessage("The " + curBP.cybernetic.Name + " on your " + curBP.displayName + " has been removed.");
                        curBP.cybernetic.Remove();
                    }

                    Cybernetic c = availableCybernetics[index].Clone();
                    c.Attach(curBP);
                    CombatLog.NewMessage("You have augmented your " + curBP.displayName + " with " + c.Name + ".");
                    ObjectManager.playerEntity.inventory.RemoveInstance(ObjectManager.playerEntity.inventory.items.Find(x => x.ID == purchaseItemID));
                    World.userInterface.CloseWindows();
                }
                else
                {
                    Alert.CustomAlert_WithTitle("No Bottle Caps!", "Purchasing Cybernetic enhancements requires a Bottle Cap.");
                }
                break;
        }
    }

    enum Mode
    {
        BodyPart, Cybernetic
    }
}

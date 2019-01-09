using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BodyPartTargetPanel : MonoBehaviour
{
    public Text title;
    public Transform bpAnchor;
    public GameObject bpButton;

    List<BodyPart> bodyParts;
    public SelectionType selType;
    Body targetBody;

    public void TargetPart(Body bod, SelectionType stype)
    {
        selType = stype;
        targetBody = bod;
        bpAnchor.DestroyChildren();

        title.text = LocalizationManager.GetContent(((Amputate) ? "Title_AmpLimb" : "Title_TargetPart"));
        bodyParts = (Amputate) ? targetBody.SeverableBodyParts() : targetBody.bodyParts;

        foreach (BodyPart b in bodyParts)
        {
            GameObject g = Instantiate(bpButton, bpAnchor);
            Text buttonText = g.GetComponentInChildren<Text>();

            if (!b.isAttached || Amputate && !b.severable)
                buttonText.text = "<color=grey>" + b.displayName + "</color>";
            else
            {
                if (selType == SelectionType.CalledShot)
                {
                    float hitChance = 100.0f - ObjectManager.playerEntity.fighter.MissChance(ObjectManager.playerEntity.body.MainHand, bod.entity.stats, b);
                    string perc = UserInterface.ColorByPercent(hitChance.ToString() + "%", (int)hitChance);
                    int armor = b.armor + b.equippedItem.armor;

                    buttonText.text = string.Format("{0} <color=silver>[{1}]</color> - ({2})", b.displayName, armor.ToString(), perc);
                }
                else
                {
                    buttonText.text = b.displayName;
                }
            }

            g.GetComponent<ItemButton>().icon.enabled = false;
            g.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
            g.GetComponent<OnHover_SetSelectedIndex>().window = (Amputate) ? UIWindow.AmputateLimb : UIWindow.TargetBodyPart;
        }
    }

    bool Amputate
    {
        get { return selType == SelectionType.Amputate; }
    }

    public void SelectPressed()
    {
        BodyPart bp = bodyParts[UserInterface.selectedItemNum];

        switch (selType)
        {
            case SelectionType.Amputate:
                if (bp.slot == ItemProperty.Slot_Head && bp.myBody.bodyParts.FindAll(x => x.slot == ItemProperty.Slot_Head).Count <= 1)
                {
                    World.userInterface.CloseWindows();
                    Alert.NewAlert("Cannot_Amputate", UIWindow.AmputateLimb);
                }
                else
                {
                    World.userInterface.CloseWindows();
                    World.userInterface.YesNoAction("YN_Amputate", () => { targetBody.RemoveLimb(bp); World.userInterface.CloseWindows(); }, null, bp.name);
                }
                break;

            case SelectionType.CalledShot:
                ObjectManager.playerEntity.fighter.Attack(targetBody.entity.stats, false, bp, 1);
                World.userInterface.CloseWindows();
                break;

            case SelectionType.Grab:
                if (!bp.isAttached)
                    return;

                ObjectManager.playerEntity.skills.Grapple_GrabPart(bp);
                World.userInterface.CloseWindows();
                break;

            default:
                break;
        }
    }

    void Update()
    {
        if (UserInterface.selectedItemNum >= 0 && UserInterface.selectedItemNum < bodyParts.Count)
            EventSystem.current.SetSelectedGameObject(bpAnchor.GetChild(UserInterface.selectedItemNum).gameObject);
    }

    public enum SelectionType
    {
        Amputate, CalledShot, Grab
    }
}

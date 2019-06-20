using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BodyDisplay_Button : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    BodyDisplayPanel display;
    Text bpTitle;
    Text bpDescription;
    Image img;
    BodyPart bodyPart;

    public void SetTargets(Text title, Text desc, BodyDisplayPanel bdp)
    {
        bodyPart = null;
        bpTitle = title;
        bpDescription = desc;
        display = bdp;
    }

    public void SetBodyPart(BodyPart bp)
    {
        if (bp == null)
        {
            return;
        }

        img = GetComponent<Image>();
        bodyPart = bp;

        img.color = (bp.isAttached) ? Color.white : Color.red;

        if (bp.isAttached && bp.Crippled)
        {
            img.color = Color.yellow;
        }
    }

    public void OnPointerEnter(PointerEventData ped)
    {
        if (bodyPart != null)
        {
            bpTitle.text = bodyPart.displayName;

            string health = (bodyPart.isAttached ? "<color=green>Healthy</color>" : "<color=red>Severed</color>");

            if (bodyPart.isAttached && bodyPart.Crippled)
            {
                health = "<color=yellow>Injured</color>";
            }

            if (bodyPart.equippedItem == null)
            {
                bodyPart.equippedItem = ItemList.GetNone();
            }

            string wielded = (bodyPart.hand != null && bodyPart.hand.EquippedItem != null ? "\nWielded: " + bodyPart.hand.EquippedItem.InvDisplay("", false, true) : "");

            string desc = "Status: " + health + 
                "\nEquipped: " + bodyPart.equippedItem.DisplayName() + 
                wielded +
                "\nArmor: <color=grey>[" + (bodyPart.armor + bodyPart.equippedItem.armor + bodyPart.myBody.entity.stats.Defense).ToString() + "]</color>" +
                "\n\nStats:\n";

            for (int i = 0; i < bodyPart.Attributes.Count; i++)
            {
                desc += "  - " + LocalizationManager.GetContent(bodyPart.Attributes[i].Stat) + " <color=orange>(" + bodyPart.Attributes[i].Amount + ")</color>\n";
            }

            desc += "\nCybernetic:\n";

            if (bodyPart.cybernetic != null)
            {
                desc += "  <color=silver>" + bodyPart.cybernetic.Name + "</color> - <i>" + bodyPart.cybernetic.Desc + "</i>\n";
            }
            else
            {
                desc += "  <color=grey>[N/A]</color>\n";
            }

            desc += "\nWounds:\n";

            if (bodyPart.wounds.Count == 0)
            {
                desc += "  <color=grey>[NONE]</color>\n";
            }
            else
            {
                for (int i = 0; i < bodyPart.wounds.Count; i++)
                {
                    desc += "  - <color=red>" + bodyPart.wounds[i].Name + "</color>\n";

                    for (int j = 0; j < bodyPart.wounds[i].statMods.Count; j++)
                    {
                        desc += "    - " + LocalizationManager.GetContent(bodyPart.wounds[i].statMods[j].Stat) + " " + bodyPart.wounds[i].statMods[j].Amount + "\n";
                    }
                }
            }

            bpDescription.text = desc;

            if (display != null)
            {
                display.ActivateExtraInfoWindow(false);
            }
        }
    }

    public void OnPointerExit(PointerEventData ped)
    {
        if (bodyPart != null)
        {
            if (bpTitle != null)
            {
                bpTitle.text = "";
            }

            if (bpDescription != null)
            {
                bpDescription.text = "";
            }

            if (display != null)
            {
                display.ActivateExtraInfoWindow(true);
            }
        }        
    }
}
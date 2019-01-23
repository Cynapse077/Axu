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
        bpTitle = title;
        bpDescription = desc;
        display = bdp;
    }

    public void SetBodyPart(BodyPart bp)
    {
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

            string desc = "Status: " + health + 
                "\nEquipped: " + bodyPart.equippedItem.DisplayName() +
                "\nArmor: <color=grey>[" + (bodyPart.armor + bodyPart.equippedItem.armor + bodyPart.myBody.entity.stats.Defense).ToString() + "]</color>" +
                "\n\nStats:\n";

            for (int i = 0; i < bodyPart.Attributes.Count; i++)
            {
                desc += "  - " + LocalizationManager.GetContent(bodyPart.Attributes[i].Stat) + " <color=orange>(" + bodyPart.Attributes[i].Amount + ")</color>\n";
            }

            desc += "\nWounds:\n";

            if (bodyPart.wounds.Count == 0)
            {
                desc += "  <color=grey>[NONE]</color>";
            }
            else
            {
                for (int i = 0; i < bodyPart.wounds.Count; i++)
                {
                    desc += "  - <color=red>" + bodyPart.wounds[i].Name + "</color>\n";

                    for (int j = 0; j < bodyPart.wounds[i].statMods.Count; j++)
                    {
                        desc += "    - <color=red>(" + LocalizationManager.GetContent(bodyPart.wounds[i].statMods[j].Stat) + " " + bodyPart.wounds[i].statMods[j].Amount + ")</color>\n";
                    }
                }
            }

            bpDescription.text = desc;
        }

        if (display != null)
        {
            display.ActivateExtraInfoWindow(false);
        }
    }

    public void OnPointerExit(PointerEventData ped)
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
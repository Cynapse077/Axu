using System.Text;
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

            string health = bodyPart.isAttached ? "Char_Healthy" : "Char_Severed";

            if (bodyPart.isAttached && bodyPart.Crippled)
            {
                health = "Char_Injured";
            }

            if (bodyPart.equippedItem == null)
            {
                bodyPart.equippedItem = ItemList.NoneItem;
            }

            string wielded = (bodyPart.hand != null && bodyPart.hand.EquippedItem != null 
                ? "\n" + LocalizationManager.GetContent("Char_Wielded") + bodyPart.hand.EquippedItem.InvDisplay("", false, true) 
                : string.Empty);
            string equipped = bodyPart.equippedItem.IsNullOrDefault() 
                ? string.Empty 
                : "\n" + LocalizationManager.GetContent("Char_Equipped") + bodyPart.equippedItem.DisplayName();

            StringBuilder sb = new StringBuilder();
            sb.Append(LocalizationManager.GetContent("Char_Status") + LocalizationManager.GetContent(health));
            sb.Append(equipped + wielded);
            sb.Append("\n" + LocalizationManager.GetContent("Char_Armor") + "<color=grey>[" 
                + (bodyPart.armor + bodyPart.equippedItem.armor + bodyPart.myBody.entity.stats.Defense).ToString() 
                + "]</color>\n");

            if (bodyPart.Attributes.Count > 0)
            {
                sb.Append("\n" + LocalizationManager.GetContent("Char_Stats") + "\n");
                for (int i = 0; i < bodyPart.Attributes.Count; i++)
                {
                    sb.Append("  - " + LocalizationManager.GetContent(bodyPart.Attributes[i].Stat));
                    sb.Append(" <color=orange>(" + bodyPart.Attributes[i].Amount + ")</color>\n");
                }
            }

            sb.Append("\n" + LocalizationManager.GetContent("Char_Cybernetic") + "\n");

            if (bodyPart.cybernetic != null)
            {
                sb.Append("  <color=silver>" + bodyPart.cybernetic.Name + "</color> - <i>");
                sb.Append(bodyPart.cybernetic.Desc + "</i>\n");
            }
            else
            {
                sb.Append(" " + LocalizationManager.GetContent("NoneBrackets") + "\n");
            }

            sb.Append("\n" + LocalizationManager.GetContent("Char_Wounds") + "\n");

            if (bodyPart.wounds.Count == 0)
            {
                sb.Append(" " + LocalizationManager.GetContent("NoneBrackets") + "\n");
            }
            else
            {
                for (int i = 0; i < bodyPart.wounds.Count; i++)
                {
                    sb.Append("  - <color=red>" + bodyPart.wounds[i].Name + "</color>\n");

                    for (int j = 0; j < bodyPart.wounds[i].statMods.Count; j++)
                    {
                        sb.Append("    - " + LocalizationManager.GetContent(bodyPart.wounds[i].statMods[j].Stat) + " " + bodyPart.wounds[i].statMods[j].Amount + "\n");
                    }
                }
            }

            bpDescription.text = sb.ToString();

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
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ModPrefab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Mod myMod;
    public Text title;

    Text descriptionText;
    Image image;
    Color normColor;

    public void Setup(Mod m, Text descText)
    {
        myMod = m;
        title.text = myMod.name;
        descriptionText = descText;
        image = GetComponent<Image>();
        normColor = image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myMod != null && descriptionText != null)
        {
            descriptionText.text = GetDescriptionText();
        }

        image.color = Color.white;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = normColor;
    }

    string GetDescriptionText()
    {
        return myMod.name + "\n\nBy: " + myMod.creator + "\n\nLoad Order: " + myMod.loadOrder.ToString() + "\n\n\"" + myMod.description + "\"";
    }
}

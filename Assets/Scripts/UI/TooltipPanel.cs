using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TooltipPanel : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject TName;
    public GameObject TElement;
    public GameObject TDescription;
    public RectTransform anchor1;
    public RectTransform anchor2;

    Image image;

    public void UpdateTooltip(Item item, bool display, bool shop = false)
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        transform.DespawnChildren();

        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        GetComponent<RectTransform>().localPosition = (shop) ? anchor2.localPosition : anchor1.localPosition;

        Color c = image.color;
        c.a = 1.0f;

        if (!display || item == null || item.Name == ItemList.GetNone().Name)
        {
            c.a = 0.0f;
            image.color = c;
            return;
        }

        image.color = c;

        //Name
        GameObject n = SimplePool.Spawn(TName, transform);
        n.GetComponentInChildren<Text>().text = ((item.displayName != null && item.displayName != "") ? item.DisplayName() : item.InvDisplay(""));

        //Elements
        List<string> elements = ItemTooltip.GetDisplayItems(item);
        for (int i = 0; i < elements.Count; i++)
        {
            GameObject e = SimplePool.Spawn(TElement, transform);
            e.GetComponentInChildren<Text>().text = elements[i];
            e.GetComponentInChildren<Text>().alignment = (i == 0) ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
        }

        //Description
        GameObject d = SimplePool.Spawn(TDescription, transform);
        d.GetComponentInChildren<Text>().text = "<i>\"" + item.flavorText + " " + item.modifier.description + "\"</i>";
    }
}

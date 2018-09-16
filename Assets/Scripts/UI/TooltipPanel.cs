using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class TooltipPanel : MonoBehaviour {

	[Header("Prefabs")]
	public GameObject TName;
	public GameObject TElement;
	public GameObject TDescription;
	public RectTransform anchor1;
	public RectTransform anchor2;

	Image image;

	public void UpdateTooltip(Item item, bool display, bool shop = false) {
		if (image == null)
			image = GetComponent<Image>();
		
		transform.DestroyChildren();

		if (item == null)
			gameObject.SetActive(false);

		if (!gameObject.activeSelf)
			return;

		GetComponent<RectTransform>().localPosition = (shop) ? anchor2.localPosition : anchor1.localPosition;

		Color c = image.color;
		c.a = 1.0f;

		if (!display || item == null || item.Name == ItemList.GetNone().Name) {
			c.a = 0.0f;
			image.color = c;
			return;
		}

		image.color = c;

		//Name
		GameObject n = (GameObject)Instantiate(TName, transform);
		n.GetComponentInChildren<Text>().text = ((item.displayName != null && item.displayName != "") ? item.DisplayName() : item.InvDisplay(""));

		//Elements
		List<string> elements = ItemTooltip.GetDisplayItems(item);
		for (int i = 0; i < elements.Count; i++) {
			GameObject e = (GameObject)Instantiate(TElement, transform);
			e.GetComponentInChildren<Text>().text = elements[i];

			if (i == 0)
				e.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
		}

		//Description
		GameObject d = (GameObject)Instantiate(TDescription, transform);
		d.GetComponentInChildren<Text>().text = "<i>\"" + item.flavorText + " " + item.modifier.description + "\"</i>";
	}
}

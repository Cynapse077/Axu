using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ModPrefab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Mod myMod;
    public Text title;

    Text descriptionText;
    ModsPanel modsPanel;
    Image image;
    Color normColor;
    bool pointerOver;

    void Update()
    {
        if (!myMod.IsCore() && pointerOver && Input.GetMouseButtonDown(0))
        {
            modsPanel.SetActive(ModManager.Mods.IndexOf(myMod), !myMod.IsActive);
        }
    }

    public void Setup(Mod m, Text descText, ModsPanel mPanel)
    {
        modsPanel = mPanel;
        myMod = m;
        title.text = myMod.IsActive ? myMod.name : string.Format("<color=grey>{0}</color>", myMod.name);
        descriptionText = descText;
        image = GetComponent<Image>();
        normColor = image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myMod != null && descriptionText != null)
        {
            descriptionText.text = GetDescriptionText(myMod.IsActive ? string.Empty : "<color=red>(Inactive)</color>");
        }

        pointerOver = true;
        image.color = Color.white;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
        image.color = normColor;
    }

    string GetDescriptionText(string extra)
    {
        return string.Format("{0} {4}\n\nBy: {1}\n\nLoad Order: {2}\n\n{3}", myMod.name, myMod.creator, myMod.loadOrder, myMod.description, extra);
    }
}

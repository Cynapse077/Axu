using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenu_LoadButton : MonoBehaviour, IPointerEnterHandler
{
    public Button deleteButton;

    LoadSaveMenu lsm;

    public void Init(LoadSaveMenu _mct)
    {
        lsm = _mct;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (lsm != null)
        {
            lsm.HoverFile(transform.GetSiblingIndex());
        }
    }
}


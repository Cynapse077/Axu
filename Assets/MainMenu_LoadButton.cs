using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenu_LoadButton : MonoBehaviour, IPointerEnterHandler
{
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


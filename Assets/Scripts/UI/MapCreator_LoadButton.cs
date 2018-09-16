using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class MapCreator_LoadButton : MonoBehaviour, IPointerEnterHandler
{
    MapCreatorTool mct;
    string info;

    public void Init(MapCreatorTool _mct, string myInfo)
    {
        mct = _mct;
        info = myInfo;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mct != null)
            mct.mapInfoText.text = info;
    }
}

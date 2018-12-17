using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class MapCreator_LoadButton : MonoBehaviour, IPointerEnterHandler
{
    public Button deleteButton;
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
        {
            mct.mapInfoText.text = info;
        }
    }
}

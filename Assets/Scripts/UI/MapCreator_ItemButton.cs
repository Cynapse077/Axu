using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreator
{
    public class MapCreator_ItemButton : MonoBehaviour, IPointerEnterHandler
    {
        public Image icon;
        MapCreatorTool mct;
        string info;

        public void Init(MapCreatorTool _mct, Item i)
        {
            mct = _mct;
            icon.sprite = EquipmentPanel.SwitchSprite(i);
            info = string.Format("Name: {0}\n\nID: {1}\n\nMod ID: {2}\n\nRarity: {3}\n\n    \"{4}\"", i.Name, i.ID, i.ModID, i.rarity, i.flavorText);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (mct != null)
            {
                mct.itemInfoText.text = info;
            }
        }
    }
}
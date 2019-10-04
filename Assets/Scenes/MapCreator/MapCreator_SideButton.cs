using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MapCreator
{
    public class MapCreator_SideButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        int tileID;
        MapCreatorTool mct;
        MC_ButtonType buttonType;

        public void Init(MapCreatorTool m, int id, MC_ButtonType bType)
        {
            tileID = id;
            mct = m;
            buttonType = bType;

            GetSprite();
            GetComponent<Button>().onClick.AddListener(OnPress);
        }

        void GetSprite()
        {
            switch (buttonType)
            {
                case MC_ButtonType.Tile:
                    GetComponent<Image>().sprite = mct.Sprites[tileID];
                    break;
                case MC_ButtonType.Object:
                    Image img = GetComponent<Image>();
                    img.sprite = mct.objectSprites[tileID];
                    float y = (img.sprite.texture.height > img.sprite.texture.width) ? 2f : 1f;
                    transform.localScale = new Vector3(1f, y, 1f);
                    break;
                case MC_ButtonType.NPC:
                    GetComponent<Image>().sprite = mct.npcSprites[tileID].sprite;
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            switch (buttonType)
            {
                case MC_ButtonType.Tile:
                    mct.EnableTooltip(TileManager.GetKey(tileID));
                    break;
                case MC_ButtonType.Object:
                    mct.EnableTooltip(mct.ViewableObjects[tileID].Name);
                    break;
                case MC_ButtonType.NPC:
                    mct.EnableTooltip(EntityList.npcs[tileID].name);
                    break;
                default:
                    break;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            mct.DisableTooltip();
        }

        void OnPress()
        {
            if (mct.selectType != MapCreatorTool.MC_Selection_Type.Save)
                mct.SetCurrentTileID(tileID);

        }

        public enum MC_ButtonType
        {
            Tile, Object, NPC
        }
    }
}
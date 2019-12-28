using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MapCreator
{
    public class MapCreator_Cell : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
    {
        public int tileID;
        public int objectID = -1;
        public int npcID;
        public Coord pos;
        public Image mapObject;
        public Image npc;
        public Image image;

        MapCreatorTool mct;

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
            if (mct == null)
                mct = GameObject.FindObjectOfType<MapCreatorTool>();
        }

        public void SetTile(Sprite s, int id)
        {
            if (image == null)
                image = GetComponent<Image>();

            image.sprite = s;
            tileID = id;
        }

        Color ColorToDisplay
        {
            get
            {
                return (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl)) ? Color.yellow : Color.magenta;
            }
        }

        public void OnPress(bool overrrideMenu = false, bool skipAuto = false)
        {
            Initialize();

            if (mct.selectType == MapCreatorTool.MC_Selection_Type.Save)
            {
                if (overrrideMenu)
                    SetTile(mct.Sprites[32], 32);
                else
                    return;
            }
            else if (mct.selectType == MapCreatorTool.MC_Selection_Type.Paint)
            {
                if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl))
                    mct.SetCurrentTileID(tileID);
                else if (Input.GetKey(KeyCode.LeftShift))
                    mct.Box(pos);
                else
                    mct.TileChanged(pos.x, pos.y, skipAuto);

            }
            else if (mct.selectType == MapCreatorTool.MC_Selection_Type.Fill)
            {
                if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl))
                    mct.SetCurrentTileID(tileID);
                else
                    mct.FloodFill(this, tileID);
            }
            else if (mct.selectType == MapCreatorTool.MC_Selection_Type.Place_Object)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    mct.Box(pos);

                else if (objectID != mct.CurrentTile)
                {
                    mct.ObjectChanged(pos.x, pos.y);
                    SetObjectSprite(mct.CurrentTile);
                    mct.AutotileObjects(pos.x, pos.y, true);
                }
            }
            else if (mct.selectType == MapCreatorTool.MC_Selection_Type.Place_NPC)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    mct.Box(pos);
                else if (npcID != mct.CurrentTile)
                {
                    mct.NPCChanged(pos.x, pos.y);
                    npc.sprite = mct.npcSprites[mct.CurrentTile].sprite;
                    npcID = mct.npcSprites[mct.CurrentTile].id;
                }
            }
        }

        public void SetObjectSprite(int id)
        {
            if (id < 0)
            {
                EmptySprite(MapCreatorTool.MC_Selection_Type.Place_Object);

            }
            else
            {
                mapObject.sprite = mct.objectSprites[id];
                objectID = id;
                float y = (mapObject.sprite.texture.height > mapObject.sprite.texture.width) ? 1f : 0.5f;
                mapObject.rectTransform.localScale = new Vector3(0.5f, y, 1f);
            }
        }

        public void SetObject(string objectID)
        {
            MapObject_Blueprint mob = ItemList.GetMOB(objectID);

            for (int i = 0; i < mct.ViewableObjects.Count; i++)
            {
                if (mct.ViewableObjects[i].objectType == objectID)
                {
                    this.objectID = i;
                    break;
                }
            }

            mapObject.sprite = SpriteManager.GetObjectSprite(mob.spriteID);
            float y = (mapObject.sprite.texture.height > mapObject.sprite.texture.width) ? 1f : 0.5f;
            mapObject.rectTransform.localScale = new Vector3(0.5f, y, 1f);

            if (mapObject.sprite.texture.width > 16)
                mapObject.sprite = Sprite.Create(mapObject.sprite.texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }

        public void SetNPCSprite(int id)
        {
            if (id < 0)
                EmptySprite(MapCreatorTool.MC_Selection_Type.Place_NPC);
            else
            {
                npc.sprite = mct.npcSprites[id].sprite;
                npcID = mct.npcSprites[mct.CurrentTile].id;
            }
        }

        public void SetNPC(string id)
        {
            npc.sprite = mct.npcSprites.Find(x => x.npcID == id).sprite;
            npcID = mct.npcSprites.Find(x => x.npcID == id).id;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            mct.MouseOver(pos);

            if (mct.selectType != MapCreatorTool.MC_Selection_Type.Save)
            {
                image.color = ColorToDisplay;

                if (Input.GetKey(KeyCode.LeftControl))
                {
                    mct.EnableTooltip(GetTootip());
                }

                if (Input.GetMouseButton(0))
                    OnPress();
                if (Input.GetMouseButton(1))
                    EmptySprite(mct.selectType);
            }
        }

        string GetTootip()
        {
            string tt = TileManager.GetKey(tileID);
            string ot = (objectID > -1) ? mct.ViewableObjects[objectID].Name : "No Object";
            string nt = (npcID > -1) ? EntityList.npcs[npcID].name : "No NPC";

            return string.Format("{0}\n{1}\n{2}", tt, ot, nt);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            image.color = Color.white;
            mct.DisableTooltip();
        }

        public void EmptySprite(MapCreatorTool.MC_Selection_Type type)
        {
            if (type == MapCreatorTool.MC_Selection_Type.Paint)
            {
                npc.sprite = mct.empty;
                npcID = -1;
                mapObject.sprite = mct.empty;
                objectID = -1;
            }
            else if (type == MapCreatorTool.MC_Selection_Type.Place_NPC)
            {
                npc.sprite = mct.empty;
                npcID = -1;
            }
            else if (type == MapCreatorTool.MC_Selection_Type.Place_Object)
            {
                mapObject.sprite = mct.empty;
                objectID = -1;
                mct.AutotileObjects(pos.x, pos.y, true);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (mct.selectType != MapCreatorTool.MC_Selection_Type.Save)
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                    OnPress();
                else if (eventData.button == PointerEventData.InputButton.Right)
                    EmptySprite(mct.selectType);
            }
        }
    }
}

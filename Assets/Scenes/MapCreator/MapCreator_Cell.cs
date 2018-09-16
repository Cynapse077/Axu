using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapCreator_Cell : MonoBehaviour,  IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler {

	MapCreatorTool mct;
	Image image;
	public int tileID;
	public int objectID = -1;
	public Coord pos;
	public Image mapObject;

	void Start() {
		Initialize();
	}

	void Initialize() {
		if (image == null)
			image = GetComponent<Image>();
		if (mct == null)
			mct = GameObject.FindObjectOfType<MapCreatorTool>();
	}

	public void SetTile(Sprite s, int id) {
		if (image == null)
			image = GetComponent<Image>();
		
		image.sprite = s;
		tileID = id;
	}

	Color ColorToDisplay {
		get {
			return (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl)) ? Color.yellow : Color.magenta;
		}
	}

	public void OnPress(bool overrrideMenu = false, bool skipAuto = false) {
		Initialize();

		if (mct.selectType == MapCreatorTool.MC_Selection_Type.Save) {
			if (overrrideMenu)
				SetTile(mct.Sprites[32], 32);
			else
				return;
		}
		
		if (mct.selectType == MapCreatorTool.MC_Selection_Type.Paint) {
			if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl))
				mct.SetCurrentTileID(tileID);
			else if (Input.GetKey(KeyCode.LeftShift))
				mct.Box(pos);
			else
				mct.Changed(pos.x, pos.y, skipAuto);
			
		} else if (mct.selectType == MapCreatorTool.MC_Selection_Type.Fill) {
			if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.LeftControl))
				mct.SetCurrentTileID(tileID);
			else
				mct.FloodFill(this, tileID);
			
		} else if (mct.selectType == MapCreatorTool.MC_Selection_Type.Place_Object) {
			mapObject.sprite = mct.objectSprites[mct.CurrentTile];
			objectID = mct.CurrentTile;
			float y = (mapObject.sprite.texture.height > mapObject.sprite.texture.width) ? 1f : 0.5f;
			mapObject.rectTransform.localScale = new Vector3(0.5f, y, 1f);

		}
	}

	public void SetObject(string name) {
		MapObjectBlueprint mob = ItemList.GetMOB(name);

		for (int i = 0; i < ItemList.mapObjectBlueprints.Count; i++) {
			if (ItemList.mapObjectBlueprints[i].objectType == name) {
				objectID = i;
				break;
			}
		}

		mapObject.sprite = SpriteManager.GetObjectSprite(mob.spriteID);
		float y = (mapObject.sprite.texture.height > mapObject.sprite.texture.width) ? 1f : 0.5f;
		mapObject.rectTransform.localScale = new Vector3(0.5f, y, 1f);

        if (mapObject.sprite.texture.width > 16)
            mapObject.sprite = Sprite.Create(mapObject.sprite.texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }

	public void OnPointerEnter(PointerEventData eventData) {
		mct.MouseOver(pos);

		if (mct.selectType != MapCreatorTool.MC_Selection_Type.Save) {
			image.color = ColorToDisplay;

            if (Input.GetKey(KeyCode.LeftControl)) {
                string tooltip = Tile.GetKey(tileID);
                mct.EnableTooltip(tooltip);
            }

			if (Input.GetMouseButton(0))
				OnPress();
			if (Input.GetMouseButton(1))
				EmptySprite();
		}
	}

	public void OnPointerExit(PointerEventData eventData) {
		image.color = Color.white;
		mct.DisableTooltip();
	}

	public void EmptySprite() {
		mapObject.sprite = mct.empty;
		objectID = -1;
	}

	public void OnPointerDown(PointerEventData eventData) {
		if (mct.selectType != MapCreatorTool.MC_Selection_Type.Save) {
			if (eventData.button == PointerEventData.InputButton.Left)
				OnPress();
			else if (eventData.button == PointerEventData.InputButton.Right)
				EmptySprite();
		}
	}
}

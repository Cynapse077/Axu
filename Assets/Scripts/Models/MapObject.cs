using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
 public class MapObject {
    public int elevation;
    public bool onScreen, seen, solid;
    public float rotation;
    public string description;
    public List<Item> inv;

    string _objectType;
    Coord _localPosition, _worldPosition;


    public MapObject() {
        _objectType = "Empty";
        _localPosition = new Coord(0, 0);
        _worldPosition = new Coord(0, 0);
        description = "";
    }

    public MapObject(string objType, Coord localPos, Coord worldPos, int elev, string desc) {
        _objectType = objType;
        _localPosition = localPos;
        _worldPosition = worldPos;
        elevation = elev;
        description = desc;

		Init();
    }

	void Init() {
        if (_objectType.Contains("Bloodstain_"))
            SetRotation();
		
		else if (_objectType == "Chest")
			inv = Inventory.GetDrops(SeedManager.combatRandom.Next(2, 6));
		else if (_objectType == "Barrel") {
			inv = new List<Item>();

			if (SeedManager.combatRandom.Next(100) < 8) {
				List<Item> possItems = ItemList.items.FindAll(x => x.HasProp(ItemProperty.Edible) && x.rarity < 3);

				for (int i = 0; i < SeedManager.combatRandom.Next(4); i++) {
					inv.Add(ItemList.GetItemByID(possItems.GetRandom(SeedManager.combatRandom).ID));
				}
			} else if (SeedManager.combatRandom.Next(100) < 10) {
				Item flask = ItemList.GetItemByID("flask");
				CLiquidContainer cl = flask.GetItemComponent<CLiquidContainer>();

				if (cl == null)
					return;

				cl.liquid = ItemList.GetRandomLiquid(cl.capacity);
			}
		}
		else if (_objectType == "Loot" || _objectType == "Body")
			inv = new List<Item>();
	}

	void SetRotation() {
		if (SeedManager.combatRandom.Next(100) < 10 || objectType == "Bloodstain_Permanent") {
			rotation = Random.Range(0, 360);
			return;
		}

		List<Coord> possPos = new List<Coord>();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if (localPosition.x + x <= 0 || localPosition.x + x >= Manager.localMapSize.x - 1 || localPosition.y + y <= 0 || localPosition.y + y >= Manager.localMapSize.y - 1)
					continue;
				if (Mathf.Abs(x) + Mathf.Abs(y) > 1 || x == 0 && y == 0)
					continue;

				if (!World.tileMap.WalkableTile(localPosition.x + x, localPosition.y + y))
					possPos.Add(new Coord(x, y));
			}
		}

		if (possPos.Count > 0) {
			Coord offset = possPos.GetRandom(SeedManager.combatRandom);
			objectType = "Bloodstain_Wall";
			localPosition.x += offset.x;
			localPosition.y += offset.y;
			RotationFromOrientation(offset);
		} else {
			rotation = Random.Range(0, 360);
			return;
		}
	}

	void RotationFromOrientation(Coord offset) {
		if (offset.y > 0)
			rotation = 0;
		if (offset.y < 0)
			rotation = 180;
		if (offset.x < 0)
			rotation = 90;
		if (offset.x > 0)
			rotation = -90; 
	}

	public bool CanSpawnThisObject(Coord pos, int elev) {
		return (pos == worldPosition && elev == elevation);
	}

    public string objectType {
        get { return _objectType; }
        set { _objectType = value; }
    }

    public Coord localPosition {
        get { return _localPosition; }
        set { _localPosition = value; }
    }

    public Coord worldPosition {
        get { return _worldPosition; }
    }

	public bool isDoor {
		get { return (_objectType.ToString().Contains("Door_")); }
	}
}
using System;
using UnityEngine;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Cell {
	public Coord position { get; protected set; }
	public Entity entity { get; protected set; }
	public List<MapObjectSprite> mapObjects;

	bool insight, hasseen;

	public Cell() {
		position = new Coord(0, 0);
		entity = null;
		mapObjects = new List<MapObjectSprite>();
		World.tileMap.onScreenChange += Reset;
	}

	public Cell(Coord pos) {
		position = pos;
		entity = null;
		mapObjects = new List<MapObjectSprite>();
		World.tileMap.onScreenChange += Reset;
	}

	public bool InSight {
		get { return insight; }
	}

	public bool Walkable {
		get { return (entity == null && mapObjects.FindAll(x => x.objectBase.solid).Count == 0); }
	}
	public bool Walkable_IgnoreEntity {
		get { return (mapObjects.FindAll(x => x.objectBase.solid || x.isDoor_Closed).Count == 0); }
	}

	public void SetEntity(Entity e) {
		entity = e;
		e.cell = this;
	}

	public void UnSetEntity(Entity e) {
		if (entity == e)
			entity = null;
		
		e.cell = null;
	}

	public void AddObject(MapObjectSprite mos) {
		mapObjects.Add(mos);
		mos.cell = this;
		mos.SetParams(insight, hasseen);
	}

	public void RemoveObject(MapObjectSprite mos) {
		mapObjects.Remove(mos);
		mos.cell = null;
	}

	public bool Reset(TileMap_Data oldMap, TileMap_Data newMap) {
		entity = null;
		mapObjects.Clear();
		return true;
	}

	public bool HasInventory() {
		return (mapObjects.Find(x => x.inv != null) != null);
	}

	public Item GetPool() {
		foreach (MapObjectSprite m in mapObjects) {
			if (m.inv != null) {
				foreach (Item i in m.inv.items) {
					if (i.HasProp(ItemProperty.Pool) && i.GetItemComponent<CLiquidContainer>() != null)
						return i;
				}
			}
		}

		return null;
	}

	public Inventory MyInventory() {
		return (mapObjects.Find(x => x.inv != null).inv);
	}

	public void UnregisterCallbacks() {
		World.tileMap.onScreenChange -= Reset;
	}

	public void UpdateInSight(bool inSight, bool hasSeen) {
		insight = inSight;
		hasseen = hasSeen;

		foreach (MapObjectSprite mos in mapObjects) {
			mos.SetParams(inSight, hasSeen);
		}
	}
}

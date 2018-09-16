﻿using UnityEngine;
using System.Collections.Generic;

public class MapObjectBlueprint {

	public string Name, spriteID;
	public string objectType;
	public int light;
	public Vector4 tint;
	public bool randomRotation, renderInBack, renderInFront;
	public string description;
	public MapOb_Interactability solid;
	public Container container;
    public LuaCall onInteract;

	public MapObjectBlueprint() {
		Name = "";
		objectType = "None";
		tint = new Vector4(1,1,1,1);
		randomRotation = false;
		renderInBack = false;
		renderInFront = false;
		description = "";
		spriteID = "";
		light = 0;
		solid = MapOb_Interactability.No;
	}

	public void SetTint(Vector4 color) {
		tint = color;
	}

	public enum MapOb_Interactability {
		No,
		Solid,
		Damage
	}

	public class Container {
		public int capacity;

		public Container(int _capacity) {
			capacity = _capacity;
		}
	}
}

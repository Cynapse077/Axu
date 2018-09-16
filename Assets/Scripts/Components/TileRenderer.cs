﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileRenderer : MonoBehaviour {
	public bool lit;

	SpriteRenderer spriteRenderer;
	bool inSight, hasSeen;
	Color outOfSightColor, litColor = new Color(1f, 1f, 0.9f);

	void Awake() {
		spriteRenderer = GetComponent<SpriteRenderer>();
		lit = false;
		outOfSightColor = Color.grey * 1.25f;
	}

	public void SetSprite(Sprite sprite) {
		spriteRenderer.sprite = sprite;
	}

	public void SetParams(bool insight, bool hasseen) {
		hasSeen = hasseen;
		ShowHide(insight);
	}

	void ShowHide(bool sighted) {
		if (inSight != sighted) {
			inSight = sighted;

			if (inSight)
				hasSeen = true;
		}

		SetSpriteColor();
	}

	public void SetSpriteColor() {
		if (!hasSeen) {
			spriteRenderer.color = Color.clear;
		} else {
			if (lit)
				spriteRenderer.color = (inSight) ? litColor : outOfSightColor;
			else
				spriteRenderer.color = (inSight) ? Color.white : outOfSightColor;
		}
	}
}

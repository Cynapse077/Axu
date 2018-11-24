using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldMap_Test : MonoBehaviour {
	[Header("Values")]
	[Range(2, 10)] public int layers;
	public float outerScale;
	public float innerScale;
	public float heatScale;

	[Space(5)]
	[Header("Colors")]
	public Color ocean;
	public Color plains;
	public Color forest;
	public Color shore;
	public Color mountains;
	public Color tallMountains;

	int seed;
	int[,] map;
	Texture2D tex;
	WorldMap.Parameters par;

	void Start() {
		seed = System.DateTime.Now.GetHashCode();
		Display();
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.Space)) {
			seed++;
			Display();
		}
	}

	void OnValidate() {
		if (Application.isPlaying) {
			par.layers = layers;
			par.outerScale = outerScale;
			par.innerScale = innerScale;
			par.heatScale = heatScale;

			Display();
		}
	}

	public void Display() {
		int width = Manager.worldMapSize.x, height = Manager.worldMapSize.y;

		map = Generate_WorldMap.Generate(new System.Random(seed), width, height, layers, outerScale, innerScale);
        tex = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point
        };

        for (int x = 0; x < map.GetLength(0); x++) {
			for (int y = 0; y < map.GetLength(1); y++) {
				tex.SetPixel(x, y, GetPixel(x, y));
			}
		}

		tex.Apply();
		GetComponent<Renderer>().material.mainTexture = tex;
	}

	Color GetPixel(int x, int y) {

		switch (map[x, y]) {
		case 0: return ocean;
		case 1: return forest;
		case 2: return plains;
		case 3: return mountains;
		case 4: return tallMountains;
		case 5: return shore;

		default: return ocean;
		}
	}
}

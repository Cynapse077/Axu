using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LitJson;

[System.Serializable]
public class TilesetManager : MonoBehaviour {
	public List<Tileset> tilesets;
}

[System.Serializable]
public class Tileset {
	public string Name;
	public int[] Norm;
	public int[] Rare;
	public Sprite[] Autotile;
}
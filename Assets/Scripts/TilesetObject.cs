using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tileset")]
public class TilesetObject : ScriptableObject
{
    public List<Tileset> tileSets;
}

[System.Serializable]
public class Tileset
{
    public string Name;
    public int[] Norm;
    public int[] Rare;
    public Sprite[] Autotile;
}

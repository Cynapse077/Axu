using UnityEngine;

[System.Serializable]
public class TilesetManager : MonoBehaviour 
{
    public TilesetObject ts;

    public Tileset GetTileSet(int id) 
    {
        return ts.tileSets[id];
    }

    public Tileset GetTileSet(string id) 
    {
        return ts.tileSets.Find(x => x.Name == id);
    }
}
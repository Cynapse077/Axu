using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using System.IO;

[MoonSharpUserData]
public class Tile_Data
{
    public int ID;
    public Biome biome;
    List<string> tags;
    public int costToEnter;
    TileAtlas atlas;

    public bool Autotiles { get { return atlas != null; } }

    public Tile_Data(int _id, Biome _biome, List<string> _tags, int _cost, TileAtlas _atlas)
    {
        ID = _id;
        tags = _tags;
        biome = _biome;
        costToEnter = _cost;
        atlas = _atlas;
    }

    public bool HasTag(string tag)
    {
        return tags.Contains(tag);
    }

    public bool CanDig(bool explosion)
    {
        return (!explosion) ? HasTag("Breakable") : HasTag("Breakable_Explosion");
    }
}

public class TileAtlas
{
    const int textureSize = 16;
    static readonly Vector2 pivot = new Vector2(0.5f, 0.5f);
    Texture2D texture;
    int columns;

    public TileAtlas(string path, int columns)
    {
        this.columns = columns;
        texture = new Texture2D(textureSize, textureSize);
        string fullPath = Path.Combine(Application.streamingAssetsPath, path);
        byte[] imageBytes = File.ReadAllBytes(fullPath);
        texture.LoadImage(imageBytes);
    }

    public Sprite GetSpriteAt(int index)
    {
        int row = 0;
        int column = 0;

        while (index > columns)
        {
            index -= columns;
            ++row;
        }

        column = index;

        return Sprite.Create(texture, new Rect(column * textureSize, row * textureSize, textureSize, textureSize), pivot);
    }
}
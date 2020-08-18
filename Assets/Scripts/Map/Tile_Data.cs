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

    public bool Autotiles => atlas != null;
    public bool Walkable => HasTag("Walkable");

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
        return (explosion) ? HasTag("Breakable_Explosion") : HasTag("Breakable");
    }
}

public class TileAtlas
{
    static readonly Vector2 pivot = new Vector2(0.5f, 0.5f);
    Texture2D texture;
    readonly int textureSize;
    readonly int columns;

    public TileAtlas(string path, int columns)
    {
        this.columns = columns;
        this.textureSize = Manager.TileResolution;
        this.texture = new Texture2D(textureSize, textureSize);

        string fullPath = Path.Combine(Application.streamingAssetsPath, path);
        byte[] imageBytes = File.ReadAllBytes(fullPath);
        texture.LoadImage(imageBytes);
    }

    public Sprite GetSpriteAt(int index)
    {
        int row = 0;

        while (index > columns)
        {
            index -= columns;
            ++row;
        }

        return Sprite.Create(texture, new Rect(index * textureSize, row * textureSize, textureSize, textureSize), pivot);
    }
}
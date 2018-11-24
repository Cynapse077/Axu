using UnityEngine;
using System.IO;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class SpriteManager
{
    public static bool initialized;
    static Dictionary<string, Sprite> npcSprites;
    static Dictionary<string, Sprite> objectSprites;

    public static void Init()
    {
        initialized = false;
        FillSpriteData();
        initialized = true;
    }

    static void FillSpriteData()
    {
        //Objects
        objectSprites = new Dictionary<string, Sprite>();
        string objectSpritePath = (Application.streamingAssetsPath + "/Data/Art/Objects");
        DirectoryInfo di = new DirectoryInfo(objectSpritePath);
        FileInfo[] info = di.GetFiles("*.png");

        foreach (FileInfo f in info)
        {
            objectSprites.Add(Path.GetFileName(f.FullName), SpriteFromFile(f, false));
        }

        objectSpritePath = (Application.streamingAssetsPath + "/Data/Art/Objects/Items");
        di = new DirectoryInfo(objectSpritePath);
        info = di.GetFiles("*.png");

        foreach (FileInfo f in info)
        {
            objectSprites.Add(Path.GetFileName(f.Name), SpriteFromFile(f, false));
        }

        //NPCs
        npcSprites = new Dictionary<string, Sprite>();
        string npcSpritePath = (Application.streamingAssetsPath + "/Data/Art/NPCs");
        di = new DirectoryInfo(npcSpritePath);
        info = di.GetFiles("*.png");

        foreach (FileInfo f in info)
        {
            npcSprites.Add(Path.GetFileName(f.FullName), SpriteFromFile(f, true));
        }
    }

    static Sprite SpriteFromFile(FileInfo f, bool overridePivot)
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, true);
        byte[] imageBytes = File.ReadAllBytes(f.FullName);
        tex.LoadImage(imageBytes);
        tex.filterMode = FilterMode.Point;

        Vector2 pivot = (tex.height > tex.width) ? new Vector2(0.5f, (tex.width / (float)tex.height) / 2f) : new Vector2(0.5f, 0.5f);

        if (overridePivot)
            pivot = new Vector2(0.5f, 0);

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 16f);
    }

    public static Sprite GetObjectSprite(string id)
    {
        if (!objectSprites.ContainsKey(id))
        {
            Debug.LogError("No Object sprite by key: \"" + id + "\"");
            return Sprite.Create(null, new Rect(0, 0, 0, 0), Vector2.zero);
        }

        return objectSprites[id];
    }

    public static Sprite GetNPCSprite(string id)
    {
        if (!npcSprites.ContainsKey(id))
        {
            Debug.LogError("No NPC sprite by key: \"" + id + "\"");
            return Sprite.Create(null, new Rect(0, 0, 0, 0), Vector2.zero);
        }

        return npcSprites[id];
    }
}

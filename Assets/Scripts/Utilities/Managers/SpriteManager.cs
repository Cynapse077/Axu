using UnityEngine;
using System.IO;
using System.Collections.Generic;

public enum SpriteType
{
    Object,
    NPC
}

[MoonSharp.Interpreter.MoonSharpUserData]
public static class SpriteManager
{
    static Dictionary<string, Sprite> npcSprites = new Dictionary<string, Sprite>();
    static Dictionary<string, Sprite> objectSprites = new Dictionary<string, Sprite>();

    public static void AddObjectSprites(string directoryPath, SpriteType spriteType)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        DirectoryInfo di = new DirectoryInfo(directoryPath);
        FileInfo[] info = di.GetFiles("*.png", SearchOption.AllDirectories);

        foreach (FileInfo f in info)
        {
            if (spriteType == SpriteType.Object)
                objectSprites.Add(Path.GetFileName(f.FullName), SpriteFromFile(f, false));
            else if (spriteType == SpriteType.NPC)
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
        {
            pivot = new Vector2(0.5f, 0);
        }

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 16f);
    }

    public static Sprite GetObjectSprite(string id)
    {
        if (!objectSprites.ContainsKey(id))
        {
            Debug.LogError("No Object sprite with key: \"" + id + "\"");
            return Sprite.Create(null, new Rect(0, 0, 0, 0), Vector2.zero);
        }

        return objectSprites[id];
    }

    public static Sprite GetNPCSprite(string id)
    {
        if (!npcSprites.ContainsKey(id))
        {
            Debug.LogError("No NPC sprite with key: \"" + id + "\"");
            return Sprite.Create(null, new Rect(0, 0, 0, 0), Vector2.zero);
        }

        return npcSprites[id];
    }
}

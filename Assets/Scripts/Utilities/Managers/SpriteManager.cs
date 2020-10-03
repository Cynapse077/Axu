using UnityEngine;
using System.IO;
using System.Collections.Generic;

public enum SpriteType
{
    Object,
    NPC
}

public struct ModSprite
{
    public Sprite sprite;
    public string modID;

    public ModSprite(Sprite spr, string mID)
    {
        sprite = spr;
        modID = mID;
    }
}

[MoonSharp.Interpreter.MoonSharpUserData]
public static class SpriteManager
{
    static Dictionary<string, ModSprite> npcSprites = new Dictionary<string, ModSprite>();
    static Dictionary<string, ModSprite> objectSprites = new Dictionary<string, ModSprite>();

    public static void ResetAll()
    {
        npcSprites.Clear();
        objectSprites.Clear();
    }

    public static void AddObjectSprites(Mod mod, string directoryPath, SpriteType spriteType)
    {
        if (Directory.Exists(directoryPath))
        {
            DirectoryInfo di = new DirectoryInfo(directoryPath);
            FileInfo[] info = di.GetFiles("*.png", SearchOption.AllDirectories);

            foreach (FileInfo f in info)
            {
                string fileName = Path.GetFileName(f.FullName);

                if (spriteType == SpriteType.Object)
                    objectSprites.Add(fileName, SpriteFromFile(mod, f, false));
                else if (spriteType == SpriteType.NPC)
                    npcSprites.Add(fileName, SpriteFromFile(mod, f, true));
            }
        }
    }

    static ModSprite SpriteFromFile(Mod mod, FileInfo f, bool overridePivot)
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

        return new ModSprite(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, Manager.TileResolution), mod.id);
    }

    public static Sprite GetObjectSprite(string id)
    {
        if (!objectSprites.ContainsKey(id))
        {
            Debug.LogError("No Object sprite with key: \"" + id + "\"");
            return Sprite.Create(null, new Rect(0, 0, 0, 0), Vector2.zero);
        }

        return objectSprites[id].sprite;
    }

    public static Sprite GetNPCSprite(string id)
    {
        if (!npcSprites.ContainsKey(id))
        {
            Debug.LogError("No NPC sprite with key: \"" + id + "\"");
            return Sprite.Create(null, new Rect(0, 0, 0, 0), Vector2.zero);
        }

        return npcSprites[id].sprite;
    }
}

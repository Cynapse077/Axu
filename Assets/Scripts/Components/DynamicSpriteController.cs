using UnityEngine;
using System.IO;

public class DynamicSpriteController : MonoBehaviour, EntitySprite
{
    public SpriteRenderer body;
    public Sprite swimmingSprite;

    string spritePath;
    Texture2D[] bodyPieces;
    Texture2D newTex;
    Texture2D baseSprite;
    Color[] emptyColors;
    Color blank;

    const int BaseBodySlot = 2;

    void Init()
    {
        baseSprite = new Texture2D(18, 18, TextureFormat.ARGB32, true);
        spritePath = Path.Combine(Application.streamingAssetsPath,"Mods/Core/Art/Player/Bases/char-baseBody.png");
        byte[] imageBytes = File.ReadAllBytes(spritePath);
        baseSprite.LoadImage(imageBytes);
        baseSprite.filterMode = FilterMode.Point;

        //Fill array of transparent colors.
        blank = Color.clear;
        emptyColors = new Color[baseSprite.width * baseSprite.height];

        for (int x = 0; x < baseSprite.width; x++)
        {
            for (int y = 0; y < baseSprite.height; y++)
            {
                emptyColors[y * baseSprite.width + x] = blank;
            }
        }

        newTex = new Texture2D(baseSprite.width, baseSprite.height, TextureFormat.ARGB32, true)
        {
            filterMode = FilterMode.Point,
            mipMapBias = 0f
        };
    }

    public void SetSprite(Item.ItemRenderer rend, bool remove)
    {
        if (rend.slot < 0 || string.IsNullOrEmpty(rend.onPlayer))
        {
            return;
        }

        Debug.Log(rend.ToString());

        if (bodyPieces == null || bodyPieces.Length <= 0)
        {
            bodyPieces = new Texture2D[10];
        }

        if (remove)
        {
            bodyPieces[rend.slot] = null;
        }
        else
        {
            ApplyTextureDefaults(rend.slot);
            string path = Path.Combine(Application.streamingAssetsPath, rend.onPlayer);

            if (File.Exists(path))
            {
                byte[] imageBytes = File.ReadAllBytes(path);
                bodyPieces[rend.slot].LoadImage(imageBytes);
            }
            else
            {
                bodyPieces[rend.slot] = null;
            }
        }

        ApplyLayers();
    }

    void ApplyLayers()
    {
        if (newTex == null || baseSprite == null)
        {
            Init();
        }

        newTex.SetPixels(emptyColors);

        for (int i = 0; i < bodyPieces.Length; i++)
        {
            //Body - After back/offhand
            if (i == BaseBodySlot && baseSprite != null)
            {
                MergeTextures(ref newTex, baseSprite);
            }
            //The rest
            if (bodyPieces[i] != null)
            {
                MergeTextures(ref newTex, bodyPieces[i]);
            }
        }

        newTex.Apply();
        body.sprite = Sprite.Create(newTex, new Rect(0, 0, baseSprite.width, baseSprite.height), new Vector2(0.5f, 0), 16f);
    }

    void MergeTextures(ref Texture2D t1, Texture2D t2)
    {
        Color[] c1 = t1.GetPixels(), c2 = t2.GetPixels();

        //Skip if the textures are different sizes. For now.
        if (c2.Length != c1.Length)
        {
            return;
        }

        for (int i = 0; i < c2.Length; i++)
        {
            c1[i] = Color.Lerp(c1[i], c2[i], c2[i].a);
        }

        t1.SetPixels(c1);
    }

    void ApplyTextureDefaults(int id)
    {
        if (baseSprite == null)
        {
            Init();
        }

        bodyPieces[id] = new Texture2D(baseSprite.width, baseSprite.height, TextureFormat.ARGB32, true)
        {
            filterMode = FilterMode.Point
        };
    }

    public void SetSwimming(bool swim)
    {
        if (baseSprite != null)
        {
            body.sprite = (swim) ? swimmingSprite : Sprite.Create(newTex, new Rect(0, 0, baseSprite.width, baseSprite.height), new Vector2(0.5f, 0), 16f);
        }
    }
}

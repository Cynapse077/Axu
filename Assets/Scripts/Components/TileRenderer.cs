using UnityEngine;

public class TileRenderer : MonoBehaviour
{
    public bool lit;
    public SpriteRenderer maskRenderer;
    public Sprite[] maskTextures;

    SpriteRenderer spriteRenderer;
    bool inSight, hasSeen;
    Color outOfSightColor;
    int posX, posY;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        lit = false;
        outOfSightColor = Color.black;
        outOfSightColor.a = 0.5f;
    }

    public void GiveCoords(int x, int y)
    {
        posX = x;
        posY = y;
    }

    public void SetSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }

    public void SetParams(bool insight, bool hasseen)
    {
        hasSeen = hasseen;
        ShowHide(insight);
    }

    void ShowHide(bool iS)
    {
        if (inSight != iS)
        {
            inSight = iS;

            if (inSight)
            {
                hasSeen = true;
            }
        }

        SetSpriteColor();
    }

    public void SetSpriteColor()
    {
        maskRenderer.sprite = maskTextures[inSight ? BitwiseAutotile() : Manager.TileResolution];
        maskRenderer.color = hasSeen ? outOfSightColor : Color.white;
        spriteRenderer.color = Color.white;
    }

    int BitwiseAutotile()
    {
        byte sum = 0;
        int width = Manager.localMapSize.x;
        int height = Manager.localMapSize.y;

        bool N = (posY < height - 1 && TileNotInSight(posX, posY + 1));
        bool E = (posX < width - 1 && TileNotInSight(posX + 1, posY));
        bool S = (posY > 0 && TileNotInSight(posX, posY - 1));
        bool W = (posX > 0 && TileNotInSight(posX - 1, posY));

        if (N) sum |= 1;
        if (W) sum |= 2;
        if (E) sum |= 4;
        if (S) sum |= 8;

        return sum;
    }

    bool TileNotInSight(int x, int y)
    {
        return !World.tileMap.GetCellAt(x, y).InSight;
    }
}

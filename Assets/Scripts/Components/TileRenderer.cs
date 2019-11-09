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

    public void SetPosition(int x, int y)
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

        if (posY < height - 1 && !TileInSight(posX, posY + 1)) sum |= 1 << 0;  //N
        if (posX > 0 && !TileInSight(posX - 1, posY)) sum |= 1 << 1;           //W
        if (posX < width - 1 && !TileInSight(posX + 1, posY)) sum |= 1 << 2;   //E
        if (posY > 0 && !TileInSight(posX, posY - 1)) sum |= 1 << 3;           //S

        return sum;
    }

    bool TileInSight(int x, int y)
    {
        return World.tileMap.GetCellAt(x, y).InSight;
    }
}

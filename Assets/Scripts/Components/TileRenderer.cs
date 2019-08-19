using UnityEngine;

public class TileRenderer : MonoBehaviour
{
    public bool lit;
    public SpriteRenderer maskRenderer;
    public Sprite[] maskTextures;

    SpriteRenderer spriteRenderer;
    bool inSight, hasSeen;
    Color outOfSightColor;
    static Color litColor = new Color(1.0f, 1.0f, 0.9f);
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
        maskRenderer.sprite = maskTextures[inSight ? BitwiseAutotile() : 16];

        maskRenderer.color = (hasSeen ? outOfSightColor : Color.white);
        spriteRenderer.color = (inSight && lit ? litColor : Color.white);
    }

    int BitwiseAutotile()
    {
        int sum = 0;
        int width = Manager.localMapSize.x;
        int height = Manager.localMapSize.y;

        if (posY < height - 1 && TileNotInSight(posX, posY + 1))
            sum++;
        if (posX > 0 && TileNotInSight(posX - 1, posY))
            sum += 2;
        if (posX < width - 1 && TileNotInSight(posX + 1, posY))
            sum += 4;
        if (posY > 0 && TileNotInSight(posX, posY - 1))
            sum += 8;

        return sum;
    }

    bool TileNotInSight(int x, int y)
    {
        return !World.tileMap.GetCellAt(x, y).InSight;
    }
}

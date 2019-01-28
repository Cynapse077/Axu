using UnityEngine;

public class WrapSprite : MonoBehaviour
{
    public Vector2 scrollDelta;

    Material mat;
    Vector2 newOffset, oldOffset;

    void Start()
    {
        mat = GetComponent<SpriteRenderer>().material;
        scrollDelta.x *= 0.00008f;
        scrollDelta.y *= 0.00008f;
        World.turnManager.incrementTurnCounter += OffsetClouds;
        OffsetClouds();
    }

    void OnDestroy()
    {
        World.turnManager.incrementTurnCounter -= OffsetClouds;
    }

    void FixedUpdate()
    {
        if (!GameSettings.Enable_Weather)
        {
            gameObject.SetActive(false);
            return;
        }

        oldOffset = mat.GetTextureOffset("_MainTex");

        if (oldOffset != newOffset)
        {
            mat.SetTextureOffset("_MainTex", Vector2.Lerp(oldOffset, newOffset, Time.deltaTime * 10f));
        }
    }

    void OffsetClouds()
    {
        newOffset -= scrollDelta;
    }
}

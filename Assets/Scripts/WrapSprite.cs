using UnityEngine;

public class WrapSprite : MonoBehaviour
{
    const float lerpSpeed = 10f;
    public Vector2 scrollDelta;

    Material mat;
    Vector2 newOffset, oldOffset;

    void Start()
    {
        mat = GetComponent<SpriteRenderer>().material;
        scrollDelta.x *= 8f / 100000f;
        scrollDelta.y *= 5f / 100000f;
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
            mat.SetTextureOffset("_MainTex", Vector2.Lerp(oldOffset, newOffset, Time.fixedDeltaTime * lerpSpeed));
        }
    }

    void OffsetClouds()
    {
        newOffset -= scrollDelta;
    }
}

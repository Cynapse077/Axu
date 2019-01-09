using UnityEngine;

public class NPCSprite : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Transform spriteObject;
    public GameObject questIcon;

    void OnEnable()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        spriteObject = spriteRenderer.transform;
    }

    public void SetSprite(string spriteKey)
    {
        spriteRenderer.sprite = SpriteManager.GetNPCSprite(spriteKey);
        int lx = (Random.value < 0.5f) ? 1 : -1;
        SetXScale(lx);
    }

    public void SetXScale(int x)
    {
        spriteObject.GetComponent<SpriteRenderer>().flipX = (x > 0);
    }

    public void SetEnabled(bool enabled)
    {
        spriteRenderer.enabled = enabled;
    }
}

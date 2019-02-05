using UnityEngine;

public class NPCSprite : MonoBehaviour, EntitySprite
{
    public GameObject questIcon;
    public Sprite swimmingSprite;

    SpriteRenderer spriteRenderer;
    Transform spriteObject;
    string spriteID;

    void OnEnable()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        spriteObject = spriteRenderer.transform;
    }

    public void SetSprite(string spriteKey)
    {
        spriteID = spriteKey;
        spriteRenderer.sprite = SpriteManager.GetNPCSprite(spriteID);
        int lx = (Random.value < 0.5f) ? 1 : -1;
        SetXScale(lx);
    }

    public void SetXScale(int x)
    {
        if (x != 0)
        {
            spriteObject.GetComponent<SpriteRenderer>().flipX = (x > 0);
        }
    }

    public void SetEnabled(bool enabled)
    {
        spriteRenderer.enabled = enabled;
    }

    public void SetSwimming(bool swim)
    {
        if (swim)
        {
            spriteRenderer.sprite = swimmingSprite;
        }
        else if (!string.IsNullOrEmpty(spriteID))
        {
            spriteRenderer.sprite = SpriteManager.GetNPCSprite(spriteID);
        }
        
    }
}

public interface EntitySprite
{
    void SetSwimming(bool swim);
}

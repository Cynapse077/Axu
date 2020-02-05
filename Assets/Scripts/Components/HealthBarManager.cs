using UnityEngine;

public class HealthBarManager : MonoBehaviour
{
    public GameObject hpBack;
    public Transform hpBar;
    public GameObject stBack;
    public Transform stBar;
    public SpriteRenderer spriteRenderer;

    Vector3 fullHP = new Vector3(0, 0, -0.05f);
    Vector3 emptyHP = new Vector3(-0.5f, 0, -0.05f);
    SpriteRenderer spr;
    Entity entity;
    Stats stats;

    // Use this for initialization
    void Start()
    {
        entity = GetComponent<Entity>();
        stats = GetComponent<Stats>();

        hpBack.SetActive(false);
        stats.hpChanged += UpdateHP;
        World.turnManager.incrementTurnCounter += UpdateHP;

        if (stBack != null)
        {
            stBack.SetActive(false);
            stats.stChanged += UpdateST;
            World.turnManager.incrementTurnCounter += UpdateST;
        }
    }

    void OnDisable()
    {
        if (stats != null)
        {
            stats.hpChanged -= UpdateHP;
            World.turnManager.incrementTurnCounter -= UpdateHP;

            if (stBack != null)
            {
                stats.stChanged -= UpdateST;
                World.turnManager.incrementTurnCounter -= UpdateST;
            }
        }
    }

    void UpdateHP()
    {
        //HP
        if (stats.health > 0)
        {
            bool active = stats.health < stats.MaxHealth;

            if (entity.isPlayer)
                hpBack.SetActive(active);
            else
                hpBack.SetActive(active && spriteRenderer.enabled);

            float x = stats.health / (float)stats.MaxHealth;
            Vector3 sc = new Vector3(x, 1, 1);

            hpBar.localScale = sc;
            hpBar.localPosition = Vector3.Lerp(emptyHP, fullHP, x);
        }
    }

    void UpdateST()
    {
        //ST - Player only
        if (stats.stamina > 0 && entity.isPlayer)
        {
            stBack.SetActive(stats.stamina < stats.MaxStamina);

            float x = stats.stamina / (float)stats.MaxStamina;
            Vector3 sc = new Vector3(x, 1, 1);

            stBar.localScale = sc;
            stBar.localPosition = Vector3.Lerp(emptyHP, fullHP, x);
        }
    }
}

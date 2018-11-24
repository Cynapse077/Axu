using UnityEngine;

public class StatusEffectPanel : MonoBehaviour
{
    public GameObject flying;
    public GameObject poison;
    public GameObject bleed;
    public GameObject confuse;
    public GameObject topple;
    public GameObject stun;
    public GameObject slow;
    public GameObject regen;
    public GameObject haste;
    public GameObject stuck;
    public GameObject held;
    public GameObject unconscious;
    public GameObject shield;
    public GameObject drunk;
    public GameObject burning;
    public GameObject sick;
    public GameObject blind;

    public Sprite[] hungerSprites;

    public void UpdateEnabledStatuses(Stats stats)
    {
        if (stats == null)
            return;

        flying.SetActive(stats.IsFlying());
        poison.SetActive(stats.HasEffect("Poison"));
        bleed.SetActive(stats.HasEffect("Bleed"));
        confuse.SetActive(stats.HasEffect("Confuse"));
        topple.SetActive(stats.HasEffect("Topple"));
        stun.SetActive(stats.HasEffect("Stun"));
        slow.SetActive(stats.HasEffect("Slow"));
        regen.SetActive(stats.HasEffect("Regen"));
        haste.SetActive(stats.HasEffect("Haste"));
        stuck.SetActive(stats.HasEffect("Stuck"));
        held.SetActive(stats.entity.body.AllGripsAgainst().Count > 0);
        shield.SetActive(stats.HasEffect("Shield"));
        unconscious.SetActive(stats.HasEffect("Unconscious"));
        drunk.SetActive(stats.HasEffect("Drunk"));
        burning.SetActive(stats.HasEffect("Aflame"));
        sick.SetActive(stats.HasEffect("Sick"));
        blind.SetActive(stats.HasEffect("Blind"));
    }
}

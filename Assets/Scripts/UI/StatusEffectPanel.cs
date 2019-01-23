using UnityEngine;

public class StatusEffectPanel : MonoBehaviour
{
    public StatusEffectObject overburdened;
    public StatusEffectObject flying;
    public StatusEffectObject poison;
    public StatusEffectObject bleed;
    public StatusEffectObject confuse;
    public StatusEffectObject topple;
    public StatusEffectObject stun;
    public StatusEffectObject slow;
    public StatusEffectObject regen;
    public StatusEffectObject haste;
    public StatusEffectObject stuck;
    public StatusEffectObject held;
    public StatusEffectObject unconscious;
    public StatusEffectObject shield;
    public StatusEffectObject drunk;
    public StatusEffectObject burning;
    public StatusEffectObject sick;
    public StatusEffectObject blind;
    public StatusEffectObject offBalance;

    public void UpdateEnabledStatuses(Stats stats)
    {
        if (stats == null)
        {
            return;
        }

        overburdened.UpdateSE(stats.entity.inventory.overCapacity(), 0);
        flying.UpdateSE(stats.IsFlying(), 0);
        held.UpdateSE(stats.entity.body.AllGripsAgainst().Count > 0, 0);

        poison.UpdateSE(stats.HasEffect("Poison"), NumTurns(stats, "Poison"));
        bleed.UpdateSE(stats.HasEffect("Bleed"), NumTurns(stats, "Bleed"));
        confuse.UpdateSE(stats.HasEffect("Confuse"), NumTurns(stats, "Confuse"));
        topple.UpdateSE(stats.HasEffect("Topple"), NumTurns(stats, "Topple"));
        stun.UpdateSE(stats.HasEffect("Stun"), NumTurns(stats, "Stun"));
        slow.UpdateSE(stats.HasEffect("Slow"), NumTurns(stats, "Slow"));
        regen.UpdateSE(stats.HasEffect("Regen"), NumTurns(stats, "Regen"));
        haste.UpdateSE(stats.HasEffect("Haste"), NumTurns(stats, "Haste"));
        stuck.UpdateSE(stats.HasEffect("Stuck"), NumTurns(stats, "Stuck"));
        shield.UpdateSE(stats.HasEffect("Shield"), NumTurns(stats, "Shield"));
        unconscious.UpdateSE(stats.HasEffect("Unconscious"), NumTurns(stats, "Unconscious"));
        drunk.UpdateSE(stats.HasEffect("Drunk"), NumTurns(stats, "Drunk"));
        burning.UpdateSE(stats.HasEffect("Aflame"), NumTurns(stats, "Aflame"));
        sick.UpdateSE(stats.HasEffect("Sick"), NumTurns(stats, "Sick"));
        blind.UpdateSE(stats.HasEffect("Blind"), NumTurns(stats, "Blind"));
        offBalance.UpdateSE(stats.HasEffect("OffBalance"), NumTurns(stats, "OffBalance"));
    }

    int NumTurns(Stats stats, string se)
    {
        return (stats.HasEffect(se) ? stats.statusEffects[se] + 1 : 0);
    }
}

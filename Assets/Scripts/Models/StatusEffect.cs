using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Container class
public class StatusManager
{
    List<StatusEffect> statusEffects = new List<StatusEffect>();
    readonly Stats stats;

    public Stats MyStats { get { return stats; } }

    public StatusManager(Stats stats)
    {
        this.stats = stats;
    }

    public void ClearAllStatuses()
    {
        statusEffects.IterateAction_Reverse((x) => x.OnRemove());
    }

    public void AddStatusEffect(StatusEffect se)
    {
        if (se != null)
        {
            //Combine together if we can.
            foreach (StatusEffect status in statusEffects)
            {
                if (status.GetTooltip() == se.GetTooltip())
                {
                    status.turns += se.turns;
                    return;
                }
            }

            //Otherwise, add a new one.
            statusEffects.Add(se);
        }
    }

    public void RemoveStatusEffect(StatusEffect se)
    {
        if (se != null)
        {
            statusEffects.Remove(se);
        }
    }

    public void OnTurn()
    {
        statusEffects.IterateAction_Reverse((x) => x.OnTurn());
    }
}

//Base
public class StatusEffect
{
    public int turns;
    public bool perpetual;
    protected StatusManager seManager;

    protected Stats stats { get { return seManager.MyStats; } }

    public StatusEffect(int turns, StatusManager seManager, bool perpetual = false)
    {
        this.turns = turns;
        this.seManager = seManager;
        this.perpetual = perpetual;

        OnAdd();
    }

    public virtual void OnTurn()
    {
        if (!perpetual)
        {
            turns--;

            if (turns <= 0)
            {
                OnRemove();
            }
        }
    }

    public virtual void OnAdd()
    {
        seManager.AddStatusEffect(this);
    }

    public virtual void OnRemove()
    {
        seManager.RemoveStatusEffect(this);
    }

    public virtual string GetTooltip()
    {
        return "";
    }
}

public class Status_Regen : StatusEffect
{
    public Status_Regen(int turns, StatusManager seManager) : base(turns, seManager) { }

    public override void OnTurn()
    {
        seManager.MyStats.Heal(seManager.MyStats.Endurance + 1);
        base.OnTurn();
    }
}

public class Status_Poison : StatusEffect
{
    public Status_Poison(int turns, StatusManager seManager) : base(turns, seManager) { }

    public override void OnAdd()
    {
        if (stats.hasTraitEffect(TraitEffects.Poison_Resist))
        {
            if (Random.Range(0, 100) < 25)
            {
                turns /= 2;
            }
        }

        base.OnAdd();
    }

    public override void OnTurn()
    {
        int amount = Random.Range(1, 5);

        if (stats.hasTraitEffect(TraitEffects.Poison_Resist))
        {
            if (amount == 1 || Random.Range(0, 100) < 20)
            {
                amount = 0;
            }
        }

        if (amount > 0 && stats.health > amount)
        {
            stats.StatusEffectDamage(amount, DamageTypes.Venom);
        }
        
        base.OnTurn();
    }
}

public class Status_Bleed : StatusEffect
{
    public Status_Bleed(int turns, StatusManager seManager) : base(turns, seManager) { }

    public override void OnAdd()
    {
        if (stats.hasTraitEffect(TraitEffects.Bleed_Resist))
        {
            if (Random.Range(0, 100) < 25)
            {
                turns /= 2;
            }
        }

        base.OnAdd();
    }

    public override void OnTurn()
    {
        int amount = Random.Range(2, 6);

        if (stats.hasTraitEffect(TraitEffects.Poison_Resist))
        {
            if (Random.Range(0, 100) < 20)
            {
                amount = 1;
            }
        }

        if (stats.health > amount)
        {
            stats.StatusEffectDamage(amount, DamageTypes.Bleed);
        }

        base.OnTurn();
    }
}

public class Status_Aflame : StatusEffect
{
    public Status_Aflame(int turns, StatusManager seManager) : base(turns, seManager) { }

    public override void OnAdd()
    {
        if (!TileManager.isWaterTile(World.tileMap.GetTileID(stats.entity.posX, stats.entity.posY)))
        {
            base.OnAdd();
        }
    }

    public override void OnTurn()
    {
        if (TileManager.isWaterTile(World.tileMap.GetTileID(stats.entity.posX, stats.entity.posY)))
        {
            turns = 0;
        }
        else
        {
            int damage = Random.Range(2, 5);
            float dm = damage;
            dm *= (-stats.HeatResist * 0.01f);
            damage += (int)dm;
            stats.StatusEffectDamage(damage, DamageTypes.Heat);
        }

        base.OnTurn();
    }
}

public class Status_Sick : StatusEffect
{
    public Status_Sick(int turns, StatusManager seManager) : base(turns, seManager) { }

    public override void OnAdd()
    {
        stats.ChangeAttribute("Accuracy", -1);
        stats.ChangeAttribute("Stealth", -1);
        stats.ChangeAttribute("Dexterity", -1);
        base.OnAdd();
    }

    public override void OnRemove()
    {
        stats.ChangeAttribute("Accuracy", 1);
        stats.ChangeAttribute("Stealth", 1);
        stats.ChangeAttribute("Dexterity", 1);
        base.OnRemove();
    }

    public override void OnTurn()
    {
        if (Random.Range(0, 100) < 10)
        {
            int amount = Random.Range(1, 3);
            World.objectManager.CreatePoolOfLiquid(stats.entity.myPos, World.tileMap.WorldPosition, World.tileMap.currentElevation, "liquid_vomit", amount);
        }

        base.OnTurn();
    }
}

public class Status_Float : StatusEffect
{
    public Status_Float(int turns, StatusManager seManager) : base(turns, seManager) { }

    public override void OnAdd()
    {
        base.OnAdd();
    }

    public override void OnRemove()
    {
        base.OnRemove();
    }

    public override void OnTurn()
    {
        base.OnTurn();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffect
{
    public int turns;
    protected Stats stats;

    public StatusEffect(int turns, Stats stats)
    {
        this.turns = turns;
        this.stats = stats;
        OnAdd();
    }

    public virtual void OnTurn()
    {
        turns--;

        if (turns <= 0)
        {
            OnRemove();
        }
    }

    public virtual void OnAdd()
    {
        //stats.AddStatusEffect(this);
    }

    public virtual void OnRemove()
    {
        //stats.RemoveStatusEffect(this);
    }
}

public class Status_Regen : StatusEffect
{
    public Status_Regen(int turns, Stats stats) : base(turns, stats) { }

    public override void OnTurn()
    {
        stats.Heal(stats.Endurance + 1);
        base.OnTurn();
    }
}

public class Status_Poison : StatusEffect
{
    public Status_Poison(int turns, Stats stats) : base(turns, stats) { }

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
    public Status_Bleed(int turns, Stats stats) : base(turns, stats) { }

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
    public Status_Aflame(int turns, Stats stats) : base(turns, stats) { }

    public override void OnAdd()
    {
        if (!Tile.isWaterTile(World.tileMap.GetTileID(stats.entity.posX, stats.entity.posY), true))
        {
            base.OnAdd();
        }
    }

    public override void OnTurn()
    {
        if (Tile.isWaterTile(World.tileMap.GetTileID(stats.entity.posX, stats.entity.posY), true))
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
    public Status_Sick(int turns, Stats stats) : base(turns, stats) { }

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
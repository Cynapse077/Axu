using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class TileDamage
{
    public Coord pos;
    public int damage;
    public Entity spawner;
    public string myName;
    public bool crit;
    public HashSet<DamageTypes> dTypes;

    public TileDamage(Entity _spawner, Coord _pos, DamageTypes[] _dType)
    {
        spawner = _spawner;
        pos = _pos;

        dTypes = new HashSet<DamageTypes>();

        foreach (DamageTypes dt in _dType)
        {
            dTypes.Add(dt);
        }
    }

    public TileDamage(Entity _spawner, Coord _pos, HashSet<DamageTypes> _dType)
    {
        spawner = _spawner;
        pos = _pos;
        dTypes = _dType;
    }

    public void ApplyDamage()
    {
        if (World.tileMap.WalkableTile(pos.x, pos.y))
        {
            if (dTypes.Contains(DamageTypes.Cold))
                World.tileMap.FreezeTile(pos.x, pos.y);

            Cell c = World.tileMap.GetCellAt(pos);

            if (c.entity != null)
            {
                Stats targetStats = c.entity.GetComponent<Stats>();

                if (c.entity != spawner && damage > 0)
                    targetStats.IndirectAttack(damage, dTypes, spawner, myName, false, crit);

                if (dTypes.Contains(DamageTypes.Venom))
                    targetStats.AddStatusEffect("Poison", Random.Range(2, 7));
                if (dTypes.Contains(DamageTypes.NonLethal))
                    targetStats.AddStatusEffect("Confuse", Random.Range(3, 8));
                if (dTypes.Contains(DamageTypes.Cold))
                    targetStats.AddStatusEffect("Slow", Random.Range(2, 7));
            }
        }
    }
}
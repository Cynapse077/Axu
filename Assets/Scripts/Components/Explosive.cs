using UnityEngine;
using System.Collections.Generic;

public class Explosive : MonoBehaviour
{
    public Coord localPosition;
    public int radius = 1;
    public GameObject[] explosion;
    public bool destroy;
    public int damage;

    string nameOfDamage = "projectile";

    void Update()
    {
        Vector3 myPos = new Vector3(localPosition.x, localPosition.y, -0.5f);
        transform.position = Vector3.Lerp(transform.position, myPos, 0.1f);
    }

    public void SetName(string n)
    {
        nameOfDamage = n;
    }

    public void DetonateExplosion(HashSet<DamageTypes> dTypes, Entity spawner)
    {
        damage = Mathf.Min(Random.Range(10, 25) + World.DangerLevel(), 99);
        destroy = true;
        nameOfDamage = LocalizationManager.GetContent("Explosion");
        Instantiate(explosion[0], new Vector3(localPosition.x, localPosition.y - Manager.localMapSize.y, 0), Quaternion.identity);

        for (int x = localPosition.x - radius; x <= localPosition.x + radius; x++)
        {
            for (int y = localPosition.y - radius; y <= localPosition.y + radius; y++)
            {

                if (destroy && !World.tileMap.WalkableTile(x, y))
                {
                    World.tileMap.DigTile(x, y, true);
                }

                TileDamage td = new TileDamage(spawner, new Coord(x, y), new HashSet<DamageTypes>() { DamageTypes.Heat })
                {
                    damage = damage,
                    myName = nameOfDamage,
                    crit = false
                };

                td.ApplyDamage();
            }
        }

        World.soundManager.Explosion();
        Destroy(gameObject);
    }

    public void DetonateOneTile(Entity spawner)
    {
        int x = localPosition.x, y = localPosition.y;
        Instantiate(explosion[2], new Vector3(x, y, 0), Quaternion.identity);

        TileDamage td = new TileDamage(spawner, new Coord(x, y), new HashSet<DamageTypes>() { DamageTypes.Blunt })
        {
            damage = damage,
            myName = nameOfDamage,
            crit = false
        };
        td.ApplyDamage();

        Destroy(gameObject);
    }
}

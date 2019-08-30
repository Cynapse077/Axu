using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public class AI_Stationary
    {
        readonly NPC npc;
        readonly Entity entity;
        Entity target = null;

        public AI_Stationary(Entity entity, NPC npc)
        {
            this.entity = entity;
            this.npc = npc;
        }

        public virtual bool Act()
        {
            target = GetTarget();



            return target != null;
        }

        protected virtual Entity GetTarget()
        {
            if (target != null)
            {
                return target;
            }

            int wepRange = entity.inventory.HasSpearEquipped() ? 2 : 1;
            List<Entity> potentialTargets = new List<Entity>();

            for (int x = -wepRange; x <= wepRange; x++)
            {
                for (int y = -wepRange; y <= wepRange; y++)
                {
                    Coord dir = new Coord(x, y);

                    if (dir.IsCardinal() || dir.IsDiagonal())
                    {
                        Entity e = GetEntityInCell(entity.posX + x, entity.posY + y);

                        if (e != null && npc.IsHostileTo(e))
                        {
                            potentialTargets.Add(e);
                        }
                    }
                }
            }

            return potentialTargets.Count > 0 ? potentialTargets.GetRandom() : null;
        }

        Entity GetEntityInCell(int x, int y)
        {
            Cell c = World.tileMap.GetCellAt(x, y);

            if (c == null || c.entity == null)
            {
                return null;
            }

            return c.entity;
        }
    }
}

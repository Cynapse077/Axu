using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class Path_TileData
    {
        public bool walkable;
        public Coord position;
        public int costToEnter;

        public Path_TileData(bool _walkable, Coord _position, int _cost)
        {
            walkable = _walkable;
            position = _position;
            costToEnter = _cost;
        }

        public List<Path_TileData> GetNeighbours(bool diagOkay, TileMap_Data data)
        {
            List<Path_TileData> ns = new List<Path_TileData>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if ((x == 0 && y == 0) || (!diagOkay && Mathf.Abs(x) + Mathf.Abs(y) > 1))
                        continue;

                    if (!World.OutOfLocalBounds(position.x + x, position.y + y))
                        ns.Add(data.GetTileData(position.x + x, position.y + y));
                }
            }

            return ns;
        }

        public List<Path_TileData> GetNeighbours(bool diagOkay, WorldMap_Data data)
        {
            List<Path_TileData> ns = new List<Path_TileData>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if ((x == 0 && y == 0) || (!diagOkay && Mathf.Abs(x) + Mathf.Abs(y) > 1) || World.OutOfWorldBounds(position.x + x, position.y + y))
                        continue;

                    ns.Add(data.GetPathDataAt(position.x + x, position.y + y));
                }
            }

            return ns;
        }
    }
}
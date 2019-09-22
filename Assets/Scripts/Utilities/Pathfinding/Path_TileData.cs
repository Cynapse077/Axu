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

        //Local map
        public List<Path_TileData> GetNeighbours(bool diagOkay, TileMap_Data data)
        {
            List<Path_TileData> ns = new List<Path_TileData>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if ((x == 0 && y == 0) || (!diagOkay && Mathf.Abs(x) + Mathf.Abs(y) > 1))
                    {
                        continue;
                    }

                    if (!World.OutOfLocalBounds(position.x + x, position.y + y))
                    {
                        Path_TileData dat = data.GetTileData(position.x + x, position.y + y);
                        if (dat.walkable)
                        {
                            ns.Add(dat);
                        }
                    }
                }
            }

            return ns;
        }

        //World map
        public List<Path_TileData> GetNeighbours(bool diagOkay, WorldMap_Data data)
        {
            List<Path_TileData> ns = new List<Path_TileData>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if ((x == 0 && y == 0) || (!diagOkay && Mathf.Abs(x) + Mathf.Abs(y) > 1))
                    {
                        continue;
                    }

                    if (!World.OutOfWorldBounds(position.x + x, position.y + y))
                    {
                        Path_TileData dat = data.GetPathDataAt(position.x + x, position.y + y);
                        if (dat.walkable)
                        {
                            ns.Add(dat);
                        }
                    }                    
                }
            }

            return ns;
        }
    }
}
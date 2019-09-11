using System.Collections.Generic;

namespace Pathfinding
{
    public class Path_TileGraph
    {
        public Dictionary<Path_TileData, Path_Node> nodes;

        //World graph
        public Path_TileGraph(WorldMap_Data data)
        {
            nodes = new Dictionary<Path_TileData, Path_Node>();

            for (int x = 0; x < Manager.worldMapSize.x; x++)
            {
                for (int y = 0; y < Manager.worldMapSize.y; y++)
                {
                    Path_TileData tileData = data.GetPathDataAt(x, y);
                    Path_Node node = new Path_Node() { data = tileData };

                    if (tileData != null)
                    {
                        nodes.Add(tileData, node);
                    }
                }
            }

            foreach (Path_TileData t in nodes.Keys)
            {
                Path_Node n = nodes[t];
                List<Path_Edge> edges = new List<Path_Edge>();
                List<Path_TileData> neighbours = t.GetNeighbours(!GameSettings.FourWayMovement, data);

                // If neighbour is walkable, create an edge to the relevant node.
                for (int i = 0; i < neighbours.Count; i++)
                {
                    if (neighbours[i] != null && neighbours[i].walkable)
                    {
                        Path_Edge e = new Path_Edge { node = nodes[neighbours[i]] };
                        edges.Add(e);
                    }
                }

                n.edges = edges.ToArray();
            }
        }

        //Local graph
        public Path_TileGraph(TileMap_Data data)
        {
            nodes = new Dictionary<Path_TileData, Path_Node>();

            for (int x = 0; x < Manager.localMapSize.x; x++)
            {
                for (int y = 0; y < Manager.localMapSize.y; y++)
                {
                    Path_TileData tileData = data.GetTileData(x, y);
                    Path_Node node = new Path_Node { data = tileData };

                    if (tileData != null)
                    {
                        nodes.Add(tileData, node);
                    }
                }
            }

            int edgeCount = 0;

            foreach (Path_TileData t in nodes.Keys)
            {
                Path_Node n = nodes[t];
                List<Path_Edge> edges = new List<Path_Edge>();
                List<Path_TileData> neighbours = t.GetNeighbours(!GameSettings.FourWayMovement, data);

                // If neighbour is walkable, create an edge to the relevant node.
                for (int i = 0; i < neighbours.Count; i++)
                {
                    if (neighbours[i] != null && neighbours[i].walkable)
                    {
                        Path_Edge e = new Path_Edge { node = nodes[neighbours[i]] };
                        edges.Add(e);

                        edgeCount++;
                    }
                }

                n.edges = edges.ToArray();
            }
        }
    }
}
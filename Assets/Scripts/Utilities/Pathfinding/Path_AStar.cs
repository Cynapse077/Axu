using UnityEngine;
using Priority_Queue;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Pathfinding
{
    public enum PathResult
    {
        Success,
        Fail
    }

    public class Path_AStar
    {
        Queue<Path_Node> steps;
        public readonly PathResult result;
        public readonly Coord Start;
        public readonly Coord End;

        Dictionary<Path_Node, Path_Node> Came_From = new Dictionary<Path_Node, Path_Node>();
        Dictionary<Path_Node, float> g_score = new Dictionary<Path_Node, float>();
        Dictionary<Path_Node, float> f_score = new Dictionary<Path_Node, float>();
        List<Path_Node> ClosedSet = new List<Path_Node>();
        SimplePriorityQueue<Path_Node> OpenSet = new SimplePriorityQueue<Path_Node>();

        public int StepCount
        {
            get
            {
                return steps == null ? 0 : steps.Count;
            }
        }

        public bool Traversable
        {
            get
            {
                return result == PathResult.Success && StepCount > 0;
            }
        }

        public Path_AStar(Coord startCoord, Coord endCoord, bool ignoreCosts, Entity entity)
        {
            Start = startCoord;
            End = endCoord;
            if (World.tileMap.tileGraph == null)
            {
                World.tileMap.tileGraph = new Path_TileGraph(World.tileMap.CurrentMap);
            }

            if (startCoord == null || endCoord == null)
            {
                return;
            }

            Path_TileData start = World.tileMap.CurrentMap.GetTileData(startCoord.x, startCoord.y);
            Path_TileData end = World.tileMap.CurrentMap.GetTileData(endCoord.x, endCoord.y);

            if (start == null || end == null)
            {
                return;
            }

            if (!Calculate(World.tileMap.tileGraph.nodes, start, end, ignoreCosts, entity))
            {
                steps = null;
                result = PathResult.Fail;
            }

            result = PathResult.Success;
        }

        public Path_AStar(Coord startCoord, Coord endCoord, WorldMap_Data gr)
        {
            if (World.worldMap.tileGraph == null)
            {
                World.worldMap.tileGraph = new Path_TileGraph(World.worldMap.worldMapData);
            }

            if (startCoord == null || endCoord == null)
            {
                return;
            }

            Path_TileData start = gr.GetPathDataAt(startCoord.x, startCoord.y), end = gr.GetPathDataAt(endCoord.x, endCoord.y);

            if (!Calculate(World.worldMap.tileGraph.nodes, start, end, true, ObjectManager.playerEntity))
            {
                steps = null;
            }
        }

        public static List<Coord> GetPath(Coord start, Coord end, bool ignoreCosts, Entity entity)
        {
            Path_AStar p = new Path_AStar(start, end, ignoreCosts, entity);
            List<Coord> points = new List<Coord>();

            foreach (Path_Node pn in p.steps)
            {
                points.Add(pn.data.position);
            }

            return points;
        }

        bool Calculate(Dictionary<Path_TileData, Path_Node> nodes, Path_TileData start, Path_TileData end, bool ignoreCosts, Entity entity)
        {
            OpenSet.Enqueue(nodes[start], 0);

            foreach (Path_Node n in nodes.Values)
            {
                g_score[n] = Mathf.Infinity;
            }

            g_score[nodes[start]] = 0;

            foreach (Path_Node n in nodes.Values)
            {
                f_score[n] = Mathf.Infinity;
            }

            f_score[nodes[start]] = HeuristicCostEstimate(nodes[start].data.position, nodes[end].data.position);


            while (OpenSet.Count > 0)
            {
                Path_Node current = OpenSet.Dequeue();

                if (current == nodes[end])
                {
                    ReconstructPath(Came_From, current);
                    return true;
                }

                ClosedSet.Add(current);

                foreach (Path_Edge edge_neighbor in current.edges)
                {
                    Path_Node neighbor = edge_neighbor.node;

                    if (ClosedSet.Contains(neighbor) || !neighbor.data.walkable)
                    {
                        continue;
                    }

                    const float costToEnterFactor = 0.8f;
                    float tentative_g_score = g_score[current] + GetDistance(current, neighbor) + neighbor.data.costToEnter * costToEnterFactor;

                    if (ignoreCosts && neighbor.data.costToEnter < 100)
                    {
                        tentative_g_score -= neighbor.data.costToEnter * costToEnterFactor;
                    }

                    //Skip doors for NPCs.
                    if (OpenSet.Contains(neighbor) && tentative_g_score > g_score[neighbor] || !entity.isPlayer  && neighbor.data.costToEnter >= 100)
                    {
                        continue;
                    }

                    Came_From[neighbor] = current;
                    g_score[neighbor] = tentative_g_score;
                    f_score[neighbor] = g_score[neighbor] + HeuristicCostEstimate(neighbor.data.position, nodes[end].data.position);

                    if (!OpenSet.Contains(neighbor))
                    {
                        OpenSet.Enqueue(neighbor, f_score[neighbor]);
                    }
                }
            }

            return false;
        }


        float DistBetween(Path_Node a, Path_Node b)
        {
            if (Mathf.Abs(a.data.position.x - b.data.position.x) + Mathf.Abs(a.data.position.y - b.data.position.y) == 1)
                return 1.00f;

            if (Mathf.Abs(a.data.position.x - b.data.position.x) == 1 && Mathf.Abs(a.data.position.y - b.data.position.y) == 1)
                return 1.01f;

            return Mathf.Sqrt(Mathf.Pow(a.data.position.x - b.data.position.x, 2) + Mathf.Pow(a.data.position.y - b.data.position.y, 2));
        }

        void ReconstructPath(Dictionary<Path_Node, Path_Node> Came_From, Path_Node current)
        {
            Queue<Path_Node> total_path = new Queue<Path_Node>();
            total_path.Enqueue(current);

            while (Came_From.ContainsKey(current))
            {
                current = Came_From[current];
                total_path.Enqueue(current);
            }

            steps = new Queue<Path_Node>(total_path.Reverse());
        }

        float HeuristicCostEstimate(Coord a, Coord b)
        {
            return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.y - b.y, 2));
        }

        float GetDistance(Path_Node nodeA, Path_Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.data.position.x - nodeB.data.position.x);
            int dstY = Mathf.Abs(nodeA.data.position.y - nodeB.data.position.y);

            if (dstX > dstY)
            {
                return 14.0f * dstY + 10.0f * (dstX - dstY);
            }

            return 14.0f * dstX + 10.0f * (dstY - dstX);
        }

        public Coord GetNextStep()
        {
            if (steps == null || steps.Count <= 0)
            {
                return null;
            }

            Coord nextPosition = steps.Dequeue().data.position;
            return nextPosition;
        }
    }
}

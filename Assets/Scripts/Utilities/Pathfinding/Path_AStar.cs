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

    public struct CoordPair
    {
        public readonly Coord first;
        public readonly Coord second;

        public CoordPair(Coord c1, Coord c2)
        {
            this.first = c1;
            this.second = c2;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CoordPair))
            {
                return false;
            }

            CoordPair pair = (CoordPair)obj;

            return pair.first == first && pair.second == second;
        }

        public static bool operator ==(CoordPair c1, CoordPair c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(CoordPair c1, CoordPair c2)
        {
            return !c1.Equals(c2);
        }

        public override int GetHashCode()
        {
            return first.GetHashCode() ^ second.GetHashCode();
        }
    }

    public class Path_AStar
    {
        Queue<Path_Node> steps;
        public readonly PathResult result;
        public readonly Coord start;
        public readonly Coord end;

        static Dictionary<Path_Node, Path_Node> Came_From = new Dictionary<Path_Node, Path_Node>();
        static Dictionary<Path_Node, float> g_score = new Dictionary<Path_Node, float>();
        static Dictionary<Path_Node, float> f_score = new Dictionary<Path_Node, float>();
        static List<Path_Node> ClosedSet = new List<Path_Node>();
        static SimplePriorityQueue<Path_Node> OpenSet = new SimplePriorityQueue<Path_Node>();

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
            if (startCoord == null || endCoord == null)
            {
                return;
            }

            Came_From.Clear();
            g_score.Clear();
            f_score.Clear();
            ClosedSet.Clear();
            OpenSet.Clear();

            start = startCoord;
            end = endCoord;

            if (World.tileMap.tileGraph == null)
            {
                World.tileMap.tileGraph = new Path_TileGraph(World.tileMap.CurrentMap);
            }

            Path_TileData startTileData = World.tileMap.CurrentMap.GetTileData(startCoord.x, startCoord.y);
            Path_TileData endTileData = World.tileMap.CurrentMap.GetTileData(endCoord.x, endCoord.y);

            if (startTileData == null || endTileData == null)
            {
                steps = null;
                result = PathResult.Fail;
                return;
            }

            if (!Calculate(World.tileMap.tileGraph.nodes, startTileData, endTileData, ignoreCosts, entity))
            {
                steps = null;
                result = PathResult.Fail;
                return;
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

            Came_From.Clear();
            g_score.Clear();
            f_score.Clear();
            ClosedSet.Clear();
            OpenSet.Clear();

            Path_TileData start = gr.GetPathDataAt(startCoord.x, startCoord.y), end = gr.GetPathDataAt(endCoord.x, endCoord.y);

            if (!Calculate(World.worldMap.tileGraph.nodes, start, end, true, ObjectManager.playerEntity))
            {
                steps = null;
                result = PathResult.Fail;
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
                f_score[n] = Mathf.Infinity;
            }

            g_score[nodes[start]] = 0;
            f_score[nodes[start]] = Distance(nodes[start], nodes[end]);


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

                    //Skip doors for NPCs.
                    if (ClosedSet.Contains(neighbor) || !neighbor.data.walkable || !entity.isPlayer && neighbor.data.costToEnter >= 100)
                    {
                        continue;
                    }

                    const float costToEnterFactor = 0.8f;
                    float tentative_g_score = g_score[current] + Distance(current, neighbor) + neighbor.data.costToEnter * costToEnterFactor;

                    if (ignoreCosts && neighbor.data.costToEnter < 100)
                    {
                        tentative_g_score -= neighbor.data.costToEnter * costToEnterFactor;
                    }
                    
                    if (OpenSet.Contains(neighbor) && tentative_g_score > g_score[neighbor])
                    {
                        continue;
                    }

                    Came_From[neighbor] = current;
                    g_score[neighbor] = tentative_g_score;
                    f_score[neighbor] = g_score[neighbor] + Distance(neighbor, nodes[end]);

                    if (!OpenSet.Contains(neighbor))
                    {
                        OpenSet.Enqueue(neighbor, f_score[neighbor]);
                    }
                }
            }

            return false;
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

        float Distance(Path_Node datA, Path_Node datB)
        {
            Coord a = datA.data.position;
            Coord b = datB.data.position;

            if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1)
            {
                return 1.00f;
            }

            return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.y - b.y, 2));
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

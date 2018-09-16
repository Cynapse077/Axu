using UnityEngine;
using Priority_Queue;
using System.Linq;
using System.Collections.Generic;

public class Path_AStar {

	public Queue<Path_Node> path;

	public Path_AStar(Coord startCoord, Coord endCoord) {
		if (World.tileMap.tileGraph == null)
            World.tileMap.tileGraph = new Path_TileGraph(World.tileMap.CurrentMap);

		if (startCoord == null || endCoord == null)
			return;
		
		Path_TileData start = World.tileMap.CurrentMap.GetTileData(startCoord.x, startCoord.y);
		Path_TileData end = World.tileMap.CurrentMap.GetTileData(endCoord.x, endCoord.y);

        if (start == null) {
            Debug.LogError("Path start null.");
            return;
        }

        if (end == null) {
            Debug.LogError("Path end null");
            return;
        }

		Calculate(World.tileMap.tileGraph.nodes, start, end);
	}

	public Path_AStar(Coord startCoord, Coord endCoord, WorldMap_Data gr) {
		if (World.worldMap.tileGraph == null)
            World.worldMap.tileGraph = new Path_TileGraph(World.worldMap.worldMapData);

		if (startCoord == null || endCoord == null)
			return;

		Path_TileData start = gr.GetPathDataAt(startCoord.x, startCoord.y), end = gr.GetPathDataAt(endCoord.x, endCoord.y);

		Calculate(World.worldMap.tileGraph.nodes, start, end);
	}

	public static List<Coord> GetPath(Coord start, Coord end) {
		Path_AStar p = new Path_AStar(start, end);
		List<Coord> points = new List<Coord>();

		foreach (Path_Node pn in p.path) {
			points.Add(pn.data.position);
		}

		return points;
	}

	void Calculate(Dictionary<Path_TileData, Path_Node> nodes, Path_TileData start, Path_TileData end) {
		List<Path_Node> ClosedSet = new List<Path_Node>();
		SimplePriorityQueue<Path_Node> OpenSet = new SimplePriorityQueue<Path_Node>();

		OpenSet.Enqueue(nodes[start], 0);

		Dictionary<Path_Node, Path_Node> Came_From = new Dictionary<Path_Node, Path_Node>();
		Dictionary<Path_Node, float> g_score = new Dictionary<Path_Node, float>();
        Dictionary<Path_Node, float> f_score = new Dictionary<Path_Node, float>();

        foreach (Path_Node n in nodes.Values) {
			g_score[n] = Mathf.Infinity;
		}

		g_score[nodes[start]] = 0;

		foreach (Path_Node n in nodes.Values) {
			f_score[n] = Mathf.Infinity;
		}

		f_score[nodes[start]] = HeuristicCostEstimate(nodes[start].data.position, nodes[end].data.position);


		while (OpenSet.Count > 0) {
			Path_Node current = OpenSet.Dequeue();

			if (current == nodes[end]) {
				ReconstructPath(Came_From, current);
				return;
			}

			ClosedSet.Add(current);

			foreach (Path_Edge edge_neighbor in current.edges) {
				Path_Node neighbor = edge_neighbor.node;

				if (ClosedSet.Contains(neighbor))
					continue;

				float tentative_g_score = g_score[current] + DistBetween(current, neighbor);

				if (OpenSet.Contains(neighbor) && tentative_g_score >= g_score[neighbor])
					continue;

				Came_From[neighbor] = current;
				g_score[neighbor] = tentative_g_score;
				f_score[neighbor] = g_score[neighbor] + HeuristicCostEstimate(neighbor.data.position, nodes[end].data.position);

				if (!OpenSet.Contains(neighbor))
					OpenSet.Enqueue(neighbor, f_score[neighbor]);
			}
		}
	}


	float DistBetween(Path_Node a, Path_Node b) {
		if (Mathf.Abs(a.data.position.x - b.data.position.x) + Mathf.Abs(a.data.position.y - b.data.position.y) == 1)
			return 1.00f;
		
		if (Mathf.Abs(a.data.position.x - b.data.position.x) == 1 && Mathf.Abs(a.data.position.y - b.data.position.y) == 1)
			return 1.01f;
		
		return Mathf.Sqrt(Mathf.Pow(a.data.position.x - b.data.position.x, 2) + Mathf.Pow(a.data.position.y - b.data.position.y, 2));

	}

	void ReconstructPath(Dictionary<Path_Node, Path_Node> Came_From, Path_Node current) {
		Queue<Path_Node> total_path = new Queue<Path_Node>();
		total_path.Enqueue(current);

		while (Came_From.ContainsKey(current) ) {
			current = Came_From[current];
			total_path.Enqueue(current);
		}

		path = new Queue<Path_Node>(total_path.Reverse());
	}

	float HeuristicCostEstimate( Coord a, Coord b ) {
		return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.y - b.y, 2));
	}

	public Coord GetNextStep() {
		if (path == null || path.Count == 0)
			return new Coord(0, 0);
		
		Coord nextPosition = path.Dequeue().data.position;
		return nextPosition;
	}
}

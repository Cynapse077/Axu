using UnityEngine;
using System.Collections.Generic;

public class Path_TileGraph {

	public Dictionary<Path_TileData, Path_Node> nodes;

	public Path_TileGraph(WorldMap_Data data) {
		nodes = new Dictionary<Path_TileData, Path_Node>();

		for (int x = 0; x < Manager.worldMapSize.x; x++) {
			for (int y = 0; y < Manager.worldMapSize.y; y++) {
				Path_TileData tileData = data.GetPathDataAt(x, y);
				Path_Node node = new Path_Node();
				node.data = tileData;

				if (tileData != null)
					nodes.Add(tileData, node);
			}
		}

		int edgeCount = 0;

		foreach (Path_TileData t in nodes.Keys) {
			Path_Node n = nodes[t];

			List<Path_Edge> edges = new List<Path_Edge>();

			List<Path_TileData> neighbours = t.GetNeighbours(false, data);

			// If neighbour is walkable, create an edge to the relevant node.
			for (int i = 0; i < neighbours.Count; i++) {
				if (neighbours[i] != null && neighbours[i].walkable) {
					Path_Edge e = new Path_Edge();
					e.node = nodes[neighbours[i]];
					edges.Add(e);

					edgeCount++;
				}
			}

			n.edges = edges.ToArray();
		}
	}

	public Path_TileGraph(TileMap_Data data) {
		nodes = new Dictionary<Path_TileData, Path_Node>();

		for (int x = 0; x < Manager.localMapSize.x; x++) {
			for (int y = 0; y < Manager.localMapSize.y; y++) {
				Path_TileData tileData = data.GetTileData(x, y);
				Path_Node node = new Path_Node();
				node.data = tileData;

				if (tileData != null)
					nodes.Add(tileData, node);
			}
		}

		int edgeCount = 0;

		foreach (Path_TileData t in nodes.Keys) {
			Path_Node n = nodes[t];

			List<Path_Edge> edges = new List<Path_Edge>();

			List<Path_TileData> neighbours = t.GetNeighbours(true, data);

			// If neighbour is walkable, create an edge to the relevant node.
			for (int i = 0; i < neighbours.Count; i++) {
				if (neighbours[i] != null && neighbours[i].walkable) {
					Path_Edge e = new Path_Edge();
					e.node = nodes[neighbours[i]];
					edges.Add(e);

					edgeCount++;
				}
			}

			n.edges = edges.ToArray();
		}
	}
}

public class Path_TileData {
	public bool walkable;
	public Coord position;

	public Path_TileData(bool _walkable, Coord _position) {
		walkable = _walkable;
		position = _position;
	}

	public List<Path_TileData> GetNeighbours(bool diagOkay, TileMap_Data data) {
		List<Path_TileData> ns = new List<Path_TileData>();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if ((x == 0 && y == 0) || (!diagOkay && Mathf.Abs(x) + Mathf.Abs(y) > 1))
					continue;
				
				if (!data.TileDataOutOfMap(position.x + x, position.y + y))
					ns.Add(data.GetTileData(position.x + x, position.y + y));
			}
		}

		return ns;
	}

	public List<Path_TileData> GetNeighbours(bool diagOkay, WorldMap_Data data) {
		List<Path_TileData> ns = new List<Path_TileData>();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if ((x == 0 && y == 0) || (!diagOkay && Mathf.Abs(x) + Mathf.Abs(y) > 1))
					continue;

                if (position.x + x < 0 || position.y + y < 0 || position.x + x >= Manager.worldMapSize.x || position.y + y >= Manager.worldMapSize.y)
                    continue;

				ns.Add(data.GetPathDataAt(position.x + x, position.y + y));
			}
		}

		return ns;
	}
}

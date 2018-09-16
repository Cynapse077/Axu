using UnityEngine;
using System.Collections.Generic;

public class Dungeon {
    ZoneBlueprint_Underground blueprint;
	public int[,] map_data;
    public List<Room> rooms;

	int FloorTile {
		get {
            return blueprint.tileInfo.GetRandomTile().ID;
		}
	}

	int LiquidTile {
		get {
			if (blueprint.id == "Cave_Volcano")
				return Tile.tiles["Lava"].ID;
			else if (blueprint.id == "Cave_Ice")
				return Tile.tiles["Ice"].ID;

            return Tile.tiles["Water"].ID;
		}
	}

    int Unwalkable {
        get { return Tile.GetByName(blueprint.tileInfo.wallTile).ID; }
    }

	System.Random RNG {
		get { return SeedManager.localRandom; }
	}

    public Dungeon(ZoneBlueprint_Underground zb) {
		map_data = new int[Manager.localMapSize.x, Manager.localMapSize.y];
        rooms = new List<Room>();
        blueprint = zb;

		ChooseLayout();	  
	}

	void ChooseLayout() {
        if (blueprint.rules.algorithms == null) {
            RandomLayout();
            return;
        } else {
            string rule = blueprint.rules.algorithms.GetRandom(RNG);

            switch (rule) {
                case "Rooms_Corridors":
                    BasicCave();
                    break;
                case "Rooms_Corridors_Circle":
                    CircleRooms();
                    break;
                case "Corridors":
                    Hallways();
                    break;
                case "CellAuto":
                    Tunneling();
                    break;
                case "Single_Room_Small":
                    Cellar();
                    break;
                case "Building":
                    SubCult();
                    break;
            }

            if (blueprint.rules.layer2.Int > 0 && RNG.Next(100) <= blueprint.rules.layer2.Int) {
                string rule2 = blueprint.rules.layer2.String;

                switch (rule2) {
                    case "Magma_Center":
                        VolcanoUnderground();
                        break;
                    case "River":
                        UndergroundRiver();
                        break;
                }
            }
        }
	}

	void RandomLayout() {
		int ranNum = RNG.Next(100);

		if (ranNum <= 33) {
			Hallways();
		} else if (ranNum <= 66) {
			BasicCave();
		} else if (ranNum <= 99) {
			Tunneling();
		}
	}

	void FillMapWithWalls() {
		for (int x = 0; x < Manager.localMapSize.x; x++) {
			for (int y = 0; y < Manager.localMapSize.y; y++) {
				map_data[x, y] = Unwalkable;
			}
		}
	}

	void SubCult() {
		FillMapWithWalls();

		for (int i = 0; i < 1000; i++) {
			int height = RNG.Next(3, 6), width = RNG.Next(3, 6);
			RectRoom(RNG.Next(3, Manager.localMapSize.y - height / 2 - 3), RNG.Next(3, Manager.localMapSize.x - width / 2 - 3), width, height);
		}

		MakeCorridors(true);
	}

	void Cellar() {
		FillMapWithWalls();
		Coord start = new Coord(RNG.Next(2, Manager.localMapSize.x - 3), RNG.Next(2, Manager.localMapSize.y - 3));
		Room st = new Room(RNG.Next(6, 14), RNG.Next(6, 14));
		st.left = start.x - st.width / 2;
		st.bottom = start.y - st.height / 2;

		for (int rx = st.left; rx < st.right; rx++) {
			for (int ry = st.bottom; ry < st.top; ry++) {
				if (rx > 1 && rx < Manager.localMapSize.x - 2 && ry > 1 && ry < Manager.localMapSize.y - 2)
					map_data[rx, ry] = FloorTile;
			}
		}
	}

	void Hallways() {
		FillMapWithWalls();

		for (int i = 0; i < 90; i++) {
			RectRoom(RNG.Next(2, Manager.localMapSize.y - 3), RNG.Next(2, Manager.localMapSize.x - 3), 1, 1);
		}

		MakeCorridors(true, false);
	}

    void BasicCave() {
        FillMapWithWalls();

        for (int i = 0; i < 500; i++) {
			if (RNG.Next(100) < 80) {
				int radius = RNG.Next(3, 7);
				Coord center = new Coord(RNG.Next(radius + 1, Manager.localMapSize.x - radius - 1), RNG.Next(radius + 1, Manager.localMapSize.y - radius - 1));
				CircleRoom(center, radius);
			} else {
				int height = RNG.Next(4, 8), width = RNG.Next(5, 8);
				RectRoom(RNG.Next(3, Manager.localMapSize.y - height / 2 - 3), RNG.Next(3, Manager.localMapSize.x - width / 2 - 3), width, height);
			}
        }

        MakeCorridors(true);
    }

	void OpenRoom() {
		int centerX = Manager.localMapSize.x / 2, centerY = Manager.localMapSize.y / 2;
		Vector2 center = new Vector2(centerX, centerY);

		for (int x = 0; x < Manager.localMapSize.x; x++) {
			for (int y = 0; y < Manager.localMapSize.y; y++) {
				if (x <= 0 || y <= 0 || x >= Manager.localMapSize.x || y >= Manager.localMapSize.y - 1 || Vector2.Distance(center, new Vector2(x, y)) > 23)
					map_data[x, y] = Unwalkable;
				else
					map_data[x, y] = FloorTile;
			}
		}

		if (RNG.Next(100) < 10)
			Pillars();
	}

	void Tunneling() {
		FillMapWithWalls();

		List<Coord> visited = new List<Coord>();
		Coord start = new Coord(RNG.Next(1, Manager.localMapSize.x - 2), RNG.Next(1, Manager.localMapSize.y - 2));
		visited.Add(start);
		int numFails = 0;
		Coord st = new Coord(start.x, start.y);

		int maxCount = RNG.Next(500, 900);
		int maxFails = RNG.Next(1000, 4000);

		while (numFails < maxFails && visited.Count < maxCount) {
			bool horizontal = RNG.Next(100) < 66;
			int move = (RNG.CoinFlip()) ? 1 : -1;

			if (horizontal)
				st.x += move;
			else
				st.y += move;


			if (st.x <= 0 || st.y <= 0 || st.x >= Manager.localMapSize.x - 1 || st.y >= Manager.localMapSize.y - 1 || visited.Contains(st)) {
				if (st.x <= 0 || st.y <= 0 || st.x >= Manager.localMapSize.x - 1 || st.y >= Manager.localMapSize.y - 1) {
					if (horizontal)
						st.x -= move;
					else
						st.y -= move;

					if (RNG.Next(1000) < 1)
						st = new Coord(start.x, start.y);
				}

				numFails++;
			} else {
				visited.Add(new Coord(st.x, st.y));
			}
		}

		for (int i = 0; i < visited.Count; i++) {
			map_data[visited[i].x, visited[i].y] = FloorTile;
		}
	}

	void CircleRooms() {
		FillMapWithWalls();

		for (int i = 0; i < 1000; i++) {
			int radius = RNG.Next(2, 6);
			Coord center = new Coord(RNG.Next(radius, Manager.localMapSize.x - radius), RNG.Next(radius, Manager.localMapSize.y - radius));
			CircleRoom(center, radius);
		}

		MakeCorridors(true);
	}

	void VolcanoUnderground() {
		int centerX = Manager.localMapSize.x / 2, centerY = Manager.localMapSize.y / 2;
		Vector2 center = new Vector2(centerX, centerY);

		for (int x = 1; x < Manager.localMapSize.x - 1; x++) {
			for (int y = 1; y < Manager.localMapSize.y - 1; y++) {
				if (Vector2.Distance(new Vector2(x, y), center) < RNG.Next(4, 6) && RNG.Next(100) < 94)
					map_data[x, y] = LiquidTile;
			}
		}
	}

	void UndergroundRiver() {
		Coord sPos = new Coord(Manager.localMapSize.x / 2 + RNG.Next(-10, 10), 0);
		Coord ePos = new Coord(Manager.localMapSize.x / 2 + RNG.Next(-10, 10), Manager.localMapSize.y - 1);

		Line l = new Line(sPos, ePos);
		List<Coord> points = l.GetPoints();

		foreach (Coord seg in points) {
			if (RNG.Next(100) < 20)
				seg.x += RNG.Next(-1, 2);

			for (int x = seg.x - 2; x <= seg.x + 2; x++) {
				if (x < 0 || x >= Manager.localMapSize.x)
					continue;
				
				map_data[x, seg.y] = LiquidTile;
			}
		}
	}

	void Pillars() {
		int iterations = RNG.Next(4, 7);

		for (int x = 0; x < Manager.localMapSize.x; x++) {
			for (int y = 0; y < Manager.localMapSize.y; y++) {
				if (map_data[x, y] != Unwalkable) {
					if (x % iterations == 0 && y % iterations == 0 && RNG.Next(100) < 40)
						map_data[x, y] = Tile.tiles["Pillar"].ID;
				}
			}
		}
	}

	bool RectRoom(int bottom, int left, int width, int height) {
        Room e = new Room(width, height, left, bottom);

        for (int r = 0; r < rooms.Count; r++) {
			if (e.CollidesWith(rooms[r]))
				return false;
        }

		bool roundCorners = (RNG.Next(100) <= 40);

        for (int rx = e.left; rx <= e.right; rx++) {
            for (int ry = e.bottom; ry <= e.top; ry++) {
				if (rx <= 0 || ry <= 0 || rx >= Manager.localMapSize.x - 1 || ry >= Manager.localMapSize.y - 1)
					continue;

				if (roundCorners) {
					if (rx == e.left && ry == e.top || rx == e.left && ry == e.bottom || rx == e.right && ry == e.top || rx == e.right && ry == e.bottom)
						continue;
				}

				map_data[rx, ry] = FloorTile;
            }
        }

        rooms.Add(e);

		return true;
    }

	bool CircleRoom(Coord center, int radius) {
		Room e = new Room(radius * 2 - 1, radius * 2 - 1, center.x - radius, center.y - radius);

		for (int r = 0; r < rooms.Count; r++) {
			if (e.OverlapsWith(rooms[r])) 
				return false;
		}

		for (int x = center.x - radius - 1; x <= center.x + radius + 1; x++) {
			for (int y = center.y - radius - 1; y <= center.y + radius + 1; y++) {
				if (x <= 0 || y <= 0 || x >= Manager.localMapSize.x - 1 || y >= Manager.localMapSize.y - 1)
					continue;
				
				if (Vector2.Distance(center.toVector2(), new Vector2(x, y)) <= radius)
					map_data[x, y] = FloorTile;
			}
		}
		rooms.Add(e); 
		return true;
	}

	void MakeCorridors(bool strict = true, bool offset = true) {
		for (int i = 1; i < rooms.Count; i++) {
			Room otherRoom = (strict) ? rooms[i].ClosestNonConnectedRoom(rooms) : rooms[i - 1];

			int x = rooms[i].centerX, y = rooms[i].centerY;
			int x1 = otherRoom.centerX, y1 = otherRoom.centerY;

			if (offset) {
				x += RNG.Next(-1, 2);
				y += RNG.Next(-1, 2);
				x1 += RNG.Next(-1, 2);
				y1 += RNG.Next(-1, 2);
			}

			int xOffset = 0, yOffset = 0;

			if (x != x1)
				xOffset = (x < x1) ? 1 : -1;
			if (y != y1)
				yOffset = (y < y1) ? 1 : -1;

			if (RNG.CoinFlip()) {
				while (x != x1) {
					if (OutOfBounds(x, y))
						break;
					
					map_data[x, y] = FloorTile;
					x += xOffset;
				}
				while (y != y1) {
					if (OutOfBounds(x, y))
						break;
					
					map_data[x, y] = FloorTile;
					y += yOffset;
				}
			} else {
				while (y != y1) {
					if (OutOfBounds(x, y))
						break;
					
					map_data[x, y] = FloorTile;
					y += yOffset;
				}
				while (x != x1) {
					if (OutOfBounds(x, y))
						break;
					
					map_data[x, y] = FloorTile;
					x += xOffset;
				}
			}
		}
	}

    bool OutOfBounds(int x, int y) {
		return (x < 0 || x >= Manager.localMapSize.x || y < 0 || y >= Manager.localMapSize.y);
    }

	public int GetTileAt(int x, int y) {
		return map_data[x, y];
    }
}

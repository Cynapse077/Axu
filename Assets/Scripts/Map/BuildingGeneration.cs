using UnityEngine;
using System.Collections.Generic;

public class BuildingGeneration {

	int width;
	int height;
	int[,] map;
	List<Room> rooms;
	int min = 6;

	public BuildingGeneration() {}

	public void Init(int w, int h) {
		width = w;
		height = h;
		map = new int[width, height];
		rooms = new List<Room>();
		GenerateMap();
	}

	public int[,] GetMap() {
		Init(Manager.localMapSize.x, Manager.localMapSize.y);
		return map;
	} 

	void GenerateMap() {
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				map[x, y] = 0;
			}
		}

		rooms.Add(new Room(width, height, 0, 0));

		SplitRoom(rooms[0], true, true);

		Room left = rooms[0], right = rooms[1];
		int numSplits = SeedManager.localRandom.Next(10, 14);

		if (SeedManager.localRandom.CoinFlip()) {
			SplitRoom(left, true, true);
			SplitRoom(right, true, true);
		} else
			numSplits += 2;

		for (int i = 0; i < numSplits; i++) {
			if (rooms.FindAll(x => x.width > min || x.height > min).Count == 0)
				break;
			
			Room r = rooms.FindAll(x => x.width > min || x.height > min).GetRandom(SeedManager.localRandom);
			SplitRoom(r, (SeedManager.localRandom.CoinFlip()));
		}
		BorderMap();
		ConnectRooms();
	}

	void SplitRoom(Room r, bool vertical, bool centre = false) {
		if (r.width <= min || r.height <= min)
			return;
		
		int	splitPoint = (vertical) ? r.centerX - 1 : r.centerY - 1;
				
		for (int x = r.right; x < r.left; x++) {
			for (int y = r.bottom; y < r.top; y++) {
				if (vertical) {
					if (x == splitPoint)
						map[x, y] = 1;
				} else {
					if (y == splitPoint)
						map[x, y] = 1;
				}
			}
		}
			
		int w = (vertical) ? (int)((float)r.right - (float)splitPoint - 0.5f) : r.width;
		int h = (vertical) ? r.height : (int)((float)r.top - (float)splitPoint - 0.5f);
		int l2 = (vertical) ? splitPoint : r.left;
		int b2 = (vertical) ? r.bottom : splitPoint;

		Room r1 = new Room(w, h, r.left, r.bottom), r2 = new Room(w, h, l2, b2);
		BorderRoom(r1);
		BorderRoom(r2);

		rooms.Add(r1);
		rooms.Add(r2);
		rooms.Remove(r);
	}

	void BorderRoom(Room r) {
		bool corners = (SeedManager.localRandom.Next(100) < 30);

		for (int x = r.left; x < r.right; x++) {
			for (int y = r.bottom; y < r.top; y++) {
				if (x < 0 || x >= width || y < 0 || y >= height)
					continue;
				
				if (x == r.left || x == r.right - 1 || y == r.bottom || y == r.top - 1)
					map[x, y] = 1;
				else 
					map[x, y] = 0;

				if (corners) {
					if (x == r.left + 1 && y == r.top - 2)
						map[x, y] = 1;
					if (x == r.left + 1 && y == r.bottom + 1)
						map[x, y] = 1;

					if (x == r.right - 2 && y == r.top - 2)
						map[x, y] = 1;
					if (x == r.right - 2 && y == r.bottom + 1)
						map[x, y] = 1;
				}
			}
		}
	}

	void BorderMap() {
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
					map[x, y] = 1;
			}
		}
	}

	void ConnectRooms() {
		for (int i = 0; i < 2; i++) {
			for (int r = 0; r < rooms.Count; r++) {
				List<Room> rms = rooms.FindAll(x => !x.isConnected && x.centerX != rooms[r].centerX && x.centerY != rooms[r].centerY);

				if (rms.Count == 0)
					continue;
				
				if (r > 0)
					ConnectRoom(rooms[r], rooms[r - 1]);
			}
		}
	}

	void ConnectRoom(Room r1, Room r2) {
		int x = r1.centerX, y = r1.centerY;
		int x1 = r2.centerX, y1 = r2.centerY;
		r2.isConnected = true;

		while (y != y1) {
			int yOffset = 0;

			if (y != y1)
				yOffset = (y < y1) ? 1 : -1;
			
			if (y < 0 || y >= height)
				break;
			
			int num = map[x, y];
			map[x, y] = (num == 1) ? 2 : 0;
			y += yOffset;
		}

		while (x != x1) {
			int xOffset = 0;

			if (x != x1)
				xOffset = (x < x1) ? 1 : -1;
			
			if (x < 0 || x >= width)
				break;
			
			int num = map[x, y];
			map[x, y] = (num == 1) ? 2 : 0;
			x += xOffset;
		}
	}

	struct Room {
		public int width;
		public int height;
		public int left; 
		public int bottom;
		public bool isConnected;

		public int right {
			get { return left + width; }
		}
		public int top {
			get { return bottom + height; }
		}
		public int centerX {
			get { return left + width / 2; }
		}
		public int centerY {
			get { return bottom + height / 2; }
		}

		public Room(int w, int h, int l, int b) {
			width = w;
			height = h;
			left = l;
			bottom = b;
			isConnected = false;
		}
	}
}
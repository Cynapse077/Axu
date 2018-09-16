using UnityEngine;
using System.IO;
using System.Collections.Generic;
using LitJson;

[System.Serializable]
public class WorldMap_Data {
    public static string ZonePath;

	public Coord startPosition;
	public List<Coord> ruinsPos = new List<Coord>(), vaultAreas = new List<Coord>();
	public List<Landmark> landmarks = new List<Landmark>();
	public List<Village_Data> villages = new List<Village_Data>();
	public bool doneLoading = false;
	public Path_TileData[,] tileData { get; set; }

    MapInfo[,] tiles;
	JsonData worldTileData;
    List<Coord> mountains, ocean;
    Dictionary<string, ZoneBlueprint> zoneBlueprints;
    Dictionary<string, ZoneBlueprint_Underground> ugZoneBlueprints;

    const int riversMin = 6, riversMax = 14;

	System.Random rng {
		get { return SeedManager.worldRandom; }
	}

    int width {
        get { return Manager.worldMapSize.x; }
    }

    int height {
        get { return Manager.worldMapSize.y; }
    }

    //Generating a new world
    public WorldMap_Data(bool newGame) {
		SeedManager.SetSeedFromName(Manager.worldSeed);
        tiles = new MapInfo[width, height];

        mountains = new List<Coord>();
        ocean = new List<Coord>();

		if (!newGame)
			new OldWorld();
		
		GenerateTerrain();
	}

	void GenerateTerrain() {
		int[,] values = Generate_WorldMap.Generate(rng, width, height, 4, 8f, 20f);

		for (int x = 0; x < values.GetLength(0); x++) {
			for (int y = 0; y < values.GetLength(1); y++) {
				float height = 0.4f;
                
				switch (values[x, y]) {
				case 0: height = 0.1f;
					break;
				case 5:
					height = 0.22f;
					break;
				case 1: height = 0.4f;
					break;
				case 2:
					height = 0.6f;
					break;
				case 3:
					height = 0.8f;
					break;
				case 4:
					height = 0.99f;
					break;

				}

				SetBiome(height, x, y);
			}
		}

		GenerateHeat();
	}

	float DistanceToPoint_Biased(int x1, int y1, float x, float y) {
		float distanceX = (x1 - x) * (x1 - x), distanceY = (y1 - y) * (y1 - y);
		float biasX = 0.5f;
		float biasY = 1.1f;

		return Mathf.Sqrt(distanceX * biasX + distanceY * biasY) / (height / 2 - 1);
	}

	void SetBiome(float biome, int x, int y) {
        Coord c = new Coord(x, y);

		if (biome <= 0.16f) { //Ocean
            tiles[x, y] = new MapInfo(c, WorldMap.Biome.Ocean);
            ocean.Add(c);

		} else if (biome <= 0.19f) { //Shore
            tiles[x, y] = new MapInfo(c, WorldMap.Biome.Shore);

        } else if (biome <= 0.42f) { //Plains
            tiles[x, y] = new MapInfo(c, (rng.Next(0, 101) < 95) ? WorldMap.Biome.Plains : WorldMap.Biome.Forest);

		} else if (biome <= 0.63f) { //Forest
            tiles[x, y] = new MapInfo(c, (rng.Next(0, 101) < 95) ? WorldMap.Biome.Forest : WorldMap.Biome.Plains);

        } else { //Mountains
            tiles[x, y] = new MapInfo(c, WorldMap.Biome.Mountain);
            mountains.Add(c);
		}
	}

    void GenerateHeat() {
		float scale = 6;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (tiles[x, y].biome == WorldMap.Biome.Mountain || tiles[x, y].biome == WorldMap.Biome.Ocean)
                    continue;

                float yOrg = rng.Next(-1000, 1000), yCoord = yOrg + y / height * (scale / 1.5f);
                float perlin = Mathf.PerlinNoise(x * 3, yCoord);
                int centerY = height / 2 - 1;
                float distToCenter = DistanceToPoint_Biased(width / 2 - 1, centerY, x, y);
                float val = (perlin + distToCenter);

                if (Mathf.Abs(val) > 0.9f) {
                    if (y > centerY + 15)
                        tiles[x, y].biome = WorldMap.Biome.Tundra;
                    else if (y < centerY - 15)
                        tiles[x, y].biome = WorldMap.Biome.Desert;
                }
            }
        }
			
		for (int i = 0; i < 4; i++) {
			for (int hx = 1; hx < width - 1; hx++) {
				for (int hy = 1; hy < height - 1; hy++) {
					if (tiles[hx, hy].biome == WorldMap.Biome.Tundra)
						SmoothBiome(hx, hy, WorldMap.Biome.Tundra);
					else if (tiles[hx, hy].biome == WorldMap.Biome.Desert)
                        SmoothBiome(hx, hy, WorldMap.Biome.Desert);
				}
			}	
		}

        for (int i = 0; i < 2; i++) {
            RemoveIsolatedTiles();
        }

		SurroundWaterWithShore();

        if (mountains.Count > 0) {
            //Place rivers
            int numRivers = 0, numTries = 0, maxRivers = rng.Next(riversMin, riversMax + 1), maxTries = 10000;

            while (numRivers < maxRivers && numTries < maxTries) {
                if (PlaceRiver(mountains.GetRandom(rng)))
                    numRivers++;
                else
                    numTries++;
            }
        }
        

        SetupPathfindingGrid();
        FinalPass();
    }

	void SmoothBiome(int x, int y, WorldMap.Biome b) {
		int neighbors = 0;

		for (int ex = -1; ex <= 1; ex++) {
			for (int ey = -1; ey <= 1; ey++) {
				if (Mathf.Abs(ex) + Mathf.Abs(ey) > 1 || ex == 0 && ey == 0)
					continue;

				if (tiles[x, y].biome == b || tiles[x, y].biome == WorldMap.Biome.Ocean)
					neighbors ++;
			}
		}

        if (neighbors <= 2 && rng.Next(100) < 65 || neighbors == 0)
            tiles[x, y].biome = WorldMap.Biome.Plains;
	}

    bool GrassTile(int x, int y) {
        return tiles[x, y].biome == WorldMap.Biome.Plains || tiles[x, y].biome == WorldMap.Biome.Forest || 
            (tiles[x, y].biome == WorldMap.Biome.Tundra && rng.Next(100) < 20) || (tiles[x, y].biome == WorldMap.Biome.Desert && rng.Next(100) < 20);
    }

	void SurroundWaterWithShore() {
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {

				//Place down shore tiles along edges of grass.
				if (GrassTile(x, y) && tiles[x, y].biome != WorldMap.Biome.Tundra && tiles[x, y].biome != WorldMap.Biome.Desert) {
					for (int ex = -1; ex <= 1; ex++) {
						for (int ey = -1; ey <= 1; ey++) {
							if (x == 0 && y == 0 || x + ex >= Manager.worldMapSize.x || x + ex < 0 || y + ey >= Manager.worldMapSize.x || y + ey < 0) 
								continue;

                            if (tiles[x + ex, y + ey].biome == WorldMap.Biome.Ocean)
                                tiles[x, y].biome = WorldMap.Biome.Shore;
						}
					}
				}
			}
		}
	}

    bool PlaceRiver(Coord startTile) {
		Coord endTile = ocean.GetRandom(rng);
        int numTries = 0, maxTries = 10000;

        while (Vector2.Distance(startTile.toVector2(), endTile.toVector2()) > 10 && numTries < maxTries) {
            endTile = ocean.GetRandom(rng);
            numTries++;
        }

        if (numTries >= maxTries)
            return false;

        int dx = (startTile.x > endTile.x) ? -1 : 1, dy = (startTile.y > endTile.y) ? -1 : 1;

        if (startTile.x == endTile.x)
            dx = 0;
        if (startTile.y == endTile.y)
            dy = 0;

        List<Coord> riverTiles = new List<Coord>();

        int nx = startTile.x;
        int ny = startTile.y;
        bool breakOut = false;

        while (tiles[nx, ny].biome != WorldMap.Biome.Ocean) {
			if (nx < 0 || ny < 0 || nx >= Manager.worldMapSize.x || ny >= Manager.worldMapSize.y)
				return false;
            if (breakOut)
                break;

            //Check adjacent oceans to avoid strangeness in autotiling.
            //Might want to use a better system...
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    if (Mathf.Abs(x) + Mathf.Abs(y) >= 2 || x == 0 && y == 0)
                        continue;
                    if (nx + x < 0 || ny + y < 0 || nx + x >= Manager.worldMapSize.x || ny + y >= Manager.worldMapSize.y)
                        continue;

                    if (tiles[nx + x, ny + y].biome == WorldMap.Biome.Ocean)
                        breakOut = true;
                }
            }

            Coord newRiver = new Coord(nx, ny);
            riverTiles.Add(newRiver);

			if (rng.Next(100) < 50) 
				nx += dx;
            else 
				ny += dy;                
        }

        if (!CanPlaceRiver(riverTiles))
            return false;

        for (int i = 0; i < riverTiles.Count; i++) {
            tiles[riverTiles[i].x, riverTiles[i].y].landmark = "River";
        }

        return true;
    }

    bool CanPlaceRiver(List<Coord> riverTiles) {
        for (int i = 1; i < riverTiles.Count; i++) {
            if (tiles[riverTiles[i].x, riverTiles[i].y].HasLandmark() || tiles[riverTiles[i].x, riverTiles[i].y].biome == WorldMap.Biome.Mountain)
                return false;
        }

        return true;
    }

    void FinalPass() {
		for (int s = 0; s < rng.Next(12, 19); s++) {
			Swamp();
		}

        PlaceZones();
        doneLoading = true;
    }

    void PlaceZones() {
        zoneBlueprints = new Dictionary<string, ZoneBlueprint>();
        ugZoneBlueprints = new Dictionary<string, ZoneBlueprint_Underground>();

        string path = Application.streamingAssetsPath + ZonePath;
        string listFromJson = File.ReadAllText(path);

        JsonData dat = JsonMapper.ToObject(listFromJson);

        for (int i = 0; i < dat["Underground Areas"].Count; i++) {
            ugZoneBlueprints.Add(dat["Underground Areas"][i]["ID"].ToString(), ZoneBlueprint_Underground.LoadFromJson(dat["Underground Areas"][i]));
        }

        for (int i = 0; i < dat["Locations"].Count; i++) {
            zoneBlueprints.Add(dat["Locations"][i]["ID"].ToString(), ZoneBlueprint.LoadFromJson(dat["Locations"][i]));
        }

        foreach (ZoneBlueprint z in zoneBlueprints.Values) {
            for (int i = 0; i < z.amount; i++) {
                PlaceZone(z);
            }
        }
    }

    void PlaceZone(ZoneBlueprint zb, Coord parentPos = null) {
        Coord pos = null;
        bool noParent = (parentPos == null && zb.placement.relativePosition == null);

        if (noParent) {
            if (!string.IsNullOrEmpty(zb.placement.landmark)) {
                pos = GetRandomLandmark(zb.placement.landmark);
            } else {
                if (zb.placement.zoneID == "Any Land") {
                    pos = (zb.placement.distFromStart != 0) ? GetOpenPosition(zb.placement.distFromStart) : GetOpenPosition();
                } else {
                    WorldMap.Biome b = zb.placement.zoneID.ToEnum<WorldMap.Biome>();
                    pos = GetOpenPosition(new List<WorldMap.Biome> { b });
                }
            }
        } else {
            pos = parentPos + zb.placement.relativePosition;
        }

        if (pos == null || pos.x < 0 || pos.y < 0 || pos.x >= width || pos.y >= height)
            return;
        
        if (tiles[pos.x, pos.y].biome == WorldMap.Biome.Mountain)
            tiles[pos.x, pos.y].biome = WorldMap.Biome.Shore;

        tiles[pos.x, pos.y].landmark = zb.id;
        tiles[pos.x, pos.y].friendly = zb.friendly;
        tileData[pos.x, pos.y].walkable = zb.walkable;

        if (!string.IsNullOrEmpty(zb.underground))
            vaultAreas.Add(new Coord(pos.x, pos.y));

        if (zb.isStart)
            startPosition = pos;

        if (zb.neighbors != null) {
            for (int i = 0; i < zb.neighbors.Length; i++) {
                PlaceZone(zb.neighbors[i], pos);
            }
        }

        if (zb.id == "Village") {
            Village_Data vd = new Village_Data(pos, NameGenerator.CityName(rng), pos);
            villages.Add(vd);

            if (zb.expand)
                ExpandVillage(vd);
        }

        landmarks.Add(new Landmark(pos, (zb.id == "Village" ? "Village of " + zb.name : zb.name)));
    }

    void RemoveIsolatedTiles() {
        for (int x = 1; x < width - 1; x++) {
            for (int y = 1; y < height - 1; y++) {
				if (tiles[x, y].biome != WorldMap.Biome.Ocean && !tiles[x, y].HasLandmark()) {
                    int waterNeighbours = 0;

                    for (int ex = -1; ex <= 1; ex++) {
                        for (int ey = -1; ey <= 1; ey++) {
                            if (ex == 0 && ey == 0)
                                continue;
							
                            if (tiles[x + ex, y + ey].biome == WorldMap.Biome.Ocean)
                                waterNeighbours++;
                        }
                    }

                    if (waterNeighbours > 5)
                        tiles[x, y].biome = WorldMap.Biome.Ocean;
                }
            }
        }
    }

	void ExpandVillage(Village_Data vData) {
        float ran = rng.Next(100);
        int xMax = (ran < 50) ? 2 : 1, yMax = (ran <= 50) ? 2 : 1;
        List<Coord> possibleLocations = new List<Coord>();

        for (int x = -xMax; x <= xMax; x++) {
            for (int y = -yMax; y <= yMax; y++) {
                if (x == 0 && y == 0)
                    continue;

                int vx = vData.center.x + x, vy = vData.center.y + y;
                if (tiles[vx, vy].biome != WorldMap.Biome.Ocean && tiles[vx, vy].biome != WorldMap.Biome.Mountain && !tiles[vx, vy].HasLandmark())
                    possibleLocations.Add(new Coord(vx, vy));
            }
        }

        for (int i = 0; i < rng.Next(4, 7); i++) {
            if (possibleLocations.Count <= 0)
                break;

            Coord v = possibleLocations.GetRandom(rng);
            possibleLocations.Remove(v);
            Village_Data vd = new Village_Data(v, vData.name, vData.MapPosition);
            tiles[v.x, v.y].landmark = "Village";
            tiles[v.x, v.y].friendly = true;
            villages.Add(vd);
        }
    }

	public Coord GetOpenPosition(float maxDistance = 200f) {
        List<Coord> openPositions = new List<Coord>();

        for (int x = 1; x < width - 1; x++) {
            for (int y = 1; y < height - 1; y++) {
                if (GrassTile(x, y) && !tiles[x, y].HasLandmark()) {
                    if (maxDistance < 200 && GetLandmark("Abandoned Building").DistanceTo(new Coord(x, y)) <= maxDistance)
                        openPositions.Add(new Coord(x, y));
                    else
                        openPositions.Add(new Coord(x, y));
                }
            }
        }

        if (openPositions.Count <= 0 && maxDistance < 190)
            return GetOpenPosition();

		return openPositions.GetRandom(rng);
	}

    public Coord GetOpenPosition(Coord start, int maxDistance) {
        List<Coord> openPositions = new List<Coord>();

        for (int x = 1; x < width - 1; x++) {
            for (int y = 1; y < height - 1; y++) {
                if (GrassTile(x, y) && !tiles[x, y].HasLandmark()) {
                    if (start.DistanceTo(new Coord(x, y)) <= maxDistance)
                        openPositions.Add(new Coord(x, y));
                }
            }
        }

        if (openPositions.Count <= 0 && maxDistance < 190)
            return GetOpenPosition();

        return openPositions.GetRandom(rng);
    }

    public Coord GetOpenPos_Conditional(System.Predicate<MapInfo> p) {
        List<Coord> openPositions = new List<Coord>();

        for (int x = 1; x < width - 1; x++) {
            for (int y = 1; y < height - 1; y++) {
                if (GrassTile(x, y) && !tiles[x, y].HasLandmark() && p(tiles[x, y])) {
                    openPositions.Add(new Coord(x, y));
                }
            }
        }

        return openPositions.GetRandom(rng);
    }

    public Coord GetLandmark(string search) {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (tiles[x, y].landmark == search)
                    return new Coord(x, y);
            }
        }

        return null;
    }

    public Coord GetRandomLandmark(string search) {
        List<Coord> cs = new List<Coord>();

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (tiles[x, y].landmark == search)
                    cs.Add(new Coord(x, y));
            }
        }

        return cs[rng.Next(0, cs.Count)];
    }

	public Coord GetOpenPosition(List<WorldMap.Biome> types) {
		List<Coord> c = new List<Coord>();

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if (types.Contains(tiles[x, y].biome) && !tiles[x, y].HasLandmark()) {
					c.Add(new Coord(x, y));
				}
			}
		}

		return c.GetRandom(rng);
	}

	public Coord GetClosestBiome(WorldMap.Biome b) {
		Coord closest = new Coord(1000, 1000);
		float dist = Mathf.Infinity;

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				Coord c = new Coord(x, y);

				if (tiles[x, y].biome == b) {
					float d =  c.DistanceTo(World.tileMap.WorldPosition);

					if (d < dist) {
						closest = c;
						dist = d;
					}
				}
			}
		}

		return closest;
	}

    public Coord GetClosestLandmark(string land) {
        Coord closest = new Coord(1000, 1000);
        float dist = Mathf.Infinity;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Coord c = new Coord(x, y);

                if (tiles[x, y].landmark == land) {
                    float d = c.DistanceTo(World.tileMap.WorldPosition);

                    if (d < dist) {
                        closest = c;
                        dist = d;
                    }
                }
            }
        }

        return closest;
    }

    public Coord GetRandomFromBiome(WorldMap.Biome b) {
		List<Coord> coords = new List<Coord>();

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
                if (tiles[x, y].biome == b)
					coords.Add(new Coord(x, y));
			}
		}

		return coords.GetRandom(rng);
	}

	void Swamp() {
		Coord start = GetOpenPosition();
        tiles[start.x, start.y].biome = WorldMap.Biome.Swamp;
		int width = rng.Next(3, 8), height = rng.Next(3, 8);
		int left = start.x, bottom = start.y;
		
		Room r = new Room(width, height, left, bottom);
		bool hasVillage = false;
		
		for (int x = left; x < r.right; x++) {
			for (int y = bottom; y < r.top; y++) {
				if (x <= 0 || x >= Manager.worldMapSize.x - 1 || y <= 0 || y >= Manager.worldMapSize.y - 1)
					continue;
				if ((x == left || x == r.right - 1 || y == bottom || y == r.top - 1) && rng.Next(0, 101) < 60) {
					continue;
				} else {
                    WorldMap.Biome b = tiles[x, y].biome;

					if (b != WorldMap.Biome.Ocean && b != WorldMap.Biome.Shore && b != WorldMap.Biome.Mountain) {
                        tiles[x, y].biome = WorldMap.Biome.Swamp;
						if (rng.Next(100) < 3 && !hasVillage) {
                            tiles[x, y].landmark = "Swamp Village";
							hasVillage = true;
						}
					}
				}
			}
		}
	}

	public MapInfo GetTileAt(int x, int y) { 
		if (x < 0 || x >= width || y < 0 || y >= height)
			return new MapInfo(new Coord(x, y), WorldMap.Biome.Ocean);

		return tiles[x, y]; 
	}

	public Village_Data GetVillageAt(int x, int y) {
		return (villages.Find(v => v.MapPosition.x == x && v.MapPosition.y == y));
	}

	void SetupPathfindingGrid() {
		tileData = new Path_TileData[width, height];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				tileData[x,y] = new Path_TileData(tiles[x, y].Walkable(), new Coord(x, y));
			}
		}
	}

	public Path_TileData GetPathDataAt(int x, int y) {
		if (x >= width || y >= height || x < 0 || y < 0)
			return null;

		if (tileData[x, y] == null)
			tileData[x, y] = new Path_TileData(tiles[x, y].Walkable(), new Coord(x, y));

		return tileData[x, y];
	}

    public ZoneBlueprint GetZone(string search) {
        if (zoneBlueprints.ContainsKey(search)) {
            return zoneBlueprints[search];
        } else {
            foreach (ZoneBlueprint zb in zoneBlueprints.Values) {
                if (zb.neighbors != null) {
                    for (int i = 0; i < zb.neighbors.Length; i++) {
                        if (zb.neighbors[i].id == search)
                            return zb.neighbors[i];
                    }
                }
            }
        }

        Debug.LogError("No ZoneBlueprint with the ID \"" + search + "\".");
        return null;
    }

    public ZoneBlueprint_Underground GetUndergroundFromLandmark(string search) {
        ZoneBlueprint zb = GetZone(search);

        if (zb != null) {
            if (ugZoneBlueprints.ContainsKey(zb.underground))
                return ugZoneBlueprints[zb.underground];
            else
                Debug.LogError("Underground area \"" + search +  "\" does not exist.");
        }

        
        return null;
    }

    public ZoneBlueprint_Underground GetUnderground(string search) {
        if (ugZoneBlueprints.ContainsKey(search))
            return ugZoneBlueprints[search];

        Debug.LogError("No ZoneBlueprint_Underground with the ID \"" + search + "\".");
        return null;
    }

    public string GetZoneNameAt(int x, int y, int ele) {
        if (ele != 0)
            return World.tileMap.GetCurrentVault(new Coord(x, y)).blueprint.name;

        MapInfo mi = tiles[x, y];

        if (mi.HasLandmark()) {
            if (mi.landmark == "River") {
                return mi.biome.ToString();
            } else if (mi.landmark == "Village") {
                string s = LocalizationManager.GetContent("loc_village");

                if (s.Contains("[NAME]"))
                    s = s.Replace("[NAME]", GetVillageAt(x, y).name);

                return s;
            } else {
                return GetZone(mi.landmark).name;
            }
        }

        string id = LocalizationManager.GetContent("Biome_" + mi.biome.ToString());
        return id; 
    }

    struct Island {
        public int id;
        public List<Coord> positions;
    }
}

public struct MapInfo {
    public WorldMap.Biome biome;
    public string landmark;
    public Coord position;
    public bool friendly;

    public MapInfo(Coord pos, WorldMap.Biome b) {
        position = pos;
        biome = b;
        landmark = "";
        friendly = false;
    }

    public bool HasLandmark() {
        return (!string.IsNullOrEmpty(landmark));
    }

    public bool Walkable() {
        return biome != WorldMap.Biome.Mountain;
    }

    public static bool BiomeHasEdge(WorldMap.Biome b) {
        if (b == WorldMap.Biome.Mountain || b == WorldMap.Biome.Tundra || b == WorldMap.Biome.Ocean || b == WorldMap.Biome.Desert)
            return true;

        return false;
    }
}

public struct Landmark {
    public Coord position;
    public string description;

    public Landmark(Coord pos, string desc = "") {
        position = pos;
        description = desc;
    }
}
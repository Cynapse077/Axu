using System;
using System.Collections.Generic;

public static class TileMap_Generator
{

    public static Tile_Data[,] Generate(WorldMap.Biome biome)
    {
        return CreateFromBiome(biome);
    }

    static Random RNG
    {
        get
        {
            return SeedManager.localRandom;
        }
    }

    static Tile_Data[,] CreateFromBiome(WorldMap.Biome b)
    {
        Tile_Data[,] td = new Tile_Data[Manager.localMapSize.x, Manager.localMapSize.y];
        bool pool = (b == WorldMap.Biome.Swamp || (b == WorldMap.Biome.Forest || b == WorldMap.Biome.Plains) && SeedManager.localRandom.Next(100) < 5);
        string waterType = (b == WorldMap.Biome.Swamp) ? "Water_Swamp" : "Water";

        for (int x = 0; x < Manager.localMapSize.x; x++)
        {
            for (int y = 0; y < Manager.localMapSize.y; y++)
            {
                td[x, y] = (pool && SeedManager.localRandom.Next(100) < 45) ? Tile.tiles[waterType] : TileFromBiome(b);

                if (pool && (x == 0 || y == 0 || x == Manager.localMapSize.x - 1 || y == Manager.localMapSize.y - 1))
                {
                    td[x, y] = TileFromBiome(b);
                }
            }
        }

        if (pool)
        {
            for (int i = 0; i < 7; i++)
            {
                td = CellAutoWater(td, Tile.tiles[waterType]);
            }

            for (int i = 0; i < 2; i++)
            {
                RemoveIsolatedWaterTiles(ref td, Tile.tiles[waterType], b);
            }
        }

        return td;
    }

    public static Tile_Data TileFromBiome(WorldMap.Biome t, bool includeTrees = true)
    {
        switch (t)
        {
            case WorldMap.Biome.Default:
                return Tile.tiles["Default"];
            case WorldMap.Biome.Ocean:
                return Tile.tiles["Water"];
            case WorldMap.Biome.Mountain:
                return Tile.tiles["Mountain"];
            case WorldMap.Biome.Desert:
                return Tile.DesertTile();
            case WorldMap.Biome.Swamp:
                return Tile.SwampTile();
            case WorldMap.Biome.Tundra:
                return Tile.TundraTile();
            case WorldMap.Biome.Plains:
                float r = RNG.Next(100);

                if (SeedManager.localRandom.Next(100) < 2 && includeTrees)
                {
                    return Tile.tiles["Plains_Tree_1"];
                }
                else
                {
                    if (r < 5)
                        return Tile.tiles["Plains_Flower"];
                    else if (r < 95)
                        return (RNG.Next(100) < 50) ? Tile.tiles["Plains_Grass_2"] : Tile.tiles["Plains_Grass_3"];
                    else
                        return Tile.tiles["Plains_Grass_1"];
                }
            case WorldMap.Biome.Forest:
                return (RNG.Next(100) < 2 && includeTrees) ? Tile.tiles["Forest_Tree"] : Tile.ForestTile();
            case WorldMap.Biome.Shore:
                if (RNG.Next(100) < 5)
                {
                    if (RNG.Next(1000) == 0)
                        return Tile.tiles["Shore_Star"];
                    else
                        return (RNG.Next(100) < 50) ? Tile.tiles["Shore_Rock_2"] : Tile.tiles["Shore_Rock"];

                }
                else
                    return Tile.tiles["Shore_Sand"];

            default:
                return Tile.tiles["Plains_Grass_1"];
        }
    }

    public static Tile_Data[,] Dream()
    {
        Tile_Data[,] map_data = new Tile_Data[Manager.localMapSize.x, Manager.localMapSize.y];

        for (int x = 0; x < Manager.localMapSize.x; x++)
        {
            for (int y = 0; y < Manager.localMapSize.y; y++)
            {
                map_data[x, y] = Tile.tiles["Dream_Wall"];
            }
        }

        List<Coord> visited = new List<Coord>();
        Coord start = new Coord(Manager.localMapSize.x / 2, Manager.localMapSize.y / 2);

        visited.Add(start);
        int numFails = 0;
        Coord st = new Coord(start.x, start.y);

        int maxCount = RNG.Next(500, 900);
        int maxFails = RNG.Next(1000, 4000);

        while (numFails < maxFails && visited.Count < maxCount)
        {
            bool horizontal = RNG.Next(100) < 66;
            int move = (RNG.CoinFlip()) ? 1 : -1;

            if (horizontal)
                st.x += move;
            else
                st.y += move;


            if (st.x <= 0 || st.y <= 0 || st.x >= Manager.localMapSize.x - 1 || st.y >= Manager.localMapSize.y - 1 || visited.Contains(st))
            {
                if (st.x <= 0 || st.y <= 0 || st.x >= Manager.localMapSize.x - 1 || st.y >= Manager.localMapSize.y - 1)
                {
                    if (horizontal)
                        st.x -= move;
                    else
                        st.y -= move;

                    if (RNG.Next(1000) < 1)
                        st = new Coord(start.x, start.y);
                }

                numFails++;
            }
            else
            {
                visited.Add(new Coord(st.x, st.y));
            }
        }

        for (int i = 0; i < visited.Count; i++)
        {
            map_data[visited[i].x, visited[i].y] = Tile.tiles["Dream_Floor"];
        }

        return map_data;
    }

    static Tile_Data[,] CellAutoWater(Tile_Data[,] input, Tile_Data water)
    {
        Tile_Data[,] td = new Tile_Data[input.GetLength(0), input.GetLength(1)];

        for (int x = 0; x < input.GetLength(0); x++)
        {
            for (int y = 0; y < input.GetLength(1); y++)
            {
                int notWaterNeighbors = 0;

                for (int ex = -1; ex <= 1; ex++)
                {
                    for (int ey = -1; ey <= 1; ey++)
                    {
                        int curX = x + ex, curY = y + ey;

                        if (OutOfBounds(curX, curY) || input[curX, curY] != water)
                            notWaterNeighbors++;
                    }
                }

                td[x, y] = (notWaterNeighbors >= 4) ? input[x, y] : water;
            }
        }

        return td;
    }

    static void RemoveIsolatedWaterTiles(ref Tile_Data[,] td, Tile_Data water, WorldMap.Biome b)
    {
        for (int x = 0; x < Manager.localMapSize.x; x++)
        {
            for (int y = 0; y < Manager.localMapSize.y; y++)
            {
                if (td[x, y] == water)
                {
                    int waterNeighbors = 0;

                    for (int ex = -1; ex <= 1; ex++)
                    {
                        for (int ey = -1; ey <= 1; ey++)
                        {
                            if (ex == 0 && ey == 0 || Math.Abs(ex) + Math.Abs(ey) >= 2)
                                continue;

                            if (td[x + ex, y + ey] == water)
                                waterNeighbors++;
                        }
                    }

                    if (waterNeighbors <= 0)
                        td[x, y] = TileFromBiome(b);
                }
            }
        }
    }

    static bool OutOfBounds(int x, int y)
    {
        return (x < 0 || y < 0 || x >= Manager.localMapSize.x || y >= Manager.localMapSize.y);
    }
}

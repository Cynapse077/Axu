using System;
using System.Collections.Generic;

public static class TileMap_Generator
{
    public static Tile_Data[,] Generate(Biome biome, bool hasLandmark)
    {
        return CreateFromBiome(biome, hasLandmark);
    }

    static Random RNG
    {
        get { return SeedManager.localRandom; }
    }

    static Tile_Data[,] CreateFromBiome(Biome b, bool hasLandmark)
    {
        Tile_Data[,] td = new Tile_Data[Manager.localMapSize.x, Manager.localMapSize.y];


        for (int x = 0; x < Manager.localMapSize.x; x++)
        {
            for (int y = 0; y < Manager.localMapSize.y; y++)
            {
                td[x, y] = TileFromBiome(b);
            }
        }

        if (b == Biome.Plains && SeedManager.localRandom.Next(100) < 5)
        {
            AdditionalLayer(ref td, b, TileManager.tiles["Shore_Sand"], 30, 6);
        }

        if (b == Biome.Forest && SeedManager.localRandom.Next(100) < 5)
        {
            AdditionalLayer(ref td, b, TileManager.tiles["Plains_Grass_2"], 25, 6);
        }

        if (b == Biome.Swamp || (b == Biome.Forest || b == Biome.Plains) && SeedManager.localRandom.Next(100) < 4)
        {
            if (!hasLandmark)
            {
                string waterType = (b == Biome.Swamp) ? "Water_Swamp" : "Water";
                AdditionalLayer(ref td, b, TileManager.tiles[waterType], 45, 7);
            }
        }

        return td;
    }

    static void AdditionalLayer(ref Tile_Data[,] map, Biome biome, Tile_Data replaceTile, int chance, int iterations)
    {
        for (int x = 1; x < Manager.localMapSize.x - 1; x++)
        {
            for (int y = 1; y < Manager.localMapSize.y - 1; y++)
            {
                if (SeedManager.localRandom.Next(100) < chance)
                {
                    map[x, y] = replaceTile;
                }
            }
        }

        for (int i = 0; i < iterations; i++)
        {
            map = CellAuto(map, replaceTile);
        }
        for (int i = 0; i < 2; i++)
        {
            RemoveIsolatedWaterTiles(ref map, replaceTile, biome);
        }
    }

    public static Tile_Data TileFromBiome(Biome t, bool includeTrees = true)
    {
        int ranNum;

        switch (t)
        {
            case Biome.Default:
                return TileManager.tiles["Default"];

            case Biome.Ocean:
                return TileManager.tiles["Water"];

            case Biome.Mountain:
                return TileManager.tiles["Mountain"];

            case Biome.Desert:
                ranNum = RNG.Next(100);

                if (ranNum == 99 && includeTrees)
                {
                    return TileManager.tiles["Desert_Cactus"];
                }

                if (ranNum < 3)
                    return TileManager.tiles["Desert_Vines"];
                else if (ranNum < 5)
                    return RNG.CoinFlip() ? TileManager.tiles["Desert_Sand_3"] : TileManager.tiles["Desert_Sand_4"];
                else if (ranNum < 6)
                    return TileManager.tiles["Desert_Sand_5"];

                return RNG.CoinFlip() ? TileManager.tiles["Desert_Sand_1"] : TileManager.tiles["Desert_Sand_2"];

            case Biome.Swamp:
                ranNum = RNG.Next(55);

                if (ranNum < 1)
                    return TileManager.tiles["Swamp_Frond"];
                else if (ranNum < 10)
                    return TileManager.tiles["Swamp_Grass_2"];
                else if (ranNum < 20)
                    return TileManager.tiles["Swamp_Grass_3"];
                else if (ranNum < 21)
                    return TileManager.tiles["Swamp_Frond_2"];
                else if (ranNum < 24)
                    return TileManager.tiles["Swamp_Flower"];
                else
                    return TileManager.tiles["Swamp_Grass_1"];

            case Biome.Tundra:
                ranNum = RNG.Next(100);

                if (ranNum > 1 || !includeTrees)
                {
                    if (ranNum == 2)
                    {
                        return TileManager.tiles["Snow_2"];
                    }

                    return (RNG.Next(100) < 96) ? TileManager.tiles["Snow_1"] : TileManager.tiles["Snow_3"];
                }
                else
                    return RNG.CoinFlip() ? TileManager.tiles["Snow_Tree"] : TileManager.tiles["Snow_Grass"];

            case Biome.Plains:
                float r = RNG.Next(100);

                if (SeedManager.localRandom.Next(100) < 2 && includeTrees)
                {
                    return (SeedManager.localRandom.Next(100) < 90) ? TileManager.tiles["Plains_Tree_1"] : TileManager.tiles["Plains_Tree_2"];
                }
                else
                {
                    if (SeedManager.localRandom.Next(200) < 1)
                    {
                        return TileManager.tiles["Plains_Vine"];
                    }

                    if (r < 5)
                        return TileManager.tiles["Plains_Flower"];
                    else if (r < 95)
                        return (RNG.Next(100) < 50) ? TileManager.tiles["Plains_Grass_2"] : TileManager.tiles["Plains_Grass_3"];
                    else
                        return TileManager.tiles["Plains_Grass_1"];
                }

            case Biome.Forest:
                bool isTree = (RNG.Next(100) < 2 && includeTrees);

                if (isTree)
                {
                    return (SeedManager.localRandom.Next(100) < 90) ? TileManager.tiles["Forest_Tree"] : TileManager.tiles["Forest_Tree_Dead"];
                }

                if (SeedManager.localRandom.Next(200) == 0)
                {
                    return TileManager.tiles["Forest_Vine"];
                }

                ranNum = RNG.Next(100);

                if (ranNum < 32)
                    return TileManager.tiles["Forest_Grass_1"];
                else if (ranNum < 48)
                    return TileManager.tiles["Forest_Grass_2"];
                else if (ranNum < 53)
                    return TileManager.tiles["Forest_Grass_3"];
                else if (ranNum < 70)
                    return TileManager.tiles["Forest_Grass_4"];
                else if (ranNum < 83)
                    return TileManager.tiles["Forest_Grass_5"];
                else
                    return TileManager.tiles["Forest_Grass_6"];

            case Biome.Shore:
                if (RNG.Next(100) < 8)
                {
                    if (RNG.OneIn(1000))
                    {
                        return TileManager.tiles["Shore_Star"];
                    }
                    else
                    {
                        return (RNG.CoinFlip()) ? TileManager.tiles["Shore_Rock_2"] : TileManager.tiles["Shore_Rock"];
                    }
                }
                else
                {
                    return TileManager.tiles["Shore_Sand"];
                }

            default:
                return TileManager.tiles["Plains_Grass_1"];
        }
    }

    public static Tile_Data[,] Dream()
    {
        Tile_Data[,] map_data = new Tile_Data[Manager.localMapSize.x, Manager.localMapSize.y];

        for (int x = 0; x < Manager.localMapSize.x; x++)
        {
            for (int y = 0; y < Manager.localMapSize.y; y++)
            {
                map_data[x, y] = TileManager.tiles["Dream_Wall"];
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
            {
                st.x += move;
            }
            else
            {
                st.y += move;
            }

            if (st.x <= 0 || st.y <= 0 || st.x >= Manager.localMapSize.x - 1 || st.y >= Manager.localMapSize.y - 1 || visited.Contains(st))
            {
                if (st.x <= 0 || st.y <= 0 || st.x >= Manager.localMapSize.x - 1 || st.y >= Manager.localMapSize.y - 1)
                {
                    if (horizontal)
                    {
                        st.x -= move;
                    }
                    else
                    {
                        st.y -= move;
                    }

                    if (RNG.Next(1000) < 1 && visited.Count > 0)
                    {
                        st = new Coord(visited.GetRandom());
                    }
                }

                numFails++;
            }
            else
            {
                visited.Add(new Coord(st));
            }
        }

        for (int i = 0; i < visited.Count; i++)
        {
            map_data[visited[i].x, visited[i].y] = TileManager.tiles["Dream_Floor"];
        }

        return map_data;
    }

    static Tile_Data[,] CellAuto(Tile_Data[,] input, Tile_Data replace)
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

                        if (OutOfBounds(curX, curY) || input[curX, curY] != replace)
                        {
                            notWaterNeighbors++;
                        }
                    }
                }

                td[x, y] = (notWaterNeighbors >= 4) ? input[x, y] : replace;
            }
        }

        return td;
    }

    static void RemoveIsolatedWaterTiles(ref Tile_Data[,] td, Tile_Data water, Biome b)
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
                            if (OutOfBounds(x + ex, y + ey) || ex == 0 && ey == 0 || Math.Abs(ex) + Math.Abs(ey) > 1)
                            {
                                continue;
                            }

                            if (td[x + ex, y + ey] == water)
                            {
                                waterNeighbors++;
                            }
                        }
                    }

                    if (waterNeighbors <= 0)
                    {
                        td[x, y] = TileFromBiome(b);
                    }
                }
            }
        }
    }

    static bool OutOfBounds(int x, int y)
    {
        return (x < 0 || y < 0 || x >= Manager.localMapSize.x || y >= Manager.localMapSize.y);
    }
}

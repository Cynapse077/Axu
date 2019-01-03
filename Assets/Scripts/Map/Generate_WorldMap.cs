using UnityEngine;
using System.Collections.Generic;

public class Generate_WorldMap : MonoBehaviour
{
    static int width = 200, height = 200;
    static System.Random rng;
    static int[,] map_data;

    static float outerScale;
    static float innerScale;

    static float stretchX;
    static float stretchY;

    readonly static int islandThreshold = 25;

    public static MapGenInfo Generate(System.Random _rng, int _width, int _height, int layers, float outer, float inner)
    {
        width = _width;
        height = _height;
        map_data = new int[width, height];
        rng = _rng;
        outerScale = outer;
        innerScale = inner;

        int centerX = width / 2 - 1 + rng.Next(-10, 11), centerY = height / 2 - 1 + rng.Next(-10, 11);

        float scale2 = outerScale / 2f;
        float xOrg = GetOriginPoint(), yOrg = GetOriginPoint();
        float xOrg2 = GetOriginPoint(), yOrg2 = GetOriginPoint();
        float xOrg3 = GetOriginPoint(), yOrg3 = GetOriginPoint();

        stretchX = rng.Next(90, 110) * 0.01f;
        stretchY = rng.Next(90, 110) * 0.01f;

        float y = 0f;

        while (y < height)
        {
            float x = 0f;

            while (x < width)
            {
                float b = 0f;

                for (int i = 1; i < layers + 1; i++)
                {
                    float perlin = Mathf.PerlinNoise(xOrg + x / width * outerScale * i, yOrg + y / height * outerScale * i);

                    b += perlin - DistanceToPoint(centerX, centerY, x, y) * 0.55f / (i * 0.5f);
                }

                b *= (Mathf.PerlinNoise(xOrg2 + x / width * scale2, yOrg2 + y / height * scale2)) / (layers - 1);

                map_data[(int)x, (int)y] = GetGeneralBiome(xOrg3, yOrg3, x, y, b);

                x++;
            }

            y++;
        }

        MapGenInfo mi = new MapGenInfo
        {
            biggestIsland = RemoveSmallIslands(ref map_data),
            mapData = map_data
        };

        return mi;
    }

    static float GetOriginPoint()
    {
        return rng.Next(-10000, 10000);
    }

    static List<Coord> RemoveSmallIslands(ref int[,] data)
    {
        List<List<Coord>> islands = new List<List<Coord>>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (data[x, y] != 0)
                {
                    bool canAdd = true;
                    Coord c = new Coord(x, y);

                    foreach (List<Coord> island in islands)
                    {
                        if (island.Contains(c))
                        {
                            canAdd = false;
                            break;
                        }
                    }

                    if (canAdd)
                    {
                        islands.Add(GetIsland(data, c));
                    }
                }
            }
        }

        List<Coord> biggestIsland = null;
        int biggestSize = 0;

        foreach (List<Coord> currIsland in islands)
        {
            int size = currIsland.Count;

            if (size < islandThreshold)
            {
                for (int i = 0; i < currIsland.Count; i++)
                {
                    data[currIsland[i].x, currIsland[i].y] = 0;
                }
            }
            else if (size > biggestSize || biggestIsland == null)
            {
                biggestIsland = currIsland;
                biggestSize = size;
            }
        }

        return biggestIsland;
    }

    static List<Coord> GetIsland(int[,] data, Coord startPos)
    {
        bool[,] processed = new bool[data.GetLength(0), data.GetLength(1)];
        Queue<Coord> myQueue = new Queue<Coord>();
        List<Coord> regionTiles = new List<Coord>();

        Coord stPos = new Coord(startPos);

        myQueue.Enqueue(stPos);
        regionTiles.Add(stPos);
        processed[stPos.x, stPos.y] = true;

        while (myQueue.Count > 0)
        {
            Coord next = myQueue.Dequeue();

            for (int x = next.x - 1; x <= next.x + 1; x++)
            {
                for (int y = next.y - 1; y <= next.y + 1; y++)
                {
                    if (x < 0 || y < 0 || x >= Manager.worldMapSize.x || y >= Manager.worldMapSize.y)
                        continue;

                    if ((x == next.x || y == next.y) && !processed[x, y] && data[x, y] != 0)
                    {
                        Coord c = new Coord(x, y);

                        myQueue.Enqueue(c);
                        regionTiles.Add(c);
                        processed[x, y] = true;
                    }
                }
            }
        }

        return regionTiles;
    }

    static int GetGeneralBiome(float xOrg, float yOrg, float x, float y, float c)
    {
        if (c < 0.1f)
        {
            return 0;
        }
        else
        {
            float perlin = Mathf.PerlinNoise(xOrg + x / width * innerScale * stretchX, yOrg + y / height * innerScale * stretchY) / 1.8f;
            perlin = Mathf.Clamp01(perlin);
            perlin += (c / 1.25f);

            if (perlin > 0.6f)
                return (perlin > 0.8f) ? 4 : 3;
            else if (perlin < 0.15f)
                return (perlin < 0.1f) ? 0 : 5;

            return GetBiome(xOrg * 2, yOrg * 2, x, y, (perlin + c) / 2f);
        }
    }

    static int GetBiome(float xOrg, float yOrg, float x, float y, float c)
    {
        float perlin = Mathf.PerlinNoise(xOrg + x / width * 15, yOrg + y / height * 15) * 0.5f;
        perlin = Mathf.Clamp01(perlin);
        perlin += (c);

        if (perlin < 0.5f)
            return 2;

        return 1;
    }

    static float DistanceToPoint(int x1, int y1, float x, float y)
    {
        float distanceX = (x1 - x) * (x1 - x), distanceY = (y1 - y) * (y1 - y);
        return Mathf.Sqrt(distanceX + distanceY) / (height / 2 - 1);
    }
}

public struct MapGenInfo
{
    public int[,] mapData;
    public List<Coord> biggestIsland;
}
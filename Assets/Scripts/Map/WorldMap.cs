﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[RequireComponent(typeof(MeshFilter))]
[MoonSharp.Interpreter.MoonSharpUserData]
public class WorldMap : MonoBehaviour
{
    public static string BiomePath;
    public static string LandmarkPath;
    const int tileResolution = 16;

    public GameObject landmarkObject;
    public WorldMap_Data worldMapData;
    public Path_TileGraph tileGraph;

    Texture2D terrainTiles, landmarkTiles, texture;
    TilesetManager tsm;
    Dictionary<Coord, GameObject> landmarks;
    Color[][] lmTiles;

    /*void Update() {
        if (Input.GetKeyDown(KeyCode.F2)) {
            string filePath = Application.persistentDataPath + "/Axu Overworld Map.png";
            SaveTextureToFile(texture, filePath);
            CombatLog.NewMessage("<color=green>World map screen saved to " + filePath + ".</color>");
        }
    }

    void SaveTextureToFile(Texture2D texture, string filename) {
        File.WriteAllBytes(filename, texture.EncodeToPNG());
    }*/

    public void Init()
    {
        World.worldMap = this;
        tsm = GetComponent<TilesetManager>();
        landmarks = new Dictionary<Coord, GameObject>();
        texture = new Texture2D(Manager.worldMapSize.x * tileResolution, Manager.worldMapSize.y * tileResolution);

        LoadImageFromStreamingAssets();

        worldMapData = new WorldMap_Data(Manager.newGame);
        StartCoroutine("BuildTexture");
    }

    void LoadImageFromStreamingAssets()
    {
        string path = Application.streamingAssetsPath + BiomePath;
        byte[] imageBytes = File.ReadAllBytes(path);

        Texture2D tex = new Texture2D(0, 0, TextureFormat.ARGB32, false);
        tex.LoadImage(imageBytes);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        terrainTiles = tex;

        path = Application.streamingAssetsPath + LandmarkPath;
        imageBytes = File.ReadAllBytes(path);

        Texture2D newTex = new Texture2D(0, 0, TextureFormat.ARGB32, false);
        newTex.LoadImage(imageBytes);
        newTex.filterMode = FilterMode.Point;
        newTex.wrapMode = TextureWrapMode.Clamp;

        landmarkTiles = newTex;
    }

    IEnumerator BuildTexture()
    {
        Color[][] tiles = ChopUpSpriteSheet(terrainTiles);
        lmTiles = ChopUpSpriteSheet(landmarkTiles);

        for (int y = 0; y < Manager.worldMapSize.x; y++)
        {
            for (int x = 0; x < Manager.worldMapSize.y; x++)
            {
                MapInfo mi = worldMapData.GetTileAt(x, y);
                int tileNum = BiomeToIndex(mi.biome);

                if (SeedManager.worldRandom.Next(100) < 40)
                    tileNum += 8;

                Color[] p = tiles[tileNum];

                if (mi.biome == Biome.Tundra)
                    p = tsm.tilesets[2].Autotile[BitwiseOceanAdjacent(x, y)].GetPixels();
                else if (mi.biome == Biome.Shore)
                    p = tsm.tilesets[1].Autotile[BitwiseOceanAdjacent(x, y)].GetPixels();
                else if (mi.biome == Biome.Desert)
                    p = tsm.tilesets[0].Autotile[BitwiseOceanAdjacent(x, y)].GetPixels();

                texture.SetPixels(x * tileResolution, y * tileResolution, tileResolution, tileResolution, p);
                PlaceLandmark(x, y, mi);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.Apply();

        GetComponent<Renderer>().material.mainTexture = texture;
        yield return null;
    }

    void PlaceLandmark(int x, int y, MapInfo mi)
    {
        //Set landmarks
        if (mi.biome != Biome.Mountain && mi.HasLandmark())
        {
            GameObject g = Instantiate(landmarkObject, new Vector3(x + 50 + 0.5f, y - 200 + 0.5f, -1), Quaternion.identity, transform);
            g.name = string.Format("{0} - {1}", mi.landmark, new Coord(x, y).ToString());
            SpriteRenderer sr = g.GetComponent<SpriteRenderer>();

            if (mi.landmark == "River")
            {
                sr.sprite = tsm.tilesets[3].Autotile[BitwiseRivers(x, y)];
            }
            else
            {
                Color[] l = lmTiles[worldMapData.GetZone(mi.landmark).tileID];
                Texture2D t = new Texture2D(tileResolution, tileResolution, TextureFormat.ARGB32, false)
                {
                    filterMode = FilterMode.Point
                };

                t.SetPixels(l);
                sr.sprite = Sprite.Create(t, new Rect(0, 0, tileResolution, tileResolution), new Vector2(0.5f, 0.5f), tileResolution);
                landmarks.Add(new Coord(x, y), g);
            }
        }
    }

    int BiomeToIndex(Biome b)
    {
        switch (b)
        {
            case Biome.Default:
            case Biome.Ocean:
                return 0;
            case Biome.Shore:
                return 1;
            case Biome.Plains:
                return 2;
            case Biome.Forest:
                return 3;
            case Biome.Swamp:
                return 4;
            case Biome.Mountain:
                return 5;
            case Biome.Desert:
                return 6;
            case Biome.Tundra:
                return 7;

            default:
                return 0;
        }
    }

    Color[][] ChopUpSpriteSheet(Texture2D tex)
    {
        int numTilesPerRow = tex.width / tileResolution;
        int numRows = tex.height / tileResolution;

        Color[][] tiles = new Color[numTilesPerRow * numRows][];

        for (int y = 0; y < numRows; y++)
        {
            for (int x = 0; x < numTilesPerRow; x++)
            {
                tiles[y * numTilesPerRow + x] = tex.GetPixels(x * tileResolution, y * tileResolution, tileResolution, tileResolution);
            }
        }

        return tiles;
    }

    int BitwiseOceanAdjacent(int x, int y)
    {
        int sum = 0;

        if (y < Manager.worldMapSize.x - 1 && worldMapData.GetTileAt(x, y + 1).biome != Biome.Ocean || y >= Manager.worldMapSize.y - 1)
            sum++;
        if (x > 0 && worldMapData.GetTileAt(x - 1, y).biome != Biome.Ocean || x <= 0)
            sum += 2;
        if (x < Manager.worldMapSize.x - 1 && worldMapData.GetTileAt(x + 1, y).biome != Biome.Ocean || x >= Manager.worldMapSize.x - 1)
            sum += 4;
        if (y > 0 && worldMapData.GetTileAt(x, y - 1).biome != Biome.Ocean || y <= 0)
            sum += 8;

        return sum;
    }


    int BitwiseRivers(int x, int y)
    {
        int sum = 0;

        if (y < Manager.worldMapSize.y - 1 && (worldMapData.GetTileAt(x, y + 1).landmark == "River" || worldMapData.GetTileAt(x, y + 1).biome == Biome.Ocean)
            || y >= Manager.worldMapSize.y - 1)
            sum++;
        if (x > 0 && (worldMapData.GetTileAt(x - 1, y).landmark == "River" || worldMapData.GetTileAt(x - 1, y).biome == Biome.Ocean)
            || x <= 0)
            sum += 2;
        if (x < Manager.worldMapSize.x - 1 && (worldMapData.GetTileAt(x + 1, y).landmark == "River" || worldMapData.GetTileAt(x + 1, y).biome == Biome.Ocean)
            || x >= Manager.worldMapSize.x - 1)
            sum += 4;
        if (y > 0 && (worldMapData.GetTileAt(x, y - 1).landmark == "River" || worldMapData.GetTileAt(x, y - 1).biome == Biome.Ocean)
            || y <= 0)
            sum += 8;

        return sum;
    }

    public struct Parameters
    {
        public int layers;
        public float gradientAmount;
        public float outerScale;
        public float innerScale;
        public float heatScale;

        public Parameters(int _layers, float _grAmount, float _outerScale, float _innerScale, float _heatScale)
        {
            layers = _layers;
            gradientAmount = _grAmount;
            outerScale = _outerScale;
            innerScale = _innerScale;
            heatScale = _heatScale;
        }
    }

    public enum Biome
    {
        Default,
        Ocean,
        Shore,
        Plains,
        Forest,
        Mountain,
        Tundra,
        Desert,
        Swamp
    }
}
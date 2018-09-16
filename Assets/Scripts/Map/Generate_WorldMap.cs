﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Generate_WorldMap : MonoBehaviour {

	static int width = 200, height = 200;
	static System.Random rng;
	static int[,] map_data;

	static float outerScale;
	static float innerScale;

	static float stretchX;
	static float stretchY;

	public static int[,] Generate(System.Random _rng, int _width, int _height, int layers, float outer, float inner) {
		width = _width;
		height = _height;
		map_data = new int[width, height];
		rng =  _rng;
		outerScale = outer;
		innerScale = inner;

		int centerX = width / 2 - 1 + rng.Next(-10, 11), centerY = height / 2 - 1 + rng.Next(-10, 11);

		float scale2 = outerScale / 2f;
		float xOrg = rng.Next(-10000, 10000), yOrg = rng.Next(-10000, 10000);
		float xOrg2 = rng.Next(-10000, 10000), yOrg2 = rng.Next(-10000, 10000);
		float xOrg3 = rng.Next(-10000, 10000), yOrg3 = rng.Next(-10000, 10000);

		stretchX = rng.Next(90, 110) * 0.01f;
		stretchY = rng.Next(90, 110) * 0.01f;

		float y = 0f;

		while (y < height) {
			float x = 0f;

			while (x < width) {
				float b = 0f;

				for (int i = 1; i < layers + 1; i++) {
					float perlin = Mathf.PerlinNoise(xOrg + x / width * outerScale * i, yOrg + y / height * outerScale * i);

					b += perlin - DistanceToPoint(centerX, centerY, x, y) * 0.6f / (i * 0.5f);
				}

				b *= (Mathf.PerlinNoise(xOrg2 + x / width * scale2, yOrg2 + y / height * scale2)) / (layers - 1);

				map_data[(int)x, (int)y] = GetGeneralBiome(xOrg3, yOrg3, x, y, b);

				x++;
			}

			y++;
		}

		return map_data;
	}

	static int GetGeneralBiome(float xOrg, float yOrg, float x, float y, float c) {
		if (c < 0.1f) {
			return 0;
		} else {
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

	static int GetBiome(float xOrg, float yOrg, float x, float y, float c) {
		float perlin = Mathf.PerlinNoise(xOrg + x / width * 15, yOrg + y / height * 15) * 0.5f;
		perlin = Mathf.Clamp01(perlin);
		perlin += (c);

		if (perlin < 0.5f)
			return 2;
		
		return 1;
	}

	static float DistanceToPoint(int x1, int y1, float x, float y) {
		float distanceX = (x1 - x) * (x1 - x), distanceY = (y1 - y) * (y1 - y);
		return Mathf.Sqrt(distanceX + distanceY) / (height / 2 - 1);
	}
}

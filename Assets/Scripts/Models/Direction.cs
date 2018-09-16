using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public static class Direction {
	public static Dictionary<string, Coord> directions = new Dictionary<string, Coord>() {
		{ "North", new Coord(0, 1) },
		{ "South", new Coord(0, -1) },
		{ "East", new Coord(1, 0) },
		{ "West", new Coord(-1, 0) },
		{ "North East", new Coord(1, 1) },
		{ "South East", new Coord(1, -1) },
		{ "South West", new Coord(-1, -1) },
		{ "North West", new Coord(-1, 1) },
		{ "In Place", new Coord(0, 0) }
	};

	public static Coord Opposite(string name) {
		Coord dir = directions[name];
		return (new Coord(dir.x * -1, dir.y * -1));
	}

	public static Coord Opposite(Coord dir) {
		return (new Coord(dir.x * -1, dir.y * -1));
	}

	public static string GetDirectionName(Coord dir) {
		string dirName = directions.FirstOrDefault(x => x.Value == dir).Key;
		return dirName;
	}

	public static Coord GetDirection(string name) {
		return directions[name];
	}

	public static string Heading(Coord fr, Coord to, int frElev = 0, int toElev = 0) {
		string heading = "";

		if (fr == null || to == null)
			return heading;

		if (to == fr && frElev == toElev)
			return "Here";

		if (fr.x != to.x || fr.y != to.y) {
			if (to.y > fr.y)
				heading += "North ";
			else if (to.y < fr.y)
				heading += "South ";
			if (to.x > fr.x)
				heading += "East";
			else if (to.x < fr.x)
				heading += "West";
		} else 
			heading += (toElev > frElev) ? "Up" : "Down";


		return heading;
	}
}

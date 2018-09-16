using UnityEngine;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Coord  {
	[SerializeField] private int _x;
	[SerializeField] private int _y;

	public int x {
		get { return _x; }
		set { _x = value; }
	}

	public int y {
		get { return _y; }
		set { _y = value; }
	}

	public Coord() {
		_x = 0;
		_y = 0;
	}

	public Coord(int mx, int my) {
		_x = mx;
		_y = my;
	}

	public Coord(Coord other) {
		_x = other.x;
		_y = other.y;
	}

	public bool Equals(Coord other) {
		if (ReferenceEquals(null, other)) 
			return false;
		if (ReferenceEquals(this, other)) 
			return true;
		
		return other.x == x && other.y == y;
	}

	public override bool Equals(object obj) {
		if (ReferenceEquals(null, obj)) 
			return false;
		if (ReferenceEquals(this, obj)) 
			return true;
		
		return obj.GetType() == typeof(Coord) && Equals((Coord)obj);
	}


	public bool directionIsDiagonal() {
		return (Mathf.Abs(x) + Mathf.Abs(y) > 1);
	}

	public Vector2 toVector2() { 
		return new Vector2(x, y); 
	}
	public override string ToString() { 
		return string.Format("({0},{1})", x, y); 
	}
	public override int GetHashCode() { 
		unchecked { return (x * 397) ^ y; } 
	}
	public static bool operator == (Coord c0, Coord c1) { 
		return Equals(c0, c1); 
	}
	public static bool operator != (Coord c0, Coord c1) { 
		return !Equals(c0, c1); 
	}
	public static Coord operator + (Coord c0, Coord c1) { 
		return new Coord(c0.x + c1.x, c0.y + c1.y); 
	}
	public static Coord operator - (Coord c0, Coord c1) { 
		return new Coord(c0.x - c1.x, c0.y - c1.y); 
	}

    public static float Distance(Coord c0, Coord c1) {
        return Vector2.Distance(c0.toVector2(), c1.toVector2());
    }
}

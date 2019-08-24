using UnityEngine;

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Coord
{
    [SerializeField] private int _x;
    [SerializeField] private int _y;

    public int x
    {
        get { return _x; }
        set { _x = value; }
    }

    public int y
    {
        get { return _y; }
        set { _y = value; }
    }

    public Coord()
    {
        _x = 0;
        _y = 0;
    }

    public Coord(int mx, int my)
    {
        _x = mx;
        _y = my;
    }

    public Coord(Coord other)
    {
        _x = other.x;
        _y = other.y;
    }

    public int[] ToArray()
    {
        int[] arr = new int[] { x, y };
        return arr;
    }

    public static float Distance(Coord c0, Coord c1)
    {
        return Vector2.Distance(c0.toVector2(), c1.toVector2());
    }

    public float DistanceTo(Coord c1)
    {
        return Distance(this, c1);
    }

    public bool Equals(Coord other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return other.x == x && other.y == y;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == typeof(Coord) && Equals((Coord)obj);
    }


    public bool directionIsDiagonal()
    {
        return (Mathf.Abs(x) + Mathf.Abs(y) > 1);
    }

    public Vector2 toVector2()
    {
        return new Vector2(x, y);
    }
    public override string ToString()
    {
        return string.Format("({0},{1})", x, y);
    }
    public override int GetHashCode()
    {
        unchecked { return (x * 397) ^ y; }
    }
    public static bool operator ==(Coord c0, Coord c1)
    {
        return Equals(c0, c1);
    }
    public static bool operator !=(Coord c0, Coord c1)
    {
        return !Equals(c0, c1);
    }
    public static Coord operator +(Coord c0, Coord c1)
    {
        return new Coord(c0.x + c1.x, c0.y + c1.y);
    }
    public static Coord operator -(Coord c0, Coord c1)
    {
        return new Coord(c0.x - c1.x, c0.y - c1.y);
    }
}

[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Coord3
{
    [SerializeField] private int _x;
    [SerializeField] private int _y;
    [SerializeField] private int _z;

    public int x
    {
        get { return _x; }
        set { _x = value; }
    }

    public int y
    {
        get { return _y; }
        set { _y = value; }
    }

    public int z
    {
        get { return _z; }
        set { z = value; }
    }

    public Coord3()
    {
        _x = 0;
        _y = 0;
        _z = 0;
    }

    public Coord3(int mx, int my, int mz)
    {
        _x = mx;
        _y = my;
        _z = mz;
    }

    public Coord3(Coord3 other)
    {
        _x = other.x;
        _y = other.y;
        _z = other.z;
    }

    public bool Equals(Coord3 other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return other.x == x && other.y == y && other.z == z;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == typeof(Coord3) && Equals((Coord3)obj);
    }

    public override string ToString()
    {
        return string.Format("({0},{1},{2})", x, y, z);
    }
    public override int GetHashCode()
    {
        unchecked { return (x * 397) ^ y - z; }
    }
    public static bool operator ==(Coord3 c0, Coord3 c1)
    {
        return Equals(c0, c1);
    }
    public static bool operator !=(Coord3 c0, Coord3 c1)
    {
        return !Equals(c0, c1);
    }
    public static Coord3 operator +(Coord3 c0, Coord3 c1)
    {
        return new Coord3(c0.x + c1.x, c0.y + c1.y, c0.z + c1.z);
    }
    public static Coord3 operator -(Coord3 c0, Coord3 c1)
    {
        return new Coord3(c0.x - c1.x, c0.y - c1.y, c0.z - c1.z);
    }
}


using UnityEngine;
using System;
using LitJson;
using MoonSharp.Interpreter;

[Serializable]
[MoonSharpUserData]
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
        x = 0;
        y = 0;
    }

    public Coord(int xy)
    {
        x = y = xy;
    }

    public Coord(int mx, int my)
    {
        x = mx;
        y = my;
    }

    public Coord(Coord other)
    {
        x = other.x;
        y = other.y;
    }

    public Coord(string value)
    {
        FromString(value);
    }

    public Coord(JsonData dat)
    {
        FromString(dat.ToString());
    }

    public void FromString(string value)
    {
        if (value.NullOrEmpty())
        {
            return;
        }

        value.Trim('(', ')', ' ');

        string[] values = value.Split(',');

        if (values.Length < 2 || !int.TryParse(values[0], out _x) || !int.TryParse(values[1], out _y))
        {
            Log.Error("Could not parse Coord from input: \"" + value + "\"");
        }
    }

    public int[] ToArray()
    {
        return new int[] { x, y };
    }

    public static float Distance(Coord c0, Coord c1)
    {
        return Vector2.Distance(c0.toVector2(), c1.toVector2());
    }

    public float DistanceTo(Coord c1)
    {
        return Distance(this, c1);
    }

    public bool AdjacentTo(Coord other)
    {
        if (this == other)
        {
            return false;
        }

        return DistanceTo(other) < 1.5f;
    }

    public bool Equals(Coord other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other.x == x && other.y == y;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        return obj.GetType() == typeof(Coord) && Equals((Coord)obj);
    }

    public bool IsDiagonal()
    {
        return Mathf.Abs(x) == Mathf.Abs(y);
    }

    public bool IsCardinal()
    {
        return x != 0 ^ y != 0;
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
    public static bool operator ==(Coord a, Coord b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(Coord a, Coord b)
    {
        return !a.Equals(b);
    }
    public static Coord operator +(Coord a, Coord b)
    {
        return new Coord(a.x + b.x, a.y + b.y);
    }
    public static Coord operator -(Coord a, Coord b)
    {
        return new Coord(a.x - b.x, a.y - b.y);
    }

    public static Coord RandomInLocalBounds()
    {
        return new Coord(UnityEngine.Random.Range(1, Manager.localMapSize.x - 1), UnityEngine.Random.Range(1, Manager.localMapSize.y - 1));
    }
}
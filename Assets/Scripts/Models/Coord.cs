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
        x = xy;
        y = xy;
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
        x = 0;
        y = 0;
        FromString(value);
    }

    public Coord(JsonData dat)
    {
        x = 0;
        y = 0;
        FromString(dat.ToString());
    }

    public void FromString(string value)
    {

        if (value.NullOrEmpty())
        {
            return;
        }

        if (value[0] == '(')
        {
            value.TrimStart('(');
        }

        if (value[value.Length - 1] == ')')
        {
            value.TrimEnd(')');
        }

        string[] values = value.Split(',');

        //Trim spaces
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = values[i].Trim(' ');
        }

        if (values.Length < 2 || !int.TryParse(values[0], out _x) || !int.TryParse(values[1], out _y))
        {
            Log.Error("Could not parse Coord from \"" + value + "\"");
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

    public bool IsDiagonal()
    {
        return (Mathf.Abs(x) == Mathf.Abs(y));
    }

    public bool IsCardinal()
    {
        return (x != 0 ^ y != 0);
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
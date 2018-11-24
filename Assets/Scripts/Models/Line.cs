using UnityEngine;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Line
{
    Coord c0;
    Coord c1;
    List<Coord> points;

    public Line(Coord _c0, Coord _c1)
    {
        c0 = _c0;
        c1 = _c1;
        points = FillLine();
    }

    public List<Coord> GetPoints()
    {
        if (c0 == null || c1 == null)
            return null;

        if (points == null)
            points = FillLine();

        return new List<Coord>(points);
    }

    List<Coord> FillLine()
    {
        if (c0 == null || c1 == null)
            return null;

        points = new List<Coord>();

        int dx = c1.x - c0.x, dy = c1.y - c0.y;
        int nx = Mathf.Abs(dx), ny = Mathf.Abs(dy);
        int sign_x = dx > 0 ? 1 : -1, sign_y = dy > 0 ? 1 : -1;
        float error = 0.5f;

        Coord p = new Coord(c0.x, c0.y);

        for (int ix = 0, iy = 0; ix < nx || iy < ny;)
        {
            bool incremented = false;

            if ((error + ix) / nx < (error + iy) / ny)
            {
                p.x += sign_x;
                ix++;
                incremented = true;
            }
            if ((error + iy) / ny < (error + ix) / nx)
            {
                p.y += sign_y;
                iy++;
                incremented = true;
            }
            if (!incremented && (error + ix) / nx == (error + iy) / ny)
            {
                p.x += sign_x;
                p.y += sign_y;
                ix++;
                iy++;
            }

            points.Add(new Coord(p.x, p.y));
        }

        return points;
    }

    public static bool inSight(Coord myPos, int cX, int cY)
    {
        int dx = cX - myPos.x, dy = cY - myPos.y;
        int nx = Mathf.Abs(dx), ny = Mathf.Abs(dy);
        int sign_x = dx > 0 ? 1 : -1, sign_y = dy > 0 ? 1 : -1;

        Coord p = new Coord(myPos.x, myPos.y);

        for (int ix = 0, iy = 0; ix < nx || iy < ny;)
        {
            if (!World.tileMap.LightPassableTile(p.x, p.y) && p != myPos)
                return false;

            float fx = (0.5f + ix) / nx, fy = (0.5f + iy) / ny;
            if (fx == fy)
            {
                p.x += sign_x;
                p.y += sign_y;
                ix++;
                iy++;
            }
            else if (fx < fy)
            {
                p.x += sign_x;
                ix++;
            }
            else
            {
                p.y += sign_y;
                iy++;
            }
        }

        return true;
    }
}

[MoonSharpUserData]
public static class LineHelper
{
    static List<Coord> points;

    public static List<Coord> GetPoints(Coord c0, Coord c1)
    {
        Line l = new Line(c0, c1);
        points = l.GetPoints();

        return points;
    }

    public static Coord GetPoint(int index)
    {
        if (points == null || index > points.Count - 1)
            return null;

        return points[index];
    }

    public static int LineLength()
    {
        return points.Count;
    }
}
using System;
using System.Collections.Generic;

public static class ShadowCasting
{

    static List<Coord> VisiblePoints;
    static int VisualRange = 5;
    static int[][] multipliers;

    public static List<Coord> GetVisibleCells()
    {
        Entity e = ObjectManager.playerEntity;

        VisiblePoints = new List<Coord>
        {
            new Coord(e.posX, e.posY)
        };

        VisualRange = e.sightRange;

        DoFOV(e.posX, e.posY, VisualRange);

        return VisiblePoints;
    }

    static void CastLight(int x, int y, int radius, int row, float start_slope, float end_slope, int xx, int xy, int yx, int yy)
    {
        if (start_slope < end_slope)
            return;

        float next_start_slope = start_slope;

        for (int i = row; i <= radius; i++)
        {
            bool blocked = false;
            for (int dx = -i, dy = -i; dx <= 0; dx++)
            {
                float l_slope = (dx - 0.51f) / (dy + 0.5f), r_slope = (dx + 0.51f) / (dy - 0.5f);

                if (start_slope < r_slope)
                    continue;
                else if (end_slope > l_slope)
                    break;

                int sax = dx * xx + dy * xy, say = dx * yx + dy * yy;

                if ((sax < 0 && Math.Abs(sax) > x) || (say < 0 && Math.Abs(say) > y))
                    continue;

                int ax = x + sax, ay = y + say;

                if (ax >= Manager.localMapSize.x || ay >= Manager.localMapSize.y)
                    continue;

                int radius2 = radius * radius;

                if ((dx * dx + dy * dy) < radius2)
                    VisiblePoints.Add(new Coord(ax, ay));

                if (blocked)
                {
                    if (!NoBlock(ax, ay))
                    {
                        next_start_slope = r_slope;
                        continue;
                    }
                    else
                    {
                        blocked = false;
                        start_slope = next_start_slope;
                    }
                }
                else if (!NoBlock(ax, ay))
                {
                    blocked = true;
                    next_start_slope = r_slope;
                    CastLight(x, y, radius, i + 1, start_slope, l_slope, xx, xy, yx, yy);
                }
            }

            if (blocked)
                break;
        }
    }

    static void DoFOV(int x, int y, int radius)
    {
        multipliers = new int[4][] {
        new int[8] {1, 0, 0, -1, -1, 0, 0, 1},
        new int[8] {0, 1, -1, 0, 0, -1, 1, 0},
        new int[8] {0, 1, 1, 0, 0, -1, -1, 0},
        new int[8] {1, 0, 0, 1, -1, 0, 0, -1}
        };

        for (int i = 0; i < 8; i++)
        {
            CastLight(x, y, radius, 1, 1.0f, 0.0f, multipliers[0][i], multipliers[1][i], multipliers[2][i], multipliers[3][i]);
        }
    }

    static bool NoBlock(int x, int y)
    {
        return World.tileMap.LightPassableTile(x, y);
    }
}

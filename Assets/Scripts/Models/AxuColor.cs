using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AxuColor
{
    public static readonly Color Red = new Color(1f, 0f, 0f);
    public static readonly Color Green = new Color(0f, 1f, 0f);
    public static readonly Color Blue = new Color(0f, 0f, 1f);

    public static readonly Color Orange = new Color(1f, 0.647f, 0f);
    public static readonly Color Purple = new Color(0.502f, 0f, 0.502f);
    public static readonly Color Pink = new Color(1f, 0.753f, 0.796f);

    public static readonly Color White = new Color(1f, 1f, 1f);
    public static readonly Color Black = new Color(0f, 0f, 0f);
    public static readonly Color Grey = new Color(0.5f, 0.5f, 0.5f);
    public static readonly Color LiteGrey = new Color(0.333f, 0.333f, 0.333f);
    public static readonly Color DarkGrey = new Color(0.666f, 0.666f, 0.666f);

    public static string Color(this string s, Color color)
    {
        return string.Format("<color=#{0}>{1}</color>", color.ToHex(), s);
    }

    public static string ToHex(this Color c)
    {
        return string.Format("{0:X2}{1:X2}{2:X2}", Mathf.RoundToInt(c.r * 255), Mathf.RoundToInt(c.g * 255), Mathf.RoundToInt(c.b * 255));
    }
}

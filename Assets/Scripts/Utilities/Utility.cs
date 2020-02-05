using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections.Generic;

public static class Utility
{
    public static void Highlight(this GameObject g)
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(g);
    }

    public static void Highlight(this Transform t)
    {
        Highlight(t.gameObject);
    }

    public static void SendMessage(this GameObject g, string methodName)
    {
        g.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
    }

    //Called from Lua
    public static List<Coord> Cone(Entity ent, Coord pos, Coord direction, int length)
    {
        List<Coord> coords = new List<Coord>();
        int o = GetOctant(direction);
        float min = (o * 45) - 46, max = (o * 45) + 46;

        if (min < 0)
        {
            min += 360;
        }

        if (max > 360)
        {
            max -= 360;
        }

        bool swap = (min > max);

        for (int x = pos.x - length; x <= pos.x + length; x++)
        {
            for (int y = pos.y - length; y <= pos.y + length; y++)
            {
                Coord newPos = new Coord(x, y);

                if (newPos != ent.myPos && pos.DistanceTo(newPos) < length + 1f)
                {
                    float b = Bearing(pos, newPos);

                    bool canAdd = (swap) ? !(b < min && b > max) : (b > min && b < max);

                    if (canAdd)
                    {
                        Cell c = World.tileMap.GetCellAt(x, y);

                        if (c != null && c.Walkable_IgnoreEntity && ent.inSight(newPos))
                        {
                            coords.Add(newPos);
                        }
                    }
                }
            }
        }

        return coords;
    }

    /// <summary>
    /// Iterate over a list, performing an action on each item within.
    /// </summary>
    /// <param name="action"></param>
    public static void IterateAction<T>(this List<T> list, Action<T> action)
    {
        if (list == null || action == null)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
            {
                action(list[i]);
            }
        }
    }

    /// <summary>
    /// Iterate over a list in reverse, performing an action on each item within. Great for when you need to delete elements as you go.
    /// </summary>
    /// <param name="action"></param>
    public static void IterateAction_Reverse<T>(this List<T> list, Action<T> action)
    {
        if (list == null || action == null)
        {
            return;
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] != null)
            {
                action(list[i]);
            }
        }
    }

    static int GetOctant(Coord direction)
    {
        if (direction.x == 0 && direction.y == 1)        return 0; //N
        else if (direction.x == 1 && direction.y == 1)   return 1; //NE
        else if (direction.x == 1 && direction.y == 0)   return 2; //E
        else if (direction.x == 1 && direction.y == -1)  return 3; //SE
        else if (direction.x == 0 && direction.y == -1)  return 4; //S
        else if (direction.x == -1 && direction.y == -1) return 5; //SW
        else if (direction.x == -1 && direction.y == 0)  return 6; //W
        else if (direction.x == -1 && direction.y == 1)  return 7; //SE

        return 0;
    }

    static Sprite SetSpriteRect(this Sprite s, int xOffset, int yOffset, Vector2 pivot, int width = 16, int height = 16)
    {
        s = Sprite.Create(s.texture, new Rect(xOffset, yOffset, width, height), pivot, width);
        return s;
    }

    public static void Refresh<T>(this List<T> l)
    {
        if (l == null)
        {
            l = new List<T>();
        }
        else
        {
            l.Clear();
        }
    }

    public static bool IsBetween(this float f, float min, float max)
    {
        return min < f && f < max;
    }

    public static bool IsBetweenInclusive(this float f, float min, float max)
    {
        return min <= f && f <= max;
    }

    public static bool NullOrEmpty<T>(this List<T> l)
    {
        return l == null || l.Count == 0;
    }

    public static bool Empty<T>(this List<T> l)
    {
        return l.Count == 0;
    }

    static float Bearing(Coord c1, Coord c2)
    {
        float theta = Mathf.Atan2(c2.x - c1.x, c2.y - c1.y);

        if (theta < 0.0f)
        {
            theta += (Mathf.PI * 2f);
        }

        return Mathf.Rad2Deg * theta;
    }

    public static Color[] GetPixels(this Sprite s)
    {
        Texture2D tex = s.texture;

        return tex.GetPixels((int)s.rect.xMin, (int)s.rect.yMin, (int)s.rect.width, (int)s.rect.height);
    }

    public static T[,] TransposeRowsAndColumns<T>(this T[,] arr)
    {
        int rowCount = arr.GetLength(0);
        int columnCount = arr.GetLength(1);
        T[,] transposed = new T[columnCount, rowCount];

        if (rowCount == columnCount)
        {
            for (int i = 1; i < rowCount; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    T temp = transposed[i, i];
                    transposed[i, j] = transposed[j, i];
                    transposed[j, i] = temp;
                }
            }
        }
        else
        {
            for (int column = 0; column < columnCount; column++)
            {
                for (int row = 0; row < rowCount; row++)
                {
                    transposed[column, row] = arr[row, column];
                }
            }
        }

        return transposed;
    }

    public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
    {
        if ((oldIndex == newIndex) || (0 > oldIndex) || (oldIndex >= list.Count) || (0 > newIndex) || (newIndex >= list.Count))
        {
            return;
        }

        T tmp = list[oldIndex];

        if (oldIndex < newIndex)
        {
            for (int i = oldIndex; i < newIndex; i++)
            {
                list[i] = list[i + 1];
            }
        }
        else
        {
            for (int i = oldIndex; i > newIndex; i--)
            {
                list[i] = list[i - 1];
            }
        }

        list[newIndex] = tmp;
    }

    public static int Clamp(this int num, int min, int max)
    {
        return Mathf.Clamp(num, min, max);
    }

    public static float Clamp(this float num, float min, float max)
    {
        return Mathf.Clamp(num, min, max);
    }

    public static void DestroyChildren(this Transform root)
    {
        int childCount = root.childCount;

        for (int i = 0; i < childCount; i++)
        {
            GameObject.DestroyImmediate(root.GetChild(0).gameObject);
        }
    }

    public static void DespawnChildren(this Transform root)
    {
        while (root.childCount > 0)
        {
            Transform t = root.GetChild(0);
            t.SetParent(null);

            if (t.GetComponent<UnityEngine.UI.Button>())
            {
                t.GetComponent<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
            }

            SimplePool.Despawn(t.gameObject);
        }
    }

    public static float Float(this System.Random ran)
    {
        return (ran.Next(100) * 0.01f);
    }

    public static float ZeroToOne(this System.Random ran)
    {
        return (ran.Next(0, 101) * 0.01f);
    }

    public static bool CoinFlip(this System.Random ran)
    {
        if (ran == null)
        {
            ran = new System.Random();
        }

        return (ran.Next(100) < 50);
    }

    public static bool OneIn(this System.Random ran, int max)
    {
        return (ran.Next(max) == 0);
    }

    public static float DistanceTo(this Coord c1, Coord c2)
    {
        return Vector2.Distance(c1.toVector2(), c2.toVector2());
    }

    public static T GetRandom<T>(this List<T> list, System.Random rng = null)
    {
        if (list == null || list.Count <= 0)
        {
            return default;
        }

        if (rng != null)
        {
            return list[rng.Next(list.Count)];
        }

        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static T GetRandom<T>(this T[] array, System.Random rng = null)
    {
        if (array == null || array.Length <= 0)
        {
            return default;
        }

        if (rng != null)
        {
            return array[rng.Next(array.Length)];
        }

        return array[UnityEngine.Random.Range(0, array.Length)];
    }

    public static T GetRandom<T, U>(this Dictionary<T, U> dict, System.Random rng = null)
    {
        if (dict == null || dict.Count <= 0)
        {
            return default;
        }

        List<T> list = new List<T>();

        foreach (KeyValuePair<T, U> kvp in dict)
        {
            list.Add(kvp.Key);
        }

        return list.GetRandom(rng);
    }

    public static T GetRandomFromValue<T, IEquatable>(this Dictionary<T, IEquatable> dict, IEquatable value, System.Random rng = null)
    {
        if (dict == null || dict.Count <= 0)
        {
            return default;
        }

        List<T> list = new List<T>();

        foreach (KeyValuePair<T, IEquatable> kvp in dict)
        {
            if (kvp.Value.Equals(value))
            {
                list.Add(kvp.Key);
            }
        }

        return list.GetRandom(rng);
    }

    public static void Swap<T>(ref T x, ref T y)
    {
        T t = y;
        y = x;
        x = t;
    }

    public static T ToEnum<T>(this string enumString)
    {
        return (T)Enum.Parse(typeof(T), enumString);
    }

    public static T WeightedChoice<T>(List<T> list) where T : IWeighted
    {
        return WeightedChoice(list.ToArray());
    }

    public static T WeightedChoice<T>(T[] list) where T : IWeighted
    {
        if (list.Length == 0)
        {
            return default;
        }

        int totalweight = list.Sum(c => c.Weight);
        int choice = SeedManager.combatRandom.Next(totalweight + 1);
        int sum = 0;

        foreach (var obj in list)
        {
            for (int i = sum; i < obj.Weight + sum; i++)
            {
                if (i >= choice)
                {
                    return obj;
                }
            }

            sum += obj.Weight;
        }

        return list.First();
    }

    public static List<Coord> Floodfill_GetRegion(Tile_Data[,] map_data, Coord startPos, Predicate<Tile_Data> p)
    {
        bool[,] processed = new bool[map_data.GetLength(0), map_data.GetLength(1)];
        Queue<Coord> myQueue = new Queue<Coord>();
        List<Coord> regionTiles = new List<Coord>();

        Coord stPos = new Coord(startPos);

        if (stPos.y < 0)
        {
            stPos.y += Manager.localMapSize.y;
        }

        myQueue.Enqueue(stPos);
        regionTiles.Add(stPos);
        processed[stPos.x, stPos.y] = true;

        while (myQueue.Count > 0)
        {
            Coord next = myQueue.Dequeue();

            for (int x = next.x - 1; x <= next.x + 1; x++)
            {
                for (int y = next.y - 1; y <= next.y + 1; y++)
                {
                    if (x < 0 || y < 0 || x >= Manager.localMapSize.x || y >= Manager.localMapSize.y)
                    {
                        continue;
                    }

                    if ((x == next.x || y == next.y) && !processed[x, y] && p(map_data[x, y]))
                    {
                        Coord c = new Coord(x, y);

                        myQueue.Enqueue(c);
                        regionTiles.Add(c);
                        processed[x, y] = true;
                    }
                }
            }
        }

        return regionTiles;
    }

    public static string Format(this string s, params string[] args)
    {
        return string.Format(s, args);
    }

    public static bool NullOrEmpty(this string s)
    {
        return string.IsNullOrEmpty(s);
    }

    public static bool IsNullOrDefault(this Item item)
    {
        return item == null || item.ID == ItemList.NoneItem.ID;
    }
}

public static class FlagsHelper
{
    public static bool IsSet<T>(this T flags, T flag) where T : struct
    {
        int flagsValue = (int)(object)flags;
        int flagValue = (int)(object)flag;

        return (flagsValue & flagValue) != 0;
    }

    public static void Set<T>(this T flags, T flag) where T : struct
    {
        if (flags.IsSet(flag))
        {
            return;
        }

        int flagsValue = (int)(object)flags;
        int flagValue = (int)(object)flag;

        flags = (T)(object)(flagsValue | flagValue);
    }

    public static void Toggle<T>(this T flags, T flag, bool on) where T : struct
    {
        if (on)
        {
            flags.Set(flag);
        }
        else
        {
            flags.UnSet(flag);
        }
    }

    public static void UnSet<T>(this T flags, T flag) where T : struct
    {
        if (!flags.IsSet(flag))
        {
            return;
        }

        int flagsValue = (int)(object)flags;
        int flagValue = (int)(object)flag;

        flags = (T)(object)(flagsValue & (~flagValue));
    }
}

public interface IWeighted
{
    int Weight { get; set; }
}

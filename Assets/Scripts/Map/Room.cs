using UnityEngine;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class Room
{

    public int width;
    public int height;
    public int left;
    public int bottom;
    public Coord signPos;
    public bool isConnected;

    public Room() { }

    public Room(int wid, int hei)
    {
        width = wid;
        height = hei;
        left = 1;
        bottom = 1;
    }

    public Room(int _width, int _height, int _left, int _bottom)
    {
        width = _width;
        height = _height;
        left = _left;
        bottom = _bottom;
    }

    public int right
    {
        get { return left + width - 1; }
    }

    public int top
    {
        get { return bottom + height - 1; }
    }

    public int centerX
    {
        get { return left + width / 2; }
    }

    public int centerY
    {
        get { return bottom + height / 2; }
    }

    public Vector2 myPos
    {
        get { return new Vector2((float)centerX, (float)centerY); }
    }

    //leaves 1 block space between rects
    public bool CollidesWith(Room other)
    {
        return (left - 1 <= other.right + 2 && right + 2 >= other.left - 1 &&
           top + 1 >= other.bottom - 2 && bottom - 2 <= other.top + 1);
    }

    //can be side-by-side
    public bool OverlapsWith(Room other)
    {
        return (left <= other.right && right >= other.left &&
                top >= other.bottom && bottom <= other.top);
    }

    //can be side-by-side
    public bool SharesEdgeWith(Room other)
    {
        return (left + 1 <= other.right - 1 && right - 1 >= other.left + 1 &&
            top - 1 >= other.bottom + 1 && bottom + 1 <= other.top - 1);
    }

    public Room ClosestNonConnectedRoom(List<Room> otherRooms)
    {
        Room closest = null;
        float distance = Mathf.Infinity;

        for (int i = 0; i < otherRooms.Count; i++)
        {
            if (otherRooms[i] == this || otherRooms[i].isConnected)
                continue;

            float dist = Vector2.Distance(myPos, otherRooms[i].myPos);

            if (dist < distance)
            {
                closest = otherRooms[i];
                distance = dist;
                isConnected = true;
            }
        }

        if (!isConnected)
            return ClosestRoom(otherRooms);

        return closest;
    }

    public Room ClosestRoom(List<Room> otherRooms)
    {
        Room closest = null;
        float distance = Mathf.Infinity;

        for (int i = 0; i < otherRooms.Count; i++)
        {
            if (otherRooms[i] == this)
                continue;
            float dist = Vector2.Distance(myPos, otherRooms[i].myPos);
            if (dist < distance)
            {
                closest = otherRooms[i];
                distance = dist;
            }
        }

        isConnected = true;
        return closest;
    }

    public bool ContainsCoord(Coord c)
    {
        return c.x >= left && c.y <= right && c.y >= bottom && c.y <= top;
    }
}

public class House
{
    public Room[] rooms;
    public HouseType houseType;

    public House(Room r1, Room r2, HouseType ht)
    {
        rooms = new Room[2];
        rooms[0] = r1;
        rooms[1] = r2;
        houseType = ht;
    }

    public Coord GetRandomPosition()
    {
        List<Coord> poss = new List<Coord>();

        for (int i = 0; i < rooms.Length; i++)
        {
            for (int x = rooms[i].left + 1; x < rooms[i].right; x++)
            {
                for (int y = rooms[i].bottom + 1; y < rooms[i].top; y++)
                {
                    if (x >= 0 && y >= 0 && x < Manager.localMapSize.x && y < Manager.localMapSize.y)
                    {
                        Coord c = new Coord(x, y);

                        if (!poss.Contains(c) && World.tileMap.CurrentMap.map_data[c.x, c.y] != TileManager.tiles["Stairs_Down"])
                        {
                            poss.Add(c);
                        }
                    }
                }
            }
        }

        return poss.GetRandom();
    }

    public List<Coord> Interior()
    {
        List<Coord> floors = new List<Coord>();

        for (int i = 0; i < rooms.Length; i++)
        {
            for (int x = rooms[i].left + 1; x < rooms[i].right; x++)
            {
                for (int y = rooms[i].bottom + 1; y < rooms[i].top; y++)
                {
                    if (x >= 0 && y >= 0 && x < Manager.localMapSize.x && y < Manager.localMapSize.y)
                    {
                        Coord c = new Coord(x, y);

                        if (!floors.Contains(c))
                        {
                            floors.Add(c);
                        }
                    }
                }
            }
        }

        return floors;
    }

    public Room Boundary()
    {
        int left = Left();
        int bottom = Bottom();

        return new Room(Right() - left + 1, Top() - bottom + 1, left, bottom);
    }

    public int Left()
    {
        return rooms[0].left < rooms[1].left ? rooms[0].left : rooms[1].left;
    }

    public int Bottom()
    {
        return rooms[0].bottom < rooms[1].bottom ? rooms[0].bottom : rooms[1].bottom;
    }

    public int Right()
    {
        return rooms[0].right > rooms[1].right ? rooms[0].right : rooms[1].right;
    }

    public int Top()
    {
        return rooms[0].top > rooms[1].top ? rooms[0].top : rooms[1].top;
    }

    public enum HouseType
    {
        Villager,
        Merchant,
        Doctor
    }
}

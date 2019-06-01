﻿using System;

public class CellTrigger
{
    readonly Action<Entity> actionToPerform;
    readonly ARect rect;

    public CellTrigger(int l, int b, int w, int h, Action<Entity> action)
    {
        rect = new ARect(l, b, w, h);
        actionToPerform = action;
        Init();
    }

    void Init()
    {
        for (int x = rect.left; x < rect.right; x++)
        {
            for (int y = rect.bottom; y < rect.top; y++)
            {
                if (World.OutOfLocalBounds(x, y))
                {
                    continue;
                }

                Cell c = World.tileMap.GetCellAt(x, y);

                if (c != null)
                {
                    c.onEntityEnter += PerformAction;
                }
            }
        }

        World.tileMap.OnScreenChange += Destroy;
    }

    bool Destroy(TileMap_Data oldMap, TileMap_Data newMap)
    {
        for (int x = rect.left; x <= rect.right; x++)
        {
            for (int y = rect.bottom; y <= rect.top; y++)
            {
                if (World.OutOfLocalBounds(x, y))
                {
                    continue;
                }

                Cell c = World.tileMap.GetCellAt(x, y);

                if (c != null)
                {
                    c.onEntityEnter -= PerformAction;
                }
            }
        }

        return true;
    }

    public void PerformAction(Entity e)
    {
        if (e.isPlayer)
        {
            actionToPerform(e);
            Destroy(null, null);
        }
    }
}

public struct ARect
{
    public int left, bottom, width, height;

    public int right
    {
        get
        {
            return left + width;
        }
    }

    public int top
    {
        get
        {
            return bottom + height;
        }
    }

    public int centerX
    {
        get
        {
            return left + (width / 2);
        }
    }

    public int centerY
    {
        get
        {
            return bottom + (height / 2);
        }
    }

    public ARect(int l, int b, int w, int h)
    {
        left = l;
        bottom = b;
        width = w;
        height = h;
    }
}

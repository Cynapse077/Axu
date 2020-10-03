using System;

public class CellTrigger
{
    public LuaCall luaCall;
    public IntRect rect;

    public CellTrigger() { }

    public CellTrigger(IntRect rect, LuaCall call)
    {
        this.rect = rect;
        luaCall = call;
    }

    public CellTrigger(int l, int b, int w, int h, LuaCall call)
    {
        rect = new IntRect(l, b, w, h);
        luaCall = call;
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
                    c.AddOnEnterCallback(PerformAction);
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
                    c.RemoveOnEnterCallback(PerformAction);
                }
            }
        }

        return true;
    }

    private void PerformAction(Entity e)
    {
        if (e.isPlayer)
        {
            LuaManager.CallScriptFunction(luaCall, e);
            Destroy(null, null);
        }
    }
}

public struct IntRect
{
    public int left, bottom, width, height;

    public int right => left + width;
    public int top => bottom + height;
    public int centerX => left + (width / 2);
    public int centerY => bottom + (height / 2);

    public IntRect(int l, int b, int w, int h)
    {
        left = l;
        bottom = b;
        width = w;
        height = h;
    }
}

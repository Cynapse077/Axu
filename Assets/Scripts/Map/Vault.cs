﻿using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Vault
{
    public Coord position;
    public TileMap_Data[] screens;
    public Vault_Blueprint blueprint;

    public Vault(Coord pos, Vault_Blueprint bp)
    {
        position = pos;
        blueprint = bp;
        screens = new TileMap_Data[bp.depth + 1];
    }

    public TileMap_Data GetLevel(int level, bool visited)
    {
        if (screens.Length <= level)
        {
            return new TileMap_Data(position.x, position.y, level, this, visited);
        }

        if (screens[level] == null)
        {
            screens[level] = CreateLevel(level, visited);
        }

        return screens[level];
    }

    TileMap_Data CreateLevel(int level, bool visited)
    {
        if (!ContainsDepth(level))
        {
            return new TileMap_Data(position.x, position.y, level, this, visited);
        }

        return screens[level];
    }

    public bool ContainsDepth(int level)
    {
        return (level <= blueprint.depth && screens[level] != null);
    }
}

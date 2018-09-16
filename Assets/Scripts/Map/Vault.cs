
[MoonSharp.Interpreter.MoonSharpUserData]
public class Vault {
    public Coord position;
    public TileMap_Data[] screens;
    public ZoneBlueprint_Underground blueprint;
    
    public Vault(Coord pos, ZoneBlueprint_Underground bp) {
        position = pos;
        blueprint = bp;
        screens = new TileMap_Data[bp.depth + 1];
    }

	public TileMap_Data GetLevel(int level, bool goingDown, bool visited) {
		if (screens[level] == null)
			screens[level] = CreateLevel(level, goingDown, visited);
		
		return screens[level];
	}


	TileMap_Data CreateLevel(int level, bool goingDown, bool visited) {
        if (!ContainsDepth(level))
            return new TileMap_Data(position.x, position.y, level, goingDown, this, visited);

        return screens[level];
    }

    public bool ContainsDepth(int level) {
		return (level <= blueprint.depth && screens[level] != null);
    }
}

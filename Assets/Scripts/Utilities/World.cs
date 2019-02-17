
[MoonSharp.Interpreter.MoonSharpUserData]
public static class World
{
    public static ObjectManager objectManager;
    public static TurnManager turnManager;
    public static TileMap tileMap;
    public static SoundManager soundManager;
    public static UserInterface userInterface;
    public static PlayerInput playerInput;
    public static int BaseDangerLevel = 0;
    public static WorldMap worldMap;
    public static Difficulty difficulty;
    public static PoolManager poolManager;

    public static int DangerLevel()
    {
        int dl = BaseDangerLevel;

        if (ObjectManager.player != null && ObjectManager.playerEntity.stats != null && ObjectManager.playerEntity.stats.MyLevel != null)
        {
            dl = BaseDangerLevel + ObjectManager.playerEntity.stats.MyLevel.CurrentLevel;
            dl += turnManager.Day / 2;
        }

        return dl;
    }

    public static void Reset()
    {
        BaseDangerLevel = 0;
        Manager.playerBuilder = null;
        ObjectManager.playerJournal = null;
    }

    public static bool OutOfLocalBounds(int x, int y)
    {
        return (x < 0 || y < 0 || x >= Manager.localMapSize.x || y >= Manager.localMapSize.y);
    }

    public static bool OutOfWorldBounds(int x, int y)
    {
        return (x < 0 || y < 0 || x >= Manager.worldMapSize.x || y >= Manager.worldMapSize.y);
    }
}

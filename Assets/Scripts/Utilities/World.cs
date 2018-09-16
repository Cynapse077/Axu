using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public static class World {
	public static ObjectManager objectManager;
	public static TurnManager turnManager;
	public static TileMap tileMap;
	public static SoundManager soundManager;
	public static UserInterface userInterface;
	public static PlayerInput playerInput;
    public static int BaseDangerLevel = 0;
	public static int HumanCorpsesEaten = 0;
	public static WorldMap worldMap;
	public static Difficulty difficulty;
	public static PoolManager poolManager;

	public static int DangerLevel() {
        int dl = BaseDangerLevel;

        if (ObjectManager.player != null && ObjectManager.playerEntity.stats != null && ObjectManager.playerEntity.stats.MyLevel != null) {
            dl = BaseDangerLevel + ObjectManager.player.GetComponent<Stats>().MyLevel.CurrentLevel;
			dl += turnManager.Day / 2 - 1;
        }
		    
		return dl;
	}

    public static void Reset() {
        BaseDangerLevel = 0;
		HumanCorpsesEaten = 0;
		Manager.playerBuilder = null;
		ObjectManager.playerJournal = null;
	}

	public static void EatHuman() {
		if (HumanCorpsesEaten > 10)
			return;
		
		HumanCorpsesEaten++;

		if (HumanCorpsesEaten >= 10 && !ObjectManager.playerJournal.HasFlag(ProgressFlags.SpawnedCannibal)) {
			ObjectManager.playerJournal.AddFlag(ProgressFlags.SpawnedCannibal);
			List<Coord> c = World.tileMap.InSightCoords();

			if (c.Count > 0) {
				NPC n = World.objectManager.npcClasses.Find(x => x.ID == "killower");

				if (n != null) {
					n.worldPosition = World.tileMap.WorldPosition;
					n.localPosition = c.GetRandom();
					World.tileMap.HardRebuild();
					World.tileMap.LightCheck(ObjectManager.playerEntity);
				}
			}
		}
	}
}

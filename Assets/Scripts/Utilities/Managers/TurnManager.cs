using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class TurnManager : MonoBehaviour {

	public static int dayLength = 4000, nightLength = 2000, costPerAction = 10;

    public int turn = 0;
    public GameObject rainEffect;
    public GameObject snowEffect;
	public GameObject sandstormEffect;
	public event Action incrementTurnCounter;

	public Gradient cloudColors;
	public Gradient timeOfDayColors;
    public Weather currentWeather = Weather.Clear;

	Color currentColor;
	int turnsSinceWeatherChange = 0, TurnsTilWeatherChange = 750;
	List<Entity> npcs = new List<Entity>();
	List<TurnTimer> timers = new List<TurnTimer>();
	int timeOfDay = 0;
	Entity playerEntity;
	Inventory playerInventory;
	ObjectManager objectManager;

    public int FullDayLength {
        get { return dayLength + nightLength; }
    }

	public int Day {
		get {
			return (turn / FullDayLength) + 1;
		}
	}

    public int TimeOfDay {
        get { return timeOfDay; }
        protected set { timeOfDay = value; }
    }

	public Color CurrentCloudColor {
		get {
			return (World.tileMap == null || World.tileMap.currentElevation == 0) ? cloudColors.Evaluate(DayProgress) : Color.white;
		}
	}

	public Color CurrentTODColor {
		get {
			return (World.tileMap == null || World.tileMap.currentElevation == 0) ? timeOfDayColors.Evaluate(DayProgress) : Color.white;
		}
	}

	void OnEnable() {
		World.turnManager = this;
	}

    public void Init() {
        playerEntity = ObjectManager.playerEntity;
		playerInventory = playerEntity.inventory;
        objectManager = World.objectManager;

        IncrementTime(0);
		World.tileMap.HardRebuild();
		World.tileMap.onScreenChange += CheckWeather;

        PlayerTurn();
    }

	public void AddTimer(int duration, Action doneAction) {
		TurnTimer timer = new TurnTimer(duration, doneAction);
		doneAction += () => { timers.Remove(timer); };
		timers.Add(timer);
	}

    public float DayProgress { 
		get {
			return Mathf.PingPong(turn, FullDayLength) / FullDayLength; 
		}
	}
    public float VisionInhibit() {
		return Mathf.Lerp(0f, 40f, DayProgress);
	}

    public void IncrementTime(int amount = 1) {
		for (int i = 0; i < amount; i++) {
			TurnAdvanceMethod();
			CheckWorldEvents();
		}

		if (GameSettings.Enable_Weather) {
			//Check weather change. Cannot snow below equator, so switch to rain if it's going to snow.
			if (turnsSinceWeatherChange >= TurnsTilWeatherChange && UnityEngine.Random.value <= 0.015f) {
				int weatherNum = UnityEngine.Random.Range(0, 4);

				if (weatherNum != 0) {
					if (World.tileMap.CurrentMap.mapInfo.position.y >= Manager.worldMapSize.y / 2 + 25)
						weatherNum = 2;
					else if (World.tileMap.CurrentMap.mapInfo.biome == WorldMap.Biome.Desert)
						weatherNum = 3;
					else
						weatherNum = 1;
				}

				ChangeWeather((Weather)weatherNum);
			}

			CheckWeather();
		}
    }

	void CheckWorldEvents() {
		if (TimeOfDay >= FullDayLength)
			NewDay();
	}

	void NewDay() {
		TimeOfDay = 0;
		World.BaseDangerLevel++;
		List<Quest> dailyQuests = ObjectManager.playerJournal.quests.FindAll(x => x.questType == Quest.QuestType.Daily);

		//Remove daily quests
		for (int i = 0; i < dailyQuests.Count; i++) {
			ObjectManager.playerJournal.quests.Remove(dailyQuests[i]);
		}

		//Give NPCs their daily quests.
		if (ObjectManager.playerJournal.HasFlag(ProgressFlags.Arena_Available) && World.objectManager.NPCExists("arenamaster")) {
			World.objectManager.npcClasses.Find(x => x.ID == "arenamaster").questID = QuestList.GetRandomDailyArenaID();
			CombatLog.SimpleMessage("Message_Arena_Available");
		}
		if (ObjectManager.playerJournal.HasFlag(ProgressFlags.Hunts_Available) && World.objectManager.NPCExists("saira")) {
			World.objectManager.npcClasses.Find(x => x.ID == "saira").questID = QuestList.GetRandomDailyHuntID();
			CombatLog.SimpleMessage("Message_Hunt_Available");
		}

		//Shuffle merchant inventories.
		foreach (NPC n in World.objectManager.npcClasses) {
			if (n.HasFlag(NPC_Flags.Merchant) || n.HasFlag(NPC_Flags.Book_Merchant) || n.HasFlag(NPC_Flags.Doctor))
				n.ReshuffleInventory();
		}
	}

	void TurnAdvanceMethod() {
		if (playerEntity == null)
			return;

		turn++;
		TimeOfDay++;
		turnsSinceWeatherChange++;

		if (incrementTurnCounter != null)
			incrementTurnCounter();
		
		
		List<BodyPart.Hand> hands = ObjectManager.playerEntity.body.Hands;

		//causes equipped items to degrade with charges
		foreach (BodyPart.Hand h in hands) {
			if (h.equippedItem != null && h.equippedItem.HasProp(ItemProperty.Degrade)) {
				if (!h.equippedItem.UseCharge() && h.equippedItem.HasProp(ItemProperty.DestroyOnZeroCharges)) {
					CombatLog.NameMessage("Item_Rot", h.equippedItem.Name);
					h.SetEquippedItem(ItemList.GetItemByID(playerInventory.baseWeapon), ObjectManager.playerEntity);
				}
			}
		}

		List<Item> rotItems = playerInventory.items.FindAll(x => x.GetItemComponent<CRot>() != null);
		//Food items rot
		foreach (Item i in rotItems) {
			if (!i.UseCharge()) {
				CombatLog.NameMessage("Item_Rot", i.Name);
				playerInventory.RemoveInstance(i);
			}
		}

		//Random chance to radiate in swamps
		if (World.tileMap.CurrentMap.mapInfo.biome == WorldMap.Biome.Swamp && SeedManager.combatRandom.Next(100) <= 3)
			playerEntity.stats.Radiate(SeedManager.combatRandom.Next(1, 3));
	}

    public void ChangeWeather(Weather _weather) {
        if (currentWeather != _weather) {
			if (World.tileMap.currentElevation == 0) {
				if (_weather == Weather.Rain)
					CombatLog.SimpleMessage("Weather_Rain");
				else if (_weather == Weather.Snow)
					CombatLog.SimpleMessage("Weather_Snow");
				else if (_weather == Weather.Clear)
					CombatLog.SimpleMessage("Weather_Clear");
				else if (_weather == Weather.Sandstorm)
					CombatLog.SimpleMessage("Weather_Sandstorm");
			}
			
			turnsSinceWeatherChange = 0;
			if (_weather == Weather.Sandstorm)
				TurnsTilWeatherChange = UnityEngine.Random.Range(150, 450);
			else
				TurnsTilWeatherChange = UnityEngine.Random.Range(500, 1500) * ((_weather == Weather.Clear) ? 2 : 1);
        }

        currentWeather = _weather;
		CheckWeather();
    }

	bool CheckWeather(TileMap_Data oldMap, TileMap_Data newMap) {
		CheckWeather();
		return true;
    }

	void CheckWeather() {
		bool canShow = CanShowWeather();
		snowEffect.SetActive(currentWeather == Weather.Snow && canShow);
		sandstormEffect.SetActive(currentWeather == Weather.Sandstorm && canShow && World.tileMap.CurrentMap.mapInfo.biome == WorldMap.Biome.Desert);
		rainEffect.SetActive(currentWeather == Weather.Rain && canShow);
	}

	bool CanShowWeather() {
		return (GameSettings.Enable_Weather && World.tileMap.currentElevation == 0);
	}

    void PlayerTurn() {
		if (playerEntity == null)
			return;

		playerEntity.RefreshActionPoints();

		if (playerEntity.actionPoints >= costPerAction)
			playerEntity.canAct = true;
		else
			playerEntity.EndTurn(0.01f, 0);
    }

    public void EndTurn(float waitTime, int actionPointCost) {
		if (playerEntity != null && playerEntity.stats.dead)
			return;

        CheckInSightObjectAndEntities();
        playerEntity.actionPoints -= actionPointCost;
        IncrementTime();

        if (playerEntity.actionPoints >= costPerAction)
            playerEntity.canAct = true;
        else
        	StartCoroutine(NPCTurns(waitTime + 0.01f));
    }

    public IEnumerator NPCTurns(float wt) {
		npcs.Clear();

		if (objectManager.onScreenNPCObjects.Count > 0)
            yield return new WaitForSeconds(0);

		npcs.AddRange(objectManager.onScreenNPCObjects);

		foreach (Entity ent in npcs) {
			if (ent != null) {
				ent.RefreshActionPoints();

				int numTries = 0;

				while (ent != null && ent.AI != null && ent.actionPoints >= costPerAction) {
					ent.AI.Decision();
					numTries++;

					if (numTries >= 10) {
						ent.actionPoints = 0;
						break;
					}
				}

				if (ent != null && playerEntity != null)
					ent.gameObject.BroadcastMessage("SetEnabled", playerEntity.inSight(ent.myPos));
			}
		}

        PlayerTurn();
    }

    public void CheckInSightObjectAndEntities() {
        if (playerEntity == null)
            return;

        for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++) {
            if (World.objectManager.onScreenNPCObjects[i] != null)
                World.objectManager.onScreenNPCObjects[i].GetComponentInChildren<SpriteRenderer>().enabled = (playerEntity.inSight(World.objectManager.onScreenNPCObjects[i].myPos));
        }
    }
}

public enum Weather {
    Clear,
    Rain,
    Snow,
	Sandstorm,
}
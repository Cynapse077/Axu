using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class TurnManager : MonoBehaviour
{
    const int costPerAction = 10;
    public static int dayLength = 4000, nightLength = 2000;

    public int turn = 0;
    public GameObject rainEffect;
    public GameObject snowEffect;
    public GameObject sandstormEffect;
    public Weather currentWeather = Weather.Clear;
    public event Action incrementTurnCounter;

    Color currentColor;
    int turnsSinceWeatherChange = 0, TurnsTilWeatherChange = 750;
    List<Entity> npcs = new List<Entity>();
    List<TurnTimer> timers = new List<TurnTimer>();
    int timeOfDay = 0;
    Entity playerEntity;

    public int FullDayLength
    {
        get { return dayLength + nightLength; }
    }

    public int Day
    {
        get { return (turn / FullDayLength) + 1; }
    }

    public int TimeOfDay
    {
        get { return timeOfDay; }
        protected set { timeOfDay = value; }
    }

    void OnEnable()
    {
        World.turnManager = this;
    }

    public void Init()
    {
        playerEntity = ObjectManager.playerEntity;

        IncrementTime(0);
        World.tileMap.HardRebuild();
        World.tileMap.OnScreenChange += CheckWeather;

        PlayerTurn();
    }

    public void AddTimer(int duration, Action doneAction)
    {
        TurnTimer timer = new TurnTimer(duration, doneAction);
        doneAction += () => { timers.Remove(timer); };
        timers.Add(timer);
    }

    public float DayProgress
    {
        get
        {
            return Mathf.PingPong(turn, FullDayLength) / FullDayLength;
        }
    }
    public float VisionInhibit()
    {
        return Mathf.Lerp(0f, 40f, DayProgress);
    }

    public void IncrementTime(int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            TurnAdvanceMethod();
            CheckWorldEvents();
        }

        if (GameSettings.Enable_Weather)
        {
            //Check weather change. Cannot snow below equator, so switch to rain if it's going to snow.
            if (turnsSinceWeatherChange >= TurnsTilWeatherChange && UnityEngine.Random.value <= 0.015f)
            {
                int weatherNum = UnityEngine.Random.Range(0, 4);

                if (weatherNum != 0)
                {
                    if (World.tileMap.CurrentMap.mapInfo.position.y >= Manager.worldMapSize.y / 2 + 25)
                        weatherNum = 2;
                    else if (World.tileMap.CurrentMap.mapInfo.biome == Biome.Desert)
                        weatherNum = 3;
                    else
                        weatherNum = 1;
                }

                ChangeWeather((Weather)weatherNum);
            }

            CheckWeather();
        }
    }

    void CheckWorldEvents()
    {
        if (TimeOfDay >= FullDayLength)
        {
            NewDay();
        }
    }

    void NewDay()
    {
        TimeOfDay = 0;

        //Shuffle merchant inventories.
        foreach (NPC n in World.objectManager.npcClasses)
        {
            if (n.ShouldShuffleInventory())
            {
                n.ReshuffleInventory();
            }
        }
    }

    void TurnAdvanceMethod()
    {
        if (playerEntity == null)
        {
            return;
        }

        turn++;
        TimeOfDay++;
        turnsSinceWeatherChange++;

        if (incrementTurnCounter != null)
        {
            incrementTurnCounter();
        }


        List<BodyPart.Hand> hands = ObjectManager.playerEntity.body.Hands;

        //causes equipped items to degrade with charges
        foreach (BodyPart.Hand h in hands)
        {
            if (h.EquippedItem != null && (h.EquippedItem.HasProp(ItemProperty.Degrade) || h.EquippedItem.HasCComponent<CRot>()))
            {
                if (!h.EquippedItem.UseCharge() && h.EquippedItem.HasProp(ItemProperty.DestroyOnZeroCharges))
                {
                    CombatLog.NameMessage("Item_Rot", h.EquippedItem.Name);
                    h.SetEquippedItem(ItemList.GetItemByID(h.baseItem), ObjectManager.playerEntity);
                }
            }
        }

        List<Item> rotItems = ObjectManager.playerEntity.inventory.items.FindAll(x => x.HasCComponent<CRot>());

        //Food items rot
        foreach (Item i in rotItems)
        {
            if (!i.UseCharge())
            {
                CombatLog.NameMessage("Item_Rot", i.Name);
                ObjectManager.playerEntity.inventory.RemoveInstance(i);
            }
        }

        if (World.tileMap.currentElevation == 0)
        {
            MapInfo mi = World.tileMap.CurrentMap.mapInfo;

            if (mi.radiation > 0 && SeedManager.combatRandom.Next(100) < (mi.radiation / 2f))
            {
                playerEntity.stats.Radiate(1);

                //TODO: NPCs radiate too.
            }
        }

        for (int i = ObjectManager.playerJournal.quests.Count - 1; i >= 0; i--)
        {
            ObjectManager.playerJournal.quests[i].OnTurn();
        }
    }

    public void ChangeWeather(Weather _weather)
    {
        if (currentWeather != _weather)
        {
            if (World.tileMap.currentElevation == 0)
            {
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

    bool CheckWeather(TileMap_Data oldMap, TileMap_Data newMap)
    {
        CheckWeather();
        return true;
    }

    void CheckWeather()
    {
        bool canShow = CanShowWeather();

        snowEffect.SetActive(currentWeather == Weather.Snow && canShow);
        sandstormEffect.SetActive(currentWeather == Weather.Sandstorm && canShow && World.tileMap.CurrentMap.mapInfo.biome == Biome.Desert);
        rainEffect.SetActive(currentWeather == Weather.Rain && canShow);
    }

    bool CanShowWeather()
    {
        return (GameSettings.Enable_Weather && World.tileMap.currentElevation == 0);
    }

    void PlayerTurn()
    {
        if (playerEntity == null)
        {
            return;
        }

        playerEntity.RefreshActionPoints();

        if (playerEntity.actionPoints >= costPerAction)
        {
            playerEntity.canAct = true;
        }
        else
        {
            playerEntity.EndTurn(0.01f, 0);
        }
    }

    public void EndTurn(float waitTime, int actionPointCost)
    {
        if (playerEntity == null || playerEntity.stats.dead)
        {
            return;
        }

        playerEntity.actionPoints -= actionPointCost;
        IncrementTime();

        if (playerEntity.actionPoints >= costPerAction)
        {
            playerEntity.canAct = true;
        }
        else
        {
            StartCoroutine(NPCTurns(waitTime + 0.01f));
        }
    }

    public IEnumerator NPCTurns(float wt)
    {
        npcs.Clear();

        if (World.objectManager.onScreenNPCObjects.Count > 0)
        {
            yield return new WaitForSeconds(0);
        }

        npcs.AddRange(World.objectManager.onScreenNPCObjects);
        npcs.OrderBy(o => o.stats.Speed);
        int numTries = 0;
        int chars = 0;

        while (npcs.Count > 0 && chars < 100)
        {
            Entity nextNPC = npcs[0];
            chars++;

            if (nextNPC != null)
            {
                nextNPC.RefreshActionPoints();
                numTries = 0;

                while (nextNPC != null && nextNPC.actionPoints >= costPerAction)
                {
                    nextNPC.AI.Decision();
                    numTries++;

                    if (numTries >= 10)
                    {
                        nextNPC.actionPoints = 0;
                        nextNPC.canAct = false;
                        break;
                    }
                }

                if (playerEntity != null && nextNPC != null)
                {
                    nextNPC.gameObject.BroadcastMessage("SetEnabled", nextNPC.AI.InSightOfPlayer());
                }
            }

            npcs.RemoveAt(0);
        }

        PlayerTurn();
    }

    //Unused
    public void CheckInSightObjectAndEntities()
    {
        if (playerEntity != null)
        {
            for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++)
            {
                if (World.objectManager.onScreenNPCObjects[i] != null)
                {
                    World.objectManager.onScreenNPCObjects[i].GetComponentInChildren<SpriteRenderer>().enabled = playerEntity.inSight(World.objectManager.onScreenNPCObjects[i].myPos);
                }
            }
        }
    }
}

public enum Weather
{
    Clear,
    Rain,
    Snow,
    Sandstorm,
}

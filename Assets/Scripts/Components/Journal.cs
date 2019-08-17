using UnityEngine;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class Journal : MonoBehaviour
{
    public List<Quest> quests;
    public Quest trackedQuest;
    public List<string> completedQuests { get; protected set; }
    public List<string> staticNPCKills { get; set; }

    List<ProgressFlags> progressFlags;

    void Start()
    {
        ObjectManager.playerJournal = this;
        quests = new List<Quest>();
        completedQuests = new List<string>();
        progressFlags = new List<ProgressFlags>();
        staticNPCKills = new List<string>();

        World.tileMap.OnScreenChange += UpdateLocation;
        EventHandler.instance.NPCDied += OnNPCKill;
        trackedQuest = null;

        if (!Manager.newGame)
        {
            foreach (Quest q in Manager.playerBuilder.quests)
            {
                StartQuest(q, false, true);
            }

            for (int i = 0; i < Manager.playerBuilder.progressFlags.Count; i++)
            {
                AddFlag(Manager.playerBuilder.progressFlags[i]);
            }

            completedQuests.AddRange(Manager.playerBuilder.completedQuests);
            staticNPCKills.AddRange(Manager.playerBuilder.killedStaticNPCs);
        }
    }

    void OnDisable()
    {
        World.tileMap.OnScreenChange -= UpdateLocation;
        EventHandler.instance.NPCDied -= OnNPCKill;
        progressFlags.Clear();
    }

    public bool HasCompletedQuest(string id)
    {
        return completedQuests.Contains(id);
    }

    public bool HasQuest(string id)
    {
        return quests.Find(x => x.ID == id) != null;
    }

    public List<ProgressFlags> AllFlags()
    {
        return new List<ProgressFlags>(progressFlags);
    }

    public bool HasFlag(ProgressFlags fl)
    {
        return progressFlags.Contains(fl);
    }

    public void AddFlag(ProgressFlags fl)
    {
        progressFlags.Add(fl);

        if (fl == ProgressFlags.Hostile_To_Ensis)
            MakeHostile("ensis");
        else if (fl == ProgressFlags.Hostile_To_Kin)
            MakeHostile("kin");
        else if (fl == ProgressFlags.Hostile_To_Oromir)
            MakeHostile("magna");
    }

    void MakeHostile(string facID)
    {
        Faction f = GameData.instance.Get<Faction>(facID) as Faction;

        if (f != null)
        {
            if (!f.hostileTo.Contains("player"))
            {
                f.hostileTo.Add("player");
            }

            if (!f.hostileTo.Contains("followers"))
            {
                f.hostileTo.Add("followers");
            }

            BreakFollowersInFaction(facID);
        }
    }

    void BreakFollowersInFaction(string facID)
    {
        foreach (NPC n in World.objectManager.npcClasses)
        {
            if (n.HasFlag(NPC_Flags.Follower) || n.faction.ID == "follower")
            {
                string factionID = EntityList.GetBlueprintByID(n.ID).faction.ID;

                if (factionID == facID)
                {
                    Coord empty = new Coord(0, 0);
                    NPC template = EntityList.GetNPCByID(n.ID, empty, empty);
                    n.faction = template.faction;
                    n.flags = template.flags;
                }
            }
        }
    }

    public void StartQuest(Quest q, bool showInLog = true, bool skipEvent = false)
    {
        quests.Add(q);

        if (showInLog)
        {
            CombatLog.NameMessage("Start_Quest", q.Name);
        }

        q.Start(skipEvent);
        trackedQuest = q;
    }

    public void CompleteQuest(Quest q)
    {
        if (q == trackedQuest)
        {
            if (quests.Count > 1)
            {
                trackedQuest = (quests[0] == q) ? quests[1] : quests[0];
            }
            else
            {
                trackedQuest = null;
            }
        }

        completedQuests.Add(q.ID);
        quests.Remove(q);

        foreach (Entity npc in World.objectManager.onScreenNPCObjects)
        {
            npc.GetComponent<DialogueController>().SetupDialogueOptions();
        }
    }

    public void FailQuest(Quest q)
    {
        quests.Remove(q);
    }

    void OnNPCKill(NPC n)
    {
        if (n.HasFlag(NPC_Flags.Static))
        {
            staticNPCKills.Add(n.ID);
        }
    }

    //Called when the local map changes, checks all quests to see if there is a destination here.
    bool UpdateLocation(TileMap_Data oldMap, TileMap_Data newMap)
    {
        if (newMap.elevation == 0 && newMap.mapInfo.landmark == "Home")
        {
            //Found home base
            if (!progressFlags.Contains(ProgressFlags.Found_Base))
            {
                Alert.NewAlert("Found_Base");
                progressFlags.Add(ProgressFlags.Found_Base);

                //Spawn chest with return tome in it.
                MapObject moj = new MapObject("Chest", new Coord(Manager.localMapSize.x / 2, Manager.localMapSize.y - 8), newMap.mapInfo.position, newMap.elevation)
                {
                    inv = new List<Item>() { ItemList.GetItemByID("tome_return") }
                };

                World.objectManager.SpawnObject(moj);
            }
        }

        EventHandler.instance.OnEnterScreen(newMap);
        return true;
    }
}

public enum ProgressFlags
{
    None,
    Can_Enter_Power_Plant, Can_Enter_Ensis, Can_Open_Prison_Cells, Can_Enter_Magna, Can_Enter_Fab,
    Hostile_To_Kin, Hostile_To_Ensis, Hostile_To_Oromir,
    Hunts_Available, Arena_Available,
    Found_Base, Learned_Butcher
}
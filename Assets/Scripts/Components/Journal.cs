using UnityEngine;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class Journal : MonoBehaviour
{
    public List<Quest> quests;
    public Quest trackedQuest;
    public List<string> completedQuests { get; protected set; }
    public List<string> staticNPCKills { get; set; }

    List<string> progressFlags;

    void Start()
    {
        ObjectManager.playerJournal = this;
        quests = new List<Quest>();
        completedQuests = new List<string>();
        progressFlags = new List<string>();
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

    public List<string> AllFlags()
    {
        return new List<string>(progressFlags);
    }

    public bool HasFlag(string fl)
    {
        return progressFlags.Contains(fl);
    }

    public void AddFlag(string fl)
    {
        if (!progressFlags.Contains(fl))
        {
            progressFlags.Add(fl);
        }

        if (fl.Contains("HostileTo_"))
        {
            string[] ss = fl.Split('_');
            MakeHostile(ss[1]);
        }
    }

    void MakeHostile(string facID)
    {
        Faction f = GameData.Get<Faction>(facID);

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
                string factionID = (GameData.Get<NPC_Blueprint>(n.ID)).faction.ID;

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
            if (!progressFlags.Contains("Found_Base"))
            {
                Alert.NewAlert("Found_Base");
                AddFlag("Found_Base");

                //Spawn chest with return tome in it.
                //Hard-coded. No like.
                List<Item> chestContents = new List<Item>();
                Item item1 = ItemList.GetItemByID("tome_return");
                if (!item1.IsNullOrDefault()) chestContents.Add(item1);
                Item item2 = ItemList.GetItemByID("journal_hunter");
                if (!item2.IsNullOrDefault()) chestContents.Add(item2);

                MapObject_Blueprint bp = GameData.Get<MapObject_Blueprint>("Chest");

                if (bp != null)
                {
                    MapObject moj = new MapObject(bp, new Coord(Manager.localMapSize.x / 2, Manager.localMapSize.y - 8), newMap.mapInfo.position, newMap.elevation)
                    {
                        inv = chestContents
                    };

                    World.objectManager.SpawnObject(moj);
                }
            }
        }

        EventHandler.instance.OnEnterScreen(newMap);
        return true;
    }
}
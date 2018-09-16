using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissionSystem {
    public class EventHandler {
        public static EventHandler instance;

        public event Action<NPC> NPCDied;
        public event Action<Coord, int> EnteredScreen;
        //public event Action<Item> PlayerPickedUpItem;
        //public event Action<Item> PlayerDroppedItem;
        public event Action<NPC> TalkedToNPC;

        public EventHandler() {
            instance = this;
        }
    }


    public class Quest {
        public string Name;
        public string description;
        public List<Goal> goals;
        public bool isComplete;

        public Quest(string _name, string desc, List<Goal> gls, bool complete) {
            Name = _name;
            description = desc;
            goals = gls;
            isComplete = complete;

            Init();
        }

        void Init() {
            if (!isComplete) {
                if (goals == null)
                    goals = new List<Goal>();

                for (int i = 0; i < goals.Count; i++) {
                    goals[i].Init();
                }
            } else {
                Debug.Log("Quest \"" + Name + "\" is complete already.");
            }
        }

        public void CheckProgress() {
            for (int i = 0; i < goals.Count; i++) {
                if (!goals[i].isComplete)
                    return;
            }

            isComplete = true;
            //ObjectManager.playerJournal.CompleteQuest(this);
        }

        public void Fail() {
            for (int i = 0; i < goals.Count; i++) {
                if (!goals[i].isComplete)
                    goals[i].Fail();
            }
        }
    }

    public class Goal {
        public bool isComplete;

        public Goal() {}
        public virtual void Init() {}
        public virtual void Evaluate() {}

        public virtual void Complete() {
            isComplete = true;
        }

        public virtual void Fail() {
            isComplete = false;
        }
    }

    public struct QuestReward {
        public int money;
        public int xp;
        public string[] itemRewards;
    }

    public class NPCKillGoal : Goal {
        public string npcID;
        public int current;
        public int max;

        public NPCKillGoal(string nID, int cur, int mx, bool complete) {
            npcID = nID;
            current = cur;
            max = mx;
            isComplete = complete;
        }

        public override void Init() {
            base.Init();
            EventHandler.instance.NPCDied += NPCKilled;
        }

        void NPCKilled(NPC n) {
            if (n.ID == npcID) {
                current++;
                Evaluate();
            }
        }

        public override void Evaluate() {
            base.Evaluate();

            if (current >= max)
                Complete();
        }

        public override void Complete() {
            base.Complete();
            EventHandler.instance.NPCDied -= NPCKilled;
        }

        public override void Fail() {
            base.Fail();
            EventHandler.instance.NPCDied -= NPCKilled;
        }
    }

    public class FactionKillGoal : Goal {
        public string faction;
        public int current;
        public int max;

        public FactionKillGoal(string _faction, int cur, int mx, bool complete) {
            faction = _faction;
            current = cur;
            max = mx;
            isComplete = complete;
        }

        public override void Init() {
            base.Init();
            EventHandler.instance.NPCDied += NPCKilled;
        }

        void NPCKilled(NPC n) {
            if (n.faction.ID == faction) {
                current++;
                Evaluate();
            }
        }

        public override void Evaluate() {
            base.Evaluate();

            if (current >= max)
                Complete();
        }

        public override void Complete() {
            base.Complete();
            EventHandler.instance.NPCDied -= NPCKilled;
        }

        public override void Fail() {
            base.Fail();
            EventHandler.instance.NPCDied -= NPCKilled;
        }
    }

    public class GoToGoal : Goal {
        public Coord destination;
        public int elevation;

        public GoToGoal(Coord dest, int ele, bool comp) {
            destination = dest;
            elevation = ele;
            isComplete = comp;
        }

        public override void Init() {
            base.Init();
            EventHandler.instance.EnteredScreen += EnteredArea;
        }

        void EnteredArea(Coord c, int ele) {
            if (c == destination && ele == elevation)
                Complete();
        }

        public override void Evaluate() {
            base.Evaluate();
        }

        public override void Complete() {
            base.Complete();
            EventHandler.instance.EnteredScreen -= EnteredArea;
        }

        public override void Fail() {
            base.Fail();
            EventHandler.instance.EnteredScreen -= EnteredArea;
        }
    }
    
    public class TalkToGoal : Goal {
        public NPC npcTarget;

        public TalkToGoal(NPC n, bool comp) {
            npcTarget = n;
            isComplete = comp;
        }

        public override void Init() {
            base.Init();
            EventHandler.instance.TalkedToNPC += TalkToNPC;
        }

        void TalkToNPC(NPC n) {
            if (npcTarget == n)
                Complete();
        }

        public override void Complete() {
            base.Complete();
            EventHandler.instance.TalkedToNPC -= TalkToNPC;
        }

        public override void Fail() {
            base.Fail();
            EventHandler.instance.TalkedToNPC -= TalkToNPC;
        }
    }

    public class FetchGoal : Goal {
        public string itemID;
        public int current;
        public int max;
        public NPC npcTarget;

        Inventory inventory;

        public FetchGoal(NPC n, string id, int cur, int mx, Inventory inv, bool comp) {
            npcTarget = n;
            itemID = id;
            current = cur;
            max = mx;
            inventory = inv;
            isComplete = comp;
        }

        public override void Init() {
            base.Init();

            current = inventory.items.FindAll(x => x.ID == itemID).Count;
            EventHandler.instance.TalkedToNPC += TalkToNPC;
        }

        void TalkToNPC(NPC n) {
            if (npcTarget == n)
                Evaluate();
        }

        public override void Evaluate() {
            base.Evaluate();

            if (current >= max)
                Complete();
        }

        public override void Complete() {
            base.Complete();
        }

        public override void Fail() {
            base.Fail();
        }
    }
} 
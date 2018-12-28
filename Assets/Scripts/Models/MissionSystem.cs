using System;
using System.Collections.Generic;

public class EventHandler
{
    public static EventHandler instance { get; protected set; }

    public event Action<NPC> NPCDied;
    public event Action<Coord, int> EnteredScreen;
    public event Action<NPC> TalkedToNPC;
    public event Action<MapObject> InteractedWithObject;

    public EventHandler()
    {
        instance = this;
    }

    public void OnNPCDeath(NPC n)
    {
        if (NPCDied != null)
        {
            NPCDied(n);
        }
    }

    public void OnEnterScreen(TileMap_Data map)
    {
        if (EnteredScreen != null)
        {
            EnteredScreen(map.mapInfo.position, map.elevation);
        }
    }

    public bool OnTalkTo(NPC n)
    {
        if (TalkedToNPC != null)
        {
            TalkedToNPC(n);
            return true;
        }

        return false;
    }

    public void OnInteract(MapObject m)
    {
        if (InteractedWithObject != null)
        {
            InteractedWithObject(m);
        }
    }
}

public class EventContainer
{
    protected List<QuestEvent> onStart;
    protected List<QuestEvent> onComplete;
    protected List<QuestEvent> onFail;

    public void AddEvent(QuestEvent.EventType eventType, QuestEvent questEvent)
    {
        switch (eventType)
        {
            case QuestEvent.EventType.OnStart:
                if (onStart == null)
                {
                    onStart = new List<QuestEvent>();
                }

                onStart.Add(questEvent);

                break;

            case QuestEvent.EventType.OnComplete:
                if (onComplete == null)
                {
                    onComplete = new List<QuestEvent>();
                }

                onComplete.Add(questEvent);

                break;

            case QuestEvent.EventType.OnFail:
                if (onFail == null)
                {
                    onFail = new List<QuestEvent>();
                }

                onFail.Add(questEvent);

                break;
        }
    }

    public void RunEvent(Quest q, QuestEvent.EventType eventType)
    {
        switch (eventType)
        {
            case QuestEvent.EventType.OnStart:
                if (onStart != null)
                {
                    for (int i = 0; i < onStart.Count; i++)
                    {
                        onStart[i].myQuest = q;
                        onStart[i].RunEvent();
                    }
                }

                break;

            case QuestEvent.EventType.OnComplete:
                if (onComplete != null)
                {
                    for (int i = 0; i < onComplete.Count; i++)
                    {
                        onComplete[i].myQuest = q;
                        onComplete[i].RunEvent();
                    }
                }

                break;

            case QuestEvent.EventType.OnFail:
                if (onFail != null)
                {
                    for (int i = 0; i < onFail.Count; i++)
                    {
                        onFail[i].myQuest = q;
                        onFail[i].RunEvent();
                    }
                }

                break;
        }
    }
}
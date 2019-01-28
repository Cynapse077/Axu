using System;
using System.IO;
using LitJson;
using System.Collections.Generic;
using UnityEngine;

public class GameData
{
    public static GameData instance;

    readonly ItemData items;
    readonly QuestData quests;

    public void InitializeData()
    {
        instance = this;
        //items = new ItemData();
        //quests = new QuestData();
    }

    protected abstract class DataList<T>
    {
        public bool initialized { get; protected set; }
        protected List<T> list;

        protected abstract void Initialize();
        public abstract T Get(string id);

        public List<T> Get(Predicate<T> p)
        {
            return list.FindAll(p);
        }
    }

    protected class ItemData : DataList<Item>
    {
        public ItemData()
        {
            Initialize();
        }

        protected override void Initialize()
        {
            list = new List<Item>();
            initialized = true;
        }

        public override Item Get(string id)
        {
            if (!initialized)
            {
                Debug.LogError("ItemData::Get() - List is not initialized.");
            }

            return list.Find(x => x.ID == id);
        }
    }

    protected class QuestData : DataList<Quest>
    {
        public QuestData()
        {
            Initialize();
        }

        protected override void Initialize()
        {
            list = new List<Quest>();
            initialized = true;
        }

        public override Quest Get(string id)
        {
            if (!initialized)
            {
                Debug.LogError("QuestData::Get() - List is not initialized.");
            }

            return list.Find(x => x.ID == id);
        }
    }
}

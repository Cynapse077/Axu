using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public interface IAsset
{
    string ID { get; set; }
}

public static class GameData
{
    static Dictionary<Type, DataList<IAsset>> data = new Dictionary<Type, DataList<IAsset>>();

    public static void Add<T>(IAsset asset)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            data.Add(typeof(T), new DataList<IAsset>());
        }

        data[typeof(T)].Add(asset);
    }

    public static void Remove<T>(IAsset asset)
    {
        if (data.ContainsKey(typeof(T)))
        {
            data[typeof(T)].Remove(asset);
        }
    }

    public static List<IAsset> Get<T>(Predicate<IAsset> p)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            return new List<IAsset>();
        }

        return data[typeof(T)].Get(p);
    }

    public static IAsset Get<T>(string id)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("Asset List: " + typeof(T).ToString() + " does not exist.");
            return null;
        }

        return data[typeof(T)].Get(id);
    }

    public static List<T> GetAll<T>()
    {
        if (!data.ContainsKey(typeof(T)))
        {
            return new List<T>();
        }

        return data[typeof(T)].GetAll().Cast<T>().ToList();
    }

    public static bool TryGet<T>(string id, out IAsset o)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("Asset List: " + typeof(T).ToString() + " does not exist.");
            o = null;
            return false;
        }

        o = data[typeof(T)].Get(id);
        return true;
    }

    private class DataList<T> where T : IAsset
    {
        private List<T> list;

        public DataList()
        {
            list = new List<T>();
        }

        public T Get(string id)
        {
            T t = list.Find(x => x.ID == id);

            if (t != null)
                return list.Find(x => x.ID == id);

            Debug.LogError("Asset of type " + typeof(T).ToString() + " with ID " + id + " does not exist.");
            return default(T);
        }

        public List<T> Get(Predicate<T> p)
        {
            return list.FindAll(p);
        }

        public List<T> GetAll()
        {
            return list;
        }

        public void Add(T t)
        {
            list.Add(t);
        }

        public void Remove(T t)
        {
            list.Remove(t);
        }
    }
}

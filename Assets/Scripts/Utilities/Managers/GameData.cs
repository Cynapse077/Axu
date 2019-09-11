using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public interface IAsset
{
    string ID { get; set; }
    string ModID { get; set; }
    void FromJson(JsonData dat);
}

public static class GameData
{
    static Dictionary<Type, DataList<IAsset>> data = new Dictionary<Type, DataList<IAsset>>();

    public static void ResetGameData()
    {
        data = new Dictionary<Type, DataList<IAsset>>();
    }

    public static void Add<T>(IAsset asset)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            data.Add(typeof(T), new DataList<IAsset>(typeof(T).ToString()));
        }

        if (string.IsNullOrEmpty(asset.ID))
        {
            Debug.LogError("GameData.Add<T>() - Asset of type " + typeof(T).ToString() + " has an empty ID.");
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
            Debug.LogError("GameData.Get<T>() - No data list of type " + typeof(T).ToString() + " exists. Returning empty list.");
            return new List<IAsset>();
        }

        return data[typeof(T)].Get(p);
    }

    public static IAsset Get<T>(int index)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.Get<T>() - No data list of type " + typeof(T).ToString() + " exists. Returning empty list.");
            return null;
        }

        return data[typeof(T)].Get(index);
    }

    public static IAsset Get<T>(string id)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.Get<T>() - Asset List: " + typeof(T).ToString() + " does not exist.");
            return null;
        }

        return data[typeof(T)].Get(id);
    }

    public static IAsset GetFirst<T>(Predicate<IAsset> p)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.GetFirst<T>() - Asset List: " + typeof(T).ToString() + " does not exist.");
            return null;
        }

        return data[typeof(T)].GetFirstOrDefault(p);
    }

    public static IAsset GetRandom<T>()
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.GetRandom<T>() - Asset List: " + typeof(T).ToString() + " does not exist.");
            return null;
        }

        return data[typeof(T)].GetRandom();
    }

    public static List<T> GetAll<T>()
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.GetAll<T>() - Asset List: " + typeof(T).ToString() + " does not exist. Returning empty list.");
            return new List<T>();
        }

        return data[typeof(T)].GetAll().Cast<T>().ToList();
    }

    public static bool TryGet<T>(string id, out IAsset o)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.TryGet<T>() - Asset List: " + typeof(T).ToString() + " does not exist.");
            o = null;
            return false;
        }

        o = data[typeof(T)].Get(id);
        return true;
    }

    private class DataList<T> where T : IAsset
    {
        readonly string dataType;
        private List<T> list;

        public DataList(string dType)
        {
            dataType = dType;
            list = new List<T>();
        }

        public void Clear()
        {
            list.Clear();
        }

        public T Get(string id)
        {
            return list.FirstOrDefault(x => x.ID.ToLower() == id.ToLower());
        }

        public T Get(int index)
        {
            if (list.Count == 0)
            {
                Log.Error("DataList<" + dataType + "> is empty.");
                return default(T);
            }

            if (index >= list.Count)
            {
                Log.Error("DataList<" + dataType + "> out of range exception.");
                return default(T);
            }

            return list[index];
        }

        public List<T> Get(Predicate<T> p)
        {
            return list.FindAll(p);
        }

        public List<T> GetAll()
        {
            if (list.Count == 0)
            {
                Debug.LogError("DataList<" + dataType + "> is empty.");
            }

            return list;
        }

        public T GetFirstOrDefault(Predicate<T> p)
        {
            List <T> ts = list.FindAll(p);

            if (ts.Count > 0)
            {
                return ts[0];
            }

            Debug.LogError("DataList<" + dataType + "> is empty.");
            return default(T);
        }

        public T GetRandom(System.Random rng = null)
        {
            if (list.Count <= 0)
            {
                Debug.LogError("DataList<" + dataType + "> is empty.");
                return default(T);
            }

            return list.GetRandom(rng);
        }

        public void Add(T t)
        {
            if (list.Any(x => x.ID == t.ID))
            {
                Debug.Log("Overwriting element \"" + t.ID + "\" in list of \"" + dataType);
                Remove(list.Find(x => x.ID == t.ID));
            }

            list.Add(t);
        }

        public void Remove(T t)
        {
            if (list.Contains(t))
            {
                list.Remove(t);
            }
        }
    }
}

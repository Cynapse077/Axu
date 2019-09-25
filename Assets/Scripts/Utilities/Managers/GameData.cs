using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public interface IAsset
{
    string ID { get; set; }
    string ModID { get; set; }
    void FromJson(JsonData dat);
    IEnumerable<string> LoadErrors();
}

public static class GameData
{
    static Dictionary<Type, DataList<IAsset>> data = new Dictionary<Type, DataList<IAsset>>();

    static IEnumerable<string> DefaultLoadWarnings(IAsset asset)
    {
        if (asset.ID.NullOrEmpty())
        {
            yield return "Asset has an empty or null ID.";
        }

        if (asset.ModID.NullOrEmpty())
        {
            yield return "Asset has an empty or null ModID.";
        }
    }

    static void LoadErrors<T>(IAsset asset)
    {
        //Configure errors for the loaded asset.
        IEnumerable<string> warnings = asset.LoadErrors().Concat(DefaultLoadWarnings(asset));

        if (warnings.Count() > 0)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Errors for asset \"{0}\" ({1}): ", asset.ID, typeof(T).ToString()));

            foreach (string warning in warnings)
            {
                sb.AppendLine(string.Format("  - {0}", warning));
            }

            Log.Error(sb.ToString());
        }
    }

    public static void ResetGameData()
    {
        data.Clear();
    }

    public static void Add<T>(IAsset asset)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            data.Add(typeof(T), new DataList<IAsset>(typeof(T).ToString()));
        }

        LoadErrors<T>(asset);
        data[typeof(T)].Add(asset);
    }

    public static void Remove<T>(IAsset asset)
    {
        if (data.ContainsKey(typeof(T)))
        {
            data[typeof(T)].Remove(asset);
        }
    }

    public static List<T> Get<T>(Predicate<IAsset> p)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.Get<T>(Predicate<IAsset> p) - No data list of type " + typeof(T).ToString() + " exists. Returning empty list.");
            return new List<T>();
        }

        return data[typeof(T)].FindAll(p).Cast<T>().ToList();
    }

    public static T Get<T>(int index)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.Get<T>(int index) - No data list of type " + typeof(T).ToString() + " exists. Returning empty list.");
            return default;
        }

        return (T)data[typeof(T)].Get(index);
    }

    public static T Get<T>(string id)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.Get<T>(string id) - Asset List: " + typeof(T).ToString() + " does not exist.");
            return default;
        }

        return (T)data[typeof(T)].Get(id);
    }

    public static T GetFirst<T>(Predicate<IAsset> p)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.GetFirst<T>(Predicate<IAsset> p) - Asset List: " + typeof(T).ToString() + " does not exist.");
            return default;
        }

        return (T)data[typeof(T)].GetFirstOrDefault(p);
    }

    public static T GetRandom<T>()
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.GetRandom<T>() - Asset List: " + typeof(T).ToString() + " does not exist.");
            return default;
        }

        return (T)data[typeof(T)].GetRandom();
    }

    public static T GetRandom<T>(Predicate<IAsset> p)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.GetRandom<T>(Predicate<IAsset> p) - Asset List: " + typeof(T).ToString() + " does not exist.");
            return default;
        }

        return data[typeof(T)].FindAll(p).Cast<T>().ToList().GetRandom();
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

    public static bool TryGet<T>(string id, out T o)
    {
        if (!data.ContainsKey(typeof(T)))
        {
            Debug.LogError("GameData.TryGet<T>() - Asset List: " + typeof(T).ToString() + " does not exist.");
            o = default;
            return false;
        }

        o = (T)data[typeof(T)].Get(id);
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
                return default;
            }

            if (index >= list.Count)
            {
                Log.Error("DataList<" + dataType + "> out of range exception.");
                return default;
            }

            return list[index];
        }

        public List<T> FindAll(Predicate<T> p)
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
            return default;
        }

        public T GetRandom(System.Random rng = null)
        {
            if (list.Count <= 0)
            {
                Debug.LogError("DataList<" + dataType + "> is empty.");
                return default;
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

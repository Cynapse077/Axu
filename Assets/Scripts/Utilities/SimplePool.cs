/// Simple pooling for Unity.
///   Author: Martin "quill18" Glaude (quill18@quill18.com)
///   Latest Version: https://gist.github.com/quill18/5a7cfffae68892621267
///   License: CC0 (http://creativecommons.org/publicdomain/zero/1.0/)


using UnityEngine;
using System.Collections.Generic;

public static class SimplePool
{
    const int DEFAULT_POOL_SIZE = 12;

    class Pool
    {
        int nextId = 1;
        Stack<GameObject> inactive;
        GameObject prefab;

        public Pool(GameObject prefab, int initialQty)
        {
            this.prefab = prefab;
            inactive = new Stack<GameObject>(initialQty);
        }

        public GameObject Spawn(Vector3 pos)
        {
            GameObject obj;

            if (inactive.Count == 0)
            {
                obj = (GameObject)GameObject.Instantiate(prefab, pos, Quaternion.identity);
                obj.name = prefab.name + " (" + (nextId++) + ")";
                obj.AddComponent<PoolMember>().myPool = this;
            }
            else
            {
                obj = inactive.Pop();

                if (obj == null)
                    return Spawn(pos);
            }

            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.identity;
            obj.SetActive(true);
            return obj;

        }

        public GameObject Spawn(Transform parent)
        {
            GameObject obj;
            if (inactive.Count == 0)
            {
                obj = (GameObject)GameObject.Instantiate(prefab, parent);
                obj.name = prefab.name + " (" + (nextId++) + ")";
                obj.AddComponent<PoolMember>().myPool = this;
            }
            else
            {
                obj = inactive.Pop();

                if (obj == null)
                {
                    return Spawn(parent);
                }
            }

            obj.transform.SetParent(parent);
            obj.SetActive(true);
            return obj;

        }

        public void Despawn(GameObject obj)
        {
            obj.SetActive(false);
            inactive.Push(obj);
        }

    }


    class PoolMember : MonoBehaviour
    {
        public Pool myPool;
    }

    static Dictionary<GameObject, Pool> pools;

    static void Init(GameObject prefab = null, int qty = DEFAULT_POOL_SIZE)
    {
        if (pools == null)
        {
            pools = new Dictionary<GameObject, Pool>();
        }
        if (prefab != null && !pools.ContainsKey(prefab))
        {
            pools[prefab] = new Pool(prefab, qty);
        }
    }

    static public void Preload(GameObject prefab, int qty = 1)
    {
        Init(prefab, qty);
        GameObject[] obs = new GameObject[qty];

        for (int i = 0; i < qty; i++)
        {
            obs[i] = Spawn(prefab, Vector3.zero);
        }

        for (int i = 0; i < qty; i++)
        {
            Despawn(obs[i]);
        }
    }

    static public GameObject Spawn(GameObject prefab, Vector3 pos)
    {
        Init(prefab);

        return pools[prefab].Spawn(pos);
    }

    public static GameObject Spawn(GameObject prefab, Transform parent)
    {
        Init(prefab);

        return pools[prefab].Spawn(parent);
    }

    static public void Despawn(GameObject obj)
    {
        PoolMember pm = obj.GetComponent<PoolMember>();

        if (pm == null)
        {
            GameObject.Destroy(obj);
        }
        else
        {
            pm.myPool.Despawn(obj);
        }
    }
}
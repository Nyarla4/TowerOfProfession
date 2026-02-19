using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }
    private SerializedDictionary<GameObject, Queue<GameObject>> _pools = new();

    private List<GameObject> _instantiated = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (prefab == null)
        {
            return null;
        }

        if (!_pools.TryGetValue(prefab, out var q))
        {
            q = new();
            _pools[prefab] = q;
        }

        GameObject go;
        if (q.Count > 0)
        {
            go = q.Dequeue();
        }
        else
        {
            go = Instantiate(prefab);
            _instantiated.Add(go);
        }

        if (parent != null)
        {
            go.transform.SetParent(parent);
        }

        if(go.TryGetComponent<IPooledObject>(out var pooled))
        {
            pooled.SetOrigin(prefab);
        }

        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);

        return go;
    }

    public void Release(GameObject prefab, GameObject instanced)
    {
        if(prefab == null || instanced == null)
        {
            return;
        }

        instanced.SetActive(false);
        instanced.transform.SetParent(transform, false);

        if(!_pools.TryGetValue(prefab,out var q))
        {
            q = new();
            _pools.Add(prefab, q);
        }

        q.Enqueue(instanced);
    }

    public void ReleaseAll()
    {
        for (int instantiatedIndex = 0; instantiatedIndex < _instantiated.Count; instantiatedIndex++)
        {
            var instantiated = _instantiated[instantiatedIndex];
            if (instantiated.activeInHierarchy)
            {
                if(instantiated.TryGetComponent<IPooledObject>(out var pooled))
                {
                    Release(pooled.OriginPrefab, instantiated);
                }
            }
        }
    }

    public static GameObject SpawnOrInstance(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        GameObject go = null;
        if (Instance != null)
        {
            go = Instance.Spawn(prefab, pos, rot, parent);
        }
        else
        {
            if (prefab == null)
            {
                return null;
            }

            if(parent == null)
            {
                go = Instantiate(prefab, pos, rot);
            }
            else
            {
                go = Instantiate(prefab, pos, rot, parent);
            }
        }

        return go;
    }

    public static void ReleaseOrDestroy(GameObject prefab, GameObject instanced)
    {
        if (Instance != null)
        {
            Instance.Release(prefab, instanced);
        }
        else
        {
            if (instanced == null)
            {
                return;
            }
            Destroy(instanced);
        }
    }

    public static List<GameObject> GetInstantiatedObject(GameObject prefab)
    {
        if (Instance != null)
        {
            return Instance._instantiated.FindAll(f => f.activeInHierarchy && f.GetComponent<IPooledObject>().OriginPrefab == prefab);
        }
        return null;
    }
}

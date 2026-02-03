using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    private static PoolManager instance;
    public static PoolManager Instance => instance;

    [SerializeField] private DataSO objectDataSO;

    Dictionary<DataType, Queue<GameObject>> poolDictionary = new Dictionary<DataType, Queue<GameObject>>();


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;


    }
    private void Start()
    {
        InitializePool();
    }

    void InitializePool()
    {
        foreach (var data in objectDataSO.dataList)
        {
            var parent = new GameObject($"{data.dataType}Pool").transform;
            parent.SetParent(transform);

            var pool = poolDictionary.GetValueOrDefault(data.dataType);
            if (pool == null)
            {
                pool = new Queue<GameObject>(data.maxSpawn);
                poolDictionary[data.dataType] = pool;
            }

            for (int i = 0; i < data.maxSpawn; i++)
            {
                var instance = Instantiate(data.prefab, parent);
                instance.SetActive(false);
                pool.Enqueue(instance);
            }
        }
    }


    // ────────────────────────────────────────────────
    //  1. Get an object from the pool
    // ────────────────────────────────────────────────
    public GameObject GetFromPool(DataType type)
    {
        if (!poolDictionary.TryGetValue(type, out var pool) || pool.Count == 0)
        {
            Debug.LogWarning($"Pool for {type} is empty or not found!");
            return null;
        }
        var obj = pool.Dequeue();
        // Prepare object for use
        obj.SetActive(true);
        obj.transform.SetParent(null);
        return obj;
    }

    // ────────────────────────────────────────────────
    //  2. Return object back to the pool
    // ────────────────────────────────────────────────
    public void ReturnToPool(GameObject obj, DataType type)
    {
        if (obj == null) return;


        obj.SetActive(false);

        // Reset position/rotation/velocity/parent/etc if needed
        obj.transform.SetParent(GetPoolParent(obj));

        poolDictionary[type].Enqueue(obj);

    }


    private Transform GetPoolParent(GameObject obj)
    {
        // You can store parent reference per type if you want
        return transform.Find($"{obj.name.Replace("(Clone)", "")}Pool") ?? transform;
    }

}
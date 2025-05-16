using System;
using System.Collections.Generic;
using UnityEngine;

public class Pool<T> where T : Component
{
    private readonly Queue<T> poolQueue = new Queue<T>();
    private readonly List<T> allObjects = new List<T>();
    private readonly T prefab;
    private readonly Transform parent;
    private readonly int initialSize;
    private Action<T> OnGet;
    private Action<T> OnReturn;

    public Pool(T prefab, int initialSize, Transform parent = null, Action<T> onGet = null, Action<T> onReturn = null)
    {
        this.prefab = prefab;
        this.initialSize = initialSize;
        this.parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            AddObjectToPool();
        }

        OnReturn = onReturn;
        OnGet = onGet;
    }

    private void AddObjectToPool()
    {
        T obj = UnityEngine.Object.Instantiate(prefab, parent);
        obj.gameObject.SetActive(false);
        poolQueue.Enqueue(obj);
        allObjects.Add(obj);
    }

    public T Get()
    {
        if (poolQueue.Count == 0)
        {
            AddObjectToPool();
        }

        T obj = poolQueue.Dequeue();
        obj.gameObject.SetActive(true);
        OnGet?.Invoke(obj);
        return obj;
    }

    public void ReturnToPool(T obj)
    {
        OnReturn?.Invoke(obj);
        obj.gameObject.SetActive(false);
        poolQueue.Enqueue(obj);
    }

    public void ReturnAll()
    {
        for (int i = 0; i < allObjects.Count; i++)
        {
            var obj = allObjects[i];
            if (obj != null && obj.gameObject.activeSelf)
            {
                ReturnToPool(obj);
            }
        }
    }

    public int PoolSize => poolQueue.Count;
}

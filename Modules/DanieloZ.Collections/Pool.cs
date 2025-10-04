using System;
using System.Collections.Generic;

public class Pool<T>
{
    private readonly Queue<T> poolQueue = new Queue<T>();
    private readonly List<T> allObjects = new List<T>();
    private readonly Func<T> factoryMethod;
    private readonly int initialSize;
    private readonly Action<T> OnGet;
    private readonly Action<T> OnReturn;
    private readonly int? maxSize;

    public Pool(Func<T> factoryMethod, int initialSize, int? maxSize = null, Action<T> onGet = null, Action<T> onReturn = null)
    {
        this.factoryMethod = factoryMethod ?? throw new ArgumentNullException(nameof(factoryMethod));
        this.initialSize = initialSize;
        OnGet = onGet;
        OnReturn = onReturn;
        this.maxSize = maxSize;

        for (int i = 0; i < initialSize; i++)
        {
            AddObjectToPool();
        }
    }

    private void AddObjectToPool()
    {
        T obj = factoryMethod();
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
        OnGet?.Invoke(obj);
        if (obj is IPoolable poolable) poolable.OnGetFromPool();
        return obj;
    }

    public void ReturnToPool(T obj)
    {
        OnReturn?.Invoke(obj);
        if (obj is IPoolable poolable)
            poolable.OnReturnToPool();

        if (maxSize.HasValue && poolQueue.Count >= maxSize.Value)
        {
            if (obj is IDisposable disposable)
                disposable.Dispose();

            if (obj is UnityEngine.Object unityObj)
                UnityEngine.Object.Destroy(unityObj);

            return;
        }

        poolQueue.Enqueue(obj);
    }

    public void ReturnAll()
    {
        foreach (var obj in allObjects)
        {
            if (!poolQueue.Contains(obj))
                ReturnToPool(obj);
        }
    }

    public int PoolSize => poolQueue.Count;

    public void Clear()
    {
        poolQueue.Clear();
        allObjects.Clear();
    }
}

public interface IPoolable
{
    void OnGetFromPool();
    void OnReturnToPool();
}

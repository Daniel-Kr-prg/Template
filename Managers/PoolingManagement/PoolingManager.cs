using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global pooling manager for all reusable game objects (bricks, balls, etc).
/// </summary>
public class PoolingManager : SingletonManager<PoolingManager>
{
    // Dictionary for pools by key (usually string or Type)
    private readonly Dictionary<string, object> pools = new();

    /// <summary>
    /// Registers a new pool for a given key.
    /// </summary>
    public void RegisterPool<T>(string key, Pool<T> pool) where T : Component
    {
        if (pools.ContainsKey(key))
        {
            DebugWarning($"Pool with key '{key}' already registered. Overwriting.");
        }
        pools[key] = pool;
    }

    /// <summary>
    /// Gets a pool by key. Returns null if not found.
    /// </summary>
    public Pool<T> GetPool<T>(string key) where T : Component
    {
        if (pools.TryGetValue(key, out var poolObj) && poolObj is Pool<T> pool)
            return pool;
        return null;
    }

    /// <summary>
    /// Gets an object from the pool by key.
    /// </summary>
    public T GetFromPool<T>(string key) where T : Component
    {
        var pool = GetPool<T>(key);
        if (pool == null)
        {
            DebugError($"No pool registered for key '{key}' and type {typeof(T).Name}");
            return null;
        }
        return pool.Get();
    }

    /// <summary>
    /// Returns an object to its pool by key.
    /// </summary>
    public void ReturnToPool<T>(string key, T obj) where T : Component
    {
        var pool = GetPool<T>(key);
        if (pool == null)
        {
            DebugError($"No pool registered for key '{key}' and type {typeof(T).Name}");
            return;
        }
        pool.ReturnToPool(obj);
    }

    /// <summary>
    /// Returns all objects to all pools.
    /// </summary>
    public void ReturnAll()
    {
        foreach (var poolObj in pools.Values)
        {
            var poolType = poolObj.GetType();
            var method = poolType.GetMethod("ReturnAll");
            method?.Invoke(poolObj, null);
        }
    }

    /// <summary>
    /// Returns all objects to a specific pool by key.
    /// </summary>
    public void ReturnAll(string key)
    {
        if (pools.TryGetValue(key, out var poolObj))
        {
            var poolType = poolObj.GetType();
            var method = poolType.GetMethod("ReturnAll");
            method?.Invoke(poolObj, null);
        }
    }

    /// <summary>
    /// Checks if a pool exists for the given key.
    /// </summary>
    public bool HasPool(string key) => pools.ContainsKey(key);

    /// <summary>
    /// Removes a pool by key.
    /// </summary>
    public void RemovePool(string key)
    {
        if (pools.ContainsKey(key))
            pools.Remove(key);
    }
} 
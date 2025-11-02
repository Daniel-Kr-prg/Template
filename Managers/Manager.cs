using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using DanieloZ.InstancePromise;

/// <summary>
/// Add [M] tag to the GameObject's name to show that this is a SingletonManager object
/// </summary>
/// <typeparam name="T"></typeparam>

public abstract class SingletonManager<T> : SingletonManagerBase where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    
    /// <summary>
    /// Promise для асинхронного получения экземпляра
    /// </summary>
    private static InstancePromise<T> _instancePromise;
    public static InstancePromise<T> Promise
    {
        get
        {
            if (_instancePromise == null)
            {
                _instancePromise = new InstancePromise<T>();
            }
            return _instancePromise;
        }
    }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
            
            // Разрешаем промис при инициализации экземпляра
            if (_instancePromise != null && !_instancePromise.IsResolved)
            {
                _instancePromise.Resolve(Instance);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        // Сбрасываем промис при уничтожении экземпляра (для повторной инициализации)
        if (Instance == this as T)
        {
            _instancePromise?.Reset();
        }
    }

    public void DebugError(string message)
    {
        string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        Debug.LogError($"[M] {typeof(T).Name} / {methodName}: {message}");
    }

    public void DebugMessage(string message)
    {
        string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        Debug.Log($"[M] {typeof(T).Name} / {methodName}: {message}");
    }

    public void DebugWarning(string message)
    {
        string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        Debug.LogWarning($"[M] {typeof(T).Name} / {methodName}: {message}");
    }

    public static bool HaveInstance() => Instance != null;
    
    /// <summary>
    /// Получает экземпляр через промис. Если экземпляр уже создан, вызывает callback немедленно.
    /// </summary>
    /// <param name="callback">Callback функция, которая будет вызвана с экземпляром</param>
    public static void GetInstanceAsync(System.Action<T> callback)
    {
        if (Instance != null)
        {
            callback?.Invoke(Instance);
        }
        else
        {
            Promise.Then(callback);
        }
    }
}

public abstract class SingletonManagerBase : MonoBehaviour { }



public abstract class SingletonNetworkManager<T> : SingletonNetworkManagerBase where T : NetworkBehaviour
{
    public static T Instance { get; private set; }
    
    /// <summary>
    /// Promise для асинхронного получения экземпляра
    /// </summary>
    private static InstancePromise<T> _instancePromise;
    public static InstancePromise<T> Promise
    {
        get
        {
            if (_instancePromise == null)
            {
                _instancePromise = new InstancePromise<T>();
            }
            return _instancePromise;
        }
    }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
            
            // Разрешаем промис при инициализации экземпляра
            if (_instancePromise != null && !_instancePromise.IsResolved)
            {
                _instancePromise.Resolve(Instance);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        // Сбрасываем промис при уничтожении экземпляра (для повторной инициализации)
        if (Instance == this as T)
        {
            _instancePromise?.Reset();
        }
    }

    public void DebugError(string message)
    {
        string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        Debug.LogError($"[M] {typeof(T).Name} / {methodName}: {message}");
    }

    public void DebugMessage(string message)
    {
        string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        Debug.Log($"[M] {typeof(T).Name} / {methodName}: {message}");
    }

    public void DebugWarning(string message)
    {
        string methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        Debug.LogWarning($"[M] {typeof(T).Name} / {methodName}: {message}");
    }

    public static bool HaveInstance() => Instance != null;
    
    /// <summary>
    /// Получает экземпляр через промис. Если экземпляр уже создан, вызывает callback немедленно.
    /// </summary>
    /// <param name="callback">Callback функция, которая будет вызвана с экземпляром</param>
    public static void GetInstanceAsync(System.Action<T> callback)
    {
        if (Instance != null)
        {
            callback?.Invoke(Instance);
        }
        else
        {
            Promise.Then(callback);
        }
    }
}

public abstract class SingletonNetworkManagerBase : NetworkBehaviour { }
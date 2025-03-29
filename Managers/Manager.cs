using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Add [M] tag to the GameObject's name to show that this is a SingletonManager object
/// </summary>
/// <typeparam name="T"></typeparam>

public abstract class SingletonManager<T> : SingletonManagerBase where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
        }
        else
        {
            Destroy(gameObject);
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
}

public abstract class SingletonManagerBase : MonoBehaviour { }



public abstract class SingletonNetworkManager<T> : SingletonNetworkManagerBase where T : NetworkBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
        }
        else
        {
            Destroy(gameObject);
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
}

public abstract class SingletonNetworkManagerBase : NetworkBehaviour { }
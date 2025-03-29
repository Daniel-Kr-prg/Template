using System;
using UnityEngine;

/// <summary>
/// Provides any kind of booleans or other specific static fields, that may be used by many different classes
/// </summary>
public class StateManager : SingletonManager<StateManager>
{
    private void Start()
    {
        // Additional handling before stage changing

        // Satisfy stage condition
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_StateManagerReady");
    }

    // public static bool IsMeow => MeowManager.Instance.MeowIsEnabled() && Meow.Meow.IsMeowing; <-- the idea is to collect different 'state' variables in one place where they can be found,
    // so different classes which requires these vars doesn't have to handle it by themselves, but can find here
    // also it may be easier to sync with server


    private SerializedDictionary<string, Func<bool>> customStates = new SerializedDictionary<string, Func<bool>>();

    public static void RegisterStateVariable(string stateName, Func<bool> stateCondition)
    {
        if (stateName == null || stateName == "")
            return;
        Instance.customStates[stateName] = stateCondition;
    }

    public static void UnregisterStateVariable(string stateName)
    {
        if (Instance.customStates.ContainsKey(stateName))
            Instance.customStates.Remove(stateName);
    }

    public static bool CheckState(string stateName, Action onStateNotFound = null)
    {
        if (Instance.customStates.TryGetValue(stateName, out var state)) 
        { 
            return state.Invoke(); 
        }
        else
        {
            onStateNotFound?.Invoke();
            Instance.DebugError($"no state named {stateName}");
            return false;
        }
    }
}

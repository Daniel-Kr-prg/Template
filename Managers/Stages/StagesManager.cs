using DanieloZ.Managers;
using DanieloZ.Managers.Config;
using DanieloZ.Managers.Sound;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[DefaultExecutionOrder(-299)]
public class StagesManager : SingletonManager<StagesManager>
{
    [SerializeField] AppStageName initialStageName = AppStageName.AppInit;

    public StageLine<AppStageName> AppStages;

    public Dictionary<string, StageLine<string>> StageLines;

    protected override void Awake()
    {
        base.Awake();

        StageLines = new Dictionary<string, StageLine<string>>();

        AppStages = new StageLine<AppStageName>(
            (AppStageName.AppInit, new Stage<AppStageName>(AppStageName.AppInit, () => AppStages.SetStage(AppStageName.ConnectServices))),
            (AppStageName.ConnectServices, new Stage<AppStageName>(AppStageName.ConnectServices, () => AppStages.SetStage(AppStageName.ConfigSetup))),
            (AppStageName.ConfigSetup, new Stage<AppStageName>(AppStageName.ConfigSetup, () => AppStages.SetStage(AppStageName.Start))),
            (AppStageName.Start, new Stage<AppStageName>(AppStageName.Start))
        );

        AppStages.SetStage(initialStageName, false);

        AppStages.currentStage.RegisterTransitionCondition("StagesManager_AppManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!AppManager.HaveInstance())
            {
                DebugError($"AppManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_InputManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!InputManager.HaveInstance())
            {
                DebugError($"InputManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_SoundManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!SoundManager.HaveInstance())
            {
                DebugError($"SoundManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_ConfigManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!ConfigManager.HaveInstance())
            {
                DebugError($"ConfigManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_CameraManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!CameraManager.HaveInstance())
            {
                DebugError($"CameraManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_EventManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!EventManager.HaveInstance())
            {
                DebugError($"EventsManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_StagesManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!StagesManager.HaveInstance())
            {
                DebugError($"StagesManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_SteamManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!SteamManager.HaveInstance())
            {
                DebugError($"SteamConnectionManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_ObjectSelectionManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!ObjectSelectionManager.HaveInstance())
            {
                DebugError($"ObjectSelectionManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_StateManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!StateManager.HaveInstance())
            {
                DebugError($"StateManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_LocalizationManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!LocalizationManager.HaveInstance())
            {
                DebugError($"LocalizationManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_IOManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!IOManager.HaveInstance())
            {
                DebugError($"IOManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_TimeManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!TimeManager.HaveInstance())
            {
                DebugError($"TimingManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_SaveManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!SaveManager.HaveInstance())
            {
                DebugError($"SaveManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_UIManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!UIManager.HaveInstance())
            {
                DebugError($"SaveManager is not initialized");
                return false;
            }
            return true;
        })));
        AppStages.currentStage.RegisterTransitionCondition("StagesManager_AddressablesManagerReady", new StageCondition(new Func<bool>(() =>
        {
            if (!AddressablesManager.HaveInstance())
            {
                DebugError($"SaveManager is not initialized");
                return false;
            }
            return true;
        })));
    }

    private void Start()
    {
        // Additional handling before stage changing
        AppStages.currentStage.SatisfyCondition("StagesManager_StagesManagerReady");
    }
}

public class StageLine<T>
{
    public Dictionary<T, Stage<T>> stages;

    public Stage<T> lastStableStage;
    public Stage<T> currentStage;

    public Dictionary<string, Action> OnStageChanged = new Dictionary<string, Action>();

    public StageLine(params (T, Stage<T>)[] stagesList) 
    {
        stages = new Dictionary<T, Stage<T>>();
        foreach (var stage in stagesList)
        {
            stages.Add(stage.Item1, stage.Item2);
        }
    }
    public void AddStage(T key, Stage<T> stage)
    {
        if (stages.ContainsKey(key))
        {
            Debug.LogWarning("AsdjaSOidjasoidJASD");
            return;
        }

        stages.Add(key, stage);
    }

    #region Moving between stages functions
    public void SetNextStage()
    {
        if (currentStage == null)
        {
            Debug.LogError("[M] StagesManager / SetNextStage: current stage is null. Seems critical... Initializing previous stable stage");
            SetStage(lastStableStage.StageName, false);
        }

        if (!stages.ContainsValue(currentStage))
        {
            Debug.LogError("[M] StagesManager / SetNextStage: current stage isn't listed in stages. Seems critical... Initializing previous stable stage");
            SetStage(lastStableStage.StageName, false);
        }

        List<Stage<T>> stageList = stages.Values.ToList();
        int stageIndex = stageList.IndexOf(currentStage);

        if (stageIndex == stages.Count - 1)
        {
            Debug.LogWarning("[M] StagesManager / SetNextStage: last stage in the list. Can't go to the next stage");
            return;
        }

        if (currentStage.CanChangeState())
        {
            lastStableStage = currentStage;

            currentStage.InvokeStageEnd();
            currentStage = stageList[++stageIndex];
            currentStage.InvokeStageStart();

            InvokeStageChanged();
        }
        else
        {
            Debug.LogWarning("[M] StagesManager / NextStage: conditions are not satisfied. Can't change stage.");
            return;
        }
    }

    public void SetPreviousStage()
    {
        if (currentStage == null)
        {
            Debug.LogError("[M] StagesManager / SetPreviousStage: current stage is null. Seems critical... Initializing previous stable stage");
            SetStage(lastStableStage.StageName, false);
        }

        if (!stages.ContainsValue(currentStage))
        {
            Debug.LogError("[M] StagesManager / SetPreviousStage: current stage isn't listed in stages. Seems critical... Initializing previous stable stage");
            SetStage(lastStableStage.StageName, false);
        }

        List<Stage<T>> stageList = stages.Values.ToList();
        int stageIndex = stageList.IndexOf(currentStage);

        if (stageIndex == 0)
        {
            Debug.LogWarning("[M] StagesManager / SetPreviousStage: last stage in the list. Can't go to the next stage");
            return;
        }

        if (currentStage.CanChangeState())
        {
            lastStableStage = currentStage;

            currentStage.InvokeStageEnd();
            currentStage = stageList[--stageIndex];
            currentStage.InvokeStageStart();

            InvokeStageChanged();
        }
        else
        {
            Debug.LogWarning("[M] StagesManager / SetPreviousStage: conditions are not satisfied. Can't change stage.");
            return;
        }
    }

    public void SetStage(T stageName, bool handleCurrentStage = true)
    {
        Stage<T> stage = stages[stageName];
        if (stage == null)
        {
            Debug.LogError($"[M] StagesManager / SetStage: {stageName} stage is null.");
            return;
        }

        //if (!stage.CanChangeState())
        //{
        //    Debug.LogWarning($"[M] StagesManager / SetStage: {stageName} can't be set. Conditions are not satisfied");
        //    return;
        //}

        if (handleCurrentStage)
        {
            bool canHandleCurrentStage = true;
            if (currentStage == null)
            {
                Debug.LogError("[M] StagesManager / SetStage: current stage is null. Seems critical...");
                canHandleCurrentStage = false;
            }

            if (!stages.ContainsValue(currentStage))
            {
                Debug.LogError("[M] StagesManager / SetStage: current stage isn't listed in stages. Seems critical...");
                canHandleCurrentStage = false;
            }

            if (canHandleCurrentStage)
            {
                if (currentStage.CanChangeState())
                {
                    currentStage.InvokeStageEnd();
                }
                else
                {
                    Debug.LogWarning("[M] StagesManager / SetStage: conditions are not satisfied. Can't change stage.");
                    return;
                }
            }
            else
            {
                return;
            }
        }

        lastStableStage = currentStage;
        currentStage = stage;
        currentStage.InvokeStageStart();

        InvokeStageChanged();
    }

    #endregion
    #region Stage changed event

    public void RegisterStageChangedAction(string key, Action callback)
    {
        OnStageChanged ??= new Dictionary<string, Action>();

        if (OnStageChanged.ContainsKey(key))
        {
            Debug.LogWarning($"[M] StagesManager / Register stage changed action: '{key}' key already exists in the dictionary");
        }
        else
        {
            OnStageChanged.Add(key, callback);
        }
    }

    public void UnregisterStageChangedAction(string key)
    {
        OnStageChanged ??= new Dictionary<string, Action>();

        if (OnStageChanged.ContainsKey(key))
        {
            OnStageChanged.Remove(key);
        }
        else
        {
            Debug.LogWarning($"[M] StagesManager / Register stage changed action: '{key}' key doesn't exist in the dictionary");
        }
    }

    public void InvokeStageChanged()
    {
        if (OnStageChanged == null)
        {
            Debug.LogWarning("[M] StagesManager / Invoke stage changed: OnStageChanged dictionary is null");
            return;
        }

        foreach (Action action in OnStageChanged.Values)
        {
            action.Invoke();
        }
    }
    #endregion
    #region Stage actions
    public void RegisterStageStartAction(T stage, string key, Action callback, StageCondition condition = null, bool OneTimeAction = false)
    {
        stages[stage]?.RegisterStageStartAction(key, callback, condition, OneTimeAction);
    }

    public void UnregisterStageStartAction(T stage, string key)
    {
        stages[stage]?.UnregisterStageStartAction(key);
    }

    public void RegisterStageEndAction(T stage, string key, Action callback, StageCondition condition = null, bool OneTimeAction = false)
    {
        stages[stage]?.RegisterStageEndAction(key, callback, condition, OneTimeAction);
    }

    public void UnregisterStageEndAction(T stage, string key)
    {
        stages[stage]?.UnregisterStageEndAction(key);
    }
    #endregion
    #region Stage conditions
    public void RegisterStageChangeCondition(T stage, string key, StageCondition condition)
    {
        stages[stage]?.RegisterTransitionCondition(key, condition);
    }

    public void UnregisterStageChangeCondition(T stage, string key)
    {
        stages[stage]?.UnregisterTransitionCondition(key);
    }

    public void SatisfyStageChangeCondition(T stage, string key)
    {
        stages[stage]?.SatisfyCondition(key);
    }
    #endregion
}

public class StageAction
{
    public bool OneTimeAction;
    public Action action;

    public StageCondition condition;

    public StageAction(bool OneTimeAction, Action callback, StageCondition condition = null)
    {
        this.OneTimeAction = OneTimeAction;
        action = callback;
        this.condition = condition;
    }

    public bool CanBeInvoked()
    {
        if (condition == null)
            return true;
        return condition.HandleCondition();
    }

}
public class StageCondition
{
    Func<bool> condition;
    Action onFailedSatisfyCallback;
    bool skipOnError = false;
    public StageCondition(Func<bool> condition, Action onFailedSatisfyCallback = null, bool skipOnError = false)
    {
        this.condition = condition;
        this.skipOnError = skipOnError;
        this.onFailedSatisfyCallback = onFailedSatisfyCallback;
    }

    public bool HandleCondition()
    {
        if (condition == null)
        {
            Debug.LogWarning($"[M] StagesManager / HandleCondition: condition func is null. {(skipOnError ? "Skipping condition." : "Can't satisfy the condition")}");
            if (!skipOnError)
                onFailedSatisfyCallback.Invoke();
            return skipOnError;
        }
        return condition();
    }
}

public enum AppStageName
{
    AppInit, // ensures that all Main managers and scenes are loaded
    ConnectServices,
    ConfigSetup,
    Start
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Stage<T>
{
    public T StageName { get; private set; }

    public Dictionary<string, StageCondition> conditionsToChangeStage = new Dictionary<string, StageCondition>();

    public Dictionary<string, StageAction> OnStageStart = new Dictionary<string, StageAction>();
    public Dictionary<string, StageAction> OnStageEnd = new Dictionary<string, StageAction>();

    public Action ConditionSatisfyCallback = null;

    public Stage(T stageName, Action conditionSatisfyCallback = null)
    {
        StageName = stageName;
        this.ConditionSatisfyCallback = conditionSatisfyCallback;
    }

    #region Registering actions
    void RegisterAction(Dictionary<string, StageAction> dictionary, string key, Action callback, StageCondition condition, bool OneTimeAction)
    {
        dictionary ??= new Dictionary<string, StageAction>();

        if (callback == null || key == null || key == "")
        {
            Debug.LogError("[M] StagesManager / Register stage action: passed parameters are not valid");
            return;
        }
        if (dictionary.ContainsKey(key))
        {
            Debug.LogError("[M] StagesManager / Register stage action: passed key already exists");
            return;
        }

        StageAction action = new StageAction(OneTimeAction, callback, condition);
        dictionary.Add(key, action);
    }

    public void RegisterStageStartAction(string key, Action callback, StageCondition condition = null, bool OneTimeAction = false)
    {
        RegisterAction(OnStageStart, key, callback, condition, OneTimeAction);
    }

    public void RegisterStageEndAction(string key, Action callback, StageCondition condition = null, bool OneTimeAction = false)
    {
        RegisterAction(OnStageEnd, key, callback, condition, OneTimeAction);
    }
    #endregion

    #region Unregister actions
    void UnregisterAction(Dictionary<string, StageAction> dictionary, string key)
    {
        if (dictionary.ContainsKey(key))
        {
            dictionary.Remove(key);
        }
    }

    public void UnregisterStageStartAction(string key)
    {
        UnregisterAction(OnStageStart, key);
    }

    public void UnregisterStageEndAction(string key)
    {
        UnregisterAction(OnStageEnd, key);
    }
    #endregion
    #region Invoking stage actions
    void Invoke(Dictionary<string, StageAction> dictionary)
    {
        //bool canBeInvoked = true;
        //foreach (StageAction action in dictionary.Values) 
        //{
        //    canBeInvoked &= action.CanBeInvoked();
        //    if (!canBeInvoked)
        //    {
        //        Debug.Log("[M] StagesManager / Invoke: can't invoke stage action. Condition is not satisfied");
        //        return;
        //    }
        //}

        for (int i = dictionary.Count - 1; i >= 0; i--)
        {
            var action = dictionary.ElementAt(i);
            if (action.Value.CanBeInvoked())
                action.Value.action.Invoke();

            if (action.Value.OneTimeAction)
                dictionary.Remove(action.Key);
        }
    }

    public void InvokeStageStart()
    {
        Invoke(OnStageStart);
    }

    public void InvokeStageEnd()
    {
        Invoke(OnStageEnd);
    }
    #endregion

    #region Transition conditions

    public void RegisterTransitionCondition(string key, StageCondition condition)
    {
        conditionsToChangeStage ??= new Dictionary<string, StageCondition>();
        if (condition == null)
        {
            Debug.LogError("[M] StagesManager / Register condition: passed condition is null");
            return;
        }

        if (key == null || key == "" || conditionsToChangeStage.ContainsKey(key))
        {
            Debug.LogError("[M] StagesManager / Register condition: passed key is not valid");
            return;
        }

        conditionsToChangeStage.Add(key, condition);
    }

    public void UnregisterTransitionCondition(string key)
    {
        if (conditionsToChangeStage.ContainsKey(key))
            conditionsToChangeStage.Remove(key);
    }

    #endregion

    #region Handling conditions

    public bool CanChangeState()
    {
        foreach (StageCondition condition in conditionsToChangeStage.Values)
        {
            if (!condition.HandleCondition())
            {
                Debug.Log("[M] StagesManager / changing state: can't invoke stage action. Condition is not satisfied");
                return false;
            }
        }
        return true;
    }

    public void SatisfyCondition(string key)
    {
        if (conditionsToChangeStage.TryGetValue(key, out StageCondition condition))
        {
            if (condition.HandleCondition())
            {
                conditionsToChangeStage.Remove(key);
                if (conditionsToChangeStage.Count == 0)
                {
                    ConditionSatisfyCallback.Invoke();
                }
            }
        }
    }

    #endregion

}

using DanieloZ.Transitions;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
public class UI_Elements_Page : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] protected bool autoShowPage = false;
    [Header("Transition")]
    [SerializeField] protected TransitionsController transitionsController;
    [Space]
    [SerializeField] protected bool autoregisterTransitions;
    [Space]
    [SerializeField] protected string showTransitionID = "PageBase";
    [SerializeField] protected string hideTransitionID = "PageBase";

    public bool Hidden {  get; private set; }

    SerializedDictionary<string, Action<bool>> onPageShow = new();
    SerializedDictionary<string, Action<bool>> onPageHide = new();

    private void Awake()
    {
        if (autoregisterTransitions)
        {
            RegisterOnPageHide(hideTransitionID, (x) => transitionsController.CallTransition(hideTransitionID, x));
            RegisterOnPageShow(showTransitionID, (x) => transitionsController.CallTransition(showTransitionID, x));
        }

        if (autoShowPage)
        {
            Show(true);
        }
        else
        {
            Hide(true);
        }
    }

    public void Switch()
    {
        if (Hidden)
            Show();
        else
            Hide();
    }

    public void Hide(bool instantly = false)
    {
        Hidden = true;
        foreach (Action<bool> hideAction in onPageHide.Values) { hideAction.Invoke(instantly); }
    }

    public void Show(bool instantly = false)
    {
        Hidden = false;
        foreach (Action<bool> showAction in onPageShow.Values) { showAction.Invoke(instantly); }
    }

    #region Register/Unregister page events
    public void RegisterOnPageShow(string name, Action<bool> onPageShowEvent)
    {
        if (onPageShow.ContainsKey(name))
        {
            UIManager.Instance.DebugWarning($"Event with the name {name} already exists. Skipping");
            return;
        }

        onPageShow.Add(name, onPageShowEvent);
    }

    public void UnregisterOnPageShow(string name)
    {
        if (onPageShow.ContainsKey (name))
        {
            onPageShow.Remove(name);
        }
        else
        {
            UIManager.Instance.DebugWarning($"Event with the name {name} doesn't exist. Skipping");
        }
    }

    public void RegisterOnPageHide(string name, Action<bool> onPageShowEvent)
    {
        if (onPageHide.ContainsKey(name))
        {
            UIManager.Instance.DebugWarning($"Event with the name {name} already exists. Skipping");
            return;
        }

        onPageHide.Add(name, onPageShowEvent);
    }

    public void UnregisterOnPageHide(string name)
    {
        if (onPageHide.ContainsKey(name))
        {
            onPageHide.Remove(name);
        }
        else
        {
            UIManager.Instance.DebugWarning($"Event with the name {name} doesn't exist. Skipping");
        }
    }
    #endregion
}

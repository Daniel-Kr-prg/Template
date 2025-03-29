using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class InputManager : SingletonManager<InputManager>
{
    private bool Active = false;

    private KeysMap KeysMap = new KeysMap();

    private SerializedDictionary<InputActionKey, SortedSet<InputAction>> KeyDownMap = new SerializedDictionary<InputActionKey, SortedSet<InputAction>>();
    private SerializedDictionary<InputActionKey, SortedSet<InputAction>> KeyUpMap = new SerializedDictionary<InputActionKey, SortedSet<InputAction>>();
    private SerializedDictionary<InputActionKey, SortedSet<InputAction>> KeyHoldMap = new SerializedDictionary<InputActionKey, SortedSet<InputAction>>();

    private static string defaultControlsKey = "DefaultControls";

    protected override void Awake()
    {
        base.Awake();

        foreach (InputActionKey key in Enum.GetValues(typeof(InputActionKey)))
        {
            KeyDownMap[key] = new SortedSet<InputAction>();
            KeyUpMap[key] = new SortedSet<InputAction>();
            KeyHoldMap[key] = new SortedSet<InputAction>();
        }
    }
    // Handle keys after steam init
    private void Start()
    {
        // Additional handling before stage changing
        StagesManager.Instance.AppStages.RegisterStageStartAction(AppStageName.ConfigSetup, "InputSetup", () =>
        {
            ReceiveKeysMap();
        });
        StagesManager.Instance.AppStages.RegisterStageChangeCondition(AppStageName.ConfigSetup, "InputSetup_Success", new StageCondition(new Func<bool>(
            () => Active
            )));

        // Satisfy stage condition
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_InputManagerReady");
    }
    private int GetHighestPriority(SortedSet<InputAction> set)
    {
        if (set.Count == 0)
            return 0;

        var topAction = set.First();
        return topAction?.priority ?? 0;
    }

    private int GetPriority(InputPriority priority, SortedSet<InputAction> set)
    {
        switch (priority)
        {
            case InputPriority.Base:
                return 0;
            case InputPriority.SameAsHighest:
                return GetHighestPriority(set);
            case InputPriority.Highest:
                return GetHighestPriority(set) + 1;
            default:
                return 0;
        }
    }

    void ReceiveKeysMap()
    {
        FileReceiver<KeysMap> receiver;
        //if (SteamManager.Instance != null && SteamManager.Instance.Active)
        //{
        //    receiver = new InputManager_KeysMapReceiver_Steam();
        //}
        //else
        //{
            receiver = new InputManager_KeysMapReceiver_Local(ImportantFilepaths.KeysConfigPath);
        //}
        if (!receiver.FileExists())
        {
            LoadDefaultControls(
                (x) => 
                { 
                    KeysMap = x;
                    receiver.SaveFileAsync(KeysMap, null, null);
                }
            );
        }
        else
        {
            KeysMap = receiver.LoadFile();
        }
        Active = KeysMap != null;

        if (Active)
        {
            StagesManager.Instance.AppStages.currentStage.SatisfyCondition("InputSetup_Success");
        }
    }

    public static void LoadDefaultControls(Action<KeysMap> onSuccess)
    {
        AddressablesManager.LoadAssetAsync<TextAsset>(defaultControlsKey, (textAsset) =>
        {
            if (textAsset != null)
            {
                try
                {
                    KeysMap keysMap = JsonConvert.DeserializeObject<KeysMap>(textAsset.text);
                    if (keysMap != null)
                    {
                        onSuccess.Invoke(keysMap);
                    }
                }
                catch (Exception ex)
                {
                    Instance.DebugWarning($"Error deserializing default controls: {ex.Message}");
                }
            }
            else
            {
                Instance.DebugWarning("Default controls TextAsset is null.");
                
            }
        });
    }

#if UNITY_EDITOR
    [ContextMenu("Save to default")]
    public static void SaveToDefaultControls(KeysMap keysMap)
    {
        if (keysMap == null)
            return;

        try
        {
            string json = JsonConvert.SerializeObject(keysMap, Formatting.Indented);
            File.WriteAllText("Assets/Settings/DefaultControls.txt", json);
            Debug.Log("Default controls saved to Assets/Settings/DefaultControls.txt");
        }
        catch (Exception ex)
        {
            InputManager.Instance.DebugError($"Error serializing KeysMap: {ex.Message}");
        }
    }
#endif

    #region Dictionaries register/unregister methods
    private InputAction RegisterKeyAction(
        SerializedDictionary<InputActionKey, SortedSet<InputAction>> dict,
        InputActionKey key,
        string actionName,
        Action keyAction,

        int priority,
        Func<bool> hasError,
        Func<bool> canHandle,
        Action userUnregisterAction,
        bool oneTimeAction
    )
    {
        InputAction inputAction = new InputAction(
            key,
            actionName,
            keyAction,
            systemUnregisterAction: () => { UnregisterKeyAction(dict, key, actionName); },
            priority: priority,
            hasError: hasError,
            canHandle,
            userUnregisterAction,
            oneTimeAction);

        dict[key].Add(inputAction);
        return inputAction;
    }

    public InputAction RegisterKeyAction(Dictionary<InputActionKey, SortedSet<InputAction>> dictionary, InputAction inputAction)
    {
        dictionary[inputAction.key].Add(inputAction);
        return inputAction;
    }

    public InputAction RegisterKeyDownAction(InputActionKey key, string actionName, Action keyAction, int priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
    {
        return RegisterKeyAction(KeyDownMap, key, actionName, keyAction, priority, hasError, canHandle, userUnregisterAction, oneTimeAction);
    }

    public InputAction RegisterKeyUpAction(InputActionKey key, string actionName, Action keyAction, int priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
    {
        return RegisterKeyAction(KeyUpMap, key, actionName, keyAction, priority, hasError, canHandle, userUnregisterAction, oneTimeAction);
    }

    public InputAction RegisterKeyHoldAction(InputActionKey key, string actionName, Action keyAction, int priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
    {
        return RegisterKeyAction(KeyHoldMap, key, actionName, keyAction, priority, hasError, canHandle, userUnregisterAction, oneTimeAction);
    }

    public InputAction RegisterKeyDownAction(InputActionKey key, string actionName, Action keyAction, InputPriority priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
    {
        return RegisterKeyAction(KeyDownMap, key, actionName, keyAction, GetPriority(priority, KeyDownMap[key]), hasError, canHandle, userUnregisterAction, oneTimeAction);
    }

    public InputAction RegisterKeyUpAction(InputActionKey key, string actionName, Action keyAction, InputPriority priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
    {
        return RegisterKeyAction(KeyUpMap, key, actionName, keyAction, GetPriority(priority, KeyUpMap[key]), hasError, canHandle, userUnregisterAction, oneTimeAction);
    }

    public InputAction RegisterKeyHoldAction(InputActionKey key, string actionName, Action keyAction, InputPriority priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
    {
        return RegisterKeyAction(KeyHoldMap, key, actionName, keyAction, GetPriority(priority, KeyHoldMap[key]), hasError, canHandle, userUnregisterAction, oneTimeAction);
    }

    public InputAction RegisterKeyDownAction(InputAction inputAction)
    {
        return RegisterKeyAction(KeyDownMap, inputAction);
    }

    public InputAction RegisterKeyUpAction(InputAction inputAction)
    {
        return RegisterKeyAction(KeyUpMap, inputAction);
    }

    public InputAction RegisterKeyHoldAction(InputAction inputAction)
    {
        return RegisterKeyAction(KeyHoldMap, inputAction);
    }

    // **************************** UNREGISTER *******************************

    public void UnregisterKeyAction(Dictionary<InputActionKey, SortedSet<InputAction>> dictionary, InputActionKey key, string actionName)
    {
        if (!dictionary.TryGetValue(key, out var set) || set.Count == 0)
            return;

        var action = set.FirstOrDefault(a => a.actionName == actionName);
        if (action != null)
        {
            action.Unregister();
            set.Remove(action);
            DebugMessage($"Unregistered key action: {key}, {actionName}");
        }
        else
        {
            DebugError($"Key action not found for unregister: {key}, {actionName}");
        }
    }

    public void UnregisterKeyAction(Dictionary<InputActionKey, SortedSet<InputAction>> dictionary, InputAction inputAction)
    {
        UnregisterKeyAction(dictionary, inputAction.key, inputAction.actionName);
    }

    public void UnregisterKeyDownAction(InputActionKey key, string actionName)
    {
        UnregisterKeyAction(KeyDownMap, key, actionName);
    }

    public void UnregisterKeyUpAction(InputActionKey key, string actionName)
    {
        UnregisterKeyAction(KeyUpMap, key, actionName);
    }

    public void UnregisterKeyHoldAction(InputActionKey key, string actionName)
    {
        UnregisterKeyAction(KeyHoldMap, key, actionName);
    }

    public void UnregisterTotally(InputActionKey key, string actionName)
    {
        UnregisterKeyAction(KeyDownMap, key, actionName);
        UnregisterKeyAction(KeyUpMap, key, actionName);
        UnregisterKeyAction(KeyHoldMap, key, actionName);
    }

    public void UnregisterKeyDownAction(InputAction inputAction)
    {
        UnregisterKeyAction(KeyDownMap, inputAction);
    }

    public void UnregisterKeyUpAction(InputAction inputAction)
    {
        UnregisterKeyAction(KeyUpMap, inputAction);
    }

    public void UnregisterKeyHoldAction(InputAction inputAction)
    {
        UnregisterKeyAction(KeyHoldMap, inputAction);
    }

    public void UnregisterTotally(InputAction inputAction)
    {
        UnregisterKeyAction(KeyDownMap, inputAction.key, inputAction.actionName);
        UnregisterKeyAction(KeyUpMap, inputAction.key, inputAction.actionName);
        UnregisterKeyAction(KeyHoldMap, inputAction.key, inputAction.actionName);
    }
    #endregion

    // ********************************************** HANDLE **********************************************

    private void Update()
    {
        if (!Active)
            return;

        HandleKeyEvents(KeyDownMap, Input.GetKeyDown);
        HandleKeyEvents(KeyUpMap, Input.GetKeyUp);
        HandleKeyEvents(KeyHoldMap, Input.GetKey);
    }

    private void HandleKeyEvents(Dictionary<InputActionKey, SortedSet<InputAction>> dict, Func<KeyCode, bool> inputCheck)
    {
        foreach (var kvp in dict)
        {
            var inputActionKey = kvp.Key;
            var actionSet = kvp.Value;
            if (actionSet.Count == 0)
                continue;

            var errorActions = actionSet.Where(a => a.HasError()).ToList();
            foreach (var errorAction in errorActions)
            {
                errorAction.Unregister(false);
                actionSet.Remove(errorAction);
            }

            if (actionSet.Count == 0)
                continue;

            if (KeysMap.Map.TryGetValue(inputActionKey, out KeyCode code))
            {
                if (inputCheck(code))
                {
                    int topPriority = actionSet.First().priority;
                    var topActions = actionSet.Where(a => a.priority == topPriority).ToList();
                    foreach (var action in topActions)
                    {
                        action.Handle();
                    }
                }
            }
        }
    }
}

[System.Serializable]
public class KeysMap
{
    public SerializedDictionary<InputActionKey, KeyCode> Map;

    public KeysMap() 
    {
        Map = new SerializedDictionary<InputActionKey, KeyCode>();
    }
}

public enum InputPriority
{
    Base,         // 0
    SameAsHighest,// max
    Highest       // max + 1
}
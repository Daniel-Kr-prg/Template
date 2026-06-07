using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

namespace DanieloZ.InputManagement
{
    public class InputManager : SingletonManager<InputManager>
    {
        public MousePositionData Mouse { get; private set; }
        
        private readonly HashSet<InputActionKey> activeDownKeys = new();
        private readonly HashSet<InputActionKey> activeUpKeys = new();
        private readonly HashSet<InputActionKey> activeHoldKeys = new();

        private bool Active = false;

        private KeysMap KeysMap = new KeysMap();

        private SerializedDictionary<InputActionKey, SortedDictionary<int, List<InputAction>>> KeyDownMap = new();
        private SerializedDictionary<InputActionKey, SortedDictionary<int, List<InputAction>>> KeyUpMap = new();
        private SerializedDictionary<InputActionKey, SortedDictionary<int, List<InputAction>>> KeyHoldMap = new();

        private static string defaultControlsKey = "DefaultControls";

        private readonly List<InputActionKey> _keysToDeactivate = new List<InputActionKey>(16);
        private readonly List<int> _bucketsToRemove = new List<int>(8);

        protected override void Awake()
        {
            base.Awake();

            foreach (InputActionKey key in Enum.GetValues(typeof(InputActionKey)))
            {
                KeyDownMap[key] = new();
                KeyUpMap[key] = new();
                KeyHoldMap[key] = new();
            }

            Mouse = new MousePositionData();
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
        private int GetHighestPriority(SortedDictionary<int, List<InputAction>> dict)
        {
            return dict.Count > 0 ? dict.Keys.Last() : 0;
        }

        private int GetPriority(InputPriority priority, SortedDictionary<int, List<InputAction>> dict)
        {
            return priority switch
            {
                InputPriority.Base => 0,
                InputPriority.SameAsHighest => GetHighestPriority(dict),
                InputPriority.Highest => GetHighestPriority(dict) + 1,
                _ => 0
            };
        }

        private void AddToPriorityDict(SortedDictionary<int, List<InputAction>> dict, InputAction action)
        {
            if (!dict.TryGetValue(action.priority, out var list))
            {
                list = new List<InputAction>();
                dict[action.priority] = list;
            }
            list.Add(action);
        }

        private void RemoveFromPriorityDict(SortedDictionary<int, List<InputAction>> dict, string actionName)
        {
            foreach (var kvp in dict.ToList())
            {
                var removed = kvp.Value.RemoveAll(a => a.actionName == actionName);
                if (removed > 0 && kvp.Value.Count == 0)
                    dict.Remove(kvp.Key);
            }
        }

        void ReceiveKeysMap()
        {
            FileReceiver<KeysMap> receiver;
            receiver = new InputManager_KeysMapReceiver_Local(ImportantFilepaths.KeysConfigPath);
            
            if (!receiver.FileExists())
            {
                LoadDefaultControls(
                    (x) => 
                    { 
                        KeysMap = x;
                        ApplyBuiltInFallbackKeys(KeysMap);
                        receiver.SaveFileAsync(KeysMap, null, null);
                    }
                );
            }
            else
            {
                KeysMap = receiver.LoadFile();
                if (ApplyBuiltInFallbackKeys(KeysMap))
                {
                    receiver.SaveFileAsync(KeysMap, null, null);
                }
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
                            ApplyBuiltInFallbackKeys(keysMap);
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

        private static bool ApplyBuiltInFallbackKeys(KeysMap keysMap)
        {
            if (keysMap == null)
            {
                return false;
            }

            keysMap.Map ??= new SerializedDictionary<InputActionKey, KeyCode>();
            var changed = false;

            changed |= TryAddFallbackKey(keysMap, InputActionKey.EXIT, KeyCode.Escape);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.CONFIRM, KeyCode.Return);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.MOVE_FORWARD, KeyCode.W);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.MOVE_BACKWARD, KeyCode.S);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.MOVE_LEFT, KeyCode.A);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.MOVE_RIGHT, KeyCode.D);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.CAMERA_ROTATE_LEFT, KeyCode.Q);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.CAMERA_ROTATE_RIGHT, KeyCode.E);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.JUMP, KeyCode.Space);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.CROUNCH, KeyCode.LeftControl);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.RUN, KeyCode.LeftShift);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.ROTATE_PIECE, KeyCode.R);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.TARGET_BOARD_LOCK, KeyCode.LeftShift);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.TEXT_CHAT, KeyCode.T);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.VOICE_CHAT, KeyCode.V);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.MOUSE_LEFT, KeyCode.Mouse0);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.MOUSE_RIGHT, KeyCode.Mouse1);
            changed |= TryAddFallbackKey(keysMap, InputActionKey.MOUSE_MIDDLE, KeyCode.Mouse2);

            return changed;
        }

        private static bool TryAddFallbackKey(KeysMap keysMap, InputActionKey key, KeyCode fallback)
        {
            if (keysMap.Map.ContainsKey(key))
            {
                return false;
            }

            keysMap.Map[key] = fallback;
            return true;
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
            SerializedDictionary<InputActionKey, SortedDictionary<int, List<InputAction>>> dict,
            InputActionKey key,
            string actionName,
            Action keyAction,
            int priority,
            Func<bool> hasError,
            Func<bool> canHandle,
            Action userUnregisterAction,
            bool oneTimeAction)
        {
            InputAction inputAction = new InputAction(
                key,
                actionName,
                keyAction,
                systemUnregisterAction: () => { UnregisterKeyAction(dict, key, actionName); },
                priority: priority,
                hasError: hasError,
                canHandle: canHandle,
                userUnregisterAction: userUnregisterAction,
                oneTimeAction: oneTimeAction);
            AddToPriorityDict(dict[key], inputAction);
            return inputAction;
        }

        public InputAction RegisterKeyAction(Dictionary<InputActionKey, SortedDictionary<int, List<InputAction>>> dictionary, InputAction inputAction)
        {
            AddToPriorityDict(dictionary[inputAction.key], inputAction);
            return inputAction;
        }

        public InputAction RegisterKeyDownAction(InputActionKey key, string actionName, Action keyAction, int priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
        {
            activeDownKeys.Add(key);
            return RegisterKeyAction(KeyDownMap, key, actionName, keyAction, priority, hasError, canHandle, userUnregisterAction, oneTimeAction);
        }

        public InputAction RegisterKeyUpAction(InputActionKey key, string actionName, Action keyAction, int priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
        {
            activeUpKeys.Add(key);
            return RegisterKeyAction(KeyUpMap, key, actionName, keyAction, priority, hasError, canHandle, userUnregisterAction, oneTimeAction);
        }

        public InputAction RegisterKeyHoldAction(InputActionKey key, string actionName, Action keyAction, int priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
        {
            activeHoldKeys.Add(key);
            return RegisterKeyAction(KeyHoldMap, key, actionName, keyAction, priority, hasError, canHandle, userUnregisterAction, oneTimeAction);
        }

        public InputAction RegisterKeyDownAction(InputActionKey key, string actionName, Action keyAction, InputPriority priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
        {
            activeDownKeys.Add(key);
            return RegisterKeyAction(KeyDownMap, key, actionName, keyAction, GetPriority(priority, KeyDownMap[key]), hasError, canHandle, userUnregisterAction, oneTimeAction);
        }

        public InputAction RegisterKeyUpAction(InputActionKey key, string actionName, Action keyAction, InputPriority priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
        {
            activeUpKeys.Add(key);
            return RegisterKeyAction(KeyUpMap, key, actionName, keyAction, GetPriority(priority, KeyUpMap[key]), hasError, canHandle, userUnregisterAction, oneTimeAction);
        }

        public InputAction RegisterKeyHoldAction(InputActionKey key, string actionName, Action keyAction, InputPriority priority, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
        {
            activeHoldKeys.Add(key);
            return RegisterKeyAction(KeyHoldMap, key, actionName, keyAction, GetPriority(priority, KeyHoldMap[key]), hasError, canHandle, userUnregisterAction, oneTimeAction);
        }

        public InputAction RegisterKeyDownAction(InputAction inputAction)
        {
            activeDownKeys.Add(inputAction.key);
            return RegisterKeyAction(KeyDownMap, inputAction);
        }

        public InputAction RegisterKeyUpAction(InputAction inputAction)
        {
            activeUpKeys.Add(inputAction.key);
            return RegisterKeyAction(KeyUpMap, inputAction);
        }

        public InputAction RegisterKeyHoldAction(InputAction inputAction)
        {
            activeHoldKeys.Add(inputAction.key);
            return RegisterKeyAction(KeyHoldMap, inputAction);
        }

        // **************************** UNREGISTER *******************************

        public void UnregisterKeyAction(Dictionary<InputActionKey, SortedDictionary<int, List<InputAction>>> dictionary, InputActionKey key, string actionName)
        {
            if (!dictionary.TryGetValue(key, out var list) || list.Count == 0)
                return;

            RemoveFromPriorityDict(list, actionName);
        }

        public void UnregisterKeyAction(Dictionary<InputActionKey, SortedDictionary<int, List<InputAction>>> dictionary, InputAction inputAction)
        {
            UnregisterKeyAction(dictionary, inputAction.key, inputAction.actionName);
        }

        public void UnregisterKeyDownAction(InputActionKey key, string actionName)
        {
            UnregisterKeyAction(KeyDownMap, key, actionName);
            if (KeyDownMap[key].Count == 0)
                activeDownKeys.Remove(key);
        }

        public void UnregisterKeyUpAction(InputActionKey key, string actionName)
        {
            UnregisterKeyAction(KeyUpMap, key, actionName);
            if (KeyUpMap[key].Count == 0)
                activeUpKeys.Remove(key);
        }

        public void UnregisterKeyHoldAction(InputActionKey key, string actionName)
        {
            UnregisterKeyAction(KeyHoldMap, key, actionName);
            if (KeyHoldMap[key].Count == 0)
                activeHoldKeys.Remove(key);
        }

        public void UnregisterTotally(InputActionKey key, string actionName)
        {
            UnregisterKeyAction(KeyDownMap, key, actionName);
            if (KeyDownMap[key].Count == 0) activeDownKeys.Remove(key);

            UnregisterKeyAction(KeyUpMap, key, actionName);
            if (KeyUpMap[key].Count == 0) activeUpKeys.Remove(key);

            UnregisterKeyAction(KeyHoldMap, key, actionName);
            if (KeyHoldMap[key].Count == 0) activeHoldKeys.Remove(key);
        }

        public void UnregisterKeyDownAction(InputAction inputAction)
        {
            UnregisterKeyAction(KeyDownMap, inputAction);
            var key = inputAction.key;
            if (KeyDownMap[key].Count == 0)
                activeDownKeys.Remove(key);
        }

        public void UnregisterKeyUpAction(InputAction inputAction)
        {
            UnregisterKeyAction(KeyUpMap, inputAction);
            var key = inputAction.key;
            if (KeyUpMap[key].Count == 0)
                activeUpKeys.Remove(key);
        }

        public void UnregisterKeyHoldAction(InputAction inputAction)
        {
            UnregisterKeyAction(KeyHoldMap, inputAction);
            var key = inputAction.key;
            if (KeyHoldMap[key].Count == 0)
                activeHoldKeys.Remove(key);
        }

        public void UnregisterTotally(InputAction inputAction)
        {
            var key = inputAction.key;

            UnregisterKeyAction(KeyDownMap, inputAction);
            if (KeyDownMap[key].Count == 0) activeDownKeys.Remove(key);

            UnregisterKeyAction(KeyUpMap, inputAction);
            if (KeyUpMap[key].Count == 0) activeUpKeys.Remove(key);

            UnregisterKeyAction(KeyHoldMap, inputAction);
            if (KeyHoldMap[key].Count == 0) activeHoldKeys.Remove(key);
        }
        #endregion

        // ********************************************** HANDLE **********************************************
        #region Unity Lifecycle
        private void Update()
        {
            if (!Active) return;

            HandleKeyEventsForMap(KeyUpMap, activeUpKeys, Input.GetKeyUp);

            if (!Input.anyKey && !Input.anyKeyDown) return;

            HandleKeyEventsForMap(KeyDownMap, activeDownKeys, Input.GetKeyDown);

            HandleKeyEventsForMap(KeyHoldMap, activeHoldKeys, Input.GetKey);
        }

        private void HandleKeyEventsForMap(
        Dictionary<InputActionKey, SortedDictionary<int, List<InputAction>>> map,
        HashSet<InputActionKey> activeKeys,
        Func<KeyCode, bool> inputCheck)
        {
            _keysToDeactivate.Clear();

            foreach (var key in activeKeys)
            {
                if (!map.TryGetValue(key, out var priorityDict) || priorityDict.Count == 0)
                {
                    _keysToDeactivate.Add(key);
                    continue;
                }

                if (!KeysMap.Map.TryGetValue(key, out var keyCode))
                    continue;

                _bucketsToRemove.Clear();
                foreach (var bucket in priorityDict)
                {
                    var prio = bucket.Key;
                    var actions = bucket.Value;
                    for (int i = actions.Count - 1; i >= 0; i--)
                    {
                        if (actions[i].HasError())
                        {
                            actions[i].Unregister(false);
                            actions.RemoveAt(i);
                        }
                    }
                    if (actions.Count == 0)
                        _bucketsToRemove.Add(prio);
                }

                for (int i = 0; i < _bucketsToRemove.Count; i++)
                    priorityDict.Remove(_bucketsToRemove[i]);

                if (priorityDict.Count == 0)
                {
                    _keysToDeactivate.Add(key);
                    continue;
                }

                if (!inputCheck(keyCode))
                    continue;

                var highest = GetHighestPriority(priorityDict);
                var handlers = priorityDict[highest];
                for (int i = 0; i < handlers.Count; i++)
                    handlers[i].Handle();
            }

            for (int i = 0; i < _keysToDeactivate.Count; i++)
                activeKeys.Remove(_keysToDeactivate[i]);
        }

        public bool IsKeyHeld(InputActionKey key)
        {
            if (!Active) return false;

            if (!KeysMap.Map.TryGetValue(key, out var keyCode))
                return false;

            return Input.GetKey(keyCode);
        }

        #endregion

        #region LeanTouch Integration

#if LEAN_TOUCH
        public event System.Action<Lean.Touch.LeanFinger> OnTouchDown;
        public event System.Action<Lean.Touch.LeanFinger> OnTouchUp;
        public event System.Action<Lean.Touch.LeanFinger> OnTap;
        public event System.Action<Lean.Touch.LeanFinger> OnSwipe;
        public event System.Action<System.Collections.Generic.List<Lean.Touch.LeanFinger>> OnGesture;

        public void HandleTouchDown(Lean.Touch.LeanFinger finger)
        {
            if (finger == null) return;

            OnTouchDown?.Invoke(finger);
        }

        public void HandleTouchUp(Lean.Touch.LeanFinger finger)
        {
            if (finger == null) return;

            OnTouchUp?.Invoke(finger);
        }

        public void HandleTap(Lean.Touch.LeanFinger finger)
        {
            if (finger == null) return;

            OnTap?.Invoke(finger);
        }

        public void HandleSwipe(Lean.Touch.LeanFinger finger)
        {
            if (finger == null) return;

            OnSwipe?.Invoke(finger);
        }

        public void HandleGesture(System.Collections.Generic.List<Lean.Touch.LeanFinger> fingers)
        {
            if (fingers == null || fingers.Count == 0) return;

            OnGesture?.Invoke(fingers);
        }
#endif

        #endregion
    }

    public class MousePositionData
    {
        private readonly Camera camera;

        public MousePositionData(Camera cam = null)
        {
            camera = cam ?? CameraManager.CurrentCamera;
        }

        public Vector3 World
        {
            get
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = camera.nearClipPlane;
                return camera.ScreenToWorldPoint(mousePos);
            }
        }

        public Vector3 LocalTo(Transform target)
        {
            return target.InverseTransformPoint(World);
        }

        public Vector2 Screen => Input.mousePosition;

        public Vector2 NormalizedScreen => new Vector2(Input.mousePosition.x / Screen.x, Input.mousePosition.y / Screen.y);
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

}

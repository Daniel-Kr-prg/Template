using Cinemachine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Base class for camera pack managers.
/// </summary>
public abstract class CameraManager_CameraPackBase : MonoBehaviour { }

/// <summary>
/// Manages a pack of cameras. Uses generic Key and CameraType for flexibility.
/// </summary>
public abstract class CameraManager_CameraPack<Key, CameraType> : CameraManager_CameraPackBase
{
    // Main camera component used for rendering.
    [Header("Components")]
    [SerializeField] protected Camera mainCamera;

    // ******************** Main Virtual Cameras ********************

    // Folder that holds main virtual camera GameObjects.
    [Header("Main Virtual Cameras")]
    [SerializeField] protected Transform mainVirtualCamerasFolder;

    // Dictionary storing base metadata for main cameras.
    // Key: Camera type enum; Value: Tuple (camera ID, switch callback, disable callback).
    [SerializeField] protected SerializedDictionary<CameraManager_MainCameraCameraType, (Key, Action<CameraType, CameraParamsBuilderBase<CameraType>>, Action<CameraType, CameraParamsBuilderBase<CameraType>>)> mainCameraBaseMetadata;

    // Dictionary of main virtual cameras, keyed by their ID.
    [SerializeField] protected SerializedDictionary<Key, CameraManager_CameraController<Key, CameraType>> mainVirtualCameras;

    // Prefab used to instantiate new main virtual cameras.
    [SerializeField] protected CameraManager_CameraControllerBase mainVirtualCameraPrefab;
    [SerializeField] protected string defaultMainCameraName = "mainVCamera";

    // ******************** Custom Virtual Cameras ********************

    // Folder that holds custom virtual camera GameObjects.
    [Header("Custom Virtual Cameras")]
    [SerializeField] protected Transform customVirtualCamerasFolder;

    // Dictionary of custom virtual cameras.
    [SerializeField] protected SerializedDictionary<Key, CameraManager_CameraController<Key, CameraType>> customVirtualCameras;

    // Prefab used to instantiate new custom virtual cameras.
    [SerializeField] protected CameraManager_CameraControllerBase customVirtualCameraPrefab;
    [SerializeField] protected string defaultCustomCameraName = "customVCamera";

    // Currently active camera controller.
    protected CameraManager_CameraController<Key, CameraType> activeCamera;

    // ******************** Camera Params Library ********************

    // Library of camera parameters (presets) stored as ScriptableObjects.
    [Header("Camera Params Library")]
    [SerializeField] protected SerializedDictionary<string, BaseCameraParams_SO> cameraParams;

    // Callback to set the default camera.
    protected Action DefaultCameraCallback;

    /// <summary>
    /// Set up the camera pack and optionally invoke a callback to set the default camera.
    /// </summary>
    public virtual void Setup(Action newDefaultCameraCallback = null)
    {
        SetupPack();
        if (newDefaultCameraCallback != null)
        {
            DefaultCameraCallback = newDefaultCameraCallback;
            DefaultCameraCallback.Invoke();
        }
    }

    /// <summary>
    /// Set the default camera callback.
    /// </summary>
    public void SetDefaultCameraCallback(Action newDefaultCameraCallback)
    {
        DefaultCameraCallback = newDefaultCameraCallback;
    }

    /// <summary>
    /// Set up the camera pack by updating main cameras and cleaning extra cameras.
    /// </summary>
    protected virtual void SetupPack()
    {
        // Loop through each entry in the main camera base metadata.
        foreach (var kvp in mainCameraBaseMetadata)
        {
            // kvp.Value.Item1 is the camera ID.
            // kvp.Value.Item2 is the switch callback.
            // kvp.Value.Item3 is the disable callback.
            UpdateCamera_MainCameras(kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, null);
        }
        // Remove cameras from the folder and dictionary that are not allowed.
        CleanMainCamerasListFromGarbage();
    }

    /// <summary>
    /// Switch the camera by its ID. It checks custom cameras first, then main cameras.
    /// </summary>
    public virtual void SwitchCameraByID(Key cameraID, CameraParamsBuilderBase<CameraType> switchData = null, CameraParamsBuilderBase<CameraType> disableData = null)
    {
        if (customVirtualCameras.TryGetValue(cameraID, out var cam))
        {
            activeCamera?.DisableCamera(disableData);
            cam.SwitchCamera(switchData);
        }
        else if (mainVirtualCameras.TryGetValue(cameraID, out cam))
        {
            activeCamera?.DisableCamera(disableData);
            cam.SwitchCamera(switchData);
        }
    }

    /// <summary>
    /// Update an existing camera or create a new one in the given dictionary.
    /// If a camera with the given ID exists, update it.
    /// If a new camera object is provided, replace the existing camera.
    /// Otherwise, instantiate a new camera from the prefab.
    /// </summary>
    protected virtual bool UpdateCamera(
        SerializedDictionary<Key, CameraManager_CameraController<Key, CameraType>> dictionary,
        CameraManager_CameraControllerBase prefab,
        Transform parent,
        Key cameraID,
        Action<CameraType, CameraParamsBuilderBase<CameraType>> switchCallback,
        Action<CameraType, CameraParamsBuilderBase<CameraType>> disableCallback,
        GameObject cameraObject)
    {
        // Check if the camera exists in the dictionary.
        if (dictionary.TryGetValue(cameraID, out var existingController))
        {
            if (cameraObject != null)
            {
                // If a new camera object is provided, get its controller.
                var newController = cameraObject.GetComponent<CameraManager_CameraController<Key, CameraType>>();
                if (newController != null)
                {
                    // Setup the new controller.
                    newController.Setup(cameraID, switchCallback, disableCallback);
                    newController.transform.SetParent(parent, false);

                    // Replace the old controller in the dictionary.
                    dictionary[cameraID] = newController;

                    // If the active camera was the old controller, update it.
                    if (activeCamera == existingController)
                    {
                        DefaultCameraCallback?.Invoke();
                        activeCamera = newController;
                    }

                    // Destroy the old camera object.
                    Destroy(existingController.gameObject);
                }
            }
            else
            {
                // If no new object is provided, update the existing camera.
                existingController.Setup(cameraID, switchCallback, disableCallback);
            }
            return false;
        }
        else
        {
            // Camera with this ID does not exist. Create a new one.
            CameraManager_CameraController<Key, CameraType> newController = null;
            if (cameraObject == null)
            {
                // Instantiate from the given prefab.
                var newCameraObj = Instantiate(prefab, parent);
                newController = newCameraObj.GetComponent<CameraManager_CameraController<Key, CameraType>>();
            }
            else
            {
                // Use the provided camera object.
                newController = cameraObject.GetComponent<CameraManager_CameraController<Key, CameraType>>();
                newController.transform.SetParent(parent, false);
            }
            if (newController != null)
            {
                // Setup the new controller and add it to the dictionary.
                newController.Setup(cameraID, switchCallback, disableCallback);
                newController.gameObject.name = cameraID.ToString();
                dictionary.Add(cameraID, newController);
                return true;
            }
        }
        return false;
    }

    #region Main Cameras

    /// <summary>
    /// Update a main camera using its ID. This method calls UpdateCamera with the main virtual cameras data.
    /// </summary>
    public virtual bool UpdateCamera_MainCameras(Key cameraID, Action<CameraType, CameraParamsBuilderBase<CameraType>> switchCallback = null, Action<CameraType, CameraParamsBuilderBase<CameraType>> disableCallback = null, GameObject cameraObject = null)
    {
        return UpdateCamera(mainVirtualCameras, mainVirtualCameraPrefab, mainVirtualCamerasFolder, cameraID, switchCallback, disableCallback, cameraObject);
    }

    /// <summary>
    /// Clean the main cameras list by removing extra or duplicate cameras.
    /// Only cameras with allowed IDs (from mainCameraBaseMetadata) will be kept.
    /// </summary>
    protected virtual void CleanMainCamerasListFromGarbage()
    {
        // Build a set of allowed camera IDs.
        HashSet<Key> allowedIDs = new HashSet<Key>(mainCameraBaseMetadata.Select(x => x.Value.Item1));

        // Dictionary to store the first encountered camera for each allowed ID.
        Dictionary<Key, Transform> encountered = new Dictionary<Key, Transform>();
        // List for children that need to be removed.
        List<Transform> childrenToRemove = new List<Transform>();

        // Loop through all children in the main virtual cameras folder.
        foreach (Transform child in mainVirtualCamerasFolder)
        {
            var controller = child.GetComponent<CameraManager_CameraController<Key, CameraType>>();
            if (controller == null)
            {
                childrenToRemove.Add(child);
                continue;
            }

            Key id = controller.FetchCameraID_Simple();

            // If the ID is not allowed, mark the object for removal.
            if (!allowedIDs.Contains(id))
            {
                childrenToRemove.Add(child);
            }
            else
            {
                // If a camera with this ID was already encountered, mark duplicate for removal.
                if (encountered.ContainsKey(id))
                {
                    childrenToRemove.Add(child);
                }
                else
                {
                    encountered.Add(id, child);
                }
            }
        }

        // Destroy all extra camera objects.
        foreach (Transform child in childrenToRemove)
        {
            Destroy(child.gameObject);
        }

        // Remove dictionary entries for cameras that are no longer in the folder.
        List<Key> keysToRemove = new List<Key>();
        foreach (var kvp in mainVirtualCameras)
        {
            if (!encountered.ContainsKey(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            mainVirtualCameras.Remove(key);
        }
    }

    // Abstract methods to switch different types of main cameras.
    /// <summary>
    /// Switches to a Follow Camera that follows the target.
    /// Uses parameters from a ScriptableObject found by the key.
    /// </summary>
    /// <param name="target">The object the camera will follow.</param>
    /// <param name="switchParamsKey">The key to find FollowCameraParams_SO.</param>
    /// <param name="disableParamsKey">The key of the camera to disable (optional).</param>
    public abstract void SwitchCamera_Follow(Transform target, string switchParamsKey = "", string disableParamsKey = "");

    /// <summary>
    /// Switches to a Follow Camera with the given parameters.
    /// </summary>
    /// <param name="target">The object the camera will follow.</param>
    /// <param name="cameraParams">FollowCameraParams with all required settings.</param>
    public abstract void SwitchCamera_Follow(Transform target, FollowCameraParams<CameraType> cameraParams);

    /// <summary>
    /// Switches to a Top-Down Camera using parameters from a ScriptableObject.
    /// </summary>
    public abstract void SwitchCamera_TopDown(Transform target, string paramsKey = "", string disableParamsKey = "");

    /// <summary>
    /// Switches to a Top-Down Camera with the given parameters.
    /// </summary>
    public abstract void SwitchCamera_TopDown(Transform target, TopDownCameraParams<CameraType> cameraParams);

    /// <summary>
    /// Switches to a First-Person Camera using parameters from a ScriptableObject.
    /// </summary>
    public abstract void SwitchCamera_FirstPerson(Transform target, string paramsKey = "", string disableParamsKey = "");

    /// <summary>
    /// Switches to a First-Person Camera with the given parameters.
    /// </summary>
    public abstract void SwitchCamera_FirstPerson(Transform target, FirstPersonCameraParams<CameraType> cameraParams);

    /// <summary>
    /// Switches to a Third-Person Camera using parameters from a ScriptableObject.
    /// </summary>
    public abstract void SwitchCamera_ThirdPerson(Transform target, string paramsKey = "", string disableParamsKey = "");

    /// <summary>
    /// Switches to a Third-Person Camera with the given parameters.
    /// </summary>
    public abstract void SwitchCamera_ThirdPerson(Transform target, ThirdPersonCameraParams<CameraType> cameraParams);

    /// <summary>
    /// Switches to an Isometric Camera using parameters from a ScriptableObject.
    /// </summary>
    public abstract void SwitchCamera_IsometricCamera(Transform target, string paramsKey = "", string disableParamsKey = "");

    /// <summary>
    /// Switches to an Isometric Camera with the given parameters.
    /// </summary>
    public abstract void SwitchCamera_IsometricCamera(Transform target, IsometricCameraParams<CameraType> cameraParams);

    /// <summary>
    /// Switches to a Fixed Camera using parameters from a ScriptableObject.
    /// </summary>
    public abstract void SwitchCamera_FixedCamera(string paramsKey = "", string disableParamsKey = "");

    /// <summary>
    /// Switches to a Fixed Camera with the given parameters.
    /// </summary>
    public abstract void SwitchCamera_FixedCamera(FixedCameraParams<CameraType> cameraParams);

    /// <summary>
    /// Switches to an Orbital Camera using parameters from a ScriptableObject.
    /// </summary>
    public abstract void SwitchCamera_OrbitalCamera(Transform target, string paramsKey = "", string disableParamsKey = "");

    /// <summary>
    /// Switches to an Orbital Camera with the given parameters.
    /// </summary>
    public abstract void SwitchCamera_OrbitalCamera(Transform target, OrbitalCameraParams<CameraType> cameraParams);

    #endregion

    #region Custom Cameras

    /// <summary>
    /// Update a custom camera using its ID. Similar to main cameras but uses the custom camera data.
    /// </summary>
    public virtual bool UpdateCamera_CustomCameras(Key cameraID, Action<CameraType, CameraParamsBuilderBase<CameraType>> switchCallback = null, Action<CameraType, CameraParamsBuilderBase<CameraType>> disableCallback = null, GameObject cameraObject = null)
    {
        return UpdateCamera(customVirtualCameras, customVirtualCameraPrefab, customVirtualCamerasFolder, cameraID, switchCallback, disableCallback, cameraObject);
    }

    /// <summary>
    /// Register a new custom camera with the given ID and callbacks.
    /// Returns false if the camera is already registered.
    /// </summary>
    /// 
    public virtual bool RegisterCustomCamera(Key cameraID, Action<CameraType, CameraParamsBuilderBase<CameraType>> switchCallback = null, Action<CameraType, CameraParamsBuilderBase<CameraType>> disableCallback = null)
    {
        if (customVirtualCameras.ContainsKey(cameraID)) return false;

        var newCamera = Instantiate(customVirtualCameraPrefab, customVirtualCamerasFolder).GetComponent<CameraManager_CameraController<Key, CameraType>>();
        newCamera.Setup(cameraID, switchCallback, disableCallback);
        customVirtualCameras.Add(cameraID, newCamera);

        return true;
    }

    /// <summary>
    /// Unregister a custom camera by its ID.
    /// If the active camera is unregistered, call the provided callback or the default camera callback.
    /// </summary>
    public virtual bool UnregisterCustomCamera(Key cameraID, Action onActiveCameraUnregisteringCallback = null)
    {
        if (customVirtualCameras.TryGetValue(cameraID, out var cam))
        {
            customVirtualCameras.Remove(cameraID);
            if (activeCamera == cam)
            {
                if (onActiveCameraUnregisteringCallback != null)
                {
                    onActiveCameraUnregisteringCallback.Invoke();
                }
                else
                {
                    CameraManager.Instance.DebugWarning("Unregistering active camera. Will set default camera");
                    DefaultCameraCallback?.Invoke();
                }
            }
            Destroy(cam.gameObject);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Switch a custom camera by its ID using the provided switch and disable data.
    /// </summary>
    public virtual void SwitchCamera_CustomCamera_ByID(Key cameraID, CameraParamsBuilderBase<CameraType> switchData = null, CameraParamsBuilderBase<CameraType> disableData = null)
    {
        if (customVirtualCameras.TryGetValue(cameraID, out var cam))
        {
            activeCamera?.DisableCamera(disableData);
            cam.SwitchCamera(switchData);
        }
    }

    #endregion
}

/// <summary>
/// Enum for main camera types.
/// </summary>
public enum CameraManager_MainCameraCameraType
{
    FOLLOW,
    FPS,
    TPS,
    FIXED,
    TOPDOWN,
    ISOMETRIC,
    ORBITAL
}
using Cinemachine;
using System;
using UnityEngine;

public class CameraManager : SingletonManager<CameraManager>
{
    [Serializable]
    private class CameraEntry
    {
        public string id;
        public CinemachineVirtualCamera camera;
    }

    public static Camera CurrentCamera => Instance != null && Instance.renderCamera != null ? Instance.renderCamera : Camera.main;
    public static CinemachineBrain CurrentBrain => Instance != null && Instance.cinemachineBrain != null
        ? Instance.cinemachineBrain
        : CurrentCamera != null
            ? CurrentCamera.GetComponent<CinemachineBrain>()
            : null;
    public static CinemachineVirtualCamera CurrentVirtualCamera => Instance != null ? Instance.activeVirtualCamera : null;

    [Header("Scene")]
    [SerializeField] private Camera renderCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;

    [Header("Cameras")]
    [SerializeField] private string defaultCameraID = string.Empty;
    [SerializeField] private CameraEntry[] cameras;

    private CinemachineVirtualCamera activeVirtualCamera;

    private void Start()
    {
        CacheSceneReferences();
        ResetPriorities();

        if (cameras != null && cameras.Length > 0)
        {
            SetDefaultInternal();
        }

        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_CameraManagerReady");
    }

    #region Public API

    public static void SetDefault()
    {
        if (!HaveInstance())
        {
            return;
        }

        Instance.SetDefaultInternal();
    }

    public static void SetCamera(string cameraID)
    {
        if (!HaveInstance())
        {
            return;
        }

        Instance.SetCameraInternal(cameraID, null);
    }

    public static void SetCamera(string cameraID, Transform followTarget, Transform lookAtTarget = null)
    {
        if (!HaveInstance())
        {
            return;
        }

        Instance.SetCameraInternal(cameraID, camera =>
        {
            if (followTarget != null)
            {
                camera.Follow = followTarget;
            }

            if (lookAtTarget != null)
            {
                camera.LookAt = lookAtTarget;
            }
        });
    }

    public static void SetCamera(string cameraID, Action<CinemachineVirtualCamera> setup)
    {
        if (!HaveInstance())
        {
            return;
        }

        Instance.SetCameraInternal(cameraID, setup);
    }

    public static void SetCurrentTarget(Transform followTarget, Transform lookAtTarget = null)
    {
        if (!HaveInstance() || CurrentVirtualCamera == null)
        {
            return;
        }

        if (followTarget != null)
        {
            CurrentVirtualCamera.Follow = followTarget;
        }

        if (lookAtTarget != null)
        {
            CurrentVirtualCamera.LookAt = lookAtTarget;
        }
    }

    #endregion

    #region Setup

    private void CacheSceneReferences()
    {
        renderCamera ??= Camera.main;
        cinemachineBrain ??= renderCamera != null ? renderCamera.GetComponent<CinemachineBrain>() : null;
    }

    private void ResetPriorities()
    {
        if (cameras == null)
        {
            return;
        }

        foreach (var entry in cameras)
        {
            if (entry.camera != null)
            {
                entry.camera.Priority = 0;
            }
        }
    }

    #endregion

    #region Switching

    private void SetDefaultInternal()
    {
        if (!string.IsNullOrWhiteSpace(defaultCameraID))
        {
            SetCameraInternal(defaultCameraID, null);
            return;
        }

        if (cameras == null)
        {
            return;
        }

        foreach (var entry in cameras)
        {
            if (entry.camera != null)
            {
                Activate(entry.camera, null);
                return;
            }
        }
    }

    private void SetCameraInternal(string cameraID, Action<CinemachineVirtualCamera> setup)
    {
        var camera = FindCamera(cameraID);
        if (camera == null)
        {
            DebugWarning($"Camera '{cameraID}' was not found.");
            return;
        }

        Activate(camera, setup);
    }

    private void Activate(CinemachineVirtualCamera camera, Action<CinemachineVirtualCamera> setup)
    {
        if (activeVirtualCamera != null && activeVirtualCamera != camera)
        {
            activeVirtualCamera.Priority = 0;
        }

        setup?.Invoke(camera);
        camera.Priority = 100;
        activeVirtualCamera = camera;
    }

    #endregion

    #region Helpers

    private CinemachineVirtualCamera FindCamera(string cameraID)
    {
        if (cameras == null)
        {
            return null;
        }

        foreach (var entry in cameras)
        {
            if (entry.camera == null)
            {
                continue;
            }

            if (GetEntryID(entry) == cameraID)
            {
                return entry.camera;
            }
        }

        return null;
    }

    private string GetEntryID(CameraEntry entry)
    {
        return string.IsNullOrWhiteSpace(entry.id) ? entry.camera.name : entry.id;
    }

    #endregion
}

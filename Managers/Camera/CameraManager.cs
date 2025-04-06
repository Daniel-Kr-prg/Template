using Cinemachine;
using System;
using UnityEngine;

public class CameraManager : SingletonManager<CameraManager>
{
    public static Camera CurrentCamera => Camera.main;

    [Header("Camera Packs")]
    [SerializeField] private CameraManager_CinemachinePack defaultPack;

    private void Start()
    {
        // Additional handling before stage changing
        defaultPack.gameObject.SetActive(true);
        defaultPack.SetDefaultCameraCallback(() => { });
        defaultPack.Setup();
        // Satisfy stage condition
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_CameraManagerReady");
    }

    #region CameraPacks



    #endregion

    #region Cameras

    public static void SwitchCamera(string cameraKey)
    {
        
    }

    public static void SetCamera_Following(Transform target, FollowCameraParams<CinemachineVirtualCamera> cameraParams)
    {
        Instance.defaultPack.SwitchCamera_Follow(target, cameraParams);
    }


    public static void RegisterCustomCamera(string cameraID, Action<CinemachineVirtualCamera, CameraParamsBuilderBase<CinemachineVirtualCamera>> onSwitch, Action<CinemachineVirtualCamera, CameraParamsBuilderBase<CinemachineVirtualCamera>> onDisable)
    {
        Instance.defaultPack.RegisterCustomCamera(cameraID, onSwitch, onDisable);
    }

    public static void RegisterCustomCamera(string cameraID)
    {
        Instance.defaultPack.RegisterCustomCamera(cameraID, switchCallbackBase, disableCallbackBase);
    }

    public static void UnregisterCustomCamera(string cameraID, Action onActiveUnregister = null)
    {
        Instance.defaultPack.UnregisterCustomCamera(cameraID, onActiveUnregister);
    }

    public static void SwitchCustomCamera(string cameraID, CameraParamsBuilderBase<CinemachineVirtualCamera> switchData = null, CameraParamsBuilderBase<CinemachineVirtualCamera> disableData = null)
    {
        Instance.defaultPack.SwitchCamera_CustomCamera_ByID(cameraID, switchData, disableData);
    }

    #endregion

    #region Callbacks

    public static Action<CinemachineVirtualCamera, CameraParamsBuilderBase<CinemachineVirtualCamera>> switchCallbackBase = (cam, parameters) =>
    {
        cam.Priority = 100;
        (parameters as CameraParamsBuilder_Cinemachine).Build(cam);
    };

    public static Action<CinemachineVirtualCamera, CameraParamsBuilderBase<CinemachineVirtualCamera>> disableCallbackBase = (cam, parameters) =>
    {
        cam.Priority = 0;
        (parameters as CameraParamsBuilder_Cinemachine).Build(cam);
    };

    #endregion
}

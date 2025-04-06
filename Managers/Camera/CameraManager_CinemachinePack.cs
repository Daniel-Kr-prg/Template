using Cinemachine;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager_CinemachinePack : CameraManager_CameraPack<string, CinemachineVirtualCamera>
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineBrain cinemachineBrain;

    protected override void SetupPack()
    {
        mainCameraBaseMetadata = new SerializedDictionary<CameraManager_MainCameraCameraType, (string, Action<CinemachineVirtualCamera, CameraParamsBuilderBase<CinemachineVirtualCamera>>, Action<CinemachineVirtualCamera, CameraParamsBuilderBase<CinemachineVirtualCamera>>)>()
        {
            { CameraManager_MainCameraCameraType.FOLLOW,
                (
                    "main_follow",
                    (cam, builder) => switchCallback_Follow(cam, builder as CameraParamsBuilder_Cinemachine),
                    (cam, builder) => disableCallback_Follow(cam, builder as CameraParamsBuilder_Cinemachine)
                ) 
            },
            { CameraManager_MainCameraCameraType.FPS, ("main_fps", null, null) },
            { CameraManager_MainCameraCameraType.FIXED, ("main_fixed", null, null) },
            { CameraManager_MainCameraCameraType.ORBITAL, ("main_orbital", null, null) },
            { CameraManager_MainCameraCameraType.TPS, ("main_tps", null, null) },
            { CameraManager_MainCameraCameraType.ISOMETRIC, ("main_isometric", null, null) },
            { CameraManager_MainCameraCameraType.TOPDOWN, ("main_topdown", null, null) }
        };

        base.SetupPack();

        List<GameObject> deleteList = new List<GameObject>();
        foreach (Transform child in customVirtualCamerasFolder)
        {
            var camController = child.GetComponent<CameraManager_CameraController<string, CinemachineVirtualCamera>>();
            if (camController != null)
            {
                camController.FetchCameraID((id) =>
                {
                    if (!customVirtualCameras.ContainsKey(id))
                    {
                        customVirtualCameras.Add(id, camController);
                    }
                    else if (customVirtualCameras[id] != camController)
                    {
                        deleteList.Add(camController.gameObject);
                    }
                });
            }
        }
        for (int i = -1; i >= 0; i--)
        {
            Destroy(deleteList[i]);
        }
    }


    public override void SwitchCamera_FirstPerson(Transform target, string paramsKey = "", string disableParamsKey = "")
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_FirstPerson(Transform target, FirstPersonCameraParams<CinemachineVirtualCamera> cameraParams)
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_FixedCamera(string paramsKey = "", string disableParamsKey = "")
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_FixedCamera(FixedCameraParams<CinemachineVirtualCamera> cameraParams)
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_Follow(Transform target, string paramsKey = "", string disableParamsKey = "")
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_Follow(Transform target, FollowCameraParams<CinemachineVirtualCamera> cameraParams)
    {
        if (target == null)
        {
            CameraManager.Instance.DebugError("Target is null! Can't set follow camera");
            return;
        }
        if (cameraParams == null)
            cameraParams = new FollowCameraParams<CinemachineVirtualCamera>(new CameraParamsBuilder_Cinemachine().SetFollowingObject(target));

        SwitchCameraByID(mainCameraBaseMetadata[CameraManager_MainCameraCameraType.FOLLOW].Item1, cameraParams.GetBuilder(), null);
    }

    public override void SwitchCamera_IsometricCamera(Transform target, string paramsKey = "", string disableParamsKey = "")
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_IsometricCamera(Transform target, IsometricCameraParams<CinemachineVirtualCamera> cameraParams)
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_OrbitalCamera(Transform target, string paramsKey = "", string disableParamsKey = "")
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_OrbitalCamera(Transform target, OrbitalCameraParams<CinemachineVirtualCamera> cameraParams)
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_ThirdPerson(Transform target, string paramsKey = "", string disableParamsKey = "")
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_ThirdPerson(Transform target, ThirdPersonCameraParams<CinemachineVirtualCamera> cameraParams)
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_TopDown(Transform target, string paramsKey = "", string disableParamsKey = "")
    {
        throw new System.NotImplementedException();
    }

    public override void SwitchCamera_TopDown(Transform target, TopDownCameraParams<CinemachineVirtualCamera> cameraParams)
    {
        throw new System.NotImplementedException();
    }


    #region Main Callbacks

    /// <summary>
    /// Callback for switching to the Follow camera.
    /// This callback sets up the camera's follow parameters.
    /// </summary>
    /// <param name="cam">The Cinemachine Virtual Camera to configure.</param>
    /// <param name="parameters">The base parameters, which should be cast to FollowCameraParams.</param>
    Action<CinemachineVirtualCamera, CameraParamsBuilder_Cinemachine> switchCallback_Follow = (cam, parameters) =>
    {
        if (parameters == null)
        {
            CameraManager.Instance.DebugError("FollowCameraParams not provided");
            return;
        }

        if (cam == null)
        {
            CameraManager.Instance.DebugError("CinemachineVirtualCamera is null. Cannot apply Follow Camera settings.");
            return;
        }

        cam.Priority = 100;

        parameters.Build(cam);
        //cam.Follow = followParams.FollowingObject;

        //var currentBody = cam.GetCinemachineComponent<CinemachineComponentBase>();

        //if (currentBody == null || currentBody is not CinemachineFramingTransposer)
        //{
        //    cam.DestroyCinemachineComponent<CinemachineComponentBase>();

        //    var framingTransposer = cam.AddCinemachineComponent<CinemachineFramingTransposer>();

        //    if (framingTransposer != null)
        //    {
        //        framingTransposer.m_TrackedObjectOffset = followParams.FollowOffset;
        //        framingTransposer.m_LookaheadTime = 0.2f;
        //        framingTransposer.m_LookaheadSmoothing = followParams.FollowSmoothing;
        //        framingTransposer.m_CameraDistance = 10f;
        //        framingTransposer.m_DeadZoneWidth = 0.1f;
        //        framingTransposer.m_DeadZoneHeight = 0.1f; // TODO add these params to the follow params class
        //    }
        //    else
        //    {
        //        CameraManager.Instance.DebugError("Failed to add CinemachineFramingTransposer to the camera.");
        //    }
        //}

        //CameraManager.Instance.DebugMessage($"Switched to Main Follow Camera. Following: {followParams.FollowingObject.name}");
    };

    /// <summary>
    /// Callback for disabling the Follow camera.
    /// This callback may reset or fade out camera-specific settings.
    /// </summary>
    /// <param name="cam">The Cinemachine Virtual Camera being disabled.</param>
    /// <param name="parameters">The base parameters, expected to be FollowCameraParams.</param>
    Action<CinemachineVirtualCamera, CameraParamsBuilder_Cinemachine> disableCallback_Follow = (cam, parameters) =>
    {
        cam.Priority = 0;
        parameters.Build(cam);
    };

    #endregion
}

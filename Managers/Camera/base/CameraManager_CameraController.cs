using Cinemachine;
using System;
using UnityEngine;

public abstract class CameraManager_CameraControllerBase : MonoBehaviour { }

public abstract class CameraManager_CameraController<cameraIDType, CameraType> : CameraManager_CameraControllerBase
{
    [SerializeField] private cameraIDType cameraID;
    [SerializeField] private CameraType cameraComponent;

    private Action<CameraType, CameraParamsBuilderBase<CameraType>> switchCallback;
    private Action<CameraType, CameraParamsBuilderBase<CameraType>> disableCallback;

    public cameraIDType FetchCameraID_Simple()
    {
        return cameraID;
    }

    public CameraType FetchCamera_Simple()
    {
        if (cameraComponent == null)
            cameraComponent = GetComponent<CameraType>();
        return cameraComponent;
    }

    public void FetchCameraID(Action<cameraIDType> handleDataCallback, Func<cameraIDType, bool> isCameraIDValid = null, Func<cameraIDType> cameraIDReceiver = null)
    {
        if (handleDataCallback == null)
        {
            CameraManager.Instance.DebugError("Fetching CameraID: Handle Data Action is null");
            return;
        }

        if (cameraID == null || (isCameraIDValid != null && !isCameraIDValid.Invoke(cameraID)))
        {
            if (cameraIDReceiver == null)
            {
                CameraManager.Instance.DebugError("Camera Metadata: can't receive new name");
                return;
            }
            cameraID = cameraIDReceiver.Invoke();
        }

        handleDataCallback?.Invoke(cameraID);
    }

    public void FetchCamera(Action<CameraType> handleDataCallback, Action onFailureCallback = null)
    {
        if (handleDataCallback == null)
        {
            CameraManager.Instance.DebugError("Fetching Camera: Handle Data Callback is null");
            return;
        }

        cameraComponent ??= GetComponent<CameraType>();
        if (cameraComponent == null)
        {
            onFailureCallback?.Invoke();
        }

        handleDataCallback?.Invoke(cameraComponent);
    }

    public bool SwitchCamera(CameraParamsBuilderBase<CameraType> data)
    {
        if (switchCallback == null)
            return false;

        switchCallback.Invoke(cameraComponent, data);
        return true;
    }

    public bool DisableCamera(CameraParamsBuilderBase<CameraType> data)
    {
        if (disableCallback == null)
            return false;

        disableCallback.Invoke(cameraComponent, data);
        return true;
    }

    public void Setup(cameraIDType cameraID, Action<CameraType, CameraParamsBuilderBase<CameraType>> switchCallback = null, Action<CameraType, CameraParamsBuilderBase<CameraType>> disableCallback = null)
    {
        this.cameraID = cameraID;
        cameraComponent = GetComponent<CameraType>();
        this.switchCallback = switchCallback;
        this.disableCallback = disableCallback;
    }
}

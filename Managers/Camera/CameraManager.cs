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

    public static void SetCamera_Following(Transform target, FollowCameraParams cameraParams)
    {
        Instance.defaultPack.SwitchCamera_Follow(target, cameraParams);
    }

    #endregion
}

using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : SingletonManager<UIManager>
{
    [Header("Canvas setup")]
    [SerializeField] private List<Canvas> canvasesToSetWorldCamera;

    private void Start()
    {
        ReassignCamera();

        StagesManager.Instance.AppStages.stages[AppStageName.AppInit].RegisterStageEndAction("LoadingScreen_Show", () => 
        {
            ShowLoadingScreen();
        });

        StagesManager.Instance.AppStages.stages[AppStageName.Start].RegisterStageStartAction("StartScreen_Show", () =>
        {
            ShowMainScreen();
        });

        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_UIManagerReady");
    }

    [Button("Reassign camera")]
    public void ReassignCamera()
    {
        foreach (var c in canvasesToSetWorldCamera)
        {
            if (c != null/* && c.renderMode == RenderMode.ScreenSpaceCamera*/)
                c.worldCamera = CameraManager.CurrentCamera;
        }
    }

    #region Screens

    public static void ShowLoadingScreen()
    {
        Debug.LogError("SHOW LOADING");
    }

    public static void ShowMainScreen()
    {
        Debug.LogError("SHOW START");
    }

    #endregion
}

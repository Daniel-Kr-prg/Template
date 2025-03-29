using UnityEngine;

public class UIManager : SingletonManager<UIManager>
{
    private void Start()
    {
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

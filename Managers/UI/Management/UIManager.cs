using UnityEngine;

public class UIManager : SingletonManager<UIManager>
{
    private void Start()
    {
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_UIManagerReady");
    }
}

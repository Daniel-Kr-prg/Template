using UnityEngine;
using System;
using System.Collections;

public class FirestoreSaveManager : SingletonManager<FirestoreSaveManager>
{
    public bool SaveLoaded { get; private set; } = false;
    public string LoadedData { get; private set; } = "";

    private void Start()
    {
        StagesManager.Instance.AppStages.RegisterStageStartAction(AppStageName.LoadPlayerData, "LoadPlayerData", () => {
            LoadSaveFromCloud(
                onSuccess: () => {
                    Debug.Log("[Stage] Save loaded from Firestore (fake)");
                    StagesManager.Instance.AppStages.currentStage.SatisfyCondition("SaveReady");
                },
                onFail: (err) => { Debug.LogError($"[Stage] Save load failed: {err}"); }
            );
        });
        StagesManager.Instance.AppStages.RegisterStageChangeCondition(AppStageName.LoadPlayerData, "SaveReady",
            new StageCondition(
                () => SaveLoaded,
                () => { Debug.LogError("[Stage] Не удалось загрузить сейв. Покажите ошибку пользователю."); }
            )
        );
    }

    public void LoadSaveFromCloud(Action onSuccess, Action<string> onFail)
    {
        StartCoroutine(FakeLoadCoroutine(onSuccess, onFail));
    }

    private IEnumerator FakeLoadCoroutine(Action onSuccess, Action<string> onFail)
    {
        yield return new WaitForSeconds(1.0f);
        SaveLoaded = true;
        LoadedData = "{\"currency\":100,\"bonuses\":{\"replace\":2}}";
        Debug.Log("[FirestoreSaveManager] Fake save loaded");
        onSuccess?.Invoke();
    }
} 
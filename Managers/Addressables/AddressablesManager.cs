using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class AddressablesManager : SingletonManager<AddressablesManager>
{
    private bool isInitialized = false;
    private bool readyConditionSatisfied;

    protected override void Awake()
    {
        base.Awake();
        InitializeAddressables();
    }

    private void Start()
    {
        SatisfyReadyCondition();
    }

    private void InitializeAddressables()
    {
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        var handle = Addressables.InitializeAsync();
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            isInitialized = true;
            DebugMessage("Addressables initialized successfully.");
            SatisfyReadyCondition();
        }
        else
        {
            DebugError("Addressables failed to initialize.");
        }
        yield return handle;
    }

    private void SatisfyReadyCondition()
    {
        if (readyConditionSatisfied || !isInitialized || !StagesManager.HaveInstance())
        {
            return;
        }

        readyConditionSatisfied = true;
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_AddressablesManagerReady");
    }

    public static void LoadAssetAsync<T>(string key, Action<T> onLoaded)
    {
        if (!Instance.isInitialized)
        {
            Instance.DebugWarning("Addressables not initialized yet.");
            return;
        }
        var handle = Addressables.LoadAssetAsync<T>(key);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                onLoaded?.Invoke(op.Result);
            }
            else
            {
                Instance.DebugError($"Failed to load asset with key {key}");
            }
        };
    }

    public AsyncOperationHandle<SceneInstance> LoadSceneAsync(string sceneKey, LoadSceneMode mode = LoadSceneMode.Additive)
    {
        if (!isInitialized)
        {
            DebugWarning("Addressables not initialized yet.");
            return default;
        }

        var handle = Addressables.LoadSceneAsync(sceneKey, mode);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                DebugMessage($"Scene '{sceneKey}' loaded successfully.");
            }
            else
            {
                DebugError($"Failed to load scene '{sceneKey}'.");
            }
        };
        return handle;
    }

    public void UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle)
    {
        if (!isInitialized)
        {
            DebugWarning("Addressables not initialized yet.");
            return;
        }

        Addressables.UnloadSceneAsync(sceneHandle).Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                DebugMessage("Scene unloaded successfully.");
            }
            else
            {
                DebugError("Failed to unload scene.");
            }
        };
    }
}

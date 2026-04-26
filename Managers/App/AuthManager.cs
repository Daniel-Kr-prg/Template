using UnityEngine;
using System;

public enum AuthType
{
    GooglePlay,
    AppleID,
    Test
}

public class AuthManager : SingletonManager<AuthManager>
{
    public AuthType CurrentAuthType { get; private set; } = AuthType.Test;
    public bool IsAuthenticated { get; private set; } = false;
    public string UserId { get; private set; } = "";

    public event Action OnAuthSuccess;
    public event Action<string> OnAuthFailed;

    private void Start()
    {
        StagesManager.Instance.AppStages.RegisterStageStartAction(AppStageName.ConnectServices, "Auth", () => {
            StartAuthProcess(
                onSuccess: () => {
                    Debug.Log("[Stage] Auth success");
                    StagesManager.Instance.AppStages.currentStage.SatisfyCondition("AuthReady");
                },
                onFail: (err) => { Debug.LogError($"[Stage] Auth failed: {err}"); }
            );
        });
        StagesManager.Instance.AppStages.RegisterStageChangeCondition(AppStageName.ConnectServices, "AuthReady",
            new StageCondition(
                () => IsAuthenticated,
                () => { Debug.LogError("[Stage] Не удалось авторизоваться. Покажите ошибку пользователю."); }
            )
        );
    }

    public void Authenticate()
    {
        switch (PlatformLanguageManager.Instance.Platform)
        {
            case DevicePlatform.Android:
                CurrentAuthType = AuthType.GooglePlay;
                break;
            case DevicePlatform.iOS:
                CurrentAuthType = AuthType.AppleID;
                break;
            default:
                CurrentAuthType = AuthType.Test;
                break;
        }
        TestAuth();
    }

    public void StartAuthProcess(Action onSuccess, Action<string> onFail)
    {
        void SuccessHandler()
        {
            OnAuthSuccess -= SuccessHandler;
            OnAuthFailed -= FailHandler;
            onSuccess?.Invoke();
        }
        void FailHandler(string err)
        {
            OnAuthSuccess -= SuccessHandler;
            OnAuthFailed -= FailHandler;
            onFail?.Invoke(err);
        }
        OnAuthSuccess += SuccessHandler;
        OnAuthFailed += FailHandler;
        Authenticate();
    }

    private void TestAuth()
    {
        IsAuthenticated = true;
        UserId = Guid.NewGuid().ToString();
        Debug.Log($"[AuthManager] Test auth success. UserId: {UserId}");
        OnAuthSuccess?.Invoke();
    }
} 
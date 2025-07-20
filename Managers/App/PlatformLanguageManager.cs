using UnityEngine;
using System.Globalization;

public enum DevicePlatform
{
    Editor,
    Android,
    iOS,
    Unknown
}

public class PlatformLanguageManager : SingletonManager<PlatformLanguageManager>
{
    public DevicePlatform Platform { get; private set; } = DevicePlatform.Unknown;
    public string Language { get; private set; } = "en";

    public void Detect()
    {
        DetectPlatform();
        DetectLanguage();
    }

    private void Start()
    {
        StagesManager.Instance.AppStages.RegisterStageStartAction(AppStageName.PlatformAndLanguage, "DetectPlatformAndLanguage", () => {
            Detect();
            Debug.Log($"[Stage] Platform: {Platform}, Language: {Language}");
        });
    }

    private void DetectPlatform()
    {
#if UNITY_EDITOR
        Platform = DevicePlatform.Editor;
#elif UNITY_ANDROID
        Platform = DevicePlatform.Android;
#elif UNITY_IOS
        Platform = DevicePlatform.iOS;
#else
        Platform = DevicePlatform.Unknown;
#endif
    }

    private void DetectLanguage()
    {
        Language = Application.systemLanguage.ToString();
        // Можно использовать CultureInfo.CurrentCulture.TwoLetterISOLanguageName для большей точности
    }
} 
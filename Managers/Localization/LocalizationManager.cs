using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocalizationManager : SingletonManager<LocalizationManager>
{
    private bool active = false;

    private void Start()
    {
        // Additional handling before stage changing

        // Satisfy stage condition
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_LocalizationManagerReady");
    }

    public static void SetLanguage(ConfigAvailableSettings.Language language)
    {
        Instance.SetLocale(language);
    }

    private void SetLocale(ConfigAvailableSettings.Language language)
    {
        if (active)
            return;

        StartCoroutine(SetLocaleCoroutine((int)language));
    }

    private IEnumerator SetLocaleCoroutine(int localeID)
    {
        active = true;
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
        active = false;
    }
}

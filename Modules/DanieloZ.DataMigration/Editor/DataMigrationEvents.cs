#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DataMigrationEvents
{
    static DataMigrationEvents()
    {
        //// Событие вызывается перед перезагрузкой сборки (Domain Reload),
        //// когда вы меняете скрипт и Unity собирается пересобирать/перезагружать Editor.
        //AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

        //// Событие после перезагрузки сборки.
        //AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }

    private static void OnBeforeAssemblyReload()
    {
        if (DataMigrationManager.HaveInstance())
        {
            Debug.Log("[DataMigrationEvents] BeforeAssemblyReload => CollectRecoveringData()");
            DataMigrationManager.Instance.CollectRecoveringData();
        }
    }

    private static void OnAfterAssemblyReload()
    {
        // В некоторых случаях сцена может не успеть полностью прогрузиться.
        // Если RecoverData нужно вызывать непосредственно после Domain Reload,
        // делаем это прямо тут:
        if (DataMigrationManager.HaveInstance())
        {
            Debug.Log("[DataMigrationEvents] AfterAssemblyReload => RecoverData()");
            DataMigrationManager.Instance.RecoverData();
        }

        // Если вам нужно дождаться загрузки сцен, можете вместо этого
        // подписаться на EditorApplication.update один раз, чтобы выполнить RecoverData
        // на первом кадре после Reload. Например:
        /*
        EditorApplication.update += DelayedRecover;
        */
    }

    /*
    private static void DelayedRecover()
    {
        EditorApplication.update -= DelayedRecover;
        if (DataMigrationManager.HaveInstance())
        {
            Debug.Log("[DataMigrationEvents] DelayedRecover => RecoverData()");
            DataMigrationManager.Instance.RecoverData();
        }
    }
    */
}
#endif
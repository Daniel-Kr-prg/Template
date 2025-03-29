using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-300)]
public class AppManager : SingletonManager<AppManager>
{
    //              SCENE MANAGEMENT
    [SerializeField] Scenes scenes;


    ////              APP INIT MANAGEMENT
    //[SerializeField] AppInitialization appInitialization;

    protected override void Awake()
    {
        base.Awake();
        DebugMessage("initialization");
        scenes.LoadAllScenes();
    }

    private void Start()
    {
        // Additional handling before stage changing
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_AppManagerReady");
    }

    [Serializable]
    class Scenes
    {
        [SceneName]
        [SerializeField] string[] scenes;

        public void LoadAllScenes()
        {
            foreach (var sceneName in scenes/*.Select(x => x.name)*/)
            {
                bool isLoaded = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);

                    if (scene.name == sceneName)
                        isLoaded = true;
                }

                if (!isLoaded)
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            }
        }
    }

    //[Serializable]
    //public class AppInitialization
    //{
    //    [SerializeField] List<SingletonManagerBase> managersToLoad;

    //    Action onInitCallback;
    //    bool canBeInitialized = false;

    //    public void RegisterInitializer(List<SingletonManagerBase> managers, Action onInitCallback)
    //    {
    //        if (managers != null && managers.Count > 0)
    //        {
    //            managersToLoad = managers;
    //            this.onInitCallback = onInitCallback;

    //            canBeInitialized = true;
    //        }
    //    }

    //    public void ManagerInitialized(SingletonManagerBase manager)
    //    {
    //        if (!canBeInitialized)
    //            return;

    //        if (managersToLoad.Contains(manager))
    //        {
    //            managersToLoad.Remove(manager);

    //            Debug.Log($"[INIT] {manager.name} was initialized.{managersToLoad.Count} left to initialize.");

    //            if (managersToLoad.Count == 0)
    //            {
    //                Debug.Log("[INIT] Everything is initialized. Starting...");
    //                onInitCallback.Invoke();
    //            }
    //        }
    //    }
    //}
}

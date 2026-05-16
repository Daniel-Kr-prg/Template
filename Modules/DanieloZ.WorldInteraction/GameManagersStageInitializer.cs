using System;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    [DefaultExecutionOrder(-240)]
    public sealed class GameManagersStageInitializer : MonoBehaviour
    {
        [Header("Stage")]
        [SerializeField] private AppStageName initializeOnStage = AppStageName.Start;
        [SerializeField] private bool initializeIfStageAlreadyStarted = true;

        [Header("Managers")]
        [SerializeField] private bool disableManagersBeforeStage = true;
        [SerializeField] private bool enableManagersOnInitialize = true;
        [SerializeField] private bool keepSingletonManagersEnabled = true;
        [SerializeField] private MonoBehaviour[] gameManagers;

        [Header("Callbacks")]
        [SerializeField] private string initializeMessage = "InitializeGameManager";
        [SerializeField] private UnityEvent initialized;

        [Header("Debug")]
        [SerializeField] private bool debugLogs;

        private string stageActionKey;
        private bool initializedOnce;
        private bool subscribed;

        private void Awake()
        {
            stageActionKey = $"{nameof(GameManagersStageInitializer)}_{Guid.NewGuid():N}";

            if (disableManagersBeforeStage)
            {
                SetManagersEnabled(false);
            }
        }

        private void Start()
        {
            if (!StagesManager.HaveInstance())
            {
                Debug.LogError($"[{nameof(GameManagersStageInitializer)}] StagesManager instance was not found.", this);
                return;
            }

            StagesManager.Instance.AppStages.RegisterStageStartAction(
                initializeOnStage,
                stageActionKey,
                InitializeGameManagers,
                OneTimeAction: true);
            subscribed = true;

            DebugLog($"Subscribed to stage '{initializeOnStage}'. Current stage: '{StagesManager.Instance.AppStages.currentStage?.StageName}'.");

            if (initializeIfStageAlreadyStarted && StagesManager.Instance.AppStages.currentStage?.StageName.Equals(initializeOnStage) == true)
            {
                InitializeGameManagers();
            }
        }

        private void OnDestroy()
        {
            if (!subscribed || !StagesManager.HaveInstance())
            {
                return;
            }

            StagesManager.Instance.AppStages.UnregisterStageStartAction(initializeOnStage, stageActionKey);
        }

        public void InitializeGameManagers()
        {
            if (initializedOnce)
            {
                return;
            }

            initializedOnce = true;
            DebugLog("Initializing game managers.");

            if (enableManagersOnInitialize)
            {
                SetManagersEnabled(true);
            }

            if (gameManagers != null)
            {
                foreach (var manager in gameManagers)
                {
                    if (manager == null || manager == this)
                    {
                        continue;
                    }

                    manager.SendMessage(initializeMessage, SendMessageOptions.DontRequireReceiver);
                }
            }

            initialized?.Invoke();
        }

        private void SetManagersEnabled(bool value)
        {
            if (gameManagers == null)
            {
                return;
            }

            foreach (var manager in gameManagers)
            {
                if (manager == null || manager == this)
                {
                    continue;
                }

                if (!value && keepSingletonManagersEnabled && manager is SingletonManagerBase)
                {
                    continue;
                }

                manager.enabled = value;
            }
        }

        private void DebugLog(string message)
        {
            if (!debugLogs)
            {
                return;
            }

            Debug.Log($"[{nameof(GameManagersStageInitializer)}] {message}", this);
        }
    }
}

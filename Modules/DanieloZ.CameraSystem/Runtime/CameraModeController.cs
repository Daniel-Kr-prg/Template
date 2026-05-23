using System;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.CameraSystem
{
    public sealed class CameraModeController : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Cameras")]
        [SerializeField] private CinemachineVirtualCamera gameplayCamera;
        [FoldoutGroup("Cameras")]
        [SerializeField] private CinemachineVirtualCamera mainMenuCamera;
        [FoldoutGroup("Cameras")]
        [SerializeField] private TopDownCameraController topDownCamera;
        [FoldoutGroup("Main Menu Pose")]
        [SerializeField] private Transform mainMenuCameraFollow;
        [FoldoutGroup("Main Menu Pose")]
        [SerializeField] private Transform mainMenuCameraLookAt;
        [FoldoutGroup("Priorities")]
        [SerializeField, Min(0)] private int activePriority = 100;
        [FoldoutGroup("Priorities")]
        [SerializeField, Min(0)] private int inactivePriority = 0;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onMainMenuEntered;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onMainMenuExited;

        #endregion

        #region Public API

        public bool IsMainMenuOpen { get; private set; }
        public event Action MainMenuEntered;
        public event Action MainMenuExited;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ApplyPriorities(false);
        }

        #endregion

        #region Mode Control

        public void ToggleMainMenu()
        {
            if (IsMainMenuOpen)
            {
                ExitMainMenu();
                return;
            }

            EnterMainMenu();
        }

        public void EnterMainMenu()
        {
            IsMainMenuOpen = true;
            ApplyPriorities(true);
            topDownCamera?.SetCameraPoseEnabled(false);
            ApplyMenuVirtualCameraPose();
            MainMenuEntered?.Invoke();
            onMainMenuEntered?.Invoke();
        }

        public void ExitMainMenu()
        {
            IsMainMenuOpen = false;
            ApplyPriorities(false);
            topDownCamera?.SetCameraPoseEnabled(true);
            topDownCamera?.ApplyNow();
            MainMenuExited?.Invoke();
            onMainMenuExited?.Invoke();
        }

        #endregion

        #region Helpers

        private void ApplyPriorities(bool menuActive)
        {
            if (gameplayCamera != null)
            {
                gameplayCamera.Priority = menuActive ? inactivePriority : activePriority;
            }

            if (mainMenuCamera != null)
            {
                mainMenuCamera.Priority = menuActive ? activePriority : inactivePriority;
            }
        }

        private void ApplyMenuVirtualCameraPose()
        {
            if (mainMenuCamera == null || mainMenuCameraFollow == null)
            {
                return;
            }

            var rotation = mainMenuCameraFollow.rotation;
            if (mainMenuCameraLookAt != null)
            {
                var direction = mainMenuCameraLookAt.position - mainMenuCameraFollow.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

            mainMenuCamera.Follow = null;
            mainMenuCamera.LookAt = null;
            mainMenuCamera.transform.SetPositionAndRotation(mainMenuCameraFollow.position, rotation);
        }

        #endregion
    }
}

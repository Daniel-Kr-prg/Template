using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.CameraSystem
{
    public sealed class CameraModeController : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera gameplayCamera;
        [SerializeField] private CinemachineVirtualCamera mainMenuCamera;
        [SerializeField] private TopDownCameraController topDownCamera;
        [SerializeField] private Transform mainMenuCameraFollow;
        [SerializeField] private Transform mainMenuCameraLookAt;
        [SerializeField, Min(0)] private int activePriority = 100;
        [SerializeField, Min(0)] private int inactivePriority = 0;
        [SerializeField] private UnityEvent onMainMenuEntered;
        [SerializeField] private UnityEvent onMainMenuExited;

        public bool IsMainMenuOpen { get; private set; }
        public event Action MainMenuEntered;
        public event Action MainMenuExited;

        private void Awake()
        {
            ApplyPriorities(false);
        }

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
    }
}

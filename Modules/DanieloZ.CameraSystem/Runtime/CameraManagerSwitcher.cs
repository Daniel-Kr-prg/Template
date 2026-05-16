using UnityEngine;

namespace DanieloZ.CameraSystem
{
    public sealed class CameraManagerSwitcher : MonoBehaviour
    {
        [SerializeField] private string mainCameraId = "Main";
        [SerializeField] private string gameplayCameraId = "Gameplay";
        [SerializeField] private string levelsCameraId = "Levels";
        [SerializeField] private string achievementsCameraId = "Achievements";
        [SerializeField] private string settingsCameraId = "Settings";

        [ContextMenu("Show Main")]
        public void ShowMain()
        {
            CameraManagerBridge.SetCamera(mainCameraId);
        }

        [ContextMenu("Show Gameplay")]
        public void ShowGameplay()
        {
            CameraManagerBridge.SetCamera(gameplayCameraId);
        }

        [ContextMenu("Show Levels")]
        public void ShowLevels()
        {
            CameraManagerBridge.SetCamera(levelsCameraId);
        }

        [ContextMenu("Show Achievements")]
        public void ShowAchievements()
        {
            CameraManagerBridge.SetCamera(achievementsCameraId);
        }

        [ContextMenu("Show Settings")]
        public void ShowSettings()
        {
            CameraManagerBridge.SetCamera(settingsCameraId);
        }

        public void ShowCamera(string cameraId)
        {
            CameraManagerBridge.SetCamera(cameraId);
        }

        public void ShowCamera(string cameraId, Transform followTarget, Transform lookAtTarget)
        {
            CameraManagerBridge.SetCamera(cameraId, followTarget, lookAtTarget);
        }
    }
}

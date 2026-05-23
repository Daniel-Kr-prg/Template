using Sirenix.OdinInspector;
using UnityEngine;

namespace DanieloZ.CameraSystem
{
    public sealed class CameraManagerSwitcher : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Camera Ids")]
        [SerializeField] private string mainCameraId = "Main";
        [FoldoutGroup("Camera Ids")]
        [SerializeField] private string gameplayCameraId = "Gameplay";
        [FoldoutGroup("Camera Ids")]
        [SerializeField] private string levelsCameraId = "Levels";
        [FoldoutGroup("Camera Ids")]
        [SerializeField] private string achievementsCameraId = "Achievements";
        [FoldoutGroup("Camera Ids")]
        [SerializeField] private string settingsCameraId = "Settings";

        #endregion

        #region Public API

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

        #endregion
    }
}

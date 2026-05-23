using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class CameraLock : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Lock")]
        [SerializeField] private string lockId;
        [FoldoutGroup("Lock")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [FoldoutGroup("Lock")]
        [SerializeField] private MonoBehaviour navigator;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onEntered;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onExited;

        #endregion

        #region Public API

        public string LockId => string.IsNullOrWhiteSpace(lockId) ? name : lockId;
        public CinemachineVirtualCamera VirtualCamera => virtualCamera;
        public MonoBehaviour Navigator => navigator;

        #endregion

        #region Runtime State

        private CameraLockController controller;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            controller = GetComponentInParent<CameraLockController>();
#if UNITY_2023_1_OR_NEWER
            controller ??= FindAnyObjectByType<CameraLockController>(FindObjectsInactive.Include);
#else
            controller ??= FindObjectOfType<CameraLockController>(true);
#endif
            controller?.Register(this);
        }

        private void OnDisable()
        {
            controller?.Unregister(this);
        }

        #endregion

        #region Lock Control

        public void Enter()
        {
            onEntered?.Invoke();
        }

        public void Exit()
        {
            onExited?.Invoke();
        }

        public void SetCameraPriority(int priority)
        {
            if (virtualCamera != null)
            {
                virtualCamera.Priority = priority;
            }
        }

        #endregion
    }
}

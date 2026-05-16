using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class CameraLock : MonoBehaviour
    {
        [SerializeField] private string lockId;
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private MonoBehaviour navigator;
        [SerializeField] private UnityEvent onEntered;
        [SerializeField] private UnityEvent onExited;

        public string LockId => string.IsNullOrWhiteSpace(lockId) ? name : lockId;
        public CinemachineVirtualCamera VirtualCamera => virtualCamera;
        public MonoBehaviour Navigator => navigator;

        private CameraLockController controller;

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
    }
}

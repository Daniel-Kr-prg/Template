using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.CameraSystem
{
    public sealed class CameraLockController : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Navigation")]
        [SerializeField] private MonoBehaviour defaultNavigator;
        [FoldoutGroup("Priorities")]
        [SerializeField, Min(0)] private int activePriority = 140;
        [FoldoutGroup("Priorities")]
        [SerializeField, Min(0)] private int inactivePriority = 0;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent<CameraLock> onLockActivated;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent<CameraLock> onLockExited;

        #endregion

        #region Public API

        public CameraLock CurrentLock { get; private set; }
        public MonoBehaviour CurrentNavigator => CurrentLock != null && CurrentLock.Navigator != null
            ? CurrentLock.Navigator
            : defaultNavigator;
        public IReadOnlyList<CameraLock> Locks => locks;
        public event Action<CameraLock> LockActivated;
        public event Action<CameraLock> LockExited;

        #endregion

        #region Runtime State

        private readonly List<CameraLock> locks = new();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ApplyPriorities();
        }

        #endregion

        #region Registration

        public void Register(CameraLock cameraLock)
        {
            if (cameraLock == null || locks.Contains(cameraLock))
            {
                return;
            }

            locks.Add(cameraLock);
            cameraLock.SetCameraPriority(cameraLock == CurrentLock ? activePriority : inactivePriority);
        }

        public void Unregister(CameraLock cameraLock)
        {
            if (cameraLock == null)
            {
                return;
            }

            locks.Remove(cameraLock);
            if (CurrentLock == cameraLock)
            {
                ExitCurrentLock();
            }
        }

        #endregion

        #region Lock Control

        public bool ActivateLock(string lockId)
        {
            var cameraLock = FindLock(lockId);
            if (cameraLock == null)
            {
                return false;
            }

            ActivateLock(cameraLock);
            return true;
        }

        public void ActivateLock(CameraLock cameraLock)
        {
            if (cameraLock == null)
            {
                return;
            }

            if (!locks.Contains(cameraLock))
            {
                Register(cameraLock);
            }

            if (CurrentLock == cameraLock)
            {
                ApplyPriorities();
                return;
            }

            CurrentLock?.Exit();
            CurrentLock = cameraLock;
            ApplyPriorities();
            CurrentLock.Enter();
            LockActivated?.Invoke(CurrentLock);
            onLockActivated?.Invoke(CurrentLock);
        }

        public bool SwitchToLock(string lockId)
        {
            return ActivateLock(lockId);
        }

        public void ExitCurrentLock()
        {
            if (CurrentLock == null)
            {
                return;
            }

            var previous = CurrentLock;
            CurrentLock = null;
            previous.Exit();
            ApplyPriorities();
            LockExited?.Invoke(previous);
            onLockExited?.Invoke(previous);
        }

        public CameraLock FindLock(string lockId)
        {
            if (string.IsNullOrWhiteSpace(lockId))
            {
                return null;
            }

            for (var i = 0; i < locks.Count; i++)
            {
                var cameraLock = locks[i];
                if (cameraLock != null && cameraLock.LockId == lockId)
                {
                    return cameraLock;
                }
            }

            return null;
        }

        #endregion

        #region Helpers

        private void ApplyPriorities()
        {
            for (var i = 0; i < locks.Count; i++)
            {
                var cameraLock = locks[i];
                if (cameraLock == null)
                {
                    continue;
                }

                cameraLock.SetCameraPriority(cameraLock == CurrentLock ? activePriority : inactivePriority);
            }
        }

        #endregion
    }
}

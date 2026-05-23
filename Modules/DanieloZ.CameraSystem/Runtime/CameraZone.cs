using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DanieloZ.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class CameraZone : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Zone")]
        [SerializeField] private string zoneId;
        [FoldoutGroup("Zone")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [FoldoutGroup("Zone")]
        [SerializeField] private Transform fastTravelAnchor;
        [FoldoutGroup("Zone")]
        [SerializeField] private Collider zoneVolume;
        [FoldoutGroup("Zone")]
        [SerializeField] private int priorityOffset;

        #endregion

        #region Public API

        public string ZoneId => string.IsNullOrWhiteSpace(zoneId) ? name : zoneId;
        public CinemachineVirtualCamera VirtualCamera => virtualCamera;
        public Transform FastTravelAnchor => fastTravelAnchor != null ? fastTravelAnchor : transform;
        public int PriorityOffset => priorityOffset;
        public Vector3 FastTravelPosition => FastTravelAnchor.position;
        public Quaternion FastTravelRotation => FastTravelAnchor.rotation;

        #endregion

        #region Configuration

        public void Configure(string id, CinemachineVirtualCamera camera, Collider volume, int offset, Transform anchor = null)
        {
            zoneId = id;
            virtualCamera = camera;
            fastTravelAnchor = anchor;
            zoneVolume = volume;
            priorityOffset = offset;

            if (zoneVolume != null)
            {
                zoneVolume.isTrigger = true;
            }
        }

        #endregion

        #region Runtime State

        private CameraZoneController controller;

        #endregion

        #region Unity Lifecycle

        private void Reset()
        {
            zoneVolume = GetComponent<Collider>();
            if (zoneVolume != null)
            {
                zoneVolume.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            controller = GetComponentInParent<CameraZoneController>();
#if UNITY_2023_1_OR_NEWER
            controller ??= FindAnyObjectByType<CameraZoneController>(FindObjectsInactive.Include);
#else
            controller ??= FindObjectOfType<CameraZoneController>(true);
#endif
            controller?.Register(this);
        }

        private void OnDisable()
        {
            controller?.Unregister(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            controller?.NotifyZoneEntered(this, other);
        }

        private void OnTriggerExit(Collider other)
        {
            controller?.NotifyZoneExited(this, other);
        }

        #endregion

        #region Zone Queries

        public bool ContainsWorldPoint(Vector3 point)
        {
            if (zoneVolume == null || !zoneVolume.enabled)
            {
                return false;
            }

            var closest = zoneVolume.ClosestPoint(point);
            return (closest - point).sqrMagnitude <= 0.0001f;
        }

        #endregion

        #region Camera

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

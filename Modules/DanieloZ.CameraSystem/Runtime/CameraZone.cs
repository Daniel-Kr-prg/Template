using Cinemachine;
using UnityEngine;

namespace DanieloZ.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class CameraZone : MonoBehaviour
    {
        [SerializeField] private string zoneId;
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private Transform fastTravelAnchor;
        [SerializeField] private Collider zoneVolume;
        [SerializeField] private int priorityOffset;

        public string ZoneId => string.IsNullOrWhiteSpace(zoneId) ? name : zoneId;
        public CinemachineVirtualCamera VirtualCamera => virtualCamera;
        public Transform FastTravelAnchor => fastTravelAnchor != null ? fastTravelAnchor : transform;
        public int PriorityOffset => priorityOffset;
        public Vector3 FastTravelPosition => FastTravelAnchor.position;
        public Quaternion FastTravelRotation => FastTravelAnchor.rotation;

        private CameraZoneController controller;

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

        public bool ContainsWorldPoint(Vector3 point)
        {
            if (zoneVolume == null || !zoneVolume.enabled)
            {
                return false;
            }

            var closest = zoneVolume.ClosestPoint(point);
            return (closest - point).sqrMagnitude <= 0.0001f;
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

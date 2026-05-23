using System.Collections.Generic;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DanieloZ.CameraSystem
{
    public sealed class CameraZoneController : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("References")]
        [SerializeField] private Transform groundPivot;
        [FoldoutGroup("References")]
        [SerializeField] private CinemachineVirtualCamera baseTopDownCamera;
        [FoldoutGroup("Priorities")]
        [SerializeField, Min(0)] private int basePriority = 100;
        [FoldoutGroup("Priorities")]
        [SerializeField, Min(0)] private int zonePriority = 120;
        [FoldoutGroup("Priorities")]
        [SerializeField, Min(0)] private int inactivePriority = 0;
        [FoldoutGroup("Polling")]
        [SerializeField] private bool pollGroundPivot = true;
        [FoldoutGroup("Polling")]
        [SerializeField] private bool applyBasePriorityOnStart = true;

        #endregion

        #region Public API

        public CameraZone CurrentZone { get; private set; }
        public IReadOnlyList<CameraZone> Zones => zones;

        #endregion

        #region Runtime State

        private readonly List<CameraZone> zones = new();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (applyBasePriorityOnStart)
            {
                ApplyPriorities();
            }
        }

        private void Update()
        {
            if (!pollGroundPivot || groundPivot == null)
            {
                return;
            }

            var zone = FindBestZoneAt(groundPivot.position);
            if (zone != CurrentZone)
            {
                ActivateZone(zone);
            }
        }

        #endregion

        #region Registration

        public void Register(CameraZone zone)
        {
            if (zone == null || zones.Contains(zone))
            {
                return;
            }

            zones.Add(zone);
            zone.SetCameraPriority(zone == CurrentZone ? GetActivePriority(zone) : inactivePriority);
        }

        public void Unregister(CameraZone zone)
        {
            if (zone == null)
            {
                return;
            }

            zones.Remove(zone);
            if (CurrentZone == zone)
            {
                CurrentZone = FindBestZoneAt(groundPivot != null ? groundPivot.position : transform.position);
                ApplyPriorities();
            }
        }

        #endregion

        #region Zone Notifications

        public void NotifyZoneEntered(CameraZone zone, Collider other)
        {
            if (zone == null || !IsGroundPivotCollider(other))
            {
                return;
            }

            ActivateZone(zone);
        }

        public void NotifyZoneExited(CameraZone zone, Collider other)
        {
            if (zone == null || zone != CurrentZone || !IsGroundPivotCollider(other))
            {
                return;
            }

            CurrentZone = FindBestZoneAt(groundPivot != null ? groundPivot.position : transform.position, zone);
            ApplyPriorities();
        }

        public void ActivateZone(CameraZone zone)
        {
            if (zone != null && !zones.Contains(zone))
            {
                Register(zone);
            }

            if (CurrentZone == zone)
            {
                ApplyPriorities();
                return;
            }

            CurrentZone = zone;
            ApplyPriorities();
        }

        public void DeactivateZone(CameraZone zone)
        {
            if (zone == null || CurrentZone != zone)
            {
                return;
            }

            CurrentZone = FindBestZoneAt(groundPivot != null ? groundPivot.position : transform.position, zone);
            ApplyPriorities();
        }

        #endregion

        #region Zone Control

        public bool FastTravelToZone(string zoneId)
        {
            var zone = FindZone(zoneId);
            if (zone == null || groundPivot == null)
            {
                return false;
            }

            groundPivot.SetPositionAndRotation(zone.FastTravelPosition, zone.FastTravelRotation);
            ActivateZone(zone);
            return true;
        }

        public CameraZone FindZone(string zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                return null;
            }

            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone != null && zone.ZoneId == zoneId)
                {
                    return zone;
                }
            }

            return null;
        }

        #endregion

        #region Helpers

        private void ApplyPriorities()
        {
            if (baseTopDownCamera != null)
            {
                baseTopDownCamera.Priority = basePriority;
            }

            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone == null)
                {
                    continue;
                }

                zone.SetCameraPriority(zone == CurrentZone ? GetActivePriority(zone) : inactivePriority);
            }
        }

        private int GetActivePriority(CameraZone zone)
        {
            return zonePriority + (zone != null ? zone.PriorityOffset : 0);
        }

        private CameraZone FindBestZoneAt(Vector3 position, CameraZone ignored = null)
        {
            CameraZone best = null;
            var bestPriority = int.MinValue;

            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone == null || zone == ignored || !zone.ContainsWorldPoint(position))
                {
                    continue;
                }

                var priority = GetActivePriority(zone);
                if (best != null && priority < bestPriority)
                {
                    continue;
                }

                best = zone;
                bestPriority = priority;
            }

            return best;
        }

        private bool IsGroundPivotCollider(Collider other)
        {
            if (groundPivot == null || other == null)
            {
                return false;
            }

            var otherTransform = other.transform;
            return otherTransform == groundPivot || otherTransform.IsChildOf(groundPivot);
        }

        #endregion
    }
}

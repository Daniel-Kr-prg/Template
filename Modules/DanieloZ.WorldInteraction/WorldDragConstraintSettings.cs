using System;
using Sirenix.OdinInspector;
using UnityEngine;
using OdinShowIf = Sirenix.OdinInspector.ShowIfAttribute;

namespace DanieloZ.WorldInteraction
{
    [Serializable]
    public sealed class WorldDragConstraintSettings
    {
        #region Inspector

        [SerializeField] private bool enabled;
        [OdinShowIf(nameof(enabled))]
        [SerializeField] private Transform constraintSpace;

        [OdinShowIf(nameof(enabled))]
        [TitleGroup("Allowed Axes")]
        [SerializeField] private bool allowX = true;
        [OdinShowIf(nameof(enabled))]
        [TitleGroup("Allowed Axes")]
        [SerializeField] private bool allowY = true;
        [OdinShowIf(nameof(enabled))]
        [TitleGroup("Allowed Axes")]
        [SerializeField] private bool allowZ = true;

        [OdinShowIf(nameof(enabled))]
        [TitleGroup("Ranges")]
        [SerializeField] private bool limitX;
        [OdinShowIf(nameof(ShowsXRange))]
        [TitleGroup("Ranges")]
        [SerializeField] private Vector2 xRange = new(-10f, 10f);
        [OdinShowIf(nameof(enabled))]
        [TitleGroup("Ranges")]
        [SerializeField] private bool limitY;
        [OdinShowIf(nameof(ShowsYRange))]
        [TitleGroup("Ranges")]
        [SerializeField] private Vector2 yRange = new(-10f, 10f);
        [OdinShowIf(nameof(enabled))]
        [TitleGroup("Ranges")]
        [SerializeField] private bool limitZ;
        [OdinShowIf(nameof(ShowsZRange))]
        [TitleGroup("Ranges")]
        [SerializeField] private Vector2 zRange = new(-10f, 10f);

        #endregion

        #region Runtime State

        private Vector3 lockedLocalPoint;
        private bool hasLockedPoint;

        #endregion

        #region Public API

        public bool Enabled => enabled;

        public void Begin(Vector3 worldPoint)
        {
            lockedLocalPoint = ToLocal(worldPoint);
            hasLockedPoint = true;
        }

        public Vector3 Apply(Vector3 worldPoint)
        {
            if (!enabled)
            {
                return worldPoint;
            }

            if (!hasLockedPoint)
            {
                Begin(worldPoint);
            }

            var local = ToLocal(worldPoint);

            if (!allowX)
            {
                local.x = lockedLocalPoint.x;
            }

            if (!allowY)
            {
                local.y = lockedLocalPoint.y;
            }

            if (!allowZ)
            {
                local.z = lockedLocalPoint.z;
            }

            if (limitX)
            {
                local.x = Mathf.Clamp(local.x, Mathf.Min(xRange.x, xRange.y), Mathf.Max(xRange.x, xRange.y));
            }

            if (limitY)
            {
                local.y = Mathf.Clamp(local.y, Mathf.Min(yRange.x, yRange.y), Mathf.Max(yRange.x, yRange.y));
            }

            if (limitZ)
            {
                local.z = Mathf.Clamp(local.z, Mathf.Min(zRange.x, zRange.y), Mathf.Max(zRange.x, zRange.y));
            }

            return ToWorld(local);
        }

        public void Reset()
        {
            hasLockedPoint = false;
        }

        #endregion

        #region Helpers

        private Vector3 ToLocal(Vector3 worldPoint)
        {
            return constraintSpace != null ? constraintSpace.InverseTransformPoint(worldPoint) : worldPoint;
        }

        private Vector3 ToWorld(Vector3 localPoint)
        {
            return constraintSpace != null ? constraintSpace.TransformPoint(localPoint) : localPoint;
        }

        #endregion

        #region Inspector State

        private bool ShowsXRange => enabled && limitX;
        private bool ShowsYRange => enabled && limitY;
        private bool ShowsZRange => enabled && limitZ;

        #endregion
    }
}

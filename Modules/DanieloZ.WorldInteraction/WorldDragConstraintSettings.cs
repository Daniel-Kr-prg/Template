using System;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    [Serializable]
    public sealed class WorldDragConstraintSettings
    {
        [SerializeField] private bool enabled;
        [SerializeField] private Transform constraintSpace;

        [Header("Allowed Axes")]
        [SerializeField] private bool allowX = true;
        [SerializeField] private bool allowY = true;
        [SerializeField] private bool allowZ = true;

        [Header("Ranges")]
        [SerializeField] private bool limitX;
        [SerializeField] private Vector2 xRange = new(-10f, 10f);
        [SerializeField] private bool limitY;
        [SerializeField] private Vector2 yRange = new(-10f, 10f);
        [SerializeField] private bool limitZ;
        [SerializeField] private Vector2 zRange = new(-10f, 10f);

        private Vector3 lockedLocalPoint;
        private bool hasLockedPoint;

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

        private Vector3 ToLocal(Vector3 worldPoint)
        {
            return constraintSpace != null ? constraintSpace.InverseTransformPoint(worldPoint) : worldPoint;
        }

        private Vector3 ToWorld(Vector3 localPoint)
        {
            return constraintSpace != null ? constraintSpace.TransformPoint(localPoint) : localPoint;
        }
    }
}

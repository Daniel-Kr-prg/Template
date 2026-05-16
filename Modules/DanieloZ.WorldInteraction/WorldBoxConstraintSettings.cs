using System;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    [Serializable]
    public sealed class WorldBoxConstraintSettings
    {
        [SerializeField] private bool enabled;
        [SerializeField] private Transform space;
        [SerializeField] private Vector3 center;
        [SerializeField] private Vector3 size = new(10f, 5f, 10f);

        public bool Enabled => enabled;
        public Vector3 Center => center;
        public Vector3 Size => size;
        public Transform Space => space;

        public Vector3 Clamp(Vector3 worldPoint)
        {
            if (!enabled)
            {
                return worldPoint;
            }

            var local = ToLocal(worldPoint);
            var half = Abs(size) * 0.5f;
            local.x = Mathf.Clamp(local.x, center.x - half.x, center.x + half.x);
            local.y = Mathf.Clamp(local.y, center.y - half.y, center.y + half.y);
            local.z = Mathf.Clamp(local.z, center.z - half.z, center.z + half.z);
            return ToWorld(local);
        }

        public bool Contains(Vector3 worldPoint)
        {
            if (!enabled)
            {
                return true;
            }

            var local = ToLocal(worldPoint);
            var half = Abs(size) * 0.5f;
            return local.x >= center.x - half.x
                && local.x <= center.x + half.x
                && local.y >= center.y - half.y
                && local.y <= center.y + half.y
                && local.z >= center.z - half.z
                && local.z <= center.z + half.z;
        }

        public Vector3 ClosestPoint(Vector3 worldPoint)
        {
            return Clamp(worldPoint);
        }

        public void DrawGizmos(Color color)
        {
            if (!enabled)
            {
                return;
            }

            var matrix = Gizmos.matrix;
            Gizmos.matrix = space != null ? space.localToWorldMatrix : Matrix4x4.identity;
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, Abs(size));
            Gizmos.matrix = matrix;
        }

        private Vector3 ToLocal(Vector3 worldPoint)
        {
            return space != null ? space.InverseTransformPoint(worldPoint) : worldPoint;
        }

        private Vector3 ToWorld(Vector3 localPoint)
        {
            return space != null ? space.TransformPoint(localPoint) : localPoint;
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }
    }
}

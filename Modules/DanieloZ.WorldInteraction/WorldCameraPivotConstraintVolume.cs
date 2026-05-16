using System;
using System.Collections.Generic;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public enum WorldCameraPivotConstraintMode
    {
        MoveInside,
        MoveOutside
    }

    [ExecuteAlways]
    public sealed class WorldCameraPivotConstraintVolume : MonoBehaviour
    {
        [SerializeField] private WorldCameraPivotConstraintMode mode = WorldCameraPivotConstraintMode.MoveInside;
        [SerializeField, Min(0f)] private float softDistance = 1f;
        [SerializeField, Min(0f)] private float softReturnSpeed = 8f;
        [SerializeField] private bool constrainHeight;
        [SerializeField] private float minY = -5f;
        [SerializeField] private float maxY = 5f;
        [SerializeField] private List<Vector2> polygon = new()
        {
            new Vector2(-5f, -5f),
            new Vector2(-5f, 5f),
            new Vector2(5f, 5f),
            new Vector2(5f, -5f)
        };

        [Header("Editor")]
        [SerializeField] private bool editingMode;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color hardColor = new(1f, 0.25f, 0.2f, 0.85f);
        [SerializeField] private Color softColor = new(0.2f, 0.8f, 1f, 0.55f);

        public WorldCameraPivotConstraintMode Mode => mode;
        public bool EditingMode => editingMode;
        public int PointCount => polygon?.Count ?? 0;

        public Vector3 Apply(Vector3 worldPosition, float radius, bool immediate)
        {
            if (!HasValidPolygon())
            {
                return worldPosition;
            }

            var local = transform.InverseTransformPoint(worldPosition);
            var localPoint = new Vector2(local.x, local.z);
            var inside = ContainsPolygon(localPoint);
            var closest = FindClosestEdge(localPoint);
            var distance = Mathf.Sqrt(closest.sqrDistance);
            var targetPoint = localPoint;
            var margin = Mathf.Max(0f, radius);
            var softMargin = margin + softDistance;

            if (mode == WorldCameraPivotConstraintMode.MoveInside)
            {
                if (!inside || distance < margin)
                {
                    targetPoint = closest.point - closest.outwardNormal * Mathf.Max(margin, 0.001f);
                }
                else if (softDistance > 0f && distance < softMargin)
                {
                    var softTarget = closest.point - closest.outwardNormal * softMargin;
                    targetPoint = SmoothToward(localPoint, softTarget, immediate);
                }
            }
            else
            {
                if (inside || distance < margin)
                {
                    targetPoint = closest.point + closest.outwardNormal * Mathf.Max(margin, 0.001f);
                }
                else if (softDistance > 0f && distance < softMargin)
                {
                    var softTarget = closest.point + closest.outwardNormal * softMargin;
                    targetPoint = SmoothToward(localPoint, softTarget, immediate);
                }
            }

            local.x = targetPoint.x;
            local.z = targetPoint.y;

            if (constrainHeight)
            {
                local.y = Mathf.Clamp(local.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
            }

            return transform.TransformPoint(local);
        }

        public Vector3 GetPointWorld(int index)
        {
            var point = polygon[Mathf.Clamp(index, 0, polygon.Count - 1)];
            return transform.TransformPoint(new Vector3(point.x, 0f, point.y));
        }

        public void SetPointWorld(int index, Vector3 worldPosition)
        {
            if (polygon == null || index < 0 || index >= polygon.Count)
            {
                return;
            }

            var local = transform.InverseTransformPoint(worldPosition);
            polygon[index] = new Vector2(local.x, local.z);
        }

        public void InsertPointAfter(int index)
        {
            if (polygon == null)
            {
                polygon = new List<Vector2>();
            }

            if (polygon.Count == 0)
            {
                polygon.Add(Vector2.zero);
                return;
            }

            index = Mathf.Clamp(index, 0, polygon.Count - 1);
            var nextIndex = (index + 1) % polygon.Count;
            var point = (polygon[index] + polygon[nextIndex]) * 0.5f;
            polygon.Insert(index + 1, point);
        }

        public void AddPointWorld(Vector3 worldPosition)
        {
            if (polygon == null)
            {
                polygon = new List<Vector2>();
            }

            var local = transform.InverseTransformPoint(worldPosition);
            polygon.Add(new Vector2(local.x, local.z));
        }

        public void RemovePoint(int index)
        {
            if (polygon == null || polygon.Count <= 3 || index < 0 || index >= polygon.Count)
            {
                return;
            }

            polygon.RemoveAt(index);
        }

        public void ReversePolygon()
        {
            polygon?.Reverse();
        }

        private Vector2 SmoothToward(Vector2 current, Vector2 target, bool immediate)
        {
            if (immediate || softReturnSpeed <= 0f || !Application.isPlaying)
            {
                return target;
            }

            var blend = 1f - Mathf.Exp(-softReturnSpeed * Time.deltaTime);
            return Vector2.Lerp(current, target, blend);
        }

        private bool ContainsPolygon(Vector2 point)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > point.y) == (b.y > point.y))
                {
                    continue;
                }

                var intersectionX = (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (point.x < intersectionX)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private EdgeHit FindClosestEdge(Vector2 point)
        {
            var hit = new EdgeHit
            {
                point = polygon[0],
                outwardNormal = Vector2.right,
                sqrDistance = float.PositiveInfinity
            };

            var ccw = SignedArea() >= 0f;
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                var candidate = ClosestPointOnSegment(a, b, point);
                var sqrDistance = (candidate - point).sqrMagnitude;
                if (sqrDistance >= hit.sqrDistance)
                {
                    continue;
                }

                var edge = b - a;
                var rightNormal = new Vector2(edge.y, -edge.x).normalized;
                hit.point = candidate;
                hit.outwardNormal = ccw ? rightNormal : -rightNormal;
                hit.sqrDistance = sqrDistance;
            }

            return hit;
        }

        private float SignedArea()
        {
            var area = 0f;
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        private static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 point)
        {
            var segment = b - a;
            var length = segment.sqrMagnitude;
            if (length <= Mathf.Epsilon)
            {
                return a;
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / length);
            return a + segment * t;
        }

        private bool HasValidPolygon()
        {
            return polygon != null && polygon.Count >= 3;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || !HasValidPolygon())
            {
                return;
            }

            DrawPolygon(hardColor, 0f);

            if (softDistance > 0f)
            {
                var offset = mode == WorldCameraPivotConstraintMode.MoveInside ? -softDistance : softDistance;
                DrawPolygon(softColor, offset);
            }
        }

        private void DrawPolygon(Color color, float offset)
        {
            Gizmos.color = color;
            for (var i = 0; i < polygon.Count; i++)
            {
                var current = GetOffsetPoint(i, offset);
                var next = GetOffsetPoint((i + 1) % polygon.Count, offset);
                Gizmos.DrawLine(ToWorld(current), ToWorld(next));

                if (constrainHeight)
                {
                    Gizmos.DrawLine(ToWorld(current, minY), ToWorld(current, maxY));
                    Gizmos.DrawLine(ToWorld(current, maxY), ToWorld(next, maxY));
                }
            }
        }

        private Vector2 GetOffsetPoint(int index, float offset)
        {
            if (Mathf.Approximately(offset, 0f))
            {
                return polygon[index];
            }

            var ccw = SignedArea() >= 0f;
            var previous = polygon[(index - 1 + polygon.Count) % polygon.Count];
            var current = polygon[index];
            var next = polygon[(index + 1) % polygon.Count];
            var previousNormal = GetOutwardNormal(previous, current, ccw);
            var nextNormal = GetOutwardNormal(current, next, ccw);
            var normal = (previousNormal + nextNormal).normalized;
            return current + normal * offset;
        }

        private static Vector2 GetOutwardNormal(Vector2 a, Vector2 b, bool ccw)
        {
            var edge = b - a;
            var rightNormal = new Vector2(edge.y, -edge.x).normalized;
            return ccw ? rightNormal : -rightNormal;
        }

        private Vector3 ToWorld(Vector2 point)
        {
            return transform.TransformPoint(new Vector3(point.x, 0f, point.y));
        }

        private Vector3 ToWorld(Vector2 point, float y)
        {
            return transform.TransformPoint(new Vector3(point.x, y, point.y));
        }

        private struct EdgeHit
        {
            public Vector2 point;
            public Vector2 outwardNormal;
            public float sqrDistance;
        }
    }
}

using UnityEngine;

namespace DanieloZ.CameraSystem
{
    public sealed class WorldCameraBezierCurve : MonoBehaviour
    {
        [SerializeField] private Vector3 start;
        [SerializeField] private Vector3 startHandle = new(0f, 2.5f, -2.5f);
        [SerializeField] private Vector3 endHandle = new(0f, 2.5f, -8f);
        [SerializeField] private Vector3 end = new(0f, 8f, -10f);

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField, Min(1)] private int gizmoSegments = 24;
        [SerializeField, Min(0f)] private float pointRadius = 0.08f;
        [SerializeField] private Color curveColor = new(0.2f, 0.9f, 0.25f, 0.9f);
        [SerializeField] private Color handleColor = new(1f, 1f, 1f, 0.65f);

        public Vector3 Start
        {
            get => start;
            set => start = value;
        }

        public Vector3 StartHandle
        {
            get => startHandle;
            set => startHandle = value;
        }

        public Vector3 EndHandle
        {
            get => endHandle;
            set => endHandle = value;
        }

        public Vector3 End
        {
            get => end;
            set => end = value;
        }

        public void Invert()
        {
            (start, end) = (end, start);
            (startHandle, endHandle) = (endHandle, startHandle);
        }

        public Vector3 EvaluateLocal(float t)
        {
            t = Mathf.Clamp01(t);
            var oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * oneMinusT * start
                + 3f * oneMinusT * oneMinusT * t * startHandle
                + 3f * oneMinusT * t * t * endHandle
                + t * t * t * end;
        }

        public Vector3 EvaluateWorld(float t)
        {
            return transform.TransformPoint(EvaluateLocal(t));
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            var segments = Mathf.Max(1, gizmoSegments);
            var previous = EvaluateWorld(0f);

            Gizmos.color = curveColor;
            Gizmos.DrawSphere(previous, pointRadius);

            for (var i = 1; i <= segments; i++)
            {
                var current = EvaluateWorld(i / (float)segments);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }

            Gizmos.DrawSphere(previous, pointRadius);

            var worldStart = transform.TransformPoint(start);
            var worldStartHandle = transform.TransformPoint(startHandle);
            var worldEndHandle = transform.TransformPoint(endHandle);
            var worldEnd = transform.TransformPoint(end);

            Gizmos.color = handleColor;
            Gizmos.DrawLine(worldStart, worldStartHandle);
            Gizmos.DrawLine(worldEnd, worldEndHandle);
            Gizmos.DrawWireSphere(worldStartHandle, pointRadius * 0.8f);
            Gizmos.DrawWireSphere(worldEndHandle, pointRadius * 0.8f);
        }
    }
}

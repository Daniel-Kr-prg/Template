using UnityEditor;
using UnityEngine;

namespace DanieloZ.WorldInteraction.Editor
{
    [CustomEditor(typeof(WorldCameraBezierCurve))]
    public sealed class WorldCameraBezierCurveEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Invert 0 / 1"))
            {
                var curve = (WorldCameraBezierCurve)target;
                Undo.RecordObject(curve, "Invert Camera Bezier Curve");
                curve.Invert();
                EditorUtility.SetDirty(curve);
                SceneView.RepaintAll();
            }

            EditorGUILayout.HelpBox(
                "Curve points are local to this transform. Keep X equal on all points if the camera rail should stay in the local Y/Z plane.",
                MessageType.Info);
        }

        private void OnSceneGUI()
        {
            var curve = (WorldCameraBezierCurve)target;

            EditorGUI.BeginChangeCheck();
            var start = MovePoint(curve, curve.Start, "Start");
            var startHandle = MovePoint(curve, curve.StartHandle, "Start Handle");
            var endHandle = MovePoint(curve, curve.EndHandle, "End Handle");
            var end = MovePoint(curve, curve.End, "End");

            Handles.color = Color.green;
            Handles.DrawBezier(
                curve.transform.TransformPoint(start),
                curve.transform.TransformPoint(end),
                curve.transform.TransformPoint(startHandle),
                curve.transform.TransformPoint(endHandle),
                Color.green,
                null,
                3f);

            Handles.color = Color.white;
            Handles.DrawLine(curve.transform.TransformPoint(start), curve.transform.TransformPoint(startHandle));
            Handles.DrawLine(curve.transform.TransformPoint(end), curve.transform.TransformPoint(endHandle));

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(curve, "Edit Camera Bezier Curve");
            curve.Start = start;
            curve.StartHandle = startHandle;
            curve.EndHandle = endHandle;
            curve.End = end;
            EditorUtility.SetDirty(curve);
        }

        private static Vector3 MovePoint(WorldCameraBezierCurve curve, Vector3 localPoint, string label)
        {
            var worldPoint = curve.transform.TransformPoint(localPoint);
            var size = HandleUtility.GetHandleSize(worldPoint) * 0.08f;

            Handles.color = Color.cyan;
            Handles.SphereHandleCap(0, worldPoint, Quaternion.identity, size, EventType.Repaint);
            Handles.Label(worldPoint + Vector3.up * size, label);

            var moved = Handles.PositionHandle(worldPoint, Quaternion.identity);
            return curve.transform.InverseTransformPoint(moved);
        }
    }
}

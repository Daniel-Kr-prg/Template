using UnityEditor;
using UnityEngine;

namespace DanieloZ.WorldInteraction.Editor
{
    [CustomEditor(typeof(WorldCameraPivotConstraintVolume))]
    public sealed class WorldCameraPivotConstraintVolumeEditor : UnityEditor.Editor
    {
        private int selectedPoint;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var volume = (WorldCameraPivotConstraintVolume)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Polygon Editing", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(volume.PointCount <= 0))
            {
                selectedPoint = EditorGUILayout.IntSlider("Selected Point", selectedPoint, 0, Mathf.Max(0, volume.PointCount - 1));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Insert After"))
                {
                    Undo.RecordObject(volume, "Insert Constraint Point");
                    volume.InsertPointAfter(selectedPoint);
                    EditorUtility.SetDirty(volume);
                }

                using (new EditorGUI.DisabledScope(volume.PointCount <= 3))
                {
                    if (GUILayout.Button("Remove"))
                    {
                        Undo.RecordObject(volume, "Remove Constraint Point");
                        volume.RemovePoint(selectedPoint);
                        selectedPoint = Mathf.Clamp(selectedPoint, 0, Mathf.Max(0, volume.PointCount - 1));
                        EditorUtility.SetDirty(volume);
                    }
                }
            }

            if (GUILayout.Button("Reverse Winding"))
            {
                Undo.RecordObject(volume, "Reverse Constraint Polygon");
                volume.ReversePolygon();
                EditorUtility.SetDirty(volume);
            }
        }

        private void OnSceneGUI()
        {
            var volume = (WorldCameraPivotConstraintVolume)target;
            if (!volume.EditingMode || volume.PointCount <= 0)
            {
                return;
            }

            for (var i = 0; i < volume.PointCount; i++)
            {
                var worldPoint = volume.GetPointWorld(i);
                var size = HandleUtility.GetHandleSize(worldPoint) * 0.08f;
                Handles.color = i == selectedPoint ? Color.yellow : Color.cyan;

                if (Handles.Button(worldPoint, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    selectedPoint = i;
                    Repaint();
                }

                EditorGUI.BeginChangeCheck();
                var movedPoint = Handles.PositionHandle(worldPoint, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(volume, "Move Constraint Point");
                    volume.SetPointWorld(i, movedPoint);
                    EditorUtility.SetDirty(volume);
                }

                Handles.Label(worldPoint + Vector3.up * size, i.ToString());
            }
        }
    }
}

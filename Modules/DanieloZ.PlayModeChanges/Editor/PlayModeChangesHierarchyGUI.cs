using UnityEditor;
using UnityEngine;

namespace DanieloZ.PlayModeChanges.Editor
{
    [InitializeOnLoad]
    internal static class PlayModeChangesHierarchyGUI
    {
        private static GUIStyle indicatorStyle;

        static PlayModeChangesHierarchyGUI()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= DrawHierarchyIndicator;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += DrawHierarchyIndicator;
        }

        private static void DrawHierarchyIndicator(EntityId entityId, Rect selectionRect)
        {
            if (!EditorApplication.isPlaying) return;

            GameObject gameObject = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (gameObject == null) return;

            bool queued = PlayModeChangesController.HasQueuedChanges(gameObject);
            bool changed = PlayModeChangesController.HasDetectedChanges(gameObject);
            if (!queued && !changed) return;

            indicatorStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            Rect indicatorRect = new Rect(selectionRect.xMax - 18f, selectionRect.y, 18f, selectionRect.height);
            Color previousColor = GUI.color;
            GUI.color = changed ? new Color(1f, 0.7f, 0.2f) : new Color(0.35f, 0.95f, 0.5f);
            GUI.Label(
                indicatorRect,
                new GUIContent("●", changed
                    ? queued
                        ? "Changed again after a Play Mode snapshot was queued"
                        : "Has Inspector or Gizmo changes made in Play Mode"
                    : "Has a Play Mode component snapshot queued for restore"),
                indicatorStyle);
            GUI.color = previousColor;
        }
    }
}

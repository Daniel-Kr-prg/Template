using UnityEditor;
using UnityEngine;

namespace DanieloZ.PlayModeChanges.Editor
{
    [InitializeOnLoad]
    internal static class PlayModeChangesInspectorGUI
    {
        private const string KeepMenuPath = "CONTEXT/Component/Keep Play Mode Changes";
        private const string ForceKeepMenuPath = "CONTEXT/Component/Force Keep Entire Component";
        private const string DiscardMenuPath = "CONTEXT/Component/Discard Kept Play Mode Changes";

        static PlayModeChangesInspectorGUI()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= DrawHeaderControls;
            UnityEditor.Editor.finishedDefaultHeaderGUI += DrawHeaderControls;
        }

        [MenuItem(KeepMenuPath, false, 2000)]
        private static void KeepFromContext(MenuCommand command)
        {
            if (command.context is Component component)
                PlayModeChangesController.Keep(component, false);
        }

        [MenuItem(KeepMenuPath, true)]
        private static bool ValidateKeepFromContext(MenuCommand command)
        {
            if (!(command.context is Component component)) return false;
            PlayModeComponentChangeStatus status = PlayModeChangesController.GetStatus(component);
            return status == PlayModeComponentChangeStatus.Changed ||
                   status == PlayModeComponentChangeStatus.ChangedAfterSave;
        }

        [MenuItem(ForceKeepMenuPath, false, 2010)]
        private static void ForceKeepFromContext(MenuCommand command)
        {
            if (command.context is Component component)
                PlayModeChangesController.Keep(component, true);
        }

        [MenuItem(ForceKeepMenuPath, true)]
        private static bool ValidateForceKeepFromContext(MenuCommand command)
        {
            return command.context is Component component && PlayModeChangesController.CanTrack(component);
        }

        [MenuItem(DiscardMenuPath, false, 2020)]
        private static void DiscardFromContext(MenuCommand command)
        {
            if (command.context is Component component)
                PlayModeChangesController.Discard(component);
        }

        [MenuItem(DiscardMenuPath, true)]
        private static bool ValidateDiscardFromContext(MenuCommand command)
        {
            if (!(command.context is Component component)) return false;
            PlayModeComponentChangeStatus status = PlayModeChangesController.GetStatus(component);
            return status == PlayModeComponentChangeStatus.Saved ||
                   status == PlayModeComponentChangeStatus.ChangedAfterSave;
        }

        [MenuItem("Tools/Play Mode Changes/Apply Pending Changes", false, 1000)]
        private static void ApplyPendingChanges() => PlayModeChangesController.ApplyPendingSnapshots();

        [MenuItem("Tools/Play Mode Changes/Apply Pending Changes", true)]
        private static bool ValidateApplyPendingChanges()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   PlayModeChangesController.PendingSnapshotCount > 0;
        }

        [MenuItem("Tools/Play Mode Changes/Clear Pending Changes", false, 1010)]
        private static void ClearPendingChanges()
        {
            if (!EditorUtility.DisplayDialog(
                    "Clear Play Mode Changes",
                    "Discard every pending Play Mode component snapshot?",
                    "Discard",
                    "Cancel"))
                return;

            PlayModeChangesController.ClearPendingSnapshots();
        }

        [MenuItem("Tools/Play Mode Changes/Clear Pending Changes", true)]
        private static bool ValidateClearPendingChanges() => PlayModeChangesController.PendingSnapshotCount > 0;

        private static void DrawHeaderControls(UnityEditor.Editor editor)
        {
            if (!EditorApplication.isPlaying || editor == null || editor.targets == null || editor.targets.Length != 1)
                return;
            if (!(editor.target is Component component)) return;

            PlayModeComponentChangeStatus status = PlayModeChangesController.GetStatus(component);
            if (status == PlayModeComponentChangeStatus.Unavailable) return;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.FlexibleSpace();
                DrawStatusButton(component, status);

                bool canDiscard = status == PlayModeComponentChangeStatus.Saved ||
                                  status == PlayModeComponentChangeStatus.ChangedAfterSave;
                using (new EditorGUI.DisabledScope(!canDiscard))
                {
                    var discardContent = new GUIContent("×", "Discard the queued Play Mode snapshot");
                    if (GUILayout.Button(discardContent, EditorStyles.miniButton, GUILayout.Width(24f)))
                        PlayModeChangesController.Discard(component);
                }
            }
        }

        private static void DrawStatusButton(Component component, PlayModeComponentChangeStatus status)
        {
            string label;
            string tooltip;
            bool enabled;
            Color color;

            switch (status)
            {
                case PlayModeComponentChangeStatus.Changed:
                    label = "Save";
                    tooltip = "Keep this component's current serialized state after Play Mode stops";
                    enabled = true;
                    color = new Color(1f, 0.72f, 0.22f);
                    break;
                case PlayModeComponentChangeStatus.Saved:
                    label = "✓ Saved";
                    tooltip = "This component snapshot will be restored after Play Mode stops";
                    enabled = false;
                    color = new Color(0.35f, 0.85f, 0.45f);
                    break;
                case PlayModeComponentChangeStatus.ChangedAfterSave:
                    label = "Update";
                    tooltip = "The component changed after its last snapshot; update the queued state";
                    enabled = true;
                    color = new Color(1f, 0.72f, 0.22f);
                    break;
                default:
                    label = "Save";
                    tooltip = "No Inspector or Gizmo changes detected. Use the component context menu to force a snapshot.";
                    enabled = false;
                    color = Color.white;
                    break;
            }

            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (GUILayout.Button(new GUIContent(label, tooltip), EditorStyles.miniButton, GUILayout.Width(82f)))
                    PlayModeChangesController.Keep(component, false);
            }
            GUI.backgroundColor = previousColor;
        }
    }
}

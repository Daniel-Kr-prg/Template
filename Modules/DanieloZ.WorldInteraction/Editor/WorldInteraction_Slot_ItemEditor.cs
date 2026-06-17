using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace DanieloZ.WorldInteraction.Editor
{
    [CustomEditor(typeof(WorldInteraction_Slot_Item), true)]
    public sealed class WorldInteraction_Slot_ItemEditor : OdinEditor
    {
        private int selectedTab;
        private WorldInteraction_Slot_Base previewSlot;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            selectedTab = GUILayout.Toolbar(selectedTab, new[] { "Settings", "Preview" });
            EditorGUILayout.Space(8f);

            if (selectedTab == 0)
            {
                base.OnInspectorGUI();
            }
            else
            {
                DrawPreviewTab();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPreviewTab()
        {
            var item = (WorldInteraction_Slot_Item)target;
            previewSlot = (WorldInteraction_Slot_Base)EditorGUILayout.ObjectField(
                "Preview Slot",
                previewSlot,
                typeof(WorldInteraction_Slot_Base),
                true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("insertedLocalPosition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("insertedLocalEulerRotation"));

            using (new EditorGUI.DisabledScope(previewSlot == null))
            {
                if (GUILayout.Button("Show Preview"))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.SetCurrentGroupName("Show Slot Item Preview");
                    Undo.RecordObject(item.transform, "Show Slot Item Preview");
                    item.PreviewInSlot(previewSlot);
                    EditorUtility.SetDirty(item);
                }

                if (GUILayout.Button("Capture Current Pose"))
                {
                    Undo.RecordObject(item, "Capture Slot Item Pose");
                    item.CaptureInsertedPoseFromSlot(previewSlot);
                    EditorUtility.SetDirty(item);
                    serializedObject.Update();
                }
            }
        }
    }
}

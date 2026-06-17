using System.Collections.Generic;
using DanieloZ.WorldInteraction;
using UnityEditor;
using UnityEngine;

namespace DanieloZ.WorldInteraction.Editor
{
    public static class WorldInteraction_Config_ObjectGroupsSettingsProvider
    {
        private const string ProviderPath = "Project/World Interaction";
        private const string ResourcesFolder = "Assets/Resources";
        private const string DataFolder = "Assets/Resources/Data";
        private const string SettingsAssetPath = DataFolder + "/" + WorldInteraction_Config_ObjectGroups.DefaultAssetName + ".asset";

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(ProviderPath, SettingsScope.Project)
            {
                label = "World Interaction",
                keywords = new HashSet<string>
                {
                    "World",
                    "Interaction",
                    "Slot",
                    "SlotItem",
                    "Group",
                    "Object",
                    "Disk"
                },
                guiHandler = _ => DrawSettings()
            };
        }

        private static void DrawSettings()
        {
            var settings = WorldInteraction_Config_ObjectGroups.LoadDefault();
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "WorldInteraction_Config_ObjectGroups was not found in Resources/Data.",
                    MessageType.Info);

                if (GUILayout.Button("Create Object Groups Asset"))
                {
                    settings = CreateSettingsAsset();
                }

                if (settings == null)
                {
                    return;
                }
            }

            var settingsObject = new SerializedObject(settings);
            settingsObject.Update();

            EditorGUILayout.LabelField("Object Groups", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(settingsObject.FindProperty("groupIds"), true);
            settingsObject.ApplyModifiedProperties();

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset To Defaults"))
                {
                    Undo.RecordObject(settings, "Reset World Interaction Object Groups");
                    settings.ResetToDefaults();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }

                if (GUILayout.Button("Select Settings Asset"))
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
            }
        }

        private static WorldInteraction_Config_ObjectGroups CreateSettingsAsset()
        {
            EnsureSettingsFolders();

            var settings = ScriptableObject.CreateInstance<WorldInteraction_Config_ObjectGroups>();
            settings.ResetToDefaults();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = settings;
            return settings;
        }

        private static void EnsureSettingsFolders()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                AssetDatabase.CreateFolder(ResourcesFolder, "Data");
            }
        }
    }
}

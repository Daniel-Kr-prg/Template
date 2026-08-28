using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DanieloZ.PlayModeChanges.Editor
{
    [InitializeOnLoad]
    internal static class PlayModeChangesController
    {
        private const string RegistrySessionKey = "DanieloZ.PlayModeChanges.Registry";
        private const string SnapshotsSessionKey = "DanieloZ.PlayModeChanges.Snapshots";
        private const string ApplyAfterPlayModeSessionKey = "DanieloZ.PlayModeChanges.ApplyAfterPlayMode";

        private static readonly Dictionary<EntityId, PlayModeObjectRegistration> PlayRegistrations =
            new Dictionary<EntityId, PlayModeObjectRegistration>();

        private static readonly Dictionary<string, Object> PlayObjectsByStableId =
            new Dictionary<string, Object>(StringComparer.Ordinal);

        private static readonly Dictionary<string, PlayModeComponentSnapshot> QueuedSnapshots =
            new Dictionary<string, PlayModeComponentSnapshot>(StringComparer.Ordinal);

        private static readonly HashSet<EntityId> ChangedComponentIds = new HashSet<EntityId>();
        private static readonly HashSet<EntityId> ChangedGameObjectIds = new HashSet<EntityId>();
        private static readonly HashSet<EntityId> QueuedGameObjectIds = new HashSet<EntityId>();

        static PlayModeChangesController()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.postprocessModifications += OnPostprocessModifications;

            LoadQueuedSnapshots();
            if (EditorApplication.isPlaying)
                EditorApplication.delayCall += RebuildPlayModeMap;
        }

        // ==== Public API ====
        public static PlayModeComponentChangeStatus GetStatus(Component component)
        {
            if (!CanTrack(component)) return PlayModeComponentChangeStatus.Unavailable;

            PlayModeObjectRegistration registration = PlayRegistrations[component.GetEntityId()];
            bool queued = QueuedSnapshots.ContainsKey(registration.StableObjectId);
            bool changed = ChangedComponentIds.Contains(component.GetEntityId());

            if (queued) return changed
                ? PlayModeComponentChangeStatus.ChangedAfterSave
                : PlayModeComponentChangeStatus.Saved;
            return changed ? PlayModeComponentChangeStatus.Changed : PlayModeComponentChangeStatus.Unchanged;
        }

        public static bool CanTrack(Component component)
        {
            return EditorApplication.isPlaying &&
                   component != null &&
                   PlayRegistrations.ContainsKey(component.GetEntityId());
        }

        public static bool Keep(Component component, bool force)
        {
            if (!CanTrack(component)) return false;
            if (!force && GetStatus(component) == PlayModeComponentChangeStatus.Unchanged) return false;

            PlayModeObjectRegistration registration = PlayRegistrations[component.GetEntityId()];
            PlayModeComponentSnapshot snapshot = CaptureSnapshot(component, registration);
            QueuedSnapshots[registration.StableObjectId] = snapshot;
            ChangedComponentIds.Remove(component.GetEntityId());
            RefreshChangedGameObject(component.gameObject);
            SaveQueuedSnapshots();
            RebuildQueuedGameObjectIds();
            RepaintIndicators();
            return true;
        }

        public static bool Discard(Component component)
        {
            if (!CanTrack(component)) return false;

            PlayModeObjectRegistration registration = PlayRegistrations[component.GetEntityId()];
            if (!QueuedSnapshots.Remove(registration.StableObjectId)) return false;

            RefreshComponentChangeState(component);
            SaveQueuedSnapshots();
            RebuildQueuedGameObjectIds();
            RepaintIndicators();
            return true;
        }

        public static bool HasDetectedChanges(GameObject gameObject)
        {
            return EditorApplication.isPlaying && gameObject != null &&
                   ChangedGameObjectIds.Contains(gameObject.GetEntityId());
        }

        public static bool HasQueuedChanges(GameObject gameObject)
        {
            return EditorApplication.isPlaying && gameObject != null &&
                   QueuedGameObjectIds.Contains(gameObject.GetEntityId());
        }

        public static int PendingSnapshotCount => QueuedSnapshots.Count;

        public static void ApplyPendingSnapshots()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!SessionState.GetBool(ApplyAfterPlayModeSessionKey, false) && QueuedSnapshots.Count == 0) return;

            LoadQueuedSnapshots();
            if (QueuedSnapshots.Count == 0)
            {
                SessionState.EraseBool(ApplyAfterPlayModeSessionKey);
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Keep Play Mode Changes");

            int appliedCount = 0;
            var unresolved = new Dictionary<string, PlayModeComponentSnapshot>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, PlayModeComponentSnapshot> entry in QueuedSnapshots)
            {
                if (TryApplySnapshot(entry.Value)) appliedCount++;
                else unresolved.Add(entry.Key, entry.Value);
            }

            Undo.CollapseUndoOperations(undoGroup);
            QueuedSnapshots.Clear();
            foreach (KeyValuePair<string, PlayModeComponentSnapshot> entry in unresolved)
                QueuedSnapshots.Add(entry.Key, entry.Value);

            SaveQueuedSnapshots();
            SessionState.EraseBool(ApplyAfterPlayModeSessionKey);

            if (appliedCount > 0)
                Debug.Log($"[Play Mode Changes] Applied {appliedCount} component snapshot(s). Scenes are dirty and can be reverted with Undo.");
            if (unresolved.Count > 0)
                Debug.LogWarning($"[Play Mode Changes] Could not resolve {unresolved.Count} component snapshot(s). They remain pending.");
        }

        public static void ClearPendingSnapshots()
        {
            QueuedSnapshots.Clear();
            SessionState.EraseBool(ApplyAfterPlayModeSessionKey);
            SaveQueuedSnapshots();
            RebuildQueuedGameObjectIds();
            RepaintIndicators();
        }

        #region Unity Lifecycle
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    CaptureEditModeRegistry();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    RebuildPlayModeMap();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    SaveQueuedSnapshots();
                    SessionState.SetBool(ApplyAfterPlayModeSessionKey, QueuedSnapshots.Count > 0);
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    ClearPlayModeState();
                    EditorApplication.delayCall += SchedulePendingSnapshotApply;
                    break;
            }
        }

        private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (!EditorApplication.isPlaying) return modifications;

            bool changed = false;
            for (int index = 0; index < modifications.Length; index++)
            {
                Object target = modifications[index].currentValue?.target;
                if (!(target is Component component) || !CanTrack(component)) continue;

                RefreshComponentChangeState(component);
                changed = true;
            }

            if (changed) RepaintIndicators();
            return modifications;
        }
        #endregion

        #region Registry
        private static void CaptureEditModeRegistry()
        {
            var registry = new PlayModeObjectRegistry();
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path)) continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                    RegisterHierarchy(registry, roots[rootIndex], scene.path, new List<int> { rootIndex }, roots[rootIndex].name);
            }

            SessionState.SetString(RegistrySessionKey, JsonUtility.ToJson(registry));
        }

        private static void RegisterHierarchy(
            PlayModeObjectRegistry registry,
            GameObject gameObject,
            string scenePath,
            List<int> siblingPath,
            string displayPath)
        {
            AddRegistration(registry, gameObject, scenePath, siblingPath, -1, displayPath);

            Component[] components = gameObject.GetComponents<Component>();
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                Component component = components[componentIndex];
                if (component == null || component.hideFlags != HideFlags.None) continue;
                AddRegistration(registry, component, scenePath, siblingPath, componentIndex, displayPath);
            }

            Transform transform = gameObject.transform;
            for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
            {
                Transform child = transform.GetChild(childIndex);
                var childPath = new List<int>(siblingPath) { childIndex };
                RegisterHierarchy(registry, child.gameObject, scenePath, childPath, displayPath + "/" + child.name);
            }
        }

        private static void AddRegistration(
            PlayModeObjectRegistry registry,
            Object target,
            string scenePath,
            List<int> siblingPath,
            int componentIndex,
            string displayPath)
        {
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(target);
            if (id.identifierType == 0) return;

            registry.Objects.Add(new PlayModeObjectRegistration
            {
                StableObjectId = id.ToString(),
                ScenePath = scenePath,
                SiblingPath = siblingPath.ToArray(),
                ComponentIndex = componentIndex,
                ObjectType = target.GetType().AssemblyQualifiedName,
                DisplayPath = displayPath,
                BaselineSignature = target is Component component ? CalculateSignature(component) : string.Empty
            });
        }

        private static void RebuildPlayModeMap()
        {
            if (!EditorApplication.isPlaying) return;

            PlayRegistrations.Clear();
            PlayObjectsByStableId.Clear();
            ChangedComponentIds.Clear();
            ChangedGameObjectIds.Clear();

            string json = SessionState.GetString(RegistrySessionKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            PlayModeObjectRegistry registry = JsonUtility.FromJson<PlayModeObjectRegistry>(json);
            if (registry?.Objects == null) return;

            for (int index = 0; index < registry.Objects.Count; index++)
            {
                PlayModeObjectRegistration registration = registry.Objects[index];
                Object playObject = ResolvePlayObject(registration);
                if (playObject == null) continue;

                PlayRegistrations[playObject.GetEntityId()] = registration;
                PlayObjectsByStableId[registration.StableObjectId] = playObject;
            }

            LoadQueuedSnapshots();
            RebuildQueuedGameObjectIds();
            RepaintIndicators();
        }

        private static Object ResolvePlayObject(PlayModeObjectRegistration registration)
        {
            if (registration?.SiblingPath == null || registration.SiblingPath.Length == 0) return null;

            Scene scene = SceneManager.GetSceneByPath(registration.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return null;

            GameObject[] roots = scene.GetRootGameObjects();
            int rootIndex = registration.SiblingPath[0];
            if (rootIndex < 0 || rootIndex >= roots.Length) return null;

            Transform current = roots[rootIndex].transform;
            for (int pathIndex = 1; pathIndex < registration.SiblingPath.Length; pathIndex++)
            {
                int childIndex = registration.SiblingPath[pathIndex];
                if (childIndex < 0 || childIndex >= current.childCount) return null;
                current = current.GetChild(childIndex);
            }

            if (registration.ComponentIndex < 0) return current.gameObject;

            Component[] components = current.GetComponents<Component>();
            if (registration.ComponentIndex < components.Length)
            {
                Component indexed = components[registration.ComponentIndex];
                if (indexed != null && indexed.GetType().AssemblyQualifiedName == registration.ObjectType) return indexed;
            }

            Component fallback = null;
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component == null || component.GetType().AssemblyQualifiedName != registration.ObjectType) continue;
                if (fallback != null) return null;
                fallback = component;
            }
            return fallback;
        }
        #endregion

        #region Snapshots
        private static PlayModeComponentSnapshot CaptureSnapshot(
            Component component,
            PlayModeObjectRegistration registration)
        {
            var snapshot = new PlayModeComponentSnapshot
            {
                StableComponentId = registration.StableObjectId,
                ScenePath = registration.ScenePath,
                ComponentType = component.GetType().AssemblyQualifiedName,
                DisplayPath = registration.DisplayPath + " (" + component.GetType().Name + ")",
                SerializedJson = EditorJsonUtility.ToJson(component, false),
                Signature = CalculateSignature(component)
            };

            var serialized = new SerializedObject(component);
            serialized.Update();
            SerializedProperty iterator = serialized.GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                snapshot.ObjectReferences.Add(CaptureObjectReference(iterator));
            }

            return snapshot;
        }

        private static PlayModeObjectReferenceSnapshot CaptureObjectReference(SerializedProperty property)
        {
            Object reference = property.objectReferenceValue;
            var snapshot = new PlayModeObjectReferenceSnapshot { PropertyPath = property.propertyPath };

            if (reference == null)
            {
                snapshot.Kind = PlayModeObjectReferenceKind.Null;
                return snapshot;
            }

            if (PlayRegistrations.TryGetValue(reference.GetEntityId(), out PlayModeObjectRegistration registration))
            {
                snapshot.Kind = PlayModeObjectReferenceKind.StableObject;
                snapshot.StableObjectId = registration.StableObjectId;
                return snapshot;
            }

            if (EditorUtility.IsPersistent(reference))
            {
                GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(reference);
                if (id.identifierType != 0)
                {
                    snapshot.Kind = PlayModeObjectReferenceKind.StableObject;
                    snapshot.StableObjectId = id.ToString();
                    return snapshot;
                }
            }

            snapshot.Kind = PlayModeObjectReferenceKind.UnsupportedRuntimeObject;
            return snapshot;
        }

        private static bool TryApplySnapshot(PlayModeComponentSnapshot snapshot)
        {
            if (!TryResolveStableObject(snapshot.StableComponentId, out Object resolved) || !(resolved is Component component))
            {
                Debug.LogWarning($"[Play Mode Changes] Component is unavailable: {snapshot.DisplayPath}");
                return false;
            }

            if (component.GetType().AssemblyQualifiedName != snapshot.ComponentType)
            {
                Debug.LogWarning($"[Play Mode Changes] Component type changed: {snapshot.DisplayPath}");
                return false;
            }

            var preservedReferences = new Dictionary<string, Object>(StringComparer.Ordinal);
            var before = new SerializedObject(component);
            before.Update();
            for (int index = 0; index < snapshot.ObjectReferences.Count; index++)
            {
                PlayModeObjectReferenceSnapshot reference = snapshot.ObjectReferences[index];
                SerializedProperty property = before.FindProperty(reference.PropertyPath);
                if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
                    preservedReferences[reference.PropertyPath] = property.objectReferenceValue;
            }

            Undo.RecordObject(component, "Keep Play Mode Changes");
            EditorJsonUtility.FromJsonOverwrite(snapshot.SerializedJson, component);

            var serialized = new SerializedObject(component);
            serialized.Update();
            for (int index = 0; index < snapshot.ObjectReferences.Count; index++)
            {
                PlayModeObjectReferenceSnapshot reference = snapshot.ObjectReferences[index];
                SerializedProperty property = serialized.FindProperty(reference.PropertyPath);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference) continue;

                switch (reference.Kind)
                {
                    case PlayModeObjectReferenceKind.Null:
                        property.objectReferenceValue = null;
                        break;
                    case PlayModeObjectReferenceKind.StableObject:
                        if (TryResolveStableObject(reference.StableObjectId, out Object value))
                            property.objectReferenceValue = value;
                        else if (preservedReferences.TryGetValue(reference.PropertyPath, out Object unresolvedFallback))
                            property.objectReferenceValue = unresolvedFallback;
                        break;
                    case PlayModeObjectReferenceKind.UnsupportedRuntimeObject:
                        if (preservedReferences.TryGetValue(reference.PropertyPath, out Object preserved))
                            property.objectReferenceValue = preserved;
                        break;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorUtility.SetDirty(component);
            if (component.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            return true;
        }

        private static bool TryResolveStableObject(string stableObjectId, out Object value)
        {
            value = null;
            if (string.IsNullOrEmpty(stableObjectId) || !GlobalObjectId.TryParse(stableObjectId, out GlobalObjectId id))
                return false;

            value = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
            return value != null;
        }

        private static void LoadQueuedSnapshots()
        {
            QueuedSnapshots.Clear();
            string json = SessionState.GetString(SnapshotsSessionKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            PlayModeSnapshotCollection collection = JsonUtility.FromJson<PlayModeSnapshotCollection>(json);
            if (collection?.Components == null) return;
            for (int index = 0; index < collection.Components.Count; index++)
            {
                PlayModeComponentSnapshot snapshot = collection.Components[index];
                if (snapshot == null || string.IsNullOrEmpty(snapshot.StableComponentId)) continue;
                QueuedSnapshots[snapshot.StableComponentId] = snapshot;
            }
        }

        private static void SaveQueuedSnapshots()
        {
            var collection = new PlayModeSnapshotCollection();
            foreach (PlayModeComponentSnapshot snapshot in QueuedSnapshots.Values)
                collection.Components.Add(snapshot);

            if (collection.Components.Count == 0) SessionState.EraseString(SnapshotsSessionKey);
            else SessionState.SetString(SnapshotsSessionKey, JsonUtility.ToJson(collection));
        }
        #endregion

        #region Private Methods
        private static void RebuildQueuedGameObjectIds()
        {
            QueuedGameObjectIds.Clear();
            foreach (string stableId in QueuedSnapshots.Keys)
            {
                if (!PlayObjectsByStableId.TryGetValue(stableId, out Object value) || !(value is Component component))
                    continue;
                QueuedGameObjectIds.Add(component.gameObject.GetEntityId());
            }
        }

        private static void RefreshChangedGameObject(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component != null && ChangedComponentIds.Contains(component.GetEntityId()))
                {
                    ChangedGameObjectIds.Add(gameObject.GetEntityId());
                    return;
                }
            }

            ChangedGameObjectIds.Remove(gameObject.GetEntityId());
        }

        private static void RefreshComponentChangeState(Component component)
        {
            if (!PlayRegistrations.TryGetValue(component.GetEntityId(), out PlayModeObjectRegistration registration))
                return;

            string comparisonSignature = QueuedSnapshots.TryGetValue(
                registration.StableObjectId,
                out PlayModeComponentSnapshot queued)
                ? queued.Signature
                : registration.BaselineSignature;

            if (CalculateSignature(component) == comparisonSignature)
                ChangedComponentIds.Remove(component.GetEntityId());
            else
                ChangedComponentIds.Add(component.GetEntityId());

            RefreshChangedGameObject(component.gameObject);
        }

        private static string CalculateSignature(Component component)
        {
            var builder = new StringBuilder(EditorJsonUtility.ToJson(component, false));
            var serialized = new SerializedObject(component);
            serialized.Update();
            SerializedProperty iterator = serialized.GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;

                builder.Append('|').Append(iterator.propertyPath).Append('=');
                Object reference = iterator.objectReferenceValue;
                if (reference == null)
                {
                    builder.Append("null");
                    continue;
                }

                if (EditorApplication.isPlaying &&
                    PlayRegistrations.TryGetValue(reference.GetEntityId(), out PlayModeObjectRegistration registration))
                {
                    builder.Append(registration.StableObjectId);
                    continue;
                }

                if (!EditorApplication.isPlaying || EditorUtility.IsPersistent(reference))
                {
                    GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(reference);
                    if (id.identifierType != 0)
                    {
                        builder.Append(id);
                        continue;
                    }
                }

                builder.Append("runtime:").Append(reference.GetType().AssemblyQualifiedName).Append(':')
                    .Append(reference.GetEntityId().ToString());
            }

            return Hash128.Compute(builder.ToString()).ToString();
        }

        private static void ClearPlayModeState()
        {
            PlayRegistrations.Clear();
            PlayObjectsByStableId.Clear();
            ChangedComponentIds.Clear();
            ChangedGameObjectIds.Clear();
            QueuedGameObjectIds.Clear();
            EditorApplication.RepaintHierarchyWindow();
            SessionState.EraseString(RegistrySessionKey);
        }

        private static void SchedulePendingSnapshotApply()
        {
            EditorApplication.delayCall += ApplyPendingSnapshots;
        }

        private static void RepaintIndicators()
        {
            EditorApplication.RepaintHierarchyWindow();
            SceneView.RepaintAll();
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;

namespace DanieloZ.PlayModeChanges.Editor
{
    [Serializable]
    internal sealed class PlayModeObjectRegistry
    {
        public List<PlayModeObjectRegistration> Objects = new List<PlayModeObjectRegistration>();
    }

    [Serializable]
    internal sealed class PlayModeObjectRegistration
    {
        public string StableObjectId;
        public string ScenePath;
        public int[] SiblingPath;
        public int ComponentIndex;
        public string ObjectType;
        public string DisplayPath;
        public string BaselineSignature;
    }

    [Serializable]
    internal sealed class PlayModeSnapshotCollection
    {
        public List<PlayModeComponentSnapshot> Components = new List<PlayModeComponentSnapshot>();
    }

    [Serializable]
    internal sealed class PlayModeComponentSnapshot
    {
        public string StableComponentId;
        public string ScenePath;
        public string ComponentType;
        public string DisplayPath;
        public string SerializedJson;
        public string Signature;
        public List<PlayModeObjectReferenceSnapshot> ObjectReferences =
            new List<PlayModeObjectReferenceSnapshot>();
    }

    [Serializable]
    internal sealed class PlayModeObjectReferenceSnapshot
    {
        public string PropertyPath;
        public PlayModeObjectReferenceKind Kind;
        public string StableObjectId;
    }

    internal enum PlayModeObjectReferenceKind
    {
        Null,
        StableObject,
        UnsupportedRuntimeObject
    }

    internal enum PlayModeComponentChangeStatus
    {
        Unavailable,
        Unchanged,
        Changed,
        Saved,
        ChangedAfterSave
    }
}

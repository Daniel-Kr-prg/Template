using System.Collections.Generic;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteraction_Outline_State
    {
        private readonly List<Renderer> renderers = new();
        private readonly Dictionary<GameObject, int> originalLayers = new();
        private bool isVisible;

        public bool IsVisible => isVisible;

        public void Show(
            Transform rendererRoot,
            string selectionLayerName,
            bool includeInactive,
            Object context)
        {
            SetVisible(true, rendererRoot, selectionLayerName, includeInactive, context);
        }

        public void Hide()
        {
            SetVisible(false, null, null, false, null);
        }

        public void SetVisible(
            bool visible,
            Transform rendererRoot,
            string selectionLayerName,
            bool includeInactive,
            Object context)
        {
            if (isVisible == visible)
            {
                return;
            }

            if (visible)
            {
                RefreshTargets(rendererRoot, includeInactive);
                var selectionLayer = LayerMask.NameToLayer(selectionLayerName);
                if (selectionLayer < 0)
                {
                    Debug.LogWarning($"Selection layer '{selectionLayerName}' is not configured.", context);
                    return;
                }

                for (var i = 0; i < renderers.Count; i++)
                {
                    var target = renderers[i] != null ? renderers[i].gameObject : null;
                    if (target == null)
                    {
                        continue;
                    }

                    if (!originalLayers.ContainsKey(target))
                    {
                        originalLayers[target] = target.layer;
                    }

                    target.layer = selectionLayer;
                }
            }
            else
            {
                foreach (var pair in originalLayers)
                {
                    if (pair.Key != null)
                    {
                        pair.Key.layer = pair.Value;
                    }
                }

                originalLayers.Clear();
                renderers.Clear();
            }

            isVisible = visible;
        }

        private void RefreshTargets(Transform rendererRoot, bool includeInactive)
        {
            renderers.Clear();
            if (rendererRoot == null)
            {
                return;
            }

            rendererRoot.GetComponentsInChildren(includeInactive, renderers);
        }
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("DanieloZ/World Interaction/Selection Layer Hover Outline")]
    public sealed class WorldInteraction_Outline_Hover : MonoBehaviour
    {
        // ==== Inspector ====

        [Header("References")]
        [SerializeField] private Transform rendererRoot;

        [Header("Selection")]
        [SerializeField] private string selectionLayerName = "Outline";
        [SerializeField] private bool includeInactive;

        [Header("Editor Preview")]
        [SerializeField, Tooltip("Shows this selection outline in Scene View outside Play Mode.")]
        private bool showInSceneEditor;

        // ==== Public API ====

        public void ConfigureRuntime(Transform runtimeRendererRoot, string runtimeSelectionLayerName)
        {
            rendererRoot = runtimeRendererRoot != null ? runtimeRendererRoot : rendererRoot;
            if (!string.IsNullOrWhiteSpace(runtimeSelectionLayerName))
            {
                selectionLayerName = runtimeSelectionLayerName;
            }

            RefreshTargets();
        }

        public void Show()
        {
            SetVisible(true, false);
        }

        public void Hide()
        {
            SetVisible(false, false);
        }

        #region Private Fields

        private readonly WorldInteraction_Outline_State outlineState = new();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            RefreshTargets();
        }

        private void OnDisable()
        {
            SetVisible(false, false);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(selectionLayerName))
            {
                selectionLayerName = "Outline";
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall -= ApplySceneEditorPreview;
                UnityEditor.EditorApplication.delayCall += ApplySceneEditorPreview;
            }
#endif
        }

        #endregion

        #region Private Methods

#if UNITY_EDITOR
        private void ApplySceneEditorPreview()
        {
            UnityEditor.EditorApplication.delayCall -= ApplySceneEditorPreview;

            if (this == null || Application.isPlaying)
            {
                return;
            }

            SetVisible(showInSceneEditor, true);
        }
#endif

        private void SetVisible(bool visible, bool editorPreview)
        {
            if (outlineState.IsVisible == visible && !(editorPreview && !visible))
            {
                return;
            }

            if (visible)
            {
                outlineState.Show(rendererRoot != null ? rendererRoot : transform, selectionLayerName, includeInactive, this);
            }
            else
            {
                outlineState.Hide();
            }
        }

        private void RefreshTargets()
        {
            if (outlineState.IsVisible)
            {
                outlineState.Hide();
                outlineState.Show(rendererRoot != null ? rendererRoot : transform, selectionLayerName, includeInactive, this);
            }
        }

        #endregion
    }
}

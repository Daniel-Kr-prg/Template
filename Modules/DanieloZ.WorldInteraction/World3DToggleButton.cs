using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public class World3DToggleButton : World3DButtonBase, IWorldUsable
    {
        #region Inspector

        [FoldoutGroup("Toggle")]
        [SerializeField] private bool allowToggleOff = true;
        [FoldoutGroup("Toggle")]
        [SerializeField] private bool useLegacyOnMouseDown;

        [FoldoutGroup("Press")]
        [SerializeField] private Transform pressTransform;
        [FoldoutGroup("Press")]
        [SerializeField] private Vector3 localPressOffset = new(0f, -0.08f, 0f);
        [FoldoutGroup("Press")]
        [SerializeField, Min(0f)] private float pressInDuration = 0.08f;
        [FoldoutGroup("Press")]
        [SerializeField, Min(0f)] private float releaseDuration = 0.12f;
        [FoldoutGroup("Press")]
        [SerializeField] private Ease pressEase = Ease.OutCubic;

        [FoldoutGroup("Visual State")]
        [SerializeField] private Renderer stateRenderer;
        [FoldoutGroup("Visual State")]
        [SerializeField] private Material offMaterial;
        [FoldoutGroup("Visual State")]
        [SerializeField] private Material onMaterial;
        [FoldoutGroup("Visual State")]
        [SerializeField] private GameObject offVisual;
        [FoldoutGroup("Visual State")]
        [SerializeField] private GameObject onVisual;

        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onPressed;

        #endregion

        #region Public API

        public event Action<World3DToggleButton, bool> Toggled;

        #endregion

        #region Runtime State

        private readonly World3DPressAnimation pressAnimation = new();

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (pressTransform == null)
            {
                pressTransform = transform;
            }

            pressAnimation.Initialize(pressTransform);
            ApplyVisualState();
        }

        private void OnMouseDown()
        {
            if (useLegacyOnMouseDown)
            {
                Press();
            }
        }

        protected virtual void OnDisable()
        {
            pressAnimation.Kill();
        }

        #endregion

        #region Interaction

        public void Use(WorldInteractionContext context)
        {
            Press();
        }

        public void Press()
        {
            if (!Interactable)
            {
                return;
            }

            pressAnimation.Play(localPressOffset, pressInDuration, releaseDuration, pressEase, HandlePressed);
        }

        public void Toggle()
        {
            if (IsActive && !allowToggleOff)
            {
                SetActiveState(true);
                return;
            }

            SetActiveState(!IsActive);
        }

        public override void SetActiveState(bool active)
        {
            var wasActive = IsActive;
            base.SetActiveState(active);
            ApplyVisualState();

            if (wasActive != IsActive)
            {
                Toggled?.Invoke(this, IsActive);
            }
        }

        #endregion

        #region Helpers

        private void HandlePressed()
        {
            onPressed?.Invoke();
            Toggle();
        }

        private void ApplyVisualState()
        {
            if (stateRenderer != null)
            {
                var material = IsActive ? onMaterial : offMaterial;
                if (material != null)
                {
                    stateRenderer.sharedMaterial = material;
                }
            }

            if (offVisual != null)
            {
                offVisual.SetActive(!IsActive);
            }

            if (onVisual != null)
            {
                onVisual.SetActive(IsActive);
            }
        }

        #endregion
    }
}

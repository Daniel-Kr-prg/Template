using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public sealed class World3DToggleButton : World3DButtonBase, IWorldUsable
    {
        [Header("Toggle")]
        [SerializeField] private bool allowToggleOff = true;
        [SerializeField] private bool useLegacyOnMouseDown;

        [Header("Press")]
        [SerializeField] private Transform pressTransform;
        [SerializeField] private Vector3 localPressOffset = new(0f, -0.08f, 0f);
        [SerializeField, Min(0f)] private float pressInDuration = 0.08f;
        [SerializeField, Min(0f)] private float releaseDuration = 0.12f;
        [SerializeField] private Ease pressEase = Ease.OutCubic;

        [Header("Visual State")]
        [SerializeField] private Renderer stateRenderer;
        [SerializeField] private Material offMaterial;
        [SerializeField] private Material onMaterial;
        [SerializeField] private GameObject offVisual;
        [SerializeField] private GameObject onVisual;

        [Header("Events")]
        [SerializeField] private UnityEvent onPressed;

        private Vector3 initialLocalPosition;
        private Tween pressTween;

        public event Action<World3DToggleButton, bool> Toggled;

        private void Awake()
        {
            if (pressTransform == null)
            {
                pressTransform = transform;
            }

            initialLocalPosition = pressTransform.localPosition;
            ApplyVisualState();
        }

        private void OnMouseDown()
        {
            if (useLegacyOnMouseDown)
            {
                Press();
            }
        }

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

            pressTween?.Kill();
            pressTween = DOTween.Sequence()
                .Append(pressTransform.DOLocalMove(initialLocalPosition + localPressOffset, pressInDuration).SetEase(pressEase))
                .AppendCallback(HandlePressed)
                .Append(pressTransform.DOLocalMove(initialLocalPosition, releaseDuration).SetEase(pressEase));
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

        private void OnDisable()
        {
            pressTween?.Kill();
        }
    }
}

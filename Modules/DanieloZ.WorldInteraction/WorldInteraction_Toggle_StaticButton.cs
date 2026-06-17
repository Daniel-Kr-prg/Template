using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public sealed class WorldInteraction_Toggle_StaticButton : MonoBehaviour, IWorldInteraction_Press_LifecycleTarget
    {
        [SerializeField] private string toggleId;
        [SerializeField] private bool interactable = true;
        [SerializeField] private bool isOn;
        [SerializeField] private bool allowToggleOff;
        [SerializeField] private Transform pressTransform;
        [SerializeField] private Vector3 localPressOffset = new(0f, -0.08f, 0f);
        [SerializeField, Min(0f)] private float pressInDuration = 0.08f;
        [SerializeField, Min(0f)] private float releaseDuration = 0.12f;
        [SerializeField] private Ease pressEase = Ease.OutCubic;
        [SerializeField] private Renderer stateRenderer;
        [SerializeField] private Material offMaterial;
        [SerializeField] private Material onMaterial;
        [SerializeField] private UnityEvent onTurnedOn;
        [SerializeField] private UnityEvent onTurnedOff;

        private Vector3 initialLocalPosition;
        private Tween pressTween;

        public string ToggleId => string.IsNullOrWhiteSpace(toggleId) ? name : toggleId;
        public bool IsOn => isOn;
        public bool Interactable
        {
            get => interactable;
            set => interactable = value;
        }

        public WorldInteraction_Toggle_StaticGroup Group { get; private set; }
        public event Action<WorldInteraction_Toggle_StaticButton, bool> Toggled;

        private void Awake()
        {
            pressTransform ??= transform;
            initialLocalPosition = pressTransform.localPosition;
            ApplyVisualState();
        }

        public void SetGroup(WorldInteraction_Toggle_StaticGroup group)
        {
            Group = group;
        }

        public bool CanPress(WorldInteraction_Pointer_Context context)
        {
            return enabled && gameObject.activeInHierarchy && interactable;
        }

        public void Press(WorldInteraction_Pointer_Context context)
        {
            Press();
        }

        public void BeginPress(WorldInteraction_Pointer_Context context)
        {
            if (!CanPress(context) || pressTransform == null)
            {
                return;
            }

            pressTween?.Kill();
            pressTween = pressTransform.DOLocalMove(initialLocalPosition + localPressOffset, pressInDuration).SetEase(pressEase);
        }

        public void EndPress(WorldInteraction_Pointer_Context context, bool activate)
        {
            if (activate && CanPress(context))
            {
                HandlePressed();
            }

            if (pressTransform == null)
            {
                return;
            }

            pressTween?.Kill();
            pressTween = pressTransform.DOLocalMove(initialLocalPosition, releaseDuration).SetEase(pressEase);
        }

        public void Press()
        {
            if (!interactable)
            {
                return;
            }

            pressTween?.Kill();
            pressTween = DOTween.Sequence()
                .Append(pressTransform.DOLocalMove(initialLocalPosition + localPressOffset, pressInDuration).SetEase(pressEase))
                .AppendCallback(HandlePressed)
                .Append(pressTransform.DOLocalMove(initialLocalPosition, releaseDuration).SetEase(pressEase));
        }

        public void SetOn(bool active)
        {
            SetOn(active, true);
        }

        public void SetOnSilently(bool active)
        {
            SetOn(active, false);
        }

        private void SetOn(bool active, bool notifyGroup)
        {
            if (isOn == active)
            {
                ApplyVisualState();
                return;
            }

            isOn = active;
            ApplyVisualState();

            if (isOn)
            {
                onTurnedOn?.Invoke();
            }
            else
            {
                onTurnedOff?.Invoke();
            }

            if (notifyGroup)
            {
                Toggled?.Invoke(this, isOn);
                Group?.NotifyToggleChanged(this, isOn);
            }
        }

        private void HandlePressed()
        {
            if (Group != null)
            {
                Group.SetActiveToggle(this);
                return;
            }

            if (isOn && !allowToggleOff)
            {
                SetOn(true);
                return;
            }

            SetOn(!isOn);
        }

        private void ApplyVisualState()
        {
            if (stateRenderer == null)
            {
                return;
            }

            var material = isOn ? onMaterial : offMaterial;
            if (material != null)
            {
                stateRenderer.sharedMaterial = material;
            }
        }

        private void OnDisable()
        {
            pressTween?.Kill();
        }
    }
}

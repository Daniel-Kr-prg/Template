using System;
using DanieloZ.Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteraction_Control_Slider : MonoBehaviour
    {
        #region Types

        private enum SliderAxis
        {
            X,
            Y,
            Z
        }

        [Serializable]
        public sealed class SliderValueEvent : UnityEvent<float>
        {
        }

        #endregion

        #region Inspector

        [FoldoutGroup("Handle")]
        [SerializeField] private Transform handleTransform;
        [FoldoutGroup("Handle")]
        [SerializeField] private Transform trackSpace;
        [FoldoutGroup("Handle")]
        [EnumToggleButtons]
        [SerializeField] private SliderAxis axis = SliderAxis.X;

        [FoldoutGroup("Range")]
        [SerializeField] private float minLocalPosition = -0.5f;
        [FoldoutGroup("Range")]
        [SerializeField] private float maxLocalPosition = 0.5f;
        [FoldoutGroup("Range")]
        [SerializeField, Range(0f, 1f)] private float value;
        [FoldoutGroup("Range")]
        [SerializeField, Min(0f)] private float step;
        [FoldoutGroup("Range")]
        [SerializeField] private bool setHandlePositionOnAwake = true;

        [FoldoutGroup("Input")]
        [SerializeField, Min(0.0001f)] private float normalizedValuePerMouseUnit = 0.01f;
        [FoldoutGroup("Input")]
        [SerializeField, Min(0f)] private float dragFollowSpeed = 24f;
        [FoldoutGroup("Input")]
        [SerializeField] private bool invertMouseDelta;

        [FoldoutGroup("Events")]
        [SerializeField] private SliderValueEvent onValueChanged;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onMinValue;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onMaxValue;

        #endregion

        #region Public API

        public event Action<WorldInteraction_Control_Slider, float> ValueChanged;

        public float Value => value;

        private Transform Space => trackSpace != null ? trackSpace : transform;

        #endregion

        #region Runtime State

        private bool isAtMin;
        private bool isAtMax;
        private bool isPointerDeltaDragging;
        private float pointerDeltaTargetValue;

        #endregion

        #region Unity Lifecycle

        private void Reset()
        {
            handleTransform = transform.Find("Handle");
            trackSpace = transform;
        }

        private void Awake()
        {
            if (setHandlePositionOnAwake)
            {
                SetValue(value, false);
            }
            else
            {
                SetValue(ReadValueFromHandle(), false, false);
            }

            pointerDeltaTargetValue = value;
        }

        private void Update()
        {
            if (!isPointerDeltaDragging)
            {
                return;
            }

            var nextValue = dragFollowSpeed <= 0f
                ? pointerDeltaTargetValue
                : Mathf.Lerp(value, pointerDeltaTargetValue, 1f - Mathf.Exp(-dragFollowSpeed * Time.deltaTime));
            SetValue(nextValue, true, true, false);
        }

        #endregion

        #region Value API

        public void SetValue(float newValue)
        {
            SetValue(newValue, true);
        }

        public void SetValueSilently(float newValue)
        {
            SetValue(newValue, false);
        }

        public void BeginPointerDeltaDrag()
        {
            isPointerDeltaDragging = true;
            pointerDeltaTargetValue = value;
            MoveHandleToValue(value);
        }

        public void ApplyPointerDelta(float horizontalDelta)
        {
            if (!isPointerDeltaDragging)
            {
                return;
            }

            var direction = invertMouseDelta ? -1f : 1f;
            pointerDeltaTargetValue = Mathf.Clamp01(pointerDeltaTargetValue + horizontalDelta * normalizedValuePerMouseUnit * direction);
        }

        public void EndPointerDeltaDrag()
        {
            isPointerDeltaDragging = false;
            pointerDeltaTargetValue = value;
            MoveHandleToValue(value);
        }

        public bool TryGetHandleScreenPosition(Camera camera, out Vector2 screenPosition)
        {
            screenPosition = default;
            if (camera == null || handleTransform == null)
            {
                return false;
            }

            var projected = camera.WorldToScreenPoint(handleTransform.position);
            if (projected.z <= 0f)
            {
                return false;
            }

            screenPosition = projected;
            return true;
        }

        #endregion

        #region Value Evaluation

        private void SetValue(float newValue, bool notify, bool moveHandle = true)
        {
            SetValue(newValue, notify, moveHandle, true);
        }

        private void SetValue(float newValue, bool notify, bool moveHandle, bool quantize)
        {
            var clamped = quantize ? Quantize(Mathf.Clamp01(newValue)) : Mathf.Clamp01(newValue);
            if (Mathf.Approximately(value, clamped))
            {
                if (moveHandle)
                {
                    MoveHandleToValue(clamped);
                }

                return;
            }

            value = clamped;
            if (moveHandle)
            {
                MoveHandleToValue(value);
            }

            UpdateEdgeEvents();

            if (notify)
            {
                onValueChanged?.Invoke(value);
                ValueChanged?.Invoke(this, value);
                if (EventManager.HaveInstance())
                {
                    EventManager.CallEvent(EventName.WorldInteraction_OnSliderValueChanged, new object[] { this, value });
                }
            }
        }

        private float ReadValueFromHandle()
        {
            if (handleTransform == null)
            {
                return value;
            }

            var local = Space.InverseTransformPoint(handleTransform.position);
            var component = GetAxisValue(local);
            return Mathf.InverseLerp(minLocalPosition, maxLocalPosition, component);
        }

        #endregion

        #region Handle Position

        private void MoveHandleToValue(float normalizedValue)
        {
            if (handleTransform == null)
            {
                return;
            }

            var local = Space.InverseTransformPoint(handleTransform.position);
            SetAxisValue(ref local, Mathf.Lerp(minLocalPosition, maxLocalPosition, normalizedValue));
            handleTransform.position = Space.TransformPoint(local);
        }

        #endregion

        #region Helpers

        private float Quantize(float normalizedValue)
        {
            if (step <= 0f)
            {
                return normalizedValue;
            }

            return Mathf.Clamp01(Mathf.Round(normalizedValue / step) * step);
        }

        private void UpdateEdgeEvents()
        {
            var atMin = value <= 0.0001f;
            var atMax = value >= 0.9999f;

            if (atMin && !isAtMin)
            {
                onMinValue?.Invoke();
            }

            if (atMax && !isAtMax)
            {
                onMaxValue?.Invoke();
            }

            isAtMin = atMin;
            isAtMax = atMax;
        }

        private float GetAxisValue(Vector3 value)
        {
            return axis switch
            {
                SliderAxis.Y => value.y,
                SliderAxis.Z => value.z,
                _ => value.x
            };
        }

        private void SetAxisValue(ref Vector3 vector, float newValue)
        {
            switch (axis)
            {
                case SliderAxis.Y:
                    vector.y = newValue;
                    break;
                case SliderAxis.Z:
                    vector.z = newValue;
                    break;
                default:
                    vector.x = newValue;
                    break;
            }
        }

        #endregion
    }
}

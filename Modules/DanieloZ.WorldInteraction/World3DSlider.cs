using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public sealed class World3DSlider : MonoBehaviour, IWorldDraggableReleaseHandler
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
        [SerializeField] private WorldDraggable handle;
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
        [FoldoutGroup("Range")]
        [SerializeField] private bool clampHandleEveryFrame = true;

        [FoldoutGroup("Events")]
        [SerializeField] private SliderValueEvent onValueChanged;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onMinValue;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onMaxValue;

        #endregion

        #region Public API

        public event Action<World3DSlider, float> ValueChanged;

        public float Value => value;
        public WorldDraggable Handle => handle;

        private Transform Space => trackSpace != null ? trackSpace : transform;

        #endregion

        #region Runtime State

        private bool isAtMin;
        private bool isAtMax;

        #endregion

        #region Unity Lifecycle

        private void Reset()
        {
            handle = GetComponentInChildren<WorldDraggable>();
            handleTransform = handle != null ? handle.transform : null;
            trackSpace = transform;
        }

        private void Awake()
        {
            if (handleTransform == null && handle != null)
            {
                handleTransform = handle.transform;
            }

            if (setHandlePositionOnAwake)
            {
                SetValue(value, false);
            }
            else
            {
                SetValue(ReadValueFromHandle(), false, false);
            }
        }

        private void Update()
        {
            if (handleTransform == null)
            {
                return;
            }

            SetValue(ReadValueFromHandle(), true, false);

            if (clampHandleEveryFrame)
            {
                MoveHandleToValue(value);
            }
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

        public bool TrySetValueFromRay(Ray ray)
        {
            return TrySetValueFromRay(ray, true);
        }

        public bool TrySetValueFromRay(Ray ray, bool notify)
        {
            if (handleTransform == null)
            {
                return false;
            }

            var localPosition = GetClosestLocalPointOnSliderAxis(ray);
            var axisValue = GetAxisValue(localPosition);
            SetValue(Mathf.InverseLerp(minLocalPosition, maxLocalPosition, axisValue), notify);
            return true;
        }

        public bool TryReleaseDraggedObject(WorldDraggable draggable, WorldDragReleaseContext context)
        {
            if (handle == null || draggable != handle)
            {
                return false;
            }

            if (context.HasScreenPosition && context.Camera != null)
            {
                TrySetValueFromRay(context.Camera.ScreenPointToRay(context.ScreenPosition), true);
            }

            MoveHandleToValue(value);
            handle.Release();

            var body = handle.Body;
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            return true;
        }

        #endregion

        #region Value Evaluation

        private void SetValue(float newValue, bool notify, bool moveHandle = true)
        {
            var clamped = Quantize(Mathf.Clamp01(newValue));
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

        private Vector3 GetClosestLocalPointOnSliderAxis(Ray ray)
        {
            var startLocal = Space.InverseTransformPoint(handleTransform.position);
            SetAxisValue(ref startLocal, minLocalPosition);

            var endLocal = startLocal;
            SetAxisValue(ref endLocal, maxLocalPosition);

            var start = Space.TransformPoint(startLocal);
            var end = Space.TransformPoint(endLocal);
            var axisDirection = end - start;
            var axisLength = axisDirection.magnitude;
            if (axisLength <= 0.0001f)
            {
                return startLocal;
            }

            axisDirection /= axisLength;
            var rayDirection = ray.direction.normalized;
            var fromRayToAxis = start - ray.origin;
            var axisDotRay = Vector3.Dot(axisDirection, rayDirection);
            var axisDotOffset = Vector3.Dot(axisDirection, fromRayToAxis);
            var rayDotOffset = Vector3.Dot(rayDirection, fromRayToAxis);
            var denominator = 1f - axisDotRay * axisDotRay;

            var distanceAlongAxis = denominator > 0.0001f
                ? (axisDotRay * rayDotOffset - axisDotOffset) / denominator
                : Vector3.Dot(axisDirection, ray.origin - start);

            distanceAlongAxis = Mathf.Clamp(distanceAlongAxis, 0f, axisLength);
            return Space.InverseTransformPoint(start + axisDirection * distanceAlongAxis);
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

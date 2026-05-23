using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public sealed class World3DOptionRoller : MonoBehaviour, IWorldPointerDraggable
    {
        #region Types

        [Serializable]
        public sealed class StringEvent : UnityEvent<string>
        {
        }

        [Serializable]
        public sealed class IndexEvent : UnityEvent<int>
        {
        }

        #endregion

        #region Inspector

        [FoldoutGroup("Options")]
        [SerializeField] private List<string> options = new();
        [FoldoutGroup("Options")]
        [SerializeField, Min(0)] private int selectedIndex;

        [FoldoutGroup("View")]
        [SerializeField] private Transform rollerTransform;
        [FoldoutGroup("View")]
        [SerializeField] private TMP_Text selectedLabel;
        [FoldoutGroup("View")]
        [SerializeField] private TMP_Text[] faceLabels;
        [FoldoutGroup("View")]
        [SerializeField] private Vector3 localRotationAxis = Vector3.right;

        [FoldoutGroup("Interaction")]
        [SerializeField] private bool interactable = true;
        [FoldoutGroup("Interaction")]
        [SerializeField, Min(0.01f)] private float degreesPerScreenPixel = 0.35f;
        [FoldoutGroup("Interaction")]
        [SerializeField, Min(0.01f)] private float snapSpeed = 16f;
        [FoldoutGroup("Interaction")]
        [SerializeField, Range(0f, 45f)] private float bottomRefreshAngleTolerance = 10f;
        [FoldoutGroup("Interaction")]
        [SerializeField] private bool invertDrag;

        [FoldoutGroup("Events")]
        [SerializeField] private IndexEvent onIndexChanged;
        [FoldoutGroup("Events")]
        [SerializeField] private StringEvent onOptionChanged;

        #endregion

        #region Public API

        public IReadOnlyList<string> Options => options;
        public int SelectedIndex => selectedIndex;
        public string SelectedOption => HasOptions ? options[selectedIndex] : string.Empty;
        public event Action<World3DOptionRoller, int> IndexChanged;
        public bool Interactable
        {
            get => interactable;
            set => interactable = value;
        }

        #endregion

        #region Runtime State

        private const float DegreesPerStep = 90f;

        private bool isDragging;
        private float currentAngle;
        private float targetAngle;
        private Vector2 previousScreenPosition;
        private int selectedStep;
        private int labelBaseStep;
        private int dragStartSelectedIndex;

        private bool HasOptions => options != null && options.Count > 0;
        private Transform RollerTransform => rollerTransform != null ? rollerTransform : transform;

        #endregion

        #region Unity Lifecycle

        private void Reset()
        {
            rollerTransform = transform;
        }

        private void Awake()
        {
            NormalizeSelection();
            selectedStep = selectedIndex;
            labelBaseStep = selectedStep;
            currentAngle = StepToAngle(selectedStep);
            targetAngle = currentAngle;
            ApplyRotation(currentAngle);
            RefreshLabels();
        }

        private void Update()
        {
            if (isDragging)
            {
                return;
            }

            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * snapSpeed);
            ApplyRotation(currentAngle);
        }

        private void OnValidate()
        {
            NormalizeSelection();
            selectedStep = selectedIndex;
            labelBaseStep = selectedStep;
            targetAngle = StepToAngle(selectedStep);
            currentAngle = targetAngle;
            ApplyRotation(currentAngle);
            RefreshLabels();
        }

        #endregion

        #region Pointer Drag

        public bool BeginPointerDrag(WorldInteractionContext context)
        {
            if (!interactable || !HasOptions)
            {
                return false;
            }

            isDragging = true;
            dragStartSelectedIndex = selectedIndex;
            previousScreenPosition = context.ScreenPosition;
            return true;
        }

        public void UpdatePointerDrag(WorldInteractionContext context)
        {
            if (!isDragging || !HasOptions)
            {
                return;
            }

            var delta = context.ScreenPosition - previousScreenPosition;
            var direction = invertDrag ? -1f : 1f;
            currentAngle += delta.y * degreesPerScreenPixel * direction;
            previousScreenPosition = context.ScreenPosition;
            ApplyRotation(currentAngle);

            var nearestStep = AngleToStep(currentAngle);
            if (ShouldRefreshLabelsAtStep(nearestStep) && nearestStep != labelBaseStep)
            {
                selectedStep = nearestStep;
                selectedIndex = NormalizeIndex(nearestStep);
                labelBaseStep = nearestStep;
                RefreshLabels();
            }
        }

        public void EndPointerDrag(WorldInteractionContext context)
        {
            if (!isDragging)
            {
                return;
            }

            var finalStep = AngleToStep(currentAngle);
            var finalIndex = NormalizeIndex(finalStep);
            var changed = finalIndex != dragStartSelectedIndex;

            isDragging = false;
            SetSelectedStep(finalStep, finalIndex, false);
            currentAngle = targetAngle;
            ApplyRotation(currentAngle);

            if (changed)
            {
                NotifySelectionChanged();
            }
        }

        public void CancelPointerDrag()
        {
            isDragging = false;
            selectedStep = ClosestStepForIndex(selectedIndex);
            labelBaseStep = selectedStep;
            targetAngle = StepToAngle(selectedStep);
            currentAngle = targetAngle;
            ApplyRotation(currentAngle);
            RefreshLabels();
        }

        #endregion

        #region Selection API

        public void SetOptions(IReadOnlyList<string> newOptions)
        {
            options = newOptions != null ? new List<string>(newOptions) : new List<string>();
            SetSelectedIndex(0, true);
        }

        public void SetSelectedIndex(int index)
        {
            SetSelectedIndex(index, true);
        }

        public void SelectNext()
        {
            SetSelectedIndex(selectedIndex + 1, true);
        }

        public void SelectPrevious()
        {
            SetSelectedIndex(selectedIndex - 1, true);
        }

        #endregion

        #region Selection Internals

        private void SetSelectedIndex(int index, bool notify)
        {
            if (!HasOptions)
            {
                selectedIndex = 0;
                RefreshLabels();
                return;
            }

            var normalized = NormalizeIndex(index);
            var step = isDragging ? AngleToStep(currentAngle) : ClosestStepForIndex(normalized);
            var changed = normalized != selectedIndex;
            SetSelectedStep(step, normalized, false);

            if (!isDragging)
            {
                currentAngle = targetAngle;
                ApplyRotation(currentAngle);
            }

            RefreshLabels();
            if (notify && changed)
            {
                NotifySelectionChanged();
            }
        }

        private void NormalizeSelection()
        {
            selectedIndex = HasOptions ? NormalizeIndex(selectedIndex) : 0;
        }

        private int NormalizeIndex(int index)
        {
            if (!HasOptions)
            {
                return 0;
            }

            var count = options.Count;
            return ((index % count) + count) % count;
        }

        private int AngleToStep(float angle)
        {
            return Mathf.RoundToInt(-angle / DegreesPerStep);
        }

        private float StepToAngle(int step)
        {
            return -step * DegreesPerStep;
        }

        private int ClosestStepForIndex(int index)
        {
            if (!HasOptions)
            {
                return 0;
            }

            var normalized = NormalizeIndex(index);
            var current = selectedStep;
            var currentIndex = NormalizeIndex(current);
            var forward = NormalizeIndex(normalized - currentIndex);
            var backward = forward - options.Count;
            var offset = Mathf.Abs(backward) < Mathf.Abs(forward) ? backward : forward;
            return current + offset;
        }

        private void SetSelectedStep(int step, int index, bool refresh)
        {
            selectedStep = step;
            selectedIndex = NormalizeIndex(index);
            labelBaseStep = selectedStep;
            targetAngle = StepToAngle(selectedStep);

            if (refresh)
            {
                RefreshLabels();
            }
        }

        private void NotifySelectionChanged()
        {
            onIndexChanged?.Invoke(selectedIndex);
            onOptionChanged?.Invoke(SelectedOption);
            IndexChanged?.Invoke(this, selectedIndex);
        }

        private bool ShouldRefreshLabelsAtStep(int step)
        {
            var target = StepToAngle(step);
            var distance = Mathf.Abs(Mathf.DeltaAngle(currentAngle, target));
            return distance <= bottomRefreshAngleTolerance;
        }

        #endregion

        #region View

        private void ApplyRotation(float angle)
        {
            if (RollerTransform == null)
            {
                return;
            }

            var axis = localRotationAxis.sqrMagnitude > 0.0001f ? localRotationAxis.normalized : Vector3.right;
            RollerTransform.localRotation = Quaternion.AngleAxis(angle, axis);
        }

        private void RefreshLabels()
        {
            if (selectedLabel != null)
            {
                selectedLabel.text = SelectedOption;
            }

            if (faceLabels == null || faceLabels.Length == 0)
            {
                return;
            }

            for (var i = 0; i < faceLabels.Length; i++)
            {
                if (faceLabels[i] == null)
                {
                    continue;
                }

                faceLabels[i].text = HasOptions ? options[NormalizeIndex(labelBaseStep + GetFaceOptionOffset(i))] : string.Empty;
            }
        }

        private static int GetFaceOptionOffset(int faceIndex)
        {
            return faceIndex switch
            {
                0 => 0,  // Top side: selected option.
                1 => -1, // Close side: previous option.
                2 => 2,  // Bottom side: technical prefetch surface.
                3 => 1,  // Far side: next option.
                _ => 0
            };
        }

        #endregion
    }
}

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

        [FoldoutGroup("Selected Preview")]
        [SerializeField] private Renderer[] selectedPreviewPlanes;
        [FoldoutGroup("Selected Preview")]
        [SerializeField] private bool createSelectedPreviewPlanesIfMissing = true;
        [FoldoutGroup("Selected Preview")]
        [SerializeField] private bool keepSelectedPreviewDuringSnap = true;
        [FoldoutGroup("Selected Preview")]
        [SerializeField] private Color selectedPreviewColor = new(0.2f, 0.85f, 1f, 0.32f);
        [FoldoutGroup("Selected Preview")]
        [SerializeField] private Vector2 selectedPreviewLocalSize = new(0.92f, 0.82f);
        [FoldoutGroup("Selected Preview")]
        [SerializeField, Min(0f)] private float selectedPreviewSurfaceOffset = 0.018f;

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
        private int selectedStep;
        private int labelBaseStep;
        private int dragStartSelectedIndex;
        private int bottomPrefetchDirection = 1;
        private bool keepPreviewVisibleUntilSnapComplete;
        private Camera dragCamera;
        private WorldCursorState cursorState;
        private bool hasCursorState;
        private Material generatedPreviewMaterial;
        private static Mesh selectedPreviewMesh;

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
            EnsureSelectedPreviewPlanes();
            NormalizeSelection();
            selectedStep = selectedIndex;
            labelBaseStep = selectedStep;
            currentAngle = StepToAngle(selectedStep);
            targetAngle = currentAngle;
            ApplyRotation(currentAngle);
            RefreshAllLabels();
            HideSelectedPreview();
        }

        private void Update()
        {
            if (isDragging)
            {
                return;
            }

            var remainingAngle = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
            if (remainingAngle <= 0.01f)
            {
                currentAngle = targetAngle;
                if (keepPreviewVisibleUntilSnapComplete)
                {
                    keepPreviewVisibleUntilSnapComplete = false;
                    HideSelectedPreview();
                }
            }
            else
            {
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * snapSpeed);
            }

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
            RefreshAllLabels();
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
            dragCamera = context.Camera;
            CaptureAndLockCursor();
            bottomPrefetchDirection = 0;
            keepPreviewVisibleUntilSnapComplete = false;
            ShowSelectedPreviewForStep(AngleToStep(currentAngle));
            return true;
        }

        public void UpdatePointerDrag(WorldInteractionContext context)
        {
            if (!isDragging || !HasOptions)
            {
                return;
            }

            var direction = invertDrag ? -1f : 1f;
            currentAngle += Input.GetAxisRaw("Mouse Y") * degreesPerScreenPixel * direction;
            ApplyRotation(currentAngle);

            var nearestStep = AngleToStep(currentAngle);
            ShowSelectedPreviewForStep(nearestStep);
            UpdateBottomLabelForCurrentDrag(nearestStep);
            if (ShouldRefreshLabelsAtStep(nearestStep) && nearestStep != labelBaseStep)
            {
                selectedStep = nearestStep;
                selectedIndex = NormalizeIndex(nearestStep);
                labelBaseStep = nearestStep;
                RefreshSelectedLabel();
                UpdateBottomLabelForPrefetchDirection(bottomPrefetchDirection);
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
            dragCamera = context.Camera != null ? context.Camera : dragCamera;
            SetSelectedStep(finalStep, finalIndex, false);
            RefreshAllLabels();
            UpdateBottomLabelForPrefetchDirection(bottomPrefetchDirection);
            ShowSelectedPreviewForStep(finalStep);
            keepPreviewVisibleUntilSnapComplete = keepSelectedPreviewDuringSnap;
            if (!keepPreviewVisibleUntilSnapComplete)
            {
                HideSelectedPreview();
            }

            if (changed)
            {
                NotifySelectionChanged();
            }

            RestoreCursor(true);
        }

        public void CancelPointerDrag()
        {
            isDragging = false;
            RestoreCursor(false);
            keepPreviewVisibleUntilSnapComplete = false;
            selectedStep = ClosestStepForIndex(selectedIndex);
            labelBaseStep = selectedStep;
            targetAngle = StepToAngle(selectedStep);
            currentAngle = targetAngle;
            ApplyRotation(currentAngle);
            RefreshAllLabels();
            HideSelectedPreview();
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
                RefreshAllLabels();
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

            if (isDragging)
            {
                RefreshSelectedLabel();
                UpdateBottomLabelForPrefetchDirection(bottomPrefetchDirection);
            }
            else
            {
                RefreshAllLabels();
            }

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
                RefreshAllLabels();
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

        private void EnsureSelectedPreviewPlanes()
        {
            if (!createSelectedPreviewPlanesIfMissing || RollerTransform == null)
            {
                return;
            }

            var planes = new Renderer[4];
            if (selectedPreviewPlanes != null)
            {
                for (var i = 0; i < Mathf.Min(planes.Length, selectedPreviewPlanes.Length); i++)
                {
                    planes[i] = selectedPreviewPlanes[i];
                }
            }

            for (var i = 0; i < planes.Length; i++)
            {
                if (planes[i] != null)
                {
                    continue;
                }

                planes[i] = CreateSelectedPreviewPlane(i);
            }

            selectedPreviewPlanes = planes;
        }

        private Renderer CreateSelectedPreviewPlane(int faceIndex)
        {
            var preview = new GameObject($"SelectedPreview_{GetPreviewFaceName(faceIndex)}");
            preview.layer = gameObject.layer;
            preview.transform.SetParent(RollerTransform, false);
            ConfigureSelectedPreviewTransform(preview.transform, faceIndex);

            var meshFilter = preview.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetSelectedPreviewMesh();

            var meshRenderer = preview.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetSelectedPreviewMaterial();
            meshRenderer.enabled = false;
            return meshRenderer;
        }

        private void ConfigureSelectedPreviewTransform(Transform previewTransform, int faceIndex)
        {
            var distance = 0.5f + selectedPreviewSurfaceOffset;
            previewTransform.localScale = new Vector3(
                Mathf.Max(0.01f, selectedPreviewLocalSize.x),
                Mathf.Max(0.01f, selectedPreviewLocalSize.y),
                1f);

            switch (PositiveModulo(faceIndex, 4))
            {
                case 0:
                    previewTransform.SetLocalPositionAndRotation(
                        new Vector3(0f, distance, 0f),
                        Quaternion.Euler(90f, 0f, 0f));
                    break;
                case 1:
                    previewTransform.SetLocalPositionAndRotation(
                        new Vector3(0f, 0f, -distance),
                        Quaternion.identity);
                    break;
                case 2:
                    previewTransform.SetLocalPositionAndRotation(
                        new Vector3(0f, -distance, 0f),
                        Quaternion.Euler(-90f, 0f, 0f));
                    break;
                default:
                    previewTransform.SetLocalPositionAndRotation(
                        new Vector3(0f, 0f, distance),
                        Quaternion.Euler(0f, 180f, 0f));
                    break;
            }
        }

        private void ShowSelectedPreviewForStep(int step)
        {
            EnsureSelectedPreviewPlanes();
            if (selectedPreviewPlanes == null || selectedPreviewPlanes.Length == 0)
            {
                return;
            }

            var highlightedFace = PositiveModulo(-step, 4);
            for (var i = 0; i < selectedPreviewPlanes.Length; i++)
            {
                var preview = selectedPreviewPlanes[i];
                if (preview == null)
                {
                    continue;
                }

                preview.enabled = i == highlightedFace;
            }
        }

        private void HideSelectedPreview()
        {
            if (selectedPreviewPlanes == null)
            {
                return;
            }

            foreach (var preview in selectedPreviewPlanes)
            {
                if (preview != null)
                {
                    preview.enabled = false;
                }
            }
        }

        private Material GetSelectedPreviewMaterial()
        {
            if (generatedPreviewMaterial != null)
            {
                return generatedPreviewMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");
            generatedPreviewMaterial = new Material(shader)
            {
                name = "World3DOptionRoller Selected Preview (Generated)",
                color = selectedPreviewColor,
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
            };

            SetMaterialColor(generatedPreviewMaterial, selectedPreviewColor);
            SetMaterialInt(generatedPreviewMaterial, "_Surface", 1);
            SetMaterialInt(generatedPreviewMaterial, "_Cull", 0);
            SetMaterialInt(generatedPreviewMaterial, "_ZWrite", 0);
            generatedPreviewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return generatedPreviewMaterial;
        }

        private static Mesh GetSelectedPreviewMesh()
        {
            if (selectedPreviewMesh != null)
            {
                return selectedPreviewMesh;
            }

            selectedPreviewMesh = new Mesh
            {
                name = "World3DOptionRoller Selected Preview Quad"
            };
            selectedPreviewMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            selectedPreviewMesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 };
            selectedPreviewMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            selectedPreviewMesh.RecalculateNormals();
            selectedPreviewMesh.RecalculateBounds();
            return selectedPreviewMesh;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void SetMaterialInt(Material material, string propertyName, int value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetInt(propertyName, value);
            }
        }

        private static string GetPreviewFaceName(int faceIndex)
        {
            return PositiveModulo(faceIndex, 4) switch
            {
                0 => "Top",
                1 => "Front",
                2 => "Bottom",
                _ => "Back"
            };
        }

        private void ApplyRotation(float angle)
        {
            if (RollerTransform == null)
            {
                return;
            }

            var axis = localRotationAxis.sqrMagnitude > 0.0001f ? localRotationAxis.normalized : Vector3.right;
            RollerTransform.localRotation = Quaternion.AngleAxis(angle, axis);
        }

        private void CaptureAndLockCursor()
        {
            if (!hasCursorState)
            {
                cursorState = WorldCursorUtility.Capture();
                hasCursorState = true;
            }

            WorldCursorUtility.Hide(true);
        }

        private void RestoreCursor(bool moveToRollerCenter)
        {
            if (!hasCursorState)
            {
                return;
            }

            if (moveToRollerCenter)
            {
                WorldCursorUtility.Restore(cursorState, dragCamera, RollerTransform.position);
            }
            else
            {
                WorldCursorUtility.Restore(cursorState);
            }

            hasCursorState = false;
            dragCamera = null;
        }

        private void RefreshSelectedLabel()
        {
            if (selectedLabel != null)
            {
                selectedLabel.text = SelectedOption;
            }
        }

        private void RefreshAllLabels()
        {
            RefreshSelectedLabel();

            if (faceLabels == null || faceLabels.Length == 0)
            {
                return;
            }

            SetFacePositionLabel(0, labelBaseStep);
            SetFacePositionLabel(1, labelBaseStep - 1);
            SetFacePositionLabel(3, labelBaseStep + 1);
            UpdateBottomLabelForPrefetchDirection(bottomPrefetchDirection);
        }

        private void UpdateBottomLabelForCurrentDrag(int nearestStep)
        {
            var direction = Math.Sign(nearestStep - labelBaseStep);
            if (direction == 0)
            {
                var baseAngle = StepToAngle(labelBaseStep);
                var angleDelta = Mathf.DeltaAngle(baseAngle, currentAngle);
                if (!Mathf.Approximately(angleDelta, 0f))
                {
                    direction = angleDelta < 0f ? 1 : -1;
                }
            }

            if (direction == 0)
            {
                return;
            }

            bottomPrefetchDirection = direction;
            UpdateBottomLabelForPrefetchDirection(direction);
        }

        private void UpdateBottomLabelForPrefetchDirection(int direction)
        {
            if (faceLabels == null || faceLabels.Length == 0)
            {
                return;
            }

            var offset = direction < 0 ? -2 : 2;
            SetFacePositionLabel(2, labelBaseStep + offset);
        }

        private void SetFacePositionLabel(int positionIndex, int optionStep)
        {
            var label = GetFaceLabelAtPosition(positionIndex, labelBaseStep);
            if (label == null)
            {
                return;
            }

            label.text = HasOptions ? options[NormalizeIndex(optionStep)] : string.Empty;
        }

        private TMP_Text GetFaceLabelAtPosition(int positionIndex, int step)
        {
            if (faceLabels == null || faceLabels.Length == 0)
            {
                return null;
            }

            var faceIndex = PositiveModulo(positionIndex - step, faceLabels.Length);
            return faceLabels[faceIndex];
        }

        private static int PositiveModulo(int value, int divisor)
        {
            return ((value % divisor) + divisor) % divisor;
        }

        #endregion
    }
}

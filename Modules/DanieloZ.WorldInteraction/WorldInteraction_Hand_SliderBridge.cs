using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteraction_Hand_SliderBridge : MonoBehaviour, IWorldInteraction_Pointer_Draggable
    {
        [SerializeField] private WorldInteraction_Control_Slider slider;

        private WorldCursorState cursorState;
        private bool hasCursorState;
        private bool isDragging;
        private Camera dragCamera;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            EndPointerDeltaDrag(false);
        }

        public bool BeginPointerDrag(WorldInteractionContext context)
        {
            if (!enabled || !gameObject.activeInHierarchy || slider == null)
            {
                return false;
            }

            isDragging = true;
            dragCamera = context.Camera;
            CaptureAndLockCursor();
            slider.BeginPointerDeltaDrag();
            return true;
        }

        public void UpdatePointerDrag(WorldInteractionContext context)
        {
            if (!isDragging)
            {
                return;
            }

            slider?.ApplyPointerDelta(Input.GetAxisRaw("Mouse X"));
        }

        public void EndPointerDrag(WorldInteractionContext context)
        {
            dragCamera = context.Camera != null ? context.Camera : dragCamera;
            EndPointerDeltaDrag(true);
        }

        public void CancelPointerDrag()
        {
            EndPointerDeltaDrag(false);
        }

        private void CaptureAndLockCursor()
        {
            if (!hasCursorState)
            {
                cursorState = WorldInteraction_Pointer_CursorUtility.Capture();
                hasCursorState = true;
            }

            WorldInteraction_Pointer_CursorUtility.Hide(true);
        }

        private void RestoreCursor(bool moveToHandle)
        {
            if (!hasCursorState)
            {
                return;
            }

            if (moveToHandle && slider != null && slider.TryGetHandleScreenPosition(dragCamera, out var screenPosition))
            {
                WorldInteraction_Pointer_CursorUtility.Restore(cursorState, screenPosition);
            }
            else
            {
                WorldInteraction_Pointer_CursorUtility.Restore(cursorState);
            }

            hasCursorState = false;
            dragCamera = null;
        }

        private void EndPointerDeltaDrag(bool moveCursorToHandle)
        {
            if (!isDragging)
            {
                RestoreCursor(false);
                return;
            }

            isDragging = false;
            slider?.EndPointerDeltaDrag();
            RestoreCursor(moveCursorToHandle);
        }

        private void ResolveReferences()
        {
            slider ??= GetComponentInParent<WorldInteraction_Control_Slider>();
        }
    }
}

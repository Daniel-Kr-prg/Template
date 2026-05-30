using System;
using System.Collections.Generic;
using DanieloZ.InputManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using OdinShowIf = Sirenix.OdinInspector.ShowIfAttribute;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteractionController : MonoBehaviour
    {
        #region Types

        [System.Flags]
        public enum SwingInteractionType
        {
            None = 0,
            Impulse = 1,
            Callback = 2
        }

        [System.Serializable]
        private sealed class LayerSwingInteraction
        {
            public int layer = 0;
            public SwingInteractionType interactionType = SwingInteractionType.Impulse;
        }

        internal enum PointerProjectionMode
        {
            WorldY,
            PlaneTransform,
            RaycastHit
        }

        #endregion

        #region Inspector

        [FoldoutGroup("Camera")]
        [SerializeField] private Camera fallbackCamera;

        [FoldoutGroup("Raycast")]
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;
        [FoldoutGroup("Raycast")]
        [SerializeField] private LayerMask hoverMask = Physics.DefaultRaycastLayers;
        [FoldoutGroup("Raycast")]
        [SerializeField, Min(0f)] private float rayDistance = 500f;
        [FoldoutGroup("Raycast")]
        [OdinShowIf(nameof(UsesProjectionRaycast))]
        [SerializeField] private LayerMask pointerProjectionRaycastMask = Physics.DefaultRaycastLayers;
        [FoldoutGroup("Raycast")]
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [FoldoutGroup("Drag")]
        [SerializeField] private bool enableDrag = true;
        [FoldoutGroup("Drag")]
        [OdinShowIf(nameof(enableDrag))]
        [EnumToggleButtons]
        [SerializeField] private PointerProjectionMode dragProjectionMode = PointerProjectionMode.WorldY;
        [FoldoutGroup("Drag")]
        [OdinShowIf(nameof(UsesDragPlaneTransform))]
        [SerializeField] private Transform dragPlaneTransform;
        [FoldoutGroup("Drag")]
        [OdinShowIf(nameof(UsesDragWorldY))]
        [SerializeField] private float dragWorldY = 1.25f;
        [FoldoutGroup("Drag")]
        [OdinShowIf(nameof(UsesDragPlaneTransform))]
        [SerializeField] private float dragPlaneOffset = 1.25f;

        [FoldoutGroup("Use")]
        [SerializeField] private bool enableUse = true;

        [FoldoutGroup("Hover")]
        [SerializeField] private bool enableHover = true;

        [FoldoutGroup("Swing")]
        [SerializeField] private bool enableSwing = true;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [SerializeField] private LayerMask swingMask = Physics.DefaultRaycastLayers;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [EnumToggleButtons]
        [SerializeField] private PointerProjectionMode swingProjectionMode = PointerProjectionMode.WorldY;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(UsesSwingPlaneTransform))]
        [SerializeField] private Transform swingPlaneTransform;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(UsesSwingWorldY))]
        [SerializeField] private float swingWorldY = 0f;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(UsesSwingPlaneTransform))]
        [SerializeField] private float swingPlaneOffset;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [SerializeField, Min(0.01f)] private float swingRadius = 2.5f;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [SerializeField, Min(0f)] private float horizontalImpulse = 16f;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [SerializeField, Min(0f)] private float upwardImpulse = 8f;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [SerializeField, Min(0f)] private float angularImpulse = 10f;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [SerializeField, Min(0f)] private float maxCursorSpeed = 8f;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [SerializeField, Min(0f)] private float minCursorMoveDistance = 0.02f;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [EnumToggleButtons]
        [SerializeField] private SwingInteractionType defaultSwingInteraction = SwingInteractionType.Impulse;
        [FoldoutGroup("Swing")]
        [OdinShowIf(nameof(enableSwing))]
        [SerializeField] private List<LayerSwingInteraction> layerSwingInteractions = new();

        [FoldoutGroup("Debug")]
        [SerializeField] private bool debugInput;
        [FoldoutGroup("Debug")]
        [SerializeField] private bool debugRaycasts;
        [FoldoutGroup("Debug")]
        [SerializeField] private bool debugHover;
        [FoldoutGroup("Debug")]
        [SerializeField, Min(0.1f)] private float debugStatusInterval = 2f;

        #endregion

        #region Internal State

        private readonly WorldDragFlow dragFlow = new();
        private readonly WorldPointerDragFlow pointerDragFlow = new();
        private readonly WorldHoverFlow hoverFlow = new();
        private readonly WorldSwingFlow swingFlow = new();
        private bool inputRegistered;
        private string actionPrefix;
        private float nextDebugStatusTime;

        private string LeftDownAction => $"{actionPrefix}_LeftDown";
        private string LeftHoldAction => $"{actionPrefix}_LeftHold";
        private string LeftUpAction => $"{actionPrefix}_LeftUp";
        private string RightHoldAction => $"{actionPrefix}_RightHold";
        private string RightUpAction => $"{actionPrefix}_RightUp";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            actionPrefix = $"{nameof(WorldInteractionController)}_{System.Guid.NewGuid():N}";
        }

        private void OnEnable()
        {
            TryRegisterInput();
        }

        private void OnDisable()
        {
            UnregisterInput();
            hoverFlow.End(debugHover, DebugLog);
            dragFlow.ReleaseToPhysics();
            pointerDragFlow.Cancel();
            swingFlow.Reset();
        }

        private void Update()
        {
            if (!inputRegistered)
            {
                TryRegisterInput();
            }

            DebugStatusTick();

            var scroll = Input.mouseScrollDelta.y;
            if (!Mathf.Approximately(scroll, 0f))
            {
                DebugLog($"Mouse wheel delta={scroll}. HeldObject={WorldInteractionInputGate.HeldObject?.name ?? "none"}");
            }

            UpdateHover();
        }

        #endregion

        #region Public API

        public void InitializeGameManager()
        {
            TryRegisterInput();
        }

        #endregion

        #region Input Registration

        private void TryRegisterInput()
        {
            if (inputRegistered || !InputManager.HaveInstance())
            {
                if (!inputRegistered && debugInput)
                {
                    DebugLog("Waiting for InputManager instance before registering input actions.");
                }

                return;
            }

            var manager = InputManager.Instance;
            manager.RegisterKeyDownAction(InputActionKey.MOUSE_LEFT, LeftDownAction, HandleLeftDown, InputPriority.Base, HasInputRegistrationError);
            manager.RegisterKeyHoldAction(InputActionKey.MOUSE_LEFT, LeftHoldAction, HandleLeftHold, InputPriority.Base, HasInputRegistrationError);
            manager.RegisterKeyUpAction(InputActionKey.MOUSE_LEFT, LeftUpAction, HandleLeftUp, InputPriority.Base, HasInputRegistrationError);
            manager.RegisterKeyHoldAction(InputActionKey.MOUSE_RIGHT, RightHoldAction, HandleRightHold, InputPriority.Base, HasInputRegistrationError);
            manager.RegisterKeyUpAction(InputActionKey.MOUSE_RIGHT, RightUpAction, HandleRightUp, InputPriority.Base, HasInputRegistrationError);
            inputRegistered = true;
            DebugLog("Registered InputManager actions for LMB/RMB.");
        }

        private bool HasInputRegistrationError()
        {
            return this == null || !isActiveAndEnabled;
        }

        private void UnregisterInput()
        {
            if (!inputRegistered || !InputManager.HaveInstance())
            {
                inputRegistered = false;
                return;
            }

            var manager = InputManager.Instance;
            manager.UnregisterKeyDownAction(InputActionKey.MOUSE_LEFT, LeftDownAction);
            manager.UnregisterKeyHoldAction(InputActionKey.MOUSE_LEFT, LeftHoldAction);
            manager.UnregisterKeyUpAction(InputActionKey.MOUSE_LEFT, LeftUpAction);
            manager.UnregisterKeyHoldAction(InputActionKey.MOUSE_RIGHT, RightHoldAction);
            manager.UnregisterKeyUpAction(InputActionKey.MOUSE_RIGHT, RightUpAction);
            inputRegistered = false;
            DebugLog("Unregistered InputManager actions.");
        }

        #endregion

        #region Drag And Use

        private void HandleLeftDown()
        {
            DebugLog("LMB down.");
            if (!TryRaycast(interactionMask, out var context))
            {
                DebugLog("LMB raycast missed.");
                return;
            }

            DebugLog($"LMB hit {context.Hit.collider.name} at {context.Hit.point}.");
            if (WorldInteractionComponentLookup.TryGetInParents<IWorldPointerDraggable>(context.Hit.collider, out var pointerDraggable)
                && pointerDragFlow.TryBegin(pointerDraggable, context))
            {
                DebugLog($"Starting pointer drag on {WorldInteractionComponentLookup.GetDebugName(pointerDraggable)}.");
                return;
            }

            if (enableDrag)
            {
                var draggable = context.Hit.collider.GetComponentInParent<WorldDraggable>();
                if (draggable != null)
                {
                    DebugLog($"Starting drag on {draggable.name}.");
                    if (!dragFlow.TryBegin(draggable))
                    {
                        DebugLog($"Drag rejected by {draggable.name}.");
                        return;
                    }

                    UpdateDragPosition();
                    return;
                }
            }

            if (enableUse && WorldInteractionComponentLookup.TryGetInParents<IWorldUsable>(context.Hit.collider, out var usable))
            {
                DebugLog($"Calling Use on {WorldInteractionComponentLookup.GetDebugName(usable)}.");
                usable.Use(context);
                return;
            }

            DebugLog("LMB hit has no WorldDraggable or IWorldUsable.");
        }

        private void HandleLeftHold()
        {
            if (pointerDragFlow.IsDragging)
            {
                pointerDragFlow.Update(GetCamera(), Input.mousePosition);
                return;
            }

            if (dragFlow.DraggedObject != null)
            {
                UpdateDragPosition();
            }
        }

        private void HandleLeftUp()
        {
            if (pointerDragFlow.IsDragging)
            {
                pointerDragFlow.End(GetCamera(), Input.mousePosition);
                return;
            }

            var draggedObject = dragFlow.DraggedObject;
            if (draggedObject == null)
            {
                return;
            }

            if (draggedObject is World3DSlotItem slotItem && TryInsertHeldSlotItem(slotItem))
            {
                dragFlow.Clear();
                return;
            }

            var releaseContext = new WorldDragReleaseContext(GetCamera(), Input.mousePosition);
            if (WorldInteractionComponentLookup.TryGetInParents<IWorldDraggableReleaseHandler>(draggedObject, out var releaseHandler)
                && releaseHandler.TryReleaseDraggedObject(draggedObject, releaseContext))
            {
                DebugLog($"Released dragged object through {WorldInteractionComponentLookup.GetDebugName(releaseHandler)}.");
                dragFlow.Clear();
                return;
            }

            draggedObject.ReleaseToPhysics();
            DebugLog("Released dragged object to physics.");
            dragFlow.Clear();
        }

        private void UpdateDragPosition()
        {
            var draggedObject = dragFlow.DraggedObject;
            if (draggedObject == null || !TryGetPointerPoint(dragProjectionMode, dragPlaneTransform, dragWorldY, dragPlaneOffset, out var point))
            {
                if (draggedObject != null)
                {
                    DebugLog("Drag pointer projection failed.");
                }

                return;
            }

            draggedObject.DragToGripPosition(point);
        }

        #endregion

        #region Swing

        private void HandleRightHold()
        {
            if (!enableSwing || WorldInteractionInputGate.HasHeldObject)
            {
                if (debugInput && InputManager.HaveInstance() && InputManager.Instance.IsKeyHeld(InputActionKey.MOUSE_RIGHT))
                {
                    DebugLog($"RMB swing blocked. enableSwing={enableSwing}, heldObject={WorldInteractionInputGate.HeldObject?.name ?? "none"}.");
                }

                swingFlow.ClearPointerHistory();
                return;
            }

            if (!TryGetPointerPoint(swingProjectionMode, swingPlaneTransform, swingWorldY, swingPlaneOffset, out var point))
            {
                DebugLog("RMB swing pointer projection failed.");
                return;
            }

            if (!swingFlow.TryConsumePointerPoint(point, minCursorMoveDistance, out var direction, out var cursorSpeed))
            {
                return;
            }

            swingFlow.ApplySwing(
                point,
                direction,
                cursorSpeed,
                GetCamera(),
                Input.mousePosition,
                swingPlaneTransform,
                swingRadius,
                swingMask,
                triggerInteraction,
                maxCursorSpeed,
                horizontalImpulse,
                upwardImpulse,
                angularImpulse,
                forceMode,
                GetSwingInteractionForLayer,
                DebugLog);
        }

        private void HandleRightUp()
        {
            swingFlow.ClearPointerHistory();
        }

        #endregion

        #region Hover

        private void UpdateHover()
        {
            if (!enableHover)
            {
                hoverFlow.End(debugHover, DebugLog);
                return;
            }

            if (WorldInteractionInputGate.HeldObject is World3DSlotItem heldSlotItem)
            {
                UpdateHeldSlotItemHover(heldSlotItem);
                return;
            }

            if (!TryRaycast(hoverMask, out var context)
                || !WorldInteractionComponentLookup.TryGetInParents<IWorldHoverable>(context.Hit.collider, out var hoverable))
            {
                hoverFlow.End(debugHover, DebugLog);
                return;
            }

            hoverFlow.SetHover(hoverable, context, debugHover, DebugLog);
        }

        private void UpdateHeldSlotItemHover(World3DSlotItem heldSlotItem)
        {
            var slotMask = WorldInteractionMaskUtility.Combine(interactionMask, hoverMask);
            if (!TryFindSlotUnderPointer(heldSlotItem, slotMask, out var context, out var slot))
            {
                hoverFlow.End(debugHover, DebugLog);
                return;
            }

            hoverFlow.RefreshSlotHover(slot, context, debugHover, DebugLog);
        }

        private bool TryInsertHeldSlotItem(World3DSlotItem slotItem)
        {
            var slotMask = WorldInteractionMaskUtility.Combine(interactionMask, hoverMask);
            if (!TryFindSlotUnderPointer(slotItem, slotMask, out _, out var slot))
            {
                return false;
            }

            if (!slot.TryInsertHeldItem(slotItem))
            {
                return false;
            }

            hoverFlow.End(debugHover, DebugLog);
            DebugLog($"Inserted held slot item {slotItem.name} into {slot.name}.");
            return true;
        }

        #endregion

        #region Raycast And Projection

        private bool TryRaycast(LayerMask mask, out WorldInteractionContext context)
        {
            return WorldInteractionPointerUtility.TryRaycast(
                GetCamera(),
                Input.mousePosition,
                mask,
                rayDistance,
                triggerInteraction,
                debugRaycasts,
                DebugLog,
                out context);
        }

        private bool TryFindSlotUnderPointer(
            WorldDraggable ignoredDraggable,
            LayerMask mask,
            out WorldInteractionContext context,
            out World3DButtonSlotBase slot)
        {
            return WorldInteractionPointerUtility.TryFindSlotUnderPointer(
                GetCamera(),
                Input.mousePosition,
                ignoredDraggable,
                mask,
                rayDistance,
                out context,
                out slot);
        }

        private bool TryGetPointerPoint(
            PointerProjectionMode projectionMode,
            Transform planeTransform,
            float worldY,
            float planeOffset,
            out Vector3 point)
        {
            return WorldInteractionPointerUtility.TryGetPointerPoint(
                GetCamera(),
                Input.mousePosition,
                projectionMode,
                planeTransform,
                worldY,
                planeOffset,
                pointerProjectionRaycastMask,
                rayDistance,
                triggerInteraction,
                out point);
        }

        #endregion

        #region Swing Helpers

        private SwingInteractionType GetSwingInteractionForLayer(int layer)
        {
            foreach (var rule in layerSwingInteractions)
            {
                if (rule != null && rule.layer == layer)
                {
                    return rule.interactionType;
                }
            }

            return defaultSwingInteraction;
        }

        #endregion

        #region Camera

        private Camera GetCamera()
        {
            return CameraManager.CurrentCamera != null ? CameraManager.CurrentCamera : fallbackCamera;
        }

        #endregion

        #region Inspector State

        private bool UsesProjectionRaycast => dragProjectionMode == PointerProjectionMode.RaycastHit
            || swingProjectionMode == PointerProjectionMode.RaycastHit;

        private bool UsesDragPlaneTransform => enableDrag
            && dragProjectionMode == PointerProjectionMode.PlaneTransform;

        private bool UsesDragWorldY => enableDrag
            && dragProjectionMode == PointerProjectionMode.WorldY;

        private bool UsesSwingPlaneTransform => enableSwing
            && swingProjectionMode == PointerProjectionMode.PlaneTransform;

        private bool UsesSwingWorldY => enableSwing
            && swingProjectionMode == PointerProjectionMode.WorldY;

        #endregion

        #region Debug

        private void DebugStatusTick()
        {
            if (!debugInput || Time.unscaledTime < nextDebugStatusTime)
            {
                return;
            }

            nextDebugStatusTime = Time.unscaledTime + debugStatusInterval;
            var camera = GetCamera();
            DebugLog(
                $"Status: inputRegistered={inputRegistered}, inputManager={InputManager.HaveInstance()}, " +
                $"camera={(camera != null ? camera.name : "none")}, held={WorldInteractionInputGate.HeldObject?.name ?? "none"}, " +
                $"mouse={Input.mousePosition}, interactionMask={interactionMask.value}, hoverMask={hoverMask.value}, swingMask={swingMask.value}.");
        }

        private void DebugLog(string message)
        {
            if (!debugInput && !debugRaycasts && !debugHover)
            {
                return;
            }

            Debug.Log($"[{nameof(WorldInteractionController)}] {message}", this);
        }

        private static string GetDebugName(object value)
        {
            return value is Component component ? component.name : value?.ToString() ?? "null";
        }

        #endregion
    }
}

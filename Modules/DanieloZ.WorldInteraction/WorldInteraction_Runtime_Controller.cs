using System;
using System.Collections.Generic;
using DanieloZ.InputManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using OdinShowIf = Sirenix.OdinInspector.ShowIfAttribute;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteraction_Runtime_Controller : MonoBehaviour
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

        [FoldoutGroup("Slots")]
        [SerializeField] private WorldInteraction_Slot_ResolutionSettings slotResolution = WorldInteraction_Slot_ResolutionSettings.CreateDefault();

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

        private readonly WorldInteraction_Runtime_DragFlow dragFlow = new();
        private readonly WorldInteraction_Runtime_PointerDragFlow pointerDragFlow = new();
        private readonly WorldInteraction_Runtime_HoverFlow hoverFlow = new();
        private readonly WorldInteraction_Runtime_SwingFlow swingFlow = new();
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
            actionPrefix = $"{nameof(WorldInteraction_Runtime_Controller)}_{System.Guid.NewGuid():N}";
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

        private void OnValidate()
        {
            slotResolution.Clamp();
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
                DebugLog($"Mouse wheel delta={scroll}. HeldObject={WorldInteraction_Runtime_InputGate.HeldObject?.name ?? "none"}");
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
            if (!enableDrag && !enableUse)
            {
                DebugLog("LMB ignored because drag and use are disabled.");
                return;
            }

            if (!TryRaycast(interactionMask, out var context))
            {
                DebugLog("LMB raycast missed.");
                return;
            }

            DebugLog($"LMB hit {context.Hit.collider.name} at {context.Hit.point}.");
            if (enableDrag
                && WorldInteraction_Runtime_ComponentLookup.TryGetInParents<IWorldInteraction_Pointer_Draggable>(context.Hit.collider, out var pointerDraggable)
                && pointerDragFlow.TryBegin(pointerDraggable, context))
            {
                DebugLog($"Starting pointer drag on {WorldInteraction_Runtime_ComponentLookup.GetDebugName(pointerDraggable)}.");
                return;
            }

            if (enableDrag)
            {
                var draggable = context.Hit.collider.GetComponentInParent<WorldInteraction_Drag_Object>();
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

            if (enableUse && WorldInteraction_Runtime_ComponentLookup.TryGetInParents<IWorldInteraction_Press_Usable>(context.Hit.collider, out var usable))
            {
                DebugLog($"Calling Use on {WorldInteraction_Runtime_ComponentLookup.GetDebugName(usable)}.");
                usable.Use(context);
                return;
            }

            DebugLog("LMB hit has no WorldInteraction_Drag_Object or IWorldInteraction_Press_Usable.");
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

            if (draggedObject is WorldInteraction_Slot_Item slotItem && TryInsertHeldSlotItem(slotItem))
            {
                dragFlow.Clear();
                return;
            }

            var releaseContext = new WorldDragReleaseContext(GetCamera(), Input.mousePosition);
            if (WorldInteraction_Runtime_ComponentLookup.TryGetInParents<IWorldInteraction_Drag_ReleaseHandler>(draggedObject, out var releaseHandler)
                && releaseHandler.TryReleaseDraggedObject(draggedObject, releaseContext))
            {
                DebugLog($"Released dragged object through {WorldInteraction_Runtime_ComponentLookup.GetDebugName(releaseHandler)}.");
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
            if (!enableSwing || WorldInteraction_Runtime_InputGate.HasHeldObject)
            {
                if (debugInput && InputManager.HaveInstance() && InputManager.Instance.IsKeyHeld(InputActionKey.MOUSE_RIGHT))
                {
                    DebugLog($"RMB swing blocked. enableSwing={enableSwing}, heldObject={WorldInteraction_Runtime_InputGate.HeldObject?.name ?? "none"}.");
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

            if (WorldInteraction_Runtime_InputGate.HeldObject is WorldInteraction_Slot_Item heldSlotItem)
            {
                UpdateHeldSlotItemHover(heldSlotItem);
                return;
            }

            if (!TryRaycast(hoverMask, out var context)
                || !WorldInteraction_Runtime_ComponentLookup.TryGetInParents<IWorldInteraction_Surface_Hoverable>(context.Hit.collider, out var hoverable))
            {
                hoverFlow.End(debugHover, DebugLog);
                return;
            }

            hoverFlow.SetHover(hoverable, context, debugHover, DebugLog);
        }

        private void UpdateHeldSlotItemHover(WorldInteraction_Slot_Item heldSlotItem)
        {
            var slotMask = WorldInteraction_Runtime_MaskUtility.Combine(interactionMask, hoverMask);
            if (!TryGetPointerContext(out var pointerContext)
                || !WorldInteraction_Slot_ResolutionUtility.TryFindSlotForHeldItem(
                    heldSlotItem,
                    slotResolution,
                    pointerContext,
                    slotMask,
                    out var context,
                    out var slot))
            {
                hoverFlow.End(debugHover, DebugLog);
                return;
            }

            hoverFlow.RefreshSlotHover(slot, context, debugHover, DebugLog);
        }

        private bool TryInsertHeldSlotItem(WorldInteraction_Slot_Item slotItem)
        {
            var slotMask = WorldInteraction_Runtime_MaskUtility.Combine(interactionMask, hoverMask);
            if (!TryGetPointerContext(out var pointerContext)
                || !WorldInteraction_Slot_ResolutionUtility.TryFindSlotForHeldItem(
                    slotItem,
                    slotResolution,
                    pointerContext,
                    slotMask,
                    out _,
                    out var slot))
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
            return WorldInteraction_Runtime_PointerUtility.TryRaycast(
                GetCamera(),
                Input.mousePosition,
                mask,
                rayDistance,
                triggerInteraction,
                debugRaycasts,
                DebugLog,
                out context);
        }

        private bool TryGetPointerContext(out WorldInteractionContext context)
        {
            context = default;
            var camera = GetCamera();
            if (camera == null)
            {
                return false;
            }

            var screenPosition = (Vector2)Input.mousePosition;
            context = new WorldInteractionContext(
                camera,
                camera.ScreenPointToRay(screenPosition),
                default,
                screenPosition);
            return true;
        }

        private bool TryGetPointerPoint(
            PointerProjectionMode projectionMode,
            Transform planeTransform,
            float worldY,
            float planeOffset,
            out Vector3 point)
        {
            return WorldInteraction_Runtime_PointerUtility.TryGetPointerPoint(
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
                $"camera={(camera != null ? camera.name : "none")}, held={WorldInteraction_Runtime_InputGate.HeldObject?.name ?? "none"}, " +
                $"mouse={Input.mousePosition}, interactionMask={interactionMask.value}, hoverMask={hoverMask.value}, swingMask={swingMask.value}.");
        }

        private void DebugLog(string message)
        {
            if (!debugInput && !debugRaycasts && !debugHover)
            {
                return;
            }

            Debug.Log($"[{nameof(WorldInteraction_Runtime_Controller)}] {message}", this);
        }

        private static string GetDebugName(object value)
        {
            return value is Component component ? component.name : value?.ToString() ?? "null";
        }

        #endregion
    }
}

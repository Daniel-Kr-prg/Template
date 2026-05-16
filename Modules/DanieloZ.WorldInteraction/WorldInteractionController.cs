using System;
using System.Collections.Generic;
using DanieloZ.InputManagement;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteractionController : MonoBehaviour
    {
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

        private enum PointerProjectionMode
        {
            WorldY,
            PlaneTransform,
            RaycastHit
        }

        [Header("Camera")]
        [SerializeField] private Camera fallbackCamera;

        [Header("Raycast")]
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;
        [SerializeField] private LayerMask hoverMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float rayDistance = 500f;
        [SerializeField] private LayerMask pointerProjectionRaycastMask = Physics.DefaultRaycastLayers;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Drag")]
        [SerializeField] private bool enableDrag = true;
        [SerializeField] private PointerProjectionMode dragProjectionMode = PointerProjectionMode.WorldY;
        [SerializeField] private Transform dragPlaneTransform;
        [SerializeField] private float dragWorldY = 1.25f;
        [SerializeField] private float dragPlaneOffset = 1.25f;

        [Header("Use")]
        [SerializeField] private bool enableUse = true;

        [Header("Hover")]
        [SerializeField] private bool enableHover = true;

        [Header("Swing")]
        [SerializeField] private bool enableSwing = true;
        [SerializeField] private LayerMask swingMask = Physics.DefaultRaycastLayers;
        [SerializeField] private PointerProjectionMode swingProjectionMode = PointerProjectionMode.WorldY;
        [SerializeField] private Transform swingPlaneTransform;
        [SerializeField] private float swingWorldY = 0f;
        [SerializeField] private float swingPlaneOffset;
        [SerializeField, Min(0.01f)] private float swingRadius = 2.5f;
        [SerializeField, Min(0f)] private float horizontalImpulse = 16f;
        [SerializeField, Min(0f)] private float upwardImpulse = 8f;
        [SerializeField, Min(0f)] private float angularImpulse = 10f;
        [SerializeField, Min(0f)] private float maxCursorSpeed = 8f;
        [SerializeField, Min(0f)] private float minCursorMoveDistance = 0.02f;
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;
        [SerializeField] private SwingInteractionType defaultSwingInteraction = SwingInteractionType.Impulse;
        [SerializeField] private List<LayerSwingInteraction> layerSwingInteractions = new();

        [Header("Debug")]
        [SerializeField] private bool debugInput;
        [SerializeField] private bool debugRaycasts;
        [SerializeField] private bool debugHover;
        [SerializeField, Min(0.1f)] private float debugStatusInterval = 2f;

        private readonly HashSet<Rigidbody> affectedBodies = new();
        private readonly HashSet<MonoBehaviour> affectedSwingables = new();
        private WorldDraggable draggedObject;
        private IWorldHoverable hoveredObject;
        private WorldInteractionContext hoveredContext;
        private bool inputRegistered;
        private bool hasPreviousSwingPoint;
        private Vector3 previousSwingPoint;
        private string actionPrefix;
        private float nextDebugStatusTime;

        private string LeftDownAction => $"{actionPrefix}_LeftDown";
        private string LeftHoldAction => $"{actionPrefix}_LeftHold";
        private string LeftUpAction => $"{actionPrefix}_LeftUp";
        private string RightHoldAction => $"{actionPrefix}_RightHold";
        private string RightUpAction => $"{actionPrefix}_RightUp";

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
            EndHover();

            if (draggedObject != null)
            {
                draggedObject.ReleaseToPhysics();
                draggedObject = null;
            }
        }

        public void InitializeGameManager()
        {
            TryRegisterInput();
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
                WorldInteractionInputGate.TryConsumeWheelForHeldObject(scroll);
            }

            UpdateHover();
        }

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
            manager.RegisterKeyDownAction(InputActionKey.MOUSE_LEFT, LeftDownAction, HandleLeftDown, InputPriority.Highest, HasInputRegistrationError);
            manager.RegisterKeyHoldAction(InputActionKey.MOUSE_LEFT, LeftHoldAction, HandleLeftHold, InputPriority.Highest, HasInputRegistrationError);
            manager.RegisterKeyUpAction(InputActionKey.MOUSE_LEFT, LeftUpAction, HandleLeftUp, InputPriority.Highest, HasInputRegistrationError);
            manager.RegisterKeyHoldAction(InputActionKey.MOUSE_RIGHT, RightHoldAction, HandleRightHold, InputPriority.Highest, HasInputRegistrationError);
            manager.RegisterKeyUpAction(InputActionKey.MOUSE_RIGHT, RightUpAction, HandleRightUp, InputPriority.Highest, HasInputRegistrationError);
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

        private void HandleLeftDown()
        {
            DebugLog("LMB down.");
            if (!TryRaycast(interactionMask, out var context))
            {
                DebugLog("LMB raycast missed.");
                return;
            }

            DebugLog($"LMB hit {context.Hit.collider.name} at {context.Hit.point}.");
            if (enableDrag)
            {
                var draggable = context.Hit.collider.GetComponentInParent<WorldDraggable>();
                if (draggable != null)
                {
                    DebugLog($"Starting drag on {draggable.name}.");
                    BeginDrag(draggable);
                    return;
                }
            }

            if (enableUse && TryGetComponentInParents<IWorldUsable>(context.Hit.collider, out var usable))
            {
                DebugLog($"Calling Use on {GetDebugName(usable)}.");
                usable.Use(context);
                return;
            }

            DebugLog("LMB hit has no WorldDraggable or IWorldUsable.");
        }

        private void BeginDrag(WorldDraggable draggable)
        {
            draggedObject = draggable;
            draggedObject.BeginDrag();

            if (!draggedObject.IsHeld)
            {
                DebugLog($"Drag rejected by {draggedObject.name}.");
                draggedObject = null;
                return;
            }

            UpdateDragPosition();
        }

        private void HandleLeftHold()
        {
            if (draggedObject != null)
            {
                UpdateDragPosition();
            }
        }

        private void HandleLeftUp()
        {
            if (draggedObject == null)
            {
                return;
            }

            if (draggedObject is World3DSlotItem slotItem && TryInsertHeldSlotItem(slotItem))
            {
                draggedObject = null;
                return;
            }

            var releaseContext = new WorldDragReleaseContext(GetCamera(), Input.mousePosition);
            if (TryGetComponentInParents<IWorldDraggableReleaseHandler>(draggedObject, out var releaseHandler)
                && releaseHandler.TryReleaseDraggedObject(draggedObject, releaseContext))
            {
                DebugLog($"Released dragged object through {GetDebugName(releaseHandler)}.");
                draggedObject = null;
                return;
            }

            draggedObject.ReleaseToPhysics();
            DebugLog("Released dragged object to physics.");
            draggedObject = null;
        }

        private void UpdateDragPosition()
        {
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

        private void HandleRightHold()
        {
            if (!enableSwing || WorldInteractionInputGate.HasHeldObject)
            {
                if (debugInput && InputManager.HaveInstance() && InputManager.Instance.IsKeyHeld(InputActionKey.MOUSE_RIGHT))
                {
                    DebugLog($"RMB swing blocked. enableSwing={enableSwing}, heldObject={WorldInteractionInputGate.HeldObject?.name ?? "none"}.");
                }

                hasPreviousSwingPoint = false;
                return;
            }

            if (!TryGetPointerPoint(swingProjectionMode, swingPlaneTransform, swingWorldY, swingPlaneOffset, out var point))
            {
                DebugLog("RMB swing pointer projection failed.");
                return;
            }

            if (!hasPreviousSwingPoint)
            {
                previousSwingPoint = point;
                hasPreviousSwingPoint = true;
                return;
            }

            var delta = point - previousSwingPoint;
            previousSwingPoint = point;
            var distance = delta.magnitude;

            if (Time.deltaTime <= 0f || distance < minCursorMoveDistance)
            {
                return;
            }

            ApplySwing(point, delta / distance, distance / Time.deltaTime);
        }

        private void HandleRightUp()
        {
            hasPreviousSwingPoint = false;
        }

        private void ApplySwing(Vector3 center, Vector3 direction, float cursorSpeed)
        {
            affectedBodies.Clear();
            affectedSwingables.Clear();

            var cappedCursorSpeed = maxCursorSpeed > 0f ? Mathf.Min(cursorSpeed, maxCursorSpeed) : cursorSpeed;
            var up = swingPlaneTransform != null ? swingPlaneTransform.up : Vector3.up;
            var force = (direction * horizontalImpulse + up * upwardImpulse) * cappedCursorSpeed;
            var torqueAxis = Vector3.Cross(up, direction).normalized;
            var torque = torqueAxis * angularImpulse * cappedCursorSpeed;
            var colliders = Physics.OverlapSphere(center, swingRadius, swingMask, triggerInteraction);
            DebugLog($"Swing at {center}, colliders={colliders.Length}, speed={cappedCursorSpeed}.");

            foreach (var collider in colliders)
            {
                var interactionType = GetSwingInteractionForLayer(collider.gameObject.layer);
                if (interactionType == SwingInteractionType.None)
                {
                    continue;
                }

                var body = collider.attachedRigidbody != null
                    ? collider.attachedRigidbody
                    : collider.GetComponentInParent<Rigidbody>();

                var context = new WorldSwingContext(
                    GetCamera(),
                    Input.mousePosition,
                    center,
                    direction,
                    cappedCursorSpeed,
                    force,
                    torque,
                    collider,
                    body);

                if ((interactionType & SwingInteractionType.Callback) != 0)
                {
                    InvokeSwingCallbacks(collider, context);
                }

                if ((interactionType & SwingInteractionType.Impulse) == 0
                    || body == null
                    || body.isKinematic
                    || !affectedBodies.Add(body))
                {
                    continue;
                }

                body.AddForce(force, forceMode);
                if (torque.sqrMagnitude > 0.000001f)
                {
                    body.AddTorque(torque, forceMode);
                }
            }
        }

        private void UpdateHover()
        {
            if (!enableHover)
            {
                EndHover();
                return;
            }

            if (WorldInteractionInputGate.HeldObject is World3DSlotItem heldSlotItem)
            {
                UpdateHeldSlotItemHover(heldSlotItem);
                return;
            }

            if (!TryRaycast(hoverMask, out var context) || !TryGetComponentInParents<IWorldHoverable>(context.Hit.collider, out var hoverable))
            {
                EndHover();
                return;
            }

            if (ReferenceEquals(hoveredObject, hoverable))
            {
                hoveredContext = context;
                return;
            }

            EndHover();
            hoveredObject = hoverable;
            hoveredContext = context;
            if (debugHover)
            {
                DebugLog($"HoverStart {GetDebugName(hoveredObject)}.");
            }
            hoveredObject.HoverStart(context);
        }

        private void EndHover()
        {
            if (hoveredObject == null)
            {
                return;
            }

            if (debugHover)
            {
                DebugLog($"HoverEnd {GetDebugName(hoveredObject)}.");
            }
            hoveredObject.HoverEnd(hoveredContext);
            hoveredObject = null;
        }

        private void UpdateHeldSlotItemHover(World3DSlotItem heldSlotItem)
        {
            var slotMask = CombineMasks(interactionMask, hoverMask);
            if (!TryFindSlotUnderPointer(heldSlotItem, slotMask, out var context, out var slot))
            {
                EndHover();
                return;
            }

            if (ReferenceEquals(hoveredObject, slot))
            {
                hoveredContext = context;
                slot.RefreshHeldItemHover();
                return;
            }

            EndHover();
            hoveredObject = slot;
            hoveredContext = context;
            slot.HoverStart(context);
        }

        private bool TryInsertHeldSlotItem(World3DSlotItem slotItem)
        {
            var slotMask = CombineMasks(interactionMask, hoverMask);
            if (!TryFindSlotUnderPointer(slotItem, slotMask, out _, out var slot))
            {
                return false;
            }

            if (!slot.TryInsertHeldItem(slotItem))
            {
                return false;
            }

            EndHover();
            DebugLog($"Inserted held slot item {slotItem.name} into {slot.name}.");
            return true;
        }

        private bool TryRaycast(LayerMask mask, out WorldInteractionContext context)
        {
            context = default;
            var camera = GetCamera();
            if (camera == null)
            {
                DebugLog("Raycast failed: no current/fallback camera.");
                return false;
            }

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, rayDistance, mask, triggerInteraction))
            {
                if (debugRaycasts)
                {
                    DebugLog($"Raycast miss. camera={camera.name}, mask={mask.value}, mouse={Input.mousePosition}.");
                }

                return false;
            }

            context = new WorldInteractionContext(camera, ray, hit, Input.mousePosition);
            if (debugRaycasts)
            {
                DebugLog($"Raycast hit {hit.collider.name}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}, distance={hit.distance}.");
            }
            return true;
        }

        private bool TryFindSlotUnderPointer(
            WorldDraggable ignoredDraggable,
            LayerMask mask,
            out WorldInteractionContext context,
            out World3DButtonSlotBase slot)
        {
            context = default;
            slot = null;
            var camera = GetCamera();
            if (camera == null)
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, rayDistance, mask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, static (left, right) => left.distance.CompareTo(right.distance));

            foreach (var hit in hits)
            {
                if (ignoredDraggable != null && hit.collider.GetComponentInParent<WorldDraggable>() == ignoredDraggable)
                {
                    continue;
                }

                var foundSlot = hit.collider.GetComponentInParent<World3DButtonSlotBase>();
                if (foundSlot == null)
                {
                    continue;
                }

                context = new WorldInteractionContext(camera, ray, hit, Input.mousePosition);
                slot = foundSlot;
                return true;
            }

            return false;
        }

        private static LayerMask CombineMasks(LayerMask first, LayerMask second)
        {
            return new LayerMask { value = first.value | second.value };
        }

        private bool TryGetPointerPoint(
            PointerProjectionMode projectionMode,
            Transform planeTransform,
            float worldY,
            float planeOffset,
            out Vector3 point)
        {
            point = default;
            var camera = GetCamera();
            if (camera == null)
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (projectionMode == PointerProjectionMode.RaycastHit)
            {
                if (!Physics.Raycast(ray, out var hit, rayDistance, pointerProjectionRaycastMask, triggerInteraction))
                {
                    return false;
                }

                point = hit.point;
                return true;
            }

            if (projectionMode == PointerProjectionMode.PlaneTransform && planeTransform != null)
            {
                var plane = new Plane(planeTransform.up, planeTransform.position + planeTransform.up * planeOffset);
                if (!plane.Raycast(ray, out var distance))
                {
                    return false;
                }

                point = ray.GetPoint(distance);
                return true;
            }

            var worldPlane = new Plane(Vector3.up, new Vector3(0f, worldY, 0f));
            if (!worldPlane.Raycast(ray, out var worldDistance))
            {
                return false;
            }

            point = ray.GetPoint(worldDistance);
            return true;
        }

        private static bool TryGetComponentInParents<T>(Collider collider, out T component) where T : class
        {
            component = null;
            if (collider == null)
            {
                return false;
            }

            var behaviours = collider.GetComponentsInParent<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is T matched)
                {
                    component = matched;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetComponentInParents<T>(Component component, out T result) where T : class
        {
            result = null;
            if (component == null)
            {
                return false;
            }

            var behaviours = component.GetComponentsInParent<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is T matched)
                {
                    result = matched;
                    return true;
                }
            }

            return false;
        }

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

        private void InvokeSwingCallbacks(Collider collider, WorldSwingContext context)
        {
            var behaviours = collider.GetComponentsInParent<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IWorldSwingable swingable && affectedSwingables.Add(behaviour))
                {
                    swingable.Swing(context);
                }
            }
        }

        private Camera GetCamera()
        {
            return CameraManager.CurrentCamera != null ? CameraManager.CurrentCamera : fallbackCamera;
        }

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
    }
}

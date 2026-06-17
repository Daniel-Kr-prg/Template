using System;
using System.Collections.Generic;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    internal static class WorldInteraction_Runtime_ComponentLookup
    {
        public static bool TryGetInParents<T>(Collider collider, out T component) where T : class
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

        public static bool TryGetInParents<T>(Component component, out T result) where T : class
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

        public static string GetDebugName(object value)
        {
            return value is Component component ? component.name : value?.ToString() ?? "null";
        }
    }

    internal static class WorldInteraction_Runtime_MaskUtility
    {
        public static LayerMask Combine(LayerMask first, LayerMask second)
        {
            return new LayerMask { value = first.value | second.value };
        }
    }

    internal static class WorldInteraction_Runtime_PointerUtility
    {
        public static bool TryRaycast(
            Camera camera,
            Vector2 screenPosition,
            LayerMask mask,
            float rayDistance,
            QueryTriggerInteraction triggerInteraction,
            bool debugRaycasts,
            Action<string> log,
            out WorldInteractionContext context)
        {
            context = default;
            if (camera == null)
            {
                log?.Invoke("Raycast failed: no current/fallback camera.");
                return false;
            }

            var ray = camera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, rayDistance, mask, triggerInteraction))
            {
                if (debugRaycasts)
                {
                    log?.Invoke($"Raycast miss. camera={camera.name}, mask={mask.value}, mouse={screenPosition}.");
                }

                return false;
            }

            context = new WorldInteractionContext(camera, ray, hit, screenPosition);
            if (debugRaycasts)
            {
                log?.Invoke($"Raycast hit {hit.collider.name}, layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}, distance={hit.distance}.");
            }

            return true;
        }

        public static bool TryGetPointerPoint(
            Camera camera,
            Vector2 screenPosition,
            WorldInteraction_Runtime_Controller.PointerProjectionMode projectionMode,
            Transform planeTransform,
            float worldY,
            float planeOffset,
            LayerMask raycastMask,
            float rayDistance,
            QueryTriggerInteraction triggerInteraction,
            out Vector3 point)
        {
            point = default;
            if (camera == null)
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(screenPosition);
            if (projectionMode == WorldInteraction_Runtime_Controller.PointerProjectionMode.RaycastHit)
            {
                if (!Physics.Raycast(ray, out var hit, rayDistance, raycastMask, triggerInteraction))
                {
                    return false;
                }

                point = hit.point;
                return true;
            }

            if (projectionMode == WorldInteraction_Runtime_Controller.PointerProjectionMode.PlaneTransform && planeTransform != null)
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
    }

    internal sealed class WorldInteraction_Runtime_PointerDragFlow
    {
        private IWorldInteraction_Pointer_Draggable draggedObject;
        private WorldInteractionContext dragContext;

        public bool IsDragging => draggedObject != null;

        public bool TryBegin(IWorldInteraction_Pointer_Draggable target, WorldInteractionContext context)
        {
            if (target == null || !target.BeginPointerDrag(context))
            {
                return false;
            }

            draggedObject = target;
            dragContext = context;
            return true;
        }

        public void Update(Camera camera, Vector2 screenPosition)
        {
            if (draggedObject == null || camera == null)
            {
                return;
            }

            var ray = camera.ScreenPointToRay(screenPosition);
            var context = new WorldInteractionContext(camera, ray, dragContext.Hit, screenPosition);
            draggedObject.UpdatePointerDrag(context);
            dragContext = context;
        }

        public void End(Camera camera, Vector2 screenPosition)
        {
            if (draggedObject == null)
            {
                return;
            }

            Update(camera, screenPosition);
            draggedObject.EndPointerDrag(dragContext);
            draggedObject = null;
        }

        public void Cancel()
        {
            draggedObject?.CancelPointerDrag();
            draggedObject = null;
        }
    }

    internal sealed class WorldInteraction_Runtime_DragFlow
    {
        public WorldInteraction_Drag_Object DraggedObject { get; private set; }

        public bool TryBegin(WorldInteraction_Drag_Object target)
        {
            DraggedObject = target;
            DraggedObject?.BeginDrag();

            if (DraggedObject == null || DraggedObject.IsHeld)
            {
                return DraggedObject != null;
            }

            DraggedObject = null;
            return false;
        }

        public void Clear()
        {
            DraggedObject = null;
        }

        public void ReleaseToPhysics()
        {
            DraggedObject?.ReleaseToPhysics();
            DraggedObject = null;
        }
    }

    internal sealed class WorldInteraction_Runtime_HoverFlow
    {
        private IWorldInteraction_Surface_Hoverable hoveredObject;
        private WorldInteractionContext hoveredContext;

        public void End(bool debugHover, Action<string> log)
        {
            if (hoveredObject == null)
            {
                return;
            }

            if (debugHover)
            {
                log?.Invoke($"HoverEnd {WorldInteraction_Runtime_ComponentLookup.GetDebugName(hoveredObject)}.");
            }

            hoveredObject.HoverEnd(hoveredContext);
            hoveredObject = null;
        }

        public void SetHover(IWorldInteraction_Surface_Hoverable hoverable, WorldInteractionContext context, bool debugHover, Action<string> log)
        {
            if (ReferenceEquals(hoveredObject, hoverable))
            {
                hoveredContext = context;
                return;
            }

            End(debugHover, log);
            hoveredObject = hoverable;
            hoveredContext = context;
            if (debugHover)
            {
                log?.Invoke($"HoverStart {WorldInteraction_Runtime_ComponentLookup.GetDebugName(hoveredObject)}.");
            }

            hoveredObject.HoverStart(context);
        }

        public void RefreshSlotHover(WorldInteraction_Slot_Base slot, WorldInteractionContext context, bool debugHover, Action<string> log)
        {
            if (ReferenceEquals(hoveredObject, slot))
            {
                hoveredContext = context;
                slot.RefreshHeldItemHover();
                return;
            }

            End(debugHover, log);
            hoveredObject = slot;
            hoveredContext = context;
            slot.HoverStart(context);
        }
    }

    internal sealed class WorldInteraction_Runtime_SwingFlow
    {
        private readonly HashSet<Rigidbody> affectedBodies = new();
        private readonly HashSet<MonoBehaviour> affectedSwingables = new();
        private bool hasPreviousSwingPoint;
        private Vector3 previousSwingPoint;

        public void Reset()
        {
            hasPreviousSwingPoint = false;
            previousSwingPoint = default;
            affectedBodies.Clear();
            affectedSwingables.Clear();
        }

        public void ClearPointerHistory()
        {
            hasPreviousSwingPoint = false;
        }

        public bool TryConsumePointerPoint(Vector3 point, float minCursorMoveDistance, out Vector3 direction, out float cursorSpeed)
        {
            direction = default;
            cursorSpeed = 0f;
            if (!hasPreviousSwingPoint)
            {
                previousSwingPoint = point;
                hasPreviousSwingPoint = true;
                return false;
            }

            var delta = point - previousSwingPoint;
            previousSwingPoint = point;
            var distance = delta.magnitude;

            if (Time.deltaTime <= 0f || distance < minCursorMoveDistance)
            {
                return false;
            }

            direction = delta / distance;
            cursorSpeed = distance / Time.deltaTime;
            return true;
        }

        public void ApplySwing(
            Vector3 center,
            Vector3 direction,
            float cursorSpeed,
            Camera camera,
            Vector2 screenPosition,
            Transform planeTransform,
            float swingRadius,
            LayerMask swingMask,
            QueryTriggerInteraction triggerInteraction,
            float maxCursorSpeed,
            float horizontalImpulse,
            float upwardImpulse,
            float angularImpulse,
            ForceMode forceMode,
            Func<int, WorldInteraction_Runtime_Controller.SwingInteractionType> getInteractionForLayer,
            Action<string> log)
        {
            affectedBodies.Clear();
            affectedSwingables.Clear();

            var cappedCursorSpeed = maxCursorSpeed > 0f ? Mathf.Min(cursorSpeed, maxCursorSpeed) : cursorSpeed;
            var up = planeTransform != null ? planeTransform.up : Vector3.up;
            var force = (direction * horizontalImpulse + up * upwardImpulse) * cappedCursorSpeed;
            var torqueAxis = Vector3.Cross(up, direction).normalized;
            var torque = torqueAxis * angularImpulse * cappedCursorSpeed;
            var colliders = Physics.OverlapSphere(center, swingRadius, swingMask, triggerInteraction);
            log?.Invoke($"Swing at {center}, colliders={colliders.Length}, speed={cappedCursorSpeed}.");

            foreach (var collider in colliders)
            {
                var interactionType = getInteractionForLayer(collider.gameObject.layer);
                if (interactionType == WorldInteraction_Runtime_Controller.SwingInteractionType.None)
                {
                    continue;
                }

                var body = collider.attachedRigidbody != null
                    ? collider.attachedRigidbody
                    : collider.GetComponentInParent<Rigidbody>();

                var context = new WorldSwingContext(
                    camera,
                    screenPosition,
                    center,
                    direction,
                    cappedCursorSpeed,
                    force,
                    torque,
                    collider,
                    body);

                if ((interactionType & WorldInteraction_Runtime_Controller.SwingInteractionType.Callback) != 0)
                {
                    InvokeSwingCallbacks(collider, context);
                }

                if ((interactionType & WorldInteraction_Runtime_Controller.SwingInteractionType.Impulse) == 0
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

        private void InvokeSwingCallbacks(Collider collider, WorldSwingContext context)
        {
            var behaviours = collider.GetComponentsInParent<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IWorldInteraction_Swing_Target swingable && affectedSwingables.Add(behaviour))
                {
                    swingable.Swing(context);
                }
            }
        }
    }
}

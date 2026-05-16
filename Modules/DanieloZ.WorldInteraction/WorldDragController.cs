using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public class WorldDragController : MonoBehaviour
    {
        private enum DragProjectionMode
        {
            WorldY,
            PlaneTransform
        }

        [Header("Camera")]
        [SerializeField] private Camera fallbackCamera;

        [Header("Raycast")]
        [SerializeField] private LayerMask draggableMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float rayDistance = 500f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Projection")]
        [SerializeField] private DragProjectionMode projectionMode = DragProjectionMode.WorldY;
        [SerializeField] private Transform planeTransform;
        [SerializeField] private float worldY = 1.25f;
        [SerializeField] private float planeOffset = 1.25f;

        private WorldDraggable draggedObject;

        private void Update()
        {
            var scroll = Input.mouseScrollDelta.y;
            if (!Mathf.Approximately(scroll, 0f))
            {
                WorldInteractionInputGate.TryConsumeWheelForHeldObject(scroll);
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryBeginDrag();
            }

            if (draggedObject != null && Input.GetMouseButton(0))
            {
                UpdateDragPosition();
            }

            if (draggedObject != null && Input.GetMouseButtonUp(0))
            {
                draggedObject.ReleaseToPhysics();
                draggedObject = null;
            }
        }

        private void TryBeginDrag()
        {
            var camera = GetCamera();
            if (camera == null)
            {
                return;
            }

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, rayDistance, draggableMask, triggerInteraction))
            {
                return;
            }

            draggedObject = hit.collider.GetComponentInParent<WorldDraggable>();
            if (draggedObject == null)
            {
                return;
            }

            draggedObject.BeginDrag();
            if (!draggedObject.IsHeld)
            {
                draggedObject = null;
                return;
            }

            UpdateDragPosition();
        }

        private void UpdateDragPosition()
        {
            if (draggedObject == null || !TryGetPointerPoint(out var point))
            {
                return;
            }

            draggedObject.DragToGripPosition(point);
        }

        private bool TryGetPointerPoint(out Vector3 point)
        {
            point = default;
            var camera = GetCamera();
            if (camera == null)
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(Input.mousePosition);

            if (projectionMode == DragProjectionMode.PlaneTransform && planeTransform != null)
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

        private Camera GetCamera()
        {
            return CameraManager.CurrentCamera != null ? CameraManager.CurrentCamera : fallbackCamera;
        }
    }
}

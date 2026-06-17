using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public class WorldInteraction_Pointer_Raycaster : MonoBehaviour
    {
        [SerializeField] private Camera fallbackCamera;
        [SerializeField] private LayerMask mask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float maxDistance = 500f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        public bool TryRaycast(out RaycastHit hit)
        {
            hit = default;
            var camera = GetCamera();
            if (camera == null)
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out hit, maxDistance, mask, triggerInteraction);
        }

        public bool TryGetComponentInParent<T>(out T component) where T : Component
        {
            component = null;

            if (!TryRaycast(out var hit))
            {
                return false;
            }

            component = hit.collider.GetComponentInParent<T>();
            return component != null;
        }

        private Camera GetCamera()
        {
            return CameraManager.CurrentCamera != null ? CameraManager.CurrentCamera : fallbackCamera;
        }
    }
}

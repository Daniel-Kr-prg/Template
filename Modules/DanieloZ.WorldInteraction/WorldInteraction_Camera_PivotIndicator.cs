using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteraction_Camera_PivotIndicator : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float radius = 0.35f;
        [SerializeField] private Collider indicatorCollider;
        [SerializeField] private bool drawGizmos = true;

        public float Radius
        {
            get
            {
                if (indicatorCollider == null)
                {
                    return radius;
                }

                var extents = indicatorCollider.bounds.extents;
                return Mathf.Max(radius, extents.x, extents.z);
            }
        }

        public Vector3 Position => transform.position;

        private void Reset()
        {
            indicatorCollider = GetComponent<Collider>();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}

using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public sealed class WorldWaterVolume : MonoBehaviour
    {
        [Header("Forces")]
        [SerializeField, Min(0f)] private float upwardAcceleration = 20f;
        [SerializeField, Min(0f)] private float shoreAcceleration = 12f;
        [SerializeField, Min(0f)] private float waveFrequency = 2.5f;
        [SerializeField, Range(0f, 1f)] private float waveAmplitude = 0.35f;
        [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

        [Header("Return")]
        [SerializeField] private LayerMask shoreReturnMask = ~0;
        [SerializeField] private Transform shoreTarget;
        [SerializeField] private WorldHardBoundsRecovery hardBoundsRecovery;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoColor = new(0f, 0.45f, 1f, 0.25f);

        private Collider volumeCollider;

        private void Reset()
        {
            volumeCollider = GetComponent<Collider>();
            volumeCollider.isTrigger = true;
        }

        private void Awake()
        {
            volumeCollider = GetComponent<Collider>();
            volumeCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            RegisterForHardBounds(other);
        }

        private void OnTriggerStay(Collider other)
        {
            var targetBody = other.attachedRigidbody;
            if (targetBody == null)
            {
                return;
            }

            targetBody.AddForce(Vector3.up * upwardAcceleration, forceMode);

            if (!IsInLayerMask(other.gameObject.layer, shoreReturnMask))
            {
                return;
            }

            ApplyShoreForce(targetBody);
            hardBoundsRecovery?.Register(targetBody);
        }

        private void RegisterForHardBounds(Collider other)
        {
            var targetBody = other.attachedRigidbody;
            if (targetBody != null && IsInLayerMask(other.gameObject.layer, shoreReturnMask))
            {
                hardBoundsRecovery?.Register(targetBody);
            }
        }

        private void ApplyShoreForce(Rigidbody targetBody)
        {
            if (shoreTarget == null || shoreAcceleration <= 0f)
            {
                return;
            }

            var direction = shoreTarget.position - targetBody.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var wave = 1f + Mathf.Sin(Time.time * Mathf.Max(0.01f, waveFrequency) * Mathf.PI * 2f) * waveAmplitude;
            targetBody.AddForce(direction.normalized * shoreAcceleration * wave, forceMode);
        }

        private static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            var targetCollider = volumeCollider != null ? volumeCollider : GetComponent<Collider>();
            if (targetCollider == null)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(targetCollider.bounds.center, targetCollider.bounds.size);
        }
    }
}

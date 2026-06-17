using Sirenix.OdinInspector;
using UnityEngine;
using OdinShowIf = Sirenix.OdinInspector.ShowIfAttribute;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public sealed class WorldInteraction_Surface_WaterVolume : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Forces")]
        [SerializeField, Min(0f)] private float upwardAcceleration = 20f;
        [FoldoutGroup("Forces")]
        [SerializeField, Min(0f)] private float shoreAcceleration = 12f;
        [FoldoutGroup("Forces")]
        [SerializeField, Min(0f)] private float waveFrequency = 2.5f;
        [FoldoutGroup("Forces")]
        [SerializeField, Range(0f, 1f)] private float waveAmplitude = 0.35f;
        [FoldoutGroup("Forces")]
        [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

        [FoldoutGroup("Return")]
        [SerializeField] private LayerMask shoreReturnMask = ~0;
        [FoldoutGroup("Return")]
        [SerializeField] private Transform shoreTarget;
        [FoldoutGroup("Return")]
        [SerializeField] private WorldInteraction_Drag_HardBoundsRecovery hardBoundsRecovery;

        [FoldoutGroup("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [FoldoutGroup("Debug")]
        [OdinShowIf(nameof(drawGizmos))]
        [SerializeField] private Color gizmoColor = new(0f, 0.45f, 1f, 0.25f);

        #endregion

        #region Runtime State

        private Collider volumeCollider;

        #endregion

        #region Unity Lifecycle

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

        #endregion

        #region Water Forces

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

        #endregion

        #region Helpers

        private static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        #endregion
    }
}

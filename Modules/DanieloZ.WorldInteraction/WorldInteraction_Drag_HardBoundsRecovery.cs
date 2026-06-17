using System.Collections.Generic;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteraction_Drag_HardBoundsRecovery : MonoBehaviour
    {
        [Header("Bounds")]
        [SerializeField] private WorldInteraction_Drag_BoxConstraintSettings hardBox = new();
        [SerializeField] private Transform returnTarget;

        [Header("Forces")]
        [SerializeField, Min(0f)] private float returnAcceleration = 18f;
        [SerializeField, Min(0f)] private float upwardAcceleration = 2f;
        [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

        [Header("Recovery")]
        [SerializeField] private bool ignoreKinematicBodies = true;
        [SerializeField] private bool resetVelocityOnTeleport = true;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        private readonly HashSet<Rigidbody> trackedBodies = new();
        private readonly List<Rigidbody> cleanup = new();

        public void Register(Rigidbody targetBody)
        {
            if (targetBody != null)
            {
                trackedBodies.Add(targetBody);
            }
        }

        public void Unregister(Rigidbody targetBody)
        {
            if (targetBody != null)
            {
                trackedBodies.Remove(targetBody);
            }
        }

        private void FixedUpdate()
        {
            cleanup.Clear();

            foreach (var targetBody in trackedBodies)
            {
                if (targetBody == null)
                {
                    cleanup.Add(targetBody);
                    continue;
                }

                if (ignoreKinematicBodies && targetBody.isKinematic)
                {
                    continue;
                }

                if (!hardBox.Enabled || hardBox.Contains(targetBody.position))
                {
                    ApplyReturnForce(targetBody);
                    continue;
                }

                Recover(targetBody);
            }

            for (var i = 0; i < cleanup.Count; i++)
            {
                trackedBodies.Remove(cleanup[i]);
            }
        }

        private void Recover(Rigidbody targetBody)
        {
            var edgePoint = hardBox.ClosestPoint(targetBody.position);
            targetBody.position = edgePoint;

            if (resetVelocityOnTeleport)
            {
                targetBody.linearVelocity = Vector3.zero;
                targetBody.angularVelocity = Vector3.zero;
            }

            ApplyReturnForce(targetBody);
        }

        private void ApplyReturnForce(Rigidbody targetBody)
        {
            if (returnTarget == null || returnAcceleration <= 0f)
            {
                return;
            }

            var direction = returnTarget.position - targetBody.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var force = direction.normalized * returnAcceleration + Vector3.up * upwardAcceleration;
            targetBody.AddForce(force, forceMode);
        }

        private void OnDrawGizmosSelected()
        {
            if (drawGizmos)
            {
                hardBox?.DrawGizmos(new Color(1f, 0.15f, 0.1f, 0.75f));
            }
        }
    }
}

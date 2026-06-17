using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public readonly struct WorldInteraction_Pointer_Context
    {
        public WorldInteraction_Pointer_Context(
            Camera camera,
            Ray ray,
            RaycastHit hit,
            bool hasHit,
            Vector2 screenPosition,
            Vector3 gripWorldPosition,
            bool hasGripWorldPosition,
            Ray handRay = default,
            RaycastHit handHit = default,
            bool hasHandRay = false,
            bool hasHandHit = false)
        {
            Camera = camera;
            Ray = ray;
            Hit = hit;
            HasHit = hasHit;
            ScreenPosition = screenPosition;
            GripWorldPosition = gripWorldPosition;
            HasGripWorldPosition = hasGripWorldPosition;
            HandRay = handRay;
            HandHit = handHit;
            HasHandRay = hasHandRay;
            HasHandHit = hasHandHit;
        }

        public Camera Camera { get; }
        public Ray Ray { get; }
        public RaycastHit Hit { get; }
        public bool HasHit { get; }
        public Vector2 ScreenPosition { get; }
        public Vector3 GripWorldPosition { get; }
        public bool HasGripWorldPosition { get; }
        public Ray HandRay { get; }
        public RaycastHit HandHit { get; }
        public bool HasHandRay { get; }
        public bool HasHandHit { get; }

        public Collider HitCollider => HasHit ? Hit.collider : null;
        public Collider HandHitCollider => HasHandHit ? HandHit.collider : null;
        public Ray EffectivePlacementRay => HasHandRay ? HandRay : Ray;

        public WorldInteraction_Pointer_Context WithGripWorldPosition(Vector3 gripWorldPosition)
        {
            return new WorldInteraction_Pointer_Context(
                Camera,
                Ray,
                Hit,
                HasHit,
                ScreenPosition,
                gripWorldPosition,
                true,
                HandRay,
                HandHit,
                HasHandRay,
                HasHandHit);
        }

        public WorldInteraction_Pointer_Context WithHandProjection(Vector3 gripWorldPosition, Ray handRay, RaycastHit handHit, bool hasHandHit)
        {
            return new WorldInteraction_Pointer_Context(
                Camera,
                Ray,
                Hit,
                HasHit,
                ScreenPosition,
                gripWorldPosition,
                true,
                handRay,
                handHit,
                true,
                hasHandHit);
        }
    }
}

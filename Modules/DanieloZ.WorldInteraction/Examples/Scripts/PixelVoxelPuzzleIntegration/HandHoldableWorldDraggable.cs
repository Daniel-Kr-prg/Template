using DanieloZ.WorldInteraction;
using UnityEngine;

namespace PixelLust.PixelVoxelPuzzle
{
    [RequireComponent(typeof(WorldDraggable))]
    public sealed class HandHoldableWorldDraggable : MonoBehaviour, IHandHoldable
    {
        [SerializeField] private WorldDraggable draggable;

        public Transform Transform => transform;
        public Transform GripRoot => draggable != null ? draggable.GripRoot : transform;
        public Rigidbody Body => draggable != null ? draggable.Body : null;

        private void Awake()
        {
            draggable ??= GetComponent<WorldDraggable>();
        }

        public bool CanPickup(HandPointerContext context)
        {
            return enabled
                && gameObject.activeInHierarchy
                && draggable != null
                && !draggable.InteractionsLocked;
        }

        public void OnHandHoverEnter(HandPointerContext context)
        {
        }

        public void OnHandHoverExit()
        {
        }

        public void OnPickup(HandPointerContext context)
        {
            draggable?.BeginDrag();
        }

        public void OnHeldMove(HandPointerContext context)
        {
            if (draggable == null || !context.HasGripWorldPosition)
            {
                return;
            }

            draggable.MoveDragged(context.GripWorldPosition);
        }

        public void OnHeldRotate(float wheelDelta, HandPointerContext context)
        {
            draggable?.TryRotateHeldByWheel(wheelDelta);
        }

        public void OnRelease(HandPointerContext context)
        {
            draggable?.ReleaseToPhysics();
        }

        public void OnCancelHold()
        {
            draggable?.ReleaseToPhysics();
        }
    }
}

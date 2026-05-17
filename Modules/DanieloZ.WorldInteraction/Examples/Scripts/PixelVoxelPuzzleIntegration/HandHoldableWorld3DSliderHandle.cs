using DanieloZ.WorldInteraction;
using UnityEngine;

namespace PixelLust.PixelVoxelPuzzle
{
    [RequireComponent(typeof(WorldDraggable))]
    public sealed class HandHoldableWorld3DSliderHandle : MonoBehaviour, IHandHoldable
    {
        [SerializeField] private World3DSlider slider;
        [SerializeField] private WorldDraggable draggable;

        public Transform Transform => transform;
        public Transform GripRoot => draggable != null ? draggable.GripRoot : transform;
        public Rigidbody Body => draggable != null ? draggable.Body : null;

        private void Awake()
        {
            draggable ??= GetComponent<WorldDraggable>();
            slider ??= GetComponentInParent<World3DSlider>();
        }

        public bool CanPickup(HandPointerContext context)
        {
            return enabled
                && gameObject.activeInHierarchy
                && slider != null
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
            MakeHandleKinematic();
            slider?.TrySetValueFromRay(context.Ray);
        }

        public void OnHeldMove(HandPointerContext context)
        {
            slider?.TrySetValueFromRay(context.Ray);
        }

        public void OnHeldRotate(float wheelDelta, HandPointerContext context)
        {
        }

        public void OnRelease(HandPointerContext context)
        {
            MakeHandleKinematic();
            slider?.TrySetValueFromRay(context.Ray);
        }

        public void OnCancelHold()
        {
            MakeHandleKinematic();
        }

        private void MakeHandleKinematic()
        {
            if (Body == null)
            {
                return;
            }

            Body.isKinematic = true;
            Body.useGravity = false;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
        }
    }
}

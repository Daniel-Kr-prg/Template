using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public class World3DPhysicalButton : WorldDraggable
    {
        [Header("Physical Button")]
        [SerializeField] private string buttonId;
        [SerializeField, Min(0f)] private float slotSnapDuration = 0.18f;
        [SerializeField] private Ease slotSnapEase = Ease.OutCubic;
        [SerializeField] private UnityEvent onInsertedIntoSlot;
        [SerializeField] private UnityEvent onRemovedFromSlot;

        private Tween slotTween;

        public string ButtonId => buttonId;
        public World3DButtonSlotBase CurrentSlot { get; private set; }

        public bool HasMatchingId(World3DButtonSlotBase slot)
        {
            return slot != null && !string.IsNullOrEmpty(buttonId) && buttonId == slot.SlotId;
        }

        public void Setup(string id)
        {
            buttonId = id;
        }

        public void AssignSlot(World3DButtonSlotBase slot)
        {
            if (CurrentSlot == slot)
            {
                return;
            }

            CurrentSlot = slot;
            onInsertedIntoSlot?.Invoke();
        }

        public void ClearSlot(World3DButtonSlotBase slot)
        {
            if (CurrentSlot != slot)
            {
                return;
            }

            CurrentSlot = null;
            onRemovedFromSlot?.Invoke();
        }

        public void SnapToSlot(World3DButtonSlotBase slot)
        {
            if (slot == null || slot.Anchor == null)
            {
                return;
            }

            slotTween?.Kill();

            if (Body != null)
            {
                Body.isKinematic = true;
                Body.useGravity = false;
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }

            slotTween = DOTween.Sequence()
                .Join(transform.DOMove(slot.Anchor.position, slotSnapDuration).SetEase(slotSnapEase))
                .Join(transform.DORotateQuaternion(slot.Anchor.rotation, slotSnapDuration).SetEase(slotSnapEase));
        }

        public override void ReleaseToPhysics()
        {
            base.ReleaseToPhysics();

            if (CurrentSlot != null)
            {
                SnapToSlot(CurrentSlot);
            }
        }

        protected override void OnAfterBeginDrag()
        {
            if (CurrentSlot != null)
            {
                CurrentSlot.RemoveButton(this);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            slotTween?.Kill();
        }
    }
}

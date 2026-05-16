using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace DanieloZ.WorldInteraction
{
    public class World3DSlotItem : WorldDraggable
    {
        [Header("Slot Item")]
        [FormerlySerializedAs("buttonId")]
        [SerializeField] private string itemId;
        [SerializeField, Min(0f)] private float slotSnapDuration = 0.18f;
        [SerializeField] private Ease slotSnapEase = Ease.OutCubic;
        [SerializeField] private UnityEvent onInsertedIntoSlot;
        [SerializeField] private UnityEvent onRemovedFromSlot;

        private Tween slotTween;

        public event Action<World3DSlotItem, World3DButtonSlotBase> InsertedIntoSlot;
        public event Action<World3DSlotItem, World3DButtonSlotBase> RemovedFromSlot;

        public string ItemId => itemId;
        public World3DButtonSlotBase CurrentSlot { get; private set; }

        public bool HasMatchingId(World3DButtonSlotBase slot)
        {
            return slot != null && slot.MatchesId(this);
        }

        public virtual void Setup(string id)
        {
            itemId = id;
        }

        public void AssignSlot(World3DButtonSlotBase slot)
        {
            if (CurrentSlot == slot)
            {
                return;
            }

            CurrentSlot = slot;
            onInsertedIntoSlot?.Invoke();
            InsertedIntoSlot?.Invoke(this, slot);
        }

        public void ClearSlot(World3DButtonSlotBase slot)
        {
            if (CurrentSlot != slot)
            {
                return;
            }

            CurrentSlot = null;
            onRemovedFromSlot?.Invoke();
            RemovedFromSlot?.Invoke(this, slot);
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

            if (slotSnapDuration <= 0f)
            {
                transform.SetPositionAndRotation(slot.Anchor.position, slot.Anchor.rotation);
                return;
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
                CurrentSlot.RemoveItem(this);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            slotTween?.Kill();
        }
    }
}

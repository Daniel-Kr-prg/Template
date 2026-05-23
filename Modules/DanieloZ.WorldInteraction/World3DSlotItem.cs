using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace DanieloZ.WorldInteraction
{
    public class World3DSlotItem : WorldDraggable
    {
        #region Inspector

        [FoldoutGroup("Slot Item")]
        [FormerlySerializedAs("buttonId")]
        [SerializeField] private string itemId;
        [FoldoutGroup("Slot Item")]
        [SerializeField, Min(0f)] private float slotSnapDuration = 0.18f;
        [FoldoutGroup("Slot Item")]
        [SerializeField] private Ease slotSnapEase = Ease.OutCubic;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onInsertedIntoSlot;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onRemovedFromSlot;

        #endregion

        #region Public API

        public event Action<World3DSlotItem, World3DButtonSlotBase> InsertedIntoSlot;
        public event Action<World3DSlotItem, World3DButtonSlotBase> RemovedFromSlot;

        public string ItemId => itemId;
        public World3DButtonSlotBase CurrentSlot { get; private set; }

        #endregion

        #region Runtime State

        private Tween slotTween;

        #endregion

        #region Matching

        public bool HasMatchingId(World3DButtonSlotBase slot)
        {
            return slot != null && slot.MatchesId(this);
        }

        #endregion

        #region Configuration

        public virtual void Setup(string id)
        {
            itemId = id;
        }

        #endregion

        #region Slot Assignment

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

        #endregion

        #region Slot Pose

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

        #endregion

        #region Drag Overrides

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

        #endregion
    }
}

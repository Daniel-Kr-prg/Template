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
        [LabelText("Inserted Local Position")]
        [SerializeField] private Vector3 insertedLocalPosition;
        [FoldoutGroup("Slot Item")]
        [LabelText("Inserted Local Rotation")]
        [SerializeField] private Vector3 insertedLocalEulerRotation;
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
        public Vector3 InsertedLocalPosition => insertedLocalPosition;
        public Vector3 InsertedLocalEulerRotation => insertedLocalEulerRotation;

        #endregion

        #region Runtime State

        private Tween slotTween;
        private Transform parentBeforeSlot;
        private bool hasParentBeforeSlot;

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

            slotTween?.Kill();
            CurrentSlot = null;
            RestoreParentBeforeSlot();
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

            if (Body != null)
            {
                Body.isKinematic = true;
                Body.useGravity = false;
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }

            AttachToSlotAnchor(slot.Anchor);
            MoveToInsertedSlotPose(true);
        }

        public void PreviewInSlot(World3DButtonSlotBase slot)
        {
            if (slot == null || slot.Anchor == null)
            {
                return;
            }

            transform.SetParent(slot.Anchor, true);
            transform.localPosition = insertedLocalPosition;
            transform.localRotation = Quaternion.Euler(insertedLocalEulerRotation);
        }

        public void CaptureInsertedPoseFromSlot(World3DButtonSlotBase slot)
        {
            if (slot == null || slot.Anchor == null)
            {
                return;
            }

            insertedLocalPosition = slot.Anchor.InverseTransformPoint(transform.position);
            insertedLocalEulerRotation = (Quaternion.Inverse(slot.Anchor.rotation) * transform.rotation).eulerAngles;
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

        #region Private Methods

        private void AttachToSlotAnchor(Transform slotAnchor)
        {
            if (slotAnchor == null || transform.parent == slotAnchor)
            {
                return;
            }

            if (!hasParentBeforeSlot)
            {
                parentBeforeSlot = transform.parent;
                hasParentBeforeSlot = true;
            }

            transform.SetParent(slotAnchor, true);
        }

        private void MoveToInsertedSlotPose(bool animated)
        {
            slotTween?.Kill();
            if (!animated || slotSnapDuration <= 0f)
            {
                ApplyInsertedSlotPose();
                return;
            }

            var targetRotation = Quaternion.Euler(insertedLocalEulerRotation);
            var tween = DOTween.Sequence()
                .Join(transform.DOLocalMove(insertedLocalPosition, slotSnapDuration).SetEase(slotSnapEase))
                .Join(transform.DOLocalRotateQuaternion(targetRotation, slotSnapDuration).SetEase(slotSnapEase));
            slotTween = tween;
            tween.OnKill(() =>
            {
                if (slotTween == tween)
                {
                    slotTween = null;
                }
            });
        }

        private void ApplyInsertedSlotPose()
        {
            transform.localPosition = insertedLocalPosition;
            transform.localRotation = Quaternion.Euler(insertedLocalEulerRotation);
        }

        private void RestoreParentBeforeSlot()
        {
            if (!hasParentBeforeSlot)
            {
                return;
            }

            transform.SetParent(parentBeforeSlot, true);
            parentBeforeSlot = null;
            hasParentBeforeSlot = false;
        }

        #endregion
    }
}

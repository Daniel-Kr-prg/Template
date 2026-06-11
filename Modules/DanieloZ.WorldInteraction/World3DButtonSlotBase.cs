using System;
using System.Collections.Generic;
using DanieloZ.Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public class World3DButtonSlotBase : MonoBehaviour, IWorldHoverable
    {
        #region Inspector

        [FoldoutGroup("Slot")]
        [SerializeField] private string slotId;
        [FoldoutGroup("Slot")]
        [SerializeField] private Transform anchor;
        [FoldoutGroup("Slot")]
        [SerializeField] private bool isActive;
        [FormerlySerializedAs("acceptedButtonIds")]
        [FoldoutGroup("Slot")]
        [SerializeField] private List<string> acceptedItemIds = new();

        [FoldoutGroup("ID Matching")]
        [SerializeField] private bool acceptOnlyMatchingId;
        [FormerlySerializedAs("lockButtonOnMatchingId")]
        [FoldoutGroup("ID Matching")]
        [SerializeField] private bool lockItemOnMatchingId;
        [FoldoutGroup("ID Matching")]
        [SerializeField] private bool callEventManagerOnMatchingId = true;

        [FoldoutGroup("Indicators")]
        [SerializeField] private GameObject activeIndicator;
        [FoldoutGroup("Indicators")]
        [SerializeField] private GameObject hoverPreview;
        [FoldoutGroup("Indicators")]
        [SerializeField] private GameObject acceptedHoverIndicator;
        [FoldoutGroup("Indicators")]
        [SerializeField] private GameObject rejectedHoverIndicator;
        [FoldoutGroup("Indicators/Hover Preview Animation")]
        [SerializeField] private bool animateHoverPreview = true;
        [FoldoutGroup("Indicators/Hover Preview Animation")]
        [SerializeField, Min(0f)] private float hoverPreviewPulseSpeed = 5f;
        [FoldoutGroup("Indicators/Hover Preview Animation")]
        [SerializeField, Min(0f)] private float hoverPreviewScaleAmplitude = 0.035f;
        [FoldoutGroup("Indicators/Hover Preview Animation")]
        [SerializeField, Min(0f)] private float hoverPreviewBobAmplitude = 0.025f;

        [FoldoutGroup("Events")]
        [FormerlySerializedAs("onButtonInserted")]
        [SerializeField] private UnityEvent onItemInserted;
        [FoldoutGroup("Events")]
        [FormerlySerializedAs("onButtonRemoved")]
        [SerializeField] private UnityEvent onItemRemoved;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onSlotActivated;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onSlotDeactivated;
        [FoldoutGroup("Events")]
        [FormerlySerializedAs("onMatchingButtonInserted")]
        [SerializeField] private UnityEvent onMatchingItemInserted;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onHeldItemHoverAccepted;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onHeldItemHoverRejected;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onHeldItemHoverEnded;

        #endregion

        #region Public API

        public event Action<World3DButtonSlotBase, World3DSlotItem> ItemInserted;
        public event Action<World3DButtonSlotBase, World3DSlotItem> ItemRemoved;
        public event Action<World3DButtonSlotBase, World3DSlotItem, string> MatchingItemInserted;
        public event Action<World3DButtonSlotBase, World3DPhysicalButton> ButtonInserted;
        public event Action<World3DButtonSlotBase, World3DPhysicalButton> ButtonRemoved;
        public event Action<World3DButtonSlotBase, World3DPhysicalButton, string> MatchingButtonInserted;
        public event Action<World3DButtonSlotBase, bool> ActiveStateChanged;

        public string SlotId => slotId;
        public Transform Anchor => anchor != null ? anchor : transform;
        public World3DSlotItem CurrentItem { get; private set; }
        public World3DPhysicalButton CurrentButton => CurrentItem as World3DPhysicalButton;
        public bool HasItem => CurrentItem != null;
        public bool HasButton => HasItem;
        public bool IsActive => isActive;

        #endregion

        #region Internal State

        private Collider triggerCollider;
        private World3DSlotItem hoveredItem;
        private bool hasHoverResult;
        private bool hoverCanInsert;
        private bool hoverPreviewVisible;
        private bool hasHoverPreviewBasePose;
        private Vector3 hoverPreviewBaseLocalPosition;
        private Vector3 hoverPreviewBaseLocalScale;

        #endregion

        #region Unity Lifecycle

        protected virtual void Reset()
        {
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        protected virtual void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
            ClearHeldItemHover();
            SetActiveState(isActive);
        }

        private void OnDisable()
        {
            ClearHeldItemHover();
        }

        private void Update()
        {
            if (!hoverPreviewVisible || hoverPreview == null || !animateHoverPreview)
            {
                return;
            }

            EnsureHoverPreviewBasePose();
            var pulse = Mathf.Sin(Time.time * hoverPreviewPulseSpeed);
            var scale = 1f + pulse * hoverPreviewScaleAmplitude;
            hoverPreview.transform.localScale = hoverPreviewBaseLocalScale * scale;
            hoverPreview.transform.localPosition = hoverPreviewBaseLocalPosition
                + Vector3.up * (pulse * hoverPreviewBobAmplitude);
        }

        #endregion

        #region Acceptance

        public virtual bool CanAccept(World3DSlotItem item)
        {
            if (item == null)
            {
                return false;
            }

            if (acceptOnlyMatchingId && !MatchesId(item))
            {
                return false;
            }

            return acceptedItemIds == null
                || acceptedItemIds.Count == 0
                || acceptedItemIds.Contains(item.ItemId);
        }

        public virtual bool CanAccept(World3DPhysicalButton button)
        {
            return CanAccept((World3DSlotItem)button);
        }

        public bool MatchesId(World3DSlotItem item)
        {
            return item != null && !string.IsNullOrEmpty(slotId) && item.ItemId == slotId;
        }

        public bool MatchesId(World3DPhysicalButton button)
        {
            return MatchesId((World3DSlotItem)button);
        }

        public virtual bool CanInsert(World3DSlotItem item)
        {
            if (!CanAccept(item))
            {
                return false;
            }

            if (item.InteractionsLocked && item.CurrentSlot != this)
            {
                return false;
            }

            return CurrentItem == null
                || CurrentItem == item
                || !CurrentItem.InteractionsLocked;
        }

        public virtual bool CanInsert(World3DPhysicalButton button)
        {
            return CanInsert((World3DSlotItem)button);
        }

        #endregion

        #region Item Placement

        public virtual bool TryInsert(World3DSlotItem item)
        {
            if (!CanInsert(item))
            {
                return false;
            }

            if (CurrentItem == item)
            {
                item.SnapToSlot(this);
                return true;
            }

            if (item.CurrentSlot != null && item.CurrentSlot != this)
            {
                item.CurrentSlot.RemoveItem(item);
            }

            if (CurrentItem != null && CurrentItem != item)
            {
                if (CurrentItem.InteractionsLocked)
                {
                    return false;
                }

                RemoveItem(CurrentItem);
            }

            CurrentItem = item;
            item.AssignSlot(this);
            item.SnapToSlot(this);
            SetActiveState(true);

            onItemInserted?.Invoke();
            ItemInserted?.Invoke(this, item);
            if (item is World3DPhysicalButton button)
            {
                ButtonInserted?.Invoke(this, button);
            }

            if (MatchesId(item))
            {
                HandleMatchingItemInserted(item);
            }

            return true;
        }

        public virtual bool TryInsert(World3DPhysicalButton button)
        {
            return TryInsert((World3DSlotItem)button);
        }

        public virtual void RemoveItem(World3DSlotItem item)
        {
            if (item == null || CurrentItem != item)
            {
                return;
            }

            CurrentItem = null;
            SetActiveState(false);
            if (lockItemOnMatchingId && MatchesId(item))
            {
                item.SetInteractionsLocked(false);
            }

            item.ClearSlot(this);
            onItemRemoved?.Invoke();
            ItemRemoved?.Invoke(this, item);
            if (item is World3DPhysicalButton button)
            {
                ButtonRemoved?.Invoke(this, button);
            }
        }

        public virtual void RemoveButton(World3DPhysicalButton button)
        {
            RemoveItem(button);
        }

        public virtual bool TryInsertHeldItem(World3DSlotItem item)
        {
            if (item == null || !CanInsert(item) || !TryReleaseHeldDraggable(item))
            {
                return false;
            }

            return TryInsert(item);
        }

        public virtual bool TryInsertHeldButton(World3DPhysicalButton button)
        {
            return TryInsertHeldItem(button);
        }

        #endregion

        #region State

        public virtual void SetActiveState(bool active)
        {
            if (isActive == active)
            {
                if (activeIndicator != null)
                {
                    activeIndicator.SetActive(active);
                }

                return;
            }

            isActive = active;

            if (activeIndicator != null)
            {
                activeIndicator.SetActive(active);
            }

            if (isActive)
            {
                onSlotActivated?.Invoke();
            }
            else
            {
                onSlotDeactivated?.Invoke();
            }

            ActiveStateChanged?.Invoke(this, isActive);
        }

        #endregion

        #region Hover

        public void HoverStart(WorldInteractionContext context)
        {
            RefreshHeldItemHover();
        }

        public void HoverEnd(WorldInteractionContext context)
        {
            ClearHeldItemHover();
        }

        public void RefreshHeldItemHover()
        {
            RefreshHeldItemHover(WorldInteractionInputGate.HeldObject as World3DSlotItem);
        }

        public void RefreshHeldItemHover(World3DSlotItem item)
        {
            if (item == null)
            {
                ClearHeldItemHover();
                return;
            }

            var canInsert = CanInsert(item);
            if (hasHoverResult && hoveredItem == item && hoverCanInsert == canInsert)
            {
                return;
            }

            hoveredItem = item;
            hasHoverResult = true;
            hoverCanInsert = canInsert;

            if (acceptedHoverIndicator != null)
            {
                acceptedHoverIndicator.SetActive(canInsert);
            }

            if (rejectedHoverIndicator != null)
            {
                rejectedHoverIndicator.SetActive(!canInsert);
            }

            SetHoverPreviewVisible(canInsert);

            if (canInsert)
            {
                onHeldItemHoverAccepted?.Invoke();
            }
            else
            {
                onHeldItemHoverRejected?.Invoke();
            }
        }

        private void ClearHeldItemHover()
        {
            if (acceptedHoverIndicator != null)
            {
                acceptedHoverIndicator.SetActive(false);
            }

            if (rejectedHoverIndicator != null)
            {
                rejectedHoverIndicator.SetActive(false);
            }

            SetHoverPreviewVisible(false);

            if (hasHoverResult)
            {
                onHeldItemHoverEnded?.Invoke();
            }

            hoveredItem = null;
            hasHoverResult = false;
            hoverCanInsert = false;
        }

        private void SetHoverPreviewVisible(bool visible)
        {
            if (hoverPreview == null)
            {
                hoverPreviewVisible = false;
                return;
            }

            EnsureHoverPreviewBasePose();
            if (!visible)
            {
                hoverPreview.transform.localPosition = hoverPreviewBaseLocalPosition;
                hoverPreview.transform.localScale = hoverPreviewBaseLocalScale;
            }

            if (hoverPreview.activeSelf != visible)
            {
                hoverPreview.SetActive(visible);
            }

            hoverPreviewVisible = visible;
        }

        private void EnsureHoverPreviewBasePose()
        {
            if (hasHoverPreviewBasePose || hoverPreview == null)
            {
                return;
            }

            hoverPreviewBaseLocalPosition = hoverPreview.transform.localPosition;
            hoverPreviewBaseLocalScale = hoverPreview.transform.localScale;
            hasHoverPreviewBasePose = true;
        }

        #endregion

        #region Matching

        private void HandleMatchingItemInserted(World3DSlotItem item)
        {
            if (lockItemOnMatchingId)
            {
                item.SetInteractionsLocked(true);
            }

            onMatchingItemInserted?.Invoke();
            MatchingItemInserted?.Invoke(this, item, slotId);
            if (item is World3DPhysicalButton button)
            {
                MatchingButtonInserted?.Invoke(this, button, slotId);
            }

            if (callEventManagerOnMatchingId && EventManager.HaveInstance())
            {
                EventManager.CallEvent(
                    EventName.WorldInteraction_OnMatchingSlotInserted,
                    new object[] { slotId, this, item });
            }
        }

        private static bool TryReleaseHeldDraggable(World3DSlotItem item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.IsHeld)
            {
                item.Release();
                return true;
            }

            var draggables = item.GetComponents<WorldDraggable>();
            for (var i = 0; i < draggables.Length; i++)
            {
                var draggable = draggables[i];
                if (draggable == null || !draggable.IsHeld)
                {
                    continue;
                }

                draggable.Release();
                return true;
            }

            return false;
        }

        #endregion
    }
}

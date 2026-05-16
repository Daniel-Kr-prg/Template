using System;
using System.Collections.Generic;
using DanieloZ.Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public class World3DButtonSlotBase : MonoBehaviour, IWorldHoverable
    {
        [Header("Slot")]
        [SerializeField] private string slotId;
        [SerializeField] private Transform anchor;
        [SerializeField] private bool isActive;
        [FormerlySerializedAs("acceptedButtonIds")]
        [SerializeField] private List<string> acceptedItemIds = new();

        [Header("ID Matching")]
        [SerializeField] private bool acceptOnlyMatchingId;
        [FormerlySerializedAs("lockButtonOnMatchingId")]
        [SerializeField] private bool lockItemOnMatchingId;
        [SerializeField] private bool callEventManagerOnMatchingId = true;

        [Header("Indicator")]
        [SerializeField] private GameObject activeIndicator;
        [SerializeField] private GameObject acceptedHoverIndicator;
        [SerializeField] private GameObject rejectedHoverIndicator;

        [Header("Events")]
        [FormerlySerializedAs("onButtonInserted")]
        [SerializeField] private UnityEvent onItemInserted;
        [FormerlySerializedAs("onButtonRemoved")]
        [SerializeField] private UnityEvent onItemRemoved;
        [SerializeField] private UnityEvent onSlotActivated;
        [SerializeField] private UnityEvent onSlotDeactivated;
        [FormerlySerializedAs("onMatchingButtonInserted")]
        [SerializeField] private UnityEvent onMatchingItemInserted;
        [SerializeField] private UnityEvent onHeldItemHoverAccepted;
        [SerializeField] private UnityEvent onHeldItemHoverRejected;
        [SerializeField] private UnityEvent onHeldItemHoverEnded;

        private Collider triggerCollider;
        private World3DSlotItem hoveredItem;
        private bool hasHoverResult;
        private bool hoverCanInsert;

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
            if (item == null || !item.IsHeld || !CanInsert(item))
            {
                return false;
            }

            item.Release();
            return TryInsert(item);
        }

        public virtual bool TryInsertHeldButton(World3DPhysicalButton button)
        {
            return TryInsertHeldItem(button);
        }

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

            if (canInsert)
            {
                onHeldItemHoverAccepted?.Invoke();
            }
            else
            {
                onHeldItemHoverRejected?.Invoke();
            }
        }

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

        private void OnDisable()
        {
            ClearHeldItemHover();
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

            if (hasHoverResult)
            {
                onHeldItemHoverEnded?.Invoke();
            }

            hoveredItem = null;
            hasHoverResult = false;
            hoverCanInsert = false;
        }
    }
}

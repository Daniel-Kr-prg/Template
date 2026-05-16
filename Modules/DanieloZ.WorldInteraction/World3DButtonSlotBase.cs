using System;
using System.Collections.Generic;
using DanieloZ.Managers;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public class World3DButtonSlotBase : MonoBehaviour
    {
        [Header("Slot")]
        [SerializeField] private string slotId;
        [SerializeField] private Transform anchor;
        [SerializeField] private bool isActive;
        [SerializeField] private List<string> acceptedButtonIds = new();

        [Header("ID Matching")]
        [SerializeField] private bool acceptOnlyMatchingId;
        [SerializeField] private bool lockButtonOnMatchingId;
        [SerializeField] private bool callEventManagerOnMatchingId = true;

        [Header("Indicator")]
        [SerializeField] private GameObject activeIndicator;

        [Header("Events")]
        [SerializeField] private UnityEvent onButtonInserted;
        [SerializeField] private UnityEvent onButtonRemoved;
        [SerializeField] private UnityEvent onSlotActivated;
        [SerializeField] private UnityEvent onSlotDeactivated;
        [SerializeField] private UnityEvent onMatchingButtonInserted;

        private readonly HashSet<World3DPhysicalButton> candidates = new();
        private Collider triggerCollider;

        public event Action<World3DButtonSlotBase, World3DPhysicalButton> ButtonInserted;
        public event Action<World3DButtonSlotBase, World3DPhysicalButton> ButtonRemoved;
        public event Action<World3DButtonSlotBase, World3DPhysicalButton, string> MatchingButtonInserted;
        public event Action<World3DButtonSlotBase, bool> ActiveStateChanged;

        public string SlotId => slotId;
        public Transform Anchor => anchor != null ? anchor : transform;
        public World3DPhysicalButton CurrentButton { get; private set; }
        public bool HasButton => CurrentButton != null;
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
            SetActiveState(isActive);
        }

        public virtual bool CanAccept(World3DPhysicalButton button)
        {
            if (button == null)
            {
                return false;
            }

            if (acceptOnlyMatchingId && !MatchesId(button))
            {
                return false;
            }

            return acceptedButtonIds == null
                || acceptedButtonIds.Count == 0
                || acceptedButtonIds.Contains(button.ButtonId);
        }

        public bool MatchesId(World3DPhysicalButton button)
        {
            return button != null && !string.IsNullOrEmpty(slotId) && button.ButtonId == slotId;
        }

        public virtual bool TryInsert(World3DPhysicalButton button)
        {
            if (!CanAccept(button))
            {
                return false;
            }

            if (CurrentButton == button)
            {
                button.SnapToSlot(this);
                return true;
            }

            if (CurrentButton != null && CurrentButton != button)
            {
                if (CurrentButton.InteractionsLocked)
                {
                    return false;
                }

                RemoveButton(CurrentButton);
            }

            CurrentButton = button;
            button.AssignSlot(this);
            button.SnapToSlot(this);
            onButtonInserted?.Invoke();
            ButtonInserted?.Invoke(this, button);

            if (MatchesId(button))
            {
                HandleMatchingButtonInserted(button);
            }

            return true;
        }

        public virtual void RemoveButton(World3DPhysicalButton button)
        {
            if (CurrentButton != button)
            {
                return;
            }

            CurrentButton = null;
            if (lockButtonOnMatchingId && MatchesId(button))
            {
                button.SetInteractionsLocked(false);
            }

            button.ClearSlot(this);
            onButtonRemoved?.Invoke();
            ButtonRemoved?.Invoke(this, button);
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

        private void OnTriggerEnter(Collider other)
        {
            var button = other.GetComponentInParent<World3DPhysicalButton>();
            if (button == null)
            {
                return;
            }

            candidates.Add(button);
            button.Released += HandleButtonReleased;

            if (!button.IsHeld)
            {
                TryInsert(button);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var button = other.GetComponentInParent<World3DPhysicalButton>();
            if (button == null)
            {
                return;
            }

            candidates.Remove(button);
            button.Released -= HandleButtonReleased;
        }

        private void HandleButtonReleased(WorldDraggable draggable)
        {
            if (draggable is World3DPhysicalButton button && candidates.Contains(button))
            {
                TryInsert(button);
            }
        }

        private void HandleMatchingButtonInserted(World3DPhysicalButton button)
        {
            if (lockButtonOnMatchingId)
            {
                button.SetInteractionsLocked(true);
            }

            onMatchingButtonInserted?.Invoke();
            MatchingButtonInserted?.Invoke(this, button, slotId);

            if (callEventManagerOnMatchingId && EventManager.HaveInstance())
            {
                EventManager.CallEvent(
                    EventName.WorldInteraction_OnMatchingSlotInserted,
                    new object[] { slotId, this, button });
            }
        }

        private void OnDisable()
        {
            foreach (var button in candidates)
            {
                if (button != null)
                {
                    button.Released -= HandleButtonReleased;
                }
            }

            candidates.Clear();
        }
    }
}

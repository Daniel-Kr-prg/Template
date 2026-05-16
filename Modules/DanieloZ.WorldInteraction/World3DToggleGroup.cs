using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public sealed class World3DToggleGroup : MonoBehaviour
    {
        [Header("Toggle Objects")]
        [SerializeField] private List<World3DToggleObject> toggleObjects = new();
        [SerializeField] private int defaultActiveIndex;

        [Header("Physical Marker")]
        [SerializeField] private World3DPhysicalButton markerButton;
        [SerializeField] private List<World3DButtonSlotBase> markerSlots = new();
        [SerializeField] private bool returnMarkerToActiveSlot = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onActiveChanged;

        private int activeIndex = -1;

        public int ActiveIndex => activeIndex;
        public World3DToggleObject ActiveObject => activeIndex >= 0 && activeIndex < toggleObjects.Count ? toggleObjects[activeIndex] : null;
        public World3DButtonSlotBase ActiveSlot => activeIndex >= 0 && activeIndex < markerSlots.Count ? markerSlots[activeIndex] : null;

        private void Awake()
        {
            for (var i = 0; i < markerSlots.Count; i++)
            {
                if (markerSlots[i] != null)
                {
                    markerSlots[i].ButtonInserted += HandleSlotButtonInserted;
                }
            }

            if (markerButton != null)
            {
                markerButton.Released += HandleMarkerReleased;
            }

            SetActiveIndex(Mathf.Clamp(defaultActiveIndex, 0, Mathf.Max(0, Mathf.Max(toggleObjects.Count, markerSlots.Count) - 1)));
        }

        public void SetActiveIndex(int index)
        {
            var maxCount = Mathf.Max(toggleObjects.Count, markerSlots.Count);
            if (index < 0 || index >= maxCount)
            {
                return;
            }

            activeIndex = index;

            for (var i = 0; i < toggleObjects.Count; i++)
            {
                if (toggleObjects[i] != null)
                {
                    toggleObjects[i].SetActiveState(i == activeIndex);
                }
            }

            for (var i = 0; i < markerSlots.Count; i++)
            {
                if (markerSlots[i] != null)
                {
                    markerSlots[i].SetActiveState(i == activeIndex);
                }
            }

            if (markerButton != null && ActiveSlot != null && !markerButton.IsHeld)
            {
                ActiveSlot.TryInsert(markerButton);
            }

            onActiveChanged?.Invoke();
        }

        public void SetActiveObject(World3DToggleObject toggleObject)
        {
            var index = toggleObjects.IndexOf(toggleObject);
            if (index >= 0)
            {
                SetActiveIndex(index);
            }
        }

        public void SetActiveSlot(World3DButtonSlotBase slot)
        {
            var index = markerSlots.IndexOf(slot);
            if (index >= 0)
            {
                SetActiveIndex(index);
            }
        }

        private void HandleSlotButtonInserted(World3DButtonSlotBase slot, World3DPhysicalButton button)
        {
            if (markerButton != null && button != markerButton)
            {
                return;
            }

            SetActiveSlot(slot);
        }

        private void HandleMarkerReleased(WorldDraggable draggable)
        {
            if (!returnMarkerToActiveSlot || markerButton == null || ActiveSlot == null)
            {
                return;
            }

            if (markerButton.CurrentSlot == null)
            {
                ActiveSlot.TryInsert(markerButton);
            }
        }

        private void OnDisable()
        {
            for (var i = 0; i < markerSlots.Count; i++)
            {
                if (markerSlots[i] != null)
                {
                    markerSlots[i].ButtonInserted -= HandleSlotButtonInserted;
                }
            }

            if (markerButton != null)
            {
                markerButton.Released -= HandleMarkerReleased;
            }
        }
    }
}

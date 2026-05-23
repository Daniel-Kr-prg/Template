using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace DanieloZ.WorldInteraction
{
    public sealed class World3DToggleGroup : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Toggle Objects")]
        [SerializeField] private List<World3DToggleObject> toggleObjects = new();
        [FoldoutGroup("Toggle Objects")]
        [SerializeField] private int defaultActiveIndex;

        [FoldoutGroup("Physical Marker")]
        [FormerlySerializedAs("markerButton")]
        [SerializeField] private World3DSlotItem markerItem;
        [FoldoutGroup("Physical Marker")]
        [SerializeField] private List<World3DButtonSlotBase> markerSlots = new();
        [FoldoutGroup("Physical Marker")]
        [SerializeField] private bool returnMarkerToActiveSlot = true;

        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onActiveChanged;

        #endregion

        #region Public API

        public int ActiveIndex => activeIndex;
        public World3DToggleObject ActiveObject => activeIndex >= 0 && activeIndex < toggleObjects.Count ? toggleObjects[activeIndex] : null;
        public World3DButtonSlotBase ActiveSlot => activeIndex >= 0 && activeIndex < markerSlots.Count ? markerSlots[activeIndex] : null;
        public World3DSlotItem MarkerItem => markerItem;

        #endregion

        #region Runtime State

        private int activeIndex = -1;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            for (var i = 0; i < markerSlots.Count; i++)
            {
                if (markerSlots[i] != null)
                {
                    markerSlots[i].ItemInserted += HandleSlotItemInserted;
                }
            }

            if (markerItem != null)
            {
                markerItem.Released += HandleMarkerReleased;
            }

            SetActiveIndex(Mathf.Clamp(defaultActiveIndex, 0, Mathf.Max(0, Mathf.Max(toggleObjects.Count, markerSlots.Count) - 1)));
        }

        private void OnDisable()
        {
            for (var i = 0; i < markerSlots.Count; i++)
            {
                if (markerSlots[i] != null)
                {
                    markerSlots[i].ItemInserted -= HandleSlotItemInserted;
                }
            }

            if (markerItem != null)
            {
                markerItem.Released -= HandleMarkerReleased;
            }
        }

        #endregion

        #region Selection

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

            if (markerItem != null && ActiveSlot != null && !markerItem.IsHeld)
            {
                ActiveSlot.TryInsert(markerItem);
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

        #endregion

        #region Event Handlers

        private void HandleSlotItemInserted(World3DButtonSlotBase slot, World3DSlotItem item)
        {
            if (markerItem != null && item != markerItem)
            {
                return;
            }

            SetActiveSlot(slot);
        }

        private void HandleMarkerReleased(WorldDraggable draggable)
        {
            if (!returnMarkerToActiveSlot || markerItem == null || ActiveSlot == null)
            {
                return;
            }

            if (markerItem.CurrentSlot == null)
            {
                ActiveSlot.TryInsert(markerItem);
            }
        }

        #endregion
    }
}

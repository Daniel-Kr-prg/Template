using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteraction_Toggle_StaticGroup : MonoBehaviour
    {
        [SerializeField] private List<WorldInteraction_Toggle_StaticButton> toggles = new();
        [SerializeField] private int defaultActiveIndex;
        [SerializeField] private bool allowNoSelection;
        [SerializeField] private UnityEvent onActiveChanged;

        public int ActiveIndex { get; private set; } = -1;
        public WorldInteraction_Toggle_StaticButton ActiveToggle =>
            ActiveIndex >= 0 && ActiveIndex < toggles.Count ? toggles[ActiveIndex] : null;

        private void Awake()
        {
            RegisterToggleReferences();

            if (toggles.Count == 0)
            {
                return;
            }

            if (allowNoSelection && defaultActiveIndex < 0)
            {
                SetActiveIndex(-1);
                return;
            }

            SetActiveIndex(Mathf.Clamp(defaultActiveIndex, 0, toggles.Count - 1));
        }

        public void SetActiveIndex(int index)
        {
            if (index < -1 || index >= toggles.Count)
            {
                return;
            }

            if (index < 0 && !allowNoSelection)
            {
                return;
            }

            ActiveIndex = index;
            for (var i = 0; i < toggles.Count; i++)
            {
                if (toggles[i] != null)
                {
                    toggles[i].SetOnSilently(i == ActiveIndex);
                }
            }

            onActiveChanged?.Invoke();
        }

        public void SetActiveToggle(WorldInteraction_Toggle_StaticButton toggle)
        {
            var index = toggles.IndexOf(toggle);
            if (index >= 0)
            {
                SetActiveIndex(index);
            }
        }

        public void NotifyToggleChanged(WorldInteraction_Toggle_StaticButton toggle, bool active)
        {
            if (active)
            {
                SetActiveToggle(toggle);
                return;
            }

            if (allowNoSelection && ActiveToggle == toggle)
            {
                SetActiveIndex(-1);
            }
        }

        private void RegisterToggleReferences()
        {
            for (var i = 0; i < toggles.Count; i++)
            {
                if (toggles[i] != null)
                {
                    toggles[i].SetGroup(this);
                }
            }
        }
    }
}

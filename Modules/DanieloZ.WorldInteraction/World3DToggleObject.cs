using System;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public class World3DToggleObject : MonoBehaviour
    {
        [SerializeField] private string toggleId;
        [SerializeField] private bool isActive;
        [SerializeField] private UnityEvent onActivated;
        [SerializeField] private UnityEvent onDeactivated;

        public event Action<World3DToggleObject, bool> ActiveStateChanged;

        public string ToggleId => toggleId;
        public bool IsActive => isActive;

        public virtual void SetActiveState(bool active)
        {
            if (isActive == active)
            {
                return;
            }

            isActive = active;

            if (isActive)
            {
                onActivated?.Invoke();
            }
            else
            {
                onDeactivated?.Invoke();
            }

            ActiveStateChanged?.Invoke(this, isActive);
        }
    }
}

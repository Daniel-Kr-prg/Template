using System;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public abstract class World3DButtonBase : MonoBehaviour
    {
        [SerializeField] private string buttonId;
        [SerializeField] private bool interactable = true;
        [SerializeField] private bool isActive;
        [SerializeField] private UnityEvent onActivated;
        [SerializeField] private UnityEvent onDeactivated;

        public event Action<World3DButtonBase> Activated;
        public event Action<World3DButtonBase> Deactivated;

        public string ButtonId => buttonId;
        public bool Interactable
        {
            get => interactable;
            set => interactable = value;
        }

        public bool IsActive => isActive;

        public virtual void Setup(string id)
        {
            buttonId = id;
        }

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
                Activated?.Invoke(this);
            }
            else
            {
                onDeactivated?.Invoke();
                Deactivated?.Invoke(this);
            }
        }
    }
}

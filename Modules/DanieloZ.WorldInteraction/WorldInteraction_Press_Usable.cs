using System;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteraction_Press_Usable : MonoBehaviour, IWorldInteraction_Press_Usable
    {
        [SerializeField] private bool interactable = true;
        [SerializeField] private UnityEvent onUsed;

        public event Action<WorldInteraction_Press_Usable> Used;

        public bool Interactable
        {
            get => interactable;
            set => interactable = value;
        }

        public void Use(WorldInteractionContext context)
        {
            if (!interactable)
            {
                return;
            }

            onUsed?.Invoke();
            Used?.Invoke(this);
        }
    }
}

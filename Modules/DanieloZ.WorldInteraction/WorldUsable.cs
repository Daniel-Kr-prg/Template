using System;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldUsable : MonoBehaviour, IWorldUsable
    {
        [SerializeField] private bool interactable = true;
        [SerializeField] private UnityEvent onUsed;

        public event Action<WorldUsable> Used;

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

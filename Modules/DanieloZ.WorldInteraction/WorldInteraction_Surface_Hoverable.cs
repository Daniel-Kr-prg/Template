using System;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteraction_Surface_Hoverable : MonoBehaviour, IWorldInteraction_Surface_Hoverable
    {
        [SerializeField] private bool interactable = true;
        [SerializeField] private UnityEvent onHoverStarted;
        [SerializeField] private UnityEvent onHoverEnded;

        public event Action<WorldInteraction_Surface_Hoverable> HoverStarted;
        public event Action<WorldInteraction_Surface_Hoverable> HoverEnded;

        public bool Interactable
        {
            get => interactable;
            set => interactable = value;
        }

        public void HoverStart(WorldInteractionContext context)
        {
            if (!interactable)
            {
                return;
            }

            onHoverStarted?.Invoke();
            HoverStarted?.Invoke(this);
        }

        public void HoverEnd(WorldInteractionContext context)
        {
            if (!interactable)
            {
                return;
            }

            onHoverEnded?.Invoke();
            HoverEnded?.Invoke(this);
        }
    }
}

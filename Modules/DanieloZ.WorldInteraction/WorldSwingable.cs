using System;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldSwingable : MonoBehaviour, IWorldSwingable
    {
        [SerializeField] private UnityEvent onSwing;

        public event Action<WorldSwingContext> Swinged;

        public void Swing(WorldSwingContext context)
        {
            onSwing?.Invoke();
            Swinged?.Invoke(context);
        }
    }
}

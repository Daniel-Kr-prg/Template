using System;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldInteraction_Swing_Target : MonoBehaviour, IWorldInteraction_Swing_Target
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

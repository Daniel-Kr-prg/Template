using DanieloZ.WorldInteraction;
using UnityEngine;
using UnityEngine.Events;

namespace PixelLust.PixelVoxelPuzzle
{
    [RequireComponent(typeof(World3DToggleButton))]
    public sealed class HandUsableWorld3DToggleButton : MonoBehaviour, IHandUsable
    {
        [SerializeField] private World3DToggleButton toggleButton;
        [SerializeField] private string debugMessage = "World 3D toggle button pressed.";
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private UnityEvent onUsed;

        private void Awake()
        {
            toggleButton ??= GetComponent<World3DToggleButton>();
        }

        public bool CanUse(HandPointerContext context)
        {
            return enabled
                && gameObject.activeInHierarchy
                && toggleButton != null
                && toggleButton.Interactable;
        }

        public void Use(HandPointerContext context)
        {
            if (!CanUse(context))
            {
                return;
            }

            toggleButton.Press();
            if (logToConsole && !string.IsNullOrWhiteSpace(debugMessage))
            {
                Debug.Log(debugMessage, this);
            }

            onUsed?.Invoke();
        }
    }
}

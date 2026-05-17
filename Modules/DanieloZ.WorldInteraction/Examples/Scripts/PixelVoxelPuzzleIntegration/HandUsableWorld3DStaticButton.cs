using DanieloZ.WorldInteraction;
using UnityEngine;
using UnityEngine.Events;

namespace PixelLust.PixelVoxelPuzzle
{
    [RequireComponent(typeof(World3DStaticButton))]
    public sealed class HandUsableWorld3DStaticButton : MonoBehaviour, IHandUsable
    {
        [SerializeField] private World3DStaticButton button;
        [SerializeField] private string debugMessage = "World 3D static button pressed.";
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private UnityEvent onUsed;

        private void Awake()
        {
            button ??= GetComponent<World3DStaticButton>();
        }

        public bool CanUse(HandPointerContext context)
        {
            return enabled
                && gameObject.activeInHierarchy
                && button != null
                && button.Interactable;
        }

        public void Use(HandPointerContext context)
        {
            if (!CanUse(context))
            {
                return;
            }

            button.Press();
            if (logToConsole && !string.IsNullOrWhiteSpace(debugMessage))
            {
                Debug.Log(debugMessage, this);
            }

            onUsed?.Invoke();
        }
    }
}

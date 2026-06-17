using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(WorldInteraction_Toggle_Button))]
    public sealed class WorldInteraction_Hand_ToggleBridge : MonoBehaviour, IWorldInteraction_Press_LifecycleTarget
    {
        [SerializeField] private WorldInteraction_Toggle_Button toggleButton;
        [SerializeField] private string debugMessage = "World 3D toggle button pressed.";
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private UnityEvent onUsed;

        private void Awake()
        {
            toggleButton ??= GetComponent<WorldInteraction_Toggle_Button>();
        }

        public bool CanPress(WorldInteraction_Pointer_Context context)
        {
            return enabled
                && gameObject.activeInHierarchy
                && toggleButton != null
                && toggleButton.Interactable;
        }

        public void Press(WorldInteraction_Pointer_Context context)
        {
            if (!CanPress(context))
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

        public void BeginPress(WorldInteraction_Pointer_Context context)
        {
            if (CanPress(context))
            {
                toggleButton.BeginPress();
            }
        }

        public void EndPress(WorldInteraction_Pointer_Context context, bool activate)
        {
            if (toggleButton == null)
            {
                return;
            }

            var shouldActivate = activate && CanPress(context);
            toggleButton.EndPress(shouldActivate);
            if (!shouldActivate)
            {
                return;
            }

            if (logToConsole && !string.IsNullOrWhiteSpace(debugMessage))
            {
                Debug.Log(debugMessage, this);
            }

            onUsed?.Invoke();
        }
    }
}

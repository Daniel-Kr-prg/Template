using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(WorldInteraction_Press_StaticButton))]
    public sealed class WorldInteraction_Hand_StaticButtonBridge : MonoBehaviour, IWorldInteraction_Press_LifecycleTarget
    {
        [SerializeField] private WorldInteraction_Press_StaticButton button;
        [SerializeField] private string debugMessage = "World 3D static button pressed.";
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private UnityEvent onUsed;

        private void Awake()
        {
            button ??= GetComponent<WorldInteraction_Press_StaticButton>();
        }

        public bool CanPress(WorldInteraction_Pointer_Context context)
        {
            return enabled
                && gameObject.activeInHierarchy
                && button != null
                && button.Interactable;
        }

        public void Press(WorldInteraction_Pointer_Context context)
        {
            if (!CanPress(context))
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

        public void BeginPress(WorldInteraction_Pointer_Context context)
        {
            if (CanPress(context))
            {
                button.BeginPress();
            }
        }

        public void EndPress(WorldInteraction_Pointer_Context context, bool activate)
        {
            if (button == null)
            {
                return;
            }

            var shouldActivate = activate && CanPress(context);
            button.EndPress(shouldActivate);
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

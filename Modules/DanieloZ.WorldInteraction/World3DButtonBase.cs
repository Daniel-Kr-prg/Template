using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public abstract class World3DButtonBase : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Button")]
        [SerializeField] private string buttonId;
        [FoldoutGroup("Button")]
        [SerializeField] private bool interactable = true;
        [FoldoutGroup("Button")]
        [SerializeField] private bool isActive;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onActivated;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onDeactivated;

        #endregion

        #region Public API

        public event Action<World3DButtonBase> Activated;
        public event Action<World3DButtonBase> Deactivated;

        public string ButtonId => buttonId;
        public bool Interactable
        {
            get => interactable;
            set => interactable = value;
        }

        public bool IsActive => isActive;

        #endregion

        #region Configuration

        public virtual void Setup(string id)
        {
            buttonId = id;
        }

        #endregion

        #region State

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

        #endregion
    }
}

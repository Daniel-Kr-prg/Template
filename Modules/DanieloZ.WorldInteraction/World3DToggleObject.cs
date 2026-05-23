using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public class World3DToggleObject : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Toggle")]
        [SerializeField] private string toggleId;
        [FoldoutGroup("Toggle")]
        [SerializeField] private bool isActive;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onActivated;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onDeactivated;

        #endregion

        #region Public API

        public event Action<World3DToggleObject, bool> ActiveStateChanged;

        public string ToggleId => toggleId;
        public bool IsActive => isActive;

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
            }
            else
            {
                onDeactivated?.Invoke();
            }

            ActiveStateChanged?.Invoke(this, isActive);
        }

        #endregion
    }
}

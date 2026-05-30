using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public sealed class World3DStaticButton : World3DButtonBase, IWorldUsable
    {
        #region Inspector

        [FoldoutGroup("Press")]
        [SerializeField] private Transform pressTransform;
        [FoldoutGroup("Press")]
        [SerializeField] private Vector3 localPressOffset = new(0f, -0.08f, 0f);
        [FoldoutGroup("Press")]
        [SerializeField, Min(0f)] private float pressInDuration = 0.08f;
        [FoldoutGroup("Press")]
        [SerializeField, Min(0f)] private float releaseDuration = 0.12f;
        [FoldoutGroup("Press")]
        [SerializeField] private Ease pressEase = Ease.OutCubic;
        [FoldoutGroup("Press")]
        [SerializeField] private bool useLegacyOnMouseDown;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onPressed;

        #endregion

        #region Runtime State

        private readonly World3DPressAnimation pressAnimation = new();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (pressTransform == null)
            {
                pressTransform = transform;
            }

            pressAnimation.Initialize(pressTransform);
        }

        private void OnMouseDown()
        {
            if (useLegacyOnMouseDown)
            {
                Press();
            }
        }

        private void OnDisable()
        {
            pressAnimation.Kill();
        }

        #endregion

        #region Public API

        public event Action<World3DStaticButton> Pressed;

        public void Use(WorldInteractionContext context)
        {
            Press();
        }

        public void Press()
        {
            if (!Interactable)
            {
                return;
            }

            pressAnimation.Play(
                localPressOffset,
                pressInDuration,
                releaseDuration,
                pressEase,
                () =>
                {
                    onPressed?.Invoke();
                    Pressed?.Invoke(this);
                });
        }

        #endregion
    }
}

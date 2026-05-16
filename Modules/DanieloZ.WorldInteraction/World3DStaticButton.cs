using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    [RequireComponent(typeof(Collider))]
    public sealed class World3DStaticButton : World3DButtonBase, IWorldUsable
    {
        [Header("Press")]
        [SerializeField] private Transform pressTransform;
        [SerializeField] private Vector3 localPressOffset = new(0f, -0.08f, 0f);
        [SerializeField, Min(0f)] private float pressInDuration = 0.08f;
        [SerializeField, Min(0f)] private float releaseDuration = 0.12f;
        [SerializeField] private Ease pressEase = Ease.OutCubic;
        [SerializeField] private bool useLegacyOnMouseDown;
        [SerializeField] private UnityEvent onPressed;

        private Vector3 initialLocalPosition;
        private Tween pressTween;

        private void Awake()
        {
            if (pressTransform == null)
            {
                pressTransform = transform;
            }

            initialLocalPosition = pressTransform.localPosition;
        }

        private void OnMouseDown()
        {
            if (useLegacyOnMouseDown)
            {
                Press();
            }
        }

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

            pressTween?.Kill();
            pressTween = DOTween.Sequence()
                .Append(pressTransform.DOLocalMove(initialLocalPosition + localPressOffset, pressInDuration).SetEase(pressEase))
                .AppendCallback(() => onPressed?.Invoke())
                .Append(pressTransform.DOLocalMove(initialLocalPosition, releaseDuration).SetEase(pressEase));
        }

        private void OnDisable()
        {
            pressTween?.Kill();
        }
    }
}

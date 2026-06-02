using System;
using DG.Tweening;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    internal sealed class World3DPressAnimation
    {
        private Transform pressTransform;
        private Vector3 initialLocalPosition;
        private Tween tween;

        public void Initialize(Transform target)
        {
            pressTransform = target;
            initialLocalPosition = pressTransform != null ? pressTransform.localPosition : Vector3.zero;
        }

        public void Play(Vector3 localPressOffset, float pressInDuration, float releaseDuration, Ease ease, Action onPressed)
        {
            if (pressTransform == null)
            {
                onPressed?.Invoke();
                return;
            }

            tween?.Kill();
            tween = DOTween.Sequence()
                .Append(pressTransform.DOLocalMove(initialLocalPosition + localPressOffset, pressInDuration).SetEase(ease))
                .AppendCallback(() => onPressed?.Invoke())
                .Append(pressTransform.DOLocalMove(initialLocalPosition, releaseDuration).SetEase(ease));
        }

        public void PressIn(Vector3 localPressOffset, float pressInDuration, Ease ease)
        {
            if (pressTransform == null)
            {
                return;
            }

            tween?.Kill();
            tween = pressTransform.DOLocalMove(initialLocalPosition + localPressOffset, pressInDuration).SetEase(ease);
        }

        public void Release(float releaseDuration, Ease ease)
        {
            if (pressTransform == null)
            {
                return;
            }

            tween?.Kill();
            tween = pressTransform.DOLocalMove(initialLocalPosition, releaseDuration).SetEase(ease);
        }

        public void Kill()
        {
            tween?.Kill();
            tween = null;
        }
    }
}

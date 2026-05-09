#if LEAN_TOUCH
using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;
using DanieloZ.InputManagement;

public class LeanTouchInputBridge : MonoBehaviour
{
    private void OnEnable()
    {
        LeanTouch.OnFingerDown += HandleFingerDown;
        LeanTouch.OnFingerUp += HandleFingerUp;
        LeanTouch.OnFingerTap += HandleFingerTap;
        LeanTouch.OnFingerSwipe += HandleFingerSwipe;
        LeanTouch.OnGesture += HandleGesture;
    }

    private void OnDisable()
    {
        LeanTouch.OnFingerDown -= HandleFingerDown;
        LeanTouch.OnFingerUp -= HandleFingerUp;
        LeanTouch.OnFingerTap -= HandleFingerTap;
        LeanTouch.OnFingerSwipe -= HandleFingerSwipe;
        LeanTouch.OnGesture -= HandleGesture;
    }

    private void HandleFingerDown(LeanFinger finger)
    {
        InputManager.Instance?.HandleTouchDown(finger);
    }

    private void HandleFingerUp(LeanFinger finger)
    {
        InputManager.Instance?.HandleTouchUp(finger);
    }

    private void HandleFingerTap(LeanFinger finger)
    {
        InputManager.Instance?.HandleTap(finger);
    }

    private void HandleFingerSwipe(LeanFinger finger)
    {
        InputManager.Instance?.HandleSwipe(finger);
    }

    private void HandleGesture(List<LeanFinger> fingers)
    {
        InputManager.Instance?.HandleGesture(fingers);
    }
}
#endif


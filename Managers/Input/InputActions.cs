using System;
using System.Collections.Generic;
using UnityEngine;


public class InputAction : IComparable<InputAction>
{
    InputActionKey _key;
    string _actionName;

    public InputActionKey key => _key;
    public string actionName => _actionName;

    Func<bool> canHandle;
    Func<bool> hasError;


    Action keyAction;

    Action systemUnregisterAction;
    Action userUnregisterAction;

    bool oneTimeAction = false;

    public int priority { get; private set; }

    public InputAction(InputActionKey key, string actionName, Action keyAction, Action systemUnregisterAction, int priority = 0, Func<bool> hasError = null, Func<bool> canHandle = null, Action userUnregisterAction = null, bool oneTimeAction = false)
    {
        _key = key;
        _actionName = actionName;
        this.keyAction = keyAction;
        this.canHandle = canHandle;
        this.hasError = hasError;

        this.priority = priority;

        this.systemUnregisterAction = systemUnregisterAction;
        this.userUnregisterAction = userUnregisterAction;

        this.oneTimeAction = oneTimeAction;
    }

    public bool HasError()
    {
        return hasError?.Invoke() ?? false;
    }

    public void Handle()
    {
        if (canHandle?.Invoke() ?? true)
        {
            keyAction.Invoke();

            if (oneTimeAction) 
                Unregister();
        }
    }

    public void Unregister(bool handleSysterUnregisterAction = true)
    {
        if (handleSysterUnregisterAction)
            systemUnregisterAction?.Invoke();
        userUnregisterAction?.Invoke();
    }

    public int CompareTo(InputAction other)
    {
        return other.priority.CompareTo(priority);
    }
}

public enum InputActionKey
{
    // System
    EXIT,
    CONFIRM,

    // Movement
    MOVE_FORWARD,
    MOVE_BACKWARD,
    MOVE_LEFT,
    MOVE_RIGHT,
    CAMERA_ROTATE_LEFT,
    CAMERA_ROTATE_RIGHT,

    JUMP,
    CROUNCH,
    RUN,
    HANDS,
    PETRIFY,
    UNSTUCK,

    // Puzzle interaction
    ROTATE_PIECE,
    TARGET_BOARD_LOCK,

    // Social
    TEXT_CHAT,
    VOICE_CHAT,

    // Pointer
    MOUSE_LEFT,
    MOUSE_RIGHT,
    MOUSE_MIDDLE
}

using System;
using UnityEngine;

/// <summary>
/// Universal handler for Collider interactions, delegates collision and trigger events.
/// </summary>
public class Game_Handler_ColliderInteraction : MonoBehaviour
{
    private Collider2D handlingCollider;

    public Action<Collision2D> onCollisionEnter;
    public Action<Collision2D> onCollisionExit;
    public Action<Collider2D> onTriggerEnter;
    public Action<Collider2D> onTriggerExit;

    private void Awake()
    {
        handlingCollider ??= GetComponent<Collider2D>();
    }

    public void Setup(Action<Collision2D> onEnter, Action<Collision2D> onExit, Action<Collider2D> onTriggerEnter, Action<Collider2D> onTriggerExit)
    {
        this.onCollisionEnter = onEnter;
        this.onCollisionExit = onExit;
        this.onTriggerEnter = onTriggerEnter;
        this.onTriggerExit = onTriggerExit;
    }

    public void SetActive(bool active)
    {
        handlingCollider.enabled = active;
    }

    public void SetColliderMode()
    {
        handlingCollider.isTrigger = false;
    }

    public void SetTriggerMode()
    {
        handlingCollider.isTrigger = true;
    }

    private void OnCollisionEnter2D(Collision2D collision) => onCollisionEnter?.Invoke(collision);
    private void OnCollisionExit2D(Collision2D collision) => onCollisionExit?.Invoke(collision);
    private void OnTriggerEnter2D(Collider2D other) => onTriggerEnter?.Invoke(other);
    private void OnTriggerExit2D(Collider2D other) => onTriggerExit?.Invoke(other);
}

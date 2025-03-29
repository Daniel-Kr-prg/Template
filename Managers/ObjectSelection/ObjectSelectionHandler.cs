using UnityEngine;
using UnityEngine.Events;

public class ObjectSelectionHandler : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private UnityEvent onHover;
    [SerializeField] private UnityEvent onExit;
    [SerializeField] private UnityEvent onSelected;
    // Base handling

    private void OnMouseEnter()
    {
        OnHoverEnter();
    }

    private void OnMouseExit()
    {
        OnHoverExit();
    }

    private void OnMouseOver()
    {
        
    }

    private void OnDisable()
    {
        ObjectSelectionManager.ExitObject(this);
    }

    private void OnDestroy()
    {
        ObjectSelectionManager.ExitObject(this);
    }

    // something else

    public virtual void OnHoverEnter()
    {
        Debug.Log($"{name} OnHoverEnter");
        ObjectSelectionManager.HoverObject(this);
        onHover?.Invoke();
    }

    public virtual void OnHoverExit()
    {
        Debug.Log($"{name} OnHoverExit");
        ObjectSelectionManager.ExitObject(this);
        onExit?.Invoke();
    }

    public virtual void OnSelected()
    {
        Debug.Log($"{name} OnSelected");
        onSelected?.Invoke();
    }

    public virtual void OnDeselected()
    {
        Debug.Log($"{name} OnDeselected");
    }
}

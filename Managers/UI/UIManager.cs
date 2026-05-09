using UnityEngine;

public class UIManager : SingletonManager<UIManager>
{
    [SerializeField] private UI_Management_PageSwitcher pageSwitcher;

    public UI_Management_PageSwitcher PageSwitcher => pageSwitcher;

    protected override void Awake()
    {
        base.Awake();

        pageSwitcher ??= GetComponentInChildren<UI_Management_PageSwitcher>(true);
    }
}

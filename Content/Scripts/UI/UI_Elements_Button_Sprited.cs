using DanieloZ.InputManagement;
using DanieloZ.Managers.Sound;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Elements_Button_Sprited : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private GameObject releasedObject;
    [SerializeField] private GameObject pressedObject;

    [SerializeField] private SoundName pressSound;
    [SerializeField] private SoundName releasedSound;

    private bool pressed = false;

    private void Awake()
    {
        InputManager.Instance.RegisterKeyUpAction(InputActionKey.CONFIRM, $"{gameObject.name}_UP", () =>
        {
            Release();
        }, InputPriority.Base);
        InputManager.Instance.RegisterKeyDownAction(InputActionKey.CONFIRM, $"{gameObject.name}_DOWN", () =>
        {
            if (EventSystem.current.currentSelectedGameObject == gameObject)
                Press();
        }, InputPriority.Base);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Press();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnButtonClick()
    {
        Press();
        Release();
    }

    private void Press()
    {
        if (pressed) return;

        if (releasedObject != null) releasedObject.SetActive(false);
        if (pressedObject != null) pressedObject.SetActive(true);

        SoundManager.Instance.PlayGlobalEffect(SoundCategory.Effects_UI_1, pressSound, AudioMixerGroupName.Effects_UI_1);

        pressed = true;
    }

    private void Release()
    {
        if (!pressed) return;

        if (releasedObject != null) releasedObject.SetActive(true);
        if (pressedObject != null) pressedObject.SetActive(false);

        SoundManager.Instance.PlayGlobalEffect(SoundCategory.Effects_UI_1, releasedSound, AudioMixerGroupName.Effects_UI_2);

        pressed = false;
    }
}

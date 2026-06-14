using System.Collections;
using DanieloZ.Managers;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UI_Elements_Page))]
[RequireComponent(typeof(CanvasGroup))]
public sealed class UI_Elements_TimedScreenPage : MonoBehaviour
{
    private const string PageCallbackId = "TimedScreenPage";

    [Header("Startup")]
    [SerializeField] private bool showOnStart;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fadeInTime = 0.2f;
    [SerializeField, Min(0f)] private float stayShownTime = 3f;
    [SerializeField, Min(0f)] private float fadeOutTime = 0.2f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Event Hold")]
    [SerializeField] private bool keepUntilEventCalled;
    [SerializeField] private EventName eventKey = EventName.UI_StartupScreenRelease;

    [Header("Presentation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string showAnimatorTrigger = "Show";
    [SerializeField] private string hideAnimatorTrigger = "Hide";
    [SerializeField] private AudioSource showSound;
    [SerializeField] private AudioSource hideSound;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private string defaultStatusText;
    [SerializeField] private bool blockRaycasts = true;

    private UI_Elements_Page page;
    private CanvasGroup canvasGroup;
    private Coroutine sequence;
    private bool internalPageStateChange;
    private bool releaseRequested;
    private bool subscribedToEvent;

    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0f;

    private void Awake()
    {
        ResolveReferences();
        page.RegisterOnPageShow(PageCallbackId, HandlePageShow);
        page.RegisterOnPageHide(PageCallbackId, HandlePageHide);

        if (!string.IsNullOrWhiteSpace(defaultStatusText) && statusLabel != null)
        {
            statusLabel.text = defaultStatusText;
        }

        ApplyHiddenInstant();
    }

    private void Start()
    {
        if (showOnStart)
        {
            Show();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromReleaseEvent();
    }

    private void OnDestroy()
    {
        UnsubscribeFromReleaseEvent();

        if (page == null)
        {
            return;
        }

        page.UnregisterOnPageShow(PageCallbackId);
        page.UnregisterOnPageHide(PageCallbackId);
    }

    public void Show()
    {
        ResolveReferences();
        page.Show();
    }

    public void Hide()
    {
        ResolveReferences();
        page.Hide();
    }

    public void Release()
    {
        releaseRequested = true;
    }

    public void SetStatus(string status)
    {
        if (statusLabel != null)
        {
            statusLabel.text = status ?? string.Empty;
        }
    }

    private void HandlePageShow(bool instantly)
    {
        if (internalPageStateChange)
        {
            return;
        }

        StartShowSequence(instantly);
    }

    private void HandlePageHide(bool instantly)
    {
        if (internalPageStateChange)
        {
            return;
        }

        StartHideSequence(instantly);
    }

    private void StartShowSequence(bool instantly)
    {
        StopCurrentSequence();
        sequence = StartCoroutine(ShowSequence(instantly));
    }

    private void StartHideSequence(bool instantly)
    {
        StopCurrentSequence();
        sequence = StartCoroutine(HideSequence(instantly));
    }

    private IEnumerator ShowSequence(bool instantly)
    {
        releaseRequested = false;
        SetInputBlock(true);
        TrySubscribeToReleaseEvent();
        PlayAnimatorTrigger(showAnimatorTrigger);
        PlaySound(showSound);

        if (instantly)
        {
            SetAlpha(1f);
        }
        else
        {
            yield return FadeTo(1f, fadeInTime);
        }

        yield return WaitForSeconds(stayShownTime);

        if (keepUntilEventCalled)
        {
            while (!releaseRequested)
            {
                TrySubscribeToReleaseEvent();
                yield return null;
            }
        }

        PlayAnimatorTrigger(hideAnimatorTrigger);
        PlaySound(hideSound);
        yield return FadeTo(0f, instantly ? 0f : fadeOutTime);
        FinishHiddenState();
    }

    private IEnumerator HideSequence(bool instantly)
    {
        releaseRequested = true;
        PlayAnimatorTrigger(hideAnimatorTrigger);
        PlaySound(hideSound);

        if (instantly)
        {
            SetAlpha(0f);
        }
        else
        {
            yield return FadeTo(0f, fadeOutTime);
        }

        FinishHiddenState(false);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        var startAlpha = canvasGroup.alpha;
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private IEnumerator WaitForSeconds(float seconds)
    {
        var duration = Mathf.Max(0f, seconds);
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void FinishHiddenState(bool updatePageState = true)
    {
        UnsubscribeFromReleaseEvent();
        ApplyHiddenInstant();
        sequence = null;

        if (!updatePageState || page.Hidden)
        {
            return;
        }

        internalPageStateChange = true;
        page.Hide(true);
        internalPageStateChange = false;
    }

    private void ApplyHiddenInstant()
    {
        SetAlpha(0f);
        SetInputBlock(false);
    }

    private void SetAlpha(float alpha)
    {
        ResolveReferences();
        canvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    private void SetInputBlock(bool visible)
    {
        ResolveReferences();
        canvasGroup.interactable = visible && blockRaycasts;
        canvasGroup.blocksRaycasts = visible && blockRaycasts;
    }

    private void TrySubscribeToReleaseEvent()
    {
        if (subscribedToEvent || !keepUntilEventCalled || !EventManager.HaveInstance())
        {
            return;
        }

        EventManager.AddCallback(eventKey, OnReleaseEvent);
        subscribedToEvent = true;
    }

    private void UnsubscribeFromReleaseEvent()
    {
        if (!subscribedToEvent || !EventManager.HaveInstance())
        {
            subscribedToEvent = false;
            return;
        }

        EventManager.RemoveCallback(eventKey, OnReleaseEvent);
        subscribedToEvent = false;
    }

    private void OnReleaseEvent(object[] payload)
    {
        Release();
    }

    private void PlayAnimatorTrigger(string triggerName)
    {
        if (animator == null
            || animator.runtimeAnimatorController == null
            || string.IsNullOrWhiteSpace(triggerName))
        {
            return;
        }

        animator.SetTrigger(triggerName);
    }

    private static void PlaySound(AudioSource source)
    {
        if (source != null)
        {
            source.Play();
        }
    }

    private void StopCurrentSequence()
    {
        if (sequence == null)
        {
            return;
        }

        StopCoroutine(sequence);
        sequence = null;
    }

    private void ResolveReferences()
    {
        page ??= GetComponent<UI_Elements_Page>();
        canvasGroup ??= GetComponent<CanvasGroup>();
        animator ??= GetComponent<Animator>();
    }

    private void OnValidate()
    {
        fadeInTime = Mathf.Max(0f, fadeInTime);
        stayShownTime = Mathf.Max(0f, stayShownTime);
        fadeOutTime = Mathf.Max(0f, fadeOutTime);
    }
}

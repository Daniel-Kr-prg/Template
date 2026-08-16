using DanieloZ.Managers.Sound;
using DG.Tweening;
using System;
using System.Collections;
using System.Drawing;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager_LocalAudioSource : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private Transform transformToFollow;
    private bool stopOnTransformDisable;
    private Action onFinish;

    private bool initialized;
    private float defaultVolume;
    private AudioMixerGroup defaultMixerGroup;
    private float delayTime = 0f;

    // SFX
    private float fadeInTime;
    private float fadeOutTime;

    public void Init()
    {
        if (initialized) return;

        audioSource ??= GetComponent<AudioSource>();
        defaultVolume = audioSource.volume;
        defaultMixerGroup = audioSource.outputAudioMixerGroup;
        initialized = true;
    }

    void PlayClip(AudioClip clip, AudioSourceSettings settings = null, SFXSettings SFX = null, Action onFinish = null)
    {
        this.onFinish = onFinish;

        ApplySettings(settings);
        if (delayTime > 0f)
        {
            PlayClipWithDelay(clip, delayTime, SFX, onFinish);
        }
        else
        {
            audioSource.clip = clip;
            audioSource.Play();
            ApplySFX(SFX);

            StartCoroutine(HandleClipCompletion());
        }
    }
    public void PlayClipWithDelay(AudioClip clip, float delay, SFXSettings SFX = null, Action onFinish = null)
    {
        StartCoroutine(DelayedPlayCoroutine(clip, delay, SFX, onFinish));
    }

    private IEnumerator DelayedPlayCoroutine(AudioClip clip, float delay, SFXSettings SFX, Action onFinish)
    {
        yield return new WaitForSeconds(delay);
        audioSource.clip = clip;
        audioSource.Play();
        ApplySFX(SFX);

        StartCoroutine(HandleClipCompletion());
    }

    public void PlayClip_AtPoint(AudioClip clip, Vector3 point, AudioSourceSettings settings = null, SFXSettings SFX = null, Action onFinish = null)
    {
        if (clip == null)
        {
            Debug.LogWarning("[M] SoundManager / LocalAudioSource: AudioClip is null!");
            return;
        }

        transformToFollow = null; // Reset follow behavior
        transform.position = point; // Set the position to the specified point

        PlayClip(clip, settings, SFX, onFinish);
    }

    /// <summary>
    /// Plays an audio clip, making it follow a specified transform.
    /// </summary>
    public void PlayClip_FollowTransform(AudioClip clip, Transform transformToFollow, bool stopOnTransformDisable = false, AudioSourceSettings settings = null, SFXSettings SFX = null, Action onFinish = null)
    {
        if (clip == null)
        {
            Debug.LogWarning("[M] SoundManager / LocalAudioSource: AudioClip is null!");
            return;
        }

        this.transformToFollow = transformToFollow;
        this.stopOnTransformDisable = stopOnTransformDisable;

        PlayClip(clip, settings, SFX, onFinish);
    }

    private void ApplySettings(AudioSourceSettings settings)
    {
        settings ??= new AudioSourceSettings();
        audioSource.loop = settings.loop;
        audioSource.volume = settings.volume;
        audioSource.pitch = settings.pitch;
        audioSource.priority = Mathf.Clamp(settings.priority, 0, 256);
        audioSource.spatialBlend = Mathf.Clamp01(settings.spatialBlend);
        audioSource.outputAudioMixerGroup = settings.group != null ? settings.group : defaultMixerGroup;
        defaultVolume = settings.volume;
        delayTime = settings.delayTime;
    }

    private void ApplySFX(SFXSettings settings)
    {
        if (settings == null)
        {
            fadeInTime = 0;
            fadeOutTime = 0;
        }
        else
        {
            fadeInTime = settings.fadeInTime > audioSource.clip.length ? audioSource.clip.length : settings.fadeInTime;
            fadeOutTime = settings.fadeOutTime > audioSource.clip.length ? audioSource.clip.length : settings.fadeOutTime;
        }

        SetFadeIn(fadeInTime);
        SetFadeOut(fadeOutTime);
    }

    private void Update()
    {
        // Follow the transform if assigned
        if (transformToFollow != null)
        {
            if (transformToFollow.gameObject.activeInHierarchy)
            {
                transform.position = transformToFollow.position;
            }
            else if (stopOnTransformDisable)
            {
                StopPlayback();
            }
        }
    }

    /// <summary>
    /// Stops playback and clears the state.
    /// </summary>
    public void StopPlayback(bool instant = false)
    {
        if (fadeOutTime > 0 && !instant)
        {
            SetFadeOutNow(fadeOutTime, () =>
            {
                audioSource.Stop();
                CleanupAfterStop();
            });
        }
        else
        {
            audioSource.Stop();
            CleanupAfterStop();
        }
    }

    public void PausePlayback()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    public void ResumePlayback()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }

    private void CleanupAfterStop()
    {
        transformToFollow = null;
        stopOnTransformDisable = false;
        onFinish?.Invoke();
        onFinish = null;
    }

    /// <summary>
    /// Handles clip completion and invokes the callback.
    /// </summary>
    private IEnumerator HandleClipCompletion()
    {
        while (audioSource != null && audioSource.isPlaying)
        {
            yield return null;
        }

        transformToFollow = null;
        onFinish?.Invoke();
        onFinish = null;
    }

    private void SetFadeIn(float duration, Action onComplete = null)
    {
        if (duration != 0)
        {
            audioSource.volume = 0f;
            audioSource.DOFade(defaultVolume, duration).OnComplete(() => onComplete?.Invoke());
        }
    }

    private void SetFadeOut(float duration, Action onComplete = null)
    {
        if (duration != 0)
            StartCoroutine(FadeOutCoroutine(fadeOutTime, onComplete));
    }

    private void SetFadeOutNow(float duration, Action onComplete = null)
    {
        audioSource.DOFade(0f, duration).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    private IEnumerator FadeOutCoroutine(float duration, Action onComplete = null)
    {
        float timeToStart = audioSource.clip.length - duration;
        yield return new WaitForSeconds(timeToStart);

        audioSource.DOFade(0f, duration).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }
}

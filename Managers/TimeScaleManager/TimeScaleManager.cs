using UnityEngine;

/// <summary>
/// Singleton manager for controlling global time scale via Inspector slider.
/// </summary>
public class TimeScaleManager : MonoBehaviour
{
    public static TimeScaleManager Instance { get; private set; }

    [Header("Time Scale Settings")]
    [Range(0f, 3f)]
    public float timeScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyTimeScale();
    }

    private void OnValidate()
    {
        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = timeScale;
    }

    /// <summary>
    /// Set time scale globally.
    /// </summary>
    public static void SetTimeScale(float value)
    {
        if (Instance != null)
        {
            Instance.timeScale = value;
            Instance.ApplyTimeScale();
        }
        else
        {
            Time.timeScale = value;
        }
    }

    /// <summary>
    /// Get current time scale.
    /// </summary>
    public static float GetTimeScale()
    {
        return Instance != null ? Instance.timeScale : Time.timeScale;
    }
} 
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;

public class TimeManager : SingletonManager<TimeManager>
{
    private Dictionary<string, ScaledAsyncTimer> scaledAsyncTimers = new();
    private Dictionary<string, ScaledAsyncStopwatch> scaledAsyncStopwatches = new();

    private Dictionary<string, UnscaledAsyncTimer> unscaledAsyncTimers = new();
    private Dictionary<string, UnscaledAsyncStopwatch> unscaledAsyncStopwatches = new();

    private List<ScaledTimer> scaledTimers = new();
    private List<ScaledStopwatch> scaledStopwatches = new();

    private List<UnscaledTimer> unscaledTimers = new();
    private List<UnscaledStopwatch> unscaledStopwatches = new();

    private void Start()
    {
        // Additional handling before stage changing

        // Satisfy stage condition
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_TimeManagerReady");
    }

    void Update()
    {
        UpdateTimers(scaledTimers, Time.deltaTime);
        UpdateTimers(unscaledTimers, Time.unscaledDeltaTime);
        UpdateStopwatches(scaledStopwatches, Time.deltaTime);
        UpdateStopwatches(unscaledStopwatches, Time.unscaledDeltaTime);
    }

    #region Async
    public void StartScaledAsyncTimer(string id, float duration, float multiplier = 1f, Action onEnd = null, float updateInterval = 0f, Action<float> onUpdate = null)
    {
        if (scaledAsyncTimers.ContainsKey(id)) return;
        var timer = new ScaledAsyncTimer(duration, multiplier, onEnd, updateInterval, onUpdate);
        timer.coroutine = StartCoroutine(timer.TimerCoroutine());
        scaledAsyncTimers[id] = timer;
    }

    public void StartUnscaledAsyncTimer(string id, float duration, float multiplier = 1f, Action onEnd = null, float updateInterval = 0f, Action<float> onUpdate = null)
    {
        if (unscaledAsyncTimers.ContainsKey(id)) return;
        var timer = new UnscaledAsyncTimer(duration, multiplier, onEnd, updateInterval, onUpdate);
        timer.coroutine = StartCoroutine(timer.TimerCoroutine());
        unscaledAsyncTimers[id] = timer;
    }

    public void StartScaledAsyncStopwatch(string id, float multiplier = 1f, float updateInterval = 1f, Action<float> onUpdate = null)
    {
        if (scaledAsyncStopwatches.ContainsKey(id)) return;
        var stopwatch = new ScaledAsyncStopwatch(multiplier, updateInterval, onUpdate);
        stopwatch.coroutine = StartCoroutine(stopwatch.StopwatchCoroutine());
        scaledAsyncStopwatches[id] = stopwatch;
    }

    public void StartUnscaledAsyncStopwatch(string id, float multiplier = 1f, float updateInterval = 1f, Action<float> onUpdate = null)
    {
        if (unscaledAsyncStopwatches.ContainsKey(id)) return;
        var stopwatch = new UnscaledAsyncStopwatch(multiplier, updateInterval, onUpdate);
        stopwatch.coroutine = StartCoroutine(stopwatch.StopwatchCoroutine());
        unscaledAsyncStopwatches[id] = stopwatch;
    }

    public void StopScaledAsyncTimer(string id)
    {
        if (scaledAsyncTimers.TryGetValue(id, out var timer))
        {
            StopCoroutine(timer.coroutine);
            scaledAsyncTimers.Remove(id);
        }
    }

    public void StopUnscaledAsyncTimer(string id)
    {
        if (unscaledAsyncTimers.TryGetValue(id, out var timer))
        {
            StopCoroutine(timer.coroutine);
            unscaledAsyncTimers.Remove(id);
        }
    }

    public void StopScaledAsyncStopwatch(string id)
    {
        if (scaledAsyncStopwatches.TryGetValue(id, out var stopwatch))
        {
            StopCoroutine(stopwatch.coroutine);
            scaledAsyncStopwatches.Remove(id);
        }
    }

    public void StopUnscaledAsyncStopwatch(string id)
    {
        if (unscaledAsyncStopwatches.TryGetValue(id, out var stopwatch))
        {
            StopCoroutine(stopwatch.coroutine);
            unscaledAsyncStopwatches.Remove(id);
        }
    }
    #endregion

    #region Sync
    public ScaledTimer CreateScaledTimer(float duration, float multiplier = 1f, Action onEnd = null, float updateInterval = 0f, Action<float> onUpdate = null)
    {
        var timer = new ScaledTimer(duration, multiplier, onEnd, updateInterval, onUpdate);
        scaledTimers.Add(timer);
        return timer;
    }

    public UnscaledTimer CreateUnscaledTimer(float duration, float multiplier = 1f, Action onEnd = null, float updateInterval = 0f, Action<float> onUpdate = null)
    {
        var timer = new UnscaledTimer(duration, multiplier, onEnd, updateInterval, onUpdate);
        unscaledTimers.Add(timer);
        return timer;
    }

    public ScaledStopwatch CreateScaledStopwatch(float updateInterval = 1f, float multiplier = 1f, Action<float> onUpdate = null)
    {
        var stopwatch = new ScaledStopwatch(multiplier, updateInterval, onUpdate);
        scaledStopwatches.Add(stopwatch);
        return stopwatch;
    }

    public UnscaledStopwatch CreateUnscaledStopwatch(float updateInterval = 1f, float multiplier = 1f, Action<float> onUpdate = null)
    {
        var stopwatch = new UnscaledStopwatch(multiplier, updateInterval, onUpdate);
        unscaledStopwatches.Add(stopwatch);
        return stopwatch;
    }
    #endregion

    #region Private
    private void UpdateTimers<T>(List<T> timers, float deltaTime) where T : TimeBase
    {
        for (int i = timers.Count - 1; i >= 0; i--)
            if (timers[i].Update(deltaTime))
                timers.RemoveAt(i);
    }

    private void UpdateStopwatches<T>(List<T> stopwatches, float deltaTime) where T : TimeBase
    {
        foreach (var stopwatch in stopwatches)
            stopwatch.Update(deltaTime);
    }
    #endregion

    #region Timer/Stopwatch
    public abstract class TimeBase
    {
        protected float elapsedTime;
        protected float updateInterval;
        protected float updateTime;
        protected float multiplier;
        protected Action<float> onUpdate;
        protected bool isPaused;
        public bool UseUnscaled { get; protected set; }

        public TimeBase(float multiplier, float interval, Action<float> onUpdate, bool unscaled)
        {
            this.multiplier = multiplier;
            this.updateInterval = interval;
            this.onUpdate = onUpdate;
            UseUnscaled = unscaled;
        }

        public virtual bool Update(float delta)
        {
            if (isPaused) return false;

            float scaledDelta = delta * multiplier;
            elapsedTime += scaledDelta;
            updateTime += scaledDelta;

            if (updateInterval > 0 && updateTime >= updateInterval)
            {
                updateTime = 0;
                onUpdate?.Invoke(elapsedTime);
            }

            return false;
        }

        public void Pause() => isPaused = true;
        public void Resume() => isPaused = false;
        public float GetElapsedTime() => elapsedTime;
        public void SetMultiplier(float newMultiplier) => multiplier = Mathf.Max(newMultiplier, 0.01f);
    }

    public class Timer : TimeBase
    {
        protected float duration;
        protected Action onTimerEnd;

        public Timer(float duration, float multiplier, Action onEnd, float interval, Action<float> onUpdate, bool unscaled)
            : base(multiplier, interval, onUpdate, unscaled)
        {
            this.duration = duration;
            this.onTimerEnd = onEnd;
        }

        public override bool Update(float deltaTime)
        {
            base.Update(deltaTime);
            if (elapsedTime >= duration)
            {
                onTimerEnd?.Invoke();
                return true;
            }
            return false;
        }

        public void ResetTimer()
        {
            elapsedTime = 0f;
        }
        public float GetRemainingTime() => Mathf.Max(duration - elapsedTime, 0f);
        public void AddTime(float extraTime) => duration += extraTime;
    }

    public class Stopwatch : TimeBase
    {
        private List<float> laps = new List<float>();
        public Stopwatch(float multiplier, float updateInterval, Action<float> onUpdate) : base(multiplier, updateInterval, onUpdate) { }

        public void RecordLap()
        {
            laps.Add(elapsedTime);
            elapsedTime = 0f;
        }

        public List<float> GetLaps()
        {
            return laps;
        }
        public void ResetStopwatch()
        {
            elapsedTime = 0f;
            laps.Clear();
        }
    }

    public abstract class AsyncTimer : Timer
    {
        public Coroutine coroutine;
        protected bool unscaled;

        protected AsyncTimer(float duration, float multiplier, Action onEnd, float updateInterval, Action<float> onUpdate)
            : base(duration, multiplier, onEnd, updateInterval, onUpdate) { }

        public IEnumerator TimerCoroutine()
        {
            while (GetRemainingTime() > 0f)
            {
                if (updateInterval > 0 && onUpdate != null)
                {
                    onUpdate.Invoke(GetRemainingTime());
                    yield return unscaled ? new WaitForSecondsRealtime(updateInterval) : new WaitForSeconds(updateInterval);
                }
                else
                    yield return null;

                elapsedTime += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) * multiplier;
            }

            onTimerEnd?.Invoke();
        }
    }

    public abstract class AsyncStopwatch : Stopwatch
    {
        public Coroutine coroutine;
        protected bool unscaled;

        protected AsyncStopwatch(float multiplier, float updateInterval, Action<float> onUpdate)
            : base(multiplier, updateInterval, onUpdate) { }

        public IEnumerator StopwatchCoroutine()
        {
            while (true)
            {
                if (updateInterval > 0 && onUpdate != null)
                {
                    onUpdate.Invoke(elapsedTime);
                    yield return unscaled ? new WaitForSecondsRealtime(updateInterval) : new WaitForSeconds(updateInterval);
                }
                else
                    yield return null;

                elapsedTime += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) * multiplier;
            }
        }
    }
    #endregion
}



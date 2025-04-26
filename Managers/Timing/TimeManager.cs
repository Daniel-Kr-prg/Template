using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;

public class TimeManager : SingletonManager<TimeManager>
{
    private Dictionary<string, AsyncTimer> asyncTimers = new();
    private Dictionary<string, AsyncStopwatch> asyncStopwatches = new();

    private List<Timer> syncTimers = new();
    private List<Stopwatch> syncStopwatches = new();

    private void Start()
    {
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_TimeManagerReady");
    }

    private void Update()
    {
        UpdateSyncTimers();
        UpdateSyncStopwatches();
    }

    #region Async Methods
    public void StartAsyncTimer(string id, float duration, float multiplier = 1f, Action onEnd = null, float interval = 0f, Action<float> onUpdate = null, bool unscaled = false)
    {
        if (asyncTimers.ContainsKey(id))
            return;

        var timer = new AsyncTimer(duration, multiplier, onEnd, interval, onUpdate, unscaled);
        timer.coroutine = StartCoroutine(timer.TimerCoroutine());
        asyncTimers[id] = timer;
    }

    public void StartAsyncStopwatch(string id, float multiplier = 1f, float interval = 1f, Action<float> onUpdate = null, bool unscaled = false)
    {
        if (asyncStopwatches.ContainsKey(id))
            return;

        var stopwatch = new AsyncStopwatch(multiplier, interval, onUpdate, unscaled);
        stopwatch.coroutine = StartCoroutine(stopwatch.StopwatchCoroutine());
        asyncStopwatches[id] = stopwatch;
    }

    public void StopAsyncTimer(string id)
    {
        if (asyncTimers.TryGetValue(id, out var timer))
        {
            StopCoroutine(timer.coroutine);
            asyncTimers.Remove(id);
        }
    }

    public void StopAsyncStopwatch(string id)
    {
        if (asyncStopwatches.TryGetValue(id, out var stopwatch))
        {
            StopCoroutine(stopwatch.coroutine);
            asyncStopwatches.Remove(id);
        }
    }
    #endregion

    #region Sync Methods
    public Timer CreateSyncTimer(float duration, float multiplier = 1f, Action onEnd = null, float interval = 0f, Action<float> onUpdate = null, bool unscaled = false)
    {
        var timer = new Timer(duration, multiplier, onEnd, interval, onUpdate, unscaled);
        syncTimers.Add(timer);
        return timer;
    }

    public Stopwatch CreateSyncStopwatch(float multiplier = 1f, float interval = 1f, Action<float> onUpdate = null, bool unscaled = false)
    {
        var stopwatch = new Stopwatch(multiplier, interval, onUpdate, unscaled);
        syncStopwatches.Add(stopwatch);
        return stopwatch;
    }

    private void UpdateSyncTimers()
    {
        for (int i = syncTimers.Count - 1; i >= 0; i--)
        {
            if (syncTimers[i].Update(syncTimers[i].UseUnscaled ? Time.unscaledDeltaTime : Time.deltaTime))
                syncTimers.RemoveAt(i);
        }
    }

    private void UpdateSyncStopwatches()
    {
        foreach (var stopwatch in syncStopwatches)
            stopwatch.Update(stopwatch.UseUnscaled ? Time.unscaledDeltaTime : Time.deltaTime);
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
        public Stopwatch(float multiplier, float interval, Action<float> onUpdate, bool unscaled)
            : base(multiplier, interval, onUpdate, unscaled) { }

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

    public class AsyncTimer : Timer
    {
        public Coroutine coroutine;

        public AsyncTimer(float duration, float multiplier, Action onEnd, float interval, Action<float> onUpdate, bool unscaled)
            : base(duration, multiplier, onEnd, interval, onUpdate, unscaled) { }

        public IEnumerator TimerCoroutine()
        {
            while (elapsedTime < duration)
            {
                yield return null;
                Update(UseUnscaled ? Time.unscaledDeltaTime : Time.deltaTime);
            }

            onTimerEnd?.Invoke();
        }
    }

    public class AsyncStopwatch : Stopwatch
    {
        public Coroutine coroutine;

        public AsyncStopwatch(float multiplier, float interval, Action<float> onUpdate, bool unscaled)
            : base(multiplier, interval, onUpdate, unscaled) { }

        public IEnumerator StopwatchCoroutine()
        {
            while (true)
            {
                yield return null;
                Update(UseUnscaled ? Time.unscaledDeltaTime : Time.deltaTime);
            }
        }
    }
    #endregion
}
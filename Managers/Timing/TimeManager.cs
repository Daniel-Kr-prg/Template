using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;

public class TimeManager : SingletonManager<TimeManager>
{
    private Dictionary<string, AsyncTimer> asyncTimers = new Dictionary<string, AsyncTimer>();
    private Dictionary<string, AsyncStopwatch> asyncStopwatches = new Dictionary<string, AsyncStopwatch>();

    private List<Timer> syncTimers = new List<Timer>();
    private List<Stopwatch> syncStopwatches = new List<Stopwatch>();

    private void Start()
    {
        // Additional handling before stage changing

        // Satisfy stage condition
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_TimeManagerReady");
    }

    void Update()
    {
        UpdateSyncTimers();
        UpdateSyncStopwatches();
    }

    #region Async timers/stopwatches
    public void StartAsyncTimer(string id, float duration, float multiplier = 1f, Action onTimerEnd = null, float updateInterval = 0f, Action<float> onUpdate = null)
    {
        if (asyncTimers.ContainsKey(id))
        {
            Debug.LogWarning($"[M] TimeManager / StartAsyncTimer: Async timer with ID '{id}' already exists.");
            return;
        }

        AsyncTimer timer = new AsyncTimer(duration, multiplier, onTimerEnd, updateInterval, onUpdate);
        timer.coroutine = StartCoroutine(timer.TimerCoroutine());
        asyncTimers[id] = timer;
    }

    public void StartAsyncStopwatch(string id, float multiplier = 1f, float updateInterval = 1f, Action<float> onUpdate = null)
    {
        if (asyncStopwatches.ContainsKey(id))
        {
            Debug.LogWarning($"[M] TimeManager / StartAsyncStopwatch: Async stopwatch with ID '{id}' already exists.");
            return;
        }

        AsyncStopwatch stopwatch = new AsyncStopwatch(multiplier, updateInterval, onUpdate);
        stopwatch.coroutine = StartCoroutine(stopwatch.StopwatchCoroutine());
        asyncStopwatches[id] = stopwatch;
    }

    public void StopAsyncTimer(string id)
    {
        if (asyncTimers.TryGetValue(id, out AsyncTimer timer))
        {
            StopCoroutine(timer.coroutine);
            asyncTimers.Remove(id);
        }
    }

    public void StopAsyncStopwatch(string id)
    {
        if (asyncStopwatches.TryGetValue(id, out AsyncStopwatch stopwatch))
        {
            StopCoroutine(stopwatch.coroutine);
            asyncStopwatches.Remove(id);
        }
    }
    #endregion

    #region Sync timers/stopwatches
    public Timer CreateSyncTimer(float duration, float multiplier = 1f, Action onTimerEnd = null, float updateInterval = 0f, Action<float> onUpdate = null)
    {
        Timer timer = new Timer(duration, multiplier, onTimerEnd, updateInterval, onUpdate);
        syncTimers.Add(timer);
        return timer;
    }

    public Stopwatch CreateSyncStopwatch(float updateInterval = 1f, float multiplier = 1f, Action<float> onUpdate = null)
    {
        Stopwatch stopwatch = new Stopwatch(updateInterval, multiplier, onUpdate);
        syncStopwatches.Add(stopwatch);
        return stopwatch;
    }
    public void StopSyncTimer(Timer timer)
    {
        if (syncTimers.Contains(timer))
        {
            syncTimers.Remove(timer);
        }
    }

    public void StopSyncStopwatch(Stopwatch stopwatch)
    {
        if (syncStopwatches.Contains(stopwatch))
        {
            syncStopwatches.Remove(stopwatch);
        }
    }
    public void ClearAllSyncTimers()
    {
        syncTimers.Clear();
    }

    public void ClearAllSyncStopwatches()
    {
        syncStopwatches.Clear();
    }

    private void UpdateSyncTimers()
    {
        for (int i = syncTimers.Count - 1; i >= 0; i--)
        {
            if (syncTimers[i].Update(Time.deltaTime))
            {
                syncTimers.RemoveAt(i); // Удаляем таймер, который завершился
            }
        }
    }

    private void UpdateSyncStopwatches()
    {
        foreach (var stopwatch in syncStopwatches)
        {
            stopwatch.Update(Time.deltaTime);
        }
    }

    public void PauseSyncTimer(Timer timer)
    {
        if (syncTimers.Contains(timer))
        {
            timer.Pause();
        }
    }

    public void ResumeSyncTimer(Timer timer)
    {
        if (syncTimers.Contains(timer))
        {
            timer.Resume();
        }
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

        protected bool isPaused = false;

        public TimeBase(float updateInterval, float multiplier, Action<float> onUpdate)
        {
            this.multiplier = multiplier;
            this.updateInterval = updateInterval;
            this.onUpdate = onUpdate;

            elapsedTime = 0f;
            updateTime = 0f;
        }

        public virtual bool Update(float deltaTime)
        {
            if (isPaused) return false;

            elapsedTime += deltaTime * multiplier;
            updateTime += deltaTime * multiplier;

            if (updateInterval > 0 && updateTime >= updateInterval)
            {
                updateTime = 0f;
                onUpdate?.Invoke(elapsedTime);
            }

            return false;
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Resume()
        {
            isPaused = false;
        }

        public float GetElapsedTime()
        {
            return elapsedTime;
        }

        public void SetMultiplier(float newMultiplier)
        {
            multiplier = Mathf.Max(newMultiplier, 0.01f);
        }
    }

    public class Timer : TimeBase
    {
        protected float duration;
        protected Action onTimerEnd;

        public Timer(float duration, float multiplier, Action onTimerEnd, float updateInterval, Action<float> onUpdate)
            : base(multiplier, updateInterval, onUpdate)
        {
            this.duration = duration;
            this.onTimerEnd = onTimerEnd;
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

        public void AddTime(float additionalTime)
        {
            duration += additionalTime;
        }

        public float GetRemainingTime()
        {
            return Math.Max(duration - elapsedTime, 0f);
        }
    }

    public class AsyncTimer : Timer
    {
        public Coroutine coroutine;

        public AsyncTimer(float duration, float multiplier, Action onTimerEnd, float updateInterval, Action<float> onUpdate) : base(duration, multiplier, onTimerEnd, updateInterval, onUpdate)
        {
        }

        public IEnumerator TimerCoroutine()
        {
            float remainingTime = GetRemainingTime();
            while (remainingTime > 0)
            {
                if (updateInterval > 0 && onUpdate != null)
                {
                    onUpdate.Invoke(remainingTime); // Вызываем onUpdate для таймера
                    yield return new WaitForSeconds(updateInterval);
                    remainingTime -= updateInterval * multiplier; // Учитываем множитель времени
                }
                else
                {
                    yield return null;
                    remainingTime -= Time.deltaTime * multiplier; // Учитываем множитель времени
                }
            }

            onTimerEnd?.Invoke();
        }
    }

    public class Stopwatch : TimeBase
    {
        private List<float> laps = new List<float>();

        public Stopwatch(float multiplier, float updateInterval, Action<float> onUpdate)
            : base(multiplier, updateInterval, onUpdate)
        {
        }

        public void RecordLap()
        {
            laps.Add(elapsedTime);
            elapsedTime = 0f;
        }

        public List<float> GetLaps()
        {
            return laps;
        }
    }

    public class AsyncStopwatch : Stopwatch
    {
        public Coroutine coroutine;

        public AsyncStopwatch(float multiplier, float updateInterval, Action<float> onUpdate) : base(multiplier, updateInterval, onUpdate)
        {
        }

        public IEnumerator StopwatchCoroutine()
        {
            float elapsedTime = 0f;
            while (true)
            {
                if (updateInterval > 0 && onUpdate != null)
                {
                    onUpdate.Invoke(elapsedTime); // Вызываем onUpdate для секундомера
                    yield return new WaitForSeconds(updateInterval);
                    elapsedTime += updateInterval * multiplier; // Учитываем множитель времени
                }
                else
                {
                    yield return null;
                    elapsedTime += Time.deltaTime * multiplier; // Учитываем множитель времени
                }
            }
        }
    }
    #endregion
}



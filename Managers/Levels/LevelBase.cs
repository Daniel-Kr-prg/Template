using System;
using UnityEngine;

public class LevelBase : MonoBehaviour
{
    public event Action OnLevelCompleted;
    public event Action OnLevelFailed;

    public LevelData LevelData { get; private set; }

    public virtual void Initialize(LevelData levelData)
    {
        LevelData = levelData;
    }

    public virtual void StartLevel()
    {
    }

    protected void CompleteLevel()
    {
        OnLevelCompleted?.Invoke();
    }

    protected void FailLevel()
    {
        OnLevelFailed?.Invoke();
    }
}

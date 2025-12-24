using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class Statistics_LevelsProgress : MonoBehaviour
{
    #region Data

    public Dictionary<string, LevelProgress> progressByLevel = new Dictionary<string, LevelProgress>();

    #endregion

    #region Save System

    private LevelsProgressSaveItem saveItem;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        saveItem = new LevelsProgressSaveItem("LevelsProgress", this);
    }

    #endregion

    #region Progress Management

    public void MarkLevelCompleted(string levelID, LevelProgress newProgress)
    {
        if (progressByLevel.TryGetValue(levelID, out var existing))
        {
            if (newProgress.Stars > existing.Stars)
                progressByLevel[levelID] = newProgress;
        }
        else
        {
            progressByLevel[levelID] = newProgress;
        }
    }

    public LevelProgress GetLevelProgress(string levelID)
    {
        return progressByLevel.TryGetValue(levelID, out var p) ? p : default;
    }

    public bool HasProgress(string levelID)
    {
        return progressByLevel.ContainsKey(levelID);
    }

    public int GetTotalStars()
    {
        int total = 0;
        foreach (var progress in progressByLevel.Values)
        {
            total += progress.Stars;
        }
        return total;
    }

    public int GetCompletedLevelsCount()
    {
        return progressByLevel.Count;
    }

    public void ClearProgress()
    {
        progressByLevel.Clear();
    }

    #endregion
}

[Serializable]
public struct LevelProgress
{
    public int Stars;
    public float CompletionTime;

    public LevelProgress(int stars, float completionTime)
    {
        Stars = stars;
        CompletionTime = completionTime;
    }
}

public class LevelsProgressSaveItem : SaveItem
{
    #region Fields

    private readonly Statistics_LevelsProgress progress;

    #endregion

    #region Constructor

    public LevelsProgressSaveItem(string id, Statistics_LevelsProgress target)
        : base(id, target)
    {
        progress = target;
    }

    #endregion

    #region Save/Load

    public override string CreateSaveData(object sourceObject)
    {
        return JsonConvert.SerializeObject(progress.progressByLevel);
    }

    protected override void LoadCallback()
    {
        var loaded = SaveManager.Load<Dictionary<string, LevelProgress>>(id);
        if (loaded != null)
            progress.progressByLevel = loaded;
        else
            progress.progressByLevel.Clear();
    }

    #endregion
}


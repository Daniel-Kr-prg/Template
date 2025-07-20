using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class Game_PlayerProgress : MonoBehaviour
{
    // Dictionary mapping level name to its progress data
    public Dictionary<string, LevelProgress> progressByLevel = new Dictionary<string, LevelProgress>();

    private PlayerProgressSaveItem saveItem;

    private void Awake()
    {
        // Register this MonoBehaviour with the save system
        saveItem = new PlayerProgressSaveItem("PlayerProgress", this);
    }

    /// <summary>
    /// Update progress for a level: store new data only if stars are higher.
    /// </summary>
    /// <param name="levelName">Unique name of the level.</param>
    /// <param name="newProgress">New progress data (stars + time).</param>
    public void MarkLevelCompleted(string levelName, LevelProgress newProgress)
    {
        if (progressByLevel.TryGetValue(levelName, out var existing))
        {
            // Replace only if more stars achieved
            if (newProgress.Stars > existing.Stars)
                progressByLevel[levelName] = newProgress;
        }
        else
        {
            // First time completion
            progressByLevel[levelName] = newProgress;
        }
    }

    /// <summary>
    /// Retrieves progress for a level, or default if not found.
    /// </summary>
    public LevelProgress GetLevelProgress(string levelName)
    {
        return progressByLevel.TryGetValue(levelName, out var p) ? p : default;
    }
}

/// <summary>
/// Value type holding star count and completion time (in seconds).
/// </summary>
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

/// <summary>
/// SaveItem implementation for Game_PlayerProgress.
/// Serializes and deserializes the progressByLevel dictionary.
/// </summary>
public class PlayerProgressSaveItem : SaveItem
{
    private readonly Game_PlayerProgress progress;

    public PlayerProgressSaveItem(string id, Game_PlayerProgress target)
        : base(id, target)
    {
        progress = target;
    }

    /// <summary>
    /// Serialize the dictionary to JSON.
    /// </summary>
    public override string CreateSaveData(object sourceObject)
    {
        // We serialize the entire progressByLevel dictionary
        return JsonConvert.SerializeObject(progress.progressByLevel);
    }

    /// <summary>
    /// Called when loading: populate the dictionary from saved JSON.
    /// </summary>
    protected override void LoadCallback()
    {
        var loaded = SaveManager.Load<Dictionary<string, LevelProgress>>(id);
        if (loaded != null)
            progress.progressByLevel = loaded;
        else
            progress.progressByLevel.Clear();
    }
}
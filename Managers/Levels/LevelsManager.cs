using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class LevelsManager : SingletonManager<LevelsManager>
{
    [Header("Data")]
    [SerializeField]
    private LevelsCollection levelsCollection;

    [Header("Dependencies")]
    [SerializeField]
    private Game_PlayerProgress playerProgress;

    [SerializeField] private Transform levelContainer;
    // Events
    public event Action<string> LevelStarted;
    public event Action<string, LevelProgress> LevelCompleted;

    private string currentLevelName;
    private ILevelController currentLevel;

    protected override void Awake()
    {
        base.Awake();
        if (levelsCollection == null) DebugError("LevelsCollection not assigned in LevelsManager");
        if (playerProgress == null) DebugError("Game_PlayerProgress not assigned in LevelsManager");
    }

    private void Start()
    {
        StagesManager.Instance.AppStages.RegisterStageStartAction(AppStageName.Start, "LevelsManager_LoadData", () =>
        {
            DebugMessage("Start stage");
        });
    }

    /// <summary>
    /// Starts the given level by instantiating its prefab.
    /// </summary>
    public void StartLevel(string levelName)
    {
        var prefab = levelsCollection.GetLevel(levelName);
        if (prefab == null)
        {
            DebugError($"Level '{levelName}' not found in collection.");
            return;
        }
        currentLevelName = levelName;
        
        var level = Instantiate(prefab, levelContainer);
        level.transform.position = Vector3.zero;
        currentLevel = level.GetComponent<ILevelController>();

        LevelStarted?.Invoke(levelName);

        currentLevel.Setup();
    }

    public void StartLevel(int index)
    {
        var prefab = levelsCollection.GetLevel(index);
        if (prefab == null)
        {
            DebugError($"Level '{prefab.name}' not found in collection.");
            return;
        }
        currentLevelName = prefab.name;

        var level = Instantiate(prefab, levelContainer);
        level.transform.position = Vector3.zero;
        currentLevel = level.GetComponent<ILevelController>();

        LevelStarted?.Invoke(prefab.name);

        currentLevel.Setup();
    }

    /// <summary>
    /// Marks the current level as completed with stars and time, saves progress.
    /// </summary>
    public void CompleteCurrentLevel(int stars, float completionTime)
    {
        CompleteLevel(currentLevelName, new LevelProgress(stars, completionTime));
    }

    /// <summary>
    /// Marks specified level as completed, updates save and fires event.
    /// </summary>
    public void CompleteLevel(string levelName, LevelProgress progressData)
    {
        playerProgress.MarkLevelCompleted(levelName, progressData);
        LevelCompleted?.Invoke(levelName, progressData);
        SaveManager.Save();
    }

    /// <summary>
    /// Gets the prefab for a given level name.
    /// </summary>
    public LevelData GetLevelPrefab(string levelName) => levelsCollection.GetLevel(levelName);

    /// <summary>
    /// Gets the prefab for the next level after the given one.
    /// </summary>
    public LevelData GetNextLevelPrefab(string levelName) => levelsCollection.GetNextLevel(levelName);

    /// <summary>
    /// Retrieves saved progress for a level.
    /// </summary>
    public LevelProgress GetProgress(string levelName) => playerProgress.GetLevelProgress(levelName);

    /// <summary>
    /// Returns true if the level is unlocked (first level always unlocked, others require previous completion).
    /// </summary>
    public bool IsLevelUnlocked(string levelName)
    {
        var all = levelsCollection.LevelNames.ToList();
        int index = all.IndexOf(levelName);
        if (index < 0) return false;
        if (index == 0) return true;
        return playerProgress.GetLevelProgress(all[index - 1]).Stars > 0;
    }

    /// <summary>
    /// Enumerates all unlocked levels in order.
    /// </summary>
    public IEnumerable<string> GetUnlockedLevels()
    {
        foreach (var lvl in levelsCollection.LevelNames)
        {
            if (IsLevelUnlocked(lvl))
                yield return lvl;
            else
                break;
        }
    }

    /// <summary>
    /// Enumerates all levels that have any saved progress (completed with at least 1 star).
    /// </summary>
    public IEnumerable<string> GetCompletedLevels() => playerProgress.progressByLevel.Keys;

    /// <summary>
    /// Utility: returns all level names in the collection.
    /// </summary>
    public IEnumerable<string> GetAllLevelNames() => levelsCollection.LevelNames;
}

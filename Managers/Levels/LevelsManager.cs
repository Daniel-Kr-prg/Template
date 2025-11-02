using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using DanieloZ.InstancePromise;

public class LevelsManager : SingletonManager<LevelsManager>
{
    #region Serialized Fields

    [SerializeField] private LevelsCollection levelsCollection;
    [SerializeField] private Game_PlayerProgress playerProgress;

    #endregion

    #region Events

    public event Action<string, LevelData> OnLevelStarted;
    public event Action<string, LevelProgress> OnLevelCompleted;
    public event Action<string> OnLevelUnloaded;

    #endregion

    #region Current Level State

    private string currentLevelName;
    private LevelData currentLevelData;
    private GameObject currentLevelInstance;
    private float levelStartTime;

    public LevelData CurrentLevelData => currentLevelData;
    public string CurrentLevelName => currentLevelName;
    public bool IsLevelActive => currentLevelInstance != null;
    public float CurrentLevelTime => IsLevelActive ? Time.time - levelStartTime : 0f;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        ValidateDependencies();
    }

    private void Start()
    {
        StagesManager.GetInstanceAsync(RegisterStageActions);
    }

    #endregion

    #region Initialization

    private void ValidateDependencies()
    {
        if (levelsCollection == null) DebugError("LevelsCollection not assigned");
        if (playerProgress == null) DebugError("Game_PlayerProgress not assigned");
    }

    private void RegisterStageActions(StagesManager stagesManager)
    {
        stagesManager.AppStages.stages[AppStageName.Start].RegisterStageStartAction(
            "LevelsManager_Ready",
            () => DebugMessage("Levels system ready")
        );
    }

    #endregion

    #region Level Loading

    public void LoadLevel(string levelID)
    {
        var levelData = levelsCollection.GetLevel(levelID);
        if (levelData == null)
        {
            DebugError($"Level '{levelID}' not found");
            return;
        }

        UnloadCurrentLevel();

        UI_LevelManager.GetInstanceAsync(uiLevelManager =>
        {
            currentLevelInstance = uiLevelManager.InstantiateLevel(levelData.levelPrefab);
            if (currentLevelInstance == null)
            {
                DebugError($"Failed to instantiate level '{levelID}'");
                return;
            }

            currentLevelName = levelID;
            currentLevelData = levelData;
            levelStartTime = Time.time;

            OnLevelStarted?.Invoke(levelID, levelData);
            DebugMessage($"Level loaded: {levelData.displayName}");
        });
    }

    public void LoadLevel(int index)
    {
        var prefab = levelsCollection.GetLevel(index);
        if (prefab == null)
        {
            DebugError($"Level at index {index} not found");
            return;
        }
        LoadLevel(levelName);
    }

    public void LoadNextLevel()
    {
        if (string.IsNullOrEmpty(currentLevelName))
        {
            DebugWarning("No current level to get next from");
            return;
        }

        var nextLevel = levelsCollection.GetNextLevel(currentLevelName);
        if (nextLevel == null)
        {
            DebugMessage("No next level available");
            return;
        }

        LoadLevel(nextLevel.levelID);
    }

    public void UnloadCurrentLevel()
    {
        if (currentLevelInstance != null)
        {
            UI_LevelManager.GetInstanceAsync(uiLevelManager =>
            {
                uiLevelManager.DestroyLevel(currentLevelInstance);
            });

            OnLevelUnloaded?.Invoke(currentLevelName);
            currentLevelInstance = null;
            currentLevelData = null;
            currentLevelName = null;
        }
    }

    #endregion

    #region Level Completion

    public void CompleteCurrentLevel(int stars)
    {
        if (string.IsNullOrEmpty(currentLevelName))
        {
            DebugWarning("No active level to complete");
            return;
        }

        float completionTime = CurrentLevelTime;
        CompleteLevel(currentLevelName, new LevelProgress(stars, completionTime));
    }

    public void CompleteLevel(string levelID, LevelProgress progress)
    {
        playerProgress.MarkLevelCompleted(levelID, progress);
        OnLevelCompleted?.Invoke(levelID, progress);
        SaveManager.Save();
        DebugMessage($"Level '{levelID}' completed: {progress.Stars} stars, {progress.CompletionTime:F2}s");
    }

    public int CalculateStars(float completionTime)
    {
        if (currentLevelData == null) return 0;

        // TODO count stars

        return 1;
    }

    #endregion

    #region Level Data Access

    public LevelData GetLevelData(string levelID) => levelsCollection.GetLevel(levelID);
    public LevelData GetLevelData(int index) => levelsCollection.GetLevel(index);
    public LevelData GetNextLevelData(string levelID) => levelsCollection.GetNextLevel(levelID);
    public IEnumerable<string> GetAllLevelIDs() => levelsCollection.LevelNames;
    public int GetLevelCount() => levelsCollection.LevelCount;

    #endregion

    #region Progress Access

    public LevelProgress GetProgress(string levelID) => playerProgress.GetLevelProgress(levelID);

    public bool IsLevelUnlocked(string levelID)
    {
        var allLevels = levelsCollection.LevelNames.ToList();
        int index = allLevels.IndexOf(levelID);
        if (index < 0) return false;
        if (index == 0) return true;
        return playerProgress.GetLevelProgress(allLevels[index - 1]).Stars > 0;
    }

    public bool IsLevelCompleted(string levelID) => GetProgress(levelID).Stars > 0;

    public IEnumerable<string> GetUnlockedLevels()
    {
        foreach (var levelID in levelsCollection.LevelNames)
        {
            if (IsLevelUnlocked(levelID))
                yield return levelID;
            else
                break;
        }
    }

    public IEnumerable<string> GetCompletedLevels() => playerProgress.progressByLevel.Keys;

    #endregion
}

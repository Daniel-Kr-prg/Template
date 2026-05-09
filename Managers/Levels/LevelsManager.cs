using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using DanieloZ.InstancePromise;

public class LevelsManager : SingletonManager<LevelsManager>
{
    #region Serialized Fields

    [SerializeField] private LevelsCollection levelsCollection;
    private Statistics_LevelsProgress levelsProgress;

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
    private LevelBase currentLevel;
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
        StatisticsManager.GetInstanceAsync(x =>
        {
            levelsProgress = x.LevelsProgress;

            if (levelsProgress == null)
            {
                DebugError("Statistics_LevelsProgress not assigned");
            }
        });
    }

    #endregion

    #region Initialization

    private void ValidateDependencies()
    {
        if (levelsCollection == null) DebugError("LevelsCollection not assigned");
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

            // Initialize and start the level
            // По правилу проекта: компонент уровня ВСЕГДА на корне префаба.
            currentLevel = currentLevelInstance.GetComponent<LevelBase>();
            if (currentLevel == null)
            {
                DebugWarning($"Level prefab '{levelID}' does not have LevelBase component on root");
                OnLevelStarted?.Invoke(levelID, levelData);
                return;
            }

            SubscribeToCurrentLevel();
            currentLevel.Initialize(levelData);
            currentLevel.StartLevel();

            OnLevelStarted?.Invoke(levelID, levelData);
            DebugMessage($"Level loaded: {levelData.displayName}");
        });
    }

    public void LoadLevel(int index)
    {
        var levelName = levelsCollection.GetLevelName(index);
        if (levelName == null)
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
            UnsubscribeFromCurrentLevel();

            UI_LevelManager.GetInstanceAsync(uiLevelManager =>
            {
                uiLevelManager.DestroyLevel(currentLevelInstance);
            });

            OnLevelUnloaded?.Invoke(currentLevelName);
            currentLevelInstance = null;
            currentLevelData = null;
            currentLevelName = null;
            currentLevel = null;
        }
    }

    #endregion

    #region LevelBase Integration

    private void SubscribeToCurrentLevel()
    {
        if (currentLevel == null)
            return;

        currentLevel.OnLevelCompleted += HandleLevelCompleted;
        currentLevel.OnLevelFailed += HandleLevelFailed;
    }

    private void UnsubscribeFromCurrentLevel()
    {
        if (currentLevel == null)
            return;

        currentLevel.OnLevelCompleted -= HandleLevelCompleted;
        currentLevel.OnLevelFailed -= HandleLevelFailed;
    }

    private void HandleLevelCompleted()
    {
        // Автозавершение уровня в системе прогресса
        CompleteCurrentLevel(CalculateStars(CurrentLevelTime));
    }

    private void HandleLevelFailed()
    {
        // Хук под будущую логику поражения
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
        if (levelsProgress == null)
        {
            DebugError("Statistics_LevelsProgress not assigned");
            return;
        }

        levelsProgress.MarkLevelCompleted(levelID, progress);
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

    public LevelProgress GetProgress(string levelID) => levelsProgress != null ? levelsProgress.GetLevelProgress(levelID) : default;

    public bool IsLevelUnlocked(string levelID)
    {
        var allLevels = levelsCollection.LevelNames.ToList();
        int index = allLevels.IndexOf(levelID);
        if (index < 0) return false;
        if (index == 0) return true;
        return levelsProgress != null && levelsProgress.GetLevelProgress(allLevels[index - 1]).Stars > 0;
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

    public IEnumerable<string> GetCompletedLevels() => levelsProgress != null ? levelsProgress.progressByLevel.Keys : Array.Empty<string>();

    public int GetTotalStars() => levelsProgress != null ? levelsProgress.GetTotalStars() : 0;

    public int GetCompletedLevelsCount() => levelsProgress != null ? levelsProgress.GetCompletedLevelsCount() : 0;

    #endregion

    #region Utilities

    public List<(string levelID, string levelName)> GetListForLevelSelect()
    {
        return levelsCollection.levels.Select(x => (x.Key, x.Value.displayName)).ToList();
    }

    #endregion
}

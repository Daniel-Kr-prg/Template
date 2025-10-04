using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

/// <summary>
/// Простой менеджер уровней WordPath.
/// Загружает данные из LevelSettings и инициализирует игровые компоненты.
/// </summary>
public class LevelsManager : SingletonManager<LevelsManager>
{
    [Header("Data")]
    [SerializeField] private LevelsCollection levelsCollection;

    [Header("Dependencies")]
    [SerializeField] private Game_PlayerProgress playerProgress;

    // Events
    public event Action<string, LevelSettings> LevelStarted;
    public event Action<string, LevelProgress> LevelCompleted;

    // Current level state
    private string currentLevelName;
    private LevelSettings currentLevelSettings;
    
    /// <summary>
    /// Настройки текущего уровня
    /// </summary>
    public LevelSettings CurrentLevelSettings => currentLevelSettings;
    
    /// <summary>
    /// Имя текущего уровня
    /// </summary>
    public string CurrentLevelName => currentLevelName;

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
    /// Запустить уровень по имени
    /// </summary>
    public void StartLevel(string levelName)
    {
        var levelSettings = levelsCollection.GetLevel(levelName);
        if (levelSettings == null)
        {
            DebugError($"Уровень '{levelName}' не найден в коллекции.");
            return;
        }
        
        currentLevelName = levelName;
        currentLevelSettings = levelSettings;
        
        InitializeLevel(levelSettings);
        
        LevelStarted?.Invoke(levelName, levelSettings);
        
        Debug.Log($"Уровень '{levelName}' ({levelSettings.displayName}) запущен");
    }

    /// <summary>
    /// Запустить уровень по индексу
    /// </summary>
    public void StartLevel(int index)
    {
        var levelName = levelsCollection.GetLevelName(index);
        if (levelName == null)
        {
            DebugError($"Уровень с индексом {index} не найден.");
            return;
        }
        
        StartLevel(levelName);
    }
    
    /// <summary>
    /// Инициализировать игровые компоненты на основе настроек уровня
    /// </summary>
    private void InitializeLevel(LevelSettings levelSettings)
    {

    }

    /// <summary>
    /// Завершить текущий уровень с указанным количеством звезд и времени
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
    /// Получить настройки уровня по имени
    /// </summary>
    public LevelSettings GetLevelSettings(string levelName) => levelsCollection.GetLevel(levelName);

    /// <summary>
    /// Получить настройки следующего уровня
    /// </summary>
    public LevelSettings GetNextLevelSettings(string levelName) => levelsCollection.GetNextLevel(levelName);

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

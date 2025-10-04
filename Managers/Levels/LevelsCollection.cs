using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Коллекция настроек уровней WordPath.
/// Содержит словарь LevelSettings - каждый уровень описывается полностью в ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "LevelsCollection", menuName = "WordPath/Levels Collection")]
public class LevelsCollection : ScriptableObject
{
    [Header("Collection Info")]
    public string collectionName = "WordPath Levels";
    
    [Header("Levels")]
    [SerializeField] public SerializedDictionary<string, LevelSettings> levels;
    
    /// <summary>
    /// Имена всех уровней в коллекции
    /// </summary>
    public IEnumerable<string> LevelNames => levels?.Keys ?? Enumerable.Empty<string>();
    
    /// <summary>
    /// Количество уровней в коллекции
    /// </summary>
    public int LevelCount => levels?.Count ?? 0;

    /// <summary>
    /// Получить настройки уровня по имени
    /// </summary>
    public LevelSettings GetLevel(string levelName)
    {
        if (levels != null && levels.ContainsKey(levelName))
            return levels[levelName];
        
        Debug.LogWarning($"Уровень '{levelName}' не найден в коллекции {name}");
        return null;
    }

    /// <summary>
    /// Получить настройки уровня по индексу
    /// </summary>
    public LevelSettings GetLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Count)
        {
            Debug.LogError($"Индекс {index} вне границ коллекции (размер: {levels?.Count ?? 0})");
            return null;
        }
        
        return levels.ElementAt(index).Value;
    }

    /// <summary>
    /// Получить настройки следующего уровня
    /// </summary>
    public LevelSettings GetNextLevel(string currentLevelName)
    {
        if (levels == null) return null;
        
        var levelNames = levels.Keys.ToList();
        int currentIndex = levelNames.IndexOf(currentLevelName);
        
        if (currentIndex < 0)
        {
            Debug.LogWarning($"Уровень '{currentLevelName}' не найден в коллекции");
            return null;
        }
        
        int nextIndex = currentIndex + 1;
        if (nextIndex >= levelNames.Count)
        {
            Debug.Log($"Уровень '{currentLevelName}' - последний в коллекции");
            return null;
        }
        
        return levels[levelNames[nextIndex]];
    }
    
    /// <summary>
    /// Получить имя уровня по индексу
    /// </summary>
    public string GetLevelName(int index)
    {
        if (levels == null || index < 0 || index >= levels.Count)
            return null;
            
        return levels.Keys.ElementAt(index);
    }
    
    /// <summary>
    /// Проверить существует ли уровень
    /// </summary>
    public bool HasLevel(string levelName)
    {
        return levels != null && levels.ContainsKey(levelName);
    }
}

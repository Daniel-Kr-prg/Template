using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "LevelsCollection", menuName = "ColorMix/Levels Collection")]
public class LevelsCollection : ScriptableObject
{
    #region Collection Info

    [BoxGroup("Collection")]
    public string collectionName = "Levels";

    [BoxGroup("Collection")]
    [ReadOnly]
    public int totalLevels;

    #endregion

    #region Levels Data

    [BoxGroup("Levels")]
    [SerializeField] public SerializedDictionary<string, LevelData> levels;

    public IEnumerable<string> LevelNames => levels?.Keys ?? Enumerable.Empty<string>();
    public int LevelCount => levels?.Count ?? 0;

    #endregion

    #region Validation

    [Button("Validate Collection"), BoxGroup("Collection")]
    private void ValidateCollection()
    {
        if (levels == null) return;

        totalLevels = levels.Count;
        int index = 1;

        foreach (var kvp in levels)
        {
            if (kvp.Value != null)
            {
                kvp.Value.levelNumber = index++;
                if (string.IsNullOrEmpty(kvp.Value.levelID))
                {
                    Debug.LogWarning($"Level at index {index} has empty ID");
                }
            }
        }

        Debug.Log($"Collection validated: {totalLevels} levels");
    }

    #endregion

    #region Level Access

    public LevelData GetLevel(string levelID)
    {
        if (levels != null && levels.ContainsKey(levelID))
            return levels[levelID];

        Debug.LogWarning($"Level '{levelID}' not found in collection {name}");
        return null;
    }

    public LevelData GetLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Count)
        {
            Debug.LogError($"Index {index} out of bounds (size: {levels?.Count ?? 0})");
            return null;
        }

        return levels.ElementAt(index).Value;
    }

    public LevelData GetNextLevel(string currentLevelID)
    {
        if (levels == null) return null;

        var levelIDs = levels.Keys.ToList();
        int currentIndex = levelIDs.IndexOf(currentLevelID);

        if (currentIndex < 0)
        {
            Debug.LogWarning($"Level '{currentLevelID}' not found in collection");
            return null;
        }

        int nextIndex = currentIndex + 1;
        if (nextIndex >= levelIDs.Count)
        {
            return null;
        }

        return levels[levelIDs[nextIndex]];
    }

    public LevelData GetPreviousLevel(string currentLevelID)
    {
        if (levels == null) return null;

        var levelIDs = levels.Keys.ToList();
        int currentIndex = levelIDs.IndexOf(currentLevelID);

        if (currentIndex <= 0) return null;

        return levels[levelIDs[currentIndex - 1]];
    }

    public string GetLevelName(int index)
    {
        if (levels == null || index < 0 || index >= levels.Count)
            return null;

        return levels.Keys.ElementAt(index);
    }

    public bool HasLevel(string levelID)
    {
        return levels != null && levels.ContainsKey(levelID);
    }

    public int GetLevelIndex(string levelID)
    {
        if (levels == null) return -1;

        var levelIDs = levels.Keys.ToList();
        return levelIDs.IndexOf(levelID);
    }

    #endregion
}


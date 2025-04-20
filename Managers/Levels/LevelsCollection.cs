using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelsCollection", menuName = "Levels/Levels collection")]
public class LevelsCollection : ScriptableObject
{
    public string collectionName;

    [SerializeField] public SerializedDictionary<string, LevelData> levelPrefabs;
    public IEnumerable<string> LevelNames => levelPrefabs?.Keys ?? Enumerable.Empty<string>();

    public LevelData GetLevel(string name)
    {
        if (levelPrefabs.ContainsKey(name))
            return levelPrefabs[name];
        else return null;
    }

    public LevelData GetLevel(int index)
    {
        if (levelPrefabs.Count > index)
            return levelPrefabs.ElementAt(index).Value;
        else
        {
            LevelsManager.Instance.DebugError("Index is out of bounds!");
            return null;
        }
    }

    public LevelData GetNextLevel(string name)
    {
        int index = levelPrefabs.Keys.ToList().IndexOf(name) + 1;
        return levelPrefabs.ElementAt(index).Value;
    }
}

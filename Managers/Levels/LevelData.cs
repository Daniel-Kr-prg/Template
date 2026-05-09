using System;
using UnityEngine;

[Serializable]
public class LevelData
{
    public string levelID;
    public string displayName = "Level";
    public int levelNumber = 1;
    public GameObject levelPrefab;
}

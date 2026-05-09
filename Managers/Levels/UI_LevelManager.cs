using UnityEngine;

public class UI_LevelManager : SingletonManager<UI_LevelManager>
{
    [SerializeField] private Transform levelContainer;

    public GameObject InstantiateLevel(GameObject levelPrefab)
    {
        if (levelPrefab == null)
        {
            DebugWarning("Level prefab is null.");
            return null;
        }

        Transform parent = levelContainer != null ? levelContainer : transform;
        return Instantiate(levelPrefab, parent);
    }

    public void DestroyLevel(GameObject levelInstance)
    {
        if (levelInstance == null)
            return;

        Destroy(levelInstance);
    }
}

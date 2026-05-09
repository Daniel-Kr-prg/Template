using UnityEngine;

public class StatisticsManager : SingletonManager<StatisticsManager>
{
    [SerializeField] private Statistics_LevelsProgress levelsProgress;
    [SerializeField] private Statistics_PlayerProfile playerProfile;

    public Statistics_LevelsProgress LevelsProgress => levelsProgress;
    public Statistics_PlayerProfile PlayerProfile => playerProfile;

    protected override void Awake()
    {
        base.Awake();

        levelsProgress ??= FindAnyObjectByType<Statistics_LevelsProgress>(FindObjectsInactive.Include);
        playerProfile ??= FindAnyObjectByType<Statistics_PlayerProfile>(FindObjectsInactive.Include);
    }
}

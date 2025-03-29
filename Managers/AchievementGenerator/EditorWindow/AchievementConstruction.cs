using UnityEngine;

/// <summary>
/// ScriptableObject that holds the root receiver (and potentially other data).
/// </summary>
[CreateAssetMenu(menuName = "Achievements/Achievement construction")]
public class AchievementConstruction : ScriptableObject
{
    public AG_TexReceiver_Default rootReceiver;
}

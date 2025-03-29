using UnityEngine;

public class LODQualityHandler : MonoBehaviour
{
    LODGroup LODGroup;

    public void UpdateLODQuality(ConfigAvailableSettings.LODQuality LODQuality)
    {
        LODGroup ??= GetComponent<LODGroup>();

        Debug.LogError("LOD QUALITY IMPLEMENT!!!!");
    }
}

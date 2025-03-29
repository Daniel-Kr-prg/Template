using UnityEngine;

public class DrawDistanceHandler : MonoBehaviour
{
    LODGroup LODGroup;

    public void UpdateDrawDistance(float distance)
    {
        LODGroup ??= GetComponent<LODGroup>();


        Debug.LogError("DRAW DISTANCE IMPLEMENT!!!!");
    }
}

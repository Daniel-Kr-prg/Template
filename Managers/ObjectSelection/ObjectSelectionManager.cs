using UnityEngine;
using UnityEngine.Events;

public class ObjectSelectionManager : SingletonManager<ObjectSelectionManager>
{
    private ObjectSelectionHandler currentHovered;
    private ObjectSelectionHandler currentSelected;

    private void Start()
    {
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_ObjectSelectionManagerReady");
    }

    public static void HoverObject(ObjectSelectionHandler hoveredObject)
    {
        if (hoveredObject == null)
            return;
        Instance?.HandleHoverObject(hoveredObject);
        
    }

    public static void ExitObject(ObjectSelectionHandler exitObject)
    {
        if (exitObject == null)
            return;
        Instance?.HandleExitObject(exitObject);
    }

    private void HandleHoverObject(ObjectSelectionHandler hoveredObject)
    {
        currentHovered = hoveredObject;
        DebugMessage($"Hovered object: {hoveredObject.name}");
    }

    private void HandleExitObject(ObjectSelectionHandler exitObject)
    {
        if (currentHovered == exitObject)
        {
            currentHovered = null;
            DebugMessage($"Exit object: {exitObject.name}");
        }
    }
}

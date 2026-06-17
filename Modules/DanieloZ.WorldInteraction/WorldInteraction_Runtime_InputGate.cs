using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public static class WorldInteraction_Runtime_InputGate
    {
        public static WorldInteraction_Drag_Object HeldObject { get; private set; }
        public static bool HasHeldObject => HeldObject != null;
        public static bool BlocksCameraWheel => false;

        public static bool TryClaimHeldObject(WorldInteraction_Drag_Object draggable)
        {
            if (draggable == null)
            {
                return false;
            }

            if (HeldObject != null && HeldObject != draggable)
            {
                return false;
            }

            HeldObject = draggable;
            return true;
        }

        public static void ReleaseHeldObject(WorldInteraction_Drag_Object draggable)
        {
            if (HeldObject == draggable)
            {
                HeldObject = null;
            }
        }

        public static bool TryConsumeWheelForHeldObject(float wheelDelta)
        {
            return false;
        }
    }
}

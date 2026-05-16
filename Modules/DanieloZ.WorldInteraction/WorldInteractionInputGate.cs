using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public static class WorldInteractionInputGate
    {
        public static WorldDraggable HeldObject { get; private set; }
        public static bool HasHeldObject => HeldObject != null;
        public static bool BlocksCameraWheel => HeldObject != null && HeldObject.ConsumesMouseWheel;

        public static bool TryClaimHeldObject(WorldDraggable draggable)
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

        public static void ReleaseHeldObject(WorldDraggable draggable)
        {
            if (HeldObject == draggable)
            {
                HeldObject = null;
            }
        }

        public static bool TryConsumeWheelForHeldObject(float wheelDelta)
        {
            if (HeldObject == null || Mathf.Approximately(wheelDelta, 0f))
            {
                return false;
            }

            return HeldObject.TryRotateHeldByWheel(wheelDelta);
        }
    }
}

using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public interface IWorldInteraction_Drag_PickupTarget
    {
        Transform Transform { get; }
        Transform GripRoot { get; }
        Rigidbody Body { get; }
        bool CanPickup(WorldInteraction_Pointer_Context context);
        void OnHandHoverEnter(WorldInteraction_Pointer_Context context);
        void OnHandHoverExit();
        void OnPickup(WorldInteraction_Pointer_Context context);
        void OnHeldMove(WorldInteraction_Pointer_Context context);
        void OnHeldRotate(float wheelDelta, WorldInteraction_Pointer_Context context);
        void OnHeldRotationStarted(WorldInteraction_Pointer_Context context);
        void OnHeldRotationDragged(Vector2 mouseDelta, WorldInteraction_Pointer_Context context);
        void OnHeldRotationClicked(WorldInteraction_Pointer_Context context);
        void OnHeldRotationEnded(WorldInteraction_Pointer_Context context);
        void OnRelease(WorldInteraction_Pointer_Context context);
        void OnCancelHold();
    }
}

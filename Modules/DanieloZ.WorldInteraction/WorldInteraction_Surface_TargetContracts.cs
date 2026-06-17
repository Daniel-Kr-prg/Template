namespace DanieloZ.WorldInteraction
{
    public interface IWorldInteraction_Press_Target
    {
        bool CanPress(WorldInteraction_Pointer_Context context);
        void Press(WorldInteraction_Pointer_Context context);
    }

    public interface IWorldInteraction_Press_LifecycleTarget : IWorldInteraction_Press_Target
    {
        void BeginPress(WorldInteraction_Pointer_Context context);
        void EndPress(WorldInteraction_Pointer_Context context, bool activate);
    }

    public interface IWorldInteraction_Surface_HoverTarget
    {
        bool CanHover(WorldInteraction_Pointer_Context context);
        void OnHandHoverEnter(WorldInteraction_Pointer_Context context);
        void OnHandHoverExit();
    }

    public interface IWorldInteraction_Surface_ActivateTarget
    {
        bool CanActivate(WorldInteraction_Pointer_Context context);
        void Activate(WorldInteraction_Pointer_Context context);
    }
}

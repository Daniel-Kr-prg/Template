using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public readonly struct WorldInteractionContext
    {
        public WorldInteractionContext(Camera camera, Ray ray, RaycastHit hit, Vector2 screenPosition)
        {
            Camera = camera;
            Ray = ray;
            Hit = hit;
            ScreenPosition = screenPosition;
        }

        public Camera Camera { get; }
        public Ray Ray { get; }
        public RaycastHit Hit { get; }
        public Vector2 ScreenPosition { get; }
    }

    public readonly struct WorldDragReleaseContext
    {
        public WorldDragReleaseContext(Camera camera, Vector2 screenPosition)
        {
            Camera = camera;
            ScreenPosition = screenPosition;
            HasScreenPosition = true;
        }

        public Camera Camera { get; }
        public Vector2 ScreenPosition { get; }
        public bool HasScreenPosition { get; }
    }

    public readonly struct WorldSwingContext
    {
        public WorldSwingContext(
            Camera camera,
            Vector2 screenPosition,
            Vector3 center,
            Vector3 direction,
            float cursorSpeed,
            Vector3 force,
            Vector3 torque,
            Collider collider,
            Rigidbody body)
        {
            Camera = camera;
            ScreenPosition = screenPosition;
            Center = center;
            Direction = direction;
            CursorSpeed = cursorSpeed;
            Force = force;
            Torque = torque;
            Collider = collider;
            Body = body;
        }

        public Camera Camera { get; }
        public Vector2 ScreenPosition { get; }
        public Vector3 Center { get; }
        public Vector3 Direction { get; }
        public float CursorSpeed { get; }
        public Vector3 Force { get; }
        public Vector3 Torque { get; }
        public Collider Collider { get; }
        public Rigidbody Body { get; }
    }

    public interface IWorldUsable
    {
        void Use(WorldInteractionContext context);
    }

    public interface IWorldHoverable
    {
        void HoverStart(WorldInteractionContext context);
        void HoverEnd(WorldInteractionContext context);
    }

    public interface IWorldPointerDraggable
    {
        bool BeginPointerDrag(WorldInteractionContext context);
        void UpdatePointerDrag(WorldInteractionContext context);
        void EndPointerDrag(WorldInteractionContext context);
        void CancelPointerDrag();
    }

    public interface IWorldDraggableReleaseHandler
    {
        bool TryReleaseDraggedObject(WorldDraggable draggable, WorldDragReleaseContext context);
    }

    public interface IWorldSwingable
    {
        void Swing(WorldSwingContext context);
    }
}

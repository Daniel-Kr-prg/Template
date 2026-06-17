using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DanieloZ.WorldInteraction
{
    public readonly struct WorldCursorState
    {
        public WorldCursorState(bool visible, CursorLockMode lockMode)
        {
            Visible = visible;
            LockMode = lockMode;
        }

        public bool Visible { get; }
        public CursorLockMode LockMode { get; }
    }

    public static class WorldInteraction_Pointer_CursorUtility
    {
        public static WorldCursorState Capture()
        {
            return new WorldCursorState(Cursor.visible, Cursor.lockState);
        }

        public static void Hide(bool lockCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        }

        public static void Restore(WorldCursorState state)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = state.Visible;
            Cursor.lockState = state.LockMode;
        }

        public static void Restore(WorldCursorState state, Camera camera, Vector3 worldPosition)
        {
            Cursor.lockState = CursorLockMode.None;
            TryWarpToWorldPosition(camera, worldPosition);
            Cursor.visible = state.Visible;
            Cursor.lockState = state.LockMode;
        }

        public static void Restore(WorldCursorState state, Vector2 screenPosition)
        {
            Cursor.lockState = CursorLockMode.None;
            TryWarpToScreenPosition(screenPosition);
            Cursor.visible = state.Visible;
            Cursor.lockState = state.LockMode;
        }

        public static bool TryWarpToWorldPosition(Camera camera, Vector3 worldPosition)
        {
            if (camera == null)
            {
                return false;
            }

            var screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f)
            {
                return false;
            }

            return TryWarpToScreenPosition(screenPosition);
        }

        public static bool TryWarpToScreenPosition(Vector2 unityScreenPosition)
        {
            if (Mouse.current != null)
            {
                Mouse.current.WarpCursorPosition(unityScreenPosition);
                return true;
            }

            return TryWarpNativeCursor(unityScreenPosition);
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int X;
            public int Y;
        }

        private static bool TryWarpNativeCursor(Vector2 unityScreenPosition)
        {
            var window = GetActiveWindow();
            if (window == IntPtr.Zero)
            {
                return false;
            }

            var origin = new Point();
            if (!ClientToScreen(window, ref origin))
            {
                return false;
            }

            var x = origin.X + Mathf.RoundToInt(unityScreenPosition.x);
            var y = origin.Y + Mathf.RoundToInt(Screen.height - unityScreenPosition.y);
            return SetCursorPos(x, y);
        }
#else
        private static bool TryWarpNativeCursor(Vector2 unityScreenPosition)
        {
            return false;
        }
#endif
    }

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

    public interface IWorldInteraction_Press_Usable
    {
        void Use(WorldInteractionContext context);
    }

    public interface IWorldInteraction_Surface_Hoverable
    {
        void HoverStart(WorldInteractionContext context);
        void HoverEnd(WorldInteractionContext context);
    }

    public interface IWorldInteraction_Pointer_Draggable
    {
        bool BeginPointerDrag(WorldInteractionContext context);
        void UpdatePointerDrag(WorldInteractionContext context);
        void EndPointerDrag(WorldInteractionContext context);
        void CancelPointerDrag();
    }

    public interface IWorldInteraction_Drag_ReleaseHandler
    {
        bool TryReleaseDraggedObject(WorldInteraction_Drag_Object draggable, WorldDragReleaseContext context);
    }

    public interface IWorldInteraction_Swing_Target
    {
        void Swing(WorldSwingContext context);
    }
}

using System.Reflection;
using UnityEngine;

namespace DanieloZ.CameraSystem
{
    public sealed class CameraZoomRigController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour zoomRig;

        public MonoBehaviour ZoomRig => zoomRig;

        [ContextMenu("Zoom Min")]
        public void ZoomMin()
        {
            SetZoom(0f);
        }

        [ContextMenu("Zoom Max")]
        public void ZoomMax()
        {
            SetZoom(1f);
        }

        public void SetZoom(float value)
        {
            InvokeZoomRig("SetZoom", value);
        }

        public void AddZoom(float delta)
        {
            InvokeZoomRig("AddZoom", delta);
        }

        public void MovePivot(Vector3 worldDelta)
        {
            if (zoomRig == null)
            {
                return;
            }

            var method = zoomRig.GetType().GetMethod("MovePivot", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(zoomRig, new object[] { worldDelta });
        }

        private void InvokeZoomRig(string methodName, float value)
        {
            if (zoomRig == null)
            {
                return;
            }

            var method = zoomRig.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(zoomRig, new object[] { value });
        }
    }
}

using System;
using System.Reflection;
using Cinemachine;
using UnityEngine;

namespace DanieloZ.CameraSystem
{
    public static class CameraManagerBridge
    {
        private static Type cameraManagerType;
        private static MethodInfo setCameraById;
        private static MethodInfo setCameraWithTargets;
        private static MethodInfo getVirtualCamera;

        public static bool IsAvailable => GetCameraManagerType() != null;

        public static bool SetCamera(string cameraId)
        {
            if (string.IsNullOrWhiteSpace(cameraId) || !TryCacheMethods())
            {
                return false;
            }

            setCameraById?.Invoke(null, new object[] { cameraId });
            return setCameraById != null;
        }

        public static bool SetCamera(string cameraId, Transform followTarget, Transform lookAtTarget = null)
        {
            if (string.IsNullOrWhiteSpace(cameraId) || !TryCacheMethods())
            {
                return false;
            }

            if (setCameraWithTargets == null)
            {
                return SetCamera(cameraId);
            }

            setCameraWithTargets.Invoke(null, new object[] { cameraId, followTarget, lookAtTarget });
            return true;
        }

        public static CinemachineVirtualCamera GetVirtualCamera(string cameraId)
        {
            if (string.IsNullOrWhiteSpace(cameraId) || !TryCacheMethods() || getVirtualCamera == null)
            {
                return null;
            }

            return getVirtualCamera.Invoke(null, new object[] { cameraId }) as CinemachineVirtualCamera;
        }

        private static bool TryCacheMethods()
        {
            var type = GetCameraManagerType();
            if (type == null)
            {
                return false;
            }

            setCameraById ??= type.GetMethod(
                "SetCamera",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);

            setCameraWithTargets ??= type.GetMethod(
                "SetCamera",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(Transform), typeof(Transform) },
                null);

            getVirtualCamera ??= type.GetMethod(
                "GetVirtualCamera",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);

            return true;
        }

        private static Type GetCameraManagerType()
        {
            if (cameraManagerType != null)
            {
                return cameraManagerType;
            }

            cameraManagerType = Type.GetType("CameraManager, Assembly-CSharp");
            if (cameraManagerType != null)
            {
                return cameraManagerType;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                cameraManagerType = assemblies[i].GetType("CameraManager");
                if (cameraManagerType != null)
                {
                    return cameraManagerType;
                }
            }

            return null;
        }
    }
}

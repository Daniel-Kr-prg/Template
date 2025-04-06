using Cinemachine;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum CameraParamKey
{
    Position,
    Rotation,
    Scale,

    TransitionDuration,
    TransitionCurve,

    FollowingObject,
    LookAtObject,
    FollowOffset,
    FollowSmoothing,
    LookaheadTime,
    LookaheadSmoothing,
    CameraDistance,
    DeadZoneWidth,
    DeadZoneHeight,

    PositionOffset,

    HeadBobbingIntensity,
    LookSensitivity,
    RotationSmoothing,

    OrthographicSize,

    OrbitRadius,
    OrbitSpeed,

    // Cinemachine specific
    LensFieldOfView,
    LensNearClipPlane,
    LensFarClipPlane,
    Dutch,
    BlendHint,

    // Advanced (noise, recentering, etc)
    NoiseAmplitude,
    NoiseFrequency,
    RecenterToTargetHeading,
    ClampToScreenEdges,
    ConfineScreenEdges,
    AimSoftZone
}

public abstract class CameraParamsBuilderBase<CameraType>
{
    protected readonly Dictionary<CameraParamKey, Action<CameraType>> _paramSetters = new();

    public void Build(CameraType camera)
    {
        foreach (var kvp in _paramSetters)
        {
            kvp.Value.Invoke(camera);
        }
    }

    public abstract CameraParamsBuilderBase<CameraType> SetTransitionDuration(float duration);
    public abstract CameraParamsBuilderBase<CameraType> SetTransitionCurve(AnimationCurve curve);
    public abstract CameraParamsBuilderBase<CameraType> SetFollowingObject(Transform obj);
    public abstract CameraParamsBuilderBase<CameraType> SetLookAtObject(Transform obj);
    public abstract CameraParamsBuilderBase<CameraType> SetFollowOffset(Vector3 offset);
    public abstract CameraParamsBuilderBase<CameraType> SetFollowSmoothing(float smoothing);
    public abstract CameraParamsBuilderBase<CameraType> SetLookaheadTime(float time);
    public abstract CameraParamsBuilderBase<CameraType> SetLookaheadSmoothing(float smoothing);
    public abstract CameraParamsBuilderBase<CameraType> SetCameraDistance(float distance);
    public abstract CameraParamsBuilderBase<CameraType> SetDeadZoneWidth(float width);
    public abstract CameraParamsBuilderBase<CameraType> SetDeadZoneHeight(float height);

    public abstract CameraParamsBuilderBase<CameraType> SetPositionOffset(Vector3 offset);
    public abstract CameraParamsBuilderBase<CameraType> SetHeadBobbingIntensity(float intensity);
    public abstract CameraParamsBuilderBase<CameraType> SetLookSensitivity(float sensitivity);
    public abstract CameraParamsBuilderBase<CameraType> SetRotationSmoothing(float smoothing);

    public abstract CameraParamsBuilderBase<CameraType> SetOrthographicSize(float size);

    public abstract CameraParamsBuilderBase<CameraType> SetPosition(Vector3 pos);
    public abstract CameraParamsBuilderBase<CameraType> SetRotation(Quaternion rot);
    public abstract CameraParamsBuilderBase<CameraType> SetCameraAngle(Vector3 angle);
    public abstract CameraParamsBuilderBase<CameraType> SetScale(Vector3 scale);

    public abstract CameraParamsBuilderBase<CameraType> SetOrbitRadius(float radius);
    public abstract CameraParamsBuilderBase<CameraType> SetOrbitSpeed(float speed);

    public abstract CameraParamsBuilderBase<CameraType> SetLensFieldOfView(float fov);
    public abstract CameraParamsBuilderBase<CameraType> SetLensNearClipPlane(float near);
    public abstract CameraParamsBuilderBase<CameraType> SetLensFarClipPlane(float far);
    public abstract CameraParamsBuilderBase<CameraType> SetDutch(float dutch);
    public abstract CameraParamsBuilderBase<CameraType> SetBlendHint(Cinemachine.CinemachineVirtualCameraBase.BlendHint hint);

    public abstract CameraParamsBuilderBase<CameraType> SetNoiseAmplitude(float amplitude);
    public abstract CameraParamsBuilderBase<CameraType> SetNoiseFrequency(float frequency);
    public abstract CameraParamsBuilderBase<CameraType> SetRecenterToTargetHeading(bool enabled);
    public abstract CameraParamsBuilderBase<CameraType> SetClampToScreenEdges(bool clamp);
    public abstract CameraParamsBuilderBase<CameraType> SetConfineScreenEdges(bool confine);
    public abstract CameraParamsBuilderBase<CameraType> SetAimSoftZone(float zone);
}

public class CameraParamsBuilder_Cinemachine : CameraParamsBuilderBase<CinemachineVirtualCamera>
{
    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetTransitionDuration(float duration)
    {
        _paramSetters[CameraParamKey.TransitionDuration] = (cam) => { };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetTransitionCurve(AnimationCurve curve)
    {
        _paramSetters[CameraParamKey.TransitionCurve] = (cam) => { };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetFollowingObject(Transform obj)
    {
        _paramSetters[CameraParamKey.FollowingObject] = (cam) => { cam.Follow = obj; };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetLookAtObject(Transform obj)
    {
        _paramSetters[CameraParamKey.LookAtObject] = (cam) => { cam.LookAt = obj; };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetFollowOffset(Vector3 offset)
    {
        _paramSetters[CameraParamKey.FollowOffset] = (cam) =>
        {
            var transposer = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null) transposer.m_TrackedObjectOffset = offset;
        };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetFollowSmoothing(float smoothing)
    {
        _paramSetters[CameraParamKey.FollowSmoothing] = (cam) =>
        {
            var transposer = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null) transposer.m_LookaheadSmoothing = smoothing;
        };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetLookaheadTime(float time)
    {
        _paramSetters[CameraParamKey.LookaheadTime] = (cam) =>
        {
            var transposer = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null) transposer.m_LookaheadTime = time;
        };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetLookaheadSmoothing(float smoothing)
    {
        return SetFollowSmoothing(smoothing);
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetCameraDistance(float distance)
    {
        _paramSetters[CameraParamKey.CameraDistance] = (cam) =>
        {
            var transposer = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null) transposer.m_CameraDistance = distance;
        };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetDeadZoneWidth(float width)
    {
        _paramSetters[CameraParamKey.DeadZoneWidth] = (cam) =>
        {
            var transposer = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null) transposer.m_DeadZoneWidth = width;
        };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetDeadZoneHeight(float height)
    {
        _paramSetters[CameraParamKey.DeadZoneHeight] = (cam) =>
        {
            var transposer = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null) transposer.m_DeadZoneHeight = height;
        };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetPositionOffset(Vector3 offset)
    {
        return SetFollowOffset(offset);
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetHeadBobbingIntensity(float intensity)
    {
        _paramSetters[CameraParamKey.HeadBobbingIntensity] = (cam) => { };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetLookSensitivity(float sensitivity)
    {
        _paramSetters[CameraParamKey.LookSensitivity] = (cam) => { };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetRotationSmoothing(float smoothing)
    {
        _paramSetters[CameraParamKey.RotationSmoothing] = (cam) => { };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetOrthographicSize(float size)
    {
        _paramSetters[CameraParamKey.OrthographicSize] = (cam) =>
        {
            CameraManager.CurrentCamera.orthographicSize = size;
        };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetPosition(Vector3 pos)
    {
        _paramSetters[CameraParamKey.Position] = (cam) => { cam.transform.position = pos; };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetRotation(Quaternion rot)
    {
        _paramSetters[CameraParamKey.Rotation] = (cam) => { cam.transform.rotation = rot; };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetCameraAngle(Vector3 angle)
    {
        _paramSetters[CameraParamKey.Rotation] = (cam) => { cam.transform.rotation = Quaternion.Euler(angle); };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetScale(Vector3 scale)
    {
        _paramSetters[CameraParamKey.Scale] = (cam) => { cam.transform.localScale = scale; };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetOrbitRadius(float radius)
    {
        _paramSetters[CameraParamKey.OrbitRadius] = (cam) => { };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetOrbitSpeed(float speed)
    {
        _paramSetters[CameraParamKey.OrbitSpeed] = (cam) => { };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetLensFieldOfView(float fov)
    {
        _paramSetters[CameraParamKey.LensFieldOfView] = (cam) => { cam.m_Lens.FieldOfView = fov; };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetLensNearClipPlane(float near)
    {
        _paramSetters[CameraParamKey.LensNearClipPlane] = (cam) => { cam.m_Lens.NearClipPlane = near; };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetLensFarClipPlane(float far)
    {
        _paramSetters[CameraParamKey.LensFarClipPlane] = (cam) => { cam.m_Lens.FarClipPlane = far; };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetDutch(float dutch)
    {
        _paramSetters[CameraParamKey.Dutch] = (cam) => { cam.m_Lens.Dutch = dutch; };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetBlendHint(CinemachineVirtualCameraBase.BlendHint hint)
    {
        //_paramSetters[CameraParamKey.BlendHint] = (cam) => { cam.m_Lens.BlendHint = hint; };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetNoiseAmplitude(float amplitude)
    {
        _paramSetters[CameraParamKey.NoiseAmplitude] = (cam) =>
        {
            var noise = cam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise != null) noise.m_AmplitudeGain = amplitude;
        };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetNoiseFrequency(float frequency)
    {
        _paramSetters[CameraParamKey.NoiseFrequency] = (cam) =>
        {
            var noise = cam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise != null) noise.m_FrequencyGain = frequency;
        };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetRecenterToTargetHeading(bool enabled)
    {
        _paramSetters[CameraParamKey.RecenterToTargetHeading] = (cam) =>
        {
            var orbital = cam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
            if (orbital != null)
                orbital.m_RecenterToTargetHeading.m_enabled = enabled;
        };
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetClampToScreenEdges(bool clamp)
    {
        //_paramSetters[CameraParamKey.ClampToScreenEdges] = (cam) =>
        //{
        //    var composer = cam.GetCinemachineComponent<CinemachineComposer>();
        //    if (composer != null) composer.m_ClampToScreenEdges = clamp;
        //};
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetConfineScreenEdges(bool confine)
    {
        //_paramSetters[CameraParamKey.ConfineScreenEdges] = (cam) =>
        //{
        //    var composer = cam.GetCinemachineComponent<CinemachineComposer>();
        //    if (composer != null) composer.m_ConfineScreenEdges = confine;
        //};
        return this;
    }

    public override CameraParamsBuilderBase<CinemachineVirtualCamera> SetAimSoftZone(float zone)
    {
        _paramSetters[CameraParamKey.AimSoftZone] = (cam) =>
        {
            var composer = cam.GetCinemachineComponent<CinemachineComposer>();
            if (composer != null) composer.m_SoftZoneWidth = zone;
        };
        return this;
    }
}

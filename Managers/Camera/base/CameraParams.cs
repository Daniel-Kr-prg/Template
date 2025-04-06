using Cinemachine;
using UnityEngine;

public interface IBaseCameraParams
{
    float TransitionDuration { get; set; }
    AnimationCurve TransitionCurve { get; set; }
}

public interface IFollowCameraParams : IBaseCameraParams
{
    Transform FollowingObject { get; set; }
    Vector3 FollowOffset { get; set; }
    float FollowSmoothing { get; set; }
}

public interface ITopDownCameraParams : IBaseCameraParams
{
    Vector3 CameraAngle { get; set; }
    Vector3 PositionOffset { get; set; }
}

public interface IFirstPersonCameraParams : IBaseCameraParams
{
    float HeadBobbingIntensity { get; set; }
    float LookSensitivity { get; set; }
}

public interface IThirdPersonCameraParams : IBaseCameraParams
{
    Vector3 FollowOffset { get; set; }
    float RotationSmoothing { get; set; }
}

public interface IIsometricCameraParams : IBaseCameraParams
{
    Vector3 CameraRotation { get; set; }
    float OrthographicSize { get; set; }
}

public interface IFixedCameraParams : IBaseCameraParams
{
    Vector3 FixedPosition { get; set; }
    Quaternion FixedRotation { get; set; }
}

public interface IOrbitalCameraParams : IBaseCameraParams
{
    float OrbitRadius { get; set; }
    float OrbitSpeed { get; set; }
}

// Base params

[System.Serializable]
public class BaseCameraParams<CameraType>
{
    protected CameraParamsBuilderBase<CameraType> builder;

    public BaseCameraParams(CameraParamsBuilderBase<CameraType> builder)
    {
        if (builder == null)
        {
            CameraManager.Instance.DebugError("Builder is null. Can't create params");
            return;
        }
        this.builder = builder
            .SetTransitionCurve(AnimationCurve.EaseInOut(0, 0, 1, 1))
            .SetTransitionDuration(0.5f);
    }

    public BaseCameraParams(CameraParamsBuilderBase<CameraType> builder, float transitionDuration, AnimationCurve transitionCurve)
    {
        if (builder == null)
        {
            CameraManager.Instance.DebugError("Builder is null. Can't create params");
            return;
        }
        this.builder = builder
            .SetTransitionCurve(transitionCurve)
            .SetTransitionDuration(transitionDuration);
    }

    public void ApplyTo(CameraType camera)
    {
        builder.Build(camera);
    }

    public CameraParamsBuilderBase<CameraType> GetBuilder()
    {
        return builder;
    }
}

[CreateAssetMenu(fileName = "Base Camera Params", menuName = "Camera Params/Base")]
public class BaseCameraParams_SO : ScriptableObject, IBaseCameraParams
{
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public float TransitionDuration { get => transitionDuration; set => transitionDuration = value; }
    public AnimationCurve TransitionCurve { get => transitionCurve; set => transitionCurve = value; }

    public virtual void ApplyTo<CameraType>(CameraParamsBuilderBase<CameraType> builder, CameraType camera)
    {
        if (builder == null)
        {
            CameraManager.Instance.DebugError("Builder is null. Cannot apply parameters.");
            return;
        }

        builder
            .SetTransitionDuration(transitionDuration)
            .SetTransitionCurve(transitionCurve)
            .Build(camera);
    }
}

// Follow camera

[System.Serializable]
public class FollowCameraParams<CameraType> : BaseCameraParams<CameraType>
{
    public FollowCameraParams(CameraParamsBuilderBase<CameraType> builder)
        : base(builder)
    {
        builder
            .SetFollowOffset(new Vector3(0, 1f, 0))
            .SetFollowSmoothing(0.2f)
            .SetLookaheadTime(0.2f)
            .SetLookaheadSmoothing(0.2f)
            .SetCameraDistance(10f)
            .SetDeadZoneWidth(0.1f)
            .SetDeadZoneHeight(0.1f);
    }

    public FollowCameraParams(CameraParamsBuilderBase<CameraType> builder, Transform target, Vector3 offset, float smoothing, float lookaheadTime, float lookaheadSmoothing, float cameraDistance, float deadZoneWidth, float deadZoneHeight, float duration, AnimationCurve curve)
        : base(builder, duration, curve)
    {
        builder
            .SetFollowingObject(target)
            .SetFollowOffset(offset)
            .SetFollowSmoothing(smoothing)
            .SetLookaheadTime(lookaheadTime)
            .SetLookaheadSmoothing(lookaheadSmoothing)
            .SetCameraDistance(cameraDistance)
            .SetDeadZoneWidth(deadZoneWidth)
            .SetDeadZoneHeight(deadZoneHeight);
    }
}

[CreateAssetMenu(fileName = "Follow Camera Params", menuName = "Camera Params/Follow")]
public class FollowCameraParams_SO : BaseCameraParams_SO, IFollowCameraParams
{
    [SerializeField] private Transform followingObject;
    [SerializeField] private Vector3 followOffset = new Vector3(0, 5, -10);
    [SerializeField] private float followSmoothing = 0.2f;
    [SerializeField] private float lookaheadTime = 0.2f;
    [SerializeField] private float lookaheadSmoothing = 0.2f;
    [SerializeField] private float cameraDistance = 10f;
    [SerializeField] private float deadZoneWidth = 0.1f;
    [SerializeField] private float deadZoneHeight = 0.1f;

    public Transform FollowingObject { get => followingObject; set => followingObject = value; }
    public Vector3 FollowOffset { get => followOffset; set => followOffset = value; }
    public float FollowSmoothing { get => followSmoothing; set => followSmoothing = value; }
    public float LookaheadTime { get => lookaheadTime; set => lookaheadTime = value; }
    public float LookaheadSmoothing { get => lookaheadSmoothing; set => lookaheadSmoothing = value; }
    public float CameraDistance { get => cameraDistance; set => cameraDistance = value; }
    public float DeadZoneWidth { get => deadZoneWidth; set => deadZoneWidth = value; }
    public float DeadZoneHeight { get => deadZoneHeight; set => deadZoneHeight = value; }

    public override void ApplyTo<CameraType>(CameraParamsBuilderBase<CameraType> builder, CameraType cam)
    {
        builder
            .SetFollowingObject(followingObject)
            .SetFollowOffset(followOffset)
            .SetFollowSmoothing(followSmoothing)
            .SetLookaheadTime(lookaheadTime)
            .SetLookaheadSmoothing(lookaheadSmoothing)
            .SetCameraDistance(cameraDistance)
            .SetDeadZoneWidth(deadZoneWidth)
            .SetDeadZoneHeight(deadZoneHeight)
            .SetTransitionDuration(TransitionDuration)
            .SetTransitionCurve(TransitionCurve)
            .Build(cam);
    }
}

// TopDown camera

// TopDown camera

[System.Serializable]
public class TopDownCameraParams<CameraType> : BaseCameraParams<CameraType>
{
    public TopDownCameraParams(CameraParamsBuilderBase<CameraType> builder)
        : base(builder)
    {
        builder
            .SetCameraAngle(new Vector3(90, 0, 0))
            .SetPositionOffset(Vector3.zero);
    }

    public TopDownCameraParams(CameraParamsBuilderBase<CameraType> builder, Vector3 cameraAngle, Vector3 positionOffset, float duration, AnimationCurve curve)
        : base(builder, duration, curve)
    {
        builder
            .SetCameraAngle(cameraAngle)
            .SetPositionOffset(positionOffset);
    }
}

[CreateAssetMenu(fileName = "TopDown Camera Params", menuName = "Camera Params/TopDown")]
public class TopDownCameraParams_SO : BaseCameraParams_SO, ITopDownCameraParams
{
    [SerializeField] private Vector3 cameraAngle = new Vector3(90, 0, 0);
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    public Vector3 CameraAngle { get => cameraAngle; set => cameraAngle = value; }
    public Vector3 PositionOffset { get => positionOffset; set => positionOffset = value; }

    public override void ApplyTo<CameraType>(CameraParamsBuilderBase<CameraType> builder, CameraType cam)
    {
        builder
            .SetCameraAngle(cameraAngle)
            .SetPositionOffset(positionOffset)
            .SetTransitionDuration(TransitionDuration)
            .SetTransitionCurve(TransitionCurve)
            .Build(cam);
    }
}

// FPS camera

[System.Serializable]
public class FirstPersonCameraParams<CameraType> : BaseCameraParams<CameraType>
{
    public FirstPersonCameraParams(CameraParamsBuilderBase<CameraType> builder)
        : base(builder)
    {
        builder
            .SetHeadBobbingIntensity(0.1f)
            .SetLookSensitivity(1.0f);
    }

    public FirstPersonCameraParams(CameraParamsBuilderBase<CameraType> builder, float headBobbingIntensity, float lookSensitivity, float duration, AnimationCurve curve)
        : base(builder, duration, curve)
    {
        builder
            .SetHeadBobbingIntensity(headBobbingIntensity)
            .SetLookSensitivity(lookSensitivity);
    }
}

[CreateAssetMenu(fileName = "First Person Camera Params", menuName = "Camera Params/FirstPerson")]
public class FirstPersonCameraParams_SO : BaseCameraParams_SO, IFirstPersonCameraParams
{
    [SerializeField] private float headBobbingIntensity = 0.1f;
    [SerializeField] private float lookSensitivity = 1.0f;

    public float HeadBobbingIntensity { get => headBobbingIntensity; set => headBobbingIntensity = value; }
    public float LookSensitivity { get => lookSensitivity; set => lookSensitivity = value; }

    public override void ApplyTo<CameraType>(CameraParamsBuilderBase<CameraType> builder, CameraType cam)
    {
        builder
            .SetHeadBobbingIntensity(headBobbingIntensity)
            .SetLookSensitivity(lookSensitivity)
            .SetTransitionDuration(TransitionDuration)
            .SetTransitionCurve(TransitionCurve)
            .Build(cam);
    }
}

// TPS camera
[System.Serializable]
public class ThirdPersonCameraParams<CameraType> : BaseCameraParams<CameraType>
{
    public ThirdPersonCameraParams(CameraParamsBuilderBase<CameraType> builder)
        : base(builder)
    {
        builder
            .SetFollowOffset(new Vector3(0, 3, -6))
            .SetRotationSmoothing(0.1f);
    }

    public ThirdPersonCameraParams(CameraParamsBuilderBase<CameraType> builder, Vector3 offset, float smoothing, float duration, AnimationCurve curve)
        : base(builder, duration, curve)
    {
        builder
            .SetFollowOffset(offset)
            .SetRotationSmoothing(smoothing);
    }
}

[CreateAssetMenu(fileName = "Third Person Camera Params", menuName = "Camera Params/ThirdPerson")]
public class ThirdPersonCameraParams_SO : BaseCameraParams_SO, IThirdPersonCameraParams
{
    [SerializeField] private Vector3 followOffset = new Vector3(0, 3, -6);
    [SerializeField] private float rotationSmoothing = 0.1f;

    public Vector3 FollowOffset { get => followOffset; set => followOffset = value; }
    public float RotationSmoothing { get => rotationSmoothing; set => rotationSmoothing = value; }

    public override void ApplyTo<CameraType>(CameraParamsBuilderBase<CameraType> builder, CameraType cam)
    {
        builder
            .SetFollowOffset(followOffset)
            .SetRotationSmoothing(rotationSmoothing)
            .SetTransitionDuration(TransitionDuration)
            .SetTransitionCurve(TransitionCurve)
            .Build(cam);
    }
}

// Isometric camera

[System.Serializable]
public class IsometricCameraParams<CameraType> : BaseCameraParams<CameraType>
{
    public IsometricCameraParams(CameraParamsBuilderBase<CameraType> builder)
        : base(builder)
    {
        builder
            .SetCameraAngle(new Vector3(45, 45, 0))
            .SetOrthographicSize(10f);
    }

    public IsometricCameraParams(CameraParamsBuilderBase<CameraType> builder, Vector3 rotation, float size, float duration, AnimationCurve curve)
        : base(builder, duration, curve)
    {
        builder
            .SetCameraAngle(rotation)
            .SetOrthographicSize(size);
    }
}

[CreateAssetMenu(fileName = "Isometric Camera Params", menuName = "Camera Params/Isometric")]
public class IsometricCameraParams_SO : BaseCameraParams_SO, IIsometricCameraParams
{
    [SerializeField] private Vector3 cameraRotation = new Vector3(45, 45, 0);
    [SerializeField] private float orthographicSize = 10f;

    public Vector3 CameraRotation { get => cameraRotation; set => cameraRotation = value; }
    public float OrthographicSize { get => orthographicSize; set => orthographicSize = value; }

    public override void ApplyTo<CameraType>(CameraParamsBuilderBase<CameraType> builder, CameraType cam)
    {
        builder
            .SetCameraAngle(cameraRotation)
            .SetOrthographicSize(orthographicSize)
            .SetTransitionDuration(TransitionDuration)
            .SetTransitionCurve(TransitionCurve)
            .Build(cam);
    }
}

// Fixed camera

[System.Serializable]
public class FixedCameraParams<CameraType> : BaseCameraParams<CameraType>
{
    public FixedCameraParams(CameraParamsBuilderBase<CameraType> builder)
        : base(builder)
    {
        builder
            .SetPosition(Vector3.zero)
            .SetRotation(Quaternion.identity);
    }

    public FixedCameraParams(CameraParamsBuilderBase<CameraType> builder, Vector3 position, Quaternion rotation, float duration, AnimationCurve curve)
        : base(builder, duration, curve)
    {
        builder
            .SetPosition(position)
            .SetRotation(rotation);
    }
}

[CreateAssetMenu(fileName = "Fixed Camera Params", menuName = "Camera Params/Fixed")]
public class FixedCameraParams_SO : BaseCameraParams_SO, IFixedCameraParams
{
    [SerializeField] private Vector3 fixedPosition = Vector3.zero;
    [SerializeField] private Quaternion fixedRotation = Quaternion.identity;

    public Vector3 FixedPosition { get => fixedPosition; set => fixedPosition = value; }
    public Quaternion FixedRotation { get => fixedRotation; set => fixedRotation = value; }

    public override void ApplyTo<CameraType>(CameraParamsBuilderBase<CameraType> builder, CameraType cam)
    {
        builder
            .SetPosition(fixedPosition)
            .SetRotation(fixedRotation)
            .SetTransitionDuration(TransitionDuration)
            .SetTransitionCurve(TransitionCurve)
            .Build(cam);
    }
}

// Orbital camera

[System.Serializable]
public class OrbitalCameraParams<CameraType> : BaseCameraParams<CameraType>
{
    public OrbitalCameraParams(CameraParamsBuilderBase<CameraType> builder)
        : base(builder)
    {
        builder
            .SetOrbitRadius(5f)
            .SetOrbitSpeed(20f);
    }

    public OrbitalCameraParams(CameraParamsBuilderBase<CameraType> builder, float radius, float speed, float duration, AnimationCurve curve)
        : base(builder, duration, curve)
    {
        builder
            .SetOrbitRadius(radius)
            .SetOrbitSpeed(speed);
    }
}

[CreateAssetMenu(fileName = "Orbital Camera Params", menuName = "Camera Params/Orbital")]
public class OrbitalCameraParams_SO : BaseCameraParams_SO, IOrbitalCameraParams
{
    [SerializeField] private float orbitRadius = 5f;
    [SerializeField] private float orbitSpeed = 20f;

    public float OrbitRadius { get => orbitRadius; set => orbitRadius = value; }
    public float OrbitSpeed { get => orbitSpeed; set => orbitSpeed = value; }

    public override void ApplyTo<CameraType>(CameraParamsBuilderBase<CameraType> builder, CameraType cam)
    {
        builder
            .SetOrbitRadius(orbitRadius)
            .SetOrbitSpeed(orbitSpeed)
            .SetTransitionDuration(TransitionDuration)
            .SetTransitionCurve(TransitionCurve)
            .Build(cam);
    }
}


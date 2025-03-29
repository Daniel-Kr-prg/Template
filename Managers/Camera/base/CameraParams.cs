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
public class BaseCameraParams : IBaseCameraParams
{
    public float TransitionDuration { get; set; } = 0.5f;
    public AnimationCurve TransitionCurve { get; set; } = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public BaseCameraParams()
    {
        TransitionDuration = 0.5f;
        TransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    public BaseCameraParams(float transitionDuration, AnimationCurve transitionCurve)
    {
        TransitionDuration = transitionDuration;
        TransitionCurve = transitionCurve;
    }
}

[CreateAssetMenu(fileName = "Base Camera Params", menuName = "Camera Params/Base")]
public class BaseCameraParams_SO : ScriptableObject, IBaseCameraParams
{
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public float TransitionDuration { get => transitionDuration; set => transitionDuration = value; }
    public AnimationCurve TransitionCurve { get => transitionCurve; set => transitionCurve = value; }
}

// Follow camera

[System.Serializable]
public class FollowCameraParams : BaseCameraParams, IFollowCameraParams
{
    public Transform FollowingObject { get; set; }
    public Vector3 FollowOffset { get; set; } = new Vector3(0, 1f, 0);
    public float FollowSmoothing { get; set; } = 0.2f;

    public FollowCameraParams() : base()
    {
        FollowOffset = new Vector3(0, 1f, 0);
        FollowSmoothing = 0.2f;
    }

    public FollowCameraParams(Transform followingObject, Vector3 followOffset, float followSmoothing, float transitionDuration, AnimationCurve transitionCurve)
        : base(transitionDuration, transitionCurve)
    {
        FollowingObject = followingObject;
        FollowOffset = followOffset;
        FollowSmoothing = followSmoothing;
    }
}

[CreateAssetMenu(fileName = "Follow Camera Params", menuName = "Camera Params/Follow")]
public class FollowCameraParams_SO : BaseCameraParams_SO, IFollowCameraParams
{
    [SerializeField] private Transform followingObject;
    [SerializeField] private Vector3 followOffset = new Vector3(0, 5, -10);
    [SerializeField] private float followSmoothing = 0.2f;

    public Transform FollowingObject { get => followingObject; set => followingObject = value; }
    public Vector3 FollowOffset { get => followOffset; set => followOffset = value; }
    public float FollowSmoothing { get => followSmoothing; set => followSmoothing = value; }
}

// TopDown camera

[System.Serializable]
public class TopDownCameraParams : BaseCameraParams, ITopDownCameraParams
{
    public Vector3 CameraAngle { get; set; } = new Vector3(90, 0, 0);
    public Vector3 PositionOffset { get; set; } = Vector3.zero;

    public TopDownCameraParams() : base()
    {
        CameraAngle = new Vector3(90, 0, 0);
        PositionOffset = Vector3.zero;
    }

    public TopDownCameraParams(Vector3 cameraAngle, Vector3 positionOffset, float transitionDuration, AnimationCurve transitionCurve)
        : base(transitionDuration, transitionCurve)
    {
        CameraAngle = cameraAngle;
        PositionOffset = positionOffset;
    }
}

[CreateAssetMenu(fileName = "TopDown Camera Params", menuName = "Camera Params/TopDown")]
public class TopDownCameraParams_SO : BaseCameraParams_SO, ITopDownCameraParams
{
    [SerializeField] private Vector3 cameraAngle = new Vector3(90, 0, 0);
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    public Vector3 CameraAngle { get => cameraAngle; set => cameraAngle = value; }
    public Vector3 PositionOffset { get => positionOffset; set => positionOffset = value; }
}

// FPS camera

[System.Serializable]
public class FirstPersonCameraParams : BaseCameraParams, IFirstPersonCameraParams
{
    public float HeadBobbingIntensity { get; set; } = 0.1f;
    public float LookSensitivity { get; set; } = 1.0f;

    public FirstPersonCameraParams() : base()
    {
        HeadBobbingIntensity = 0.1f;
        LookSensitivity = 1.0f;
    }

    public FirstPersonCameraParams(float headBobbingIntensity, float lookSensitivity, float transitionDuration, AnimationCurve transitionCurve)
        : base(transitionDuration, transitionCurve)
    {
        HeadBobbingIntensity = headBobbingIntensity;
        LookSensitivity = lookSensitivity;
    }
}

[CreateAssetMenu(fileName = "First Person Camera Params", menuName = "Camera Params/FirstPerson")]
public class FirstPersonCameraParams_SO : BaseCameraParams_SO, IFirstPersonCameraParams
{
    [SerializeField] private float headBobbingIntensity = 0.1f;
    [SerializeField] private float lookSensitivity = 1.0f;

    public float HeadBobbingIntensity { get => headBobbingIntensity; set => headBobbingIntensity = value; }
    public float LookSensitivity { get => lookSensitivity; set => lookSensitivity = value; }
}

// TPS camera

[System.Serializable]
public class ThirdPersonCameraParams : BaseCameraParams, IThirdPersonCameraParams
{
    public Vector3 FollowOffset { get; set; } = new Vector3(0, 3, -6);
    public float RotationSmoothing { get; set; } = 0.1f;

    public ThirdPersonCameraParams() : base()
    {
        FollowOffset = new Vector3(0, 3, -6);
        RotationSmoothing = 0.1f;
    }

    public ThirdPersonCameraParams(Vector3 followOffset, float rotationSmoothing, float transitionDuration, AnimationCurve transitionCurve)
        : base(transitionDuration, transitionCurve)
    {
        FollowOffset = followOffset;
        RotationSmoothing = rotationSmoothing;
    }
}

[CreateAssetMenu(fileName = "Third Person Camera Params", menuName = "Camera Params/ThirdPerson")]
public class ThirdPersonCameraParams_SO : BaseCameraParams_SO, IThirdPersonCameraParams
{
    [SerializeField] private Vector3 followOffset = new Vector3(0, 3, -6);
    [SerializeField] private float rotationSmoothing = 0.1f;

    public Vector3 FollowOffset { get => followOffset; set => followOffset = value; }
    public float RotationSmoothing { get => rotationSmoothing; set => rotationSmoothing = value; }
}

// Isometric camera

[System.Serializable]
public class IsometricCameraParams : BaseCameraParams, IIsometricCameraParams
{
    public Vector3 CameraRotation { get; set; } = new Vector3(45, 45, 0);
    public float OrthographicSize { get; set; } = 10f;

    public IsometricCameraParams() : base()
    {
        CameraRotation = new Vector3(45, 45, 0);
        OrthographicSize = 10f;
    }

    public IsometricCameraParams(Vector3 cameraRotation, float orthographicSize, float transitionDuration, AnimationCurve transitionCurve)
        : base(transitionDuration, transitionCurve)
    {
        CameraRotation = cameraRotation;
        OrthographicSize = orthographicSize;
    }
}

[CreateAssetMenu(fileName = "Isometric Camera Params", menuName = "Camera Params/Isometric")]
public class IsometricCameraParams_SO : BaseCameraParams_SO, IIsometricCameraParams
{
    [SerializeField] private Vector3 cameraRotation = new Vector3(45, 45, 0);
    [SerializeField] private float orthographicSize = 10f;

    public Vector3 CameraRotation { get => cameraRotation; set => cameraRotation = value; }
    public float OrthographicSize { get => orthographicSize; set => orthographicSize = value; }
}

// Fixed camera

[System.Serializable]
public class FixedCameraParams : BaseCameraParams, IFixedCameraParams
{
    public Vector3 FixedPosition { get; set; } = Vector3.zero;
    public Quaternion FixedRotation { get; set; } = Quaternion.identity;

    public FixedCameraParams() : base()
    {
        FixedPosition = Vector3.zero;
        FixedRotation = Quaternion.identity;
    }

    public FixedCameraParams(Vector3 fixedPosition, Quaternion fixedRotation, float transitionDuration, AnimationCurve transitionCurve)
        : base(transitionDuration, transitionCurve)
    {
        FixedPosition = fixedPosition;
        FixedRotation = fixedRotation;
    }
}

[CreateAssetMenu(fileName = "Fixed Camera Params", menuName = "Camera Params/Fixed")]
public class FixedCameraParams_SO : BaseCameraParams_SO, IFixedCameraParams
{
    [SerializeField] private Vector3 fixedPosition = Vector3.zero;
    [SerializeField] private Quaternion fixedRotation = Quaternion.identity;

    public Vector3 FixedPosition { get => fixedPosition; set => fixedPosition = value; }
    public Quaternion FixedRotation { get => fixedRotation; set => fixedRotation = value; }
}

// Orbital camera

[System.Serializable]
public class OrbitalCameraParams : BaseCameraParams, IOrbitalCameraParams
{
    public float OrbitRadius { get; set; } = 5f;
    public float OrbitSpeed { get; set; } = 20f;

    public OrbitalCameraParams() : base()
    {
        OrbitRadius = 5f;
        OrbitSpeed = 20f;
    }

    public OrbitalCameraParams(float orbitRadius, float orbitSpeed, float transitionDuration, AnimationCurve transitionCurve)
        : base(transitionDuration, transitionCurve)
    {
        OrbitRadius = orbitRadius;
        OrbitSpeed = orbitSpeed;
    }
}

[CreateAssetMenu(fileName = "Orbital Camera Params", menuName = "Camera Params/Orbital")]
public class OrbitalCameraParams_SO : BaseCameraParams_SO, IOrbitalCameraParams
{
    [SerializeField] private float orbitRadius = 5f;
    [SerializeField] private float orbitSpeed = 20f;

    public float OrbitRadius { get => orbitRadius; set => orbitRadius = value; }
    public float OrbitSpeed { get => orbitSpeed; set => orbitSpeed = value; }
}


using Cinemachine;
using UnityEngine;

namespace DanieloZ.CameraSystem
{
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private Transform pivot;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Transform lookTarget;
        [SerializeField] private CinemachineVirtualCamera virtualCamera;

        [Header("Curves")]
        [SerializeField] private WorldCameraBezierCurve cameraCurve;
        [SerializeField] private WorldCameraBezierCurve lookCurve;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5.5f;
        [SerializeField, Min(0f)] private float moveAcceleration = 24f;
        [SerializeField, Min(0f)] private float moveDeceleration = 36f;
        [SerializeField, Min(0f)] private float orbitSpeed = 72f;
        [SerializeField, Min(0f)] private float orbitAcceleration = 720f;
        [SerializeField, Min(0f)] private float orbitDeceleration = 1080f;

        [Header("Middle Mouse Orbit")]
        [SerializeField] private bool enableMiddleMouseOrbit = true;
        [SerializeField, Min(0f)] private float middleMouseOrbitDegreesPerPixel = 0.18f;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float zoomLerpSpeed = 12f;
        [SerializeField, Min(0f)] private float cameraPositionLerpSpeed = 12f;
        [SerializeField, Min(0f)] private float cameraRotationLerpSpeed = 16f;
        [SerializeField, Min(0f)] private float zoomSpeed = 0.12f;
        [SerializeField, Range(0f, 1f)] private float zoom = 0.8f;

        [Header("Lens")]
        [SerializeField] private bool driveFovByZoom;
        [SerializeField] private AnimationCurve fovByZoom = AnimationCurve.Linear(0f, 40f, 1f, 40f);
        [SerializeField, Min(0f)] private float cameraFovLerpSpeed = 12f;

        private Vector2 moveInput;
        private float orbitInput;
        private Vector3 currentMoveVelocity;
        private Vector2 previousOrbitPointerPosition;
        private float currentOrbitVelocity;
        private float currentZoom;
        private bool hasAppliedCameraPose;
        private bool middleMouseOrbitHeld;
        private bool cameraPoseEnabled = true;

        public float Zoom01
        {
            get => zoom;
            set
            {
                zoom = Mathf.Clamp01(value);
                if (!Application.isPlaying)
                {
                    currentZoom = zoom;
                    ApplyRigPose();
                    ApplyVirtualCameraPose(true);
                }
            }
        }

        private void Awake()
        {
            currentZoom = zoom;

            if (virtualCamera != null)
            {
                virtualCamera.Follow = null;
                virtualCamera.LookAt = null;
            }

            ApplyRigPose();
            ApplyVirtualCameraPose(true);
        }

        public void Move(Vector2 input)
        {
            moveInput += input;
        }

        public void Orbit(float direction)
        {
            orbitInput += direction;
        }

        public void Zoom(float wheelDelta)
        {
            if (Mathf.Approximately(wheelDelta, 0f))
            {
                return;
            }

            Zoom01 = zoom - wheelDelta * zoomSpeed;
        }

        public void BeginPointerOrbit(Vector2 screenPosition)
        {
            if (!enableMiddleMouseOrbit)
            {
                return;
            }

            middleMouseOrbitHeld = true;
            previousOrbitPointerPosition = screenPosition;
        }

        public void UpdatePointerOrbit(Vector2 screenPosition)
        {
            if (!enableMiddleMouseOrbit || !middleMouseOrbitHeld)
            {
                return;
            }

            var delta = screenPosition - previousOrbitPointerPosition;
            previousOrbitPointerPosition = screenPosition;

            if (!Mathf.Approximately(delta.x, 0f))
            {
                OrbitImmediate(delta.x * middleMouseOrbitDegreesPerPixel);
            }
        }

        public void EndPointerOrbit()
        {
            middleMouseOrbitHeld = false;
        }

        public void ApplyNow()
        {
            currentZoom = zoom;
            ApplyRigPose();
            ApplyVirtualCameraPose(true);
        }

        public void SetCameraPoseEnabled(bool enabled)
        {
            cameraPoseEnabled = enabled;
        }

        private void LateUpdate()
        {
            TickZoom();
            TickMove();
            TickOrbit();
            ApplyRigPose();
            ApplyVirtualCameraPose(false);
            moveInput = Vector2.zero;
            orbitInput = 0f;
        }

        private void TickZoom()
        {
            currentZoom = zoomLerpSpeed <= 0f || !Application.isPlaying
                ? zoom
                : Mathf.Lerp(currentZoom, zoom, ExponentialFactor(zoomLerpSpeed));
        }

        private void TickMove()
        {
            if (pivot == null)
            {
                return;
            }

            var desiredVelocity = Vector3.zero;
            if (moveInput.sqrMagnitude > 0.0001f)
            {
                var input = Vector2.ClampMagnitude(moveInput, 1f);
                var forward = Vector3.ProjectOnPlane(pivot.forward, Vector3.up).normalized;
                var right = Vector3.ProjectOnPlane(pivot.right, Vector3.up).normalized;
                desiredVelocity = (forward * input.y + right * input.x) * moveSpeed;
            }

            var acceleration = desiredVelocity.sqrMagnitude > currentMoveVelocity.sqrMagnitude
                ? moveAcceleration
                : moveDeceleration;

            currentMoveVelocity = Mathf.Approximately(acceleration, 0f)
                ? desiredVelocity
                : Vector3.MoveTowards(currentMoveVelocity, desiredVelocity, acceleration * Time.deltaTime);

            if (currentMoveVelocity.sqrMagnitude > 0.000001f)
            {
                pivot.position += currentMoveVelocity * Time.deltaTime;
            }
        }

        private void TickOrbit()
        {
            if (pivot == null)
            {
                return;
            }

            var desiredVelocity = Mathf.Clamp(orbitInput, -1f, 1f) * orbitSpeed;
            var acceleration = Mathf.Approximately(desiredVelocity, 0f)
                ? orbitDeceleration
                : orbitAcceleration;

            currentOrbitVelocity = Mathf.Approximately(acceleration, 0f)
                ? desiredVelocity
                : Mathf.MoveTowards(currentOrbitVelocity, desiredVelocity, acceleration * Time.deltaTime);

            if (!Mathf.Approximately(currentOrbitVelocity, 0f))
            {
                OrbitImmediate(currentOrbitVelocity * Time.deltaTime);
            }
        }

        private void OrbitImmediate(float degrees)
        {
            if (pivot == null || Mathf.Approximately(degrees, 0f))
            {
                return;
            }

            pivot.Rotate(Vector3.up, degrees, Space.World);
        }

        private void ApplyRigPose()
        {
            if (cameraTarget != null)
            {
                cameraTarget.position = cameraCurve != null
                    ? cameraCurve.EvaluateWorld(currentZoom)
                    : GetFallbackCameraTargetPosition();
            }

            if (lookTarget != null)
            {
                lookTarget.position = lookCurve != null
                    ? lookCurve.EvaluateWorld(currentZoom)
                    : GetFallbackLookTargetPosition();
            }
        }

        private Vector3 GetFallbackCameraTargetPosition()
        {
            return pivot != null ? pivot.TransformPoint(new Vector3(0f, 15f, -10f)) : new Vector3(0f, 15f, -10f);
        }

        private Vector3 GetFallbackLookTargetPosition()
        {
            return pivot != null ? pivot.position : Vector3.zero;
        }

        private void ApplyVirtualCameraPose(bool immediate)
        {
            if (!cameraPoseEnabled || virtualCamera == null)
            {
                return;
            }

            ApplyVirtualCameraFov(immediate);

            if (cameraTarget == null || lookTarget == null)
            {
                return;
            }

            var direction = lookTarget.position - cameraTarget.position;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var targetPosition = cameraTarget.position;
            var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            if (immediate || !Application.isPlaying || !hasAppliedCameraPose)
            {
                virtualCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
                hasAppliedCameraPose = true;
                return;
            }

            var positionFactor = ExponentialFactor(cameraPositionLerpSpeed);
            var rotationFactor = ExponentialFactor(cameraRotationLerpSpeed);
            virtualCamera.transform.SetPositionAndRotation(
                cameraPositionLerpSpeed <= 0f
                    ? targetPosition
                    : Vector3.Lerp(virtualCamera.transform.position, targetPosition, positionFactor),
                cameraRotationLerpSpeed <= 0f
                    ? targetRotation
                    : Quaternion.Slerp(virtualCamera.transform.rotation, targetRotation, rotationFactor));
        }

        private void ApplyVirtualCameraFov(bool immediate)
        {
            if (!driveFovByZoom)
            {
                return;
            }

            var targetFov = EvaluateCurve(fovByZoom, currentZoom);
            virtualCamera.m_Lens.FieldOfView = immediate || !Application.isPlaying || cameraFovLerpSpeed <= 0f
                ? targetFov
                : Mathf.Lerp(virtualCamera.m_Lens.FieldOfView, targetFov, ExponentialFactor(cameraFovLerpSpeed));
        }

        private static float EvaluateCurve(AnimationCurve curve, float value)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(value) : value;
        }

        private static float ExponentialFactor(float speed)
        {
            return 1f - Mathf.Exp(-speed * Time.deltaTime);
        }
    }
}

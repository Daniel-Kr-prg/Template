using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using OdinShowIf = Sirenix.OdinInspector.ShowIfAttribute;

namespace DanieloZ.CameraSystem
{
    public sealed class TopDownCameraController : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Rig")]
        [FormerlySerializedAs("pivot")]
        [SerializeField] private Transform positionLerpTarget;
        [FoldoutGroup("Rig")]
        [FormerlySerializedAs("rotationLerpPivot")]
        [SerializeField] private Transform cameraRigContainer;
        [FoldoutGroup("Rig")]
        [SerializeField] private Transform cameraTarget;
        [FoldoutGroup("Rig")]
        [SerializeField] private Transform lookTarget;
        [FoldoutGroup("Rig")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;

        [FoldoutGroup("Curves")]
        [SerializeField] private WorldCameraBezierCurve cameraCurve;
        [FoldoutGroup("Curves")]
        [SerializeField] private WorldCameraBezierCurve lookCurve;

        [FoldoutGroup("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5.5f;
        [FoldoutGroup("Movement")]
        [SerializeField, Min(0f)] private float moveAcceleration = 24f;
        [FoldoutGroup("Movement")]
        [SerializeField, Min(0f)] private float moveDeceleration = 36f;
        [FoldoutGroup("Movement")]
        [SerializeField, Min(0f)] private float orbitSpeed = 72f;
        [FoldoutGroup("Movement")]
        [SerializeField, Min(0f)] private float orbitAcceleration = 720f;
        [FoldoutGroup("Movement")]
        [SerializeField, Min(0f)] private float orbitDeceleration = 1080f;

        [FoldoutGroup("Middle Mouse Orbit")]
        [SerializeField] private bool enableMiddleMouseOrbit = true;
        [FoldoutGroup("Middle Mouse Orbit")]
        [OdinShowIf(nameof(enableMiddleMouseOrbit))]
        [SerializeField, Min(0f)] private float middleMouseOrbitDegreesPerPixel = 0.18f;

        [FoldoutGroup("Smoothing")]
        [SerializeField, Min(0f)] private float zoomLerpSpeed = 12f;
        [FoldoutGroup("Smoothing")]
        [SerializeField, Min(0f)] private float cameraPositionLerpSpeed = 12f;
        [FoldoutGroup("Smoothing")]
        [SerializeField, Min(0f)] private float cameraRotationLerpSpeed = 16f;
        [FoldoutGroup("Smoothing")]
        [SerializeField, Min(0f)] private float zoomSpeed = 0.12f;
        [FoldoutGroup("Smoothing")]
        [SerializeField, Range(0f, 1f)] private float zoom = 0.8f;

        [FoldoutGroup("Lens")]
        [SerializeField] private bool driveFovByZoom;
        [FoldoutGroup("Lens")]
        [OdinShowIf(nameof(driveFovByZoom))]
        [SerializeField] private AnimationCurve fovByZoom = AnimationCurve.Linear(0f, 40f, 1f, 40f);
        [FoldoutGroup("Lens")]
        [OdinShowIf(nameof(driveFovByZoom))]
        [SerializeField, Min(0f)] private float cameraFovLerpSpeed = 12f;

        #endregion

        #region Public API

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
        public Transform PositionLerpTarget => positionLerpTarget;

        public void Move(Vector2 input)
        {
            moveInput += input;
        }

        // Pointer-driven pan that reuses the WASD movement pipeline but scales the top speed by speedScale
        // (speedScale 1 == WASD moveSpeed, 2 == 2x moveSpeed, etc.).
        public void MoveByPointer(Vector2 input, float speedScale)
        {
            pointerMoveInput += input;
            pointerMoveSpeedScale = Mathf.Max(0f, speedScale);
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
            ApplyRigSmoothing(true);
            ApplyRigPose();
            ApplyVirtualCameraPose(true);
        }

        public void SetCameraPoseEnabled(bool enabled)
        {
            cameraPoseEnabled = enabled;
        }

        public void SetPivotPose(Vector3 position, Quaternion rotation, float zoom01, bool applyImmediately = true)
        {
            if (positionLerpTarget != null)
            {
                positionLerpTarget.SetPositionAndRotation(position, rotation);
            }

            desiredPivotYaw = rotation.eulerAngles.y;
            Zoom01 = zoom01;

            if (applyImmediately)
            {
                ApplyNow();
            }
        }

        #endregion

        #region Runtime State

        private Vector2 moveInput;
        private Vector2 pointerMoveInput;
        private float pointerMoveSpeedScale = 1f;
        private float orbitInput;
        private Vector3 currentMoveVelocity;
        private Vector2 previousOrbitPointerPosition;
        private float currentOrbitVelocity;
        private float currentZoom;
        private float desiredPivotYaw;
        private bool hasAppliedCameraPose;
        private bool middleMouseOrbitHeld;
        private bool cameraPoseEnabled = true;
        private bool UsesSeparatedRigSmoothing => positionLerpTarget != null
            && cameraRigContainer != null
            && cameraRigContainer != positionLerpTarget;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            currentZoom = zoom;
            desiredPivotYaw = GetInitialPivotYaw();

            if (virtualCamera != null)
            {
                virtualCamera.Follow = null;
                virtualCamera.LookAt = null;
            }

            ApplyRigSmoothing(true);
            ApplyRigPose();
            ApplyVirtualCameraPose(true);
        }

        private void LateUpdate()
        {
            TickZoom();
            TickMove();
            TickOrbit();
            ApplyRigSmoothing(false);
            ApplyRigPose();
            ApplyVirtualCameraPose(false);
            moveInput = Vector2.zero;
            pointerMoveInput = Vector2.zero;
            orbitInput = 0f;
        }

        #endregion

        #region Zoom And Movement

        private void TickZoom()
        {
            currentZoom = zoomLerpSpeed <= 0f || !Application.isPlaying
                ? zoom
                : Mathf.Lerp(currentZoom, zoom, ExponentialFactor(zoomLerpSpeed));
        }

        private void TickMove()
        {
            if (positionLerpTarget == null)
            {
                pointerMoveInput = Vector2.zero;
                return;
            }

            var movementRotation = UsesSeparatedRigSmoothing
                ? Quaternion.Euler(0f, desiredPivotYaw, 0f)
                : positionLerpTarget.rotation;
            var forward = Vector3.ProjectOnPlane(movementRotation * Vector3.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(movementRotation * Vector3.right, Vector3.up).normalized;

            var desiredVelocity = Vector3.zero;
            if (moveInput.sqrMagnitude > 0.0001f)
            {
                var input = Vector2.ClampMagnitude(moveInput, 1f);
                desiredVelocity += (forward * input.y + right * input.x) * moveSpeed;
            }

            if (pointerMoveInput.sqrMagnitude > 0.0001f)
            {
                var input = Vector2.ClampMagnitude(pointerMoveInput, 1f);
                desiredVelocity += (forward * input.y + right * input.x) * moveSpeed * pointerMoveSpeedScale;
            }

            var acceleration = desiredVelocity.sqrMagnitude > currentMoveVelocity.sqrMagnitude
                ? moveAcceleration
                : moveDeceleration;

            currentMoveVelocity = Mathf.Approximately(acceleration, 0f)
                ? desiredVelocity
                : Vector3.MoveTowards(currentMoveVelocity, desiredVelocity, acceleration * Time.deltaTime);

            if (currentMoveVelocity.sqrMagnitude > 0.000001f)
            {
                positionLerpTarget.position += currentMoveVelocity * Time.deltaTime;
            }
        }

        private void TickOrbit()
        {
            if (positionLerpTarget == null)
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
            if (positionLerpTarget == null || Mathf.Approximately(degrees, 0f))
            {
                return;
            }

            if (UsesSeparatedRigSmoothing)
            {
                desiredPivotYaw += degrees;
                return;
            }

            positionLerpTarget.Rotate(Vector3.up, degrees, Space.World);
        }

        #endregion

        #region Rig Pose

        private void ApplyRigSmoothing(bool immediate)
        {
            if (!UsesSeparatedRigSmoothing)
            {
                return;
            }

            var targetPosition = positionLerpTarget.position;
            cameraRigContainer.position = immediate || !Application.isPlaying || cameraPositionLerpSpeed <= 0f
                ? targetPosition
                : Vector3.Lerp(cameraRigContainer.position, targetPosition, ExponentialFactor(cameraPositionLerpSpeed));

            var targetRotation = Quaternion.Euler(0f, desiredPivotYaw, 0f);
            cameraRigContainer.rotation = immediate || !Application.isPlaying || cameraRotationLerpSpeed <= 0f
                ? targetRotation
                : Quaternion.Slerp(cameraRigContainer.rotation, targetRotation, ExponentialFactor(cameraRotationLerpSpeed));
        }

        private void ApplyRigPose()
        {
            var rigTransform = GetRigEvaluationTransform();
            if (cameraTarget != null)
            {
                cameraTarget.position = cameraCurve != null
                    ? EvaluateCurveWorld(cameraCurve, rigTransform, currentZoom)
                    : GetFallbackCameraTargetPosition();
            }

            if (lookTarget != null)
            {
                lookTarget.position = lookCurve != null
                    ? EvaluateCurveWorld(lookCurve, rigTransform, currentZoom)
                    : GetFallbackLookTargetPosition();
            }
        }

        private Vector3 GetFallbackCameraTargetPosition()
        {
            var rigTransform = GetRigEvaluationTransform();
            return rigTransform != null ? rigTransform.TransformPoint(new Vector3(0f, 15f, -10f)) : new Vector3(0f, 15f, -10f);
        }

        private Vector3 GetFallbackLookTargetPosition()
        {
            var rigTransform = GetRigEvaluationTransform();
            return rigTransform != null ? rigTransform.position : Vector3.zero;
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

            if (UsesSeparatedRigSmoothing)
            {
                virtualCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
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

        #endregion

        #region Helpers

        private static float EvaluateCurve(AnimationCurve curve, float value)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(value) : value;
        }

        private Transform GetRigEvaluationTransform()
        {
            if (UsesSeparatedRigSmoothing)
            {
                return cameraRigContainer;
            }

            return positionLerpTarget;
        }

        private static Vector3 EvaluateCurveWorld(WorldCameraBezierCurve curve, Transform rigTransform, float t)
        {
            if (curve == null)
            {
                return rigTransform != null ? rigTransform.position : Vector3.zero;
            }

            if (rigTransform != null && curve.transform.IsChildOf(rigTransform))
            {
                return curve.EvaluateWorld(t);
            }

            return rigTransform != null
                ? rigTransform.TransformPoint(curve.EvaluateLocal(t))
                : curve.EvaluateWorld(t);
        }

        private float GetInitialPivotYaw()
        {
            if (cameraRigContainer != null)
            {
                return cameraRigContainer.eulerAngles.y;
            }

            return positionLerpTarget != null ? positionLerpTarget.eulerAngles.y : transform.eulerAngles.y;
        }

        private static float ExponentialFactor(float speed)
        {
            return 1f - Mathf.Exp(-speed * Time.deltaTime);
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using Cinemachine;
using DanieloZ.InputManagement;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public sealed class WorldCameraZoomRig : MonoBehaviour
    {
        private enum OrbitMode
        {
            RotatePivot = 0,
            CameraAroundLookAt = 1
        }

        private enum CameraReferenceMode
        {
            CameraId = 0,
            VirtualCamera = 1
        }

        private enum MovingTargetMode
        {
            VirtualCameraTransform = 0,
            CustomTransform = 1
        }

        [Serializable]
        private sealed class ZoomCameraEntry
        {
            [Tooltip("How this entry finds the Cinemachine virtual camera to activate and drive.")]
            public CameraReferenceMode cameraReferenceMode = CameraReferenceMode.CameraId;

            [Tooltip("CameraManager id used when Camera Reference Mode is Camera Id.")]
            public string cameraId;

            [Tooltip("Direct Cinemachine virtual camera reference used when Camera Reference Mode is Virtual Camera.")]
            public CinemachineVirtualCamera virtualCamera;

            [Tooltip("Global zoom value where this camera entry starts being active.")]
            [Range(0f, 1f)] public float rangeStart;

            [Tooltip("Global zoom value where this camera entry stops being active.")]
            [Range(0f, 1f)] public float rangeEnd = 1f;

            [Tooltip("Which transform should be moved along the zoom curve.")]
            public MovingTargetMode movingTargetMode = MovingTargetMode.VirtualCameraTransform;

            [Tooltip("Custom transform moved along the zoom curve when Moving Target Mode is Custom Transform.")]
            public Transform movingTransform;

            [Tooltip("World/local Bezier rail used to place the camera for this entry.")]
            public WorldCameraBezierCurve zoomCurve;

            [Tooltip("When enabled, this entry maps its range to 0..1 before evaluating the zoom curve. When disabled, the global zoom value is sent to the curve.")]
            public bool normalizeZoomForCurve;

            [Tooltip("Optional Bezier curve that defines where this camera looks for this entry. If empty, the camera looks at Ground Pivot.")]
            public WorldCameraBezierCurve lookAtCurve;

            [Tooltip("When enabled, the Look At Curve point is treated as an offset from Ground Pivot.")]
            public bool lookAtCurveRelativeToPivot;

            [Tooltip("When enabled, this entry maps its range to 0..1 before evaluating the Look At Curve. When disabled, the global zoom value is sent to the curve.")]
            public bool normalizeLookAtZoomForCurve;

            [Tooltip("If enabled, this entry drives the Cinemachine lens field of view with FOV Curve.")]
            public bool driveFov;

            [Tooltip("FOV by normalized camera-entry zoom. X is 0..1 inside this entry range, Y is field of view.")]
            public AnimationCurve fovCurve = AnimationCurve.Linear(0f, 45f, 1f, 45f);

            [NonSerialized] private CinemachineVirtualCamera cachedCamera;

            public bool Contains(float zoom)
            {
                var min = Mathf.Min(rangeStart, rangeEnd);
                var max = Mathf.Max(rangeStart, rangeEnd);
                return zoom >= min && zoom <= max;
            }

            public float Normalize(float zoom)
            {
                if (Mathf.Approximately(rangeStart, rangeEnd))
                {
                    return 0f;
                }

                return Mathf.InverseLerp(rangeStart, rangeEnd, zoom);
            }

            public void Apply(
                float zoom,
                Transform pivot,
                Transform lookAtTarget,
                bool curveRelativeToPivot,
                float orbitYawDegrees,
                bool immediate,
                float positionLerpSpeed,
                float rotationLerpSpeed,
                float fovLerpSpeed,
                bool avoidCollision,
                LayerMask collisionMask,
                float collisionRadius,
                float collisionPadding)
            {
                var localT = Normalize(zoom);
                var curveT = normalizeZoomForCurve ? localT : zoom;
                var resolvedCamera = ResolveVirtualCamera();

                if (resolvedCamera != null && lookAtTarget != null)
                {
                    resolvedCamera.LookAt = lookAtTarget;
                }

                var target = ResolveMovingTransform(resolvedCamera);

                if (target != null)
                {
                    if (zoomCurve != null)
                    {
                        var targetPosition = GetTargetPosition(curveT, pivot, curveRelativeToPivot);
                        targetPosition = ApplyOrbit(targetPosition, lookAtTarget, orbitYawDegrees);
                        targetPosition = ApplyCollision(targetPosition, lookAtTarget, avoidCollision, collisionMask, collisionRadius, collisionPadding);
                        target.position = immediate || positionLerpSpeed <= 0f || !Application.isPlaying
                            ? targetPosition
                            : Vector3.Lerp(target.position, targetPosition, ExponentialFactor(positionLerpSpeed));
                    }

                    if (target != resolvedCamera?.transform || !HasLookAtDrivenAim(resolvedCamera))
                    {
                        ApplyLookAt(target, lookAtTarget, immediate, rotationLerpSpeed);
                    }
                }

                if (driveFov && resolvedCamera != null)
                {
                    var targetFov = Evaluate(fovCurve, localT);
                    resolvedCamera.m_Lens.FieldOfView = immediate || fovLerpSpeed <= 0f || !Application.isPlaying
                        ? targetFov
                        : Mathf.Lerp(resolvedCamera.m_Lens.FieldOfView, targetFov, ExponentialFactor(fovLerpSpeed));
                }
            }

            public void DrawFocusGizmos(Transform pivot, Color color, float pointRadius)
            {
                var resolvedCamera = ResolveVirtualCamera();
                var target = ResolveMovingTransform(resolvedCamera);

                if (target == null)
                {
                    return;
                }

                Gizmos.color = color;
                if (pivot != null)
                {
                    Gizmos.DrawLine(target.position, pivot.position);
                    Gizmos.DrawWireSphere(pivot.position, pointRadius);
                }
            }

            public bool TryGetCurvePosition(
                float zoom,
                Transform pivot,
                bool curveRelativeToPivot,
                out Vector3 position)
            {
                if (zoomCurve == null)
                {
                    position = Vector3.zero;
                    return false;
                }

                position = GetTargetPosition(GetCurveT(zoom), pivot, curveRelativeToPivot);
                return true;
            }

            public bool TryGetLookAtPosition(float zoom, Transform pivot, out Vector3 position)
            {
                if (lookAtCurve == null)
                {
                    position = pivot != null ? pivot.position : Vector3.zero;
                    return false;
                }

                position = GetCurvePosition(lookAtCurve, GetLookAtCurveT(zoom), pivot, lookAtCurveRelativeToPivot);
                return true;
            }

            public float GetCurveT(float zoom)
            {
                return normalizeZoomForCurve ? Normalize(zoom) : zoom;
            }

            public float GetLookAtCurveT(float zoom)
            {
                return normalizeLookAtZoomForCurve ? Normalize(zoom) : zoom;
            }

            private static float Evaluate(AnimationCurve curve, float value)
            {
                return curve != null && curve.length > 0 ? curve.Evaluate(value) : value;
            }

            private Vector3 GetTargetPosition(float curveT, Transform pivot, bool curveRelativeToPivot)
            {
                if (zoomCurve == null)
                {
                    return pivot != null ? pivot.position : Vector3.zero;
                }

                if (curveRelativeToPivot && pivot != null)
                {
                    return pivot.position + zoomCurve.transform.TransformVector(zoomCurve.EvaluateLocal(curveT));
                }

                return zoomCurve.EvaluateWorld(curveT);
            }

            private static Vector3 GetCurvePosition(
                WorldCameraBezierCurve curve,
                float curveT,
                Transform pivot,
                bool curveRelativeToPivot)
            {
                if (curve == null)
                {
                    return pivot != null ? pivot.position : Vector3.zero;
                }

                if (curveRelativeToPivot && pivot != null)
                {
                    return pivot.position + curve.transform.TransformVector(curve.EvaluateLocal(curveT));
                }

                return curve.EvaluateWorld(curveT);
            }

            private static Vector3 ApplyOrbit(Vector3 position, Transform center, float yawDegrees)
            {
                if (center == null || Mathf.Approximately(yawDegrees, 0f))
                {
                    return position;
                }

                var offset = position - center.position;
                if (offset.sqrMagnitude <= 0.000001f)
                {
                    return position;
                }

                return center.position + Quaternion.AngleAxis(yawDegrees, Vector3.up) * offset;
            }

            private static Vector3 ApplyCollision(
                Vector3 position,
                Transform lookAtTarget,
                bool avoidCollision,
                LayerMask collisionMask,
                float collisionRadius,
                float collisionPadding)
            {
                if (!avoidCollision || lookAtTarget == null)
                {
                    return position;
                }

                var origin = lookAtTarget.position;
                var direction = position - origin;
                var distance = direction.magnitude;
                if (distance <= 0.0001f)
                {
                    return position;
                }

                direction /= distance;
                var query = QueryTriggerInteraction.Ignore;
                RaycastHit hit;
                var hasHit = collisionRadius > 0f
                    ? Physics.SphereCast(origin, collisionRadius, direction, out hit, distance, collisionMask, query)
                    : Physics.Raycast(origin, direction, out hit, distance, collisionMask, query);

                if (!hasHit)
                {
                    return position;
                }

                return hit.point - direction * Mathf.Max(0f, collisionPadding);
            }

            private static void ApplyLookAt(Transform target, Transform pivot, bool immediate, float rotationLerpSpeed)
            {
                if (target == null || pivot == null)
                {
                    return;
                }

                var direction = pivot.position - target.position;
                if (direction.sqrMagnitude <= 0.000001f)
                {
                    return;
                }

                var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                target.rotation = immediate || rotationLerpSpeed <= 0f || !Application.isPlaying
                    ? targetRotation
                    : Quaternion.Slerp(target.rotation, targetRotation, ExponentialFactor(rotationLerpSpeed));
            }

            private static bool HasLookAtDrivenAim(CinemachineVirtualCamera camera)
            {
                var aim = camera != null ? camera.GetCinemachineComponent(CinemachineCore.Stage.Aim) : null;
                return aim is CinemachineHardLookAt || aim is CinemachineComposer;
            }

            private static float ExponentialFactor(float speed)
            {
                return 1f - Mathf.Exp(-speed * Time.deltaTime);
            }

            public CinemachineVirtualCamera ResolveVirtualCamera()
            {
                if (cameraReferenceMode == CameraReferenceMode.VirtualCamera)
                {
                    return virtualCamera;
                }

                if (string.IsNullOrWhiteSpace(cameraId))
                {
                    cachedCamera = null;
                    return null;
                }

                if (cachedCamera == null)
                {
                    cachedCamera = CameraManager.GetVirtualCamera(cameraId);
                }

                return cachedCamera;
            }

            public void ClearCachedCamera()
            {
                cachedCamera = null;
            }

            private Transform ResolveMovingTransform(CinemachineVirtualCamera resolvedCamera)
            {
                return movingTargetMode == MovingTargetMode.CustomTransform
                    ? movingTransform
                    : resolvedCamera != null
                        ? resolvedCamera.transform
                        : null;
            }
        }

        [Header("Scene")]
        [Tooltip("Pivot moved by WASD, edge pan, ground projection and camera bounds.")]
        [SerializeField] private Transform groundPivot;

        [Tooltip("Root translated when the pivot moves. Usually the same transform as Ground Pivot, or a parent root that owns pivot children.")]
        [SerializeField] private Transform movableRoot;

        [Tooltip("Optional radius marker used by pivot constraint volumes.")]
        [SerializeField] private WorldCameraPivotIndicator pivotIndicator;

        [Header("Zoom")]
        [Tooltip("Current global zoom value. Camera entries use this value to choose active range and evaluate curves.")]
        [SerializeField, Range(0f, 1f)] private float zoom;

        [Tooltip("When enabled, Start Zoom overwrites Zoom during Awake.")]
        [SerializeField] private bool useStartZoom;

        [Tooltip("Initial global zoom value applied on Awake when Use Start Zoom is enabled.")]
        [SerializeField, Range(0f, 1f)] private float startZoom;

        [Tooltip("Mouse wheel zoom delta per wheel tick.")]
        [SerializeField, Min(0.001f)] private float zoomStep = 0.05f;

        [Tooltip("Smoothing speed for current zoom catching up to target Zoom.")]
        [SerializeField, Min(0f)] private float zoomLerpSpeed = 12f;

        [Tooltip("Camera entries selected by global zoom ranges.")]
        [SerializeField] private ZoomCameraEntry[] cameras;

        [Header("Camera Follow")]
        [Tooltip("When enabled, each camera Zoom Curve position is treated as an offset from Ground Pivot. When disabled, curve position is literal world space.")]
        [SerializeField] private bool curvePositionRelativeToPivot;

        [Tooltip("Smoothing speed for camera transform movement along the zoom curve.")]
        [SerializeField, Min(0f)] private float cameraPositionLerpSpeed = 12f;

        [Tooltip("Smoothing speed for manual transform LookAt rotation. Cinemachine virtual camera rotation is driven by its LookAt target.")]
        [SerializeField, Min(0f)] private float cameraRotationLerpSpeed = 16f;

        [Tooltip("Smoothing speed for optional FOV changes.")]
        [SerializeField, Min(0f)] private float cameraFovLerpSpeed = 12f;

        [Header("Camera Collision")]
        [Tooltip("When enabled, the camera point is pulled toward LookAt if geometry blocks the line of sight.")]
        [SerializeField] private bool avoidCameraCollision;

        [Tooltip("Layers that can block the camera line of sight.")]
        [SerializeField] private LayerMask cameraCollisionMask = Physics.DefaultRaycastLayers;

        [Tooltip("Sphere radius used for camera collision. Zero uses a raycast.")]
        [SerializeField, Min(0f)] private float cameraCollisionRadius = 0.25f;

        [Tooltip("Small distance kept between the camera and the blocking hit point.")]
        [SerializeField, Min(0f)] private float cameraCollisionPadding = 0.2f;

        [Header("Orbit")]
        [Tooltip("Rotate Pivot changes Ground Pivot yaw. Camera Around Look At rotates the camera position around the current LookAt point.")]
        [SerializeField] private OrbitMode orbitMode = OrbitMode.CameraAroundLookAt;

        [Tooltip("Allow middle mouse horizontal drag to orbit.")]
        [SerializeField] private bool enableMiddleMouseOrbit = true;

        [Tooltip("Orbit degrees added per mouse pixel.")]
        [SerializeField, Min(0f)] private float orbitDegreesPerPixel = 0.18f;

        [Tooltip("Smoothing speed for camera-position orbit yaw.")]
        [SerializeField, Min(0f)] private float orbitLerpSpeed = 16f;

        [Tooltip("Allow Q/E keyboard orbit.")]
        [SerializeField] private bool enableKeyboardOrbit = true;

        [Tooltip("Target orbit speed for Q/E input.")]
        [SerializeField, Min(0f)] private float keyboardOrbitDegreesPerSecond = 80f;

        [Tooltip("Acceleration toward target keyboard orbit speed.")]
        [SerializeField, Min(0f)] private float keyboardOrbitAcceleration = 720f;

        [Tooltip("Deceleration toward zero keyboard orbit speed.")]
        [SerializeField, Min(0f)] private float keyboardOrbitDeceleration = 1080f;

        [Header("Zoom Speed Scaling")]
        [Tooltip("When enabled, movement and orbit speeds are multiplied by Zoom Speed Scale Curve evaluated at current zoom.")]
        [SerializeField] private bool scaleCameraInputSpeedByZoom = true;

        [Tooltip("Input speed multiplier by current global zoom. X is zoom 0..1, Y is speed multiplier.")]
        [SerializeField] private AnimationCurve zoomSpeedScaleCurve = AnimationCurve.Linear(0f, 0.45f, 1f, 1.25f);

        [Header("Map Movement")]
        [Tooltip("Allow WASD to move Ground Pivot.")]
        [SerializeField] private bool enableKeyboardMove = true;

        [Tooltip("Max WASD movement speed before zoom scaling.")]
        [SerializeField, Min(0f)] private float keyboardMoveSpeed = 6f;

        [Tooltip("Acceleration toward desired WASD movement velocity.")]
        [SerializeField, Min(0f)] private float keyboardMoveAcceleration = 24f;

        [Tooltip("Deceleration toward zero movement velocity.")]
        [SerializeField, Min(0f)] private float keyboardMoveDeceleration = 36f;

        [Header("Screen Edge Pan")]
        [Tooltip("Allow cursor near screen edges to move Ground Pivot.")]
        [SerializeField] private bool enableScreenEdgePan = true;

        [Tooltip("Distance from screen edge where edge pan begins.")]
        [SerializeField, Min(1f)] private float edgePanDistance = 72f;

        [Tooltip("Max screen edge pan speed before zoom scaling.")]
        [SerializeField, Min(0f)] private float edgePanMaxSpeed = 6f;

        [Tooltip("Edge pan speed ramp by normalized edge proximity.")]
        [SerializeField] private AnimationCurve edgePanSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Ground Surface")]
        [Tooltip("When enabled, Ground Pivot is projected onto colliders in Ground Mask.")]
        [SerializeField] private bool keepPivotOnGroundSurface;

        [Tooltip("Layers used to find the ground surface under Ground Pivot.")]
        [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;

        [Tooltip("Raycast starts this high above the pivot when searching for ground.")]
        [SerializeField, Min(0f)] private float groundRaycastHeight = 30f;

        [Tooltip("Additional downward distance used by the ground raycast.")]
        [SerializeField, Min(0.001f)] private float groundRaycastDistance = 80f;

        [Tooltip("Vertical offset added after projecting onto ground.")]
        [SerializeField] private float groundHeightOffset;

        [Tooltip("Smoothing speed for pivot height following ground.")]
        [SerializeField, Min(0f)] private float groundFollowSpeed = 24f;

        [Tooltip("Trigger handling used by ground raycasts.")]
        [SerializeField] private QueryTriggerInteraction groundTriggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Pivot Constraints")]
        [Tooltip("Constraint volumes applied to Ground Pivot in list order.")]
        [SerializeField] private List<WorldCameraPivotConstraintVolume> pivotConstraints = new();

        [Header("Debug")]
        [Tooltip("Draw selected gizmos for camera rig debug information.")]
        [SerializeField] private bool drawGizmos = true;

        [Tooltip("Draw focus lines from camera entries to Ground Pivot.")]
        [SerializeField] private bool drawFocusLines = true;

        [Tooltip("Radius for current curve point gizmos.")]
        [SerializeField, Min(0f)] private float zoomPathPointRadius = 0.08f;

        [Tooltip("Gizmo color for the current camera curve point.")]
        [SerializeField] private Color currentCameraCurvePointColor = new(1f, 0.85f, 0.1f, 0.95f);

        [Tooltip("Gizmo color for the current LookAt curve point.")]
        [SerializeField] private Color currentLookAtCurvePointColor = new(0.1f, 0.85f, 1f, 0.95f);

        [Tooltip("Gizmo color for the final LookAt target.")]
        [SerializeField] private Color currentLookAtTargetColor = new(1f, 0.35f, 0.95f, 0.95f);

        [Tooltip("Print periodic input/camera status logs.")]
        [SerializeField] private bool debugInput;

        [Tooltip("Seconds between debug status logs.")]
        [SerializeField, Min(0.1f)] private float debugStatusInterval = 2f;

        private int activeIndex = -1;
        private Vector3 previousMousePosition;
        private Vector3 currentPivotMoveVelocity;
        private float currentKeyboardOrbitVelocity;
        private float currentZoom;
        private float orbitYaw;
        private float currentOrbitYaw;
        private bool middleMouseWasHeld;
        private float nextDebugStatusTime;
        private Transform lookAtTarget;

        public float Zoom => zoom;
        public Transform GroundPivot => groundPivot;

        private void Awake()
        {
            if (groundPivot == null)
            {
                groundPivot = transform;
            }

            if (movableRoot == null)
            {
                movableRoot = groundPivot;
            }

            if (pivotIndicator == null && groundPivot != null)
            {
                pivotIndicator = groundPivot.GetComponent<WorldCameraPivotIndicator>();
            }

            if (useStartZoom)
            {
                zoom = startZoom;
            }

            currentZoom = zoom;
            orbitYaw = 0f;
            currentOrbitYaw = orbitYaw;
            ApplyPivotGroundAndBounds(true);
        }

        private void Start()
        {
            ApplyZoom(true, true);
        }

        public void InitializeGameManager()
        {
            ApplyPivotGroundAndBounds(true);
            ApplyZoom(true, true);
        }

        private void Update()
        {
            DebugStatusTick();
            UpdateZoomInput();
            UpdatePivotMovementInput();
            UpdateOrbitInput();
            ApplyBounds();
        }

        private void LateUpdate()
        {
            ApplyZoom(false, false);
        }

        public void SetZoom(float value)
        {
            var previousZoom = zoom;
            zoom = Mathf.Clamp01(value);
            DebugLog($"SetZoom {previousZoom} -> {zoom}.");
            if (!Application.isPlaying)
            {
                ApplyZoom(false, true);
            }
        }

        public void AddZoom(float delta)
        {
            SetZoom(zoom + delta);
        }

        public void MovePivot(Vector3 worldDelta)
        {
            if (groundPivot == null)
            {
                return;
            }

            TranslatePivot(worldDelta);
            ApplyPivotGroundAndBounds(false);
        }

        private void UpdateZoomInput()
        {
            if (WorldInteractionInputGate.BlocksCameraWheel)
            {
                DebugLog($"Wheel blocked by held object {WorldInteractionInputGate.HeldObject?.name ?? "none"}.");
                return;
            }

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            DebugLog($"Wheel zoom scroll={scroll}, step={zoomStep}, oldZoom={zoom}.");
            AddZoom(Mathf.Sign(scroll) * zoomStep);
        }

        private void UpdateOrbitInput()
        {
            if (groundPivot == null)
            {
                return;
            }

            UpdateKeyboardOrbitInput();
            UpdateMiddleMouseOrbitInput();
        }

        private void UpdateKeyboardOrbitInput()
        {
            if (!enableKeyboardOrbit)
            {
                DecelerateKeyboardOrbit();
                return;
            }

            var input = 0f;
            if (IsActionHeld(InputActionKey.CAMERA_ROTATE_LEFT))
            {
                input -= 1f;
            }

            if (IsActionHeld(InputActionKey.CAMERA_ROTATE_RIGHT))
            {
                input += 1f;
            }

            var targetVelocity = input * keyboardOrbitDegreesPerSecond * GetCameraInputSpeedScale();
            var acceleration = Mathf.Approximately(targetVelocity, 0f) ? keyboardOrbitDeceleration : keyboardOrbitAcceleration;
            currentKeyboardOrbitVelocity = Mathf.MoveTowards(
                currentKeyboardOrbitVelocity,
                targetVelocity,
                acceleration * Time.deltaTime);

            if (!Mathf.Approximately(currentKeyboardOrbitVelocity, 0f))
            {
                DebugLog($"Keyboard orbit velocity={currentKeyboardOrbitVelocity}.");
                ApplyOrbitYawInput(currentKeyboardOrbitVelocity * Time.deltaTime);
            }
        }

        private void DecelerateKeyboardOrbit()
        {
            currentKeyboardOrbitVelocity = Mathf.MoveTowards(
                currentKeyboardOrbitVelocity,
                0f,
                keyboardOrbitDeceleration * Time.deltaTime);

            if (!Mathf.Approximately(currentKeyboardOrbitVelocity, 0f))
            {
                ApplyOrbitYawInput(currentKeyboardOrbitVelocity * Time.deltaTime);
            }
        }

        private void UpdateMiddleMouseOrbitInput()
        {
            if (!enableMiddleMouseOrbit)
            {
                middleMouseWasHeld = false;
                return;
            }

            var middleMouseHeld = IsActionHeld(InputActionKey.MOUSE_MIDDLE);
            if (middleMouseHeld && !middleMouseWasHeld)
            {
                previousMousePosition = Input.mousePosition;
            }

            middleMouseWasHeld = middleMouseHeld;
            if (!middleMouseHeld)
            {
                return;
            }

            var currentMousePosition = Input.mousePosition;
            var delta = currentMousePosition - previousMousePosition;
            previousMousePosition = currentMousePosition;

            if (Mathf.Approximately(delta.x, 0f))
            {
                return;
            }

            DebugLog($"Middle mouse orbit deltaX={delta.x}.");
            ApplyOrbitYawInput(delta.x * orbitDegreesPerPixel * GetCameraInputSpeedScale());
        }

        private void UpdatePivotMovementInput()
        {
            if (groundPivot == null)
            {
                return;
            }

            var desiredVelocity = Vector3.zero;

            if (enableKeyboardMove)
            {
                desiredVelocity += DirectionToWorldVelocity(ReadKeyboardMoveInput(), keyboardMoveSpeed * GetCameraInputSpeedScale());
            }

            if (enableScreenEdgePan)
            {
                desiredVelocity += DirectionToWorldVelocity(ReadScreenEdgePanInput(), edgePanMaxSpeed * GetCameraInputSpeedScale());
            }

            var acceleration = desiredVelocity.sqrMagnitude > currentPivotMoveVelocity.sqrMagnitude
                ? keyboardMoveAcceleration
                : keyboardMoveDeceleration;

            currentPivotMoveVelocity = Vector3.MoveTowards(
                currentPivotMoveVelocity,
                desiredVelocity,
                acceleration * Time.deltaTime);

            if (currentPivotMoveVelocity.sqrMagnitude > 0.000001f)
            {
                DebugLog($"Move pivot velocity={currentPivotMoveVelocity}.");
                MovePivot(currentPivotMoveVelocity * Time.deltaTime);
            }
        }

        private Vector2 ReadKeyboardMoveInput()
        {
            var input = Vector2.zero;

            if (IsActionHeld(InputActionKey.MOVE_LEFT))
            {
                input.x -= 1f;
            }

            if (IsActionHeld(InputActionKey.MOVE_RIGHT))
            {
                input.x += 1f;
            }

            if (IsActionHeld(InputActionKey.MOVE_BACKWARD))
            {
                input.y -= 1f;
            }

            if (IsActionHeld(InputActionKey.MOVE_FORWARD))
            {
                input.y += 1f;
            }

            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private Vector2 ReadScreenEdgePanInput()
        {
            if (edgePanDistance <= 0f || Screen.width <= 0 || Screen.height <= 0)
            {
                return Vector2.zero;
            }

            var mouse = Input.mousePosition;
            if (mouse.x < 0f || mouse.y < 0f || mouse.x > Screen.width || mouse.y > Screen.height)
            {
                return Vector2.zero;
            }

            var input = new Vector2(
                EdgeFactor(Screen.width - mouse.x) - EdgeFactor(mouse.x),
                EdgeFactor(Screen.height - mouse.y) - EdgeFactor(mouse.y));

            if (input.sqrMagnitude <= 0.000001f)
            {
                return Vector2.zero;
            }

            var magnitude = Mathf.Clamp01(input.magnitude);
            var curveValue = EvaluateCurve(edgePanSpeedCurve, magnitude);
            return input.normalized * Mathf.Clamp01(curveValue);
        }

        private float EdgeFactor(float distanceToEdge)
        {
            return Mathf.Clamp01((edgePanDistance - distanceToEdge) / edgePanDistance);
        }

        private Vector3 DirectionToWorldVelocity(Vector2 input, float speed)
        {
            if (input.sqrMagnitude <= 0.000001f || speed <= 0f)
            {
                return Vector3.zero;
            }

            var forward = Vector3.ProjectOnPlane(GetMovementReferenceForward(), Vector3.up).normalized;
            if (forward.sqrMagnitude <= 0.000001f)
            {
                forward = Vector3.forward;
            }

            var right = Vector3.ProjectOnPlane(GetMovementReferenceRight(), Vector3.up).normalized;
            if (right.sqrMagnitude <= 0.000001f)
            {
                right = Vector3.right;
            }

            var direction = right * input.x + forward * input.y;
            return direction.sqrMagnitude > 1f ? direction.normalized * speed : direction * speed;
        }

        private Vector3 GetMovementReferenceForward()
        {
            var camera = CameraManager.CurrentCamera;
            if (camera != null)
            {
                return camera.transform.forward;
            }

            return groundPivot != null ? groundPivot.forward : Vector3.forward;
        }

        private Vector3 GetMovementReferenceRight()
        {
            var camera = CameraManager.CurrentCamera;
            if (camera != null)
            {
                return camera.transform.right;
            }

            return groundPivot != null ? groundPivot.right : Vector3.right;
        }

        private void ApplyOrbitYawInput(float degrees)
        {
            if (Mathf.Approximately(degrees, 0f))
            {
                return;
            }

            if (orbitMode == OrbitMode.CameraAroundLookAt)
            {
                orbitYaw += degrees;
                return;
            }

            if (groundPivot != null)
            {
                groundPivot.Rotate(Vector3.up, degrees, Space.World);
            }
        }

        private float GetCameraInputSpeedScale()
        {
            if (!scaleCameraInputSpeedByZoom)
            {
                return 1f;
            }

            return Mathf.Max(0f, EvaluateCurve(zoomSpeedScaleCurve, currentZoom));
        }

        private static float EvaluateCurve(AnimationCurve curve, float value)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(value) : value;
        }

        private static bool IsActionHeld(InputActionKey key)
        {
            return InputManager.HaveInstance() && InputManager.Instance.IsKeyHeld(key);
        }

        private void ApplyZoom(bool forceSwitch, bool immediate)
        {
            var index = FindCameraIndex(zoom);
            if (index < 0)
            {
                return;
            }

            currentZoom = immediate || zoomLerpSpeed <= 0f || !Application.isPlaying
                ? zoom
                : Mathf.Lerp(currentZoom, zoom, 1f - Mathf.Exp(-zoomLerpSpeed * Time.deltaTime));

            currentOrbitYaw = immediate || orbitLerpSpeed <= 0f || !Application.isPlaying
                ? orbitYaw
                : Mathf.LerpAngle(currentOrbitYaw, orbitYaw, 1f - Mathf.Exp(-orbitLerpSpeed * Time.deltaTime));

            if (forceSwitch || index != activeIndex)
            {
                ActivateCamera(index);
            }

            var entry = cameras[index];
            UpdateLookAtTarget(entry, currentZoom, immediate);
            var lookAtTransform = GetLookAtTransform(entry);
            DebugLog($"ApplyZoom index={index}, zoom={zoom}, currentZoom={currentZoom}, normalized={entry.Normalize(currentZoom)}.");
            entry.Apply(
                currentZoom,
                groundPivot,
                lookAtTransform,
                curvePositionRelativeToPivot,
                orbitMode == OrbitMode.CameraAroundLookAt ? currentOrbitYaw : 0f,
                immediate,
                cameraPositionLerpSpeed,
                cameraRotationLerpSpeed,
                cameraFovLerpSpeed,
                avoidCameraCollision,
                cameraCollisionMask,
                cameraCollisionRadius,
                cameraCollisionPadding);
        }

        private int FindCameraIndex(float zoomValue)
        {
            if (cameras == null)
            {
                return -1;
            }

            for (var i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].Contains(zoomValue))
                {
                    return i;
                }
            }

            return cameras.Length > 0 ? Mathf.Clamp(activeIndex, 0, cameras.Length - 1) : -1;
        }

        private void ActivateCamera(int index)
        {
            if (cameras == null || index < 0 || index >= cameras.Length)
            {
                return;
            }

            var entry = cameras[index];
            entry.ClearCachedCamera();
            var resolvedCamera = entry.ResolveVirtualCamera();
            UpdateLookAtTarget(entry, currentZoom, true);
            var lookAtTransform = GetLookAtTransform(entry);

            if (entry.cameraReferenceMode == CameraReferenceMode.CameraId && !string.IsNullOrWhiteSpace(entry.cameraId))
            {
                DebugLog($"Activate camera by id '{entry.cameraId}'.");
                CameraManager.SetCamera(entry.cameraId, camera =>
                {
                    entry.ClearCachedCamera();
                    if (lookAtTransform != null)
                    {
                        camera.LookAt = lookAtTransform;
                    }
                });
            }
            else if (resolvedCamera != null)
            {
                DebugLog($"Activate virtual camera '{resolvedCamera.name}'.");
                for (var i = 0; i < cameras.Length; i++)
                {
                    var camera = cameras[i]?.ResolveVirtualCamera();
                    if (camera != null)
                    {
                        camera.Priority = i == index ? 100 : 0;
                    }
                }
            }

            activeIndex = index;
        }

        private void ApplyBounds()
        {
            ApplyPivotGroundAndBounds(false);
        }

        private void ApplyPivotGroundAndBounds(bool immediate)
        {
            if (groundPivot == null)
            {
                return;
            }

            var position = groundPivot.position;
            position = ProjectPivotToGround(position, immediate);
            position = ApplyPivotConstraints(position, immediate);
            position = ProjectPivotToGround(position, immediate);
            TranslatePivot(position - groundPivot.position);
        }

        private Transform GetLookAtTransform(ZoomCameraEntry entry)
        {
            if (groundPivot == null)
            {
                return null;
            }

            if (entry == null || entry.lookAtCurve == null)
            {
                return groundPivot;
            }

            EnsureLookAtTarget();
            return lookAtTarget;
        }

        private Vector3 GetLookAtPosition(ZoomCameraEntry entry, float zoomValue)
        {
            if (groundPivot == null)
            {
                return Vector3.zero;
            }

            if (entry != null && entry.TryGetLookAtPosition(zoomValue, groundPivot, out var lookAtPosition))
            {
                return lookAtPosition;
            }

            return groundPivot.position;
        }

        private void UpdateLookAtTarget(ZoomCameraEntry entry, float zoomValue, bool immediate)
        {
            if (groundPivot == null || entry == null || entry.lookAtCurve == null)
            {
                return;
            }

            EnsureLookAtTarget();
            lookAtTarget.position = GetLookAtPosition(entry, zoomValue);
            lookAtTarget.rotation = immediate ? groundPivot.rotation : lookAtTarget.rotation;
        }

        private Vector3 GetCurvePosition(WorldCameraBezierCurve curve, float curveT, bool curveRelativeToPivot)
        {
            if (curve == null)
            {
                return groundPivot != null ? groundPivot.position : Vector3.zero;
            }

            if (curveRelativeToPivot && groundPivot != null)
            {
                return groundPivot.position + curve.transform.TransformVector(curve.EvaluateLocal(curveT));
            }

            return curve.EvaluateWorld(curveT);
        }

        private void EnsureLookAtTarget()
        {
            if (lookAtTarget != null)
            {
                if (groundPivot != null && lookAtTarget.parent != groundPivot)
                {
                    lookAtTarget.SetParent(groundPivot, true);
                }

                return;
            }

            var targetObject = new GameObject("Camera Look At Target");
            lookAtTarget = targetObject.transform;
            if (groundPivot != null)
            {
                lookAtTarget.SetParent(groundPivot, false);
                lookAtTarget.localPosition = Vector3.zero;
                lookAtTarget.localRotation = Quaternion.identity;
                lookAtTarget.localScale = Vector3.one;
            }
        }

        private Vector3 ApplyPivotConstraints(Vector3 position, bool immediate)
        {
            if (pivotConstraints == null || pivotConstraints.Count == 0)
            {
                return position;
            }

            var radius = pivotIndicator != null ? pivotIndicator.Radius : 0f;
            for (var i = 0; i < pivotConstraints.Count; i++)
            {
                if (pivotConstraints[i] != null && pivotConstraints[i].isActiveAndEnabled)
                {
                    position = pivotConstraints[i].Apply(position, radius, immediate);
                }
            }

            return position;
        }

        private Vector3 ProjectPivotToGround(Vector3 position, bool immediate)
        {
            if (!keepPivotOnGroundSurface)
            {
                return position;
            }

            if (!TryGetGroundPosition(position, out var groundPosition))
            {
                return position;
            }

            if (immediate || groundFollowSpeed <= 0f || !Application.isPlaying)
            {
                return groundPosition;
            }

            return Vector3.Lerp(position, groundPosition, 1f - Mathf.Exp(-groundFollowSpeed * Time.deltaTime));
        }

        private bool TryGetGroundPosition(Vector3 position, out Vector3 groundPosition)
        {
            var origin = position + Vector3.up * groundRaycastHeight;
            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out var hit,
                    groundRaycastHeight + groundRaycastDistance,
                    groundMask,
                    groundTriggerInteraction))
            {
                groundPosition = hit.point + Vector3.up * groundHeightOffset;
                return true;
            }

            groundPosition = position;
            return false;
        }

        private void TranslatePivot(Vector3 worldDelta)
        {
            if (worldDelta.sqrMagnitude <= 0.0000001f)
            {
                return;
            }

            if (movableRoot != null)
            {
                movableRoot.position += worldDelta;
            }
            else if (groundPivot != null)
            {
                groundPivot.position += worldDelta;
            }
        }

        private void DebugStatusTick()
        {
            if (!debugInput || Time.unscaledTime < nextDebugStatusTime)
            {
                return;
            }

            nextDebugStatusTime = Time.unscaledTime + debugStatusInterval;
            DebugLog(
                $"Status: inputManager={InputManager.HaveInstance()}, groundPivot={(groundPivot != null ? groundPivot.name : "none")}, " +
                $"camera={CameraManager.CurrentCamera?.name ?? "none"}, zoom={zoom}, activeIndex={activeIndex}, " +
                $"WASD=({IsActionHeld(InputActionKey.MOVE_LEFT)},{IsActionHeld(InputActionKey.MOVE_RIGHT)},{IsActionHeld(InputActionKey.MOVE_FORWARD)},{IsActionHeld(InputActionKey.MOVE_BACKWARD)}), " +
                $"QE=({IsActionHeld(InputActionKey.CAMERA_ROTATE_LEFT)},{IsActionHeld(InputActionKey.CAMERA_ROTATE_RIGHT)}), " +
                $"MMB={IsActionHeld(InputActionKey.MOUSE_MIDDLE)}, wheel={Input.mouseScrollDelta.y}.");
        }

        private void DebugLog(string message)
        {
            if (!debugInput)
            {
                return;
            }

            Debug.Log($"[{nameof(WorldCameraZoomRig)}] {message}", this);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            if (drawFocusLines && cameras != null)
            {
                for (var i = 0; i < cameras.Length; i++)
                {
                    var hue = cameras.Length <= 1 ? 0.32f : i / (float)cameras.Length;
                    var color = Color.HSVToRGB(hue, 0.85f, 1f);
                    color.a = 0.85f;
                    cameras[i]?.DrawFocusGizmos(groundPivot, color, zoomPathPointRadius);
                }
            }

            DrawCurrentCurvePointsGizmos();
        }

        private void DrawCurrentCurvePointsGizmos()
        {
            var gizmoZoom = Application.isPlaying ? currentZoom : zoom;
            var index = FindCameraIndex(gizmoZoom);
            Vector3? cameraCurvePoint = null;
            Vector3? lookAtCurvePoint = null;

            if (cameras != null && index >= 0 && index < cameras.Length)
            {
                var entry = cameras[index];
                if (entry != null && entry.TryGetCurvePosition(gizmoZoom, groundPivot, curvePositionRelativeToPivot, out var position))
                {
                    cameraCurvePoint = position;
                    DrawPointGizmo(position, currentCameraCurvePointColor, zoomPathPointRadius * 1.65f);
                }

                if (entry != null && entry.TryGetLookAtPosition(gizmoZoom, groundPivot, out var lookAtPosition))
                {
                    lookAtCurvePoint = lookAtPosition;
                    DrawPointGizmo(lookAtPosition, currentLookAtCurvePointColor, zoomPathPointRadius * 1.45f);
                }
            }

            var lookAtTargetPosition = index >= 0 && cameras != null && index < cameras.Length
                ? GetLookAtGizmoPosition(cameras[index], gizmoZoom)
                : null;
            if (lookAtTargetPosition.HasValue)
            {
                DrawPointGizmo(lookAtTargetPosition.Value, currentLookAtTargetColor, zoomPathPointRadius * 1.2f);
            }

            if (cameraCurvePoint.HasValue && lookAtCurvePoint.HasValue)
            {
                Gizmos.color = currentLookAtCurvePointColor;
                Gizmos.DrawLine(cameraCurvePoint.Value, lookAtCurvePoint.Value);
            }

            if (cameraCurvePoint.HasValue && lookAtTargetPosition.HasValue)
            {
                Gizmos.color = currentLookAtTargetColor;
                Gizmos.DrawLine(cameraCurvePoint.Value, lookAtTargetPosition.Value);
            }
        }

        private Vector3? GetLookAtGizmoPosition(ZoomCameraEntry entry, float gizmoZoom)
        {
            if (groundPivot == null || entry == null || entry.lookAtCurve == null)
            {
                return null;
            }

            return GetLookAtPosition(entry, gizmoZoom);
        }

        private static void DrawPointGizmo(Vector3 position, Color color, float radius)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(position, radius);
            Gizmos.DrawWireSphere(position, radius * 1.7f);
        }
    }
}

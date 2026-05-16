using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace DanieloZ.WorldInteraction
{
    public class WorldDraggable : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] protected Rigidbody body;
        [SerializeField] private Transform gripRoot;

        [Header("Pickup")]
        [SerializeField, Min(0f)] private float pickupDuration = 0.18f;
        [SerializeField] private bool makeKinematicWhileHeld = true;
        [SerializeField] private bool disableGravityWhileHeld = true;

        [Header("Held Rotation")]
        [SerializeField] private bool snapYawToStepOnPickup = true;
        [SerializeField, Min(1f)] private float yawStep = 90f;
        [SerializeField] private Vector3 heldRotationOffsetEuler;
        [SerializeField] private bool consumeMouseWheel = true;
        [SerializeField, Min(0f)] private float wheelRotationDuration = 0.18f;
        [SerializeField] private Ease wheelRotationEase = Ease.InOutQuart;

        [Header("Drag Wobble")]
        [SerializeField, Min(0f)] private float wobbleStrength = 0.35f;
        [SerializeField, Min(0f)] private float maxWobbleAngle = 14f;
        [SerializeField, Min(0f)] private float wobbleFollowSpeed = 18f;
        [SerializeField, Min(0f)] private float wobbleReturnSpeed = 10f;

        [Header("Release Inertia")]
        [SerializeField] private bool preserveReleaseInertia = true;
        [SerializeField, Min(0f)] private float releaseVelocityMultiplier = 1f;
        [SerializeField, Min(0f)] private float maxReleaseSpeed = 8f;
        [SerializeField, Min(0f)] private float releaseVelocitySmoothing = 24f;
        [SerializeField, Min(0f)] private float maxReleaseSampleAge = 0.12f;

        [Header("Constraints")]
        [SerializeField] private WorldDragConstraintSettings constraints = new();

        [Header("Events")]
        [SerializeField] private UnityEvent onPickedUp;
        [SerializeField] private UnityEvent onReleased;
        [SerializeField] private UnityEvent onReturnedToPhysics;

        private Vector3 localGripPoint;
        private Vector3 desiredGripPosition;
        private Vector3 pickupStartPosition;
        private Vector3 previousGripPosition;
        private Quaternion pickupStartRotation;
        private Quaternion currentWobbleRotation = Quaternion.identity;
        private Quaternion targetWobbleRotation = Quaternion.identity;
        private Vector3 releaseVelocity;
        private Vector3 previousReleaseGripPosition;
        private float lastReleaseVelocitySampleTime;
        private float pickupElapsed;
        private float pickupBlend;
        private float heldYaw;
        private float targetHeldYaw;
        private bool isPickupRunning;
        private bool hasGripPosition;
        private bool hasReleaseVelocitySample;
        private bool interactionsLocked;
        private Tween wheelRotationTween;

        public event Action<WorldDraggable> PickedUp;
        public event Action<WorldDraggable> Released;
        public event Action<WorldDraggable> ReturnedToPhysics;

        public Rigidbody Body => body;
        public Transform GripRoot => gripRoot != null ? gripRoot : transform;
        public bool IsHeld { get; private set; }
        public bool InteractionsLocked => interactionsLocked;
        public bool ConsumesMouseWheel => consumeMouseWheel;
        public WorldDragConstraintSettings Constraints => constraints;
        protected float HeldYaw => heldYaw;
        protected float TargetHeldYaw => targetHeldYaw;
        protected float YawStep => yawStep;
        protected bool IsHeldYawRotationAnimating => wheelRotationTween != null
            && wheelRotationTween.IsActive()
            && !wheelRotationTween.IsComplete();

        protected virtual void Reset()
        {
            body = GetComponent<Rigidbody>();
        }

        protected virtual void Update()
        {
            if (!IsHeld)
            {
                return;
            }

            UpdateWobble();
            UpdatePickup();
            ApplyDragPose();
        }

        public virtual void BeginDrag()
        {
            if (interactionsLocked)
            {
                return;
            }

            if (!WorldInteractionInputGate.TryClaimHeldObject(this))
            {
                return;
            }

            OnBeforeBeginDrag();

            IsHeld = true;
            isPickupRunning = pickupDuration > 0f;
            hasGripPosition = false;
            localGripPoint = transform.InverseTransformPoint(GetGripWorldPosition());
            desiredGripPosition = GetGripWorldPosition();
            constraints?.Begin(desiredGripPosition);
            pickupStartPosition = transform.position;
            pickupStartRotation = transform.rotation;
            pickupElapsed = 0f;
            pickupBlend = isPickupRunning ? 0f : 1f;
            currentWobbleRotation = Quaternion.identity;
            targetWobbleRotation = Quaternion.identity;
            releaseVelocity = Vector3.zero;
            previousReleaseGripPosition = desiredGripPosition;
            lastReleaseVelocitySampleTime = Time.time;
            hasReleaseVelocitySample = false;
            heldYaw = snapYawToStepOnPickup ? SnapToYawStep(transform.eulerAngles.y) : transform.eulerAngles.y;
            targetHeldYaw = heldYaw;
            wheelRotationTween?.Kill();

            if (body != null && makeKinematicWhileHeld)
            {
                body.isKinematic = true;
                body.useGravity = !disableGravityWhileHeld;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            onPickedUp?.Invoke();
            PickedUp?.Invoke(this);
            OnAfterBeginDrag();
        }

        public virtual void MoveDragged(Vector3 gripWorldPosition)
        {
            DragToGripPosition(gripWorldPosition);
        }

        public virtual void DragToGripPosition(Vector3 gripWorldPosition)
        {
            if (!IsHeld)
            {
                return;
            }

            desiredGripPosition = constraints != null ? constraints.Apply(gripWorldPosition) : gripWorldPosition;
            UpdateReleaseVelocity(desiredGripPosition);
            UpdateWobbleTarget(desiredGripPosition);
            UpdateWobble();
            ApplyDragPose();
            OnDragged(desiredGripPosition);
        }

        public virtual void Release()
        {
            if (!IsHeld)
            {
                return;
            }

            IsHeld = false;
            isPickupRunning = false;
            hasGripPosition = false;
            constraints?.Reset();
            targetWobbleRotation = Quaternion.identity;
            wheelRotationTween?.Kill();
            WorldInteractionInputGate.ReleaseHeldObject(this);

            onReleased?.Invoke();
            Released?.Invoke(this);
            OnReleased();
        }

        public virtual void ReleaseToPhysics()
        {
            var inheritedVelocity = GetReleaseInertiaVelocity();
            Release();

            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = inheritedVelocity;
            }

            onReturnedToPhysics?.Invoke();
            ReturnedToPhysics?.Invoke(this);
            OnReturnedToPhysics();
        }

        public bool TryRotateHeldByWheel(float wheelDelta)
        {
            if (!IsHeld || !consumeMouseWheel || Mathf.Approximately(wheelDelta, 0f))
            {
                return false;
            }

            var direction = wheelDelta > 0f ? 1f : -1f;
            RotateHeldByYawStep(direction);
            return true;
        }

        public virtual void RotateHeldByYawStep(float direction)
        {
            RotateHeldByYawStep(direction, 0f, yawStep);
        }

        protected void RotateHeldByYawStep(float direction, float gridBaseYaw, float step)
        {
            if (Mathf.Approximately(direction, 0f))
            {
                return;
            }

            targetHeldYaw = SnapYawToGrid(targetHeldYaw + Mathf.Sign(direction) * step, gridBaseYaw, step);
            AnimateHeldYawToTarget();
        }

        protected void RotateHeldYawByDegrees(float degrees, bool fromCurrentYaw, bool killWheelTween)
        {
            if (Mathf.Approximately(degrees, 0f))
            {
                return;
            }

            targetHeldYaw = (fromCurrentYaw ? heldYaw : targetHeldYaw) + degrees;
            AnimateHeldYawToTarget(killWheelTween);
        }

        protected void AnimateHeldYawTo(float yaw, bool killWheelTween)
        {
            targetHeldYaw = yaw;
            AnimateHeldYawToTarget(killWheelTween);
        }

        protected void SnapHeldYawToGrid(float gridBaseYaw, float step, bool killWheelTween)
        {
            var snappedYaw = SnapYawToGrid(heldYaw, gridBaseYaw, step);
            if (Mathf.Abs(snappedYaw - heldYaw) <= 0.0001f && Mathf.Abs(snappedYaw - targetHeldYaw) <= 0.0001f)
            {
                return;
            }

            if (killWheelTween)
            {
                wheelRotationTween?.Kill();
            }

            heldYaw = snappedYaw;
            targetHeldYaw = snappedYaw;
        }

        protected void SetHeldYawImmediate(float yaw, bool killWheelTween)
        {
            if (killWheelTween)
            {
                wheelRotationTween?.Kill();
            }

            heldYaw = yaw;
            targetHeldYaw = yaw;
        }

        protected float SnapToYawStep(float yaw)
        {
            return SnapYawToGrid(yaw, 0f, yawStep);
        }

        protected static float SnapYawToGrid(float yaw, float gridBaseYaw, float step)
        {
            return step <= 0f ? yaw : gridBaseYaw + Mathf.Round((yaw - gridBaseYaw) / step) * step;
        }

        private void AnimateHeldYawToTarget(bool killExistingTween = true)
        {
            if (killExistingTween)
            {
                wheelRotationTween?.Kill();
            }

            if (wheelRotationDuration <= 0f)
            {
                heldYaw = targetHeldYaw;
                return;
            }

            wheelRotationTween = DOTween.To(
                    () => heldYaw,
                    value => heldYaw = value,
                    targetHeldYaw,
                    wheelRotationDuration)
                .SetEase(wheelRotationEase)
                .OnComplete(() => heldYaw = targetHeldYaw);
        }

        public void SetInteractionsLocked(bool locked)
        {
            if (interactionsLocked == locked)
            {
                return;
            }

            interactionsLocked = locked;
            if (interactionsLocked && IsHeld)
            {
                ReleaseToPhysics();
            }
        }

        protected virtual void OnBeforeBeginDrag()
        {
        }

        protected virtual void OnAfterBeginDrag()
        {
        }

        protected virtual void OnDragged(Vector3 gripWorldPosition)
        {
        }

        protected virtual void OnReleased()
        {
        }

        protected virtual void OnReturnedToPhysics()
        {
        }

        protected virtual Quaternion GetBaseHeldRotation()
        {
            return Quaternion.Euler(heldRotationOffsetEuler.x, heldYaw + heldRotationOffsetEuler.y, heldRotationOffsetEuler.z);
        }

        private void UpdatePickup()
        {
            if (!isPickupRunning)
            {
                return;
            }

            pickupElapsed += Time.deltaTime;
            pickupBlend = Mathf.Clamp01(pickupElapsed / pickupDuration);

            if (pickupBlend >= 1f)
            {
                isPickupRunning = false;
                pickupBlend = 1f;
            }
        }

        private void ApplyDragPose()
        {
            var rootRotation = GetDraggedRootRotation();
            var targetPosition = GetRootPositionForGrip(desiredGripPosition, rootRotation);
            var rootPosition = isPickupRunning
                ? Vector3.Lerp(pickupStartPosition, targetPosition, pickupBlend)
                : targetPosition;

            transform.SetPositionAndRotation(rootPosition, rootRotation);
        }

        private Quaternion GetDraggedRootRotation()
        {
            var targetRotation = GetBaseHeldRotation() * currentWobbleRotation;
            return isPickupRunning
                ? Quaternion.Slerp(pickupStartRotation, targetRotation, pickupBlend)
                : targetRotation;
        }

        private Vector3 GetRootPositionForGrip(Vector3 gripWorldPosition, Quaternion rootRotation)
        {
            return gripWorldPosition - rootRotation * localGripPoint;
        }

        private Vector3 GetGripWorldPosition()
        {
            return GripRoot.position;
        }

        private void UpdateWobbleTarget(Vector3 gripPosition)
        {
            if (!hasGripPosition)
            {
                previousGripPosition = gripPosition;
                hasGripPosition = true;
                return;
            }

            var delta = gripPosition - previousGripPosition;
            previousGripPosition = gripPosition;

            if (Time.deltaTime <= 0f || delta.sqrMagnitude < 0.000001f)
            {
                targetWobbleRotation = Quaternion.identity;
                return;
            }

            var localVelocity = transform.InverseTransformDirection(delta / Time.deltaTime);
            var pitch = Mathf.Clamp(localVelocity.z * wobbleStrength, -maxWobbleAngle, maxWobbleAngle);
            var roll = Mathf.Clamp(-localVelocity.x * wobbleStrength, -maxWobbleAngle, maxWobbleAngle);
            targetWobbleRotation = Quaternion.Euler(pitch, 0f, roll);
        }

        private void UpdateReleaseVelocity(Vector3 gripPosition)
        {
            if (!preserveReleaseInertia)
            {
                return;
            }

            if (!hasReleaseVelocitySample)
            {
                previousReleaseGripPosition = gripPosition;
                lastReleaseVelocitySampleTime = Time.time;
                hasReleaseVelocitySample = true;
                return;
            }

            if (Time.deltaTime <= Mathf.Epsilon)
            {
                return;
            }

            var instantVelocity = (gripPosition - previousReleaseGripPosition) / Time.deltaTime;
            previousReleaseGripPosition = gripPosition;
            lastReleaseVelocitySampleTime = Time.time;

            if (releaseVelocitySmoothing <= 0f)
            {
                releaseVelocity = instantVelocity;
                return;
            }

            var blend = 1f - Mathf.Exp(-releaseVelocitySmoothing * Time.deltaTime);
            releaseVelocity = Vector3.Lerp(releaseVelocity, instantVelocity, blend);
        }

        private Vector3 GetReleaseInertiaVelocity()
        {
            if (!preserveReleaseInertia || !hasReleaseVelocitySample)
            {
                return Vector3.zero;
            }

            if (maxReleaseSampleAge > 0f && Time.time - lastReleaseVelocitySampleTime > maxReleaseSampleAge)
            {
                return Vector3.zero;
            }

            var velocity = releaseVelocity * releaseVelocityMultiplier;
            if (maxReleaseSpeed > 0f)
            {
                velocity = Vector3.ClampMagnitude(velocity, maxReleaseSpeed);
            }

            return velocity;
        }

        private void UpdateWobble()
        {
            var speed = targetWobbleRotation == Quaternion.identity ? wobbleReturnSpeed : wobbleFollowSpeed;
            if (speed <= 0f)
            {
                return;
            }

            currentWobbleRotation = Quaternion.Slerp(
                currentWobbleRotation,
                targetWobbleRotation,
                1f - Mathf.Exp(-speed * Time.deltaTime));
        }

        protected virtual void OnDisable()
        {
            wheelRotationTween?.Kill();
            WorldInteractionInputGate.ReleaseHeldObject(this);
        }
    }
}

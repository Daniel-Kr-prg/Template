using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using OdinHideLabel = Sirenix.OdinInspector.HideLabelAttribute;
using OdinShowIf = Sirenix.OdinInspector.ShowIfAttribute;

namespace DanieloZ.WorldInteraction
{
    public class WorldDraggable : MonoBehaviour
    {
        #region Inspector

        [FoldoutGroup("Physics")]
        [SerializeField] protected Rigidbody body;
        [FoldoutGroup("Physics")]
        [SerializeField] private Transform gripRoot;

        [FoldoutGroup("Pickup")]
        [SerializeField, Min(0f)] private float pickupDuration = 0.18f;
        [FoldoutGroup("Pickup")]
        [SerializeField] private bool makeKinematicWhileHeld = true;
        [FoldoutGroup("Pickup")]
        [SerializeField] private bool disableGravityWhileHeld = true;

        [FoldoutGroup("Held Rotation")]
        [SerializeField] private bool snapYawToStepOnPickup = true;
        [FoldoutGroup("Held Rotation")]
        [SerializeField, Min(1f)] private float yawStep = 90f;
        [FoldoutGroup("Held Rotation")]
        [SerializeField] private Vector3 heldRotationOffsetEuler;
        [FoldoutGroup("Held Rotation")]
        [SerializeField] private bool consumeMouseWheel = true;
        [FoldoutGroup("Held Rotation")]
        [OdinShowIf(nameof(consumeMouseWheel))]
        [SerializeField, Min(0f)] private float wheelRotationDuration = 0.18f;
        [FoldoutGroup("Held Rotation")]
        [OdinShowIf(nameof(consumeMouseWheel))]
        [SerializeField] private Ease wheelRotationEase = Ease.InOutQuart;

        [FoldoutGroup("Drag Wobble")]
        [SerializeField, Min(0f)] private float wobbleStrength = 0.35f;
        [FoldoutGroup("Drag Wobble")]
        [OdinShowIf(nameof(UsesWobble))]
        [SerializeField, Min(0f)] private float maxWobbleAngle = 14f;
        [FoldoutGroup("Drag Wobble")]
        [OdinShowIf(nameof(UsesWobble))]
        [SerializeField, Min(0f)] private float wobbleFollowSpeed = 18f;
        [FoldoutGroup("Drag Wobble")]
        [OdinShowIf(nameof(UsesWobble))]
        [SerializeField, Min(0f)] private float wobbleReturnSpeed = 10f;

        [FoldoutGroup("Release Inertia")]
        [SerializeField] private bool preserveReleaseInertia = true;
        [FoldoutGroup("Release Inertia")]
        [OdinShowIf(nameof(preserveReleaseInertia))]
        [SerializeField, Min(0f)] private float releaseVelocityMultiplier = 1f;
        [FoldoutGroup("Release Inertia")]
        [OdinShowIf(nameof(preserveReleaseInertia))]
        [SerializeField, Min(0f)] private float maxReleaseSpeed = 8f;
        [FoldoutGroup("Release Inertia")]
        [OdinShowIf(nameof(preserveReleaseInertia))]
        [SerializeField, Min(0f)] private float releaseVelocitySmoothing = 24f;
        [FoldoutGroup("Release Inertia")]
        [OdinShowIf(nameof(preserveReleaseInertia))]
        [SerializeField, Min(0f)] private float maxReleaseSampleAge = 0.12f;

        [FoldoutGroup("Constraints")]
        [OdinHideLabel]
        [SerializeField] private WorldDragConstraintSettings constraints = new();

        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onPickedUp;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onReleased;
        [FoldoutGroup("Events")]
        [SerializeField] private UnityEvent onReturnedToPhysics;

        #endregion

        #region Public API

        public event Action<WorldDraggable> PickedUp;
        public event Action<WorldDraggable> Released;
        public event Action<WorldDraggable> ReturnedToPhysics;

        public Rigidbody Body => body;
        public Transform GripRoot => gripRoot != null ? gripRoot : transform;
        public bool IsHeld { get; private set; }
        public bool InteractionsLocked => interactionsLocked;
        public bool ConsumesMouseWheel => consumeMouseWheel;
        public WorldDragConstraintSettings Constraints => constraints;
        protected float HeldYaw => rotationState.HeldYaw;
        protected float TargetHeldYaw => rotationState.TargetHeldYaw;
        protected float YawStep => yawStep;
        protected bool IsHeldYawRotationAnimating => rotationState.IsYawRotationAnimating;

        #endregion

        #region Internal State

        private readonly WorldHeldPoseState poseState = new();
        private readonly WorldHeldRotationState rotationState = new();
        private readonly WorldDragWobbleState wobbleState = new();
        private readonly WorldReleaseInertiaState inertiaState = new();
        private bool interactionsLocked;

        #endregion

        #region Unity Lifecycle

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

            wobbleState.Tick(wobbleFollowSpeed, wobbleReturnSpeed);
            UpdatePickup();
            ApplyDragPose();
        }

        protected virtual void OnDisable()
        {
            rotationState.KillTween();
            WorldInteractionInputGate.ReleaseHeldObject(this);
        }

        #endregion

        #region Drag Flow

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
            var gripPosition = GetGripWorldPosition();
            poseState.Begin(transform, gripPosition, pickupDuration);
            constraints?.Begin(poseState.DesiredGripPosition);
            wobbleState.Begin();
            inertiaState.Begin(poseState.DesiredGripPosition);
            rotationState.Begin(transform.rotation, transform.eulerAngles.y, snapYawToStepOnPickup, yawStep);

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

            var desiredGripPosition = constraints != null ? constraints.Apply(gripWorldPosition) : gripWorldPosition;
            poseState.SetDesiredGripPosition(desiredGripPosition);
            inertiaState.Sample(desiredGripPosition, preserveReleaseInertia, releaseVelocitySmoothing);
            wobbleState.UpdateTarget(transform, desiredGripPosition, wobbleStrength, maxWobbleAngle);
            wobbleState.Tick(wobbleFollowSpeed, wobbleReturnSpeed);
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
            poseState.End();
            constraints?.Reset();
            wobbleState.End();
            rotationState.KillTween();
            WorldInteractionInputGate.ReleaseHeldObject(this);

            onReleased?.Invoke();
            Released?.Invoke(this);
            OnReleased();
        }

        public virtual void ReleaseToPhysics()
        {
            var inheritedVelocity = inertiaState.GetVelocity(
                preserveReleaseInertia,
                releaseVelocityMultiplier,
                maxReleaseSpeed,
                maxReleaseSampleAge);
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

        #endregion

        #region Held Rotation

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

        public void BeginFreeHeldRotation()
        {
            if (!IsHeld)
            {
                return;
            }

            rotationState.BeginFreeRotation(transform.rotation);
        }

        public bool TrySetFreeHeldRotation(Quaternion rootRotation)
        {
            if (!IsHeld)
            {
                return false;
            }

            rotationState.SetFreeRotation(rootRotation);
            return true;
        }

        public void RotateHeldFreely(Vector2 mouseDelta, Camera camera, float degreesPerMouseUnit)
        {
            if (!IsHeld || mouseDelta.sqrMagnitude <= 0.000001f || degreesPerMouseUnit <= 0f)
            {
                return;
            }

            rotationState.RotateFreely(mouseDelta, camera, transform, degreesPerMouseUnit);
        }

        protected void RotateHeldByYawStep(float direction, float gridBaseYaw, float step)
        {
            if (Mathf.Approximately(direction, 0f))
            {
                return;
            }

            rotationState.RotateYawByStep(direction, gridBaseYaw, step, wheelRotationDuration, wheelRotationEase);
        }

        protected void RotateHeldYawByDegrees(float degrees, bool fromCurrentYaw, bool killWheelTween)
        {
            if (Mathf.Approximately(degrees, 0f))
            {
                return;
            }

            rotationState.RotateYawByDegrees(degrees, fromCurrentYaw, killWheelTween, wheelRotationDuration, wheelRotationEase);
        }

        protected void AnimateHeldYawTo(float yaw, bool killWheelTween)
        {
            rotationState.AnimateYawTo(yaw, killWheelTween, wheelRotationDuration, wheelRotationEase);
        }

        protected void SnapHeldYawToGrid(float gridBaseYaw, float step, bool killWheelTween)
        {
            rotationState.SnapYaw(gridBaseYaw, step, killWheelTween);
        }

        protected void SetHeldYawImmediate(float yaw, bool killWheelTween)
        {
            rotationState.SetYawImmediate(yaw, killWheelTween);
        }

        protected float SnapToYawStep(float yaw)
        {
            return WorldHeldRotationState.SnapYawToGrid(yaw, 0f, yawStep);
        }

        protected static float SnapYawToGrid(float yaw, float gridBaseYaw, float step)
        {
            return WorldHeldRotationState.SnapYawToGrid(yaw, gridBaseYaw, step);
        }

        #endregion

        #region State

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

        public void SetGripRoot(Transform value)
        {
            gripRoot = value;
        }

        public void SetSnapYawToStepOnPickup(bool value)
        {
            snapYawToStepOnPickup = value;
        }

        #endregion

        #region Extension Hooks

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
            if (rotationState.UsesFreeRotation)
            {
                return rotationState.FreeRotation;
            }

            return Quaternion.Euler(heldRotationOffsetEuler.x, rotationState.HeldYaw + heldRotationOffsetEuler.y, heldRotationOffsetEuler.z);
        }

        #endregion

        #region Pose

        private void UpdatePickup()
        {
            poseState.Tick(Time.deltaTime);
        }

        private void ApplyDragPose()
        {
            var rootRotation = GetDraggedRootRotation();
            var rootPosition = poseState.GetRootPositionForGrip(rootRotation);

            transform.SetPositionAndRotation(rootPosition, rootRotation);
        }

        private Quaternion GetDraggedRootRotation()
        {
            var targetRotation = GetBaseHeldRotation() * wobbleState.CurrentRotation;
            return poseState.BlendRotation(targetRotation);
        }

        private Vector3 GetGripWorldPosition()
        {
            return GripRoot.position;
        }

        #endregion

        #region Inspector State

        private bool UsesWobble => wobbleStrength > 0f;

        #endregion
    }
}

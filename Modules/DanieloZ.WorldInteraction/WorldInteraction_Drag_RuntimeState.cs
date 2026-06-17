using DG.Tweening;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    internal sealed class WorldInteraction_Drag_HeldPoseState
    {
        public Vector3 LocalGripPoint { get; private set; }
        public Vector3 DesiredGripPosition { get; private set; }
        public bool IsPickupRunning { get; private set; }

        private Vector3 pickupStartPosition;
        private Quaternion pickupStartRotation;
        private float pickupDuration;
        private float pickupElapsed;
        private float pickupBlend = 1f;

        public void Begin(Transform root, Vector3 gripWorldPosition, float duration)
        {
            LocalGripPoint = root.InverseTransformPoint(gripWorldPosition);
            DesiredGripPosition = gripWorldPosition;
            pickupStartPosition = root.position;
            pickupStartRotation = root.rotation;
            pickupDuration = Mathf.Max(0f, duration);
            pickupElapsed = 0f;
            IsPickupRunning = pickupDuration > 0f;
            pickupBlend = IsPickupRunning ? 0f : 1f;
        }

        public void SetDesiredGripPosition(Vector3 gripWorldPosition)
        {
            DesiredGripPosition = gripWorldPosition;
        }

        public void Tick(float deltaTime)
        {
            if (!IsPickupRunning)
            {
                return;
            }

            pickupElapsed += deltaTime;
            pickupBlend = Mathf.Clamp01(pickupElapsed / pickupDuration);
            if (pickupBlend >= 1f)
            {
                IsPickupRunning = false;
                pickupBlend = 1f;
            }
        }

        public void End()
        {
            IsPickupRunning = false;
        }

        public Vector3 GetRootPositionForGrip(Quaternion rootRotation)
        {
            var targetPosition = DesiredGripPosition - rootRotation * LocalGripPoint;
            return IsPickupRunning
                ? Vector3.Lerp(pickupStartPosition, targetPosition, pickupBlend)
                : targetPosition;
        }

        public Quaternion BlendRotation(Quaternion targetRotation)
        {
            return IsPickupRunning
                ? Quaternion.Slerp(pickupStartRotation, targetRotation, pickupBlend)
                : targetRotation;
        }
    }

    internal sealed class WorldInteraction_Drag_HeldRotationState
    {
        private Tween wheelRotationTween;

        public float HeldYaw { get; private set; }
        public float TargetHeldYaw { get; private set; }
        public bool UsesFreeRotation { get; private set; }
        public Quaternion FreeRotation { get; private set; } = Quaternion.identity;
        public bool IsYawRotationAnimating => wheelRotationTween != null
            && wheelRotationTween.IsActive()
            && !wheelRotationTween.IsComplete();

        public void Begin(Quaternion rootRotation, float rootYaw, bool snapYawToStep, float yawStep)
        {
            HeldYaw = snapYawToStep ? SnapYawToGrid(rootYaw, 0f, yawStep) : rootYaw;
            TargetHeldYaw = HeldYaw;
            UsesFreeRotation = false;
            FreeRotation = rootRotation;
            KillTween();
        }

        public void BeginFreeRotation(Quaternion rootRotation)
        {
            KillTween();
            UsesFreeRotation = true;
            FreeRotation = rootRotation;
        }

        public void SetFreeRotation(Quaternion rootRotation)
        {
            KillTween();
            UsesFreeRotation = true;
            FreeRotation = rootRotation;
        }

        public void RotateFreely(Vector2 mouseDelta, Camera camera, Transform root, float degreesPerMouseUnit)
        {
            if (mouseDelta.sqrMagnitude <= 0.000001f || degreesPerMouseUnit <= 0f)
            {
                return;
            }

            if (!UsesFreeRotation)
            {
                BeginFreeRotation(root.rotation);
            }

            var yawAxis = camera != null ? camera.transform.up : Vector3.up;
            var pitchAxis = camera != null ? camera.transform.right : root.right;
            var yaw = Quaternion.AngleAxis(mouseDelta.x * degreesPerMouseUnit, yawAxis);
            var pitch = Quaternion.AngleAxis(-mouseDelta.y * degreesPerMouseUnit, pitchAxis);
            FreeRotation = yaw * pitch * FreeRotation;
        }

        public void RotateYawByStep(float direction, float gridBaseYaw, float step, float duration, Ease ease)
        {
            if (Mathf.Approximately(direction, 0f))
            {
                return;
            }

            TargetHeldYaw = SnapYawToGrid(TargetHeldYaw + Mathf.Sign(direction) * step, gridBaseYaw, step);
            AnimateHeldYawToTarget(duration, ease, true);
        }

        public void RotateYawByDegrees(float degrees, bool fromCurrentYaw, bool killExistingTween, float duration, Ease ease)
        {
            if (Mathf.Approximately(degrees, 0f))
            {
                return;
            }

            TargetHeldYaw = (fromCurrentYaw ? HeldYaw : TargetHeldYaw) + degrees;
            AnimateHeldYawToTarget(duration, ease, killExistingTween);
        }

        public void AnimateYawTo(float yaw, bool killExistingTween, float duration, Ease ease)
        {
            TargetHeldYaw = yaw;
            AnimateHeldYawToTarget(duration, ease, killExistingTween);
        }

        public void SnapYaw(float gridBaseYaw, float step, bool killExistingTween)
        {
            var snappedYaw = SnapYawToGrid(HeldYaw, gridBaseYaw, step);
            if (Mathf.Abs(snappedYaw - HeldYaw) <= 0.0001f && Mathf.Abs(snappedYaw - TargetHeldYaw) <= 0.0001f)
            {
                return;
            }

            if (killExistingTween)
            {
                KillTween();
            }

            HeldYaw = snappedYaw;
            TargetHeldYaw = snappedYaw;
        }

        public void SetYawImmediate(float yaw, bool killExistingTween)
        {
            if (killExistingTween)
            {
                KillTween();
            }

            HeldYaw = yaw;
            TargetHeldYaw = yaw;
        }

        public void KillTween()
        {
            wheelRotationTween?.Kill();
            wheelRotationTween = null;
        }

        public static float SnapYawToGrid(float yaw, float gridBaseYaw, float step)
        {
            return step <= 0f ? yaw : gridBaseYaw + Mathf.Round((yaw - gridBaseYaw) / step) * step;
        }

        private void AnimateHeldYawToTarget(float duration, Ease ease, bool killExistingTween)
        {
            if (killExistingTween)
            {
                KillTween();
            }

            if (duration <= 0f)
            {
                HeldYaw = TargetHeldYaw;
                return;
            }

            wheelRotationTween = DOTween.To(
                    () => HeldYaw,
                    value => HeldYaw = value,
                    TargetHeldYaw,
                    duration)
                .SetEase(ease)
                .OnComplete(() => HeldYaw = TargetHeldYaw);
        }
    }

    internal sealed class WorldInteraction_Drag_WobbleState
    {
        private Vector3 previousGripPosition;
        private Quaternion currentRotation = Quaternion.identity;
        private Quaternion targetRotation = Quaternion.identity;
        private bool hasGripPosition;

        public Quaternion CurrentRotation => currentRotation;

        public void Begin()
        {
            hasGripPosition = false;
            currentRotation = Quaternion.identity;
            targetRotation = Quaternion.identity;
        }

        public void UpdateTarget(Transform root, Vector3 gripPosition, float wobbleStrength, float maxWobbleAngle)
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
                targetRotation = Quaternion.identity;
                return;
            }

            var localVelocity = root.InverseTransformDirection(delta / Time.deltaTime);
            var pitch = Mathf.Clamp(localVelocity.z * wobbleStrength, -maxWobbleAngle, maxWobbleAngle);
            var roll = Mathf.Clamp(-localVelocity.x * wobbleStrength, -maxWobbleAngle, maxWobbleAngle);
            targetRotation = Quaternion.Euler(pitch, 0f, roll);
        }

        public void Tick(float followSpeed, float returnSpeed)
        {
            var speed = targetRotation == Quaternion.identity ? returnSpeed : followSpeed;
            if (speed <= 0f)
            {
                return;
            }

            currentRotation = Quaternion.Slerp(
                currentRotation,
                targetRotation,
                1f - Mathf.Exp(-speed * Time.deltaTime));
        }

        public void End()
        {
            hasGripPosition = false;
            targetRotation = Quaternion.identity;
        }
    }

    internal sealed class WorldInteraction_Drag_ReleaseInertiaState
    {
        private Vector3 releaseVelocity;
        private Vector3 previousGripPosition;
        private float lastSampleTime;
        private bool hasSample;

        public void Begin(Vector3 gripPosition)
        {
            releaseVelocity = Vector3.zero;
            previousGripPosition = gripPosition;
            lastSampleTime = Time.time;
            hasSample = false;
        }

        public void Sample(Vector3 gripPosition, bool enabled, float smoothing)
        {
            if (!enabled)
            {
                return;
            }

            if (!hasSample)
            {
                previousGripPosition = gripPosition;
                lastSampleTime = Time.time;
                hasSample = true;
                return;
            }

            if (Time.deltaTime <= Mathf.Epsilon)
            {
                return;
            }

            var instantVelocity = (gripPosition - previousGripPosition) / Time.deltaTime;
            previousGripPosition = gripPosition;
            lastSampleTime = Time.time;

            if (smoothing <= 0f)
            {
                releaseVelocity = instantVelocity;
                return;
            }

            var blend = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            releaseVelocity = Vector3.Lerp(releaseVelocity, instantVelocity, blend);
        }

        public Vector3 GetVelocity(bool enabled, float multiplier, float maxSpeed, float maxSampleAge)
        {
            if (!enabled || !hasSample)
            {
                return Vector3.zero;
            }

            if (maxSampleAge > 0f && Time.time - lastSampleTime > maxSampleAge)
            {
                return Vector3.zero;
            }

            var velocity = releaseVelocity * multiplier;
            return maxSpeed > 0f ? Vector3.ClampMagnitude(velocity, maxSpeed) : velocity;
        }
    }
}

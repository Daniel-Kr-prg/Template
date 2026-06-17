using System;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    [Serializable]
    public struct WorldInteraction_Slot_ResolutionSettings
    {
        [SerializeField] private LayerMask slotMask;
        [SerializeField] private QueryTriggerInteraction triggerInteraction;
        [SerializeField] private bool useHeldItemCast;
        [SerializeField] private Vector3 heldItemCastDirection;
        [SerializeField, Min(0f)] private float heldItemCastStartOffset;
        [SerializeField, Min(0f)] private float heldItemCastDistance;
        [SerializeField, Min(0f)] private float heldItemCastRadius;
        [SerializeField] private bool useHeldItemOverlap;
        [SerializeField, Min(0f)] private float heldItemOverlapPadding;
        [SerializeField] private bool usePointerRaycast;
        [SerializeField, Min(0f)] private float pointerRayDistance;

        public LayerMask SlotMask => slotMask;
        public QueryTriggerInteraction TriggerInteraction => triggerInteraction;
        public bool UseHeldItemCast => useHeldItemCast;
        public Vector3 HeldItemCastDirection => heldItemCastDirection;
        public float HeldItemCastStartOffset => heldItemCastStartOffset;
        public float HeldItemCastDistance => heldItemCastDistance;
        public float HeldItemCastRadius => heldItemCastRadius;
        public bool UseHeldItemOverlap => useHeldItemOverlap;
        public float HeldItemOverlapPadding => heldItemOverlapPadding;
        public bool UsePointerRaycast => usePointerRaycast;
        public float PointerRayDistance => pointerRayDistance;

        public static WorldInteraction_Slot_ResolutionSettings CreateDefault()
        {
            return new WorldInteraction_Slot_ResolutionSettings
            {
                slotMask = Physics.DefaultRaycastLayers,
                triggerInteraction = QueryTriggerInteraction.Collide,
                useHeldItemCast = true,
                heldItemCastDirection = Vector3.down,
                heldItemCastStartOffset = 0.05f,
                heldItemCastDistance = 6f,
                heldItemCastRadius = 0.12f,
                useHeldItemOverlap = true,
                heldItemOverlapPadding = 0.04f,
                usePointerRaycast = true,
                pointerRayDistance = 500f
            };
        }

        public void Clamp()
        {
            if (heldItemCastDirection.sqrMagnitude <= 0.000001f)
            {
                heldItemCastDirection = Vector3.down;
            }

            heldItemCastStartOffset = Mathf.Max(0f, heldItemCastStartOffset);
            heldItemCastDistance = Mathf.Max(0f, heldItemCastDistance);
            heldItemCastRadius = Mathf.Max(0f, heldItemCastRadius);
            heldItemOverlapPadding = Mathf.Max(0f, heldItemOverlapPadding);
            pointerRayDistance = Mathf.Max(0f, pointerRayDistance);
        }

        public LayerMask ResolveSlotMask(LayerMask fallbackMask)
        {
            return slotMask.value != 0 ? slotMask : fallbackMask;
        }
    }
}

using System;
using UnityEngine;

namespace DanieloZ.WorldInteraction
{
    public static class WorldInteraction_Slot_ResolutionUtility
    {
        public static bool TryFindSlotForHeldItem(
            WorldInteraction_Slot_Item item,
            WorldInteraction_Slot_ResolutionSettings settings,
            WorldInteractionContext pointerContext,
            out WorldInteractionContext slotContext,
            out WorldInteraction_Slot_Base slot)
        {
            return TryFindSlotForHeldItem(
                item,
                settings,
                pointerContext,
                Physics.DefaultRaycastLayers,
                out slotContext,
                out slot);
        }

        public static bool TryFindSlotForHeldItem(
            WorldInteraction_Slot_Item item,
            WorldInteraction_Slot_ResolutionSettings settings,
            WorldInteractionContext pointerContext,
            LayerMask fallbackSlotMask,
            out WorldInteractionContext slotContext,
            out WorldInteraction_Slot_Base slot)
        {
            slotContext = default;
            slot = null;
            if (item == null)
            {
                return false;
            }

            settings.Clamp();
            var mask = settings.ResolveSlotMask(fallbackSlotMask);
            var best = SlotCandidate.None;

            if (settings.UseHeldItemCast)
            {
                FindHeldItemCastCandidates(item, settings, mask, pointerContext, ref best);
            }

            if (settings.UseHeldItemOverlap)
            {
                FindHeldItemOverlapCandidates(item, settings, mask, pointerContext, ref best);
            }

            if (settings.UsePointerRaycast)
            {
                FindPointerCandidates(item, settings, mask, pointerContext, ref best);
            }

            if (best.Slot == null)
            {
                return false;
            }

            slot = best.Slot;
            slotContext = best.Context;
            return true;
        }

        private static void FindHeldItemCastCandidates(
            WorldInteraction_Slot_Item item,
            WorldInteraction_Slot_ResolutionSettings settings,
            LayerMask mask,
            WorldInteractionContext pointerContext,
            ref SlotCandidate best)
        {
            if (!TryGetItemBounds(item, out var bounds))
            {
                return;
            }

            var direction = settings.HeldItemCastDirection.normalized;
            var extent = GetProjectedExtent(bounds.extents, direction);
            var distance = extent * 2f + settings.HeldItemCastStartOffset + settings.HeldItemCastDistance;
            if (distance <= 0f)
            {
                return;
            }

            var origin = bounds.center - direction * (extent + settings.HeldItemCastStartOffset);
            var hits = settings.HeldItemCastRadius > 0f
                ? Physics.SphereCastAll(
                    origin,
                    settings.HeldItemCastRadius,
                    direction,
                    distance,
                    mask,
                    settings.TriggerInteraction)
                : Physics.RaycastAll(
                    origin,
                    direction,
                    distance,
                    mask,
                    settings.TriggerInteraction);
            Array.Sort(hits, static (left, right) => left.distance.CompareTo(right.distance));

            var ray = new Ray(origin, direction);
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (ShouldSkipHit(item, hit.collider))
                {
                    continue;
                }

                var candidate = hit.collider.GetComponentInParent<WorldInteraction_Slot_Base>();
                var context = new WorldInteractionContext(pointerContext.Camera, ray, hit, pointerContext.ScreenPosition);
                ConsiderCandidate(item, candidate, context, hit.distance, CandidatePriority.HeldItemCast, ref best);
            }
        }

        private static void FindHeldItemOverlapCandidates(
            WorldInteraction_Slot_Item item,
            WorldInteraction_Slot_ResolutionSettings settings,
            LayerMask mask,
            WorldInteractionContext pointerContext,
            ref SlotCandidate best)
        {
            if (!TryGetItemBounds(item, out var bounds))
            {
                return;
            }

            var hits = Physics.OverlapBox(
                bounds.center,
                bounds.extents + Vector3.one * settings.HeldItemOverlapPadding,
                Quaternion.identity,
                mask,
                settings.TriggerInteraction);

            var ray = new Ray(bounds.center, settings.HeldItemCastDirection.normalized);
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (ShouldSkipHit(item, hit))
                {
                    continue;
                }

                var candidate = hit.GetComponentInParent<WorldInteraction_Slot_Base>();
                var distance = candidate != null
                    ? (candidate.Anchor.position - bounds.center).sqrMagnitude
                    : float.PositiveInfinity;
                var context = new WorldInteractionContext(pointerContext.Camera, ray, default, pointerContext.ScreenPosition);
                ConsiderCandidate(item, candidate, context, distance, CandidatePriority.HeldItemOverlap, ref best);
            }
        }

        private static void FindPointerCandidates(
            WorldInteraction_Slot_Item item,
            WorldInteraction_Slot_ResolutionSettings settings,
            LayerMask mask,
            WorldInteractionContext pointerContext,
            ref SlotCandidate best)
        {
            if (pointerContext.Camera == null || settings.PointerRayDistance <= 0f)
            {
                return;
            }

            var hits = Physics.RaycastAll(
                pointerContext.Ray,
                settings.PointerRayDistance,
                mask,
                settings.TriggerInteraction);
            Array.Sort(hits, static (left, right) => left.distance.CompareTo(right.distance));

            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (ShouldSkipHit(item, hit.collider))
                {
                    continue;
                }

                var candidate = hit.collider.GetComponentInParent<WorldInteraction_Slot_Base>();
                var context = new WorldInteractionContext(pointerContext.Camera, pointerContext.Ray, hit, pointerContext.ScreenPosition);
                ConsiderCandidate(item, candidate, context, hit.distance, CandidatePriority.PointerRaycast, ref best);
            }
        }

        private static void ConsiderCandidate(
            WorldInteraction_Slot_Item item,
            WorldInteraction_Slot_Base candidate,
            WorldInteractionContext context,
            float distance,
            int priority,
            ref SlotCandidate best)
        {
            if (candidate == null)
            {
                return;
            }

            var canInsert = candidate.CanInsert(item);
            if (best.Slot != null)
            {
                if (!canInsert && best.CanInsert)
                {
                    return;
                }

                if (canInsert == best.CanInsert)
                {
                    if (priority > best.Priority)
                    {
                        return;
                    }

                    if (priority == best.Priority && distance >= best.Distance)
                    {
                        return;
                    }
                }
            }

            best = new SlotCandidate(candidate, context, distance, priority, canInsert);
        }

        private static bool TryGetItemBounds(WorldInteraction_Slot_Item item, out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;
            var colliders = item.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(collider.bounds);
            }

            return hasBounds;
        }

        private static bool ShouldSkipHit(WorldInteraction_Slot_Item item, Collider collider)
        {
            return collider == null || collider.GetComponentInParent<WorldInteraction_Slot_Item>() == item;
        }

        private static float GetProjectedExtent(Vector3 extents, Vector3 direction)
        {
            return Mathf.Abs(direction.x) * extents.x
                + Mathf.Abs(direction.y) * extents.y
                + Mathf.Abs(direction.z) * extents.z;
        }

        private static class CandidatePriority
        {
            public const int HeldItemCast = 0;
            public const int HeldItemOverlap = 1;
            public const int PointerRaycast = 2;
        }

        private readonly struct SlotCandidate
        {
            public SlotCandidate(
                WorldInteraction_Slot_Base slot,
                WorldInteractionContext context,
                float distance,
                int priority,
                bool canInsert)
            {
                Slot = slot;
                Context = context;
                Distance = distance;
                Priority = priority;
                CanInsert = canInsert;
            }

            public WorldInteraction_Slot_Base Slot { get; }
            public WorldInteractionContext Context { get; }
            public float Distance { get; }
            public int Priority { get; }
            public bool CanInsert { get; }

            public static SlotCandidate None => new(null, default, float.PositiveInfinity, int.MaxValue, false);
        }
    }
}

# DanieloZ Play Mode Changes

Editor-only authoring tool for keeping selected component state after leaving Play Mode.

## Workflow

1. Enter Play Mode.
2. Change a serialized component value through the Inspector or a Scene-view gizmo.
3. Click `Save` in the compact component header row, or use the component context menu.
4. Stop Play Mode.
5. The queued snapshot is restored in Edit Mode as one undoable operation. The affected scene is marked dirty but is not saved automatically.

The component header uses these states:

- `Save` is disabled until an Inspector or Gizmo modification is detected.
- `Save` is enabled and yellow when the component changed.
- `Saved` is green when a snapshot is queued.
- `Update` is shown when the component changed again after the snapshot.

Hierarchy indicators use yellow for detected changes and green for queued snapshots.

## Context Menu

Every existing scene component receives:

- `Keep Play Mode Changes`
- `Force Keep Entire Component`
- `Discard Kept Play Mode Changes`

`Force Keep Entire Component` is useful when a custom inspector does not register changes through Unity Undo.

## Supported Scope

- Existing components on GameObjects in saved, loaded scenes.
- Transform and RectTransform serialized state.
- MonoBehaviour and built-in component serialized fields.
- Persistent asset references and references to existing scene objects.
- Additively loaded scenes.
- Prefab instances; restored values remain instance overrides.
- Domain reload during the Play Mode transition through `SessionState` persistence.

## Intentional Limitations

- Runtime-created GameObjects and components are not mapped back into Edit Mode.
- Removed components and GameObjects are not recreated or deleted.
- Reparenting and sibling-order changes are not preserved.
- Static, non-serialized and runtime-only state is not captured.
- References to runtime-created objects are left unchanged in Edit Mode.
- Automatic change detection covers modifications registered through Unity Undo. Gameplay-code changes require `Force Keep Entire Component`.
- Multi-object Inspector editing does not show the header control in the first version.

The module uses only public Unity Editor extension points. It deliberately does not use reflection to inject controls into Unity's built-in `?`, preset and options icon strip.

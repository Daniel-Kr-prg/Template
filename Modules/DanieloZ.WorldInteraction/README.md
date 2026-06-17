# DanieloZ.WorldInteraction

Runtime module for physical world-space interaction: raycast use, hover, draggable objects, swing impulses/callbacks, physical 3D buttons, toggle controls, insertable slot items, slots, sliders, water/recovery helpers and camera constraint helpers.

The module is intentionally generic. Game-specific systems should subscribe to events, call public APIs or provide project-side bridge components instead of adding puzzle rules directly into the interaction controller.

## Main Runtime Components

`WorldInteraction_Runtime_Controller`

- Central mouse/raycast router for drag, use, hover and swing.
- Registers LMB/RMB actions through the template `InputManager`.
- Finds compatible `WorldInteraction_Slot_Base` targets for held `WorldInteraction_Slot_Item` objects through configurable held-item slot resolution.
- Delegates feature flows to internal runtime services so the scene component stays a facade for serialized settings and lifecycle.
- Inspector is grouped by Camera, Raycast, Drag, Use, Hover, Swing and Debug. Projection-specific fields are shown only for the selected projection mode.

`WorldInteraction_Runtime_InputGate`

- Static state for the currently held `WorldInteraction_Drag_Object`.
- Lets drag, camera and UI systems avoid fighting over the same input.
- Does not consume wheel input; mouse wheel is reserved for camera zoom/orbit style controls in the current interaction model.

`WorldInteraction_Pointer_Context` and hand interaction contracts

- Generic hand/input contracts used by project-side or module-side hand routers.
- `IWorldInteraction_Drag_PickupTarget` describes objects that can be picked up, moved, rotated, released or cancelled.
- `IWorldInteraction_Press_Target` / `IWorldInteraction_Press_LifecycleTarget` describe pressable controls with optional press begin/end animation.
- `IWorldInteraction_Surface_HoverTarget` and `IWorldInteraction_Surface_ActivateTarget` cover hand hover and held-object secondary activation.
- The contracts do not know about a concrete game, layer map, save system, placement area or UI flow.

`WorldInteraction_Drag_Object`

- Generic draggable physical object.
- Supports pickup blending, kinematic hold state, RMB/free held rotation APIs, drag wobble, release inertia and optional constraints.
- Keeps pose math, rotation state, wobble and release inertia in internal runtime state helpers while preserving the public component API.
- Legacy wheel-yaw settings remain for old prefabs, but new project-side hand input should route held-object rotation through RMB click/hold instead of wheel.

`WorldInteraction_Drag_ConstraintSettings`

- Serializable constraint block used by `WorldInteraction_Drag_Object`.
- Can lock axes and clamp local ranges.
- Range fields appear only when constraints and the matching axis limit are enabled.

`WorldInteraction_Slot_Item`

- Draggable object with one `ItemId` and optional `ObjectGroup`.
- Can be inserted into a `WorldInteraction_Slot_Base`.
- Snaps to the slot anchor and raises inserted/removed events.
- `World3DPhysicalButton` is a compatibility alias for older prefab naming.

`WorldInteraction_Slot_Base`

- Physical raycast/hover slot for `WorldInteraction_Slot_Item`.
- Supports selectable validation by accepted item ids or object group, strict id matching, lock-on-match, active state, accepted/rejected hover indicators, UnityEvents and C# events.
- Cursor hover while holding a slot item shows whether insertion is valid. Actual insertion happens on release or through `TryInsert`.

`WorldInteraction_Config_ObjectGroups`

- Project-level group id list for slot/item configuration.
- Edited through `Project Settings > World Interaction`.
- Runtime matching uses the serialized strings on `WorldInteraction_Slot_Item.objectGroup` and `WorldInteraction_Slot_Base.acceptedObjectGroup` when the slot validation mode is `ObjectGroup`; an empty slot group means any item group.

`WorldInteraction_Slot_ResolutionSettings` and `WorldInteraction_Slot_ResolutionUtility`

- Shared held item to slot resolver used by the module controller and project-side hand bridges.
- Supports circular cast from the held item bounds with configurable direction, radius, start offset and distance.
- Supports overlap and pointer raycast fallbacks.
- Candidate selection prefers slots that can insert the item, then held item cast, then overlap, then pointer raycast.

`WorldInteraction_Outline_Hover` and `WorldInteraction_Outline_State`

- Shared layer-switching outline helper for hover visuals.
- Stores original renderer layers while visible and restores them on hide/disable.
- `WorldInteraction_Outline_State` can be used by project-side components that need outline behavior without adding a separate component.

`WorldInteraction_Press_ButtonBase`

- Shared base for simple active/interactable world buttons.
- Exposes `ButtonId`, `Interactable`, `IsActive`, activation events and `SetActiveState`.

`WorldInteraction_Press_StaticButton`

- Pressable 3D button implementing `IWorldInteraction_Press_Usable`.
- Animates a press transform and raises `onPressed`.

`WorldInteraction_Hand_StaticButtonBridge`

- Generic hand bridge for `WorldInteraction_Press_StaticButton`.
- Implements `IWorldInteraction_Press_LifecycleTarget`, forwards press begin/end to the button and exposes an optional `onUsed` event.
- Hides no cursor by itself; cursor lifetime is handled by the active hand/input controller.

`WorldInteraction_Toggle_Button`

- Toggle button implementing `IWorldInteraction_Press_Usable`.
- Supports optional toggle-off behavior, press animation, material/visual state switching and toggle events.

`WorldInteraction_Hand_ToggleBridge`

- Generic hand bridge for `WorldInteraction_Toggle_Button`.
- Implements `IWorldInteraction_Press_LifecycleTarget` and forwards press lifecycle to the toggle button.

`WorldInteraction_Toggle_StaticButton` and `WorldInteraction_Toggle_StaticGroup`

- Simple physical switch/radio-button style controls used by module examples.
- `WorldInteraction_Toggle_StaticButton` implements `IWorldInteraction_Press_LifecycleTarget`, has on/off materials and raises `Toggled`.
- `WorldInteraction_Toggle_StaticGroup` keeps one active toggle, or no active toggle when `allowNoSelection` is enabled.

`WorldInteraction_Press_Animation`

- Internal shared press animation helper used by static and toggle buttons.
- Keeps button components focused on state/events instead of tween lifecycle details.

`WorldInteraction_Toggle_Group` and `WorldInteraction_Toggle_Object`

- Small physical toggle-state system.
- Can use a marker `WorldInteraction_Slot_Item` and several `WorldInteraction_Slot_Base` slots to select the active state.

`WorldInteraction_Control_Slider`

- Slider driven by a draggable handle along a local X/Y/Z axis.
- Supports normalized value, optional stepping and edge events.
- Current handle drag uses hidden/locked cursor mouse-delta input, smooth handle follow and cursor restore to the handle on release, so slider value is independent of camera angle after the initial hit.
- Sensitivity is exposed through `normalizedValuePerMouseUnit`; follow smoothing is exposed through `dragFollowSpeed`.

`WorldInteraction_Hand_SliderBridge`

- Generic pointer-drag bridge for a `WorldInteraction_Control_Slider` handle.
- Implements `IWorldInteraction_Pointer_Draggable`, hides/locks the cursor while dragging and restores it to the handle screen position on release.

`WorldInteraction_Control_OptionRoller`

- Four-sided option roller implementing `IWorldInteraction_Pointer_Draggable`.
- Uses hidden/locked cursor mouse-delta input: drag up/down changes the pending face, release smoothly snaps to the nearest 0/90/180/270 step and returns the cursor to the roller center.
- Can generate simple transparent selected-preview planes to show which face will become selected on release.
- Sensitivity is exposed through `degreesPerScreenPixel`.

`WorldInteraction_Pointer_CursorUtility`

- Shared helper for capturing cursor visibility/lock state, hiding/locking cursor during delta-driven interactions and restoring/warping the cursor to a world-space target.
- Project-side held-object code can use the same pattern: hide/lock while integrating mouse delta, then restore to a world-space grip/handle target on release.

`WorldInteraction_UI_ObjectPool`

- Registers a small component pool through the template `PoolingManager`.

`WorldInteraction_Surface_WaterVolume`

- Trigger volume that applies upward/shore force to rigidbodies.
- Can register matching objects with `WorldInteraction_Drag_HardBoundsRecovery`.
- Debug gizmo settings are grouped and hidden when gizmos are disabled.

`WorldInteraction_Drag_HardBoundsRecovery`

- Simple recovery registry for rigidbodies that should return to a safe transform if lost.

`WorldInteraction_Camera_PivotConstraintVolume`, `WorldInteraction_Drag_BoxConstraintSettings`, `WorldInteraction_Camera_PivotIndicator`

- Camera/pivot constraint helpers kept in this module for compatibility with older template camera code.
- New camera mode/zone/lock work should prefer `DanieloZ.CameraSystem`.

## Contracts

`WorldInteractionContracts.cs` defines:

- `WorldInteractionContext`;
- `WorldDragReleaseContext`;
- `WorldSwingContext`;
- `IWorldInteraction_Press_Usable`;
- `IWorldInteraction_Surface_Hoverable`;
- `IWorldInteraction_Drag_ReleaseHandler`;
- `IWorldInteraction_Swing_Target`.

Use these contracts for game-specific components instead of adding special cases to the controller.

## Inspector Style

Runtime components follow this script layout:

1. Inspector fields.
2. Public API: events, properties and public commands.
3. Runtime/internal state.
4. Unity lifecycle callbacks.
5. Main feature flow.
6. Helpers, validation and debug sections.

Inspector fields use Odin foldout groups where the component has several concerns. Variant-only settings use Odin conditional attributes so the Inspector shows only relevant controls.

## Slot Usage

1. Add a trigger collider and `WorldInteraction_Slot_Base` to the slot object.
2. Assign `Anchor`, the transform where the item should snap.
3. Choose `validationMode`: `AcceptedItemIds` uses the item id allow-list, `ObjectGroup` uses the slot/item object group.
4. Fill `acceptedItemIds` for id validation. An empty list accepts any item in that mode.
5. Fill `acceptedObjectGroup` on the slot and `objectGroup` on matching items for group validation. Empty group means any group in that mode.
6. Add `WorldInteraction_Slot_Item` to the carried object and assign its `ItemId`.
7. Configure the item's inserted local position/rotation. These offsets are relative to the slot `Anchor`.
8. Use the item's Inspector `Preview` tab to assign a slot, show the item in that slot, then capture the current pose after tuning.
9. Let `WorldInteraction_Runtime_Controller` or a project-side hand bridge release the held item over the slot.
10. Configure `WorldInteraction_Slot_ResolutionSettings` on the interaction controller/bridge. The default path is a circular cast down from the held item bounds.

Physical collision alone does not insert the item. The collider is the cast/raycast/hover area; insertion is done by release logic or by `TryInsert`.
When inserted, the item becomes a child of the slot `Anchor` and is restored to its previous parent when removed.

## Examples

`Examples/Prefabs` contains reusable and test prefabs:

- `Base3DStaticButton.prefab`
- `Base3DStaticToggleButton.prefab`
- `Base3DStaticToggleGroup.prefab`
- `Base3DOptionRoller.prefab`
- `Base3DPhysicalButtonSlot.prefab`
- `Base3DPhysicalButton.prefab`
- `Test3DSlotItem.prefab`
- `Test3DSlot.prefab`
- `Test3DStaticButton.prefab`
- `Test3DToggleButton.prefab`
- `Test3DSlider.prefab`

`Examples/Scenes/WorldInteractionExamples.unity` shows the components in one standalone scene.

Module examples should not depend on `PixelLust.PixelVoxelPuzzle` components. Project-specific bridge components for PixelVoxelPuzzle hand/use input remain in `Assets/Runtime/WorldInteractionBridge` only when they contain puzzle, placement, box, save, sound or project layer rules.

## Project Guides

Integration guides live in `Docs/Guides`:

- `World3DDraggablePrefabs.md`
- `World3DSlotsAndSlotItems.md`
- `WorldInteraction_HandIntegration.md`
- `WorldInteraction_StaticButtons.md`
- `WorldInteraction_Switches.md`
- `WorldInteraction_Sliders.md`
- `WorldInteraction_OptionRollers.md`
- `WorldInteraction_OutlineAndCursor.md`

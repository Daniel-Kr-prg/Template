# DanieloZ.WorldInteraction

Runtime module for physical world-space interaction: raycast use, hover, draggable objects, swing impulses/callbacks, physical 3D buttons, toggle controls, insertable slot items, slots, sliders, water/recovery helpers, and small example bridges for PixelVoxelPuzzle hand input.

The module is intentionally generic. Game-specific systems should subscribe to events or call public APIs instead of adding puzzle rules directly into the interaction controller.

## Main Runtime Components

`WorldInteractionController`

- Central mouse/raycast router for drag, use, hover and swing.
- Registers LMB/RMB actions through the template `InputManager`.
- Routes mouse wheel to the currently held object through `WorldInteractionInputGate`.
- Finds compatible `World3DButtonSlotBase` targets for held `World3DSlotItem` objects.
- Inspector is grouped by Camera, Raycast, Drag, Use, Hover, Swing and Debug. Projection-specific fields are shown only for the selected projection mode.

`WorldInteractionInputGate`

- Static state for the currently held `WorldDraggable`.
- Blocks camera wheel when the held object consumes wheel input.
- Lets drag, camera and UI systems avoid fighting over the same input.

`WorldDraggable`

- Generic draggable physical object.
- Supports pickup blending, kinematic hold state, wheel yaw rotation, drag wobble, release inertia and optional constraints.
- Inspector hides wheel, wobble and release-inertia details when their parent option is disabled.

`WorldDragConstraintSettings`

- Serializable constraint block used by `WorldDraggable`.
- Can lock axes and clamp local ranges.
- Range fields appear only when constraints and the matching axis limit are enabled.

`World3DSlotItem`

- Draggable object with one `ItemId`.
- Can be inserted into a `World3DButtonSlotBase`.
- Snaps to the slot anchor and raises inserted/removed events.
- `World3DPhysicalButton` is a compatibility alias for older prefab naming.

`World3DButtonSlotBase`

- Physical raycast/hover slot for `World3DSlotItem`.
- Supports accepted item ids, strict id matching, lock-on-match, active state, accepted/rejected hover indicators, UnityEvents and C# events.
- Cursor hover while holding a slot item shows whether insertion is valid. Actual insertion happens on release or through `TryInsert`.

`World3DButtonBase`

- Shared base for simple active/interactable world buttons.
- Exposes `ButtonId`, `Interactable`, `IsActive`, activation events and `SetActiveState`.

`World3DStaticButton`

- Pressable 3D button implementing `IWorldUsable`.
- Animates a press transform and raises `onPressed`.

`World3DToggleButton`

- Toggle button implementing `IWorldUsable`.
- Supports optional toggle-off behavior, press animation, material/visual state switching and toggle events.

`World3DToggleGroup` and `World3DToggleObject`

- Small physical toggle-state system.
- Can use a marker `World3DSlotItem` and several `World3DButtonSlotBase` slots to select the active state.

`World3DSlider`

- Slider driven by a draggable handle along a local X/Y/Z axis.
- Supports normalized value, optional stepping, edge events and ray-based value setting.

`World3DUIObjectPool`

- Registers a small component pool through the template `PoolingManager`.

`WorldWaterVolume`

- Trigger volume that applies upward/shore force to rigidbodies.
- Can register matching objects with `WorldHardBoundsRecovery`.
- Debug gizmo settings are grouped and hidden when gizmos are disabled.

`WorldHardBoundsRecovery`

- Simple recovery registry for rigidbodies that should return to a safe transform if lost.

`WorldCameraPivotConstraintVolume`, `WorldBoxConstraintSettings`, `WorldCameraPivotIndicator`

- Camera/pivot constraint helpers kept in this module for compatibility with older template camera code.
- New camera mode/zone/lock work should prefer `DanieloZ.CameraSystem`.

## Contracts

`WorldInteractionContracts.cs` defines:

- `WorldInteractionContext`;
- `WorldDragReleaseContext`;
- `WorldSwingContext`;
- `IWorldUsable`;
- `IWorldHoverable`;
- `IWorldDraggableReleaseHandler`;
- `IWorldSwingable`.

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

1. Add a trigger collider and `World3DButtonSlotBase` to the slot object.
2. Assign `Anchor`, the transform where the item should snap.
3. Fill `acceptedItemIds`. An empty list accepts any item.
4. Add `World3DSlotItem` to the carried object and assign its `ItemId`.
5. Let `WorldInteractionController` or a project-side hand bridge release the held item over the slot.

Physical collision alone does not insert the item. The collider is the raycast/hover area; insertion is done by release logic or by `TryInsert`.

## Examples

`Examples/Prefabs` contains reusable and test prefabs:

- `Base3DStaticButton.prefab`
- `Base3DStaticToggleButton.prefab`
- `Base3DStaticToggleGroup.prefab`
- `Base3DPhysicalButtonSlot.prefab`
- `Base3DPhysicalButton.prefab`
- `Test3DSlotItem.prefab`
- `Test3DSlot.prefab`
- `Test3DStaticButton.prefab`
- `Test3DToggleButton.prefab`
- `Test3DSlider.prefab`

`Examples/Scenes/WorldInteractionExamples.unity` shows the components in one standalone scene.

`Examples/Scripts/PixelVoxelPuzzleIntegration` contains bridge components for the current PixelVoxelPuzzle hand/use system. They are examples/integration adapters, not core module code.

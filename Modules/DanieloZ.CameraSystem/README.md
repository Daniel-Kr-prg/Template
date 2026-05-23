# DanieloZ Camera System

Runtime camera layer for template projects. The module owns camera motion, Cinemachine priority switching, camera zones, camera locks, and Bezier camera rails. It does not read player input directly: input code calls public methods such as `Move`, `Zoom`, `BeginPointerOrbit`, `ActivateLock`, or `EnterMainMenu`.

## Assembly

Runtime scripts compile in `DanieloZ.CameraSystem.asmdef`.

The module references:

- `Cinemachine`
- Unity runtime assemblies

It intentionally does not reference project gameplay input, menu navigation, or `Assembly-CSharp`. When it needs the template `CameraManager`, it uses `CameraManagerBridge`, so the module can still live behind an asmdef while calling the existing global `CameraManager` at runtime.

## Components

`TopDownCameraController`

- Moves a pivot from input values supplied by another script.
- Drives camera and look targets from `WorldCameraBezierCurve`.
- Applies Cinemachine virtual camera pose and optional FOV by zoom.
- Pointer orbit is input-agnostic: pass screen positions into `BeginPointerOrbit` and `UpdatePointerOrbit`.
- Inspector is grouped by Rig, Curves, Movement, Middle Mouse Orbit, Smoothing and Lens. FOV fields are shown only when FOV-by-zoom is enabled.

`WorldCameraBezierCurve`

- Local cubic Bezier rail.
- Used for camera position curves and optional look-at curves.
- Includes an editor handle inspector for direct scene editing.

`CameraZone`

- Trigger/polled area with its own Cinemachine virtual camera.
- Has a `ZoneId`, optional fast-travel anchor, zone collider, and priority offset.

`CameraZoneController`

- Registers active `CameraZone` objects.
- Polls a ground pivot and activates the best zone camera by priority.
- Can fast-travel the pivot to a zone anchor with `FastTravelToZone`.
- Inspector separates references, priorities and polling behavior.

`CameraLock`

- Named focus target with its own Cinemachine virtual camera.
- Can expose a project-specific navigator as a plain `MonoBehaviour`.
- Raises `onEntered` and `onExited` UnityEvents.

`CameraLockController`

- Activates and exits camera locks.
- Raises C# events and UnityEvents when locks change.
- Does not push or pop input contexts. Add a project-side bridge if locks should affect input mode.
- Inspector separates navigation fallback, priorities and events.

`CameraModeController`

- Switches between gameplay and menu cameras.
- Temporarily disables the top-down camera pose while menu camera is active.
- Raises C# events and UnityEvents, leaving input state ownership outside the module.
- Inspector separates cameras, main-menu pose, priorities and events.

`CameraManagerSwitcher`

- Small utility component that calls the template `CameraManager` through `CameraManagerBridge`.

## Input Separation

The module has no direct `Input.*`, `InputContextStack`, or project enum dependency. A project input router should translate buttons, wheel deltas, and pointer positions into method calls:

```csharp
topDownCamera.Move(moveVector);
topDownCamera.Orbit(orbitDirection);
topDownCamera.Zoom(mouseWheelDelta);
topDownCamera.BeginPointerOrbit(Input.mousePosition);
topDownCamera.UpdatePointerOrbit(Input.mousePosition);
topDownCamera.EndPointerOrbit();
```

For PixelVoxelPuzzle, `PixelVoxelPuzzleCameraInputContextBridge` subscribes to camera mode/lock events and pushes or pops `PixelVoxelPuzzleInputContext.MainMenu`.

## Script Layout

Runtime scripts follow the same layout convention as `DanieloZ.WorldInteraction`:

1. Inspector fields.
2. Public API.
3. Runtime state.
4. Unity lifecycle callbacks.
5. Feature-specific flow.
6. Helpers.

Odin foldout groups are used for components with several inspector concerns. Input handling remains outside the module.

## Examples

Example prefabs are in `Prefabs/Examples`:

- `CameraCurvesExample.prefab` shows a pivot, Bezier camera rail, look rail, virtual camera, and top-down controller.
- `CameraZoneExample.prefab` shows a trigger volume, zone camera, and fast-travel anchor.
- `CameraLockExample.prefab` shows a named lock with a dedicated virtual camera.

The `Game` scene contains:

- `CameraZones/Zone_Board_Close`
- `CameraZones/Zone_Piece_Staging`
- `CameraZones/Zone_Menu_Gallery`
- `CameraLocks/Lock_Main_Menu`
- `CameraLocks/Lock_Achievement_Gallery`

Each zone and lock has its own Cinemachine virtual camera under the scene `Cameras` root.

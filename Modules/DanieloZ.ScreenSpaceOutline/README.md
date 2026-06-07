# DanieloZ Screen Space Outline

Reusable URP RenderGraph outline module for rendering visible outlines, occluded outlines, and optional occluded object visuals.

The module uses layer masks to select outlined renderers. Add multiple `ScreenSpaceOutlineMaskFeature` instances when different object groups need different materials or silhouette behavior.

## Requirements

- Universal Render Pipeline with RenderGraph enabled.
- A URP Renderer asset where the feature can be added.
- Dedicated Unity layers for each independently configured outline group.

The module intentionally has no asmdef. `ScreenSpaceOutlineMaskFeature` remains in `Assembly-CSharp`, preserving compatibility with existing serialized Renderer Feature instances.

## Module Layout

- `Runtime/ScreenSpaceOutlineMaskFeature.cs`
  - Renders visible and occluded masks.
  - Optionally renders the selected object's front-most visual into a private color/depth target.
  - Composites the result over the active camera color target.
- `Shaders/MaskShader.shader`
  - Visible mask using `ZTest LEqual`.
- `Shaders/OutlineOccludedMask.shader`
  - Occluded mask using `ZTest Greater`.
- `Shaders/ScreenSpaceOutlineComposite.shader`
  - Produces visible outlines, occluded outlines, and the optional occluded silhouette overlay.
- `Materials/WithTexture`
  - Profile that mixes the object's rendered visual into its occluded silhouette.
- `Materials/WithoutTexture`
  - Profile that uses a solid-color occluded silhouette and skips the object-visual render pass.

## Renderer Setup

1. Open the target URP Renderer asset.
2. Add a `ScreenSpaceOutlineMaskFeature`.
3. Assign an outlined layer mask.
4. Assign the matching Visible Mask, Occluded Mask, and Composite materials from one material profile.
5. Repeat with another feature only when another layer group needs different outline behavior or materials.

The current PixelVoxelPuzzle renderer uses:

- `ScreenSpaceOutlineMaskFeatureWithTexture` for `PuzzlePieceSelect`.
- `ScreenSpaceOutlineMaskFeatureWithoutTexture` for the remaining outline layers.

Objects are selected by switching their renderers to a layer included by the corresponding feature.

## Composite Controls

The composite material exposes:

- visible and occluded outline colors, alpha, and pixel thickness;
- occluded silhouette color;
- `Occluded Silhouette Color / Object Visual`, which mixes solid color with the rendered object visual;
- `Occluded Silhouette Overlay Alpha`, which controls the common silhouette overlay alpha;
- optional debug mask views.

Set the texture mix to zero when the object visual is not needed. The feature then skips the additional object-visual render pass.

## Performance Notes

Each feature renders two mask passes and two composite passes. A feature with object visual enabled also renders the selected objects into a private color/depth target.

Prefer a small number of feature instances and group layers that share materials. Outline thickness increases composite shader sampling cost. Validate MSAA changes in the Game view because temporary RenderGraph targets inherit the camera target descriptor.


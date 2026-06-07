using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceOutlineMaskFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Header("Objects")]
        public LayerMask outlinedLayer;

        [Header("Mask Materials")]
        public Material visibleMaskMaterial;
        public Material occludedMaskMaterial;

        [Header("Composite")]
        public Material compositeMaterial;

        [Header("Pass Event")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public Settings settings = new Settings();

    private OutlinePass _outlinePass;

    public override void Create()
    {
        _outlinePass = new OutlinePass(settings)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.visibleMaskMaterial == null)
            return;

        if (settings.occludedMaskMaterial == null)
            return;

        if (settings.compositeMaterial == null)
            return;

        renderer.EnqueuePass(_outlinePass);
    }

    private class OutlinePass : ScriptableRenderPass
    {
        private readonly Settings _settings;

        private static readonly int VisibleOutlineMaskTextureId =
            Shader.PropertyToID("_VisibleOutlineMaskTexture");

        private static readonly int OccludedObjectVisualTextureId =
            Shader.PropertyToID("_OccludedObjectVisualTexture");

        private readonly List<ShaderTagId> _shaderTagIdList = new List<ShaderTagId>
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit")
        };

        private class MaskPassData
        {
            public RendererListHandle rendererListHandle;
        }

        private class ObjectVisualPassData
        {
            public RendererListHandle rendererListHandle;
        }

        private class CompositePassData
        {
            public TextureHandle visibleMaskTexture;
            public TextureHandle occludedMaskTexture;
            public TextureHandle occludedObjectVisualTexture;
            public Material compositeMaterial;
        }

        public OutlinePass(Settings settings)
        {
            _settings = settings;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            if (_settings.visibleMaskMaterial == null ||
                _settings.occludedMaskMaterial == null ||
                _settings.compositeMaterial == null)
            {
                return;
            }

            UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
            UniversalLightData lightData = frameContext.Get<UniversalLightData>();

            RenderTextureDescriptor maskDescriptor = cameraData.cameraTargetDescriptor;
            maskDescriptor.depthBufferBits = 0;
            maskDescriptor.depthStencilFormat = GraphicsFormat.None;
            maskDescriptor.colorFormat = RenderTextureFormat.R8;

            RenderTextureDescriptor visualDescriptor = cameraData.cameraTargetDescriptor;
            visualDescriptor.depthBufferBits = 0;
            visualDescriptor.depthStencilFormat = GraphicsFormat.None;

            RenderTextureDescriptor visualDepthDescriptor = cameraData.cameraTargetDescriptor;
            visualDepthDescriptor.graphicsFormat = GraphicsFormat.None;

            TextureHandle visibleMaskTexture = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                maskDescriptor,
                "_VisibleOutlineMaskTexture",
                false
            );

            TextureHandle occludedMaskTexture = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                maskDescriptor,
                "_OccludedOutlineMaskTexture",
                false
            );

            bool useObjectVisual = _settings.compositeMaterial.HasProperty("_OccludedSilhouetteTextureMix")
                && _settings.compositeMaterial.GetFloat("_OccludedSilhouetteTextureMix") > 0.001f;

            TextureHandle occludedObjectVisualTexture = renderGraph.defaultResources.blackTexture;
            TextureHandle occludedObjectVisualDepthTexture = TextureHandle.nullHandle;

            if (useObjectVisual)
            {
                occludedObjectVisualTexture = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    visualDescriptor,
                    "_OccludedObjectVisualTexture",
                    false
                );

                occludedObjectVisualDepthTexture = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    visualDepthDescriptor,
                    "_OccludedObjectVisualDepthTexture",
                    false
                );
            }

            RenderMaskPass(
                renderGraph,
                resourceData,
                renderingData,
                cameraData,
                lightData,
                visibleMaskTexture,
                _settings.visibleMaskMaterial,
                "Render Visible Outline Mask"
            );

            RenderMaskPass(
                renderGraph,
                resourceData,
                renderingData,
                cameraData,
                lightData,
                occludedMaskTexture,
                _settings.occludedMaskMaterial,
                "Render Occluded Outline Mask"
            );

            if (useObjectVisual)
            {
                RenderOccludedObjectVisualPass(
                    renderGraph,
                    renderingData,
                    cameraData,
                    lightData,
                    occludedObjectVisualTexture,
                    occludedObjectVisualDepthTexture
                );
            }

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                "Composite Screen Space Outline",
                out CompositePassData passData))
            {
                passData.visibleMaskTexture = visibleMaskTexture;
                passData.occludedMaskTexture = occludedMaskTexture;
                passData.occludedObjectVisualTexture = occludedObjectVisualTexture;
                passData.compositeMaterial = _settings.compositeMaterial;

                builder.UseTexture(visibleMaskTexture, AccessFlags.Read);
                builder.UseTexture(occludedMaskTexture, AccessFlags.Read);
                builder.UseTexture(occludedObjectVisualTexture, AccessFlags.Read);

                builder.SetRenderAttachment(
                    resourceData.activeColorTexture,
                    0
                );

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(
                        VisibleOutlineMaskTextureId,
                        data.visibleMaskTexture
                    );

                    context.cmd.SetGlobalTexture(
                        OccludedObjectVisualTextureId,
                        data.occludedObjectVisualTexture
                    );

                    // Pass 0: visible outline.
                    // _BlitTexture = visibleMaskTexture
                    Blitter.BlitTexture(
                        context.cmd,
                        data.visibleMaskTexture,
                        new Vector4(1, 1, 0, 0),
                        data.compositeMaterial,
                        0
                    );

                    // Pass 1: occluded silhouette + occluded outline.
                    // _BlitTexture = occludedMaskTexture
                    Blitter.BlitTexture(
                        context.cmd,
                        data.occludedMaskTexture,
                        new Vector4(1, 1, 0, 0),
                        data.compositeMaterial,
                        1
                    );
                });
            }
        }

        private void RenderOccludedObjectVisualPass(
            RenderGraph renderGraph,
            UniversalRenderingData renderingData,
            UniversalCameraData cameraData,
            UniversalLightData lightData,
            TextureHandle targetVisualTexture,
            TextureHandle targetVisualDepthTexture)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<ObjectVisualPassData>(
                "Render Occluded Object Visual",
                out ObjectVisualPassData passData))
            {
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    _shaderTagIdList,
                    renderingData,
                    cameraData,
                    lightData,
                    cameraData.defaultOpaqueSortFlags
                );

                FilteringSettings filteringSettings = new FilteringSettings(
                    RenderQueueRange.all,
                    _settings.outlinedLayer
                );

                NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(1, Allocator.Temp);
                NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(1, Allocator.Temp);

                tagValues[0] = ShaderTagId.none;
                stateBlocks[0] = new RenderStateBlock(RenderStateMask.Depth)
                {
                    depthState = new DepthState(true, CompareFunction.LessEqual)
                };

                RendererListParams rendererListParams = new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings)
                {
                    tagValues = tagValues,
                    stateBlocks = stateBlocks,
                    isPassTagName = false
                };

                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

                tagValues.Dispose();
                stateBlocks.Dispose();

                builder.UseRendererList(passData.rendererListHandle);
                builder.SetRenderAttachment(targetVisualTexture, 0);
                builder.SetRenderAttachmentDepth(targetVisualDepthTexture, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (ObjectVisualPassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(true, true, Color.clear);
                    context.cmd.DrawRendererList(data.rendererListHandle);
                });
            }
        }

        private void RenderMaskPass(
            RenderGraph renderGraph,
            UniversalResourceData resourceData,
            UniversalRenderingData renderingData,
            UniversalCameraData cameraData,
            UniversalLightData lightData,
            TextureHandle targetMaskTexture,
            Material maskMaterial,
            string passName)
        {
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                passName,
                out MaskPassData passData))
            {
                SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;

                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    _shaderTagIdList,
                    renderingData,
                    cameraData,
                    lightData,
                    sortingCriteria
                );

                drawingSettings.overrideMaterial = maskMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;

                FilteringSettings filteringSettings = new FilteringSettings(
                    RenderQueueRange.all,
                    _settings.outlinedLayer
                );

                RendererListParams rendererListParams = new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings
                );

                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

                builder.UseRendererList(passData.rendererListHandle);

                builder.SetRenderAttachment(
                    targetMaskTexture,
                    0
                );

                builder.SetRenderAttachmentDepth(
                    resourceData.activeDepthTexture,
                    AccessFlags.Read
                );

                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(false, true, Color.black);
                    context.cmd.DrawRendererList(data.rendererListHandle);
                });
            }
        }
    }
}

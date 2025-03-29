using DanieloZ.Config;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ConfigAvailableSettings
{
    // GAME --------------------------------
    public enum Language
    {
        en,
        ru
    }


    // VIDEO ----------------------------------------
    public static int[] FrameRateOptions = { 30, 60, 75, 120, 144, 165, 240, 0 };

    public static List<ResolutionSettings> Resolutions = new List<ResolutionSettings>
    {
        // 16:9
        new ResolutionSettings() { Height = 720, Width = 1280, H_Scale = 9, W_Scale = 16 },
        new ResolutionSettings() { Height = 1080, Width = 1920, H_Scale = 9, W_Scale = 16 },
        new ResolutionSettings() { Height = 1440, Width = 2560, H_Scale = 9, W_Scale = 16 },
        new ResolutionSettings() { Height = 2160, Width = 3840, H_Scale = 9, W_Scale = 16 },
    
        // 16:10
        new ResolutionSettings() { Height = 800, Width = 1280, H_Scale = 10, W_Scale = 16 },
        new ResolutionSettings() { Height = 1050, Width = 1680, H_Scale = 10, W_Scale = 16 },
        new ResolutionSettings() { Height = 1200, Width = 1920, H_Scale = 10, W_Scale = 16 },
        new ResolutionSettings() { Height = 1600, Width = 2560, H_Scale = 10, W_Scale = 16 },
    
        // 21:9
        new ResolutionSettings() { Height = 1080, Width = 2560, H_Scale = 9, W_Scale = 21 },
        new ResolutionSettings() { Height = 1440, Width = 3440, H_Scale = 9, W_Scale = 21 },
        new ResolutionSettings() { Height = 2160, Width = 5120, H_Scale = 9, W_Scale = 21 },

        // 32:9
        new ResolutionSettings() { Height = 1080, Width = 3840, H_Scale = 9, W_Scale = 32 },
        new ResolutionSettings() { Height = 1440, Width = 5120, H_Scale = 9, W_Scale = 32 },
        new ResolutionSettings() { Height = 2160, Width = 7680, H_Scale = 9, W_Scale = 32 }
    };

    // QUALITY ----------------------------------------------------------------

    public enum MSAA_Sampling
    {
        Disabled = 0,
        x2 = 2,
        x4 = 4,
        x8 = 8
    }

    public enum ShadowResolution
    {
        Low = 512,
        Medium = 1024,
        High = 2048,
        Ultra = 4096
    }
    public enum ShadowCascades
    {
        One = 1,
        Two = 2,
        Four = 4
    }
    
    public enum AmbientOcclusion
    {
        Disabled,
        SSAO,
        HBAO
    }

    public enum LightQuality
    {
        Low,
        Medium,
        High
    }
    
    public enum TextureQuality
    {
        Low,
        Medium,
        High,
        VeryHigh
    }
    public static Dictionary<TextureQuality, UnityEngine.TextureMipmapLimitSettings> AvailableTextureQuality = new Dictionary<TextureQuality, TextureMipmapLimitSettings>
    {
        {
            TextureQuality.Low,
            new TextureMipmapLimitSettings
            {
                limitBiasMode = TextureMipmapLimitBiasMode.OffsetGlobalLimit,
                limitBias = 5
            }
        },
        {
            TextureQuality.Medium,
            new TextureMipmapLimitSettings
            {
                limitBiasMode = TextureMipmapLimitBiasMode.OffsetGlobalLimit,
                limitBias = 3
            }
        },
        {
            TextureQuality.High,
            new TextureMipmapLimitSettings
            {
                limitBiasMode = TextureMipmapLimitBiasMode.OffsetGlobalLimit,
                limitBias = 1
            }
        },
        {
            TextureQuality.VeryHigh,
            new TextureMipmapLimitSettings
            {
                limitBiasMode = TextureMipmapLimitBiasMode.OffsetGlobalLimit,
                limitBias = 0
            }
        }
    };

    public enum LODQuality
    {
        Low,
        Medium,
        High
    }
    public enum WaterQuality
    {
        Low,
        Medium,
        High
    }
    public enum ReflectionQuality
    {
        Disabled,
        Low,
        High
    }
    public enum ParticleQuality
    {
        Low,
        Medium,
        High
    }

    public enum AnisotropicFiltering
    {
        Disabled = 1,
        x2 = 2,
        x4 = 4,
        x8 = 8,
        x16 = 16
    }


    // POST-PROCESSING -----------------------------------------------

    public enum Bloom
    {
        Disabled,
        Low,
        Medium,
        High
    }
    public enum MotionBlur
    {
        Disabled,
        Low,
        High
    }
    public enum DepthOfField
    {
        Disabled,
        Enabled
    }

    public class SettingsPreset
    {
        public string Name { get; set; }

        public ConfigAvailableSettings.MSAA_Sampling MSAA { get; set; }
        public AntialiasingMode AntiAliasing { get; set; }
        public ConfigAvailableSettings.ShadowResolution ShadowRes { get; set; }
        public ConfigAvailableSettings.ShadowCascades ShadowCascades { get; set; }
        public float ShadowsDistance { get; set; }
        public float LightDistance { get; set; }
        public float DrawDistance { get; set; }
        public ConfigAvailableSettings.TextureQuality TextureQuality { get; set; }
        public ConfigAvailableSettings.LODQuality LODQuality { get; set; }
        public ConfigAvailableSettings.WaterQuality WaterQuality { get; set; }
        public ConfigAvailableSettings.ReflectionQuality ReflectionQuality { get; set; }
        public ConfigAvailableSettings.ParticleQuality ParticleQuality { get; set; }
        public ConfigAvailableSettings.AnisotropicFiltering AnisotropicFiltering { get; set; }
        public ConfigAvailableSettings.Bloom Bloom { get; set; }
        public ConfigAvailableSettings.MotionBlur MotionBlur { get; set; }
        public ConfigAvailableSettings.DepthOfField DepthOfField { get; set; }
        public ConfigAvailableSettings.AmbientOcclusion AmbientOcclusion { get; set; }
        public bool VolumetricLighting { get; set; }

        public SettingsPreset(
            string name,
            ConfigAvailableSettings.MSAA_Sampling msaa,
            AntialiasingMode antiAliasing,
            ConfigAvailableSettings.ShadowResolution shadowRes,
            ConfigAvailableSettings.ShadowCascades shadowCascades,
            float shadowsDistance,
            float lightDistance,
            float drawDistance,
            ConfigAvailableSettings.TextureQuality textureQuality,
            ConfigAvailableSettings.LODQuality lodQuality,
            ConfigAvailableSettings.WaterQuality waterQuality,
            ConfigAvailableSettings.ReflectionQuality reflectionQuality,
            ConfigAvailableSettings.ParticleQuality particleQuality,
            ConfigAvailableSettings.AnisotropicFiltering anisotropicFiltering,
            ConfigAvailableSettings.Bloom bloom,
            ConfigAvailableSettings.MotionBlur motionBlur,
            ConfigAvailableSettings.DepthOfField depthOfField,
            ConfigAvailableSettings.AmbientOcclusion ambientOcclusion,
            bool volumetricLighting
        )
        {
            Name = name;
            MSAA = msaa;
            AntiAliasing = antiAliasing;
            ShadowRes = shadowRes;
            ShadowCascades = shadowCascades;
            ShadowsDistance = shadowsDistance;
            LightDistance = lightDistance;
            DrawDistance = drawDistance;
            TextureQuality = textureQuality;
            LODQuality = lodQuality;
            WaterQuality = waterQuality;
            ReflectionQuality = reflectionQuality;
            ParticleQuality = particleQuality;
            AnisotropicFiltering = anisotropicFiltering;
            Bloom = bloom;
            MotionBlur = motionBlur;
            DepthOfField = depthOfField;
            AmbientOcclusion = ambientOcclusion;
            VolumetricLighting = volumetricLighting;
        }
    }


    public static List<SettingsPreset> Presets = new List<SettingsPreset>
{
    new SettingsPreset(
        "Low",
        ConfigAvailableSettings.MSAA_Sampling.Disabled,
        AntialiasingMode.FastApproximateAntialiasing,
        ConfigAvailableSettings.ShadowResolution.Low,
        ConfigAvailableSettings.ShadowCascades.One,
        50, // ShadowsDistance
        0.5f, // LightDistance
        100, // DrawDistance
        ConfigAvailableSettings.TextureQuality.Low,
        ConfigAvailableSettings.LODQuality.Low,
        ConfigAvailableSettings.WaterQuality.Low,
        ConfigAvailableSettings.ReflectionQuality.Disabled,
        ConfigAvailableSettings.ParticleQuality.Low,
        ConfigAvailableSettings.AnisotropicFiltering.Disabled,
        ConfigAvailableSettings.Bloom.Disabled,
        ConfigAvailableSettings.MotionBlur.Disabled,
        ConfigAvailableSettings.DepthOfField.Disabled,
        ConfigAvailableSettings.AmbientOcclusion.Disabled,
        false
    ),
    new SettingsPreset(
        "High",
        ConfigAvailableSettings.MSAA_Sampling.x4,
        AntialiasingMode.SubpixelMorphologicalAntiAliasing,
        ConfigAvailableSettings.ShadowResolution.High,
        ConfigAvailableSettings.ShadowCascades.Four,
        150, // ShadowsDistance
        1.0f, // LightDistance
        300, // DrawDistance
        ConfigAvailableSettings.TextureQuality.High,
        ConfigAvailableSettings.LODQuality.High,
        ConfigAvailableSettings.WaterQuality.High,
        ConfigAvailableSettings.ReflectionQuality.High,
        ConfigAvailableSettings.ParticleQuality.High,
        ConfigAvailableSettings.AnisotropicFiltering.x16,

        ConfigAvailableSettings.Bloom.High,
        ConfigAvailableSettings.MotionBlur.Low,
        ConfigAvailableSettings.DepthOfField.Enabled,
        ConfigAvailableSettings.AmbientOcclusion.SSAO,
        true
    )
};
}

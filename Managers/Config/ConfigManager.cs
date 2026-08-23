using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DanieloZ.Config;
using System.Linq;
using DanieloZ.Managers.Sound;
using UnityEngine.Localization.Settings;

namespace DanieloZ.Managers.Config
{
    public class ConfigManager : SingletonManager<ConfigManager>
    {
        [Header("Source type")]
        private LoadSourceType LoadSource;

        [Header("Components")]
        [SerializeField] VolumeProfile volumeProfile;
        [SerializeField] UniversalRenderPipelineAsset URPSettings;
        Camera CurrentCamera => CameraManager.CurrentCamera;

        [Header("Configuration")]
        [SerializeField] public ConfigData configData;
        Dictionary<SettingsKeyname, Action<object>> configValuesChangedCallbacks = new Dictionary<SettingsKeyname, Action<object>>();

        bool IsInitialized = false;

        private const int TargetFrameRate = 60;
        public const string TEXTURE_MIPMAP_GROUP_NAME = "MIPMAP_TEXTURES_GROUP";

        private void Start()
        {
            // Additional handling before stage changing
            StagesManager.Instance.AppStages.RegisterStageStartAction(AppStageName.ConfigSetup, "ConfigSetup", () =>
            {
                Initialize();
            });
            StagesManager.Instance.AppStages.RegisterStageChangeCondition(AppStageName.ConfigSetup, "ConfigSetup_Success", new StageCondition(new Func<bool>(
                () => IsInitialized
                )));

            // Satisfy stage condition
            StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_ConfigManagerReady");
        }

        void Initialize()
        {
            InitializeConfigData();
            InitializeCallbacks();

            IsInitialized = true;

            StagesManager.Instance.AppStages.currentStage.SatisfyCondition("ConfigSetup_Success");
        }

        private void InitializeConfigData()
        {
            LoadConfig();
        }

        void InitializeCallbacks()
        {
            configValuesChangedCallbacks = new Dictionary<SettingsKeyname, Action<object>>()
            {
                {
                    SettingsKeyname.GAME_FOV, new Action<object>(x =>
                    {
                        if (x is int fov)
                        {
                            SetFOV(fov);
                            Debug.Log($"[M] ConfigManager: Set FOV to {fov}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / GAME_FOV: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateFOV();
                        }
                    })
                },
                {
                    SettingsKeyname.GAME_LANGUAGE, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.Language language)
                        {
                            SetLanguage(language);
                            Debug.Log($"[M] ConfigManager: Set language to {language}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / GAME_LANGUAGE: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateLanguage();
                        }
                    })
                },
                {
                    SettingsKeyname.VIDEO_RESOLUTION, new Action<object>(x =>
                    {
                        if (x is ResolutionSettings resolution)
                        {
                            SetResolution(resolution);
                            Debug.Log($"[M] ConfigManager: Set resolution to {resolution.Width}x{resolution.Height}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / VIDEO_RESOLUTION: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateResolution();
                        }
                    })
                },
                {
                    SettingsKeyname.VIDEO_FULLSCREEN, new Action<object>(x =>
                    {
                        if (x is FullScreenMode mode)
                        {
                            SetFullscreenMode(mode);
                            Debug.Log($"[M] ConfigManager: Set fullscreen mode to {mode}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / VIDEO_FULLSCREEN: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateFullscreenMode();
                        }
                    })
                },
                {
                    SettingsKeyname.VIDEO_LIMIT_REFRESHRATE, new Action<object>(x =>
                    {
                        if (x is bool limitRefreshRate)
                        {
                            SetLimitRefreshRate(limitRefreshRate);
                            Debug.Log($"[M] ConfigManager: Set limit refresh rate to {limitRefreshRate}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / VIDEO_LIMIT_REFRESHRATE: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateLimitRefreshRate();
                        }
                    })
                },
                {
                    SettingsKeyname.VIDEO_REFRESHRATE, new Action<object>(x =>
                    {
                        if (x is int refreshRate)
                        {
                            SetRefreshRate(refreshRate);
                            Debug.Log($"[M] ConfigManager: Set refresh rate to {refreshRate}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / VIDEO_REFRESHRATE: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateRefreshRate();
                        }
                    })
                },
                {
                    SettingsKeyname.VIDEO_VSYNC, new Action<object>(x =>
                    {
                        if (x is int vSyncEnabled)
                        {
                            SetVSync(vSyncEnabled);
                            Debug.Log($"[M] ConfigManager: Set VSync to {vSyncEnabled}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / VIDEO_VSYNC: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateVSync();
                        }
                    })
                },
                {
                    SettingsKeyname.VIDEO_BRIGHTNESS, new Action<object>(x =>
                    {
                        if (x is float brightness)
                        {
                            SetBrightness(brightness);
                            Debug.Log($"[M] ConfigManager: Set brightness to {brightness}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / VIDEO_BRIGHTNESS: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateBrightness();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_TEXTURES_RESOLUTION, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.TextureQuality textureQuality)
                        {
                            SetTextureQuality(textureQuality);
                            Debug.Log($"[M] ConfigManager: Set texture quality to {textureQuality}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_TEXTURES_RESOLUTION: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateTextureQuality();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_ANISOTROPIC_FILTRATION, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.AnisotropicFiltering filtering)
                        {
                            SetAnisotropicFiltering(filtering);
                            Debug.Log($"[M] ConfigManager: Set anisotropic filtering to {filtering}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_ANISOTROPIC_FILTRATION: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateAnisotropicFiltering();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_LOD, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.LODQuality lodQuality)
                        {
                            SetLODQuality(lodQuality);
                            Debug.Log($"[M] ConfigManager: Set LOD quality to {lodQuality}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_LOD: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateLODQuality();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_DRAW_DISTANCE, new Action<object>(x =>
                    {
                        if (x is float distance)
                        {
                            SetDrawDistance(distance);
                            Debug.Log($"[M] ConfigManager: Set draw distance to {distance}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_DRAW_DISTANCE: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateDrawDistance();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_ANTI_ALIASING, new Action<object>(x =>
                    {
                        if (x is AntialiasingMode antiAliasing)
                        {
                            SetAntiAliasing(antiAliasing);
                            Debug.Log($"[M] ConfigManager: Set anti-aliasing to {antiAliasing}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_ANTI_ALIASING: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateAntiAliasing();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_MSAA_SAMPLES, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.MSAA_Sampling msaaSamples)
                        {
                            SetMSAASamples(msaaSamples);
                            Debug.Log($"[M] ConfigManager: Set MSAA samples to {msaaSamples}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_MSAA_SAMPLES: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateMSAASamples();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_SHADOWS_RESOLUTION, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.ShadowResolution resolution)
                        {
                            SetShadowResolution(resolution);
                            Debug.Log($"[M] ConfigManager: Set shadow resolution to {resolution}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_SHADOWS_RESOLUTION: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateShadowResolution();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_SHADOWS_CASCADES, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.ShadowCascades cascades)
                        {
                            SetShadowCascades(cascades);
                            Debug.Log($"[M] ConfigManager: Set shadow cascades to {cascades}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_SHADOWS_CASCADES: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateShadowCascades();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_SHADOWS_DISTANCE, new Action<object>(x =>
                    {
                        if (x is float distance)
                        {
                            SetShadowsDistance(distance);
                            Debug.Log($"[M] ConfigManager: Set shadow distance to {distance}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_SHADOWS_DISTANCE: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateShadowsDistance();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_LIGHT_QUALITY, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.LightQuality lightQuality)
                        {
                            SetLightQuality(lightQuality);
                            Debug.Log($"[M] ConfigManager: Set light quality to {lightQuality}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_LIGHT_QUALITY: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateLightQuality();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_LIGHT_VOLUMETRIC, new Action<object>(x =>
                    {
                        if (x is bool volumetric)
                        {
                            SetVolumetricLighting(volumetric);
                            Debug.Log($"[M] ConfigManager: Set volumetric lighting to {volumetric}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_LIGHT_VOLUMETRIC: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateVolumetricLighting();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_LIGHT_DISTANCE, new Action<object>(x =>
                    {
                        if (x is float distance)
                        {
                            SetLightDistance(distance);
                            Debug.Log($"[M] ConfigManager: Set light distance to {distance}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_LIGHT_DISTANCE: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateLightDistance();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_LIGHT_AMBIENT_OCCLUSION, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.AmbientOcclusion ao)
                        {
                            SetAmbientOcclusion(ao);
                            Debug.Log($"[M] ConfigManager: Set ambient occlusion to {ao}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_LIGHT_AMBIENT_OCCLUSION: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateAmbientOcclusion();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_WATER_QUALITY, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.WaterQuality waterQuality)
                        {
                            SetWaterQuality(waterQuality);
                            Debug.Log($"[M] ConfigManager: Set water quality to {waterQuality}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_WATER_QUALITY: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateWaterQuality();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_REFLECTION_QUALITY, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.ReflectionQuality reflectionQuality)
                        {
                            SetReflectionQuality(reflectionQuality);
                            Debug.Log($"[M] ConfigManager: Set reflection quality to {reflectionQuality}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_REFLECTION_QUALITY: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateReflectionQuality();
                        }
                    })
                },
                {
                    SettingsKeyname.QUALITY_PARTICLE_QUALITY, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.ParticleQuality particleQuality)
                        {
                            SetParticleQuality(particleQuality);
                            Debug.Log($"[M] ConfigManager: Set particle quality to {particleQuality}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / QUALITY_PARTICLE_QUALITY: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateParticleQuality();
                        }
                    })
                },
                {
                    SettingsKeyname.PP_BLOOM, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.Bloom bloom)
                        {
                            SetBloom(bloom);
                            Debug.Log($"[M] ConfigManager: Set bloom to {bloom}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / PP_BLOOM: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateBloom();
                        }
                    })
                },
                {
                    SettingsKeyname.PP_MOTION_BLUR, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.MotionBlur motionBlur)
                        {
                            SetMotionBlur(motionBlur);
                            Debug.Log($"[M] ConfigManager: Set motion blur to {motionBlur}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / PP_MOTION_BLUR: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateMotionBlur();
                        }
                    })
                },
                {
                    SettingsKeyname.PP_DEPTH_OF_FIELD, new Action<object>(x =>
                    {
                        if (x is ConfigAvailableSettings.DepthOfField depthOfField)
                        {
                            SetDepthOfField(depthOfField);
                            Debug.Log($"[M] ConfigManager: Set depth of field to {depthOfField}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / PP_DEPTH_OF_FIELD: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateDepthOfField();
                        }
                    })
                },
                {
                    SettingsKeyname.AUDIO_MASTER, new Action<object>(x =>
                    {
                        if (x is float volume)
                        {
                            SetMasterVolume(volume);
                            Debug.Log($"[M] ConfigManager: Set master volume to {volume}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / AUDIO_MASTER: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateMasterVolume();
                        }
                    })
                },
                {
                    SettingsKeyname.AUDIO_MUSIC, new Action<object>(x =>
                    {
                        if (x is float volume)
                        {
                            SetMusicVolume(volume);
                            Debug.Log($"[M] ConfigManager: Set music volume to {volume}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / AUDIO_MUSIC: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateMusicVolume();
                        }
                    })
                },
                {
                    SettingsKeyname.AUDIO_EFFECTS, new Action<object>(x =>
                    {
                        if (x is float volume)
                        {
                            SetEffectsVolume(volume);
                            Debug.Log($"[M] ConfigManager: Set effects volume to {volume}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / AUDIO_EFFECTS: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateEffectsVolume();
                        }
                    })
                },
                {
                    SettingsKeyname.AUDIO_VOICE_CHAT, new Action<object>(x =>
                    {
                        if (x is float volume)
                        {
                            SetVoiceChatVolume(volume);
                            Debug.Log($"[M] ConfigManager: Set voice chat volume to {volume}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / AUDIO_VOICE_CHAT: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateVoiceChatVolume();
                        }
                    })
                },
                {
                    SettingsKeyname.UI_CONTROL_HINTS, new Action<object>(x =>
                    {
                        if (x is bool enabled)
                        {
                            SetControlHintsEnabled(enabled);
                            Debug.Log($"[M] ConfigManager: Set control hints to {enabled}");
                        }
                        else
                        {
                            Debug.Log($"[M] ConfigManager / UI_CONTROL_HINTS: Invalid value passed. Value will be updated from ConfigData.");
                            UpdateControlHintsEnabled();
                        }
                    })
                }
            };
        }

        public static void UpdateAllSettings()
        {
            foreach (var action in Instance.configValuesChangedCallbacks.Values)
            {
                action.Invoke(null);
            }
        }

        public static void UpdateSettings(SettingsKeyname key, object value)
        {
            if (Instance.configValuesChangedCallbacks.TryGetValue(key, out Action<object> settingsUpdateAction)) {
                settingsUpdateAction.Invoke(value);
            }
            else
            {
                Debug.LogError($"[M] Config Manager / UpdateSettings: No handler for {key} parameter");
            }
        }

        #region Save/Load

#if UNITY_EDITOR
        [ContextMenu("Save default config")]
        public void SaveDefault()
        {
            ConfigManagerIO.SaveToDefaultConfig(configData);
        }
#endif
        [ContextMenu("Load default config")]
        public void LoadDefault()
        {
            ConfigManagerIO.LoadDefaultConfig(configData);
        }

        [ContextMenu("Test save")]
        public void TestSave()
        {
            ConfigManagerIO.SaveConfig_Local("Assets/meow.cfg", configData);
        }

        [ContextMenu("Test load")]
        public void TestLoad()
        {
            ConfigManagerIO.LoadConfig_Local("Assets/meow.cfg", configData);
        }

        [ContextMenu("Save config local")]
        public void SaveConfig()
        {
            ConfigManagerIO.SaveConfig_Local(ImportantFilepaths.SettingsConfigPath, configData);
        }

        [ContextMenu("Load config local")]
        public void LoadConfig()
        {
            ConfigManagerIO.LoadConfig_Local(ImportantFilepaths.SettingsConfigPath, configData, () =>
            {
                UpdateAllSettings();
            });
        }
        #endregion

        #region Settings handling methods

        void SetFOV(int FOV)
        {
            configData.GameSettings.FOV = FOV;
            CurrentCamera.fieldOfView = FOV;
            Debug.Log($"[M] ConfigManager: FOV updated to {FOV}");
        }

        // Game Settings
        public void SetLanguage(ConfigAvailableSettings.Language language)
        {
            configData.GameSettings.Language = language;
            LocalizationManager.SetLanguage(language);
            Debug.Log($"[M] ConfigManager: Language set to {language}");
        }

        // Video Settings
        void SetResolution(ResolutionSettings resolution)
        {
            configData.GraphicsSettings.Resolution = resolution;
            Screen.SetResolution(resolution.Width, resolution.Height, configData.GraphicsSettings.FullscreenMode);
            Debug.Log($"[M] Resolution set to {resolution.Width}x{resolution.Height}, scale: {resolution.W_Scale}x{resolution.H_Scale}");
        }

        public void SetFullscreenMode(FullScreenMode mode)
        {
            configData.GraphicsSettings.FullscreenMode = mode;
            Screen.fullScreenMode = mode;
            Debug.Log($"[M] ConfigManager: Fullscreen mode set to {mode}");
        }

        public void SetLimitRefreshRate(bool limit)
        {
            configData.GraphicsSettings.LimitRefreshRate = true;
            configData.GraphicsSettings.RefreshRate = TargetFrameRate;
            Application.targetFrameRate = TargetFrameRate;
            Debug.Log($"[M] ConfigManager: Target frame rate set to {TargetFrameRate}");
        }

        public void SetRefreshRate(int rate)
        {
            configData.GraphicsSettings.LimitRefreshRate = true;
            configData.GraphicsSettings.RefreshRate = TargetFrameRate;
            Application.targetFrameRate = TargetFrameRate;
            Debug.Log($"[M] ConfigManager: Refresh rate set to {TargetFrameRate}");
        }

        public void SetVSync(int vSyncValue)
        {
            configData.GraphicsSettings.vSync = 0;
            UnityEngine.QualitySettings.vSyncCount = 0;
            Debug.Log("[M] ConfigManager: VSync disabled");
        }

        public void SetBrightness(float brightness)
        {
            float newValue = Mathf.Lerp(ConfigSettingsLimits.BrightnessLimit.x, ConfigSettingsLimits.BrightnessLimit.y, brightness);
            configData.GraphicsSettings.Brightness = newValue;
            if (volumeProfile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
                colorAdjustments.postExposure.value = newValue;
            }
            Debug.Log($"[M] ConfigManager: Brightness set to {brightness} / {newValue}");
        }

        // Quality Settings
        public void SetTextureQuality(ConfigAvailableSettings.TextureQuality quality)
        {
            configData.QualitySettings.TextureQuality = quality;

            UnityEngine.QualitySettings.SetTextureMipmapLimitSettings(TEXTURE_MIPMAP_GROUP_NAME, ConfigAvailableSettings.AvailableTextureQuality[quality]);
            Debug.Log($"[M] ConfigManager: Texture quality set to {quality}");
        }

        public void SetAnisotropicFiltering(ConfigAvailableSettings.AnisotropicFiltering filtering)
        {
            configData.QualitySettings.AnisotropicFiltering = filtering;
        
            if (filtering == ConfigAvailableSettings.AnisotropicFiltering.Disabled)
            {
                UnityEngine.QualitySettings.anisotropicFiltering = UnityEngine.AnisotropicFiltering.Disable;
            }
            else
            {
                Texture[] textures = Resources.LoadAll<Texture>("Textures");
                foreach (Texture texture in textures)
                {
                    if (texture != null)
                    {
                        texture.anisoLevel = Mathf.Clamp((int)filtering, 1, 16);
                    }
                }
            }
            Debug.Log($"[M] ConfigManager: Anisotropic filtering set to {filtering}");
        }

        public void SetLODQuality(ConfigAvailableSettings.LODQuality quality)
        {
            configData.QualitySettings.LODQuality = quality;

            ApplyLODQualityToScene(quality);

            Debug.Log($"[M] ConfigManager: LOD quality set to {quality}");
        }

        public void SetDrawDistance(float distance)
        {
            configData.QualitySettings.DrawDistance = distance;

            ApplyDrawDistanceToScene(distance);

            Debug.Log($"[M] ConfigManager: Draw distance set to {distance}");
        }

        public void SetAntiAliasing(AntialiasingMode mode)
        {
            if (mode == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                mode = AntialiasingMode.FastApproximateAntialiasing;

            configData.QualitySettings.AntiAliasing = mode;

            CameraManager.CurrentCamera.GetComponent<UniversalAdditionalCameraData>().antialiasing = mode;

            Debug.Log($"[M] ConfigManager: Anti-aliasing set to {mode}");
        }

        public void SetMSAASamples(ConfigAvailableSettings.MSAA_Sampling samples)
        {
            samples = ConfigAvailableSettings.MSAA_Sampling.Disabled;
            configData.QualitySettings.MSAA_Sampling = samples;

            UnityEngine.QualitySettings.antiAliasing = (int)samples;

            Debug.Log($"[M] ConfigManager: MSAA samples set to {samples}");
        }

        public void SetShadowResolution(ConfigAvailableSettings.ShadowResolution resolution)
        {
            configData.QualitySettings.ShadowRes = resolution;

            URPSettings.mainLightShadowmapResolution = (int)resolution;
        
        
            Debug.Log($"[M] ConfigManager: Shadow resolution set to {resolution}");
        }

        public void SetShadowCascades(ConfigAvailableSettings.ShadowCascades cascades)
        {
            configData.QualitySettings.ShadowCascades = cascades;

            URPSettings.shadowCascadeCount = (int)cascades;

            Debug.Log($"[M] ConfigManager: Shadow cascades set to {cascades}");
        }

        public void SetShadowsDistance(float distance)
        {
            configData.QualitySettings.ShadowsDistance = distance;
        
            URPSettings.shadowDistance = distance;

            Debug.Log($"[M] ConfigManager: Shadow distance set to {distance}");
        }

        public void SetLightQuality(ConfigAvailableSettings.LightQuality lightQuality)
        {
            configData.QualitySettings.LightQuality = lightQuality;
            Debug.Log($"[M] ConfigManager: Light quality set to {lightQuality}");
        }
        public void SetVolumetricLighting(bool quality)
        {
            configData.QualitySettings.VolumetricLighting = quality;
        
            Debug.Log($"[M] ConfigManager: Volumetric lighting set to {quality}");
        }

        public void SetLightDistance(float distance)
        {
            configData.QualitySettings.LightDistance = distance;
            Debug.Log($"[M] ConfigManager: Light distance set to {distance}");
        }

        public void SetAmbientOcclusion(ConfigAvailableSettings.AmbientOcclusion quality)
        {
            configData.QualitySettings.AmbientOcclusion = quality;
            Debug.Log($"[M] ConfigManager: Ambient occlusion set to {quality}");
        }

        public void SetWaterQuality(ConfigAvailableSettings.WaterQuality quality)
        {
            configData.QualitySettings.WaterQuality = quality;
            Debug.Log($"[M] ConfigManager: Water quality set to {quality}");
        }

        public void SetReflectionQuality(ConfigAvailableSettings.ReflectionQuality quality)
        {
            configData.QualitySettings.ReflectionQuality = quality;
            Debug.Log($"[M] ConfigManager: Reflection quality set to {quality}");
        }

        public void SetParticleQuality(ConfigAvailableSettings.ParticleQuality quality)
        {
            configData.QualitySettings.ParticleQuality = quality;
            Debug.Log($"[M] ConfigManager: Particle quality set to {quality}");
        }

        // Post-Processing Settings
        public void SetBloom(ConfigAvailableSettings.Bloom bloom)
        {
            bloom = ConfigAvailableSettings.Bloom.Disabled;
            configData.PostProcessingSettings.Bloom = bloom;

            if (volumeProfile.TryGet(out UnityEngine.Rendering.Universal.Bloom bloomComponent))
            {
                switch (bloom)
                {
                    case ConfigAvailableSettings.Bloom.Disabled:
                        bloomComponent.active = false;
                        break;
                    case ConfigAvailableSettings.Bloom.Low:
                        bloomComponent.active = true;
                        bloomComponent.intensity.value = 0.5f;
                        break;
                    case ConfigAvailableSettings.Bloom.Medium:
                        bloomComponent.active = true;
                        bloomComponent.intensity.value = 1.0f;
                        break;
                    case ConfigAvailableSettings.Bloom.High:
                        bloomComponent.active = true;
                        bloomComponent.intensity.value = 1.5f;
                        break;
                }
            }
            Debug.Log($"[M] ConfigManager: Bloom set to {bloom}");
        }

        public void SetMotionBlur(ConfigAvailableSettings.MotionBlur motionBlur)
        {
            motionBlur = ConfigAvailableSettings.MotionBlur.Disabled;
            configData.PostProcessingSettings.MotionBlur = motionBlur;

            if (volumeProfile.TryGet(out UnityEngine.Rendering.Universal.MotionBlur blurComponent))
            {
                switch (motionBlur)
                {
                    case ConfigAvailableSettings.MotionBlur.Disabled:
                        blurComponent.active = false;
                        break;
                    case ConfigAvailableSettings.MotionBlur.Low:
                        blurComponent.active = true;
                        blurComponent.intensity.value = 0.2f;
                        break;
                    case ConfigAvailableSettings.MotionBlur.High:
                        blurComponent.active = true;
                        blurComponent.intensity.value = 0.5f;
                        break;
                }
            }

            Debug.Log($"[M] ConfigManager: Motion blur set to {motionBlur}");
        }

        public void SetDepthOfField(ConfigAvailableSettings.DepthOfField depthOfField)
        {
            depthOfField = ConfigAvailableSettings.DepthOfField.Disabled;
            configData.PostProcessingSettings.DepthOfField = depthOfField;

            if (volumeProfile.TryGet(out UnityEngine.Rendering.Universal.DepthOfField dofComponent))
            {
                switch (depthOfField)
                {
                    case ConfigAvailableSettings.DepthOfField.Disabled:
                        dofComponent.active = false;
                        break;
                    case ConfigAvailableSettings.DepthOfField.Enabled:
                        dofComponent.active = true;
                        dofComponent.focusDistance.value = 10f;
                        break;
                }
            }

            Debug.Log($"[M] ConfigManager: Depth of field set to {depthOfField}");
        }

        // Audio Settings
        public void SetMasterVolume(float volume)
        {
            configData.AudioSettings.MasterVolume = volume;
            SoundManager.SetMasterVolume(volume);
            Debug.Log($"[M] ConfigManager: Master volume set to {volume}");
        }

        public void SetMusicVolume(float volume)
        {
            configData.AudioSettings.MusicVolume = volume;
            SoundManager.SetMusicVolume(volume);
            Debug.Log($"[M] ConfigManager: Music volume set to {volume}");
        }

        public void SetEffectsVolume(float volume)
        {
            configData.AudioSettings.EffectsVolume = volume;
            SoundManager.SetEffectsVolume(volume);
            Debug.Log($"[M] ConfigManager: Effects volume set to {volume}");
        }

        public void SetVoiceChatVolume(float volume)
        {
            configData.AudioSettings.VoiceChatVolume = volume;
            SoundManager.SetVCVolume(volume);
            Debug.Log($"[M] ConfigManager: Voice chat volume set to {volume}");
        }

        public void SetControlHintsEnabled(bool enabled)
        {
            configData.InterfaceSettings ??= new InterfaceSettings();
            configData.InterfaceSettings.ControlHintsEnabled = enabled;
            Debug.Log($"[M] ConfigManager: Control hints set to {enabled}");
        }

        #endregion

        #region Update settings methods

        public void UpdateFOV()
        {
            if (configData?.GameSettings.FOV != null)
            {
                int fov = configData.GameSettings.FOV;
                CurrentCamera.fieldOfView = fov;
                Debug.Log($"[M] ConfigManager: FOV updated to {fov}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update FOV, configData or FOV is null");
            }
        }

        public void UpdateLanguage()
        {
            if (configData?.GameSettings.Language != null)
            {
                var language = configData.GameSettings.Language;
                LocalizationManager.SetLanguage(language);
                Debug.Log($"[M] ConfigManager: Language updated to {language}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update language, configData or language is null");
            }
        }

        public void UpdateResolution()
        {
            if (configData?.GraphicsSettings.Resolution != null)
            {
                var resolution = configData.GraphicsSettings.Resolution;
                Screen.SetResolution(resolution.Width, resolution.Height, configData.GraphicsSettings.FullscreenMode);
                Debug.Log($"[M] ConfigManager: Resolution updated to {resolution.Width}x{resolution.Height}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update resolution, configData or resolution is null");
            }
        }

        public void UpdateFullscreenMode()
        {
            if (configData?.GraphicsSettings.FullscreenMode != null)
            {
                var mode = configData.GraphicsSettings.FullscreenMode;
                Screen.fullScreenMode = mode;
                Debug.Log($"[M] ConfigManager: Fullscreen mode updated to {mode}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update fullscreen mode, configData or mode is null");
            }
        }

        public void UpdateLimitRefreshRate()
        {
            if (configData?.GraphicsSettings.LimitRefreshRate != null)
            {
                SetLimitRefreshRate(true);
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update limit refresh rate, configData or limit is null");
            }
        }

        public void UpdateRefreshRate()
        {
            if (configData?.GraphicsSettings.RefreshRate != null)
            {
                SetRefreshRate(configData.GraphicsSettings.RefreshRate);
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update refresh rate, configData or refresh rate is null");
            }
        }

        public void UpdateVSync()
        {
            if (configData?.GraphicsSettings.vSync != null)
            {
                SetVSync(0);
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update VSync, configData or vSyncValue is null");
            }
        }

        public void UpdateBrightness()
        {
            if (configData?.GraphicsSettings.Brightness != null)
            {
                float brightness = configData.GraphicsSettings.Brightness;
                if (volumeProfile.TryGet<ColorAdjustments>(out var colorAdjustments))
                {
                    colorAdjustments.postExposure.value = brightness;
                }
                Debug.Log($"[M] ConfigManager: Brightness updated to {brightness}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update brightness, configData or brightness is null");
            }
        }

        public void UpdateTextureQuality()
        {
            if (configData?.QualitySettings.TextureQuality != null)
            {
                var quality = configData.QualitySettings.TextureQuality;
                UnityEngine.QualitySettings.SetTextureMipmapLimitSettings(TEXTURE_MIPMAP_GROUP_NAME, ConfigAvailableSettings.AvailableTextureQuality[quality]);
                Debug.Log($"[M] ConfigManager: Texture quality updated to {quality}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update texture quality, configData or quality is null");
            }
        }

        public void UpdateAnisotropicFiltering()
        {
            if (configData?.QualitySettings.AnisotropicFiltering != null)
            {
                var filtering = configData.QualitySettings.AnisotropicFiltering;
                if (filtering == ConfigAvailableSettings.AnisotropicFiltering.Disabled)
                {
                    UnityEngine.QualitySettings.anisotropicFiltering = UnityEngine.AnisotropicFiltering.Disable;
                }
                else
                {
                    Texture[] textures = Resources.LoadAll<Texture>("Textures");
                    foreach (Texture texture in textures)
                    {
                        if (texture != null)
                        {
                            texture.anisoLevel = Mathf.Clamp((int)filtering, 1, 16);
                        }
                    }
                }
                Debug.Log($"[M] ConfigManager: Anisotropic filtering updated to {filtering}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update anisotropic filtering, configData or filtering is null");
            }
        }
        public void UpdateLODQuality()
        {
            if (configData?.QualitySettings.LODQuality != null)
            {
                var quality = configData.QualitySettings.LODQuality;
                ApplyLODQualityToScene(quality);
                Debug.Log($"[M] ConfigManager: LOD quality updated to {quality}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update LOD quality, configData or quality is null");
            }
        }

        public void UpdateDrawDistance()
        {
            if (configData?.QualitySettings.DrawDistance != null)
            {
                float distance = configData.QualitySettings.DrawDistance;
                ApplyDrawDistanceToScene(distance);
                Debug.Log($"[M] ConfigManager: Draw distance updated to {distance}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update draw distance, configData or distance is null");
            }
        }

        private static void ApplyLODQualityToScene(ConfigAvailableSettings.LODQuality quality)
        {
            foreach (var handler in UnityEngine.Object.FindObjectsByType<LODQualityHandler>())
            {
                handler.UpdateLODQuality(quality);
            }
        }

        private static void ApplyDrawDistanceToScene(float distance)
        {
            foreach (var handler in UnityEngine.Object.FindObjectsByType<DrawDistanceHandler>())
            {
                handler.UpdateDrawDistance(distance);
            }
        }

        public void UpdateAntiAliasing()
        {
            if (configData?.QualitySettings.AntiAliasing != null)
            {
                var mode = configData.QualitySettings.AntiAliasing;
                if (mode == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                {
                    mode = AntialiasingMode.FastApproximateAntialiasing;
                    configData.QualitySettings.AntiAliasing = mode;
                }

                CameraManager.CurrentCamera.GetComponent<UniversalAdditionalCameraData>().antialiasing = mode;
                Debug.Log($"[M] ConfigManager: Anti-aliasing updated to {mode}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update anti-aliasing, configData or mode is null");
            }
        }

        public void UpdateMSAASamples()
        {
            if (configData?.QualitySettings.MSAA_Sampling != null)
            {
                var samples = ConfigAvailableSettings.MSAA_Sampling.Disabled;
                configData.QualitySettings.MSAA_Sampling = samples;
                UnityEngine.QualitySettings.antiAliasing = (int)samples;
                Debug.Log($"[M] ConfigManager: MSAA samples updated to {samples}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update MSAA samples, configData or samples is null");
            }
        }

        public void UpdateShadowResolution()
        {
            if (configData?.QualitySettings.ShadowRes != null)
            {
                var resolution = configData.QualitySettings.ShadowRes;
                URPSettings.mainLightShadowmapResolution = (int)resolution;
                Debug.Log($"[M] ConfigManager: Shadow resolution updated to {resolution}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update shadow resolution, configData or resolution is null");
            }
        }

        public void UpdateShadowCascades()
        {
            if (configData?.QualitySettings.ShadowCascades != null)
            {
                var cascades = configData.QualitySettings.ShadowCascades;
                URPSettings.shadowCascadeCount = Mathf.Clamp((int)cascades, 1, 4);
                Debug.Log($"[M] ConfigManager: Shadow cascades updated to {cascades}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update shadow cascades, configData or cascades is null");
            }
        }

        public void UpdateShadowsDistance()
        {
            if (configData?.QualitySettings.ShadowsDistance != null)
            {
                float distance = configData.QualitySettings.ShadowsDistance;
                URPSettings.shadowDistance = distance;
                Debug.Log($"[M] ConfigManager: Shadow distance updated to {distance}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update shadow distance, configData or distance is null");
            }
        }

        public void UpdateLightQuality()
        {
            if (configData?.QualitySettings.LightQuality != null)
            {
                var lightQuality = configData.QualitySettings.LightQuality;
                Debug.Log($"[M] ConfigManager: Light quality updated to {lightQuality}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update light quality, configData or quality is null");
            }
        }

        public void UpdateVolumetricLighting()
        {
            if (configData?.QualitySettings.VolumetricLighting != null)
            {
                bool volumetric = configData.QualitySettings.VolumetricLighting;
                Debug.Log($"[M] ConfigManager: Volumetric lighting updated to {volumetric}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update volumetric lighting, configData or volumetric setting is null");
            }
        }

        public void UpdateLightDistance()
        {
            if (configData?.QualitySettings.LightDistance != null)
            {
                float distance = configData.QualitySettings.LightDistance;
                Debug.Log($"[M] ConfigManager: Light distance updated to {distance}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update light distance, configData or distance is null");
            }
        }

        public void UpdateAmbientOcclusion()
        {
            if (configData?.QualitySettings.AmbientOcclusion != null)
            {
                var quality = configData.QualitySettings.AmbientOcclusion;
                Debug.Log($"[M] ConfigManager: Ambient occlusion updated to {quality}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update ambient occlusion, configData or quality is null");
            }
        }

        public void UpdateWaterQuality()
        {
            if (configData?.QualitySettings.WaterQuality != null)
            {
                var quality = configData.QualitySettings.WaterQuality;
                Debug.Log($"[M] ConfigManager: Water quality updated to {quality}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update water quality, configData or quality is null");
            }
        }

        public void UpdateReflectionQuality()
        {
            if (configData?.QualitySettings.ReflectionQuality != null)
            {
                var quality = configData.QualitySettings.ReflectionQuality;
                Debug.Log($"[M] ConfigManager: Reflection quality updated to {quality}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update reflection quality, configData or quality is null");
            }
        }

        public void UpdateParticleQuality()
        {
            if (configData?.QualitySettings.ParticleQuality != null)
            {
                var quality = configData.QualitySettings.ParticleQuality;
                Debug.Log($"[M] ConfigManager: Particle quality updated to {quality}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update particle quality, configData or quality is null");
            }
        }

        private void UpdateBloom()
        {
            if (configData == null || configData.PostProcessingSettings == null)
            {
                Debug.LogWarning("[M] ConfigManager: ConfigData or PostProcessingSettings is null. Cannot update Bloom.");
                return;
            }

            var bloom = ConfigAvailableSettings.Bloom.Disabled;
            configData.PostProcessingSettings.Bloom = bloom;
            if (volumeProfile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloomComponent))
            {
                switch (bloom)
                {
                    case ConfigAvailableSettings.Bloom.Disabled:
                        bloomComponent.active = false;
                        break;
                    case ConfigAvailableSettings.Bloom.Low:
                        bloomComponent.active = true;
                        bloomComponent.intensity.value = 0.5f;
                        break;
                    case ConfigAvailableSettings.Bloom.Medium:
                        bloomComponent.active = true;
                        bloomComponent.intensity.value = 1.0f;
                        break;
                    case ConfigAvailableSettings.Bloom.High:
                        bloomComponent.active = true;
                        bloomComponent.intensity.value = 1.5f;
                        break;
                }
            }
            Debug.Log($"[M] ConfigManager: Bloom updated to {bloom} from ConfigData.");
        }

        private void UpdateMotionBlur()
        {
            if (configData == null || configData.PostProcessingSettings == null)
            {
                Debug.LogWarning("[M] ConfigManager: ConfigData or PostProcessingSettings is null. Cannot update Motion Blur.");
                return;
            }

            var motionBlur = ConfigAvailableSettings.MotionBlur.Disabled;
            configData.PostProcessingSettings.MotionBlur = motionBlur;
            if (volumeProfile.TryGet<UnityEngine.Rendering.Universal.MotionBlur>(out var blurComponent))
            {
                switch (motionBlur)
                {
                    case ConfigAvailableSettings.MotionBlur.Disabled:
                        blurComponent.active = false;
                        break;
                    case ConfigAvailableSettings.MotionBlur.Low:
                        blurComponent.active = true;
                        blurComponent.intensity.value = 0.2f;
                        break;
                    case ConfigAvailableSettings.MotionBlur.High:
                        blurComponent.active = true;
                        blurComponent.intensity.value = 0.5f;
                        break;
                }
            }
            Debug.Log($"[M] ConfigManager: Motion Blur updated to {motionBlur} from ConfigData.");
        }

        private void UpdateDepthOfField()
        {
            if (configData == null || configData.PostProcessingSettings == null)
            {
                Debug.LogWarning("[M] ConfigManager: ConfigData or PostProcessingSettings is null. Cannot update Depth of Field.");
                return;
            }

            var depthOfField = ConfigAvailableSettings.DepthOfField.Disabled;
            configData.PostProcessingSettings.DepthOfField = depthOfField;
            if (volumeProfile.TryGet<UnityEngine.Rendering.Universal.DepthOfField>(out var dofComponent))
            {
                switch (depthOfField)
                {
                    case ConfigAvailableSettings.DepthOfField.Disabled:
                        dofComponent.active = false;
                        break;
                    case ConfigAvailableSettings.DepthOfField.Enabled:
                        dofComponent.active = true;
                        dofComponent.focusDistance.value = 10f; // Example default focus distance
                        break;
                }
            }
            Debug.Log($"[M] ConfigManager: Depth of Field updated to {depthOfField} from ConfigData.");
        }

        public void UpdateMasterVolume()
        {
            if (configData?.AudioSettings.MasterVolume != null)
            {
                float volume = configData.AudioSettings.MasterVolume;
                SoundManager.SetMasterVolume(volume);
                Debug.Log($"[M] ConfigManager: Master volume updated to {volume}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update master volume, configData or volume is null");
            }
        }

        public void UpdateMusicVolume()
        {
            if (configData?.AudioSettings.MusicVolume != null)
            {
                float volume = configData.AudioSettings.MusicVolume;
                SoundManager.SetMusicVolume(volume);
                Debug.Log($"[M] ConfigManager: Music volume updated to {volume}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update music volume, configData or volume is null");
            }
        }

        public void UpdateEffectsVolume()
        {
            if (configData?.AudioSettings.EffectsVolume != null)
            {
                float volume = configData.AudioSettings.EffectsVolume;
                SoundManager.SetEffectsVolume(volume);
                Debug.Log($"[M] ConfigManager: Effects volume updated to {volume}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update effects volume, configData or volume is null");
            }
        }

        public void UpdateVoiceChatVolume()
        {
            if (configData?.AudioSettings.VoiceChatVolume != null)
            {
                float volume = configData.AudioSettings.VoiceChatVolume;
                SoundManager.SetVCVolume(volume);
                Debug.Log($"[M] ConfigManager: Voice chat volume updated to {volume}");
            }
            else
            {
                Debug.LogWarning("[M] ConfigManager: Cannot update voice chat volume, configData or volume is null");
            }
        }

        public void UpdateControlHintsEnabled()
        {
            configData.InterfaceSettings ??= new InterfaceSettings();
            Debug.Log($"[M] ConfigManager: Control hints updated to {configData.InterfaceSettings.ControlHintsEnabled}");
        }

        #endregion
    }
}

public enum LoadSourceType
{
    Local,
    Steam
}


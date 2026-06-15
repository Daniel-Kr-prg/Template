using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DanieloZ.Config
{
    [System.Serializable]
    public class ConfigData : ConfigDataBase
    {
        public override string CFG_KEY => "CORE";

        public GameSettings GameSettings;
        public GraphicsSettings GraphicsSettings;
        public QualitySettings QualitySettings;
        public PostProcessingSettings PostProcessingSettings;
        public AudioSettings AudioSettings;
        public InterfaceSettings InterfaceSettings;

        public SerializedDictionary<string, string> customSettings = new();

        public override SerializedDictionary<string, string> GetSaveableConfigData()
        {
            SerializedDictionary<string, string> result = new SerializedDictionary<string, string>();

            GameSettings ??= new GameSettings();
            GraphicsSettings ??= new GraphicsSettings();
            QualitySettings ??= new QualitySettings();
            PostProcessingSettings ??= new PostProcessingSettings();
            AudioSettings ??= new AudioSettings();
            InterfaceSettings ??= new InterfaceSettings();

            result.AddRange(GameSettings.GetSaveableConfigData());
            result.AddRange(GraphicsSettings.GetSaveableConfigData());
            result.AddRange(QualitySettings.GetSaveableConfigData());
            result.AddRange(PostProcessingSettings.GetSaveableConfigData());
            result.AddRange(AudioSettings.GetSaveableConfigData());
            result.AddRange(InterfaceSettings.GetSaveableConfigData());

            customSettings ??= new SerializedDictionary<string, string>();
            foreach (KeyValuePair<string, string> kvp in customSettings)
            {
                result.Add($"{(kvp.Key.Contains($"{CFG_KEY}_") ? "" : CFG_KEY)}_{kvp.Key}", kvp.Value);
            }

            return result;
        }

        public override void HandleSaveableConfigData(SerializedDictionary<string, string> data)
        {
            base.HandleSaveableConfigData(data);
            HandleCustomSettings(data.Where(kvp => kvp.Key.Contains(CFG_KEY)).ToDictionary(x => x.Key.Substring($"{CFG_KEY}_".Length), x => x.Value));
        }

        void HandleCustomSettings(Dictionary<string, string> customStuff)
        {
            customSettings = customStuff;
        }
    }

    [System.Serializable]
    public class GameSettings : ConfigDataBase
    {
        public override string CFG_KEY => "GAME";

        [Range(60, 120)] public int FOV;
        [Min(0f)] public float AutoSaveMinutes = 5f;

        public ConfigAvailableSettings.Language Language;

    }


    [System.Serializable]
    public class GraphicsSettings : ConfigDataBase
    {
        public override string CFG_KEY => "VIDEO";

        public ResolutionSettings Resolution;
        public FullScreenMode FullscreenMode;
        public bool LimitRefreshRate;
        public int vSync;
        [Range(30, 240)] public int RefreshRate;
        [Range(0f, 1f)] public float Brightness;
    }

    [System.Serializable]
    public class QualitySettings : ConfigDataBase
    {
        public override string CFG_KEY => "QUALITY";

        [Header("Objects draw distance")]
        public float DrawDistance;
    
        [Header("Textures")]
        public ConfigAvailableSettings.TextureQuality TextureQuality;
        public ConfigAvailableSettings.AnisotropicFiltering AnisotropicFiltering;
        public ConfigAvailableSettings.LODQuality LODQuality;
    
        [Header("AntiAliasing")]
        public AntialiasingMode AntiAliasing;
        public ConfigAvailableSettings.MSAA_Sampling MSAA_Sampling;

        [Header("Shadows")]
        public ConfigAvailableSettings.ShadowResolution ShadowRes;
        public ConfigAvailableSettings.ShadowCascades ShadowCascades;
        public float ShadowsDistance;

        [Header("Lights")]
        public ConfigAvailableSettings.LightQuality LightQuality;
        public bool VolumetricLighting;
        public float LightDistance;
        public ConfigAvailableSettings.AmbientOcclusion AmbientOcclusion;

        [Header("Water")]
        public ConfigAvailableSettings.WaterQuality WaterQuality;

        [Header("Reflections")]
        public ConfigAvailableSettings.ReflectionQuality ReflectionQuality;

        [Header("Particles")]
        public ConfigAvailableSettings.ParticleQuality ParticleQuality;

    }

    [System.Serializable]
    public class PostProcessingSettings : ConfigDataBase
    {
        public override string CFG_KEY => "POST-PROCESSING";

        public ConfigAvailableSettings.Bloom Bloom { get; set; }
        public ConfigAvailableSettings.MotionBlur MotionBlur { get; set; }
        public ConfigAvailableSettings.DepthOfField DepthOfField { get; set; }
    }

    [System.Serializable]
    public class ResolutionSettings : ConfigDataBase
    {
        public override string CFG_KEY => "RES";

        public int Width;
        public int Height;

        public int W_Scale;
        public int H_Scale;
    }

    [System.Serializable]
    public class AudioSettings : ConfigDataBase
    {
        public override string CFG_KEY => "AUDIO";

        [Range(0f, 1f)] public float MasterVolume;
        [Range(0f, 1f)] public float MusicVolume;
        [Range(0f, 1f)] public float EffectsVolume;
        [Range(0f, 1f)] public float VoiceChatVolume;
    }

    [System.Serializable]
    public class InterfaceSettings : ConfigDataBase
    {
        public override string CFG_KEY => "UI";

        public bool ControlHintsEnabled = true;
    }



    public abstract class ConfigDataBase
    {
        public abstract string CFG_KEY { get; }

        public virtual SerializedDictionary<string, string> GetSaveableConfigData()
        {
            var dictionary = new SerializedDictionary<string, string>();

            var fields = this.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance);

            //.Where(field => field.IsDefined(typeof(RangeAttribute), true) || field.FieldType == typeof(string))

            foreach (var field in fields)
            {
                string key = $"{CFG_KEY}_{field.Name}";
                object value = field.GetValue(this);

                if (value != null)
                {
                    if (value is ConfigDataBase nestedConfigData)
                    {
                        var nestedData = nestedConfigData.GetSaveableConfigData();
                        dictionary.AddRange(nestedData);
                    }
                    else
                    {
                        dictionary[key] = value.ToString();
                    }
                }
            }

            return dictionary;
        }

        public virtual void HandleSaveableConfigData(SerializedDictionary<string, string> data)
        {
            var fields = this.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance);//.Where(field => field.IsDefined(typeof(RangeAttribute), true) || field.FieldType == typeof(string));

            foreach (var field in fields)
            {
                if (typeof(ConfigDataBase).IsAssignableFrom(field.FieldType))
                {
                    if (field.GetValue(this) is not ConfigDataBase nestedConfigData)
                    {
                        nestedConfigData = (ConfigDataBase)Activator.CreateInstance(field.FieldType);

                        field.SetValue(this, nestedConfigData);
                    }

                    nestedConfigData.HandleSaveableConfigData(data);
                }
                else
                {
                    string key = $"{CFG_KEY}_{field.Name}";

                    if (data.TryGetValue(key, out string stringValue))
                    {
                        try
                        {
                            if (field.FieldType == typeof(float))
                            {
                                field.SetValue(this, float.Parse(stringValue));
                            }
                            else if (field.FieldType == typeof(string))
                            {
                                field.SetValue(this, stringValue);
                            }
                            else if (field.FieldType == typeof(int))
                            {
                                field.SetValue(this, int.Parse(stringValue));
                            }
                            else if (field.FieldType == typeof(bool))
                            {
                                field.SetValue(this, bool.Parse(stringValue));
                            }
                            else if (field.FieldType.IsEnum)
                            {
                                var enumValue = Enum.Parse(field.FieldType, stringValue);
                                field.SetValue(this, enumValue);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Failed to parse config field {field.Name}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

}


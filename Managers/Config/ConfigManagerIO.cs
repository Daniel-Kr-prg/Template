using DanieloZ.Config;
using DanieloZ.Managers;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace DanieloZ.Managers.Config
{
    [RequireComponent(typeof(ConfigManager))]
    public class ConfigManagerIO : MonoBehaviour
    {
        private static string defaultConfigKey = "DefaultConfig";

        public static void LoadDefaultConfig(ConfigData cfg, Action onSuccess = null)
        {
            AddressablesManager.LoadAssetAsync<TextAsset>(defaultConfigKey, (textAsset) =>
            {
                if (textAsset != null)
                {
                    try
                    {
                        var dict = JsonConvert.DeserializeObject<SerializedDictionary<string, string>>(textAsset.text);
                        cfg.HandleSaveableConfigData(dict);
                        SaveConfig_Local(ImportantFilepaths.SettingsConfigPath, cfg);
                        ConfigManager.Instance.DebugMessage("Default config loaded from Addressables.");
                    }
                    catch (Exception ex)
                    {
                        ConfigManager.Instance.DebugWarning($"Error deserializing default config: {ex.Message}");
                    }
                }
                else
                {
                    ConfigManager.Instance.DebugWarning("Default config TextAsset is null.");
                }
            });
        }

#if UNITY_EDITOR
        public static void SaveToDefaultConfig(ConfigData cfg)
        {
            if (cfg == null)
            {
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(cfg, Formatting.Indented);

                File.WriteAllText("Assets/Settings/DefaultConfig.txt", json);
            }
            catch (Exception ex)
            {
                ConfigManager.Instance.DebugError($"Error serializing ConfigData: {ex.Message}");
            }
        }
#endif

        public static void SaveConfig_File(ConfigData cfg, Action<TextAsset> onSuccess, Action<string> onFailure)
        {
            if (cfg == null)
            {
                onFailure?.Invoke("ConfigData is null. Cannot save.");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    string json = JsonConvert.SerializeObject(cfg, Formatting.Indented);
                    await Task.Yield();
                    TextAsset textAsset = new TextAsset(json);
                    onSuccess?.Invoke(textAsset);
                }
                catch (Exception ex)
                {
                    onFailure?.Invoke($"Error serializing ConfigData: {ex.Message}");
                }
            });
        }
        public static void LoadConfig_File(TextAsset cfgFile, ConfigData cfg, Action onSuccess = null, Action<string> onFailure = null)
        {
            if (cfgFile == null)
            {
                onFailure?.Invoke("TextAsset is null. Cannot load.");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    await Task.Yield();
                    var dict = JsonConvert.DeserializeObject<SerializedDictionary<string, string>>(cfgFile.text);
                    cfg.HandleSaveableConfigData(dict);
                    onSuccess?.Invoke();
                }
                catch (Exception ex)
                {
                    onFailure?.Invoke($"Error deserializing ConfigData: {ex.Message}");
                }
            });
        }


        public static void SaveConfig_Local(string path, ConfigData cfg, Action onSuccess = null, Action<string> onFailure = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                onFailure?.Invoke("Path is null or empty. Cannot save.");
                return;
            }

            if (cfg == null)
            {
                onFailure?.Invoke("ConfigData is null. Cannot save.");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    string directory = Path.GetDirectoryName(path);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string json = JsonConvert.SerializeObject(cfg.GetSaveableConfigData(), Formatting.Indented);
                    await File.WriteAllTextAsync(path, json);
                    onSuccess?.Invoke();
                }
                catch (Exception ex)
                {
                    onFailure?.Invoke($"Error saving ConfigData to file: {ex.Message}");
                }
            });
        }

        public static void LoadConfig_Local(string path, ConfigData cfg, Action onSuccess = null, Action<string> onFailure = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                onFailure?.Invoke("Path is null or empty. Cannot load.");
                return;
            }

            if (!File.Exists(path))
            {
                LoadDefaultConfig(cfg, onSuccess);
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    string json = await File.ReadAllTextAsync(path);
                    var dict = JsonConvert.DeserializeObject<SerializedDictionary<string, string>>(json);
                    cfg.HandleSaveableConfigData(dict);
                    onSuccess?.Invoke();
                }
                catch (Exception ex)
                {
                    onFailure?.Invoke($"Error loading ConfigData from file: {ex.Message}");
                }
            });
        }

        public static bool LocalConfigExists()
        {
            return File.Exists(ImportantFilepaths.SettingsConfigPath);
        }
    }
}

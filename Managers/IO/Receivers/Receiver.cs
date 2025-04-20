using Newtonsoft.Json;
using Steamworks;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class FileReceiver<T>
{
    public abstract T LoadFile();
    public abstract void SaveFile(T objectToSave);

    public abstract void LoadFileAsync(Action<T> onSuccess, Action<string> onFailure);
    public abstract void SaveFileAsync(T objectToSave, Action onSuccess, Action<string> onFailure);

    public virtual bool FileExists() { return true; }
}

public class FileReceiver_Steam<T> : FileReceiver<T>
{
    protected string path;

    public FileReceiver_Steam(string path)
    {
        this.path = path;
    }

    public override bool FileExists()
    {
        return SteamRemoteStorage.FileExists(path);
    }

    public override T LoadFile()
    {
        if (!SteamRemoteStorage.FileExists(path))
        {
            SteamManager.Instance.DebugError($"[Steam] File '{path}' does not exist in Steam Cloud.");
            return default(T);
        }

        byte[] buffer;
        buffer = SteamRemoteStorage.FileRead(path);
        string json = Encoding.UTF8.GetString(buffer);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public override void SaveFile(T objectToSave)
    {
        string json = JsonConvert.SerializeObject(objectToSave, Formatting.Indented);
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        bool success = SteamRemoteStorage.FileWrite(path, buffer);
        if (!success)
        {
            SteamManager.Instance.DebugError($"[Steam] Failed to save file '{path}' to Steam Cloud.");
        }
    }

    public override void LoadFileAsync(Action<T> onSuccess, Action<string> onFailure)
    {
        Task.Run(() =>
        {
            try
            {
                if (!SteamRemoteStorage.FileExists(path))
                {
                    onFailure?.Invoke($"[Steam] File '{path}' does not exist in Steam Cloud.");
                    return;
                }

                byte[] buffer;
                buffer = SteamRemoteStorage.FileRead(path);
                string json = Encoding.UTF8.GetString(buffer);
                T result = JsonConvert.DeserializeObject<T>(json);
                onSuccess?.Invoke(result);
            }
            catch (Exception ex)
            {
                onFailure?.Invoke($"[Steam] Exception during LoadFileAsync: {ex.Message}");
            }
        });
    }

    public override void SaveFileAsync(T objectToSave, Action onSuccess, Action<string> onFailure)
    {
        Task.Run(() =>
        {
            try
            {
                string json = JsonConvert.SerializeObject(objectToSave, Formatting.Indented);
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                bool success = SteamRemoteStorage.FileWrite(path, buffer);
                Debug.Log("KEYS MAP DEFAULT SAVED");
                if (success)
                {
                    onSuccess?.Invoke();
                }
                else
                {
                    onFailure?.Invoke($"[Steam] Failed to save file '{path}' to Steam Cloud.");
                }
            }
            catch (Exception ex)
            {
                onFailure?.Invoke($"[Steam] Exception during SaveFileAsync: {ex.Message}");
            }
        });
    }
}

public class FileReceiver_Local<T> : FileReceiver<T>
{
    protected string path;

    public FileReceiver_Local(string path)
    {
        this.path = path;
    }

    public override T LoadFile()
    {
        return IOManager.LoadLocalJSON<T>(path);
    }

    public override void SaveFile(T objectToSave)
    {
        IOManager.SaveLocalJSON(path, objectToSave);
    }

    public override void LoadFileAsync(Action<T> onSuccess, Action<string> onFailure)
    {
        Task.Run(() =>
        {
            try
            {
                T result = IOManager.LoadLocalJSON<T>(path);
                onSuccess?.Invoke(result);
            }
            catch (Exception ex)
            {
                onFailure?.Invoke($"Error loading file from '{path}': {ex.Message}");
            }
        });
    }

    public override void SaveFileAsync(T objectToSave, Action onSuccess, Action<string> onFailure)
    {
        Task.Run(() =>
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                IOManager.SaveLocalJSON(path, objectToSave);
                onSuccess?.Invoke();
            }
            catch (Exception ex)
            {
                onFailure?.Invoke($"Error saving file to '{path}': {ex.Message}");
            }
        });
    }

    public override bool FileExists()
    {
        return File.Exists(path);
    }
}

public class FileReceiver_Combined<T> : FileReceiver<T>
{
    private FileReceiver_Local<T> localReceiver;
    private FileReceiver_Steam<T> steamReceiver;

    public FileReceiver_Combined(string localPath, string steamPath)
    {
        localReceiver = new FileReceiver_Local<T>(localPath);
        steamReceiver = new FileReceiver_Steam<T>(steamPath);
    }

    public override bool FileExists()
    {
        if (SteamManager.Instance.IsInitialized)
            return steamReceiver.FileExists();
        else
            return localReceiver.FileExists();
    }

    public override T LoadFile()
    {
        if (SteamManager.Instance.IsInitialized)
        {
            if (steamReceiver.FileExists())
            {
                T config = steamReceiver.LoadFile();
                localReceiver.SaveFile(config);
                return config;
            }
            else
            {
                if (localReceiver.FileExists())
                {
                    T config = localReceiver.LoadFile();
                    steamReceiver.SaveFile(config);
                    return config;
                }
                else
                {
                    return default(T);
                }
            }
        }
        else
        {
            return localReceiver.LoadFile();
        }
    }

    public override void SaveFile(T objectToSave)
    {
        localReceiver.SaveFile(objectToSave);
        if (SteamManager.Instance.IsInitialized)
        {
            steamReceiver.SaveFile(objectToSave);
        }
    }

    public override void LoadFileAsync(Action<T> onSuccess, Action<string> onFailure)
    {
        if (SteamManager.Instance.IsInitialized)
        {
            steamReceiver.LoadFileAsync(
                (steamConfig) =>
                {
                    localReceiver.SaveFileAsync(steamConfig,
                        () => onSuccess?.Invoke(steamConfig),
                        (err) =>
                        {
                            Debug.LogWarning($"Local sync error: {err}");
                            onSuccess?.Invoke(steamConfig);
                        });
                },
                (err) =>
                {
                    if (!steamReceiver.FileExists())
                    {
                        if (localReceiver.FileExists())
                        {
                            localReceiver.LoadFileAsync(
                                (localConfig) =>
                                {
                                    steamReceiver.SaveFileAsync(localConfig,
                                        () => onSuccess?.Invoke(localConfig),
                                        (saveErr) =>
                                        {
                                            Debug.LogWarning($"Steam save error: {saveErr}");
                                            onSuccess?.Invoke(localConfig);
                                        });
                                },
                                onFailure);
                        }
                        else
                        {
                            onFailure?.Invoke("No config file exists in Steam Cloud or locally.");
                        }
                    }
                    else
                    {
                        onFailure?.Invoke(err);
                    }
                });
        }
        else
        {
            localReceiver.LoadFileAsync(onSuccess, onFailure);
        }
    }

    public override void SaveFileAsync(T objectToSave, Action onSuccess, Action<string> onFailure)
    {
        localReceiver.SaveFileAsync(objectToSave,
            () =>
            {
                if (SteamManager.Instance.IsInitialized)
                {
                    steamReceiver.SaveFileAsync(objectToSave,
                        () => onSuccess?.Invoke(),
                        (err) =>
                        {
                            Debug.LogWarning($"Failed to save config to Steam: {err}");
                            onSuccess?.Invoke();
                        });
                }
                else
                {
                    onSuccess?.Invoke();
                }
            },
            onFailure);
    }
}
#if UNITY_STANDALONE || UNITY_EDITOR
using Newtonsoft.Json;
using Steamworks;
using System;
using UnityEngine;

public static class SteamManager_IO
{
    public static bool IsCloudAvailable()
    {
        return SteamRemoteStorage.IsCloudEnabled && SteamRemoteStorage.IsCloudEnabledForApp;
    }

    public static void SaveToSteamCloud(string fileName, object savingObject)
    {
        if (savingObject == null)
        {
            Debug.LogError("[SteamManager_IO] SaveSteamCloud: savingObject is null.");
            return;
        }

        if (!IsCloudAvailable())
        {
            Debug.LogWarning("[SteamManager_IO] Steam Cloud is not available.");
            return;
        }

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(savingObject, Newtonsoft.Json.Formatting.Indented);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

        bool success = SteamRemoteStorage.FileWrite(fileName, data);
        if (!success)
        {
            Debug.LogError($"[SteamManager_IO] Failed to save file {fileName} to Steam Cloud");
        }
        else
        {
            Debug.Log($"[SteamManager_IO] File {fileName} saved to Steam Cloud");
        }
    }

    public static T LoadFromSteamCloud<T>(string fileName)
    {
        if (!IsCloudAvailable())
        {
            Debug.LogWarning("[SteamManager_IO] Steam Cloud is not available.");
            return default;
        }

        if (!SteamRemoteStorage.FileExists(fileName))
        {
            Debug.LogWarning($"[SteamManager_IO] File {fileName} not found in Steam Cloud");
            return default;
        }

        //int fileSize = SteamRemoteStorage.FileSize(fileName);
        //if (fileSize <= 0)
        //{
        //    Debug.LogWarning($"[SteamManager_IO] File {fileName} is empty or error reading size");
        //    return default;
        //}

        byte[] buffer = SteamRemoteStorage.FileRead(fileName);

        string json = System.Text.Encoding.UTF8.GetString(buffer);
        try
        {
            T obj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            return obj;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SteamManager_IO] Failed to deserialize {fileName}: {e.Message}");
            return default;
        }
    }

    public static bool DeleteSteamCloudFile(string fileName)
    {
        if (!IsCloudAvailable())
        {
            Debug.LogWarning("[SteamManager_IO] Steam Cloud is not available.");
            return false;
        }

        if (!SteamRemoteStorage.FileExists(fileName))
        {
            Debug.LogWarning($"[SteamManager_IO] Cannot delete file {fileName}, it does not exist");
            return false;
        }

        bool success = SteamRemoteStorage.FileDelete(fileName);
        if (!success)
        {
            Debug.LogError($"[SteamManager_IO] Failed to delete file {fileName} from Steam Cloud");
        }
        else
        {
            Debug.Log($"[SteamManager_IO] Deleted file {fileName} from Steam Cloud");
        }
        return success;
    }
}
#else
public static class SteamManager_IO
{
    public static bool IsCloudAvailable() => false;

    public static void SaveToSteamCloud(string fileName, object savingObject)
    {
    }

    public static T LoadFromSteamCloud<T>(string fileName) => default;

    public static bool DeleteSteamCloudFile(string fileName) => false;
}
#endif

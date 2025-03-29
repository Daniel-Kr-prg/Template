using DanieloZ.Config;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class IOManager : SingletonManager<IOManager>
{
    private void Start()
    {
        // Additional handling before stage changing

        // Satisfy stage condition
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_IOManagerReady");
    }

    // Local JSON
    public static void SaveLocalJSON(string path, object objectToSave)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[M] IOManager / SaveLocal: path is null.");
            return;
        }

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonConvert.SerializeObject(objectToSave, Formatting.Indented);
            File.WriteAllText(path, json);
            Debug.Log($"[M] IOManager / SaveLocal: saved to {path}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[M] IOManager / SaveLocal: {ex.Message}");
        }
    }
    public static T LoadLocalJSON<T>(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[M] IOManager / LoadLocal: path is null.");
            return default;
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"[M] IOManager / LoadLocal: file does not exist.");
            return default;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[M] IOManager / LoadLocal: {ex.Message}");
            return default;
        }
    }

    // File JSON
    public static TextAsset SaveFileJSON(object objectToSave)
    {
        if (objectToSave == null)
        {
            Debug.LogError("[M] IOManager / SaveFile: object is null.");
            return default;
        }

        try
        {
            string json = JsonConvert.SerializeObject(objectToSave, Formatting.Indented);
            TextAsset file = new TextAsset(json);
            return file;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[M] IOManager / SaveFile: {ex.Message}");
            return default;
        }
    }
    public static T LoadFileJSON<T>(TextAsset file)
    {
        if (file == null)
        {
            Debug.LogError("[M] IOManager / LoadFile: file is null.");
            return default;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(file.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[M] IOManager / LoadFile: {ex.Message}");
            return default;
        }
    }

    // from JSON string to T class

    public static string SaveStringJSON(object objectToSave)
    {
        if (objectToSave == null)
        {
            Debug.LogError("[M] IOManager / SaveFile: object is null.");
            return default;
        }

        try
        {
            string json = JsonConvert.SerializeObject(objectToSave, Formatting.Indented);
            return json;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[M] IOManager / SaveStringJSON: {ex.Message}");
            return default;
        }
    }

    public static T LoadStringJSON<T>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[M] IOManager / LoadStringJSON: {ex.Message}");
            return default;
        }
    }

    // Local Binary Formatter
    public static void SaveLocalBinary(string path, object objectToSave)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[M] IOManager / SaveLocalBinary: path is null.");
            return;
        }

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fileStream, objectToSave);
            }

            Debug.Log($"[M] IOManager / SaveLocalBinary: saved to {path}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[M] IOManager / SaveLocalBinary: {ex.Message}");
        }
    }

    public static T LoadLocalBinary<T>(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[M] IOManager / LoadLocalBinary: path is null.");
            return default;
        }

        if (!File.Exists(path))
        {
            Debug.LogError("[M] IOManager / LoadLocalBinary: file does not exist.");
            return default;
        }

        try
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(fileStream);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[M] IOManager / LoadLocalBinary: {ex.Message}");
            return default;
        }
    }

    // File Binary Formatter
    public static TextAsset SaveFileBinary(object objectToSave)
    {
        if (objectToSave == null)
        {
            Debug.LogError("[M] IOManager / SaveFileBinary: object is null.");
            return null;
        }

        try
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, objectToSave);

                byte[] data = memoryStream.ToArray();
                string base64String = Convert.ToBase64String(data);
                return new TextAsset(base64String);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[M] IOManager / SaveFileBinary: {ex.Message}");
            return null;
        }
    }

    public static T LoadFileBinary<T>(TextAsset file)
    {
        if (file == null)
        {
            Debug.LogError("[M] IOManager / LoadFileBinary: file is null.");
            return default;
        }

        try
        {
            byte[] data = Convert.FromBase64String(file.text);
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(memoryStream);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[M] IOManager / LoadFileBinary: {ex.Message}");
            return default;
        }
    }
}

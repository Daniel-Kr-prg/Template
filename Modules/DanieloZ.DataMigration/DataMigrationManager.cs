using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataMigrationManager : SingletonManager<DataMigrationManager>
{
    private const string TEMP_FILE_PATH = "Temp/DataMigrationBackup.json";

    public HashSet<string> CollectAllGUIDs(IRecoverItem exclude = null)
    {
        var result = new HashSet<string>();

        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                IRecoverItem[] items = root.GetComponentsInChildren<IRecoverItem>(true);
                foreach (var item in items)
                {
                    // Если нужно исключить текущий объект (чтобы не считать его GUID "занятым")
                    // делаем проверку
                    if (exclude != null && item == exclude)
                        continue;

                    string guid = item.GetGUID();
                    if (!string.IsNullOrEmpty(guid))
                    {
                        result.Add(guid);
                    }
                }
            }
        }
        return result;
    }

    public void CollectRecoveringData()
    {
        DebugMessage("CollectRecoveringData started.");

        var recoverDataList = new List<RecoverData>();

        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObj in rootObjects)
            {
                IRecoverItem[] items = rootObj.GetComponentsInChildren<IRecoverItem>(true);
                foreach (var item in items)
                {
                    recoverDataList.Add(item.GetState());
                }
            }
        }

        var container = new MigrationContainer { Items = recoverDataList };
        string json = JsonUtility.ToJson(container, true);

        SaveToTempFile(json);
        DebugMessage($"CollectRecoveringData completed. Saved to {TEMP_FILE_PATH}");
    }

    public void RecoverData()
    {
        DebugMessage("RecoverData started.");

        string json = LoadFromTempFile();
        if (string.IsNullOrEmpty(json))
        {
            DebugWarning("No data found to recover from.");
            return;
        }

        var container = JsonUtility.FromJson<MigrationContainer>(json);
        if (container?.Items == null)
        {
            DebugWarning("Container is null or empty.");
            return;
        }

        var dataByGuid = new Dictionary<string, RecoverData>();
        foreach (var rd in container.Items)
        {
            if (!string.IsNullOrEmpty(rd.GUID))
            {
                dataByGuid[rd.GUID] = rd;
            }
        }

        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObj in rootObjects)
            {
                IRecoverItem[] items = rootObj.GetComponentsInChildren<IRecoverItem>(true);
                foreach (var item in items)
                {
                    string guid = item.GetGUID();
                    if (dataByGuid.TryGetValue(guid, out RecoverData rd))
                    {
                        item.SetState(rd);
                    }
                }
            }
        }

        DebugMessage("RecoverData completed.");
    }

    private void SaveToTempFile(string json)
    {
        string path = Path.Combine(Application.dataPath, "../" + TEMP_FILE_PATH);
        File.WriteAllText(path, json);
    }

    private string LoadFromTempFile()
    {
        string path = Path.Combine(Application.dataPath, "../" + TEMP_FILE_PATH);
        if (File.Exists(path))
            return File.ReadAllText(path);
        return null;
    }

    public static object ConvertStringToType(string valueStr, Type fieldType)
    {
        if (fieldType == typeof(string))
        {
            return valueStr;
        }
        else if (fieldType == typeof(bool))
        {
            if (bool.TryParse(valueStr, out bool bVal))
                return bVal;
            return false;
        }
        else if (fieldType == typeof(int))
        {
            if (int.TryParse(valueStr, out int iVal))
                return iVal;
            return 0;
        }
        else if (fieldType == typeof(float))
        {
            if (float.TryParse(valueStr, out float fVal))
                return fVal;
            return 0f;
        }
        else if (fieldType.IsEnum)
        {
            try
            {
                return Enum.Parse(fieldType, valueStr);
            }
            catch { }
        }

        return null;
    }

    public static string ConvertTypeToString(object obj)
    {
        if (obj == null) return "";

        Type t = obj.GetType();
        if (t.IsEnum)
            return obj.ToString();
        if (t == typeof(bool) || t == typeof(int) || t == typeof(float) || t == typeof(string))
            return obj.ToString();

        return obj.ToString();
    }
}

[Serializable]
public class MigrationContainer
{
    public List<RecoverData> Items;
}

public interface IRecoverItem
{
    RecoverData GetState();
    string GetGUID();
    void SetState(RecoverData data);
}

[Serializable]
public class RecoverData
{
    public string GUID;
    public Dictionary<string, string> Data = new Dictionary<string, string>();
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class RecoverAsAttribute : Attribute
{
    public string[] OldNames { get; private set; }

    public RecoverAsAttribute(params string[] oldNames)
    {
        OldNames = oldNames;
    }
}
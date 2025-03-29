#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using Newtonsoft.Json.Linq;

public static class SyncExecutor
{
    [MenuItem("Template/Run Full Template Sync")]
    public static void RunFullSync()
    {
        CopyFolder("Assets/Template/.Sync/Settings", "Assets/Settings");
        CopyFolder("Assets/Template/.Sync/AddressableAssetsData", "Assets/AddressableAssetsData");
        SyncProjectSettings();
        SyncPackages();
    }

    static void CopyFolder(string sourcePath, string targetPath)
    {
        if (!Directory.Exists(sourcePath))
        {
            Debug.LogWarning($"⚠️ Source folder missing: {sourcePath}");
            return;
        }

        foreach (string filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            string relativePath = filePath.Substring(sourcePath.Length + 1);
            string destPath = Path.Combine(targetPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            File.Copy(filePath, destPath, true);
        }

        Debug.Log($"📁 Copied from {sourcePath} to {targetPath}");
    }

    static void SyncProjectSettings()
    {
        string source = "Assets/Template/.Sync/ProjectSettings/";
        string target = "ProjectSettings/";

        if (!Directory.Exists(source))
        {
            Debug.LogWarning("⚠️ No ProjectSettings to sync.");
            return;
        }

        foreach (var file in Directory.GetFiles(source, "*.asset"))
        {
            string dest = Path.Combine(target, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        Debug.Log("✅ ProjectSettings synced.");
    }

    static void SyncPackages()
    {
        string sourceManifest = "Assets/Template/.Sync/Packages/manifest.json";
        string targetManifest = "Packages/manifest.json";

        if (!File.Exists(sourceManifest) || !File.Exists(targetManifest))
        {
            Debug.LogError("❌ Missing manifest.json.");
            return;
        }

        JObject src = JObject.Parse(File.ReadAllText(sourceManifest));
        JObject dst = JObject.Parse(File.ReadAllText(targetManifest));

        JObject srcDeps = (JObject)src["dependencies"];
        JObject dstDeps = (JObject)dst["dependencies"];

        bool changed = false;

        foreach (var pair in srcDeps)
        {
            if (dstDeps[pair.Key] == null)
            {
                dstDeps[pair.Key] = pair.Value;
                Debug.Log($"📦 Added: {pair.Key} → {pair.Value}");
                changed = true;
            }
        }

        if (changed)
        {
            File.WriteAllText(targetManifest, dst.ToString());
            Debug.Log("✅ Packages manifest updated.");
        }
        else
        {
            Debug.Log("🟢 All packages already present.");
        }

        string srcLock = "Assets/Template/.Sync/Packages/packages-lock.json";
        string dstLock = "Packages/packages-lock.json";

        if (File.Exists(srcLock))
        {
            File.Copy(srcLock, dstLock, true);
            Debug.Log("🔒 packages-lock.json updated.");
        }

        AssetDatabase.Refresh();
    }
}
#endif
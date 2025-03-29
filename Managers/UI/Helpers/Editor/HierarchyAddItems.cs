using System.IO;
using UnityEditor;
using UnityEngine;

//[CreateAssetMenu(
//        fileName = "Prefab Creator",
//        menuName = "Hierarchy Helpers/Creator",
//        order = 1)]
public class HierarchyAddItems/* : ScriptableObject*/
{
    //public static GameObject PagePrefab;

    //[MenuItem("DanieloZ UI/Page", false, 10)]
    //public static void CreatePage()
    //{
    //    if (PagePrefab == null)
    //    {
    //        Debug.LogError("Can't find prefab");
    //        return;
    //    }

    //    GameObject page = Object.Instantiate(PagePrefab);
    //    page.name = PagePrefab.name;

    //    Selection.activeObject = page;

    //    Undo.RegisterCreatedObjectUndo(page, $"Create page");
    //}

    private const string PrefabName = "Page";

    [MenuItem("GameObject/DanieloZ UI/Page", false, 10)]
    public static void CreatePage()
    {
        string[] guids = AssetDatabase.FindAssets($"{PrefabName} t:Prefab");

        if (guids.Length == 0)
        {
            Debug.LogError($"Prefab '{PrefabName}.prefab' не найден в проекте!");
            return;
        }

        string prefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"Ошибка загрузки префаба: {prefabPath}");
            return;
        }
        Transform parent = Selection.activeTransform;
        GameObject page = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        if (parent != null)
        {
            page.transform.SetParent(parent, false);
        }

        page.name = prefab.name;

        Selection.activeObject = page;

        Undo.RegisterCreatedObjectUndo(page, "Create Page");
    }
}

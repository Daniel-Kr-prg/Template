using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LayeredAchievementWindow : EditorWindow
{
    // Reference to our ScriptableObject asset
    private AchievementConstruction loadedAsset;

    // Internal AchievementGenerator (not in the scene)
    private AchievementGenerator generatorInstance;

    // For drawing the hierarchy
    private Vector2 scrollPos;
    private Object selectedObject;

    // Node structure for hierarchy
    private class Node
    {
        public Object obj;
        public List<Node> children = new List<Node>();
        public bool expanded = true;
    }
    private Node rootNode;

    [MenuItem("Window/Layered Achievement")]
    private static void ShowWindow()
    {
        var wnd = GetWindow<LayeredAchievementWindow>("Layered Achievement");
        wnd.Show();
    }

    private void OnEnable()
    {
        // Create an instance of AchievementGenerator in memory
        generatorInstance = new AchievementGenerator();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        // Field to load or select the ScriptableObject asset
        loadedAsset = (AchievementConstruction)EditorGUILayout.ObjectField("Achievement Asset", loadedAsset, typeof(AchievementConstruction), false);

        // If we have an asset loaded, show the hierarchy
        if (loadedAsset != null)
        {
            // Build or rebuild the hierarchy
            if (rootNode == null)
            {
                BuildHierarchy();
            }

            // Button to rebuild if needed
            if (GUILayout.Button("Rebuild Hierarchy"))
            {
                BuildHierarchy();
            }

            // Draw hierarchy
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            if (rootNode != null)
            {
                DrawNode(rootNode, 0);
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("Select or create a LayeredAchievementAsset to see the hierarchy.");
        }
    }

    /// <summary>
    /// Build the tree starting from loadedAsset.rootReceiver
    /// </summary>
    private void BuildHierarchy()
    {
        if (loadedAsset.rootReceiver == null)
        {
            rootNode = null;
            return;
        }

        rootNode = new Node { obj = loadedAsset.rootReceiver };
        BuildNodeRecursive(rootNode);
    }

    private void BuildNodeRecursive(Node node)
    {
        // If node is AG_TexReceiver_Concat or Multiply, etc., add children
        if (node.obj is AG_TexReceiver_Concat concat)
        {
            foreach (var childReceiver in concat.receivers)
            {
                var childNode = new Node { obj = childReceiver };
                node.children.Add(childNode);
                BuildNodeRecursive(childNode);
            }
        }
        else if (node.obj is AG_TexReceiver_Multiply multiply)
        {
            foreach (var childReceiver in multiply.receivers)
            {
                var childNode = new Node { obj = childReceiver };
                node.children.Add(childNode);
                BuildNodeRecursive(childNode);
            }
        }
        else if (node.obj is AG_TexReceiver_Default def)
        {
            // If there's a layer inside, add it as a child
            var layer = def.GetComponent<AG_Layer>();
            if (layer != null)
            {
                var childNode = new Node { obj = layer };
                node.children.Add(childNode);
                BuildNodeRecursive(childNode);
            }
        }
        else if (node.obj is AG_Layer layer)
        {
            // Potentially no children if it's a single layer
        }
    }

    /// <summary>
    /// Draw a node (foldout, name, etc.)
    /// </summary>
    private void DrawNode(Node node, int indent)
    {
        if (node.obj == null) return;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(indent * 15);

        // Foldout
        if (node.children.Count > 0)
        {
            node.expanded = EditorGUILayout.Foldout(node.expanded, node.obj.name, true);
        }
        else
        {
            GUILayout.Label(node.obj.name);
        }

        // "Select" button
        if (GUILayout.Button("Select", GUILayout.Width(50)))
        {
            selectedObject = node.obj;
        }

        // Right-click (Context menu)
        Rect lastRect = GUILayoutUtility.GetLastRect();
        Event e = Event.current;
        if (e.type == EventType.ContextClick && lastRect.Contains(e.mousePosition))
        {
            GenericMenu menu = new GenericMenu();
            // For example: Add new receiver
            menu.AddItem(new GUIContent("Add Receiver"), false, () => OnAddReceiver(node));
            // For example: Add new layer
            menu.AddItem(new GUIContent("Add Layer"), false, () => OnAddLayer(node));
            menu.ShowAsContext();
            e.Use();
        }

        EditorGUILayout.EndHorizontal();

        if (node.expanded)
        {
            foreach (var child in node.children)
            {
                DrawNode(child, indent + 1);
            }
        }
    }

    private void OnAddReceiver(Node node)
    {
        // Example: create a new AG_TexReceiver_Default in memory
        // You can also create Concat/Multiply, etc.
        var newReceiver = CreateNewReceiver<AG_TexReceiver_Default>("NewReceiver");

        // If node.obj is Concat or Multiply, we add newReceiver to their list
        if (node.obj is AG_TexReceiver_Concat concat)
        {
            var receiversList = new List<AG_TexReceiver_Default>(concat.receivers);
            receiversList.Add(newReceiver);
            concat.receivers = receiversList;
        }
        else if (node.obj is AG_TexReceiver_Multiply multiply)
        {
            var receiversList = new List<AG_TexReceiver_Default>(multiply.receivers);
            receiversList.Add(newReceiver);
            multiply.receivers = receiversList;
        }
        // If node.obj is a single default receiver, you might need to convert it to Concat or handle differently

        // Rebuild
        BuildHierarchy();
    }

    private void OnAddLayer(Node node)
    {
        // Create new AG_Layer-based object. We assume it attaches to a default receiver.
        var newLayerGO = new GameObject("NewLayer");
        var newLayer = newLayerGO.AddComponent<AG_ImageLayer>(); // for example
        newLayer.generator = generatorInstance; // link to the in-window generator

        // If node.obj is AG_TexReceiver_Default, attach the new layer there
        if (node.obj is AG_TexReceiver_Default def)
        {
            // Remove old layer if needed, or just do nothing
            // Usually you'd do def.GetComponent<AG_Layer>() but we can have only one layer or many
            // For now, let's just attach
            newLayerGO.transform.SetParent(def.transform, false);
        }
        // Rebuild
        BuildHierarchy();
    }

    /// <summary>
    /// Creates a new AG_TexReceiver_Default (or derived) in memory.
    /// Possibly attach it to some hidden parent if needed.
    /// </summary>
    private T CreateNewReceiver<T>(string name) where T : AG_TexReceiver_Default
    {
        GameObject go = new GameObject(name);
        go.hideFlags = HideFlags.HideAndDontSave;
        var receiver = go.AddComponent<T>();
        return receiver;
    }
}
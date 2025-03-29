using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

//public class AchievementGeneratorWindow : EditorWindow
//{
//    [SerializeField] private EditorScriptRegistry editorRegistry;

//    private Texture2D spritePreviewTex;

//    private Vector2 leftScroll, midScroll, rightScroll;

//    // Hierarchy node
//    private class Node
//    {
//        public Object obj; // Can be AG_TexReceiver_Default or AG_Layer
//        public List<Node> children = new List<Node>();
//        public bool expanded = true;
//    }

//    private Node rootNode;
//    private Object selectedObject;

//    // Cache for final textures
//    private List<List<Texture2D>> finalTextures;

//    [MenuItem("Window/Achievement Generator")]
//    private static void ShowWindow()
//    {
//        var wnd = GetWindow<AchievementGeneratorWindow>("Achievement Generator");
//        wnd.Show();
//    }

//    private void OnEnable()
//    {
//        BuildHierarchy(); // Build the tree when the window opens
//    }

//    private void OnGUI()
//    {
//        float w = position.width;
//        float h = position.height;

//        float leftWidth = w * 0.25f;
//        float midWidth = w * 0.4f;
//        float rightWidth = w - leftWidth - midWidth;

//        // Draw three areas
//        Rect leftRect = new Rect(0, 0, leftWidth, h);
//        Rect midRect = new Rect(leftRect.xMax, 0, midWidth, h);
//        Rect rightRect = new Rect(midRect.xMax, 0, rightWidth, h);

//        // Left column: hierarchy
//        GUILayout.BeginArea(leftRect, EditorStyles.helpBox);
//        leftScroll = GUILayout.BeginScrollView(leftScroll);
//        if (rootNode != null)
//        {
//            DrawNode(rootNode, 0);
//        }
//        else
//        {
//            GUILayout.Label("No hierarchy");
//        }
//        GUILayout.EndScrollView();
//        GUILayout.EndArea();

//        // Middle column: inspector + previews
//        GUILayout.BeginArea(midRect, EditorStyles.helpBox);
//        midScroll = GUILayout.BeginScrollView(midScroll);
//        DrawSelectedInspector();
//        GUILayout.EndScrollView();
//        GUILayout.EndArea();

//        // Right column: final result or extra
//        GUILayout.BeginArea(rightRect, EditorStyles.helpBox);
//        rightScroll = GUILayout.BeginScrollView(rightScroll);
//        // If you want a final preview, call DrawFinalPreview() here
//        GUILayout.EndScrollView();
//        GUILayout.EndArea();
//    }

//    /// <summary>
//    /// Recursively draw a node (and its children) to form the hierarchy tree
//    /// </summary>
//    private void DrawNode(Node node, int indent)
//    {
//        if (node.obj == null) return;

//        GUILayout.BeginHorizontal();
//        GUILayout.Space(indent * 15);

//        // Foldout (only if there are children)
//        if (node.children.Count > 0)
//        {
//            node.expanded = EditorGUILayout.Foldout(node.expanded, node.obj.name, true);
//        }
//        else
//        {
//            GUILayout.Label(node.obj.name);
//        }

//        // "Select" button
//        if (GUILayout.Button("Select", GUILayout.Width(50)))
//        {
//            selectedObject = node.obj;
//        }
//        GUILayout.EndHorizontal();

//        if (node.expanded)
//        {
//            foreach (var child in node.children)
//            {
//                DrawNode(child, indent + 1);
//            }
//        }
//    }

//    /// <summary>
//    /// Draw inspector and previews for the selected object
//    /// </summary>
//    private void DrawSelectedInspector()
//    {
//        if (selectedObject == null)
//        {
//            GUILayout.Label("Select an object in the left column", EditorStyles.centeredGreyMiniLabel);
//            return;
//        }

//        // Built-in inspector for the selected object
//        Editor editor = Editor.CreateEditor(selectedObject);
//        if (editor != null)
//        {
//            editor.OnInspectorGUI();
//        }

//        GUILayout.Space(10);

//        // Custom logic depending on type
//        if (selectedObject is AG_ImageLayer imageLayer)
//        {
//            DrawImageLayerInspector(imageLayer);
//        }
//        else if (selectedObject is AG_IconLayer iconLayer)
//        {
//            DrawIconLayerInspector(iconLayer);
//        }
//        else if (selectedObject is AG_SymbolLayer symbolLayer)
//        {
//            DrawSymbolLayerInspector(symbolLayer);
//        }
//        else if (selectedObject is AG_Layer layer)
//        {
//            GUILayout.Label("Layer Preview:", EditorStyles.boldLabel);
//            var tex = layer.GetTextures();
//            DrawTexturesGrid(tex);
//        }
//        else if (selectedObject is AG_TexReceiver_Default receiver)
//        {
//            GUILayout.Label("Receiver Preview:", EditorStyles.boldLabel);
//            var tex = receiver.GetTextures();
//            DrawTexturesGrid(tex);
//        }
//    }

//    /// <summary>
//    /// Inspector for AG_ImageLayer
//    /// Shows sprite preview with tile gizmos, then "Preview" button to generate tiles, then tile grid
//    /// </summary>
//    private void DrawImageLayerInspector(AG_ImageLayer imageLayer)
//    {
//        // Sprite field
//        imageLayer.sourceImage = (Sprite)EditorGUILayout.ObjectField(
//            "Source Sprite",
//            imageLayer.sourceImage,
//            typeof(Sprite),
//            false
//        );

//        // If sprite exists, draw it
//        if (imageLayer.sourceImage != null)
//        {
//            // Create/Update spritePreviewTex from the sprite
//            if (spritePreviewTex == null
//                || spritePreviewTex.width != (int)imageLayer.sourceImage.rect.width
//                || spritePreviewTex.height != (int)imageLayer.sourceImage.rect.height)
//            {
//                spritePreviewTex = new Texture2D(
//                    (int)imageLayer.sourceImage.rect.width,
//                    (int)imageLayer.sourceImage.rect.height,
//                    TextureFormat.RGBA32,
//                    false
//                );

//                Color[] pix = imageLayer.sourceImage.texture.GetPixels(
//                    (int)imageLayer.sourceImage.rect.x,
//                    (int)imageLayer.sourceImage.rect.y,
//                    (int)imageLayer.sourceImage.rect.width,
//                    (int)imageLayer.sourceImage.rect.height
//                );
//                spritePreviewTex.SetPixels(pix);
//                spritePreviewTex.Apply();
//            }

//            // Calculate preview size
//            float aspect = (float)spritePreviewTex.height / spritePreviewTex.width;
//            float previewWidth = EditorGUIUtility.currentViewWidth - 40;
//            float previewHeight = previewWidth * aspect;
//            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);

//            // Draw the texture
//            EditorGUI.DrawPreviewTexture(previewRect, spritePreviewTex);

//            // Draw tile gizmos (squares)
//            Handles.BeginGUI();
//            Color oldColor = Handles.color;
//            Handles.color = Color.red;

//            float scaleX = previewRect.width / imageLayer.sourceImage.rect.width;
//            float scaleY = previewRect.height / imageLayer.sourceImage.rect.height;

//            float ratio = imageLayer.tileSizeOnTexture / (float)imageLayer.tileSize;
//            float paddingTexX = imageLayer.paddingX * ratio;
//            float paddingTexY = imageLayer.paddingY * ratio;

//            for (int x = 0; x < imageLayer.tileCountX; x++)
//            {
//                for (int y = 0; y < imageLayer.tileCountY; y++)
//                {
//                    float tileLeft = imageLayer.gridOffsetX + x * (imageLayer.tileSizeOnTexture + paddingTexX);
//                    float tileBottom = imageLayer.gridOffsetY + y * (imageLayer.tileSizeOnTexture + paddingTexY);

//                    float leftUI = previewRect.x + tileLeft * scaleX;
//                    float topUI = previewRect.yMax - tileBottom * scaleY;
//                    float tileW_UI = imageLayer.tileSizeOnTexture * scaleX;
//                    float tileH_UI = imageLayer.tileSizeOnTexture * scaleY;

//                    Rect r = new Rect(leftUI, topUI - tileH_UI, tileW_UI, tileH_UI);

//                    Handles.DrawWireCube(
//                        new Vector3(r.x + r.width / 2f, r.y + r.height / 2f, 0),
//                        new Vector3(r.width, r.height, 0)
//                    );
//                }
//            }

//            Handles.color = oldColor;
//            Handles.EndGUI();
//        }

//        GUILayout.Space(10);

//        // "Preview" button
//        if (GUILayout.Button("Preview"))
//        {
//            imageLayer.GenerateTextures();
//        }

//        // Display generated textures
//        GUILayout.Label("Layer Preview:", EditorStyles.boldLabel);
//        var generated = imageLayer.GetTextures();
//        DrawTexturesGrid(generated);
//    }

//    /// <summary>
//    /// Inspector for AG_IconLayer
//    /// "Preview" button, then display the generated textures in a grid
//    /// </summary>
//    private void DrawIconLayerInspector(AG_IconLayer iconLayer)
//    {
//        // "Preview" button
//        if (GUILayout.Button("Preview"))
//        {
//            iconLayer.GenerateTextures();
//        }

//        GUILayout.Label("IconLayer Preview:", EditorStyles.boldLabel);
//        var generated = iconLayer.GetTextures();
//        DrawTexturesGrid(generated);
//    }

//    /// <summary>
//    /// Inspector for AG_SymbolLayer
//    /// "Preview" button, then display the generated textures in a grid
//    /// </summary>
//    private void DrawSymbolLayerInspector(AG_SymbolLayer symbolLayer)
//    {
//        // "Preview" button
//        if (GUILayout.Button("Preview"))
//        {
//            symbolLayer.GenerateTextures();
//        }

//        GUILayout.Label("SymbolLayer Preview:", EditorStyles.boldLabel);
//        var generated = symbolLayer.GetTextures();
//        DrawTexturesGrid(generated);
//    }

//    /// <summary>
//    /// Draw a grid of textures
//    /// </summary>
//    private void DrawTexturesGrid(List<List<Texture2D>> textures)
//    {
//        if (textures == null) return;

//        // Just a simple example: 64x64 thumbnails
//        for (int x = 0; x < textures.Count; x++)
//        {
//            GUILayout.BeginHorizontal();
//            for (int y = 0; y < textures[x].Count; y++)
//            {
//                Texture2D tex = textures[x][y];
//                if (tex == null) continue;
//                GUILayout.Box(tex, GUILayout.Width(64), GUILayout.Height(64));
//            }
//            GUILayout.EndHorizontal();
//        }
//    }

//    /// <summary>
//    /// Build the hierarchy starting from editorRegistry.achievementGenerator.texturesReceiver
//    /// </summary>
//    private void BuildHierarchy()
//    {
//        if (editorRegistry == null || editorRegistry.achievementGenerator == null || editorRegistry.achievementGenerator.texturesReceiver == null)
//        {
//            rootNode = null;
//            return;
//        }

//        var gen = editorRegistry.achievementGenerator;
//        rootNode = new Node { obj = gen.texturesReceiver };
//        BuildNodeRecursive(rootNode);
//    }

//    private void BuildNodeRecursive(Node node)
//    {
//        if (node.obj is AG_TexReceiver_Concat concat)
//        {
//            foreach (var childReceiver in concat.receivers)
//            {
//                var childNode = new Node { obj = childReceiver };
//                node.children.Add(childNode);
//                BuildNodeRecursive(childNode);
//            }
//        }
//        else if (node.obj is AG_TexReceiver_Multiply multiply)
//        {
//            foreach (var childReceiver in multiply.receivers)
//            {
//                var childNode = new Node { obj = childReceiver };
//                node.children.Add(childNode);
//                BuildNodeRecursive(childNode);
//            }
//        }
//        else if (node.obj is AG_TexReceiver_Default def)
//        {
//            // By default, no children for default receiver
//            // But if there's an AG_Layer component, we add it
//            var layer = def.GetComponent<AG_Layer>();
//            if (layer != null)
//            {
//                var childNode = new Node { obj = layer };
//                node.children.Add(childNode);
//            }
//        }
//        else if (node.obj is AG_Layer layer)
//        {
//            // No children for AG_Layer
//        }
//    }

//    // If you want a final preview in the right column, implement it here
//    private void DrawFinalPreview()
//    {
//        GUILayout.Label("Final Result:", EditorStyles.boldLabel);
//        if (GUILayout.Button("Generate Final"))
//        {
//            if (editorRegistry != null && editorRegistry.achievementGenerator != null && editorRegistry.achievementGenerator.texturesReceiver != null)
//            {
//                finalTextures = editorRegistry.achievementGenerator.texturesReceiver.GetTextures();
//            }
//        }

//        if (finalTextures == null)
//        {
//            GUILayout.Label("Press \"Generate Final\" to get the result");
//            return;
//        }

//        DrawTexturesGrid(finalTextures);
//    }
//}

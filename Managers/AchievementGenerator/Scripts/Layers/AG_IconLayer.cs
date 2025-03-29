using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AG_IconLayer : AG_Layer
{
    [Header("List of icon sprites")]
    public List<Sprite> iconSprites = new List<Sprite>();

    [Header("Split the icon into two vertical parts?")]
    public bool splitSymbolVertically = false;

    [Header("Tile size on the source texture (pixels)")]
    public int tileSizeOnTexture = 100;

    protected override void ProcessLayer()
    {
        generatedTextures = new List<List<Texture2D>>();

        // The number of rows = the number of icons
        tileCountY = iconSprites.Count;

        // For each column (tileCountX), generate the same set of icons
        for (int x = 0; x < tileCountX; x++)
        {
            List<Texture2D> rowTextures = new List<Texture2D>();
            for (int i = 0; i < tileCountY; i++)
            {
                // Generate 1 or 2 textures for each icon
                List<Texture2D> texturesForIcon = GenerateIconTextures(iconSprites[i]);
                // Add them to the row
                rowTextures.AddRange(texturesForIcon);
            }
            generatedTextures.Add(rowTextures);
        }
    }

    private List<Texture2D> GenerateIconTextures(Sprite icon)
    {
        // Render the full icon
        Texture2D fullIcon = RenderFullIcon(icon);

        if (!splitSymbolVertically)
        {
            return new List<Texture2D> { TextureUtils.ResizeTexture(fullIcon, tileSize, tileSize) };
        }
        else
        {
            int half = tileSizeOnTexture / 2;
            int halfPadding = (int)((float)(paddingY / 2) * ((float)tileSizeOnTexture / (float)generator.tileSize));
            Color[] blank = new Color[tileSizeOnTexture * tileSizeOnTexture];
            for (int i = 0; i < blank.Length; i++)
                blank[i] = Color.clear;

            // Top half
            Texture2D topTex = new Texture2D(tileSizeOnTexture, tileSizeOnTexture, TextureFormat.RGBA32, false);
            topTex.SetPixels(blank);
            Color[] topPixels = fullIcon.GetPixels(0, half + halfPadding, tileSizeOnTexture, half - halfPadding);
            topTex.SetPixels(0, 0, tileSizeOnTexture, half - halfPadding, topPixels);
            topTex.Apply();

            // Bottom half
            Texture2D bottomTex = new Texture2D(tileSizeOnTexture, tileSizeOnTexture, TextureFormat.RGBA32, false);
            bottomTex.SetPixels(blank);
            Color[] bottomPixels = fullIcon.GetPixels(0, 0, tileSizeOnTexture, half - halfPadding);
            bottomTex.SetPixels(0, half + halfPadding, tileSizeOnTexture, half - halfPadding, bottomPixels);
            bottomTex.Apply();

            return new List<Texture2D>
            {
                TextureUtils.ResizeTexture(topTex, tileSize, tileSize),
                TextureUtils.ResizeTexture(bottomTex, tileSize, tileSize)
            };
        }
    }

    /// <summary>
    /// Renders the icon into a RenderTexture (tileSizeOnTexture x tileSizeOnTexture) and returns a Texture2D.
    /// </summary>
    private Texture2D RenderFullIcon(Sprite icon)
    {
        Texture2D finalTexture = new Texture2D(tileSizeOnTexture, tileSizeOnTexture, TextureFormat.RGBA32, false);
        finalTexture.filterMode = FilterMode.Point;
        finalTexture.wrapMode = TextureWrapMode.Clamp;

        RenderTexture renderTex = new RenderTexture(tileSizeOnTexture, tileSizeOnTexture, 24);
        renderTex.Create();
        RenderTexture.active = renderTex;

        // Temporary Canvas + Camera
        GameObject canvasObj = new GameObject("TempCanvas");
        canvasObj.hideFlags = HideFlags.HideAndDontSave;
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;

        GameObject camObj = new GameObject("TempCamera");
        camObj.hideFlags = HideFlags.HideAndDontSave;
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.orthographic = true;
        cam.orthographicSize = tileSizeOnTexture / 2f;
        cam.transform.position = new Vector3(0, 0, -10f);
        cam.targetTexture = renderTex;
        canvas.worldCamera = cam;

        // CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(tileSizeOnTexture, tileSizeOnTexture);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Create Image
        GameObject imageObj = new GameObject("TempIcon");
        imageObj.hideFlags = HideFlags.HideAndDontSave;
        imageObj.transform.SetParent(canvasObj.transform, false);

        RectTransform rt = imageObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(tileSizeOnTexture, tileSizeOnTexture);

        Image uiImage = imageObj.AddComponent<Image>();
        uiImage.sprite = icon;
        uiImage.preserveAspect = true;
        uiImage.SetNativeSize();

        // Center
        rt.anchoredPosition = Vector2.zero;

        Canvas.ForceUpdateCanvases();
        cam.Render();

        finalTexture.ReadPixels(new Rect(0, 0, tileSizeOnTexture, tileSizeOnTexture), 0, 0);
        finalTexture.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;
        renderTex.Release();

        Object.DestroyImmediate(imageObj);
        Object.DestroyImmediate(canvasObj);
        Object.DestroyImmediate(camObj);

        return finalTexture;
    }
}


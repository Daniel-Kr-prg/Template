using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AG_SymbolLayer : AG_Layer
{
    [Header("Symbol settings")]
    public TMP_FontAsset fontAsset;
    public int fontSize = 64;
    public Color textColor = Color.white;
    public List<string> asciiSymbols = new List<string>();

    [Header("Generation settings")]
    public int tileSizeOnTexture = 100; // The size of the tile (width and height)
    public bool splitSymbolVertically = false; // Split the character into two parts

    protected override void ProcessLayer()
    {
        generatedTextures = new List<List<Texture2D>>();

        // Let the number of rows = the number of symbols
        tileCountY = asciiSymbols.Count;

        // For each column, we generate the same set of symbols
        for (int x = 0; x < tileCountX; x++)
        {
            List<Texture2D> rowTextures = new List<Texture2D>();
            for (int i = 0; i < tileCountY; i++)
            {
                // Generate 1 or 2 textures for the symbol
                List<Texture2D> texturesForSymbol = GenerateSymbolTextures(asciiSymbols[i]);
                rowTextures.AddRange(texturesForSymbol);
            }
            generatedTextures.Add(rowTextures);
        }
    }

    private List<Texture2D> GenerateSymbolTextures(string symbol)
    {
        // 1. Render the entire symbol (100x100)
        Texture2D fullLetter = RenderFullLetter(symbol);

        if (!splitSymbolVertically)
        {
            return new List<Texture2D> { TextureUtils.ResizeTexture(fullLetter, tileSize, tileSize) };
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
            Color[] topPixels = fullLetter.GetPixels(0, half + halfPadding, tileSizeOnTexture, half - halfPadding);
            topTex.SetPixels(0, 0, tileSizeOnTexture, half - halfPadding, topPixels);
            topTex.Apply();

            // Bottom half
            Texture2D bottomTex = new Texture2D(tileSizeOnTexture, tileSizeOnTexture, TextureFormat.RGBA32, false);
            bottomTex.SetPixels(blank);
            Color[] bottomPixels = fullLetter.GetPixels(0, 0, tileSizeOnTexture, half - halfPadding);
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
    /// Render a symbol into a RenderTexture (tileSizeOnTexture x tileSizeOnTexture) and return a Texture2D
    /// </summary>
    private Texture2D RenderFullLetter(string symbol)
    {
        Texture2D finalTexture = new Texture2D(tileSizeOnTexture, tileSizeOnTexture, TextureFormat.RGBA32, false);
        finalTexture.filterMode = FilterMode.Point;

        RenderTexture renderTex = new RenderTexture(tileSizeOnTexture, tileSizeOnTexture, 24, RenderTextureFormat.ARGB32);
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

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(tileSizeOnTexture, tileSizeOnTexture);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Create TMP_Text
        GameObject textObj = new GameObject("TempTMP_Text");
        textObj.hideFlags = HideFlags.HideAndDontSave;
        textObj.transform.SetParent(canvasObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(tileSizeOnTexture, tileSizeOnTexture);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = symbol;
        tmp.font = fontAsset;
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.enableAutoSizing = false;
        tmp.ForceMeshUpdate();

        Canvas.ForceUpdateCanvases();
        cam.Render();

        finalTexture.ReadPixels(new Rect(0, 0, tileSizeOnTexture, tileSizeOnTexture), 0, 0);
        finalTexture.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;
        renderTex.Release();

        Object.DestroyImmediate(textObj);
        Object.DestroyImmediate(canvasObj);
        Object.DestroyImmediate(camObj);

        return finalTexture;
    }
}

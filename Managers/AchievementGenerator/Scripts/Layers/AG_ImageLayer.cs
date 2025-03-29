using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AG_ImageLayer : AG_Layer
{
    [Header("Offset and size on the source texture (pixels)")]
    public int tileSizeOnTexture = 100;
    public int gridOffsetX = 0;
    public int gridOffsetY = 0;

    public Image sourceImage;

    protected override void ProcessLayer()
    {
        if (sourceImage == null)
        {
            Debug.LogError("AG_ImageLayer: Sprite is missing!");
            return;
        }

        Texture2D srcTex = sourceImage.sprite.texture;
        if (srcTex == null)
        {
            Debug.LogError("AG_ImageLayer: Could not get Texture2D from the sprite!");
            return;
        }

        generatedTextures = new List<List<Texture2D>>();

        float ratio = tileSizeOnTexture / (float)tileSize;

        for (int x = 0; x < tileCountX; x++)
        {
            List<Texture2D> rowList = new List<Texture2D>();
            for (int y = tileCountY - 1; y >= 0; y--)
            {
                int offsetX = 0;
                for (int j = 0; j < x; j++)
                {
                    offsetX += Mathf.RoundToInt((j < generator.paddingsX.Count ? generator.paddingsX[j] : paddingX) * ratio);
                }

                int startX = gridOffsetX + offsetX + Mathf.RoundToInt(x * tileSizeOnTexture);
                int startY = gridOffsetY + Mathf.RoundToInt(y * tileSizeOnTexture);

                Texture2D cropped = CropTexture(srcTex, startX, startY, tileSizeOnTexture, tileSizeOnTexture);
                if (cropped != null)
                    rowList.Add(TextureUtils.ResizeTexture(cropped, tileSize, tileSize));
            }
            generatedTextures.Add(rowList);
        }
    }

    private Texture2D CropTexture(Texture2D src, int startX, int startY, int w, int h)
    {
        if (src == null) return null;
        if (startX < 0 || startY < 0 || startX + w > src.width || startY + h > src.height)
        {
            Debug.LogWarning($"CropTexture: Out of bounds -> startX={startX}, startY={startY}, w={w}, h={h}, src={src.width}x{src.height}");
            return null;
        }

        Texture2D c = new Texture2D(w, h, TextureFormat.RGBA32, false);
        c.filterMode = FilterMode.Point;
        c.wrapMode = TextureWrapMode.Clamp;
        c.SetPixels(src.GetPixels(startX, startY, w, h));
        c.Apply();
        return c;
    }

    private void OnDrawGizmos()
    {
        if (generator == null) return;
        if (tileSizeOnTexture <= 0) return;

        Image img = GetComponent<Image>();
        if (img == null || img.sprite == null) return;

        RectTransform rt = GetComponent<RectTransform>();
        float w = rt.rect.width;
        float h = rt.rect.height;

        Sprite sp = img.sprite;
        float spW = sp.rect.width;
        float spH = sp.rect.height;

        float scaleX = w / spW;
        float scaleY = h / spH;
        float ratio = tileSizeOnTexture / (float)tileSize;
        float paddingTexX = paddingX * ratio;
        float paddingTexY = paddingY * ratio;

        Gizmos.color = Color.red;

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                float tileLeft = gridOffsetX + x * (tileSizeOnTexture + paddingTexX);
                float tileBottom = gridOffsetY + y * (tileSizeOnTexture + paddingTexY);

                float leftUI = tileLeft * scaleX;
                float bottomUI = tileBottom * scaleY;
                float tileW_UI = tileSizeOnTexture * scaleX;
                float tileH_UI = tileSizeOnTexture * scaleY;

                float cx = rt.rect.xMin + leftUI + tileW_UI / 2f;
                float cy = rt.rect.yMin + bottomUI + tileH_UI / 2f;

                Vector3 worldCenter = rt.TransformPoint(new Vector3(cx, cy, 0));
                Gizmos.DrawWireCube(worldCenter, new Vector3(tileW_UI, tileH_UI, 1f));
            }
        }
    }
}
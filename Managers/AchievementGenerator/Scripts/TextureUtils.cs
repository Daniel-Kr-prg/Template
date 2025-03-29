using System.Collections.Generic;
using UnityEngine;

public class TextureUtils
{
    public static Texture2D CloneTexture(Texture2D source)
    {
        Texture2D clone = new Texture2D(source.width, source.height, source.format, false);
        clone.filterMode = FilterMode.Point;
        clone.wrapMode = TextureWrapMode.Clamp;
        clone.SetPixels(source.GetPixels());
        clone.Apply();
        return clone;
    }

    public static List<List<Texture2D>> CloneTextureList(List<List<Texture2D>> original)
    {
        var cloned = new List<List<Texture2D>>();
        foreach (var row in original)
        {
            var newRow = new List<Texture2D>();
            foreach (var tex in row)
            {
                if (tex == null)
                {
                    newRow.Add(null);
                }
                else
                {
                    newRow.Add(CloneTexture(tex));
                }
            }
            cloned.Add(newRow);
        }
        return cloned;
    }

    public static Sprite TextureToSprite(Texture2D tex)
    {
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    public static Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Point;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight, source.format, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    public static Texture2D AlphaBlendTextures(Texture2D bottomTex, Texture2D topTex)
    {
        if (bottomTex.width != topTex.width || bottomTex.height != topTex.height)
        {
            Debug.LogWarning("Размеры текстур не совпадают!");
            return null;
        }

        int width = bottomTex.width;
        int height = bottomTex.height;

        // Получаем пиксели исходных текстур
        Color[] bottomPixels = bottomTex.GetPixels();
        Color[] topPixels = topTex.GetPixels();
        Color[] outPixels = new Color[bottomPixels.Length];

        // Проходим по всем пикселям
        for (int i = 0; i < outPixels.Length; i++)
        {
            Color b = bottomPixels[i];
            Color t = topPixels[i];

            // Формула "top over bottom"
            // outA = t.a + b.a * (1 - t.a)
            float outA = t.a + b.a * (1f - t.a);

            // Если итоговая альфа 0 - пиксель прозрачен
            if (Mathf.Approximately(outA, 0f))
            {
                outPixels[i] = Color.clear;
            }
            else
            {
                // outRGB = (t.rgb * t.a + b.rgb * b.a * (1 - t.a)) / outA
                float outR = (t.r * t.a + b.r * b.a * (1f - t.a)) / outA;
                float outG = (t.g * t.a + b.g * b.a * (1f - t.a)) / outA;
                float outB = (t.b * t.a + b.b * b.a * (1f - t.a)) / outA;
                outPixels[i] = new Color(outR, outG, outB, outA);
            }
        }

        // Создаем новую текстуру и записываем результат
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.SetPixels(outPixels);
        result.Apply();
        return result;
    }

}

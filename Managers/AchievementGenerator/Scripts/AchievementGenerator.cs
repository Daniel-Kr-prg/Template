using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AchievementGenerator : MonoBehaviour
{
    [Header("Basic Settings")]
    public int tileSize = 256;

    [Header("Padding Settings")]
    public int paddingX = 28; // Default padding
    public List<int> paddingsX = new List<int>(); // Individual cell paddings

    public int paddingY = 5;

    [Header("Tile Count Settings")]
    public int tileCountX = 7;
    public int tileCountY = 3;

    public AG_TexReceiver_Default texturesReceiver; // Receiver for final textures

    [Button("Save sprites")]
    public void SaveTexturesAsPNGs()
    {
        if (texturesReceiver == null)
        {
            Debug.LogError("texturesReceiver is not assigned!");
            return;
        }

        // Retrieve textures from the texturesReceiver
        List<List<Texture2D>> textureColumns = texturesReceiver.GetTextures();
        string folderPath = "Assets/AG_TempStorage";

#if UNITY_EDITOR
        // Check if the folder exists; if not, create it
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }
#endif

        // Iterate over each column (X index) and each texture in the column (multiple variants)
        for (int x = 0; x < textureColumns.Count; x++)
        {
            List<Texture2D> column = textureColumns[x];
            for (int i = 0; i < column.Count; i++)
            {
                int variant = (i / tileCountY) + 1;
                int y = i % tileCountY;

                // Format file name: "ach_X_Y_variantN.png"
                string fileName = $"ach_{x}_{y}_variant{variant}.png";
                string filePath = System.IO.Path.Combine(folderPath, fileName);

                Texture2D texture = column[i];
                byte[] pngData = texture.EncodeToPNG();
                if (pngData != null)
                {
                    File.WriteAllBytes(filePath, pngData);
                }
                else
                {
                    Debug.LogError("Failed to encode texture to PNG: " + fileName);
                }
            }
        }

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        Debug.Log("PNG images saved to " + folderPath);
    }
    //private void OnDrawGizmos()
    //{
    //    RectTransform rectTransform = GetComponent<RectTransform>();
    //    if (rectTransform == null) return;

    //    Gizmos.color = Color.red;
    //    Vector3 anchorWorldPos = rectTransform.position;

    //    // --- Расчет общей ширины и высоты сетки с учетом отступов ---
    //    float totalWidth = tileSize * tileCountX;
    //    for (int i = 0; i < tileCountX - 1; i++)
    //    {
    //        totalWidth += (i < paddingsX.Count) ? paddingsX[i] : paddingX;
    //    }

    //    float totalHeight = tileSize * tileCountY + (tileCountY - 1) * paddingY;

    //    // --- Выравниваем сетку по центру ---
    //    float startX = anchorWorldPos.x - totalWidth / 2 + tileSize / 2;
    //    float startY = anchorWorldPos.y + totalHeight / 2 - tileSize / 2;

    //    for (int y = 0; y < tileCountY; y++)
    //    {
    //        float yPos = startY - y * (tileSize + paddingY);

    //        float currentX = startX;
    //        for (int x = 0; x < tileCountX; x++)
    //        {
    //            Vector3 worldPos = new Vector3(currentX, yPos, 0);
    //            Gizmos.DrawWireCube(worldPos, new Vector3(tileSize, tileSize, 1));

    //            // Добавляем отступ к X, если это не последний элемент
    //            if (x < tileCountX - 1)
    //                currentX += tileSize + ((x < paddingsX.Count) ? paddingsX[x] : paddingX);
    //        }
    //    }
    //}

}

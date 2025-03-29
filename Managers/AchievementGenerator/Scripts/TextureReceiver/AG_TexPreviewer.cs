using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class AG_TexPreviewer : MonoBehaviour
{
    [SerializeField] private AchievementGenerator generator;

    [Required, Sirenix.OdinInspector.OnValueChanged("ReceiverChanged")]
    public AG_TexReceiver_Default receiver;

    private List<GameObject> createdUIImages = new List<GameObject>();

    private void ReceiverChanged()
    {
        Unsubscribe();
        Subscribe();
        RefreshPreview();
    }

    private void Subscribe()
    {
        if (receiver != null)
            receiver.OnTexturesUpdated += (x) => DisplayGeneratedTextures(x);
    }

    private void Unsubscribe()
    {
        if (receiver != null)
            receiver.OnTexturesUpdated -= (x) => DisplayGeneratedTextures(x);
    }

    [Button("Refresh Preview")]
    public void RefreshPreview()
    {
        if (receiver == null) return;
        DisplayGeneratedTextures(receiver.GetTextures());
    }

    public void DisplayGeneratedTextures(List<List<Texture2D>> generatedTextures)
    {
        foreach (var rt in createdUIImages)
        {
            DestroyImmediate(rt);
        }
        createdUIImages.Clear();

        int tileSize = generator.tileSize;

        for (int x = 0; x < generatedTextures.Count; x++)
        {
            for (int y = 0; y < generatedTextures[x].Count; y++)
            {
                Texture2D t = generatedTextures[x][y];
                if (t == null) continue;

                GameObject tileObj = new GameObject($"Tile_{x}_{y}", typeof(RectTransform), typeof(Image));
                tileObj.transform.SetParent(transform, false);

                RectTransform rt = tileObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(tileSize, tileSize);
                rt.anchoredPosition = new Vector2(
                    x * (tileSize + generator.paddingX),
                    -y * (tileSize + generator.paddingY)
                );

                Image img = tileObj.GetComponent<Image>();
                img.sprite = TextureUtils.TextureToSprite(t);
                createdUIImages.Add(tileObj);
            }
        }
    }
}

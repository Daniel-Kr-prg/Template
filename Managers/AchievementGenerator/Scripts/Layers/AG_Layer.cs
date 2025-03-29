using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class AG_Layer : MonoBehaviour
{
    [SerializeField] public AchievementGenerator generator;

    [Header("Tile grid settings")]
    public int tileCountX = 7;
    public int tileCountY = 1;

    public int paddingY => generator.paddingY;
    public int paddingX => generator.paddingX;
    public int tileSize => generator.tileSize;

    protected List<List<Texture2D>> generatedTextures;

    public virtual List<Texture2D> GetTextureListForTile(int tileIndex)
    {
        if (generatedTextures == null || generatedTextures.Count == 0)
            return new List<Texture2D>();

        if (tileIndex < 0 || tileIndex >= generatedTextures.Count)
            return new List<Texture2D>();

        return generatedTextures[tileIndex];
    }

    public virtual List<List<Texture2D>> GetTextures()
    {
        GenerateTextures();
        return generatedTextures;
    }

    [Button("Generate Textures")]
    public void GenerateTextures()
    {
        generatedTextures = new List<List<Texture2D>>();
        ProcessLayer();
    }

    protected abstract void ProcessLayer();
}

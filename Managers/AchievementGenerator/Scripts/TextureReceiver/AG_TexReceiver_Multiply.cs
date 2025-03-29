using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AG_TexReceiver_Multiply : AG_TexReceiver_Default
{
    public List<AG_TexReceiver_Default> receivers = new List<AG_TexReceiver_Default>();

    public override List<List<Texture2D>> GetTextures()
    {
        if (receivers == null || receivers.Count == 0)
        {
            Debug.LogWarning("AG_TexReceiver_Multiply: —писок ресиверов пуст!");
            return new List<List<Texture2D>>();
        }

        // »нициализируем результат текстурами первого ресивера (делаем глубокую копию)
        List<List<Texture2D>> result = TextureUtils.CloneTextureList(receivers[0].GetTextures());

        // ƒл€ каждого последующего ресивера выполн€ем "перемножение" текстур (Cartesian product)
        for (int r = 1; r < receivers.Count; r++)
        {
            List<List<Texture2D>> nextLayer = receivers[r].GetTextures();

            if (nextLayer.Count != result.Count)
            {
                Debug.LogWarning($"AG_TexReceiver_Multiply: Ќесоответствие количества столбцов между ресиверами [{r}]!");
                continue;
            }

            for (int col = 0; col < result.Count; col++)
            {
                List<Texture2D> combinedColumn = new List<Texture2D>();
                List<Texture2D> currentColumn = result[col];
                List<Texture2D> nextColumn = nextLayer[col];

                foreach (Texture2D baseTex in currentColumn)
                {
                    foreach (Texture2D topTex in nextColumn)
                    {
                        // —оздаем копию базовой текстуры, чтобы не мен€ть исходную
                        Texture2D newTex = TextureUtils.CloneTexture(baseTex);
                        // Ќакладываем topTex поверх newTex с учетом прозрачности
                        newTex = TextureUtils.AlphaBlendTextures(newTex, topTex);
                        combinedColumn.Add(newTex);
                    }
                }
                result[col] = combinedColumn;
            }
        }
        InvokeUpdatePreview(result);
        return result;
    }

   


    
}

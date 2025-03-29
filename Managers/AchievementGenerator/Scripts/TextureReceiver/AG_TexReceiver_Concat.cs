using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AG_TexReceiver_Concat : AG_TexReceiver_Default
{
    public List<AG_TexReceiver_Default> receivers;

    public override List<List<Texture2D>> GetTextures()
    {
        List<AG_TexReceiver_Default> tmp_receivers = receivers.ToList();

        List<List<Texture2D>> textures = tmp_receivers[0].GetTextures();
        tmp_receivers.RemoveAt(0);

        foreach (AG_TexReceiver_Default receiver in tmp_receivers)
        {
            List<List<Texture2D>> texturesOnLayer = receiver.GetTextures();

            for (int i = 0; i < textures.Count; i++)
            {
                textures[i].AddRange(texturesOnLayer[(i + texturesOnLayer.Count) % texturesOnLayer.Count]);
            }
        }
        InvokeUpdatePreview(textures);
        return textures;
    }
}

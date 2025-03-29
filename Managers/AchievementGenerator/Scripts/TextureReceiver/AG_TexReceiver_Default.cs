using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class AG_TexReceiver_Default : MonoBehaviour
{
    public event System.Action<List<List<Texture2D>>> OnTexturesUpdated;

    [Button("Get Textures")]
    public void GetTexturesButtonCall()
    {
        GetTextures();
    }

    public void InvokeUpdatePreview(List<List<Texture2D>> tex)
    {
        OnTexturesUpdated?.Invoke(tex);
    }

    public virtual List<List<Texture2D>> GetTextures()
    {
        List<List<Texture2D>> textures = TextureUtils.CloneTextureList(GetComponent<AG_Layer>().GetTextures());
        OnTexturesUpdated?.Invoke(textures);
        return textures;
    }
}

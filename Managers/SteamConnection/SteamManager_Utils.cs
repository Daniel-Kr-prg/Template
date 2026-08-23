#if UNITY_STANDALONE || UNITY_EDITOR
using Steamworks;
using UnityEngine;

public class SteamManager_Utils
{
    public static async void GetUserAvatar(SteamId steamID, System.Action<Texture2D> callback)
    {
        var friendObj = new Friend(steamID);
        var avatar = await friendObj.GetLargeAvatarAsync();
        if (!avatar.HasValue)
        {
            callback(null);
            return;
        }

        var width = avatar.Value.Width;
        var height = avatar.Value.Height;
        var data = avatar.Value.Data;

        Texture2D tempTex = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        tempTex.LoadImage(data);
        tempTex.Apply();

        Texture2D flipped = FlipTexture(tempTex);
        callback(flipped);

        Texture2D.DestroyImmediate(tempTex);
    }

    private static Texture2D FlipTexture(Texture2D source)
    {
        int w = source.width;
        int h = source.height;
        Texture2D result = new Texture2D(w, h, source.format, false);
        for (int y = 0; y < h; y++)
        {
            result.SetPixels(0, y, w, 1, source.GetPixels(0, h - y - 1, w, 1));
        }
        result.Apply();
        return result;
    }
}
#endif

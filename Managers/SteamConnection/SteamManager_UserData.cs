#if UNITY_STANDALONE || UNITY_EDITOR
using Steamworks;
using UnityEngine;

public class SteamManager_UserData
{
    private SteamManager steam;

    public SteamId CurrentUserId { get; private set; }
    public Friend CurrentUser { get; private set; }

    public string CurrentUserName { get; private set; }
    public Texture2D CurrentUserAvatar { get; private set; }

    public SteamManager_UserData(SteamManager steam)
    {
        this.steam = steam;
        FetchUserData();
    }

    private void FetchUserData()
    {
        CurrentUserId = SteamClient.SteamId;
        CurrentUser = new Friend(CurrentUserId);

        CurrentUserName = CurrentUser.Name;

        SteamManager_Utils.GetUserAvatar(CurrentUserId, (x) => { CurrentUserAvatar = x; });
    }
}
#endif

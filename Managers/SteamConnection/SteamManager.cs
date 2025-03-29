using DanieloZ.Managers;
using Sirenix.OdinInspector;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class SteamManager : SingletonManager<SteamManager>
{
    [SerializeField] private uint appID;

    private Lobby? _lobby = null;

    public bool Active = false;
    public bool IsInitialized { get; private set; } = false;
    public bool IsInLobby => _lobby != null;

    // User info
    public SteamManager_UserData user;

    public bool IsCloudEnabledForApp { get; private set; }

    private void Start()
    {
        StagesManager.Instance.AppStages.RegisterStageStartAction(AppStageName.ConnectServices, "SteamConnection", () =>
        {
            if (appID != 0)
            {
                InitializeSteam(appID);
            }
        });
        StagesManager.Instance.AppStages.RegisterStageChangeCondition(AppStageName.ConnectServices, "SteamConnection_Success", new StageCondition(new Func<bool>(
            () => IsInitialized
            )));
        
        StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_SteamManagerReady");
    }
    #region Core
    public void InitializeSteam(uint appId)
    {
        try
        {
            SteamClient.Init(appId, true);
            IsCloudEnabledForApp = SteamRemoteStorage.IsCloudEnabledForApp;
         
            user = new(this);
            
            IsInitialized = true;
            DebugMessage($"Steam initialized");

            StagesManager.Instance.AppStages.currentStage.SatisfyCondition("SteamConnection_Success");
        }
        catch (Exception e)
        {
            DebugError($"Unable to initialize Steam: {e.Message}");
        }
    }

    private bool CheckInitialized()
    {
        if (!IsInitialized)
        {
            DebugError("Steam not initialized");
            return false;
        }
        return true;
    }
    private void RegisterSteamCallbacks()
    {
        SteamInventory.OnDefinitionsUpdated += OnInventoryDefinitionsUpdated;
        SteamInventory.OnInventoryUpdated += OnInventoryUpdated;

        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;

        SteamFriends.OnGameOverlayActivated += OnGameOverlayActivated;
    }

    private void OnDestroy()
    {
        if (IsInitialized)
        {
            UnregisterSteamCallbacks();

            SteamClient.Shutdown();
            IsInitialized = false;
        }
    }

    private void UnregisterSteamCallbacks()
    {
        SteamInventory.OnDefinitionsUpdated -= OnInventoryDefinitionsUpdated;
        SteamInventory.OnInventoryUpdated -= OnInventoryUpdated;

        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;

        SteamFriends.OnGameOverlayActivated -= OnGameOverlayActivated;
    }
    #endregion

    #region Event calls
    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        EventManager.CallEvent(EventName.Steam_OnLobbyCreated, new object[] { result, lobby });
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        _lobby = lobby;
        EventManager.CallEvent(EventName.Steam_OnLobbyEntered, new object[] { lobby });
    }

    private void OnLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId steamId)
    {
        EventManager.CallEvent(EventName.Steam_OnLobbyGameCreated, new object[] { lobby, ip, port, steamId });
    }

    private void OnGameOverlayActivated(bool active)
    {
        EventManager.CallEvent(EventName.Steam_OnGameOverlayActivated, new object[] { active });
    }

    private void OnInventoryDefinitionsUpdated()
    {
        EventManager.CallEvent(EventName.Steam_OnInventoryDefinitionsUpdated);
    }

    private void OnInventoryUpdated(InventoryResult result)
    {
        EventManager.CallEvent(EventName.Steam_OnInventoryUpdated, new object[] { result });
    }
    #endregion

    #region Lobby
    public async Task<bool> CreateLobby(int members)
    {
        if (!CheckInitialized())
            return false;

        _lobby = await SteamMatchmaking.CreateLobbyAsync(members);
        return IsInLobby;
    }

    #endregion
}





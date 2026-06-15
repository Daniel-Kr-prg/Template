using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DanieloZ.Managers
{
    public class EventManager : SingletonManager<EventManager>
    {
        EventBook<EventName> mainEventsBook = new();

        private void Start()
        {
            // Additional handling before stage changing

            // Satisfy stage condition
            StagesManager.Instance.AppStages.currentStage.SatisfyCondition("StagesManager_EventManagerReady");
        }

        public static void AddCallback(EventName eventName, Action<object[]> callback)
        {
            Instance.mainEventsBook.AddCallback(eventName, callback);
        }

        public static void AddAsyncCallback(EventName eventName, Action<object[]> asyncCallback)
        {
            Instance.mainEventsBook.AddAsyncCallback(eventName, asyncCallback);
        }

        public static void RemoveCallback(EventName eventName, Action<object[]> callback)
        {
            Instance.mainEventsBook.RemoveCallback(eventName, callback);
        }

        public static void RemoveAsyncCallback(EventName eventName, Action<object[]> asyncCallback)
        {
            Instance.mainEventsBook.RemoveAsyncCallback(eventName, asyncCallback);
        }

        public static void CallEvent(EventName eventName, object[] data = null)
        {
            Instance.mainEventsBook.CallEvent(eventName, data);
        }

        public static async Task CallEventAsync(EventName eventName, object[] data = null)
        {
            await Instance.mainEventsBook.CallEventAsync(eventName, data);
        }

    }
    public class EventBook<T>
    {
        Dictionary<T, List<Action<object[]>>> syncEvents = new();
        Dictionary<T, List<Action<object[]>>> asyncEvents = new();

        public void AddCallback(T eventName, Action<object[]> callback)
        {
            if (syncEvents.TryGetValue(eventName, out List<Action<object[]>> list))
                list.Add(callback);
            else
                syncEvents.Add(eventName, new() { callback });
        }

        public void AddAsyncCallback(T eventName, Action<object[]> asyncCallback)
        {
            if (asyncEvents.TryGetValue(eventName, out var list))
                list.Add(asyncCallback);
            else
                asyncEvents.Add(eventName, new() { asyncCallback });
        }

        public void RemoveCallback(T eventName, Action<object[]> callback)
        {
            if (syncEvents.TryGetValue(eventName, out List<Action<object[]>> list))
            {
                list.Remove(callback);
                if (list.Count == 0)
                    syncEvents.Remove(eventName);
            }
        }

        public void RemoveAsyncCallback(T eventName, Action<object[]> asyncCallback)
        {
            if (asyncEvents.TryGetValue(eventName, out var list))
            {
                list.Remove(asyncCallback);
                if (list.Count == 0)
                    asyncEvents.Remove(eventName);
            }
        }

        public void CallEvent(T eventName, object[] data = null)
        {
            if (syncEvents.TryGetValue(eventName, out List<Action<object[]>> list))
            {
                foreach (Action<object[]> callback in list)
                {
                    callback?.Invoke(data);
                }
            }
        }

        public async Task CallEventAsync(T eventName, object[] data = null)
        {
            if (asyncEvents.TryGetValue(eventName, out var list))
            {
                var tasks = list.Select(callback => Task.Run(() => callback(data)));
                await Task.WhenAll(tasks);
            }
        }
    }

    public enum EventName
    {
        // Steam
        Steam_OnLobbyCreated, // <Result, Data.Lobby> | You created a lobby
        Steam_OnLobbyEntered, // <Data.Lobby> | You joined a lobby
        Steam_OnLobbyGameCreated, // <Data.Lobby, uint, ushort, SteamId> | A game server has been associated with the lobby
        Steam_OnLobbyInvite, // <Friend, Data.Lobby> | Someone invited you to a lobby
        Steam_OnLobbyMemberDisconnected, // <Data.Lobby, Friend> | The lobby member left the room
        Steam_OnFriendInvite, 
        Steam_OnFriendChatMessage, // <Friend, string, string> (friend, msgtype, message) | You'll need to turn on ListenForFriendsMessages
        Steam_OnGameLobbyJoinRequested, // <Data.Lobby, SteamId>
        Steam_OnGameOverlayActivated, // <bool>

        // Steam Inventory
        Steam_OnInventoryDefinitionsUpdated,  // Called when item definitions have been updated
        Steam_OnInventoryUpdated,            // <InventoryResult> | Called when the user's inventory has been updated

        // Steam Lobby (!)
        Steam_OnLobbyChatMessage, // <Data.Lobby, Friend, string> 

        // Save Management
        SaveManager_Save,
        SaveManager_ItemsCount,
        SaveManager_Load,
        SaveManager_LoadFailure,

        // UI
        UI_StartupScreenRelease, // releases StartupScreen when a timed screen keeps itself until event
        UI_LoadingScreenRelease, // releases LoadingScreen when a timed screen keeps itself until event

        // World Interaction
        WorldInteraction_OnMatchingSlotInserted, // payload: [string id, World3DButtonSlotBase slot, World3DSlotItem item]

        // Pixel Voxel Puzzle MVP Flow
        PixelPuzzle_OnLevelSelectRequested, // payload: [int levelIndex, PixelPuzzleAsset asset]
        PixelPuzzle_OnLevelSelected, // payload: [int levelIndex, PixelPuzzleAsset asset]
        PixelPuzzle_OnLevelStartRequested, // payload: [PixelPuzzleAsset asset]
        PixelPuzzle_OnLevelStarted, // payload: [PuzzleSession session]
        PixelPuzzle_OnLevelReset, // payload: [PuzzleSession session]
        PixelPuzzle_OnLevelPackRequested, // payload: [PuzzleSession session]
        PixelPuzzle_OnLevelPacked, // payload: [PuzzleSession session]
        PixelPuzzle_OnLevelCompleted, // payload: [PuzzleSession session]
        PixelPuzzle_OnLevelLockedSelected, // payload: [int levelIndex, PixelPuzzleAsset asset]
        PixelPuzzle_OnPiecePlaced, // payload: [PuzzleSession session, PuzzlePiece piece, PuzzlePlacementArea placementSurface]
        PixelPuzzle_OnPieceRemoved, // payload: [PuzzleSession session, PuzzlePiece piece, PuzzlePlacementArea placementSurface]
        PixelPuzzle_OnFastTravelRequested, // payload: [string pointId, Transform anchor]
        PixelPuzzle_OnFastTravelCompleted, // payload: [string pointId, Transform anchor]
        PixelPuzzle_OnInputContextChanged, // payload: [PixelVoxelPuzzleInputContext previous, PixelVoxelPuzzleInputContext current]
        PixelPuzzle_OnMainMenuOpened, // payload: [UIMainMenuModule module, string cameraLockId]
        PixelPuzzle_OnMainMenuClosed, // payload: [UIMainMenuModule module]
        PixelPuzzle_OnOptionsOpened, // payload: [UIMainMenuModule module, string cameraLockId]
        PixelPuzzle_OnOptionsClosed, // payload: [UIMainMenuModule module]
        PixelPuzzle_OnUIActionPressed, // payload: [string actionId, UnityEngine.Object source, optional context]
        PixelPuzzle_OnClearSaveRequested, // payload: [UIOptionsModule module]
        PixelPuzzle_OnClearSaveConfirmed, // payload: [UIOptionsModule module]
        PixelPuzzle_OnSaveStarted, // payload: [string reason]
        PixelPuzzle_OnSaveCompleted, // payload: [string reason]
        PixelPuzzle_OnSaveFailed, // payload: [string reason]
        PixelPuzzle_OnSaveLoaded, // payload: [bool success]
        PixelPuzzle_OnObjectPickedUp, // payload: [IInteractionPickupTarget target]
        PixelPuzzle_OnObjectReleased, // payload: [IInteractionPickupTarget target]
        PixelPuzzle_OnMainMenuTitleSpawned, // payload: [string titleId, GameObject instance]
        PixelPuzzle_OnMainMenuTitleUnlocked, // payload: [string titleId]

        // Game Flow
        Game_OnLevelStarted,
        Game_OnLevelFinished,
        Game_OnBrickDestroyed,    // payload: [BrickController]
        Game_OnXpGained,          // payload: [float xpAmount]
        Game_OnAllWavesCompleted, // no payload

        // Ball
        Game_OnBallSpawned,       // payload: [Game_DefaultBall]
        Game_OnBallLost,          // payload: [Game_DefaultBall]
        Game_OnBallBounced,
        Game_OnBallXPChanged,
        Game_OnBallEaten,              
        Game_OnBallCreatedWithNoFreeBall,
        Game_OnBallCreatedWithFreeBall,  

        // Wave
        Game_OnWaveStarted,       // payload: [int waveIndex]
        Game_OnWavesReset,                    
        Game_OnWaveStep,   
        Game_OnWaveMaxStepsDecreased,
        Game_OnWaveMaxStepsIncreased,
        Game_OnWaveStepTimeChanged,
        Game_OnWaveDifficultyChanged,


        Game_OnComboChanged,           
        Game_OnCoinsChanged,         

        // Triggered when a new wave is about to appear (for ghost ball logic, etc)
        Game_OnNewWaveComing, // Triggered when new wave movement starts (bricks move)
        Game_OnNewWaveCame,   // Triggered when new wave movement ends (bricks stop)

        // Pixel Voxel Puzzle MVP audio routing
        PixelPuzzle_OnObjectHoverStarted, // payload: [IInteractionHoverTarget target, InteractionCollider collider]
        PixelPuzzle_OnObjectHoverEnded, // payload: [IInteractionHoverTarget target, InteractionCollider collider]
        PixelPuzzle_OnPieceRotated, // payload: [PuzzlePieceRotationController rotation]
        PixelPuzzle_OnWorldControlDragStarted, // payload: [string controlId, UnityEngine.Object source]
        PixelPuzzle_OnWorldControlDragEnded, // payload: [string controlId, UnityEngine.Object source]
        PixelPuzzle_OnBoxOpened, // payload: [PuzzleBoxController box]
        PixelPuzzle_OnBoxClosed, // payload: [PuzzleBoxController box]
        PixelPuzzle_OnBoxPieceCaptureStarted, // payload: [PuzzleBoxController box, PuzzlePieceHoldable holdable, PuzzlePiece piece]
        PixelPuzzle_OnBoxPourStarted, // payload: [PuzzleBoxController box]
        PixelPuzzle_OnBoxPourEnded, // payload: [PuzzleBoxController box]
        PixelPuzzle_OnBoxAutoDropChargeStarted, // payload: [PuzzleBoxController box, PuzzleBoxAutoDropZone zone]
        PixelPuzzle_OnBoxAutoDropChargeCompleted, // payload: [PuzzleBoxController box, PuzzleBoxAutoDropZone zone]
        PixelPuzzle_OnBoxAutoDropChargeCancelled, // payload: [PuzzleBoxController box, PuzzleBoxAutoDropZone zone]
        WorldInteraction_OnSlotItemInserted, // payload: [World3DButtonSlotBase slot, World3DSlotItem item]
        WorldInteraction_OnOptionRollerChanged, // payload: [World3DOptionRoller roller, int index]
        WorldInteraction_OnSliderValueChanged // payload: [World3DSlider slider, float value]
    }

}

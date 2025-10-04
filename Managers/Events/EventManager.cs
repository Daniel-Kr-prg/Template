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

        // LettersBag events
        LettersBag_Changed,         // payload: [Dictionary<Letter, int> currentBag]
        LettersBag_LetterTaken,     // payload: [Letter letter, int countLeft]
        LettersBag_LetterAdded,     // payload: [Letter letter, int countNow]
        LettersBag_Refilled,        // payload: [Dictionary<Letter, int> currentBag]
        LettersBag_Emptied,         // payload: []
        
        // WordPath specific events
        Game_OnWordConfirmed        // payload: [string word] - when player confirms a word
    }

}
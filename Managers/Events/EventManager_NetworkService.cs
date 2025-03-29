using DanieloZ.Managers;
using System;
using Unity.Netcode;
using UnityEngine;

public class EventManager_NetworkService : MonoBehaviour
{
    private void Start()
    {

    }

    // CLIENT -> SERVER
    // Клиент вызывает этот метод, когда хочет "выбросить" событие на сервере
    [ServerRpc(RequireOwnership = false)]
    public void SendEventToServerRpc(EventName eventName, string jsonData)
    {
        // Локально на сервере вызываем событие
        // (если нужно логика на сервере)
        EventManager.CallEvent(eventName, new object[] { jsonData });

        // Optionally, рассылаем клиентам
        // SendEventToAllClients(eventName, new object[]{ jsonData });
    }

    // SERVER -> CLIENT
    // Сервер вызывает, чтобы разослать событие всем клиентам
    [ClientRpc]
    private void BroadcastEventClientRpc(EventName eventName, string jsonData)
    {
        // На клиенте вызываем локальное событие
        // (чтобы сработали подписчики EventManager)
        EventManager.CallEvent(eventName, new object[] { jsonData });
    }

    /// <summary>
    /// Рассылаем событие только одному клиенту (по ClientId)
    /// </summary>
    [ClientRpc]
    private void BroadcastEventToOneClientRpc(EventName eventName, string jsonData, ClientRpcParams rpcParams = default)
    {
        EventManager.CallEvent(eventName, new object[] { jsonData });
    }

    public void SendEventToOneClient(ulong clientId, EventName eventName, string jsonData)
    {
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };
        BroadcastEventToOneClientRpc(eventName, jsonData, rpcParams);
    }
}

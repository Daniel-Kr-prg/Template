using DanieloZ.InputManagement;
using Unity.Netcode;
using UnityEngine;

public class NetCodeNetworkManager : SingletonManager<InputManager>
{
    [SerializeField]
    NetworkManager _networkManager;

    public NetCodeNetworkManager()
    {
    }
}

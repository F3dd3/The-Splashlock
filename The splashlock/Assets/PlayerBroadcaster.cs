using Unity.Netcode;
using UnityEngine;

public class PlayerBroadcaster : NetworkBehaviour
{
    public static PlayerBroadcaster Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowLocalJoinMessage(string message)
    {
        Debug.Log(message);
    }
}

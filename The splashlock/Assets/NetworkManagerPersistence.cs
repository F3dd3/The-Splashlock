using UnityEngine;
using Unity.Netcode;

public class NetworkManagerPersistence : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}

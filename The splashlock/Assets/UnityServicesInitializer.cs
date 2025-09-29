using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;

public class UnityServicesInitializer : MonoBehaviour
{
    public static bool ServicesInitialized = false;

    async void Awake()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
            ServicesInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Services initialization failed: " + e.Message);
            ServicesInitialized = false;
        }
    }
}

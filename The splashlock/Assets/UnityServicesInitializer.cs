using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;

public class UnityServicesInitializer : MonoBehaviour
{
    public static bool ServicesInitialized = false;
    private static bool isInitializing = false;

    async void Awake()
    {
        if (ServicesInitialized || isInitializing)
            return;

        isInitializing = true;

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            ServicesInitialized = true;
            Debug.Log("Unity Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Services initialization failed: " + e.Message);
        }
        finally
        {
            isInitializing = false;
        }
    }
}

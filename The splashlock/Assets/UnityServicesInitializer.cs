using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class UnityServicesInitializer : MonoBehaviour
{
    public static UnityServicesInitializer Instance { get; private set; }
    public static bool ServicesInitialized = false;
    private static bool isInitializing = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _ = InitializeServicesSafe();
    }

    private async Task InitializeServicesSafe()
    {
        if (ServicesInitialized || isInitializing) return;

        isInitializing = true;

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                catch (System.InvalidOperationException e)
                {
                    // Already signing in: geen fout, gewoon loggen
                    Debug.Log("Sign-in skipped (already signing in): " + e.Message);
                }
            }

            ServicesInitialized = true;
            Debug.Log("Unity Services ready. PlayerId: " + AuthenticationService.Instance.PlayerId);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Unity Services initialization failed: " + e.Message);
        }
        finally
        {
            isInitializing = false;
        }
    }
}

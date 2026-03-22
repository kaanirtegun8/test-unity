using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UnityAuthBootstrap : MonoBehaviour
{
    [SerializeField] private bool signInOnStart = true;
    [SerializeField] private string fallbackDisplayName = "Player";

    private readonly AuthStateStore authStateStore = new AuthStateStore();
    private bool isSignInInProgress;

    private async void Start()
    {
        if (!signInOnStart)
        {
            return;
        }

        await InitializeAndSignInAnonymousAsync();
    }

    [ContextMenu("Sign In Anonymous")]
    public void SignInAnonymousFromContextMenu()
    {
        _ = InitializeAndSignInAnonymousAsync();
    }

    private async Task InitializeAndSignInAnonymousAsync()
    {
        if (isSignInInProgress)
        {
            return;
        }

        isSignInInProgress = true;
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            string safeUserId = AuthenticationService.Instance.PlayerId;
            if (string.IsNullOrWhiteSpace(safeUserId))
            {
                safeUserId = "unity_auth_anonymous";
            }

            string safeDisplayName = string.IsNullOrWhiteSpace(fallbackDisplayName)
                ? "Player"
                : fallbackDisplayName.Trim();

            AuthUser authenticatedUser = new AuthUser
            {
                userId = safeUserId,
                displayName = safeDisplayName,
                isAnonymous = true
            };

            authStateStore.SetAuthenticatedUser(authenticatedUser);
            authStateStore.SyncCurrentUser(LobbyStateStore.Local.SyncLocalPlayerFromAuth);
            Debug.Log($"UnityAuthBootstrap: Anonymous sign-in success. userId={safeUserId}, displayName={safeDisplayName}", this);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"UnityAuthBootstrap: Anonymous sign-in failed safely. {exception.Message}");
        }
        finally
        {
            isSignInInProgress = false;
        }
    }
}

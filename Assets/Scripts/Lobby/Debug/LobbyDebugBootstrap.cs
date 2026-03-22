using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyDebugBootstrap : MonoBehaviour
{
    [SerializeField] private bool createLobbyOnStart;
    [SerializeField] private bool queryLobbiesOnStart;
    [SerializeField] private string debugLobbyName = "Debug Lobby";
    [SerializeField] private int debugMaxPlayers = 4;
    [SerializeField] private bool debugIsPrivate;
    [SerializeField] private int debugTreasureCount = 2;
    [SerializeField] private int debugSelectedMapIndex;
    [SerializeField] private float authReadyTimeoutSeconds = 8f;

    private UnityLobbyService lobbyService;

    private void Awake()
    {
        lobbyService = new UnityLobbyService();
    }

    private async void Start()
    {
        if (!createLobbyOnStart && !queryLobbiesOnStart)
        {
            return;
        }

        await RunDebugFlowAsync();
    }

    [ContextMenu("Run Lobby Debug Flow")]
    public void RunLobbyDebugFlowFromContextMenu()
    {
        _ = RunDebugFlowAsync();
    }

    private async Task RunDebugFlowAsync()
    {
        bool isAuthReady = await WaitForAuthenticationReadyAsync();
        if (!isAuthReady)
        {
            Debug.LogWarning("LobbyDebugBootstrap: Authentication is not ready. Skipping debug lobby calls.");
            return;
        }

        if (createLobbyOnStart)
        {
            await TryCreateLobbyAsync();
        }

        if (queryLobbiesOnStart)
        {
            await TryQueryLobbiesAsync();
        }
    }

    private async Task TryCreateLobbyAsync()
    {
        if (lobbyService == null)
        {
            Debug.LogWarning("LobbyDebugBootstrap: UnityLobbyService is not initialized.");
            return;
        }

        string safeLobbyName = string.IsNullOrWhiteSpace(debugLobbyName) ? "Debug Lobby" : debugLobbyName.Trim();
        int safeMaxPlayers = Mathf.Max(1, debugMaxPlayers);
        int safeTreasureCount = Mathf.Max(0, debugTreasureCount);
        int safeMapIndex = Mathf.Max(0, debugSelectedMapIndex);

        try
        {
            Lobby createdLobby = await lobbyService.CreateLobbyAsync(
                safeLobbyName,
                safeMaxPlayers,
                debugIsPrivate,
                safeTreasureCount,
                safeMapIndex);

            if (createdLobby != null)
            {
                Debug.Log($"LobbyDebugBootstrap: Create success. lobbyId={createdLobby.Id}, lobbyName={createdLobby.Name}");
                return;
            }

            Debug.LogWarning("LobbyDebugBootstrap: Create returned null lobby.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"LobbyDebugBootstrap: Create threw exception safely. {exception.Message}");
        }
    }

    private async Task TryQueryLobbiesAsync()
    {
        if (lobbyService == null)
        {
            Debug.LogWarning("LobbyDebugBootstrap: UnityLobbyService is not initialized.");
            return;
        }

        try
        {
            QueryResponse queryResponse = await lobbyService.QueryLobbiesAsync();
            if (queryResponse != null)
            {
                int lobbyCount = queryResponse.Results != null ? queryResponse.Results.Count : 0;
                Debug.Log($"LobbyDebugBootstrap: Query success. lobbyCount={lobbyCount}");
                return;
            }

            Debug.LogWarning("LobbyDebugBootstrap: Query returned null response.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"LobbyDebugBootstrap: Query threw exception safely. {exception.Message}");
        }
    }

    private static bool IsAuthenticationReady()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            return false;
        }

        try
        {
            var authService = AuthenticationService.Instance;
            return authService != null && authService.IsSignedIn;
        }
        catch (ServicesInitializationException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> WaitForAuthenticationReadyAsync()
    {
        if (IsAuthenticationReady())
        {
            return true;
        }

        float safeTimeout = Mathf.Max(0.2f, authReadyTimeoutSeconds);
        DateTime deadlineUtc = DateTime.UtcNow.AddSeconds(safeTimeout);

        while (DateTime.UtcNow < deadlineUtc)
        {
            await Task.Delay(120);
            if (IsAuthenticationReady())
            {
                return true;
            }
        }

        return IsAuthenticationReady();
    }
}

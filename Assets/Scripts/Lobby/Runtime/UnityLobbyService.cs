using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class UnityLobbyService
{
    private const string LobbyDataKeyRoomName = "roomName";
    private const string LobbyDataKeyTreasureCount = "treasureCount";
    private const string LobbyDataKeySelectedMapIndex = "selectedMapIndex";
    private const string LobbyDataKeyIsPrivate = "isPrivate";

    public async Task<Lobby> CreateLobbyAsync(
        string roomName,
        int maxPlayers,
        bool isPrivate,
        int treasureCount,
        int selectedMapIndex)
    {
        if (!EnsureSignedIn())
        {
            return null;
        }

        string safeRoomName = string.IsNullOrWhiteSpace(roomName) ? "Fun Match" : roomName.Trim();
        int safeMaxPlayers = Math.Max(1, maxPlayers);
        int safeTreasureCount = Math.Max(0, treasureCount);
        int safeSelectedMapIndex = Math.Max(0, selectedMapIndex);

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject>
            {
                { LobbyDataKeyRoomName, new DataObject(DataObject.VisibilityOptions.Public, safeRoomName) },
                { LobbyDataKeyTreasureCount, new DataObject(DataObject.VisibilityOptions.Public, safeTreasureCount.ToString()) },
                { LobbyDataKeySelectedMapIndex, new DataObject(DataObject.VisibilityOptions.Public, safeSelectedMapIndex.ToString()) },
                { LobbyDataKeyIsPrivate, new DataObject(DataObject.VisibilityOptions.Public, isPrivate ? "1" : "0") }
            }
        };

        try
        {
            return await LobbyService.Instance.CreateLobbyAsync(safeRoomName, safeMaxPlayers, options);
        }
        catch (LobbyServiceException exception)
        {
            Debug.LogWarning($"UnityLobbyService.CreateLobbyAsync failed: {exception.Reason} ({exception.ErrorCode}) {exception.Message}");
            return null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"UnityLobbyService.CreateLobbyAsync unexpected error: {exception.Message}");
            return null;
        }
    }

    public async Task<QueryResponse> QueryLobbiesAsync()
    {
        if (!EnsureSignedIn())
        {
            return null;
        }

        QueryLobbiesOptions options = new QueryLobbiesOptions();
        try
        {
            return await LobbyService.Instance.QueryLobbiesAsync(options);
        }
        catch (LobbyServiceException exception)
        {
            Debug.LogWarning($"UnityLobbyService.QueryLobbiesAsync failed: {exception.Reason} ({exception.ErrorCode}) {exception.Message}");
            return null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"UnityLobbyService.QueryLobbiesAsync unexpected error: {exception.Message}");
            return null;
        }
    }

    public async Task<Lobby> JoinLobbyByIdAsync(string lobbyId)
    {
        if (!EnsureSignedIn())
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            Debug.LogWarning("UnityLobbyService.JoinLobbyByIdAsync skipped: lobbyId is empty.");
            return null;
        }

        try
        {
            return await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId.Trim());
        }
        catch (LobbyServiceException exception)
        {
            Debug.LogWarning($"UnityLobbyService.JoinLobbyByIdAsync failed: {exception.Reason} ({exception.ErrorCode}) {exception.Message}");
            return null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"UnityLobbyService.JoinLobbyByIdAsync unexpected error: {exception.Message}");
            return null;
        }
    }

    public async Task<bool> LeaveLobbyAsync(string lobbyId, string playerId)
    {
        if (!EnsureSignedIn())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(lobbyId) || string.IsNullOrWhiteSpace(playerId))
        {
            Debug.LogWarning("UnityLobbyService.LeaveLobbyAsync skipped: lobbyId or playerId is empty.");
            return false;
        }

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(lobbyId.Trim(), playerId.Trim());
            return true;
        }
        catch (LobbyServiceException exception)
        {
            Debug.LogWarning($"UnityLobbyService.LeaveLobbyAsync failed: {exception.Reason} ({exception.ErrorCode}) {exception.Message}");
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"UnityLobbyService.LeaveLobbyAsync unexpected error: {exception.Message}");
            return false;
        }
    }

    private static bool EnsureSignedIn()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            Debug.LogWarning("UnityLobbyService: Unity Services is not initialized yet.");
            return false;
        }

        try
        {
            var authService = AuthenticationService.Instance;
            if (authService != null && authService.IsSignedIn)
            {
                return true;
            }

            Debug.LogWarning("UnityLobbyService: Authentication required before Lobby calls.");
            return false;
        }
        catch (ServicesInitializationException exception)
        {
            Debug.LogWarning($"UnityLobbyService: Authentication singleton is not ready yet. {exception.Message}");
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"UnityLobbyService: Authentication readiness check failed safely. {exception.Message}");
            return false;
        }
    }
}

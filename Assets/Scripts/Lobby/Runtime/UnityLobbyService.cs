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
    private const string LobbyPlayerDataKeyDisplayName = "displayName";
    private const string LobbyPlayerDataKeyIsReady = "isReady";

    public bool LastGetLobbyWasNotFound { get; private set; }

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
            if (IsAlreadyMemberJoinConflict(exception))
            {
                Lobby existingLobby = await GetLobbyAsync(lobbyId.Trim());
                if (existingLobby != null)
                {
                    Debug.Log($"UnityLobbyService.JoinLobbyByIdAsync: player already member, using existing lobby {existingLobby.Id}.");
                    return existingLobby;
                }
            }

            Debug.LogWarning($"UnityLobbyService.JoinLobbyByIdAsync failed: {exception.Reason} ({exception.ErrorCode}) {exception.Message}");
            return null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"UnityLobbyService.JoinLobbyByIdAsync unexpected error: {exception.Message}");
            return null;
        }
    }

    public async Task<Lobby> GetLobbyAsync(string lobbyId)
    {
        LastGetLobbyWasNotFound = false;

        if (!EnsureSignedIn())
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            Debug.LogWarning("UnityLobbyService.GetLobbyAsync skipped: lobbyId is empty.");
            return null;
        }

        try
        {
            return await LobbyService.Instance.GetLobbyAsync(lobbyId.Trim());
        }
        catch (LobbyServiceException exception)
        {
            LastGetLobbyWasNotFound = IsNotFoundLobbyException(exception);
            if (LastGetLobbyWasNotFound)
            {
                Debug.Log($"UnityLobbyService.GetLobbyAsync: lobby is not available anymore. {exception.Reason} ({exception.ErrorCode})");
            }
            else
            {
                Debug.LogWarning($"UnityLobbyService.GetLobbyAsync failed: {exception.Reason} ({exception.ErrorCode}) {exception.Message}");
            }

            return null;
        }
        catch (Exception exception)
        {
            LastGetLobbyWasNotFound = false;
            Debug.LogWarning($"UnityLobbyService.GetLobbyAsync unexpected error: {exception.Message}");
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

    public async Task<bool> UpdatePlayerReadyAsync(
        string lobbyId,
        bool isReady,
        string playerId = null,
        string displayName = null)
    {
        if (!EnsureSignedIn())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            Debug.LogWarning("UnityLobbyService.UpdatePlayerReadyAsync skipped: lobbyId is empty.");
            return false;
        }

        string safePlayerId = string.IsNullOrWhiteSpace(playerId)
            ? AuthenticationService.Instance.PlayerId
            : playerId.Trim();
        if (string.IsNullOrWhiteSpace(safePlayerId))
        {
            Debug.LogWarning("UnityLobbyService.UpdatePlayerReadyAsync skipped: playerId is empty.");
            return false;
        }

        Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>
        {
            { LobbyPlayerDataKeyIsReady, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isReady ? "1" : "0") }
        };

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            playerData[LobbyPlayerDataKeyDisplayName] = new PlayerDataObject(
                PlayerDataObject.VisibilityOptions.Member,
                displayName.Trim());
        }

        UpdatePlayerOptions options = new UpdatePlayerOptions
        {
            Data = playerData
        };

        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(lobbyId.Trim(), safePlayerId, options);
            return true;
        }
        catch (LobbyServiceException exception)
        {
            Debug.LogWarning($"UnityLobbyService.UpdatePlayerReadyAsync failed: {exception.Reason} ({exception.ErrorCode}) {exception.Message}");
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"UnityLobbyService.UpdatePlayerReadyAsync unexpected error: {exception.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteLobbyAsync(string lobbyId)
    {
        if (!EnsureSignedIn())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            Debug.LogWarning("UnityLobbyService.DeleteLobbyAsync skipped: lobbyId is empty.");
            return false;
        }

        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(lobbyId.Trim());
            return true;
        }
        catch (LobbyServiceException exception)
        {
            Debug.LogWarning($"UnityLobbyService.DeleteLobbyAsync failed: {exception.Reason} ({exception.ErrorCode}) {exception.Message}");
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"UnityLobbyService.DeleteLobbyAsync unexpected error: {exception.Message}");
            return false;
        }
    }

    public RoomState MapLobbyToRoomState(Lobby lobby, bool useAvailableSlotsFallback = false)
    {
        if (lobby == null)
        {
            return null;
        }

        int safeMaxPlayers = Math.Max(1, lobby.MaxPlayers);
        List<PlayerState> players = MapLobbyPlayers(lobby);
        if (useAvailableSlotsFallback && players.Count == 0)
        {
            int fallbackPlayerCount = Math.Clamp(safeMaxPlayers - lobby.AvailableSlots, 0, safeMaxPlayers);
            for (int i = 0; i < fallbackPlayerCount; i++)
            {
                players.Add(new PlayerState());
            }
        }

        string roomNameFromData = GetLobbyDataValueSafe(lobby, LobbyDataKeyRoomName);
        string safeRoomName = !string.IsNullOrWhiteSpace(lobby.Name) ? lobby.Name : roomNameFromData;
        int treasureCount = ParseLobbyDataIntSafe(lobby, LobbyDataKeyTreasureCount, 0);
        int selectedMapIndex = ParseLobbyDataIntSafe(lobby, LobbyDataKeySelectedMapIndex, 0);
        string safeRoomId = !string.IsNullOrWhiteSpace(lobby.Id) ? lobby.Id : lobby.LobbyCode;

        return new RoomState
        {
            roomId = string.IsNullOrWhiteSpace(safeRoomId) ? "000000" : safeRoomId,
            roomName = string.IsNullOrWhiteSpace(safeRoomName) ? "Fun Match" : safeRoomName,
            isPublic = !lobby.IsPrivate,
            maxPlayers = safeMaxPlayers,
            selectedMapIndex = Math.Max(0, selectedMapIndex),
            treasureCount = Math.Max(0, treasureCount),
            players = players
        };
    }

    private static bool IsAlreadyMemberJoinConflict(LobbyServiceException exception)
    {
        if (exception == null)
        {
            return false;
        }

        string message = exception.Message ?? string.Empty;
        return exception.Reason == LobbyExceptionReason.LobbyConflict &&
               message.IndexOf("already a member", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsNotFoundLobbyException(LobbyServiceException exception)
    {
        if (exception == null)
        {
            return false;
        }

        return exception.Reason == LobbyExceptionReason.LobbyNotFound ||
               exception.Reason == LobbyExceptionReason.EntityNotFound ||
               exception.Reason == LobbyExceptionReason.Gone;
    }

    private static List<PlayerState> MapLobbyPlayers(Lobby lobby)
    {
        List<PlayerState> players = new List<PlayerState>();
        if (lobby == null || lobby.Players == null)
        {
            return players;
        }

        for (int i = 0; i < lobby.Players.Count; i++)
        {
            Player lobbyPlayer = lobby.Players[i];
            if (lobbyPlayer == null)
            {
                continue;
            }

            string safePlayerId = string.IsNullOrWhiteSpace(lobbyPlayer.Id) ? string.Empty : lobbyPlayer.Id.Trim();
            string displayNameFromData = GetLobbyPlayerDataValueSafe(lobbyPlayer, LobbyPlayerDataKeyDisplayName);
            string safeDisplayName = !string.IsNullOrWhiteSpace(displayNameFromData)
                ? displayNameFromData
                : (!string.IsNullOrWhiteSpace(safePlayerId) ? safePlayerId : "Player");
            bool isReady = ParseLobbyPlayerDataBoolSafe(lobbyPlayer, LobbyPlayerDataKeyIsReady, false);

            players.Add(new PlayerState
            {
                playerId = safePlayerId,
                displayName = safeDisplayName,
                isReady = isReady,
                isHost = !string.IsNullOrWhiteSpace(lobby.HostId) &&
                         string.Equals(lobby.HostId, safePlayerId, StringComparison.Ordinal),
                selectedColorIndex = 0
            });
        }

        return players;
    }

    private static int ParseLobbyDataIntSafe(Lobby lobby, string key, int fallback)
    {
        string value = GetLobbyDataValueSafe(lobby, key);
        if (int.TryParse(value, out int parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static string GetLobbyDataValueSafe(Lobby lobby, string key)
    {
        if (lobby == null || lobby.Data == null || string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        if (!lobby.Data.TryGetValue(key, out DataObject dataObject) || dataObject == null)
        {
            return string.Empty;
        }

        return dataObject.Value ?? string.Empty;
    }

    private static string GetLobbyPlayerDataValueSafe(Player player, string key)
    {
        if (player == null || player.Data == null || string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        if (!player.Data.TryGetValue(key, out PlayerDataObject dataObject) || dataObject == null)
        {
            return string.Empty;
        }

        return dataObject.Value ?? string.Empty;
    }

    private static bool ParseLobbyPlayerDataBoolSafe(Player player, string key, bool fallback)
    {
        string value = GetLobbyPlayerDataValueSafe(player, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string normalized = value.Trim();
        if (string.Equals(normalized, "1", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(normalized, "0", StringComparison.Ordinal))
        {
            return false;
        }

        if (bool.TryParse(normalized, out bool parsed))
        {
            return parsed;
        }

        return fallback;
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
